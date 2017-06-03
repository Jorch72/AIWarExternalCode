using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public class Window_InGameStandardGroupsMenu : ToggleableWindowController
    {
        public static Window_InGameStandardGroupsMenu Instance;
        public Window_InGameStandardGroupsMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        public class bSelectAllMobileMilitary : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Select All Mobile Military" );
            }
            public override void HandleClick()
            {
                Input_MainHandler.HandleInner( 0, "SelectAllMobileMilitary" );
                Window_InGameMasterMenu.Instance.CloseAllExpansions();
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bSelectController : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Select Controllers" );
            }
            public override void HandleClick()
            {
                Input_MainHandler.HandleInner( 0, "SelectController" );
                Window_InGameMasterMenu.Instance.CloseAllExpansions();
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bSelectSpaceDock : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Select Space Docks" );
            }
            public override void HandleClick()
            {
                Input_MainHandler.HandleInner( 0, "SelectSpaceDock" );
                Window_InGameMasterMenu.Instance.CloseAllExpansions();
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }
    }
}