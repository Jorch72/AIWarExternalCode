using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameObjectivesWindow : ToggleableWindowController
    {
        public static Window_InGameObjectivesWindow Instance;
        public Window_InGameObjectivesWindow()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        private int UpdatesSinceLastRefresh;
        private List<Objective> LastComputedObjectives = new List<Objective>();

        public class bsObjectives : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                //Window_InGameObjectivesWindow windowController = (Window_InGameObjectivesWindow)Element.Window.Controller;

                Instance.UpdatesSinceLastRefresh++;
                if ( Instance.UpdatesSinceLastRefresh >= 5 )
                {
                    Instance.UpdatesSinceLastRefresh = 0;

                    List<Objective> objectives = new List<Objective>();
                    for ( ObjectiveType type = ObjectiveType.None + 1; type < ObjectiveType.Length; type++ )
                    {
                        Objective objective = new Objective( type );
                        objective.ComputeCurrentState();
                        if ( objective.State == ObjectiveState.NotApplicable )
                            continue;
                        objectives.Add( objective );
                    }

                    bool detectedChange = objectives.Count != Instance.LastComputedObjectives.Count;
                    if ( !detectedChange )
                    {
                        for ( int i = 0; i < objectives.Count; i++ )
                        {
                            Objective newItem = objectives[i];
                            Objective oldItem = Instance.LastComputedObjectives[i];
                            if ( newItem.GetMatches( oldItem ) )
                                continue;
                            detectedChange = true;
                            break;
                        }
                    }

                    if ( detectedChange )
                    {
                        Instance.LastComputedObjectives = objectives;

                        elementAsType.ClearButtons();

                        for ( int y = 0; y < Instance.LastComputedObjectives.Count; y++ )
                        {
                            Objective item = Instance.LastComputedObjectives[y];
                            bObjective newButtonController = new bObjective( item );
                            Vector2 offset;
                            offset.x = 0;
                            offset.y = y * elementAsType.ButtonHeight;
                            Vector2 size;
                            size.x = elementAsType.ButtonWidth;
                            size.y = elementAsType.ButtonHeight;
                            elementAsType.AddButton( newButtonController, size, offset );
                        }

                        elementAsType.ActuallyDestroyButtonsThatAreStillCleared();
                    }
                }
            }
        }

        private class bObjective : ButtonAbstractBase
        {
            public Objective Item;

            public bObjective( Objective Item )
            {
                this.Item = Item;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                this.Item.GetTextToShow( buffer );
            }

            public override void HandleClick()
            {
                this.Item.HandleClick();
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }
    }

    public enum ObjectiveType
    {
        None,
        Victory,
        GainFleetStrength,
        ExploreGalaxy,
        ClaimFlagship,
        GetBonusShipType,
        DestroyAIPReducer,
        Length
    }

    public enum ObjectiveSubType
    {
        None,
        GainFleetStrength_ThroughProduction,
        GainFleetStrength_ThroughSpendingScience,
        GainFleetStrength_ThroughScience,
        GainFleetStrength_ThroughFuel,
        Length
    }

    public enum ObjectiveState
    {
        NotApplicable,
        Met,
        NeedToFind,
        NeedToAchieve,
        Length,
    }

    public class Objective
    {
        public readonly ObjectiveType Type;
        public ObjectiveSubType SubType;
        public ObjectiveState State;
        public readonly List<Planet> RelatedPlanets = new List<Planet>();
        public readonly List<GameEntityTypeData> RelatedEntityTypes = new List<GameEntityTypeData>();

        public Objective( ObjectiveType Type )
        {
            this.Type = Type;
        }

        public void ComputeCurrentState()
        {
            switch ( this.Type )
            {
                #region Victory
                case ObjectiveType.Victory:
                    {
                        GameEntity masterController = World_AIW2.Instance.GetFirstEntityMatching( SpecialEntityType.AIKingUnit, EntityRollupType.KingUnits );
                        if ( masterController == null )
                        {
                            this.State = ObjectiveState.Met;
                            break;
                        }
                        if ( masterController.Combat.Planet.HumansHaveBasicIntel )
                        {
                            this.State = ObjectiveState.NeedToAchieve;
                            this.RelatedPlanets.Add( masterController.Combat.Planet );
                            break;
                        }
                        this.State = ObjectiveState.NeedToFind;
                    }
                    break;
                #endregion
                #region GainFleetStrength
                case ObjectiveType.GainFleetStrength:
                    {
                        WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                        GameEntity ark = localSide.Entities.GetFirstMatching( SpecialEntityType.HumanKingUnit );
                        if ( ark == null )
                        {
                            this.State = ObjectiveState.NotApplicable;
                            break;
                        }
                        List<BuildMenu> menus = ark.TypeData.BuildMenus;
                        List<GameEntityTypeData> produceableTypes = new List<GameEntityTypeData>();
                        List<GameEntityTypeData> researchableTypes = new List<GameEntityTypeData>();
                        bool foundAnyCurrentlyProduceable = false;
                        bool foundAnyTechsCurrentlyResearchable = false;
                        for ( int i = 0; i < menus.Count; i++ )
                        {
                            BuildMenu menu = menus[i];
                            for ( int j = 0; j < menu.List.Count; j++ )
                            {
                                GameEntityTypeData item = menu.List[j];
                                if ( item.BalanceStats.SquadFuelConsumption <= 0 )
                                    continue;
                                ArcenRejectionReason reason = localSide.GetCanBuildAnother( item );
                                switch ( reason )
                                {
                                    case ArcenRejectionReason.SideDoesNotHaveEnoughCap:
                                        continue;
                                    case ArcenRejectionReason.SideDoesNotHavePrerequisiteTech:
                                        if ( item.TechPrereq.NotOnMainTechMenu )
                                            continue;
                                        ArcenRejectionReason techRejectionReason = localSide.GetCanResearch( item.TechPrereq, false, false );
                                        switch ( techRejectionReason )
                                        {
                                            case ArcenRejectionReason.SideDoesNotHavePrerequisiteTech:
                                                continue;
                                            case ArcenRejectionReason.Unknown:
                                                foundAnyTechsCurrentlyResearchable = true;
                                                break;
                                        }
                                        researchableTypes.Add( item );
                                        continue;
                                    case ArcenRejectionReason.Unknown:
                                        foundAnyCurrentlyProduceable = true;
                                        break;
                                }
                                produceableTypes.Add( item );
                            }
                        }
                        this.State = ObjectiveState.NeedToAchieve;
                        if ( foundAnyCurrentlyProduceable )
                        {
                            this.SubType = ObjectiveSubType.GainFleetStrength_ThroughProduction;
                            for ( int i = 0; i < produceableTypes.Count; i++ )
                            {
                                GameEntityTypeData entityType = produceableTypes[i];
                                if ( localSide.GetCanBuildAnother( entityType ) != ArcenRejectionReason.Unknown )
                                    continue;
                                this.RelatedEntityTypes.Add( entityType );
                            }
                            this.RelatedEntityTypes.Sort( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                             {
                                 FInt leftValue = Left.BalanceStats.StrengthPerSquad * localSide.GetRemainingCap( Left );
                                 FInt rightValue = Right.BalanceStats.StrengthPerSquad * localSide.GetRemainingCap( Right );
                                 return rightValue.CompareTo( leftValue );
                             } );
                        }
                        else if ( produceableTypes.Count > 0 )
                        {
                            this.SubType = ObjectiveSubType.GainFleetStrength_ThroughFuel;
                            GetPotentialCapturePlanets( localSide );
                            this.RelatedPlanets.Sort( delegate ( Planet Left, Planet Right )
                            {
                                return Right.ResourceOutputs[ResourceType.Fuel].CompareTo( Left.ResourceOutputs[ResourceType.Fuel] );
                            } );
                        }
                        else if ( foundAnyTechsCurrentlyResearchable )
                        {
                            this.SubType = ObjectiveSubType.GainFleetStrength_ThroughSpendingScience;
                            for ( int i = 0; i < researchableTypes.Count; i++ )
                            {
                                GameEntityTypeData entityType = researchableTypes[i];
                                if ( localSide.GetCanResearch( entityType.TechPrereq, false, false ) != ArcenRejectionReason.Unknown )
                                    continue;
                                this.RelatedEntityTypes.Add( entityType );
                            }
                            this.RelatedEntityTypes.Sort( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                            {
                                return Right.BalanceStats.StrengthPerCap.CompareTo( Left.BalanceStats.StrengthPerCap );
                            } );
                        }
                        else if ( researchableTypes.Count > 0 )
                        {
                            this.SubType = ObjectiveSubType.GainFleetStrength_ThroughScience;
                            GetPotentialCapturePlanets( localSide );
                            this.RelatedPlanets.Sort( delegate ( Planet Left, Planet Right )
                            {
                                FInt LeftValue = Left.ResourceOutputs[ResourceType.Science] - Left.ScienceGatheredBySideIndex[localSide.SideIndex];
                                FInt RightValue = Right.ResourceOutputs[ResourceType.Science] - Right.ScienceGatheredBySideIndex[localSide.SideIndex];
                                return RightValue.CompareTo( LeftValue );
                            } );
                        }
                        else
                            this.State = ObjectiveState.NotApplicable;
                    }
                    break;
                #endregion
                #region ExploreGalaxy
                case ObjectiveType.ExploreGalaxy:
                    {
                        GetKnownPlanetsWithAtLeastOneUnitOf( WorldSideType.AI, EntityRollupType.ScramblesSensors );
                        this.RelatedPlanets.Sort( delegate ( Planet Left, Planet Right )
                        {
                            return Left.OriginalHopsToHumanHomeworld.CompareTo( Right.OriginalHopsToHumanHomeworld );
                        } );
                        if ( this.RelatedPlanets.Count > 0 )
                            this.State = ObjectiveState.NeedToAchieve;
                    }
                    break;
                #endregion
                #region ClaimFlagship
                case ObjectiveType.ClaimFlagship:
                    {
                        GetKnownPlanetsWithAtLeastOneUnitOf( WorldSideType.NaturalObject, "Flagship" );
                        this.RelatedPlanets.Sort( delegate ( Planet Left, Planet Right )
                        {
                            return Left.OriginalHopsToHumanHomeworld.CompareTo( Right.OriginalHopsToHumanHomeworld );
                        } );
                        if ( this.RelatedPlanets.Count > 0 )
                            this.State = ObjectiveState.NeedToAchieve;
                    }
                    break;
                #endregion
                #region GetBonusShipType
                case ObjectiveType.GetBonusShipType:
                    {
                        GetKnownPlanetsWithAtLeastOneUnitOf( WorldSideType.AI, "AdvancedResearchStation" );
                        this.RelatedPlanets.Sort( delegate ( Planet Left, Planet Right )
                        {
                            return Left.OriginalHopsToHumanHomeworld.CompareTo( Right.OriginalHopsToHumanHomeworld );
                        } );
                        if ( this.RelatedPlanets.Count > 0 )
                            this.State = ObjectiveState.NeedToAchieve;
                    }
                    break;
                #endregion
                #region DestroyAIPReducer
                case ObjectiveType.DestroyAIPReducer:
                    {
                        GetKnownPlanetsWithAtLeastOneUnitOf( WorldSideType.AI, "DataCenter" );
                        this.RelatedPlanets.Sort( delegate ( Planet Left, Planet Right )
                        {
                            return Left.OriginalHopsToHumanHomeworld.CompareTo( Right.OriginalHopsToHumanHomeworld );
                        } );
                        if ( this.RelatedPlanets.Count > 0 )
                            this.State = ObjectiveState.NeedToAchieve;
                    }
                    break;
                    #endregion
            }
        }

        public void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
        {
            switch ( this.Type )
            {
                #region Victory
                case ObjectiveType.Victory:
                    switch ( this.State )
                    {
                        case ObjectiveState.Met:
                            buffer.Add( "You've Won!" );
                            break;
                        case ObjectiveState.NeedToFind:
                            buffer.Add( "Find the AI Master Controller, and destroy it" );
                            break;
                        case ObjectiveState.NeedToAchieve:
                            buffer.Add( "Destroy the AI Master Controller on " );
                            if ( this.RelatedPlanets.Count > 0 )
                                buffer.Add( this.RelatedPlanets[0].Name );
                            break;
                    }
                    break;
                #endregion
                #region GainFleetStrength
                case ObjectiveType.GainFleetStrength:
                    buffer.Add( "Strengthen your Fleet by " );
                    switch ( this.SubType )
                    {
                        case ObjectiveSubType.GainFleetStrength_ThroughProduction:
                            buffer.Add( "producing " );
                            AppendEntityNames( buffer, 3 );
                            break;
                        case ObjectiveSubType.GainFleetStrength_ThroughFuel:
                            buffer.Add( "securing more fuel from " );
                            AppendPlanetNames( buffer, 3 );
                            break;
                        case ObjectiveSubType.GainFleetStrength_ThroughSpendingScience:
                            buffer.Add( "researching " );
                            AppendEntityNames( buffer, 3 );
                            break;
                        case ObjectiveSubType.GainFleetStrength_ThroughScience:
                            buffer.Add( "securing more science from " );
                            AppendPlanetNames( buffer, 3 );
                            break;
                    }
                    break;
                #endregion
                #region ExploreGalaxy
                case ObjectiveType.ExploreGalaxy:
                    {
                        buffer.Add( "Explore Galaxy by destroying sensor scrambler(s) on " );
                        AppendPlanetNames( buffer, 3 );
                    }
                    break;
                #endregion
                #region ClaimFlagship
                case ObjectiveType.ClaimFlagship:
                    {
                        buffer.Add( "Strengthen your Fleet by claiming Flagship(s) on " );
                        AppendPlanetNames( buffer, 3 );
                    }
                    break;
                #endregion
                #region GetBonusShipType
                case ObjectiveType.GetBonusShipType:
                    {
                        buffer.Add( "Strengthen your Fleet by claiming Advanced Research Station(s) on " );
                        AppendPlanetNames( buffer, 3 );
                    }
                    break;
                #endregion
                #region DestroyAIPReducer
                case ObjectiveType.DestroyAIPReducer:
                    {
                        buffer.Add( "Disrupt enemy operations by destroying Data Center(s) on " );
                        AppendPlanetNames( buffer, 3 );
                    }
                    break;
                    #endregion
            }
        }

        private void AppendPlanetNames( ArcenDoubleCharacterBuffer buffer, int max )
        {
            for ( int i = 0; i < this.RelatedPlanets.Count && i < max; i++ )
            {
                if ( i > 0 )
                    buffer.Add( ", " );
                buffer.Add( this.RelatedPlanets[i].Name );
            }
            if ( this.RelatedPlanets.Count > max )
                buffer.Add( ", ..." );
        }

        private void AppendEntityNames( ArcenDoubleCharacterBuffer buffer, int max )
        {
            for ( int i = 0; i < this.RelatedEntityTypes.Count && i < max; i++ )
            {
                if ( i > 0 )
                    buffer.Add( ", " );
                buffer.Add( this.RelatedEntityTypes[i].Name );
            }
            if ( this.RelatedEntityTypes.Count > max )
                buffer.Add( ", ..." );
        }

        private void GetPotentialCapturePlanets( WorldSide localSide )
        {
            Galaxy galaxy = Engine_AIW2.Instance.NonSim_GetGalaxyBeingCurrentlyViewed();
            for ( int i = 0; i < galaxy.Planets.Count; i++ )
            {
                Planet planet = galaxy.Planets[i];
                if ( !planet.HumansHaveBasicIntel )
                    continue;
                if ( localSide.GetIsFriendlyTowards( planet.GetControllingSide() ) )
                    continue;
                bool foundEligibleSelfOrNeighbor = false;
                if ( planet.Combat.GetSideForWorldSide( localSide ).Entities.GetNumberIn( EntityRollupType.KingUnits ) > 0 )
                    foundEligibleSelfOrNeighbor = true;
                else
                {
                    planet.DoForLinkedNeighbors( delegate ( Planet neighbor )
                    {
                        if ( localSide.GetIsFriendlyTowards( neighbor.GetControllingSide() ) )
                        {
                            foundEligibleSelfOrNeighbor = true;
                            return DelReturn.Break;
                        }
                        else if ( neighbor.Combat.GetSideForWorldSide( localSide ).Entities.GetNumberIn( EntityRollupType.KingUnits ) > 0 )
                        {
                            foundEligibleSelfOrNeighbor = true;
                            return DelReturn.Break;
                        }
                        return DelReturn.Continue;
                    } );
                }
                if ( !foundEligibleSelfOrNeighbor )
                    continue;
                this.RelatedPlanets.Add( planet );
            }
        }

        public bool GetMatches( Objective Other  )
        {
            if ( this.Type != Other.Type )
                return false;
            if ( this.SubType != Other.SubType )
                return false;
            if ( this.State != Other.State )
                return false;
            if ( this.RelatedPlanets.Count != Other.RelatedPlanets.Count )
                return false;
            for ( int i = 0; i < this.RelatedPlanets.Count; i++ )
                if ( this.RelatedPlanets[i] != Other.RelatedPlanets[i] )
                    return false;
            if ( this.RelatedEntityTypes.Count != Other.RelatedEntityTypes.Count )
                return false;
            for ( int i = 0; i < this.RelatedEntityTypes.Count; i++ )
                if ( this.RelatedEntityTypes[i] != Other.RelatedEntityTypes[i] )
                    return false;
            return true;
        }

        private void GetKnownPlanetsWithAtLeastOneUnitOf( WorldSideType sideType, EntityRollupType rollup )
        {
            Galaxy galaxy = Engine_AIW2.Instance.NonSim_GetGalaxyBeingCurrentlyViewed();
            for ( int i = 0; i < galaxy.Planets.Count; i++ )
            {
                Planet planet = galaxy.Planets[i];
                if ( !planet.HumansHaveBasicIntel )
                    continue;
                if ( planet.Combat.GetNumberIn( sideType, rollup ) <= 0 )
                    continue;
                this.RelatedPlanets.Add( planet );
            }
        }

        private void GetKnownPlanetsWithAtLeastOneUnitOf( WorldSideType sideType, string Tag )
        {
            Galaxy galaxy = Engine_AIW2.Instance.NonSim_GetGalaxyBeingCurrentlyViewed();
            for ( int i = 0; i < galaxy.Planets.Count; i++ )
            {
                Planet planet = galaxy.Planets[i];
                if ( !planet.HumansHaveBasicIntel )
                    continue;
                if ( planet.Combat.GetFirstMatching( sideType, Tag ) == null )
                    continue;
                this.RelatedPlanets.Add( planet );
            }
        }

        public void HandleClick()
        {
            switch(this.Type)
            {
                case ObjectiveType.Victory:
                    if(this.State == ObjectiveState.NeedToAchieve)
                    {
                        if ( this.RelatedPlanets.Count <= 0 )
                            return;
                        Planet planet = this.RelatedPlanets[0];
                        GameEntity entity = planet.Combat.GetFirstMatching( WorldSideType.AI, SpecialEntityType.AIKingUnit );
                        CenteringHelper( planet, entity );
                    }
                    break;
                case ObjectiveType.GainFleetStrength:
                    {
                        switch ( this.SubType )
                        {
                            case ObjectiveSubType.GainFleetStrength_ThroughProduction:
                                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                                WorldSide worldSide = World_AIW2.Instance.GetLocalSide();
                                CombatSide side = planet.Combat.GetSideForWorldSide( worldSide );
                                GameEntity producer = null;
                                side.Entities.DoForEntities( SpecialEntityType.HumanKingUnit, delegate ( GameEntity entity )
                                 {
                                     producer = entity;
                                     return DelReturn.Break;
                                 } );
                                if ( producer == null )
                                {
                                    side.Entities.DoForEntities( EntityRollupType.HasAnyMetalFlows, delegate ( GameEntity entity )
                                    {
                                        if ( entity.TypeData.MetalFlows[MetalFlowPurpose.BuildingShipsInternally] == null )
                                            return DelReturn.Continue;
                                        producer = entity;
                                        return DelReturn.Break;
                                    } );
                                }
                                if ( producer == null )
                                    break;
                                Engine_AIW2.Instance.ClearSelection();
                                producer.Select();
                                Window_InGameBuildMenu.Instance.Open();
                                Window_InGameMasterMenu.Instance.CloseWindowsOtherThanThisOne( Window_InGameBuildMenu.Instance );
                                break;
                            case ObjectiveSubType.GainFleetStrength_ThroughSpendingScience:
                                Window_InGameTechMenu.Instance.Open();
                                Window_InGameMasterMenu.Instance.CloseWindowsOtherThanThisOne( Window_InGameTechMenu.Instance );
                                break;
                            case ObjectiveSubType.GainFleetStrength_ThroughFuel:
                            case ObjectiveSubType.GainFleetStrength_ThroughScience:
                                CenteringHelperForPlanetList( this.RelatedPlanets, this.Type, this.SubType );
                                break;
                        }
                    }
                    break;
                case ObjectiveType.ClaimFlagship:
                case ObjectiveType.DestroyAIPReducer:
                case ObjectiveType.ExploreGalaxy:
                case ObjectiveType.GetBonusShipType:
                    CenteringHelperForPlanetList( this.RelatedPlanets, this.Type, this.SubType );
                    break;
            }
        }

        private static void CenteringHelperForPlanetList(List<Planet> planets, ObjectiveType Type, ObjectiveSubType SubType)
        {
            if ( planets.Count <= 0 )
                return;
            {
                Planet planetToCenterOn = null;
                Planet currentPlanet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                bool foundCurrent = false;
                for ( int i = 0; i < planets.Count; i++ )
                {
                    Planet planet = planets[i];
                    if ( foundCurrent )
                    {
                        planetToCenterOn = planet;
                        break;
                    }
                    if ( currentPlanet == planet )
                        foundCurrent = true;
                }
                if ( planetToCenterOn == null )
                    planetToCenterOn = planets[0];
                if ( planetToCenterOn == currentPlanet )
                {
                    WorldSideType sideType = WorldSideType.Length;
                    string tag = string.Empty;
                    switch ( Type )
                    {
                        case ObjectiveType.ClaimFlagship:
                            tag = "Flagship";
                            sideType = WorldSideType.NaturalObject;
                            break;
                        case ObjectiveType.DestroyAIPReducer:
                            tag = "DataCenter";
                            sideType = WorldSideType.AI;
                            break;
                        case ObjectiveType.ExploreGalaxy:
                            tag = "SensorScrambler";
                            sideType = WorldSideType.AI;
                            break;
                        case ObjectiveType.GetBonusShipType:
                            tag = "AdvancedResearchStation";
                            sideType = WorldSideType.AI;
                            break;
                        case ObjectiveType.GainFleetStrength:
                            switch ( SubType )
                            {
                                case ObjectiveSubType.GainFleetStrength_ThroughFuel:
                                    tag = "FuelGenerator";
                                    sideType = WorldSideType.NaturalObject;
                                    break;
                                case ObjectiveSubType.GainFleetStrength_ThroughScience:
                                    tag = "FuelGenerator";
                                    sideType = WorldSideType.NaturalObject;
                                    break;
                            }
                            break;
                    }
                    if ( tag.Length > 0 )
                    {
                        GameEntity entity = currentPlanet.Combat.GetFirstMatching( sideType, tag );
                        CenteringHelper( currentPlanet, entity );
                    }
                }
                else
                {
                    CenteringHelper( planetToCenterOn, null );
                }
            }
        }

        private static void CenteringHelper( Planet planet, GameEntity entity )
        {
            if ( planet.GetDoHumansHaveVision() && ( Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() == planet ) && entity != null )
            {
                if ( Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() != planet )
                    Engine_AIW2.Instance.PresentationLayer.ReactToLeavingPlanetView( Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() );
                bool needToSwitchViewMode = Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.MainGameView;
                World_AIW2.Instance.SwitchViewToPlanet( planet );
                if ( needToSwitchViewMode )
                    Engine_AIW2.Instance.SetCurrentGameViewMode( GameViewMode.MainGameView );
                Engine_AIW2.Instance.PresentationLayer.CenterPlanetViewOnEntity( entity, false );
                if ( needToSwitchViewMode )
                    Engine_AIW2.Instance.PresentationLayer.ReactToEnteringPlanetView( planet );
            }
            else
            {
                if ( Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.GalaxyMapView )
                    Engine_AIW2.Instance.PresentationLayer.ReactToLeavingPlanetView( Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() );
                World_AIW2.Instance.SwitchViewToPlanet( planet );
                Engine_AIW2.Instance.SetCurrentGameViewMode( GameViewMode.GalaxyMapView );
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( planet, false );
            }
        }
    }
}