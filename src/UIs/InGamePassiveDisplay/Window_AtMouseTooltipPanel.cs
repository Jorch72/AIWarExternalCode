using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Arcen.AIW2.External
{
    public class Window_AtMouseTooltipPanel : WindowControllerAbstractBase
    {
        public Window_AtMouseTooltipPanel()
        {
            this.IsAtMouseTooltip = true;
        }

        public class bPanel : ImageButtonAbstractBase
        {
            public static bPanel Instance;
            public bPanel() { Instance = this; }

            public ArcenUI_ImageButton Element;
            //private ArcenUI_Image.SubImageGroup SubImages;
            private SubTextGroup SubTexts;
            private string NextTextToShow = string.Empty;
            private string WrappedNextTextToShow = string.Empty;
            private string LastTextToShow = string.Empty;
            private bool NeedsToResize = true;
            private DateTime TimeLastSet;
            private float LastRequestedWidth;
            private float LastRequestedHeight;

            public override void UpdateContent( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup _SubImages, SubTextGroup _SubTexts )
            {
                //this.SubImages = _SubImages;
                this.SubTexts = _SubTexts;
                string nextText = this.WrappedNextTextToShow;
                if ( this.LastTextToShow.Length <= 0 && nextText.Length <= 0 )
                    return;

                try
                {
                    if ( this.LastTextToShow != nextText )
                    {
                        this.LastTextToShow = nextText;
                    }
                    ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                    buffer.Add( this.LastTextToShow );
                    SubTexts[0].Text.FinishWritingToBuffer();
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Exception in UpdateContent for the single text element:" + e.ToString(), Verbosity.ShowAsError );
                }

                float targetXPixel = ArcenInput.MouseScreenX + 5;
                float targetYPixel = ArcenUI.Instance.LastScreenHeight - (ArcenInput.MouseScreenY + 5 );
                float maxXPixel = ArcenUI.Instance.LastScreenWidth - this.LastRequestedWidth;
                float maxYPixel = ArcenUI.Instance.LastScreenHeight - this.LastRequestedHeight;
                targetXPixel = Mathf.Min( targetXPixel, maxXPixel );
                targetYPixel = Mathf.Min( targetYPixel, maxYPixel );
                float xPercent = targetXPixel / ArcenUI.Instance.PixelsPerPercent_X;
                float yPercent = targetYPixel / ArcenUI.Instance.PixelsPerPercent_Y;
                this.Element.Window.SubContainer.Alignment.XAlignment.Offset = xPercent;
                this.Element.Window.SubContainer.Alignment.YAlignment.Offset = yPercent;
            }

            public override void SetElement( ArcenUI_Element Element )
            {
                this.Element = (ArcenUI_ImageButton)Element;
            }

            public override MouseHandlingResult HandleClick()
            {
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                if ( this.NextTextToShow.Length <= 0 )
                    return true;
                if ( ( ArcenUI.Instance.CurrentNow - this.TimeLastSet ).TotalSeconds > 0.5 )
                    return true;
                return false;
            }

            public void SetText( string Text )
            {
                if ( Text == null )
                    Text = string.Empty;
                this.NextTextToShow = Text;
                this.TimeLastSet = ArcenUI.Instance.CurrentNow;
                this.NeedsToResize = true;
            }

            public override void OnUpdate()
            {
                if ( this.Element != null && this.NeedsToResize )
                {
                    this.NeedsToResize = false;

                    string text = this.NextTextToShow;
                    Vector2 messageSize = CalculateMessageSize( ref text );
                    this.WrappedNextTextToShow = text;

                    float textInset = 6;
                    
                    this.LastRequestedWidth = textInset + messageSize.x + textInset;
                    this.LastRequestedHeight = textInset + messageSize.y + textInset;

                    float referenceSideLength = 50;
                    float xScale = this.LastRequestedWidth / referenceSideLength;
                    float yScale = this.LastRequestedHeight / referenceSideLength;

                    ArcenUI_Image.SubImage subImage = this.Element.SubImages[0];
                    subImage.Img.rectTransform.sizeDelta = new Vector2( referenceSideLength * xScale, referenceSideLength * yScale );

                    float textSideLength = referenceSideLength - ( textInset * 2 );
                    
                    SubText subText = this.Element.SubTexts[0];
                    subText.ReferenceText.rectTransform.sizeDelta = new Vector2( textSideLength * xScale, textSideLength * yScale );
                }
                base.OnUpdate();
            }

            private readonly ArcenDoubleCharacterBuffer buffer_CalculateMessageSize = new ArcenDoubleCharacterBuffer();
            private Vector2 CalculateMessageSize( ref string message )
            {
                if ( this.SubTexts == null )
                    return Vector2.zero;

                TextMeshProUGUI text = this.SubTexts[1].ReferenceText;

                TMP_TextInfo info = text.GetTextInfo( message );

                buffer_CalculateMessageSize.EnsureResetForNextUpdate();

                Vector2 result = Vector2.zero;
                int newLinesToSkip = 0;
                for(int i = 0; i < info.lineCount;i++)
                {
                    TMP_LineInfo lineInfo = info.lineInfo[i];
                    float lineLength = lineInfo.maxAdvance + 20; // fudge factor because it always seems to sell itself a little short
                    result.x = Mathf.Max( result.x, lineLength );
                    result.y += lineInfo.lineHeight;
                    if ( i > 0 )
                    {
                        if ( lineInfo.length < 5 ) // empty lines tend to about 3.8 long, for some reason
                            newLinesToSkip = 2;
                        if ( newLinesToSkip > 0 )
                            newLinesToSkip--;
                        else
                            buffer_CalculateMessageSize.Add( '\n' );
                    }
                    ArcenCharacterBuffer debugBuffer = new ArcenCharacterBuffer();
                    for ( int j = lineInfo.firstCharacterIndex; j <= lineInfo.lastCharacterIndex; j++ )
                    {
                        debugBuffer.Add( message[j] );
                        buffer_CalculateMessageSize.Add( message[j] );
                    }
                }

                message = buffer_CalculateMessageSize.GetStringAndResetForNextUpdate();

                return result;
            }
        }
    }
}
