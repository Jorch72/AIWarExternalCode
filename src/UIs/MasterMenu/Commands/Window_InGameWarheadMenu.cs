using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameWarheadMenu : ToggleableWindowController
    {
        public static Window_InGameWarheadMenu Instance;
        public Window_InGameWarheadMenu()
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
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return;
                GameEntity launcher = localSide.Entities.GetFirstMatching( EntityRollupType.KingUnits );
                if ( launcher == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                Window_InGameWarheadMenu windowController = (Window_InGameWarheadMenu)Element.Window.Controller;
                if ( windowController != null ) { } //prevent compiler warning

                if ( windowController.PlanetChangedSinceLastButtonSetUpdate )
                {
                    elementAsType.ClearButtons();

                    int x = 0;
                    for ( int i = 0; i < GameEntityTypeDataTable.Instance.Rows.Count;i++)
                    {
                        GameEntityTypeData type = GameEntityTypeDataTable.Instance.Rows[i];
                        if ( type.SpecialType != SpecialEntityType.Warhead )
                            continue;
                        bool foundIt = false;
                        for(int j = 0; j < launcher.WarheadContents.Count;j++)
                        {
                            if ( launcher.WarheadContents[j].ContainedType != type )
                                continue;
                            foundIt = true;
                            break;
                        }
                        if ( !foundIt )
                            continue;
                        bItem newButtonController = new bItem( type );
                        Vector2 offset;
                        offset.x = x * elementAsType.ButtonWidth;
                        offset.y = 0;
                        Vector2 size;
                        size.x = elementAsType.ButtonWidth;
                        size.y = elementAsType.ButtonHeight;
                        elementAsType.AddButton( newButtonController, size, offset );
                        x++;
                    }

                    elementAsType.ActuallyPutItemsBackInPoolThatAreStillCleared();

                    windowController.PlanetChangedSinceLastButtonSetUpdate = false;
                }
            }
        }

        private class bItem : ButtonAbstractBase
        {
            private GameEntityTypeData Type;

            public bItem( GameEntityTypeData Type )
            {
                this.Type = Type;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                buffer.Add( this.Type.InternalName );
            }

            public override MouseHandlingResult HandleClick()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                GameEntity launcher = localSide.Entities.GetFirstMatching( EntityRollupType.KingUnits );
                if ( launcher == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet == null || planet != launcher.Combat.Planet )
                    return MouseHandlingResult.PlayClickDeniedSound;
                GameCommand command = GameCommand.Create( GameCommandType.LaunchWarhead );
                command.RelatedEntityType = this.Type;
                command.RelatedEntityIDs.Add( launcher.PrimaryKeyID );
                if ( command.RelatedEntityIDs.Count > 0 )
                    World_AIW2.Instance.QueueGameCommand( command, true );
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }
    }
}