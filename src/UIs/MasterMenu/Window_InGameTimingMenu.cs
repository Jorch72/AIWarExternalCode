using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public class Window_InGameTimingMenu : ToggleableWindowController
    {
        public static Window_InGameTimingMenu Instance;
        public Window_InGameTimingMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }
        
        public class bTogglePause : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( World_AIW2.Instance.IsPaused ? "Unpause" : "Pause" );
            }
            public override void HandleClick() { Input_MainHandler.HandleInner( 0, "TogglePause" ); }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bIncreaseFrameSize : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "+FrameSize (" ).Add( Engine_AIW2.Instance.FrameSizeMultiplier ).Add( ")" );
            }
            public override void HandleClick() { Input_MainHandler.HandleInner( 0, "IncreaseFrameSize" ); }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bDecreaseFrameSize : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "-FrameSize (" ).Add( Engine_AIW2.Instance.FrameSizeMultiplier ).Add( ")" );
            }
            public override void HandleClick() { Input_MainHandler.HandleInner( 0, "DecreaseFrameSize" ); }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bIncreaseFrameFrequency : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                buffer.Add( "+FrameFrequency (" );
                if ( Engine_AIW2.Instance.FrameFrequencyMultiplier >= 10 )
                    buffer.Add( "MAX" );
                else
                    buffer.Add( Engine_AIW2.Instance.FrameFrequencyMultiplier );
                buffer.Add( ")" );
            }
            public override void HandleClick() { Input_MainHandler.HandleInner( 0, "IncreaseFrameFrequency" ); }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bDecreaseFrameFrequency : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                buffer.Add( "-FrameFrequency (" );
                if ( Engine_AIW2.Instance.FrameFrequencyMultiplier >= 10 )
                    buffer.Add( "MAX" );
                else
                    buffer.Add( Engine_AIW2.Instance.FrameFrequencyMultiplier );
                buffer.Add( ")" );
            }

            public override void HandleClick() { Input_MainHandler.HandleInner( 0, "DecreaseFrameFrequency" ); }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }
    }
}