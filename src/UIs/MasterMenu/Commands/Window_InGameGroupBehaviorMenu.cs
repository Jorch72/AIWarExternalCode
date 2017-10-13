using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameGroupBehaviorMenu : ToggleableWindowController
    {
        public static Window_InGameGroupBehaviorMenu Instance;
        public Window_InGameGroupBehaviorMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;

            if ( World_AIW2.Instance.CurrentActiveSelectionControlGroupPrimaryKeyID <= 0 )
                return false;

            return true;
        }

        public class bToggleFormationMenu : WindowTogglingButtonController
        {
            public bToggleFormationMenu() : base( "Formation", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameFormationMenu.Instance; }
            public override bool GetShouldBeHidden()
            {
                return false;
            }
        }

        public class bToggleTargetSorterMenu : WindowTogglingButtonController
        {
            public bToggleTargetSorterMenu() : base( "Targeting", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameTargetSorterMenu.Instance; }
            public override bool GetShouldBeHidden()
            {
                return false;
            }
        }
    }
}