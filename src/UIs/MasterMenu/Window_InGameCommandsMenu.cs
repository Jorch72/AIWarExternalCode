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
            if ( !Engine_AIW2.Instance.GetHasSelection() )
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
                Engine_AIW2.Instance.DoForSelected( delegate ( GameEntity selected )
                 {
                     if ( selected.EntitySpecificOrders.Behavior == EntityBehaviorType.Attacker )
                         foundOn = true;
                     else
                         foundOff = true;
                     return DelReturn.Continue;
                 } );
                Buffer.Add( "Free Roaming Defender: " );
                if ( foundOn && foundOff )
                    Buffer.Add( "Mixed" );
                else if ( foundOn )
                    Buffer.Add( "On" );
                else
                    Buffer.Add( "Off" );
            }
            public override void HandleClick() { Input_MainHandler.HandleInner( 0, "ToggleFRD" ); }
            public override bool GetShouldBeHidden()
            {
                if ( !Engine_AIW2.Instance.GetSelectionContains( EntityRollupType.MobileCombatants ) )
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
            public override void HandleClick()
            {
                Input_MainHandler.HandleInner( 0, "ScrapUnits" );
                Window_InGameCommandsMenu.Instance.CloseAllExpansions();
            }
            public override bool GetShouldBeHidden()
            {
                if ( !Engine_AIW2.Instance.GetSelectionContainsNon( SpecialEntityType.HumanKingUnit ) )
                    return true;
                return false;
            }
        }

        public class bToggleBuildMenu : WindowTogglingButtonController
        {
            public static bToggleBuildMenu Instance;
            public bToggleBuildMenu() : base( "Build", "^" ) { Instance = this; }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameBuildMenu.Instance; }
            public override bool GetShouldBeHidden()
            {
                if ( Window_InGameBuildMenu.GetEntityToUseForBuildMenu() == null )
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
                if ( !Engine_AIW2.Instance.GetSelectionContains( SpecialEntityType.HumanKingUnit ) )
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
                if ( !Engine_AIW2.Instance.GetSelectionContains( SpecialEntityType.HumanKingUnit ) )
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