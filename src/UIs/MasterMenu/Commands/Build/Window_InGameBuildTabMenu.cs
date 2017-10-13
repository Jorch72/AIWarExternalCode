using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameBuildTabMenu : ToggleableWindowController
    {
        public static Window_InGameBuildTabMenu Instance;
        public Window_InGameBuildTabMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        public int CurrentMenuIndex;
        public Int64 EntityID = -1;
        public GameEntityTypeData EntityData;
        public bool EntityChangedSinceLastButtonSetUpdate_Queue;
        public bool EntityChangedSinceLastButtonSetUpdate_QueueControls;
        public bool EntityChangedSinceLastButtonSetUpdate_Menu;
        public bool MenuIndexChangedSinceLastButtonSetUpdate;
        public DateTime LastEntityBuildQueueUpdateTimestamp;

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;

            GameEntity possibleEntity = ArcenExternalUIUtilities.GetEntityToUseForBuildMenu();

            if ( possibleEntity == null )
                return false;

            if ( possibleEntity.PrimaryKeyID != this.EntityID ||
                 possibleEntity.TypeData != this.EntityData )
            {
                this.EntityID = possibleEntity.PrimaryKeyID;
                this.EntityData = possibleEntity.TypeData;
                this.EntityChangedSinceLastButtonSetUpdate_Queue = true;
                this.EntityChangedSinceLastButtonSetUpdate_QueueControls = true;
                this.EntityChangedSinceLastButtonSetUpdate_Menu = true;
            }

            if ( LastEntityBuildQueueUpdateTimestamp < possibleEntity.TimeBuildQueueLastUpdated )
            {
                this.EntityChangedSinceLastButtonSetUpdate_Queue = true;
                this.EntityChangedSinceLastButtonSetUpdate_QueueControls = true;
                LastEntityBuildQueueUpdateTimestamp = possibleEntity.TimeBuildQueueLastUpdated;
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
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                Window_InGameBuildTabMenu windowController = (Window_InGameBuildTabMenu)Element.Window.Controller;
                if ( windowController != null ) { } //prevent compiler warning

                if ( windowController.EntityChangedSinceLastButtonSetUpdate_Menu )
                {
                    elementAsType.ClearButtons();

                    GameEntity entity = World_AIW2.Instance.GetEntityByID( windowController.EntityID );
                    if ( entity != null )
                    {
                        List<BuildMenu> menus = entity.TypeData.BuildMenus;
                        int x = 0;
                        for ( int i = 0; i < menus.Count; i++ )
                        {
                            BuildMenu item = menus[i];
                            if ( item.RequiresPresenceOf != null )
                            {
                                bool foundIt = false;
                                planet.Combat.DoForEntities( EntityRollupType.SpecialBuildMenuEnablers, delegate ( GameEntity enabler )
                                {
                                    if ( enabler.TypeData != item.RequiresPresenceOf )
                                        return DelReturn.Continue;
                                    foundIt = true;
                                    return DelReturn.Break;
                                } );
                                if ( !foundIt )
                                    continue;
                            }
                            bool foundPatternItem = false;
                            for ( int j = 0; j < item.Columns.Count; j++ )
                            {
                                List<BuildMenuItem> itemList = item.Columns[j];
                                for ( int k = 0; k < itemList.Count; k++ )
                                    if ( itemList[k].PatternController != null )
                                    {
                                        foundPatternItem = true;
                                        break;
                                    }
                            }
                            bItem newButtonController = new bItem( entity.TypeData, i, foundPatternItem );
                            Vector2 offset;
                            offset.x = x * elementAsType.ButtonWidth;
                            offset.y = 0;
                            Vector2 size;
                            size.x = elementAsType.ButtonWidth;
                            size.y = elementAsType.ButtonHeight;
                            elementAsType.AddButton( newButtonController, size, offset );
                            x++;
                        }
                    }

                    elementAsType.ActuallyPutItemsBackInPoolThatAreStillCleared();

                    windowController.EntityChangedSinceLastButtonSetUpdate_Menu = false;
                    windowController.MenuIndexChangedSinceLastButtonSetUpdate = true;
                }
            }
        }

        private class bItem : WindowTogglingButtonController
        {
            public GameEntityTypeData TypeDoingTheBuilding;
            public int MenuIndex;
            public bool IsPatternMenu;

            public bItem( GameEntityTypeData TypeDoingTheBuilding, int MenuIndex, bool IsPatternMenu )
                 : base( string.Empty, "^" )
            {
                this.TypeDoingTheBuilding = TypeDoingTheBuilding;
                this.MenuIndex = MenuIndex;
                this.IsPatternMenu = IsPatternMenu;
            }

            private BuildMenu GetMenu()
            {
                if ( this.MenuIndex < 0 || this.MenuIndex >= this.TypeDoingTheBuilding.BuildMenus.Count )
                    return null;
                return this.TypeDoingTheBuilding.BuildMenus[this.MenuIndex];
            }

            public override bool GetShouldSuppressOpenIndicatorEvenIfToggledWindowIsShown()
            {
                if ( MenuIndex != Window_InGameBuildTabMenu.Instance.CurrentMenuIndex )
                    return true;
                return false;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                BuildMenu menu = this.GetMenu();
                if ( menu == null )
                    Buffer.Add( "NULL" );
                else
                    Buffer.Add( menu.Abbreviation );
            }

            public override MouseHandlingResult HandleClick()
            {
                bool justSwitching = Instance.CurrentMenuIndex >= 0 && MenuIndex != Instance.CurrentMenuIndex;
                Instance.CurrentMenuIndex = this.MenuIndex;
                if ( this.IsPatternMenu )
                {
                    Window_InGameBuildTypeIconMenu.Instance.Close();
                }
                else
                {
                    if ( Window_InGameBuildTypeMenu.Instance.IsOpen )
                        Window_InGameBuildTypeMenu.Instance.Close();
                    if ( !Window_InGameBuildTypeIconMenu.Instance.IsOpen )
                    {
                        Window_InGameBuildTypeIconMenu.Instance.LastMenuIndex = -1;
                        Window_InGameBuildTypeIconMenu.Instance.LastTypeIndex = -1;
                    }
                    else
                    {
                        if ( justSwitching )
                            return MouseHandlingResult.None; // skip the HandleClick at the end
                    }
                }
                base.HandleClick();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }

            public override ToggleableWindowController GetRelatedController() { return this.IsPatternMenu ? (ToggleableWindowController)Window_InGameBuildTypeMenu.Instance : (ToggleableWindowController)Window_InGameBuildTypeIconMenu.Instance; }
        }
    }
}