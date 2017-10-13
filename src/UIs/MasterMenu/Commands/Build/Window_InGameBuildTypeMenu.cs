using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameBuildTypeMenu : ToggleableWindowController
    {
        public static Window_InGameBuildTypeMenu Instance;
        public Window_InGameBuildTypeMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        public int LastMenuIndex = -1;

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;

            if ( !Window_InGameBuildTabMenu.Instance.GetShouldDrawThisFrame() )
                return false;

            return true;
        }

        public override void OnShowingRefused()
        {
            this.LastMenuIndex = -1;
        }

        public class bsItems : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                Window_InGameBuildTypeMenu windowController = (Window_InGameBuildTypeMenu)Element.Window.Controller;
                if ( windowController != null ) { } //prevent compiler warning

                if ( Window_InGameBuildTypeMenu.Instance.LastMenuIndex != Window_InGameBuildTabMenu.Instance.CurrentMenuIndex )
                {
                    Window_InGameBuildTypeMenu.Instance.LastMenuIndex = Window_InGameBuildTabMenu.Instance.CurrentMenuIndex;
                    elementAsType.ClearButtons();

                    GameEntity entity = World_AIW2.Instance.GetEntityByID( Window_InGameBuildTabMenu.Instance.EntityID );
                    if ( entity != null && entity.TypeData.BuildMenus.Count > 0 )
                    {
                        if ( Window_InGameBuildTypeMenu.Instance.LastMenuIndex >= entity.TypeData.BuildMenus.Count )
                        {
                            Window_InGameBuildTypeMenu.Instance.LastMenuIndex = 0;
                            Window_InGameBuildTabMenu.Instance.CurrentMenuIndex = 0;
                        }
                        BuildMenu menu = entity.TypeData.BuildMenus[Window_InGameBuildTypeMenu.Instance.LastMenuIndex];
                        if ( menu != null )
                        {
                            int shownColumnCount = 0;
                            for ( int x = 0; x < menu.Columns.Count; x++ )
                            {
                                bool haveShownAnythingInThisColumn = false;
                                List<BuildMenuItem> column = menu.Columns[x];
                                if ( column.Count <= 0 )
                                    continue;
                                for ( int y = 0; y < column.Count; y++ )
                                {
                                    BuildMenuItem item = column[y];
                                    haveShownAnythingInThisColumn = true;
                                    bItem newButtonController = new bItem( entity.TypeData, item );
                                    newButtonController.ItemMenuIndex = Window_InGameBuildTypeMenu.Instance.LastMenuIndex;
                                    newButtonController.ItemTypeIndex = x;
                                    newButtonController.ItemIndex = y;
                                    Vector2 offset;
                                    offset.x = shownColumnCount * elementAsType.ButtonWidth;
                                    offset.y = 0;
                                    Vector2 size;
                                    size.x = elementAsType.ButtonWidth;
                                    size.y = elementAsType.ButtonHeight;
                                    elementAsType.AddButton( newButtonController, size, offset );
                                }
                                if ( haveShownAnythingInThisColumn )
                                    shownColumnCount++;
                            }
                        }
                    }

                    elementAsType.ActuallyPutItemsBackInPoolThatAreStillCleared();
                }
            }
        }

        private class bItem : ButtonAbstractBase
        {
            public GameEntityTypeData TypeDoingTheBuilding;
            public int ItemMenuIndex;
            public int ItemTypeIndex;
            public int ItemIndex;
            public BuildMenuItem Item;

            public bItem( GameEntityTypeData TypeDoingTheBuilding, BuildMenuItem Item )
            {
                this.TypeDoingTheBuilding = TypeDoingTheBuilding;
                this.Item = Item;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                if ( this.Item == null || this.Item.PatternController == null )
                {
                    buffer.Add( "NULL" );
                    return;
                }
                else
                    buffer.Add( this.Item.PatternController.Name );
            }

            public override MouseHandlingResult HandleClick()
            {
                GameCommand command = GameCommand.Create( GameCommandType.InvokeBuildPattern );
                command.RelatedMagnitude = this.ItemMenuIndex;
                command.RelatedPoint.X = this.ItemTypeIndex;
                command.RelatedPoint.Y = this.ItemIndex;

                Engine_AIW2.Instance.DoForSelected( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, delegate( GameEntity selected )
                {
                    if ( selected.TypeData != this.TypeDoingTheBuilding )
                        return DelReturn.Continue;
                    command.RelatedEntityIDs.Add( selected.PrimaryKeyID );
                    return DelReturn.Continue;
                } );

                if ( command.RelatedEntityIDs.Count > 0 )
                    World_AIW2.Instance.QueueGameCommand( command, true );
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
            }

            public override void OnUpdate()
            {
            }
        }
    }
}