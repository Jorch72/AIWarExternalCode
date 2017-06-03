using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGamePlanetActionMenu : ToggleableWindowController
    {
        public static Window_InGamePlanetActionMenu Instance;
        public Window_InGamePlanetActionMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        private int PlanetIndex = -1;
        private bool PlanetChangedSinceLastButtonSetUpdate;

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;

            Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();

            if ( planet == null )
            {
                this.PlanetIndex = -1;
                return false;
            }

            if ( planet.PlanetIndex != this.PlanetIndex )
            {
                this.PlanetIndex = planet.PlanetIndex;
                this.PlanetChangedSinceLastButtonSetUpdate = true;
            }

            return true;
        }

        public class bsItems : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                Window_InGamePlanetActionMenu windowController = (Window_InGamePlanetActionMenu)Element.Window.Controller;

                if ( windowController.PlanetChangedSinceLastButtonSetUpdate )
                {
                    elementAsType.ClearButtons();

                    Planet planet = World_AIW2.Instance.GetPlanetByIndex( windowController.PlanetIndex );
                    if ( planet != null )
                    {
                        int x = 0;
                        for ( CombatSideBooleanFlag flag = CombatSideBooleanFlag.None + 1; flag < CombatSideBooleanFlag.Length; flag++ )
                        {
                            bCombatSideBooleanFlagToggle newButtonController = new bCombatSideBooleanFlagToggle( flag );
                            Vector2 offset;
                            offset.x = 0;
                            offset.y = x * elementAsType.ButtonHeight;
                            Vector2 size;
                            size.x = elementAsType.ButtonWidth;
                            size.y = elementAsType.ButtonHeight;
                            elementAsType.AddButton( newButtonController, size, offset );
                            x++;
                        }
                    }

                    elementAsType.ActuallyDestroyButtonsThatAreStillCleared();

                    windowController.PlanetChangedSinceLastButtonSetUpdate = false;
                }
            }
        }

        private class bCombatSideBooleanFlagToggle : ButtonAbstractBase
        {
            private CombatSideBooleanFlag Flag;

            public bCombatSideBooleanFlagToggle( CombatSideBooleanFlag Flag )
            {
                this.Flag = Flag;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet == null )
                    return;
                CombatSide side = planet.Combat.GetSideForWorldSide( World_AIW2.Instance.GetLocalSide() );
                buffer.Add( Flag.ToString() ).Add( ": " );
                if ( side.BooleanFlags[Flag] )
                    buffer.Add( "On" );
                else
                    buffer.Add( "Off" );
            }
            public override void HandleClick() { Input_MainHandler.HandleInner( (int)Flag, "ToggleCombatSideBooleanFlag" ); }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }
    }
}