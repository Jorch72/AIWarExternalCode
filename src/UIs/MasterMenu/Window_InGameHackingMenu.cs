using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameHackingMenu : ToggleableWindowController
    {
        public static Window_InGameHackingMenu Instance;
        public Window_InGameHackingMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        private int PlanetIndex = -1;
        private bool PlanetChangedSinceLastButtonSetUpdate;
        private HackingType LastObservedActiveHack;

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
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet == null )
                    return;
                GameEntity hacker = localSide.Entities.GetFirstMatching( EntityRollupType.KingUnits );
                if ( hacker == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                Window_InGameHackingMenu windowController = (Window_InGameHackingMenu)Element.Window.Controller;

                if ( hacker.ActiveHack != Instance.LastObservedActiveHack )
                {
                    Instance.LastObservedActiveHack = hacker.ActiveHack;
                    windowController.PlanetChangedSinceLastButtonSetUpdate = true;
                }

                if ( windowController.PlanetChangedSinceLastButtonSetUpdate )
                {
                    elementAsType.ClearButtons();

                    int x = 0;
                    for ( int i = 0; i < HackingTypeTable.Instance.Rows.Count;i++)
                    {
                        HackingType type = HackingTypeTable.Instance.Rows[i];
                        planet.Combat.DoForEntities( EntityRollupType.Hackable, delegate ( GameEntity entity )
                         {
                             if ( !entity.TypeData.EligibleForHacks.Contains( type ) )
                                 return DelReturn.Continue;
                             if ( !type.Implementation.GetCanBeHacked( entity, hacker ) )
                                 return DelReturn.Continue;
                             bItem newButtonController = new bItem( type, entity );
                             Vector2 offset;
                             offset.x = 0;
                             offset.y = x * elementAsType.ButtonHeight;
                             Vector2 size;
                             size.x = elementAsType.ButtonWidth;
                             size.y = elementAsType.ButtonHeight;
                             elementAsType.AddButton( newButtonController, size, offset );
                             x++;
                             return DelReturn.Continue;
                         } );
                    }

                    elementAsType.ActuallyDestroyButtonsThatAreStillCleared();

                    windowController.PlanetChangedSinceLastButtonSetUpdate = false;
                }
            }
        }

        private class bItem : ButtonAbstractBase
        {
            private HackingType Type;
            private GameEntity Target;

            public bItem( HackingType Type, GameEntity Target )
            {
                this.Type = Type;
                this.Target = Target;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                buffer.Add( this.Type.InternalName );
                buffer.Add( " (" );
                buffer.Add( this.Target.TypeData.GetDisplayName() );
                buffer.Add( ")" );
            }

            public override void HandleClick()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                GameEntity hacker = localSide.Entities.GetFirstMatching( EntityRollupType.KingUnits );
                if ( hacker == null )
                    return;
                if ( hacker.Combat != this.Target.Combat )
                    return;
                if ( !this.Type.Implementation.GetCanBeHacked( this.Target, hacker ) )
                    return;
                if ( localSide.StoredHacking < this.Type.Implementation.GetCostToHack( this.Target, hacker ) )
                    return;
                GameCommand command = GameCommand.Create( GameCommandType.SetActiveHack );
                command.RelatedHack = this.Type;
                command.RelatedEntityIDs.Add( hacker.PrimaryKeyID );
                command.TargetEntityIDs.Add( this.Target.PrimaryKeyID );
                if ( command.RelatedEntityIDs.Count > 0 )
                    World_AIW2.Instance.QueueGameCommand( command );
            }

            public override void HandleMouseover()
            {
                GameEntity.CurrentlyHoveredOver = this.Target;
            }

            public override void OnUpdate() { }
        }
    }
}