using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public class Window_InGameCommandsMenu : WindowControllerAbstractBase
    {
        public static Window_InGameCommandsMenu Instance;
        public Window_InGameCommandsMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( !Engine_AIW2.Instance.GetHasSelection( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy ) )
                return false;
            return true;
        }

        public class bToggleFRD : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                bool foundOn = false;
                bool foundOff = false;
                Engine_AIW2.Instance.DoForSelected( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, delegate( GameEntity selected )
                 {
                     if ( selected.EntitySpecificOrders.Behavior == EntityBehaviorType.Attacker )
                         foundOn = true;
                     else
                         foundOff = true;
                     return DelReturn.Continue;
                 } );
                Buffer.Add( "Pursue: " );
                if ( foundOn && foundOff )
                    Buffer.Add( "Mixed" );
                else if ( foundOn )
                    Buffer.Add( "On" );
                else
                    Buffer.Add( "Off" );
            }
            public override MouseHandlingResult HandleClick() { Input_MainHandler.HandleInner( 0, "ToggleFRD" );
                return MouseHandlingResult.None;
            }
            public override bool GetShouldBeHidden()
            {
                if ( !Engine_AIW2.Instance.GetSelectionContains( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, EntityRollupType.MobileCombatants ) )
                    return true;
                return false;
            }
        }

        public class bScrap : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Scrap" );
            }
            public override MouseHandlingResult HandleClick()
            {
                Input_MainHandler.HandleInner( 0, "ScrapUnits" );
                Window_InGameCommandsMenu.Instance.CloseAllExpansions();
                return MouseHandlingResult.None;
            }
            public override bool GetShouldBeHidden()
            {
                if ( !Engine_AIW2.Instance.GetSelectionContainsNon( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, SpecialEntityType.HumanKingUnit ) )
                    return true;
                return false;
            }
        }

        public class bToggleBuildMenu : WindowTogglingButtonController
        {
            public static bToggleBuildMenu Instance;
            public bToggleBuildMenu() : base( "Build", "^" ) { Instance = this; }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameBuildTabMenu.Instance; }
            public override bool GetShouldBeHidden()
            {
                if ( ArcenExternalUIUtilities.GetEntityToUseForBuildMenu() == null )
                    return true;
                return false;
            }
        }

        public class bToggleHackingMenu : WindowTogglingButtonController
        {
            public bToggleHackingMenu() : base( "Hacking", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameHackingMenu.Instance; }
            public override bool GetShouldBeHidden()
            {
                if ( !Engine_AIW2.Instance.GetSelectionContains( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, SpecialEntityType.HumanKingUnit ) )
                    return true;
                return false;
            }
        }

        public class bToggleWarheadMenu : WindowTogglingButtonController
        {
            public bToggleWarheadMenu() : base( "Warhead", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameWarheadMenu.Instance; }
            public override bool GetShouldBeHidden()
            {
                if ( !Engine_AIW2.Instance.GetSelectionContains( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, SpecialEntityType.HumanKingUnit ) )
                    return true;
                return false;
            }
        }

        public class bToggleRallyMenu : WindowTogglingButtonController
        {
            public bToggleRallyMenu() : base( "Rally", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameRallyMenu.Instance; }
            public override bool GetShouldBeHidden()
            {
                if ( ArcenExternalUIUtilities.GetEntityToUseForBuildMenu() == null )
                    return true;
                return false;
            }
        }

        public class bToggleControlGroupMenu : WindowTogglingButtonController
        {
            public bToggleControlGroupMenu() : base( "Control Group", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameAssignControlGroupMenu.Instance; }
            public override bool GetShouldBeHidden()
            {
                return false;
            }
        }

        public class bToggleGroupBehaviorMenu : WindowTogglingButtonController
        {
            public bToggleGroupBehaviorMenu() : base( "Group Behavior", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameGroupBehaviorMenu.Instance; }
            public override bool GetShouldBeHidden()
            {
                if ( World_AIW2.Instance.CurrentActiveSelectionControlGroupPrimaryKeyID <= 0 )
                    return true;
                return false;
            }
        }

        public void CloseAllExpansions()
        {
            this.CloseWindowsOtherThanThisOne( null );
        }
    }
}