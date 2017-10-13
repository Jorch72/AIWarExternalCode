using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameBuildQueueControlsMenu : WindowControllerAbstractBase
    {
        public static Window_InGameBuildQueueControlsMenu Instance;
        public Window_InGameBuildQueueControlsMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;

            if ( !Window_InGameBuildTabMenu.Instance.GetShouldDrawThisFrame() )
                return false;

            return true;
        }
        
        public class bsItems : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;

                if ( Window_InGameBuildTabMenu.Instance.EntityChangedSinceLastButtonSetUpdate_QueueControls )
                {
                    elementAsType.ClearButtons();

                    GameEntity entity = World_AIW2.Instance.GetEntityByID( Window_InGameBuildTabMenu.Instance.EntityID );
                    if ( entity != null )
                    {
                        //float aspectRatioAdjustedButtonWidth = elementAsType.ButtonWidth;
                        //float aspectRatioAdjustedButtonHeight = elementAsType.ButtonHeight;
                        //if ( ArcenUI.Instance.PixelsPerPercent_X != ArcenUI.Instance.PixelsPerPercent_Y )
                        //    aspectRatioAdjustedButtonWidth *= ArcenUI.Instance.PixelsPerPercent_Y / ArcenUI.Instance.PixelsPerPercent_X;

                        float runningY = 0;
                        {
                            bTogglePause newButtonController = new bTogglePause();
                            Vector2 offset;
                            offset.x = 0;
                            offset.y = 0;
                            Vector2 size;
                            size.x = elementAsType.ButtonWidth;
                            size.y = elementAsType.ButtonHeight;
                            elementAsType.AddButton( newButtonController, size, offset );
                            runningY += size.y;
                        }
                        {
                            bToggleLoop newButtonController = new bToggleLoop();
                            Vector2 offset;
                            offset.x = 0;
                            offset.y = runningY;
                            Vector2 size;
                            size.x = elementAsType.ButtonWidth;
                            size.y = elementAsType.ButtonHeight;
                            elementAsType.AddButton( newButtonController, size, offset );
                            runningY += size.y;
                        }

                        elementAsType.ActuallyPutItemsBackInPoolThatAreStillCleared();

                        Window_InGameBuildTabMenu.Instance.EntityChangedSinceLastButtonSetUpdate_QueueControls = false;
                        Window_InGameBuildTabMenu.Instance.MenuIndexChangedSinceLastButtonSetUpdate = true;
                    }
                }
            }
        }

        private class bToggleLoop : ButtonAbstractBase
        {
            private bool currentState;
            public bToggleLoop()
            {
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( this.currentState ? "Looping" : "Not Looping" );
            }

            public override MouseHandlingResult HandleClick()
            {
                GameCommand command = GameCommand.Create( GameCommandType.SetQueueLoop );
                command.RelatedBool = !currentState;

                Engine_AIW2.Instance.DoForSelected( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, delegate( GameEntity selected )
                {
                    command.RelatedEntityIDs.Add( selected.PrimaryKeyID );
                    return DelReturn.Continue;
                } );

                if ( command.RelatedEntityIDs.Count > 0 )
                    World_AIW2.Instance.QueueGameCommand( command, true );
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
                Window_InGameBuildQueueControlsMenu windowController = (Window_InGameBuildQueueControlsMenu)Element.Window.Controller;
                if ( windowController != null ) { } //prevent compiler warning
                GameEntity entity = World_AIW2.Instance.GetEntityByID( Window_InGameBuildTabMenu.Instance.EntityID );
                if ( entity == null || entity.BuildQueue == null || entity.BuildQueue.IsLooping )
                    this.currentState = true;
                else
                    this.currentState = false;
            }
        }

        private class bTogglePause : ButtonAbstractBase
        {
            private bool currentState;
            public bTogglePause()
            {
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( this.currentState ? "Paused" : "Running" );
            }

            public override MouseHandlingResult HandleClick()
            {
                GameCommand command = GameCommand.Create( GameCommandType.SetHoldFireMode );
                command.RelatedBool = !currentState;

                Engine_AIW2.Instance.DoForSelected( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, delegate ( GameEntity selected )
                {
                    command.RelatedEntityIDs.Add( selected.PrimaryKeyID );
                    return DelReturn.Continue;
                } );

                if ( command.RelatedEntityIDs.Count > 0 )
                    World_AIW2.Instance.QueueGameCommand( command, true );
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
                Window_InGameBuildQueueControlsMenu windowController = (Window_InGameBuildQueueControlsMenu)Element.Window.Controller;
                if ( windowController != null ) { } //prevent compiler warning
                GameEntity entity = World_AIW2.Instance.GetEntityByID( Window_InGameBuildTabMenu.Instance.EntityID );
                if ( entity != null && entity.IsInHoldFireMode )
                    this.currentState = true;
                else
                    this.currentState = false;
            }
        }
    }
}