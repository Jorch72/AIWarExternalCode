using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameAssignControlGroupMenu : ToggleableWindowController
    {
        public static Window_InGameAssignControlGroupMenu Instance;
        public Window_InGameAssignControlGroupMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        private bool NeedRefresh;
        private DateTime TimeOfLastRefresh = DateTime.Now;

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;

            return true;
        }

        public override void OnOpen()
        {
            this.NeedRefresh = true;
        }

        public class bsItems : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                Window_InGameAssignControlGroupMenu windowController = (Window_InGameAssignControlGroupMenu)Element.Window.Controller;
                if ( windowController != null ) { } //prevent compiler warning

                if(windowController.TimeOfLastRefresh < Engine_AIW2.Instance.TimeOfLastControlGroupChange)
                    windowController.NeedRefresh = true;

                if ( windowController.NeedRefresh )
                {
                    windowController.NeedRefresh = false;
                    windowController.TimeOfLastRefresh = DateTime.Now;

                    elementAsType.ClearButtons();

                    int x = 0;
                    for ( int i = 0; i < localSide.ControlGroups.Count;i++)
                    {
                        ControlGroup controlGroup = localSide.ControlGroups[i];
                        if ( controlGroup.EntityIDs.Count <= 0 )
                            continue;
                        bItem newButtonController = new bItem( controlGroup );
                        Vector2 offset;
                        offset.x = x * elementAsType.ButtonWidth;
                        offset.y = 0;
                        Vector2 size;
                        size.x = elementAsType.ButtonWidth;
                        size.y = elementAsType.ButtonHeight;
                        elementAsType.AddButton( newButtonController, size, offset );
                        x++;
                    }

                    if(x <= 8)
                    {
                        bNew newButtonController = new bNew();
                        Vector2 offset;
                        offset.x = x * elementAsType.ButtonWidth;
                        offset.y = 0;
                        Vector2 size;
                        size.x = elementAsType.ButtonWidth;
                        size.y = elementAsType.ButtonHeight;
                        elementAsType.AddButton( newButtonController, size, offset );
                        x++;
                    }

                    {
                        bClear newButtonController = new bClear();
                        Vector2 offset;
                        offset.x = 9 * elementAsType.ButtonWidth;
                        offset.y = 0;
                        Vector2 size;
                        size.x = elementAsType.ButtonWidth;
                        size.y = elementAsType.ButtonHeight;
                        elementAsType.AddButton( newButtonController, size, offset );
                    }

                    elementAsType.ActuallyPutItemsBackInPoolThatAreStillCleared();
                }
            }
        }

        private class bItem : ButtonAbstractBase
        {
            private ControlGroup Item;

            public bItem( ControlGroup Item )
            {
                this.Item = Item;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );

                Engine_AIW2.Instance.DoForSelected( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, delegate ( GameEntity selected )
                {
                    if ( selected.EntitySpecificOrders.ControlGroup == null || selected.EntitySpecificOrders.ControlGroup.PrimaryKeyID != this.Item.PrimaryKeyID )
                        return DelReturn.Continue;
                    buffer.Add( "*" );
                    return DelReturn.Break;
                } );

                buffer.Add( "(" ).Add( this.Item.Name ).Add( ") " );
            }

            public override MouseHandlingResult HandleClick()
            {
                GameCommand command = GameCommand.Create( GameCommandType.AddToControlGroupPopulation );
                command.RelatedControlGroup = this.Item;
                Engine_AIW2.Instance.DoForSelected( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, delegate( GameEntity selected )
                 {
                     command.RelatedEntityIDs.Add( selected.PrimaryKeyID );
                     return DelReturn.Continue;
                 } );
                if ( command.RelatedEntityIDs.Count > 0 )
                    World_AIW2.Instance.QueueGameCommand( command, true );
                World_AIW2.Instance.CurrentActiveSelectionControlGroupPrimaryKeyID = this.Item.PrimaryKeyID;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        private class bNew : ButtonAbstractBase
        {
            public bNew() { }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                buffer.Add( "Assign To New" );
            }

            public override MouseHandlingResult HandleClick()
            {
                GameCommand command = GameCommand.Create( GameCommandType.CreateNewControlGroup );
                command.RelatedSide = World_AIW2.Instance.GetLocalPlayerSide();
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
            public override void OnUpdate() { }
        }

        private class bClear : ButtonAbstractBase
        {
            public bClear() { }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                buffer.Add( "None" );
            }

            public override MouseHandlingResult HandleClick()
            {
                GameCommand command = GameCommand.Create( GameCommandType.RemoveFromControlGroupPopulation );
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
            public override void OnUpdate() { }
        }
    }
}