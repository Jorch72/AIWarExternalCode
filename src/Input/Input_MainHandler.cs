using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public class Input_MainHandler : IInputActionHandler
    {
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            HandleInner( Int1, InputActionType.InternalName );
        }

        public static void HandleInner( Int32 Int1, string InputActionInternalName )
        {
            if ( ArcenUI.CurrentlyShownWindowsWith_PreventsNormalInputHandlers.Count > 0 )
                return;
            if ( ArcenUI.Instance.ShowingConsole )
                return;
            switch ( InputActionInternalName )
            {
                #region Development Tools
                case "DebugGenerateMap":
                    if ( World.Instance.IsLoaded )
                        return;
                    ArcenSocket.Instance.Shutdown();
                    Engine_AIW2.Instance.InnerDoStartNewWorldOKLogic();
                    break;
                case "DebugSendNextWave":
                    {
                        if ( !World.Instance.IsLoaded )
                            return;
                        GameCommand command = GameCommand.Create( GameCommandType.Debug_SendNextWave );
                        World_AIW2.Instance.QueueGameCommand( command, true );
                    }
                    break;
                case "DebugIncreaseAIP":
                    {
                        if ( !World.Instance.IsLoaded )
                            return;
                        GameCommand command = GameCommand.Create( GameCommandType.Debug_IncreaseAIP );
                        World_AIW2.Instance.QueueGameCommand( command, true );
                    }
                    break;
                case "DebugGiveSomeMetal":
                    {
                        if ( !World.Instance.IsLoaded )
                            return;
                        GameCommand command = GameCommand.Create( GameCommandType.Debug_GiveMetal );
                        World_AIW2.Instance.QueueGameCommand( command, true );
                    }
                    break;
                case "DebugGiveScience":
                    {
                        if ( !World.Instance.IsLoaded )
                            return;
                        GameCommand command = GameCommand.Create( GameCommandType.Debug_GiveScience );
                        World_AIW2.Instance.QueueGameCommand( command, true );
                    }
                    break;
                case "DebugConnectToLocalServer":
                    ArcenSocket.Instance.OpenAsClient( Window_MainMenu.Instance.TargetIP, (ushort)GameSettings.Current.GetInt( ArcenIntSetting_Universal.NetworkPort ) );
                    break;
                case "ReloadExternalDefinitions":
                    ArcenUI.Instance.Initialize( true );
                    //Engine_Universal.OnReloadAllExternalDefinitions();
                    break;
                case "ReloadExternalConstantsAndLanguageOnly":
                    Engine_Universal.OnReloadExternalConstantsAndLanguageOnly();
                    break;                    
                #endregion
                case "ToggleGalaxyMap":
                    if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                        return;
                    switch ( Engine_AIW2.Instance.CurrentGameViewMode )
                    {
                        case GameViewMode.MainGameView:
                            Engine_AIW2.Instance.PresentationLayer.ReactToLeavingPlanetView( Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() );
                            Engine_AIW2.Instance.SetCurrentGameViewMode( GameViewMode.GalaxyMapView );
                            break;
                        case GameViewMode.GalaxyMapView:
                            Engine_AIW2.Instance.SetCurrentGameViewMode( GameViewMode.MainGameView );
                            Engine_AIW2.Instance.PresentationLayer.ReactToEnteringPlanetView( Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() );
                            break;
                    }
                    break;
                case "TogglePause":
                    {
                        if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                            return;
                        GameCommand command = GameCommand.Create( GameCommandType.TogglePause );
                        World_AIW2.Instance.QueueGameCommand( command, true );
                    }
                    break;
                case "ScoutAll":
                    {
                        if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                            return;
                        GameCommand command = GameCommand.Create( GameCommandType.Debug_RevealAll );
                        World_AIW2.Instance.QueueGameCommand( command, true );
                    }
                    break;
                case "ScrapUnits":
                    {
                        if ( !World.Instance.IsLoaded )
                            return;
                        GameCommand command = GameCommand.Create( GameCommandType.ScrapUnits );
                        Engine_AIW2.Instance.DoForSelected( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, delegate ( GameEntity ship )
                        {
                            command.RelatedEntityIDs.Add( ship.PrimaryKeyID );
                            return DelReturn.Continue;
                        } );
                        if ( command.RelatedEntityIDs.Count > 0 )
                            World_AIW2.Instance.QueueGameCommand( command, true );
                    }
                    break;
                case "ToggleFRD":
                    {
                        if ( !World.Instance.IsLoaded )
                            return;
                        GameCommand command = GameCommand.Create( GameCommandType.SetBehavior );
                        command.SentWithToggleSet_SetOrdersForProducedUnits = Engine_AIW2.Instance.SettingOrdersForProducedUnits;
                        bool foundSomeOff = false;
                        Engine_AIW2.Instance.DoForSelected( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, delegate ( GameEntity selected )
                        {
                            if ( selected.EntitySpecificOrders.Behavior != EntityBehaviorType.Attacker )
                                foundSomeOff = true;
                            return DelReturn.Continue;
                        } );
                        EntityBehaviorType targetType = EntityBehaviorType.Stationary;
                        if ( foundSomeOff )
                            targetType = EntityBehaviorType.Attacker;
                        command.RelatedMagnitude = (int)targetType;
                        Engine_AIW2.Instance.DoForSelected( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, delegate ( GameEntity ship )
                        {
                            command.RelatedEntityIDs.Add( ship.PrimaryKeyID );
                            return DelReturn.Continue;
                        } );
                        if ( command.RelatedEntityIDs.Count > 0 )
                            World_AIW2.Instance.QueueGameCommand( command, true );
                    }
                    break;
                case "ToggleSettingOrdersForProducedUnits":
                    Engine_AIW2.Instance.SettingOrdersForProducedUnits = !Engine_AIW2.Instance.SettingOrdersForProducedUnits;
                    break;
                case "ToggleCombatSideBooleanFlag":
                    {
                        Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                        CombatSide side = planet.Combat.GetSideForWorldSide( World_AIW2.Instance.GetLocalPlayerSide() );
                        GameCommand command = GameCommand.Create( GameCommandType.ChangeCombatSideBooleanFlag );
                        command.RelatedSide = side.WorldSide;
                        command.RelatedPlanetIndex = planet.PlanetIndex;
                        command.RelatedCombatSideBooleanFlag = (CombatSideBooleanFlag)Int1;
                        command.RelatedBool = !side.BooleanFlags[command.RelatedCombatSideBooleanFlag];
                        World_AIW2.Instance.QueueGameCommand( command, true );
                    }
                    break;
                case "SelectAllMobileMilitary":
                case "SelectController":
                case "SelectSpaceDock":
                    {
                        if ( !World.Instance.IsLoaded )
                            return;
                        WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                        if ( localSide == null )
                            return;
                        Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                        if ( planet == null )
                            return;
                        EntityRollupType rollup = EntityRollupType.None;
                        MetalFlowPurpose flowType = MetalFlowPurpose.None;
                        switch ( InputActionInternalName )
                        {
                            case "SelectAllMobileMilitary":
                                rollup = EntityRollupType.MobileCombatants;
                                break;
                            case "SelectController":
                                rollup = EntityRollupType.Controllers;
                                break;
                            case "SelectSpaceDock":
                                rollup = EntityRollupType.HasAnyMetalFlows;
                                flowType = MetalFlowPurpose.BuildingShipsInternally;
                                break;
                            default:
                                return;
                        }

                        bool unselectingInstead = false;
                        if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Additive ) )
                        { }
                        else if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Subtractive ) )
                        {
                            unselectingInstead = true;
                        }
                        else
                        {
                            Engine_AIW2.Instance.ClearSelection( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy );
                        }

                        for ( int i = 0; i < planet.Combat.Sides.Count; i++ )
                        {
                            CombatSide side = planet.Combat.Sides[i];
                            if ( !side.WorldSide.ControlledByPlayerAccounts.Contains( PlayerAccount.Local.PlayerPrimaryKeyID ) )
                                continue;
                            side.Entities.DoForEntities( rollup, delegate ( GameEntity entity )
                             {
                                 if ( flowType != MetalFlowPurpose.None &&
                                      entity.TypeData.MetalFlows[flowType] == null )
                                     return DelReturn.Continue;
                                 if ( unselectingInstead )
                                     entity.Unselect();
                                 else
                                     entity.Select();
                                 return DelReturn.Continue;
                             } );
                        }
                    }
                    break;
                case "IncreaseFrameSize":
                case "DecreaseFrameSize":
                    {
                        GameCommand command = GameCommand.Create( GameCommandType.ChangeFrameSize );
                        command.RelatedMagnitude = 1;
                        if ( InputActionInternalName == "DecreaseFrameSize" )
                            command.RelatedMagnitude = -command.RelatedMagnitude;
                        World_AIW2.Instance.QueueGameCommand( command, true );
                    }
                    break;
                case "IncreaseFrameFrequency":
                case "DecreaseFrameFrequency":
                    {
                        GameCommand command = GameCommand.Create( GameCommandType.ChangeFrameFrequency );
                        command.RelatedMagnitude = 1;
                        if ( InputActionInternalName == "DecreaseFrameFrequency" )
                            command.RelatedMagnitude = -command.RelatedMagnitude;
                        World_AIW2.Instance.QueueGameCommand( command, true );
                    }
                    break;
                case "ShowShipRanges_Selected":
                    ArcenInput_AIW2.ShouldShowShipRanges_Selected = true;
                    break;
                case "ShowShipRanges_Hovered":
                    ArcenInput_AIW2.ShouldShowShipRanges_Hovered = true;
                    break;
                case "ShowShipRanges_All":
                    ArcenInput_AIW2.ShouldShowShipRanges_All = true;
                    break;
                case "ShowShipOrders":
                    ArcenInput_AIW2.ShouldShowShipOrders = true;
                    break;
                case "SelectBuilder":
                    {
                        if ( !World.Instance.IsLoaded )
                            return;
                        WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                        if ( localSide == null )
                            return;
                        Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                        if ( planet == null )
                            return;

                        GameEntity currentBuilder = null;
                        if ( Engine_AIW2.Instance.GetHasSelection( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy ) )
                        {
                            currentBuilder = ArcenExternalUIUtilities.GetEntityToUseForBuildMenu();
                            if ( currentBuilder != null )
                                Engine_AIW2.Instance.ClearSelection( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy );
                        }

                        Window_InGameBottomMenu.Instance.CloseAllExpansions();

                        CombatSide side = planet.Combat.GetSideForWorldSide( localSide );
                        bool foundCurrent = false;
                        GameEntity newBuilder = null;
                        GameEntity firstBuilder = null;
                        side.Entities.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity entity )
                         {
                             if ( entity.TypeData.BuildMenus == null || entity.TypeData.BuildMenus.Count <= 0 )
                                 return DelReturn.Continue;
                             if ( firstBuilder == null )
                             {
                                 firstBuilder = entity;
                             }
                             if ( entity == currentBuilder )
                             {
                                 foundCurrent = true;
                             }
                             else if ( foundCurrent )
                             {
                                 newBuilder = entity;
                                 return DelReturn.Break;
                             }
                             return DelReturn.Continue;
                         } );
                        if ( newBuilder == null && firstBuilder != null )
                        {
                            newBuilder = firstBuilder;
                        }
                        if ( newBuilder != null )
                        {
                            newBuilder.Select();
                            if ( !Window_InGameBuildTabMenu.Instance.IsOpen )
                                Window_InGameCommandsMenu.bToggleBuildMenu.Instance.HandleClick();
                        }
                    }
                    break;
                case "OpenTechMenu":
                case "ClearMenus":
                    if ( !World.Instance.IsLoaded )
                        return;
                    ToggleableWindowController window;
                    switch(InputActionInternalName)
                    {
                        case "OpenTechMenu":
                            window = Window_InGameTechTabMenu.Instance;
                            break;
                        case "ClearMenus":
                            window = null;
                            break;
                        default:
                            return;
                    }
                    bool closing = window == null || window.IsOpen;
                    Engine_AIW2.Instance.ClearSelection( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy );
                    Window_InGameBottomMenu.Instance.CloseAllExpansions();
                    if ( !closing )
                    {
                        Window_InGameBottomMenu.bToggleMasterMenu.Instance.HandleClick();

                        switch ( InputActionInternalName )
                        {
                            case "OpenTechMenu":
                                Window_InGameMasterMenu.bToggleTechMenu.Instance.HandleClick();
                                break;
                            default:
                                return;
                        }
                    }
                    break;
            }
        }
    }
}