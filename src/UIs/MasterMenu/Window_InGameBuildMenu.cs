using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameBuildMenu : ToggleableWindowController
    {
        public static Window_InGameBuildMenu Instance;
        public Window_InGameBuildMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        private static int CurrentMenuIndex;

        private Int64 EntityID = -1;
        private GameEntityTypeData EntityData;
        private bool EntityChangedSinceLastButtonSetUpdate_Queue;
        private bool EntityChangedSinceLastButtonSetUpdate_Menu;
        private int LastMenuIndex;
        private bool MenuIndexChangedSinceLastButtonSetUpdate;
        private DateTime LastEntityBuildQueueUpdateTimestamp;

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;

            GameEntity possibleEntity = null;
            Engine_AIW2.Instance.DoForSelected( delegate ( GameEntity selected )
             {
                 if ( selected.TypeData.BuildMenus == null || selected.TypeData.BuildMenus.Count <= 0 )
                 {
                     possibleEntity = null;
                     return DelReturn.Break;
                 }
                 if ( possibleEntity != null && possibleEntity.TypeData != selected.TypeData )
                 {
                     possibleEntity = null;
                     return DelReturn.Break;
                 }
                 if ( possibleEntity == null )
                     possibleEntity = selected;
                 return DelReturn.Continue;
             } );

            if ( possibleEntity == null )
                return false;

            if ( possibleEntity.PrimaryKeyID != this.EntityID ||
                 possibleEntity.TypeData != this.EntityData )
            {
                this.EntityID = possibleEntity.PrimaryKeyID;
                this.EntityData = possibleEntity.TypeData;
                this.EntityChangedSinceLastButtonSetUpdate_Queue = true;
                this.EntityChangedSinceLastButtonSetUpdate_Menu = true;
            }

            if( LastEntityBuildQueueUpdateTimestamp < possibleEntity.TimeBuildQueueLastUpdated)
            {
                this.EntityChangedSinceLastButtonSetUpdate_Queue = true;
                LastEntityBuildQueueUpdateTimestamp = possibleEntity.TimeBuildQueueLastUpdated;
            }

            if( LastMenuIndex != CurrentMenuIndex)
            {
                MenuIndexChangedSinceLastButtonSetUpdate = true;
                LastMenuIndex = CurrentMenuIndex;
            }

            return true;
        }

        public class bsMenuGrid : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                Window_InGameBuildMenu windowController = (Window_InGameBuildMenu)Element.Window.Controller;

                if ( windowController.MenuIndexChangedSinceLastButtonSetUpdate )
                {
                    elementAsType.ClearButtons();

                    GameEntity entity = World_AIW2.Instance.GetEntityByID( windowController.EntityID );
                    if ( entity != null && entity.TypeData.BuildMenus.Count > 0 )
                    {
                        if ( windowController.LastMenuIndex >= entity.TypeData.BuildMenus.Count )
                            windowController.LastMenuIndex = 0;
                        BuildMenu menu = entity.TypeData.BuildMenus[windowController.LastMenuIndex];
                        if ( menu != null )
                        {
                            int shownColumnCount = 0;
                            for ( int x = 0; x < menu.Columns.Count; x++ )
                            {
                                bool haveShownAnythingInThisColumn = false;
                                List<BuildMenuItem> column = menu.Columns[x];
                                for ( int y = 0; y < column.Count; y++ )
                                {
                                    BuildMenuItem item = column[y];
                                    if ( item.EntityDataOrNull != null && localSide.GetCanBuildAtAll( item.EntityDataOrNull ) != ArcenRejectionReason.Unknown )
                                        continue;
                                    haveShownAnythingInThisColumn = true;
                                    bBuildItem newButtonController = new bBuildItem( entity.TypeData, item );
                                    newButtonController.ItemMenuIndex = windowController.LastMenuIndex;
                                    newButtonController.ItemColumnIndex = x;
                                    newButtonController.ItemIndex = y;
                                    Vector2 offset;
                                    offset.x = shownColumnCount * elementAsType.ButtonWidth;
                                    offset.y = y * elementAsType.ButtonHeight;
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

                    elementAsType.ActuallyDestroyButtonsThatAreStillCleared();

                    windowController.MenuIndexChangedSinceLastButtonSetUpdate = false;
                }
            }
        }

        private class bBuildItem : ButtonAbstractBase
        {
            public GameEntityTypeData TypeDoingTheBuilding;
            public int ItemMenuIndex;
            public int ItemColumnIndex;
            public int ItemIndex;
            public BuildMenuItem Item;

            public bBuildItem( GameEntityTypeData TypeDoingTheBuilding, BuildMenuItem Item )
            {
                this.TypeDoingTheBuilding = TypeDoingTheBuilding;
                this.Item = Item;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                if ( this.Item == null )
                {
                    buffer.Add( "NULL" );
                    return;
                }
                if ( this.Item.EntityDataOrNull != null )
                {
                    buffer.Add( this.Item.EntityDataOrNull.Name );
                    if ( this.Item.EntityDataOrNull.BalanceStats.SquadsPerCap > 0 )
                    {
                        int currentValue;
                        WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                        if ( this.Item.EntityDataOrNull.CapIsPerPlanet )
                        {
                            Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                            CombatSide thisSide = planet.Combat.GetSideForWorldSide( localSide );
                            currentValue = thisSide.SquadCountsByType[this.Item.EntityDataOrNull];
                        }
                        else
                            currentValue = localSide.SquadCountsByType[this.Item.EntityDataOrNull];
                        buffer.Add( "\n" ).Add( "(" ).Add( currentValue ).Add( "/" ).Add( this.Item.EntityDataOrNull.BalanceStats.SquadsPerCap ).Add( ")" );
                        if ( this.Item.EntityDataOrNull.Balance_PowerCost.PowerMultiplier > 0 )
                            buffer.Add( "\n" ).Add( this.Item.EntityDataOrNull.BalanceStats.SquadPowerConsumption ).Add( " Power" );
                    }
                }
                else
                    buffer.Add( this.Item.PatternController.Name );
            }

            public override void HandleClick()
            {
                if ( this.Item.EntityDataOrNull != null )
                {
                    if ( this.Item.EntityDataOrNull.MetalFlows[MetalFlowPurpose.SelfConstruction] != null )
                    {
                        Engine_AIW2.Instance.PlacingEntityType = this.Item.EntityDataOrNull;
                    }
                    else
                    {
                        GameCommand command = GameCommand.Create( GameCommandType.AlterBuildQueue );
                        command.RelatedEntityType = this.Item.EntityDataOrNull;
                        command.RelatedMagnitude = 1;
                        if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Subtractive ) )
                            command.RelatedMagnitude = -command.RelatedMagnitude;

                        Engine_AIW2.Instance.DoForSelected( delegate ( GameEntity selected )
                         {
                             if ( selected.TypeData != this.TypeDoingTheBuilding )
                                 return DelReturn.Continue;
                             command.RelatedEntityIDs.Add( selected.PrimaryKeyID );
                             return DelReturn.Continue;
                         } );

                        if ( command.RelatedEntityIDs.Count > 0 )
                            World_AIW2.Instance.QueueGameCommand( command );
                    }
                }
                else
                {
                    GameCommand command = GameCommand.Create( GameCommandType.InvokeBuildPattern );
                    command.RelatedMagnitude = this.ItemMenuIndex;
                    command.RelatedPoint.X = this.ItemColumnIndex;
                    command.RelatedPoint.Y = this.ItemIndex;

                    Engine_AIW2.Instance.DoForSelected( delegate ( GameEntity selected )
                    {
                        if ( selected.TypeData != this.TypeDoingTheBuilding )
                            return DelReturn.Continue;
                        command.RelatedEntityIDs.Add( selected.PrimaryKeyID );
                        return DelReturn.Continue;
                    } );

                    if ( command.RelatedEntityIDs.Count > 0 )
                        World_AIW2.Instance.QueueGameCommand( command );
                }
            }

            public override void HandleMouseover()
            {
                if ( this.Item.EntityDataOrNull != null )
                    GameEntityTypeData.CurrentlyHoveredOver = this.Item.EntityDataOrNull;
            }

            public override void OnUpdate()
            {
            }
        }

        public class bsMenuSelectionRow : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                Window_InGameBuildMenu windowController = (Window_InGameBuildMenu)Element.Window.Controller;

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
                            bMenuSelectionItem newButtonController = new bMenuSelectionItem( entity.TypeData, i );
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

                    elementAsType.ActuallyDestroyButtonsThatAreStillCleared();

                    windowController.EntityChangedSinceLastButtonSetUpdate_Menu = false;
                    windowController.MenuIndexChangedSinceLastButtonSetUpdate = true;
                }
            }
        }

        private class bMenuSelectionItem : ButtonAbstractBase
        {
            public GameEntityTypeData TypeDoingTheBuilding;
            public int MenuIndex;

            public bMenuSelectionItem( GameEntityTypeData TypeDoingTheBuilding, int MenuIndex )
            {
                this.TypeDoingTheBuilding = TypeDoingTheBuilding;
                this.MenuIndex = MenuIndex;
            }

            private BuildMenu GetMenu()
            {
                if ( this.MenuIndex < 0 || this.MenuIndex >= this.TypeDoingTheBuilding.BuildMenus.Count )
                    return null;
                return this.TypeDoingTheBuilding.BuildMenus[this.MenuIndex];
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

            public override void HandleClick()
            {
                Window_InGameBuildMenu.CurrentMenuIndex = this.MenuIndex;
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }

        public class bsQueueItemRow : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                Window_InGameBuildMenu windowController = (Window_InGameBuildMenu)Element.Window.Controller;

                if ( windowController.EntityChangedSinceLastButtonSetUpdate_Queue )
                {
                    elementAsType.ClearButtons();

                    GameEntity entity = World_AIW2.Instance.GetEntityByID( windowController.EntityID );
                    if ( entity != null )
                    {
                        float runningX = 0;
                        {
                            bTogglePause newButtonController = new bTogglePause();
                            Vector2 offset;
                            offset.x = runningX;
                            offset.y = 0;
                            Vector2 size;
                            size.x = elementAsType.ButtonWidth / 2;
                            size.y = elementAsType.ButtonHeight;
                            elementAsType.AddButton( newButtonController, size, offset );
                            runningX += size.x;
                        }
                        {
                            bToggleLoop newButtonController = new bToggleLoop();
                            Vector2 offset;
                            offset.x = runningX;
                            offset.y = 0;
                            Vector2 size;
                            size.x = elementAsType.ButtonWidth / 2;
                            size.y = elementAsType.ButtonHeight;
                            elementAsType.AddButton( newButtonController, size, offset );
                            runningX += size.x;
                        }
                        if ( entity.BuildQueue != null )
                        {
                            List<BuildQueueItem> items = entity.BuildQueue.Items;
                            for ( int x = 0; x < items.Count; x++ )
                            {
                                BuildQueueItem item = items[x];
                                bQueueItem newButtonController = new bQueueItem( entity.TypeData, item, x );
                                Vector2 offset;
                                offset.x = runningX;
                                offset.y = 0;
                                Vector2 size;
                                size.x = elementAsType.ButtonWidth;
                                size.y = elementAsType.ButtonHeight;
                                elementAsType.AddButton( newButtonController, size, offset );
                                runningX += size.x;
                            }
                        }
                    }

                    elementAsType.ActuallyDestroyButtonsThatAreStillCleared();

                    windowController.EntityChangedSinceLastButtonSetUpdate_Queue = false;
                    windowController.MenuIndexChangedSinceLastButtonSetUpdate = true;
                }
            }
        }

        private class bQueueItem : ButtonAbstractBase
        {
            public GameEntityTypeData TypeDoingTheBuilding;
            public BuildQueueItem Item;
            public int ItemIndex;

            public bQueueItem( GameEntityTypeData TypeDoingTheBuilding, BuildQueueItem Item, int ItemIndex )
            {
                this.TypeDoingTheBuilding = TypeDoingTheBuilding;
                this.Item = Item;
                this.ItemIndex = ItemIndex;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );

                if ( this.Item == null || this.Item.TypeToBuild == null )
                {
                    buffer.Add( "NULL" );
                    return;
                }
                buffer.Add( this.Item.TypeToBuild.Name );
                buffer.Add( "\n" ).Add( "(" ).Add( this.Item.NumberBuiltThisLoop ).Add( "/" ).Add( this.Item.NumberToBuildEachLoop ).Add( ")" );
                FInt percent = ( this.Item.MetalSpentOnCurrentIteration * 100 ) / this.Item.TypeToBuild.BalanceStats.SquadMetalCost;
                if ( percent > FInt.Zero )
                    buffer.Add( "\n" ).Add( percent.IntValue ).Add( "%" );
            }

            public override void HandleClick()
            {
                GameCommand command = GameCommand.Create( GameCommandType.AlterBuildQueue );
                command.RelatedEntityType = this.Item.TypeToBuild;
                command.RelatedMagnitude = 1;
                if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Subtractive ) )
                    command.RelatedMagnitude = -command.RelatedMagnitude;

                Engine_AIW2.Instance.DoForSelected( delegate ( GameEntity selected )
                {
                    if ( selected.TypeData != this.TypeDoingTheBuilding )
                        return DelReturn.Continue;
                    command.RelatedEntityIDs.Add( selected.PrimaryKeyID );
                    return DelReturn.Continue;
                } );

                if ( command.RelatedEntityIDs.Count > 0 )
                    World_AIW2.Instance.QueueGameCommand( command );
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
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

            public override void HandleClick()
            {
                GameCommand command = GameCommand.Create( GameCommandType.SetQueueLoop );
                command.RelatedBool = !currentState;

                Engine_AIW2.Instance.DoForSelected( delegate ( GameEntity selected )
                {
                    command.RelatedEntityIDs.Add( selected.PrimaryKeyID );
                    return DelReturn.Continue;
                } );

                if ( command.RelatedEntityIDs.Count > 0 )
                    World_AIW2.Instance.QueueGameCommand( command );
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
                Window_InGameBuildMenu windowController = (Window_InGameBuildMenu)Element.Window.Controller;
                GameEntity entity = World_AIW2.Instance.GetEntityByID( windowController.EntityID );
                if ( entity != null && entity.BuildQueue != null && entity.BuildQueue.IsLooping )
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

            public override void HandleClick()
            {
                GameCommand command = GameCommand.Create( GameCommandType.SetHoldFireMode );
                command.RelatedBool = !currentState;

                Engine_AIW2.Instance.DoForSelected( delegate ( GameEntity selected )
                {
                    command.RelatedEntityIDs.Add( selected.PrimaryKeyID );
                    return DelReturn.Continue;
                } );

                if ( command.RelatedEntityIDs.Count > 0 )
                    World_AIW2.Instance.QueueGameCommand( command );
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
                Window_InGameBuildMenu windowController = (Window_InGameBuildMenu)Element.Window.Controller;
                GameEntity entity = World_AIW2.Instance.GetEntityByID( windowController.EntityID );
                if ( entity != null && entity.IsInHoldFireMode )
                    this.currentState = true;
                else
                    this.currentState = false;
            }
        }
    }
}