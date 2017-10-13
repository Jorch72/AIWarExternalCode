using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arcen.AIW2.External
{
    public class AIDefensePlacer_Default : IAIDefensePlacerImplementation
    {
        private bool HaveLoadedData;
        private int MinimumControllerDistance;
        private int MaximumControllerDistance;
        private int MinimumNumberOfNonControllerStrongAreas;
        private int MaximumNumberOfNonControllerStrongAreas;
        private int MinimumDistanceOfStrongAreaFromController;
        private int MaximumDistanceOfStrongAreaFromController;

        private void EnsureLoadedCustomData(ArcenDynamicTableRow Row)
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;
            CustomDataSet set = Row.GetCustomData( "defensePlacer" );
            this.MinimumControllerDistance = ( set.GetFInt( "min_distance_percent_center_to_controller" ) * ExternalConstants.Instance.Balance_AverageGravWellRadius ).IntValue;
            this.MaximumControllerDistance = ( set.GetFInt( "max_distance_percent_center_to_controller" ) * ExternalConstants.Instance.Balance_AverageGravWellRadius ).IntValue;
            this.MinimumNumberOfNonControllerStrongAreas = set.GetInt( "min_number_of_non_controller_strong_areas" );
            this.MaximumNumberOfNonControllerStrongAreas = set.GetInt( "max_number_of_non_controller_strong_areas" );
            this.MinimumDistanceOfStrongAreaFromController = ( set.GetFInt( "min_distance_percent_controller_to_strong_area" ) * ExternalConstants.Instance.Balance_AverageGravWellRadius ).IntValue;
            this.MaximumDistanceOfStrongAreaFromController = ( set.GetFInt( "max_distance_percent_controller_to_strong_area" ) * ExternalConstants.Instance.Balance_AverageGravWellRadius ).IntValue;
        }

        public ArcenPoint GetPointForController( ArcenSimContext Context, Planet ThisPlanet )
        {
            if ( ThisPlanet.PopulationType == PlanetPopulationType.AIHomeworld )
                return Engine_AIW2.Instance.CombatCenter;
            this.EnsureLoadedCustomData( ThisPlanet.AIDefensePlacer );
            Context.QualityRandom.ReinitializeWithSeed( ThisPlanet.PlanetIndex );
            return Engine_AIW2.Instance.CombatCenter.GetRandomPointWithinDistance( Context.QualityRandom, this.MinimumControllerDistance, this.MaximumControllerDistance );
        }

        public int GetNumberOfNonControllerStrongAreas( ArcenSimContext Context, Planet ThisPlanet )
        {
            if ( ThisPlanet.PopulationType == PlanetPopulationType.AIHomeworld )
                return 0;
            this.EnsureLoadedCustomData( ThisPlanet.AIDefensePlacer );
            Context.QualityRandom.ReinitializeWithSeed( ThisPlanet.PlanetIndex+1 );
            return Context.QualityRandom.Next( this.MinimumNumberOfNonControllerStrongAreas, this.MaximumNumberOfNonControllerStrongAreas );
        }

        public ArcenPoint GetPointForNonControllerStrongArea( ArcenSimContext Context, Planet ThisPlanet, int StrongAreaIndex )
        {
            this.EnsureLoadedCustomData( ThisPlanet.AIDefensePlacer );
            Context.QualityRandom.ReinitializeWithSeed( ThisPlanet.PlanetIndex + 2 + StrongAreaIndex );

            ArcenPoint controllerLocation = ThisPlanet.GetController().WorldLocation;

            int triesLeft = 100;
            ArcenPoint strongAreaLocation = ArcenPoint.ZeroZeroPoint;
            while ( true )
            {
                triesLeft--;
                strongAreaLocation = controllerLocation.GetRandomPointWithinDistance( Context.QualityRandom, this.MinimumDistanceOfStrongAreaFromController, this.MaximumDistanceOfStrongAreaFromController );
                if ( !Engine_AIW2.Instance.FastGetIsPointOutsideGravWell( strongAreaLocation ) )
                    break;
                if ( triesLeft <= 0 )
                    strongAreaLocation = controllerLocation;
            }

            return strongAreaLocation;
        }

        public void DoInitialOrReconquestDefenseSeeding( ArcenSimContext Context, Planet ThisPlanet )
        {
            GameEntity controller = ThisPlanet.GetController();

            AIBudgetItem budgetItem = controller.Side.WorldSide.AITypeData.BudgetItems[AIBudgetType.Reinforcement];

            int direGuardianCount = ThisPlanet.MarkLevel.PlanetDireGuardianCount;

            List<GameEntityTypeData> guardianTypes = new List<GameEntityTypeData>();
            if ( direGuardianCount > 0 )
            {
                for ( int j = 0; j < budgetItem.DireGuardianMenusToBuyFrom.Count; j++ )
                {
                    for ( int i = 0; i < budgetItem.DireGuardianMenusToBuyFrom[j].List.Count; i++ )
                    {
                        GameEntityTypeData entityType = budgetItem.DireGuardianMenusToBuyFrom[j].List[i];
                        guardianTypes.Add( entityType );
                    }
                }

                Helper_SeedDireGuardians( Context, ThisPlanet, controller.WorldLocation, controller, guardianTypes, direGuardianCount, FInt.FromParts( 0, 250 ), FInt.FromParts( 0, 400 ), EntityBehaviorType.Guard_Guardian_Patrolling, true );
            }
            
            guardianTypes.Clear();
            for ( int j = 0; j < budgetItem.GuardianMenusToBuyFrom.Count; j++ )
            {
                for ( int i = 0; i < budgetItem.GuardianMenusToBuyFrom[j].List.Count; i++ )
                {
                    GameEntityTypeData entityType = budgetItem.GuardianMenusToBuyFrom[j].List[i];
                    if ( entityType.Balance_MarkLevel.Ordinal > 0 &&
                        entityType.Balance_MarkLevel != ThisPlanet.MarkLevel )
                        continue;
                    if ( World_AIW2.Instance.CorruptedAIDesigns.Contains( entityType ) )
                        continue;
                    if ( !entityType.AICanUseThisWithoutUnlockingIt && !World_AIW2.Instance.UnlockedAIDesigns.Contains( entityType ) )
                        continue;
                    guardianTypes.Add( entityType );
                }
            }

            FInt turretStrengthBudget = ThisPlanet.GetGuardingAITurretStrengthCap() / 2;
            FInt shieldStrengthBudget = ThisPlanet.GetGuardingAIShieldStrengthCap() / 2;
            FInt guardianStrengthBudget = ThisPlanet.GetGuardingAIGuardianStrengthCap() / 2;
            FInt fleetShipStrengthBudget = ThisPlanet.GetGuardingAIFleetShipStrengthCap() / 2;

            int extraStrongPoints = this.GetNumberOfNonControllerStrongAreas( Context, ThisPlanet );

            // if seeding more than one strong point, seed half as many mobile patrollers, so we have enough for the static stuff
            FInt mobilePatrollingPortion = extraStrongPoints <= 0 ? FInt.FromParts( 0, 400 ) : FInt.FromParts( 0, 200 );
            guardianStrengthBudget -= Helper_SeedGuardians( Context, ThisPlanet, Engine_AIW2.Instance.CombatCenter, controller, guardianTypes,
                guardianStrengthBudget * mobilePatrollingPortion, FInt.FromParts( 0, 500 ), FInt.FromParts( 0, 750 ), EntityBehaviorType.Guard_Guardian_Patrolling, true, 0 );
            
            // if seeding more than one strong point, don't seed the stationary patrollers, so we have enough for the static stuff
            if ( extraStrongPoints <= 0 )
            {
                guardianStrengthBudget -= Helper_SeedGuardians( Context, ThisPlanet, controller.WorldLocation, controller, guardianTypes,
                    guardianStrengthBudget * FInt.FromParts( 0, 500 ), FInt.FromParts( 0, 250 ), FInt.FromParts( 0, 400 ), EntityBehaviorType.Guard_Guardian_Patrolling, false, 0 );
            }

            FInt portionPerStrongPoint = FInt.One / ( extraStrongPoints + 1 );
            for ( int i = -1; i < extraStrongPoints; i++ )
            {
                ArcenPoint strongPointLocation;
                FInt maxDistance;
                int seedingsLeft = extraStrongPoints - i;
                FInt budgetPortion = FInt.One / seedingsLeft;
                if ( i == -1 )
                {
                    strongPointLocation = controller.WorldLocation;
                    maxDistance = FInt.FromParts( 0, 150 );
                    //FInt portionNotSpending = FInt.One - budgetPortion;
                    //budgetPortion = FInt.One - ( portionNotSpending / 2 ); // leave half as much as we normally would
                    //budgetPortion = Mat.Max( budgetPortion * 2, FInt.One );
                }
                else
                {
                    strongPointLocation = this.GetPointForNonControllerStrongArea( Context, ThisPlanet, i );
                    maxDistance = FInt.FromParts( 0, 100 );
                }
                guardianStrengthBudget -= Helper_SeedGuardians( Context, ThisPlanet, strongPointLocation, controller, guardianTypes,
                    guardianStrengthBudget * budgetPortion, FInt.FromParts( 0, 050 ), maxDistance, EntityBehaviorType.Guard_Guardian_Anchored, false, 0 );
            }

            Reinforce( Context, ThisPlanet, controller.Side.WorldSide, shieldStrengthBudget, ReinforcementType.Shield );
            Reinforce( Context, ThisPlanet, controller.Side.WorldSide, turretStrengthBudget, ReinforcementType.Turret );
            Reinforce( Context, ThisPlanet, controller.Side.WorldSide, fleetShipStrengthBudget, ReinforcementType.FleetShip );
        }

        private void Helper_SeedDireGuardians( ArcenSimContext Context, Planet ThisPlanet, ArcenPoint centerPoint, GameEntity controller, List<GameEntityTypeData> guardianTypes, int Count, FInt minDistanceFactor, FInt maxDistanceFactor, EntityBehaviorType behavior, Boolean isMobilePatrol )
        {
            int minDistance = ( ExternalConstants.Instance.Balance_AverageGravWellRadius * minDistanceFactor ).IntValue;
            int maxDistance = ( ExternalConstants.Instance.Balance_AverageGravWellRadius * maxDistanceFactor ).IntValue;

            while ( Count > 0 )
            {
                Count--;

                GameEntityTypeData guardianData = guardianTypes[Context.QualityRandom.Next( 0, guardianTypes.Count )];

                ArcenPoint point = ThisPlanet.Combat.GetSafePlacementPoint( Context, guardianData, centerPoint, minDistance, maxDistance );
                if ( point == ArcenPoint.ZeroZeroPoint )
                    continue;

                GameEntity newEntity = GameEntity.CreateNew( controller.Side, guardianData, point, Context );
                newEntity.EntitySpecificOrders.Behavior = behavior;
                newEntity.GuardedObject = controller;
                switch ( behavior )
                {
                    case EntityBehaviorType.Guard_Guardian_Anchored:
                        break;
                    case EntityBehaviorType.Guard_Guardian_Patrolling:
                        newEntity.GuardingOffsets.Add( newEntity.WorldLocation - newEntity.GuardedObject.WorldLocation );
                        if ( isMobilePatrol )
                        {
                            AngleDegrees initialAngle = newEntity.GuardedObject.WorldLocation.GetAngleToDegrees( newEntity.WorldLocation );
                            int initialDistance = newEntity.GuardedObject.WorldLocation.GetDistanceTo( newEntity.WorldLocation, false );
                            int step = ( AngleDegrees.MaxValue / 6 ).IntValue;
                            for ( int i = step; i < AngleDegrees.MaxValue; i += step )
                            {
                                AngleDegrees angleToThisPoint = initialAngle.Add( AngleDegrees.Create( (FInt)i ) );
                                ArcenPoint thisPoint = newEntity.GuardedObject.WorldLocation.GetPointAtAngleAndDistance( angleToThisPoint, initialDistance );
                                newEntity.GuardingOffsets.Add( thisPoint - newEntity.GuardedObject.WorldLocation );
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// returns amount spent on guardians
        /// </summary>
        private FInt Helper_SeedGuardians( ArcenSimContext Context, Planet ThisPlanet, ArcenPoint centerPoint, GameEntity entityToGuard, List<GameEntityTypeData> guardianTypes, FInt budget, FInt minDistanceFactor, FInt maxDistanceFactor, EntityBehaviorType behavior, Boolean isMobilePatrol, int MaxCountToSeed )
        {
            int minDistance = ( ExternalConstants.Instance.Balance_AverageGravWellRadius * minDistanceFactor ).IntValue;
            int maxDistance = ( ExternalConstants.Instance.Balance_AverageGravWellRadius * maxDistanceFactor ).IntValue;

            FInt result = FInt.Zero;
            while ( budget > FInt.Zero )
            {
                GameEntityTypeData guardianData = guardianTypes[Context.QualityRandom.Next( 0, guardianTypes.Count )];

                budget -= guardianData.BalanceStats.StrengthPerSquad;

                ArcenPoint point = ThisPlanet.Combat.GetSafePlacementPoint( Context, guardianData, centerPoint, minDistance, maxDistance );
                if ( point == ArcenPoint.ZeroZeroPoint )
                    continue;

                result += guardianData.BalanceStats.StrengthPerSquad;

                GameEntity newEntity = GameEntity.CreateNew( entityToGuard.Side, guardianData, point, Context );
                newEntity.EntitySpecificOrders.Behavior = behavior;
                newEntity.GuardedObject = entityToGuard;
                switch ( behavior )
                {
                    case EntityBehaviorType.Guard_Guardian_Anchored:
                        break;
                    case EntityBehaviorType.Guard_Guardian_Patrolling:
                        newEntity.GuardingOffsets.Add( newEntity.WorldLocation - newEntity.GuardedObject.WorldLocation );
                        if ( isMobilePatrol )
                        {
                            AngleDegrees initialAngle = centerPoint.GetAngleToDegrees( newEntity.WorldLocation );
                            int initialDistance = centerPoint.GetDistanceTo( newEntity.WorldLocation, false );
                            int step = ( AngleDegrees.MaxValue / 6 ).IntValue;
                            for ( int i = step; i < AngleDegrees.MaxValue; i += step )
                            {
                                AngleDegrees angleToThisPoint = initialAngle.Add( AngleDegrees.Create( (FInt)i ) );
                                ArcenPoint thisPoint = centerPoint.GetPointAtAngleAndDistance( angleToThisPoint, initialDistance );
                                newEntity.GuardingOffsets.Add( thisPoint - newEntity.GuardedObject.WorldLocation );
                            }
                        }
                        break;
                }

                if ( MaxCountToSeed > 0 )
                {
                    MaxCountToSeed--;
                    if ( MaxCountToSeed == 0 )
                        break;
                }
            }

            return result;
        }

        public FInt Reinforce( ArcenSimContext Context, Planet planet, WorldSide side, FInt budget, ReinforcementType reinforcementType )
        {
            ShortRangePlanning_StrengthData_CombatSide_Stance strengthData = planet.Combat.GetSideForWorldSide( side ).DataByStance[SideStance.Self];

            FInt strengthCap;
            FInt strengthPresent;
            switch ( reinforcementType )
            {
                case ReinforcementType.FleetShip:
                    strengthCap = planet.GetGuardingAIFleetShipStrengthCap();
                    strengthPresent = strengthData.GuardStrength - strengthData.TurretStrength - strengthData.ShieldStrength;
                    break;
                case ReinforcementType.Turret:
                    strengthCap = planet.GetGuardingAITurretStrengthCap();
                    strengthPresent = strengthData.TurretStrength;
                    break;
                case ReinforcementType.Shield:
                    strengthCap = planet.GetGuardingAIShieldStrengthCap();
                    strengthPresent = strengthData.ShieldStrength;
                    break;
                default:
                    return FInt.Zero;
            }

            if ( strengthPresent >= strengthCap )
                return FInt.Zero;

            List<GameEntity> entitiesWeCanReinforce = new List<GameEntity>();
            planet.Combat.GetSideForWorldSide( side ).Entities.DoForEntities( EntityRollupType.ReinforcementLocations, delegate ( GameEntity guardian )
            {
                switch ( reinforcementType )
                {
                    case ReinforcementType.Shield:
                    case ReinforcementType.Turret:
                        if ( guardian.EntitySpecificOrders.Behavior != EntityBehaviorType.Guard_Guardian_Anchored )
                            return DelReturn.Continue;
                        break;
                }
                entitiesWeCanReinforce.Add( guardian );
                guardian.Working_ReinforcementsOnly_ContentsStrength = guardian.GetStrengthOfContentsIfAny();
                return DelReturn.Continue;
            } );

            switch ( reinforcementType )
            {
                case ReinforcementType.Turret:
                case ReinforcementType.Shield:
                    GameEntity controller = planet.GetController();
                    if ( controller.Side.WorldSide == side )
                        entitiesWeCanReinforce.Add( controller );
                    break;
            }

                    FInt result = FInt.Zero;
            List<BuildMenu> buildMenus = null;

            bool atMostOnePerReinforceable = false;
            switch ( reinforcementType )
            {
                case ReinforcementType.Shield:
                    List<GameEntity> entitiesThatNeedMoreShieldCoverage = new List<GameEntity>();
                    for ( int i = 0; i < entitiesWeCanReinforce.Count; i++ )
                    {
                        GameEntity entity = entitiesWeCanReinforce[i];
                        if ( entity.ProtectingShieldIDs.Count > 0 )
                            continue;
                        entitiesThatNeedMoreShieldCoverage.Add( entity );
                    }

                    buildMenus = side.AITypeData.BudgetItems[AIBudgetType.Reinforcement].ShieldMenusToBuyFrom;
                    entitiesWeCanReinforce = entitiesThatNeedMoreShieldCoverage;
                    atMostOnePerReinforceable = true;
                    break;
                case ReinforcementType.Turret:
                    buildMenus = side.AITypeData.BudgetItems[AIBudgetType.Reinforcement].TurretMenusToBuyFrom;
                    break;
                case ReinforcementType.FleetShip:
                    buildMenus = side.AITypeData.BudgetItems[AIBudgetType.Reinforcement].NormalMenusToBuyFrom;
                    break;
            }

            result += Inner_ReinforceWithFleetShipsOrTurrets( Context, planet, side, ref budget, reinforcementType, strengthCap, ref strengthPresent, buildMenus, entitiesWeCanReinforce, atMostOnePerReinforceable );

            return result;
        }

        private FInt Inner_ReinforceWithFleetShipsOrTurrets( ArcenSimContext Context, Planet planet, WorldSide side, ref FInt budget, ReinforcementType reinforcementType, FInt strengthCap, ref FInt strengthPresent, List<BuildMenu> buildMenus, List<GameEntity> entitiesWeCanReinforce, bool atMostOnePerReinforceable )
        {
            if ( entitiesWeCanReinforce.Count <= 0 )
                return FInt.Zero;

            ArcenRandomDrawBag<GameEntityTypeData> bag = new ArcenRandomDrawBag<GameEntityTypeData>();
            for ( int i = 0; i < buildMenus.Count; i++ )
            {
                BuildMenu menu = buildMenus[i];
                for ( int j = 0; j < menu.List.Count; j++ )
                {
                    int timesToAdd = 0;
                    GameEntityTypeData buyableType = menu.List[j];
                    if ( buyableType.Balance_MarkLevel.Ordinal > 0 && buyableType.Balance_MarkLevel != planet.MarkLevel )
                        continue;
                    if ( World_AIW2.Instance.CorruptedAIDesigns.Contains( buyableType ) )
                        continue;
                    if ( !buyableType.AICanUseThisWithoutUnlockingIt && !World_AIW2.Instance.UnlockedAIDesigns.Contains( buyableType ) )
                        continue;
                    timesToAdd = 1;
                    if ( timesToAdd <= 0 )
                        continue;
                    bag.AddItem( buyableType, timesToAdd );
                }
            }

            if ( !bag.GetHasItems() )
                return FInt.Zero;

            entitiesWeCanReinforce.Sort( delegate ( GameEntity Left, GameEntity Right )
            {
                return Left.Working_ReinforcementsOnly_ContentsStrength.CompareTo( Right.Working_ReinforcementsOnly_ContentsStrength );
            } );

            FInt result = FInt.Zero;
            while ( budget > FInt.Zero && strengthPresent < strengthCap && entitiesWeCanReinforce.Count > 0 )
            {
                int index = 0;
                switch(reinforcementType)
                {
                    case ReinforcementType.Turret:
                    case ReinforcementType.Shield:
                        index = Context.QualityRandom.Next( 0, entitiesWeCanReinforce.Count );
                        break;
                }
                GameEntity entityToReinforce = entitiesWeCanReinforce[index];
                GameEntityTypeData typeToBuy = bag.PickRandomItemAndReplace( Context.QualityRandom );
                budget -= typeToBuy.BalanceStats.StrengthPerSquad;
                result += typeToBuy.BalanceStats.StrengthPerSquad;
                strengthPresent += typeToBuy.BalanceStats.StrengthPerSquad;

                switch ( reinforcementType )
                {
                    case ReinforcementType.Turret:
                    case ReinforcementType.Shield:
                        {
                            int minDistance = ( ExternalConstants.Instance.Balance_AverageGravWellRadius * FInt.FromParts( 0, 050 ) ).IntValue;
                            int maxDistance = ( ExternalConstants.Instance.Balance_AverageGravWellRadius * FInt.FromParts( 0, 150 ) ).IntValue;
                            ArcenPoint point = planet.Combat.GetSafePlacementPoint( Context, typeToBuy, entityToReinforce.WorldLocation, minDistance, maxDistance );
                            if ( point == ArcenPoint.ZeroZeroPoint )
                                continue;

                            result += typeToBuy.BalanceStats.StrengthPerSquad;

                            GameEntity newEntity = GameEntity.CreateNew( planet.Combat.GetSideForWorldSide( side ), typeToBuy, point, Context );
                            newEntity.EntitySpecificOrders.Behavior = EntityBehaviorType.Stationary;
                        }
                        break;
                    case ReinforcementType.FleetShip:
                        {
                            entityToReinforce.ChangeContents( typeToBuy, 1 );
                            entityToReinforce.Working_ReinforcementsOnly_ContentsStrength += typeToBuy.BalanceStats.StrengthPerSquad;

                            for ( int i = 1; i < entitiesWeCanReinforce.Count; i++ )
                            {
                                GameEntity otherReinforceable = entitiesWeCanReinforce[i];
                                if ( entityToReinforce.Working_ReinforcementsOnly_ContentsStrength <= otherReinforceable.Working_ReinforcementsOnly_ContentsStrength )
                                    break;
                                entitiesWeCanReinforce[i - 1] = otherReinforceable;
                                entitiesWeCanReinforce[i] = entityToReinforce;
                            }
                        }
                        break;
                }

                if ( atMostOnePerReinforceable )
                    entitiesWeCanReinforce.Remove( entityToReinforce );
            }

            return result;
        }
    }
}
