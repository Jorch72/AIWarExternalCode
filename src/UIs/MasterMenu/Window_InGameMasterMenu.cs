using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameMasterMenu : WindowControllerAbstractBase
    {
        public static Window_InGameMasterMenu Instance;
        public Window_InGameMasterMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        public class bToggleEscapeMenu : WindowTogglingButtonController
        {
            public bToggleEscapeMenu() : base( "Menu", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameEscapeMenu.Instance; }
        }

        public class bToggleDebugMenu : WindowTogglingButtonController
        {
            public bToggleDebugMenu() : base( "Debug", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameDeveloperToolsMenu.Instance; }
        }

        public class bToggleTimingMenu : WindowTogglingButtonController
        {
            public bToggleTimingMenu() : base( "Timing", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameTimingMenu.Instance; }
        }

        public class bToggleTechMenu : WindowTogglingButtonController
        {
            public bToggleTechMenu() : base( "Tech", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameTechMenu.Instance; }
        }

        public class bToggleCommandsMenu : WindowTogglingButtonController
        {
            public bToggleCommandsMenu() : base( "Commands", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameCommandsMenu.Instance; }
        }

        public class bTogglePlanetMenu : WindowTogglingButtonController
        {
            public bTogglePlanetMenu() : base( "Planet", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGamePlanetActionMenu.Instance; }
        }

        public class bToggleObjectivesMenu : WindowTogglingButtonController
        {
            public bToggleObjectivesMenu() : base( "Objectives", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameObjectivesWindow.Instance; }
        }

        public class bToggleControlGroupsMenu : WindowTogglingButtonController
        {
            public bToggleControlGroupsMenu() : base( "Control Groups", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameControlGroupsMenu.Instance; }
        }

        public void CloseAllExpansions()
        {
            this.CloseWindowsOtherThanThisOne( null );
        }
    }
}