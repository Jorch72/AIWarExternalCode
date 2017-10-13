using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public class Window_GUIToggling : WindowControllerAbstractBase
    {
        public Window_GUIToggling()
        {
            this.ShouldShowEvenWhenGUIHidden = true;
        }

        public class bToggleGUI : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );

                if ( ArcenUI.Instance.InHideGUIMode )
                    Buffer.Add( "Show GUI" );
                else
                    Buffer.Add( "Hide GUI" );
            }

            public override MouseHandlingResult HandleClick() { ArcenUI.Instance.InHideGUIMode = !ArcenUI.Instance.InHideGUIMode;
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class tVersion : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "Version " ).Add( GameVersionTable.Instance.CurrentVersion.GetAsString() );
            }

            public override void OnUpdate() { }
        }
    }
}