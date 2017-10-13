using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public class Window_InGameDeveloperToolsMenu : ToggleableWindowController
    {
        public static Window_InGameDeveloperToolsMenu Instance;
        public Window_InGameDeveloperToolsMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        public class bGiveMetal : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Give Metal" );
            }
            public override MouseHandlingResult HandleClick() { Input_MainHandler.HandleInner( 0, "DebugGiveSomeMetal" );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bGiveScience : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Give Science" );
            }
            public override MouseHandlingResult HandleClick() { Input_MainHandler.HandleInner( 0, "DebugGiveScience" );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bGiveHacking : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Give Hacking" );
            }
            public override MouseHandlingResult HandleClick()
            {
                GameCommand command = GameCommand.Create( GameCommandType.Debug_GiveHacking );
                World_AIW2.Instance.QueueGameCommand( command, true );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bSpawnWave : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Spawn AI Wave" );
            }
            public override MouseHandlingResult HandleClick() { Input_MainHandler.HandleInner( 0, "DebugSendNextWave" );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bIncreaseAIP : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Increase AIP" );
            }
            public override MouseHandlingResult HandleClick() { Input_MainHandler.HandleInner( 0, "DebugIncreaseAIP" );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bRevealAll : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Scout All" );
            }
            public override MouseHandlingResult HandleClick() { Input_MainHandler.HandleInner( 0, "ScoutAll" );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bToggleTracingMenu : WindowTogglingButtonController
        {
            public bToggleTracingMenu() : base( "Tracing", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameTracingMenu.Instance; }
        }

        //public class bAdd : ButtonAbstractBase
        //{
        //    public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
        //    {
        //        return "+" + Window_InGameDeveloperToolsMenu.scale;
        //    }

        //    public override MouseHandlingResult HandleClick()
        //    {
        //        ArcenUI.Instance.ExtraSpecialSuperDuperFudgeFactor += scale;
        //        ArcenDebugging.ArcenDebugLog( "New factor:" + ArcenUI.Instance.ExtraSpecialSuperDuperFudgeFactor );
        //        ArcenUI.Instance.PositionAndSizeAllElements();
        //    }

        //    public override void OnUpdate()
        //    {
        //    }
        //}

        //public class bSubtract : ButtonAbstractBase
        //{
        //    public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
        //    {
        //        return "-" + Window_InGameDeveloperToolsMenu.scale;
        //    }

        //    public override MouseHandlingResult HandleClick()
        //    {
        //        ArcenUI.Instance.ExtraSpecialSuperDuperFudgeFactor -= scale;
        //        ArcenDebugging.ArcenDebugLog( "New factor:" + ArcenUI.Instance.ExtraSpecialSuperDuperFudgeFactor );
        //        ArcenUI.Instance.PositionAndSizeAllElements();
        //    }

        //    public override void OnUpdate()
        //    {
        //    }
        //}

        //public class bIncreaseScale : ButtonAbstractBase
        //{
        //    public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
        //    {
        //        return "+scale";
        //    }

        //    public override MouseHandlingResult HandleClick()
        //    {
        //        Window_InGameDeveloperToolsMenu.scale *= 10;
        //        ArcenDebugging.ArcenDebugLog( "New scale:" + Window_InGameDeveloperToolsMenu.scale );
        //    }

        //    public override void OnUpdate()
        //    {
        //    }
        //}

        //public class bDecreaseScale : ButtonAbstractBase
        //{
        //    public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
        //    {
        //        return "-scale";
        //    }

        //    public override MouseHandlingResult HandleClick()
        //    {
        //        Window_InGameDeveloperToolsMenu.scale /= 10;
        //        ArcenDebugging.ArcenDebugLog( "New scale:" + Window_InGameDeveloperToolsMenu.scale );
        //    }

        //    public override void OnUpdate()
        //    {
        //    }
        //}
    }
}