using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameFormationMenu : ToggleableWindowController
    {
        public static Window_InGameFormationMenu Instance;
        public Window_InGameFormationMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        private bool NeedRefresh;
        private DateTime TimeOfLastRefresh = DateTime.Now;
        private Int64 LastControlGroupID;

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;

            if ( World_AIW2.Instance.CurrentActiveSelectionControlGroupPrimaryKeyID <= 0 )
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
                Window_InGameFormationMenu windowController = (Window_InGameFormationMenu)Element.Window.Controller;

                if ( windowController.TimeOfLastRefresh < Engine_AIW2.Instance.TimeOfLastControlGroupChange )
                    windowController.NeedRefresh = true;

                if ( windowController.LastControlGroupID != World_AIW2.Instance.CurrentActiveSelectionControlGroupPrimaryKeyID )
                    windowController.NeedRefresh = true;

                if ( windowController.NeedRefresh )
                {
                    windowController.NeedRefresh = false;
                    windowController.TimeOfLastRefresh = DateTime.Now;
                    windowController.LastControlGroupID = World_AIW2.Instance.CurrentActiveSelectionControlGroupPrimaryKeyID;

                    elementAsType.ClearButtons();

                    int x = 0;

                    {
                        bClear newButtonController = new bClear();
                        Vector2 offset;
                        offset.x = x * elementAsType.ButtonWidth;
                        offset.y = 0;
                        Vector2 size;
                        size.x = elementAsType.ButtonWidth;
                        size.y = elementAsType.ButtonHeight;
                        elementAsType.AddButton( newButtonController, size, offset );
                        x++;
                    }

                    for ( int i = 0; i < FormationTypeDataTable.Instance.Rows.Count; i++ )
                    {
                        FormationTypeData item = FormationTypeDataTable.Instance.Rows[i];
                        bItem newButtonController = new bItem( item );
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
                }
            }
        }

        private class bItem : ButtonAbstractBase
        {
            private FormationTypeData Item;

            public bItem( FormationTypeData Item )
            {
                this.Item = Item;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );

                if ( World_AIW2.Instance.CurrentActiveSelectionControlGroupPrimaryKeyID > 0 )
                {
                    ControlGroup group = World_AIW2.Instance.GetControlGroupByID( World_AIW2.Instance.CurrentActiveSelectionControlGroupPrimaryKeyID );
                    if ( group != null && group.Formation == this.Item )
                        buffer.Add( "*" );
                }

                buffer.Add( this.Item.Name );
            }

            public override MouseHandlingResult HandleClick()
            {
                ControlGroup group = World_AIW2.Instance.GetControlGroupByID( World_AIW2.Instance.CurrentActiveSelectionControlGroupPrimaryKeyID );
                if ( group == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                GameCommand command = GameCommand.Create( GameCommandType.SetGroupFormation );
                command.RelatedControlGroup = group;
                command.RelatedString = this.Item.InternalName;
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

                ControlGroup group = World_AIW2.Instance.GetControlGroupByID( World_AIW2.Instance.CurrentActiveSelectionControlGroupPrimaryKeyID );
                if ( group != null && group.Formation == null )
                    buffer.Add( "*" );

                buffer.Add( "None" );
            }

            public override MouseHandlingResult HandleClick()
            {
                ControlGroup group = World_AIW2.Instance.GetControlGroupByID( World_AIW2.Instance.CurrentActiveSelectionControlGroupPrimaryKeyID );
                if ( group == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                GameCommand command = GameCommand.Create( GameCommandType.SetGroupFormation );
                command.RelatedControlGroup = group;
                command.RelatedString = string.Empty;
                World_AIW2.Instance.QueueGameCommand( command, true );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }
    }
}