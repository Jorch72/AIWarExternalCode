using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class BuildPattern_BasicTurretry : IBuildPatternController
    {
        public String Name
        {
            get
            {
                return "Basic Turretry";
            }
        }

        public void Execute( ArcenSimContext Context, GameEntity BuildingEntity )
        {
            List<GameEntityTypeData> turretTypes = new List<GameEntityTypeData>();
            List<GameEntityTypeData> shieldTypes = new List<GameEntityTypeData>();

            for ( int menuIndex = 0; menuIndex < BuildingEntity.TypeData.BuildMenus.Count; menuIndex++ )
            {
                BuildMenu menu = BuildingEntity.TypeData.BuildMenus[menuIndex];
                for ( int i = 0; i < menu.List.Count; i++ )
                {
                    GameEntityTypeData entityData = menu.List[i];
                    if ( entityData.Balance_FuelCost.FuelMultiplier > 0 )
                        continue;
                    if ( entityData.Balance_PowerCost.PowerMultiplier <= 0 )
                        continue;
                    if ( !entityData.CapIsPerPlanet )
                        continue;
                    List<GameEntityTypeData> listToAddTo = null;
                    if ( entityData.RollupLookup[EntityRollupType.Combatants] )
                        listToAddTo = turretTypes;
                    else if ( entityData.RollupLookup[EntityRollupType.ProjectsShield] )
                        listToAddTo = shieldTypes;
                    if ( listToAddTo == null )
                        continue;
                    ArcenRejectionReason rejectionReason = BuildingEntity.Side.GetCanBuildAnother( entityData );
                    if ( rejectionReason != ArcenRejectionReason.Unknown )
                        continue;
                    listToAddTo.Add( entityData );
                }
            }

            int remainingBudget = BuildingEntity.Side.NetPower;

            remainingBudget -= SpendBudgetOnItemsInList( Context, BuildingEntity.Side, BuildingEntity.WorldLocation, BuildingEntity.TypeData.Radius * 2, shieldTypes, ( remainingBudget * 25 ) / 100 );
            remainingBudget -= SpendBudgetOnItemsInList( Context, BuildingEntity.Side, BuildingEntity.WorldLocation, BuildingEntity.TypeData.Radius * 2, turretTypes, remainingBudget );
        }

        private static int SpendBudgetOnItemsInList( ArcenSimContext Context, CombatSide Side, ArcenPoint CenterLocation, int MinimumDistanceFromCenter, List<GameEntityTypeData> listToBuyFrom, int budget )
        {
            if ( listToBuyFrom.Count <= 0 )
                return 0;
            if ( budget <= 0 )
                return 0;

            int remainingBudget = budget;

            int budgetPerType = remainingBudget / listToBuyFrom.Count;

            ArcenPoint formationCenterPoint = CenterLocation;

            listToBuyFrom.Sort( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
            {
                int leftValue = 0;
                if ( Left.SystemEntries.Count > 0 )
                {
                    SystemEntry systemEntry = Left.SystemEntries[0];
                    if ( systemEntry.SubEntries.Count > 0 )
                        leftValue = systemEntry.SubEntries[0].BalanceStats.Range;
                }
                int rightValue = 0;
                if ( Right.SystemEntries.Count > 0 )
                {
                    SystemEntry systemEntry = Right.SystemEntries[0];
                    if ( systemEntry.SubEntries.Count > 0 )
                        rightValue = systemEntry.SubEntries[0].BalanceStats.Range;
                }
                return rightValue.CompareTo( leftValue );
            } );

            int innerRingDistance = MinimumDistanceFromCenter;
            int outerRingDistance = ( ExternalConstants.Instance.Balance_AverageGravWellRadius * FInt.FromParts( 0, 100 ) ).IntValue;

            int distanceBetweenRings = ( outerRingDistance - innerRingDistance ) / listToBuyFrom.Count;

            for ( int i = 0; i < listToBuyFrom.Count; i++ )
            {
                GameEntityTypeData entityType = listToBuyFrom[i];
                int numberToPlace = budgetPerType / entityType.BalanceStats.SquadPowerConsumption;
                numberToPlace = Math.Min( Side.GetRemainingCap( entityType ), numberToPlace );
                if ( numberToPlace <= 0 )
                    continue;
                int ringDistance = innerRingDistance + ( distanceBetweenRings * i );
                AngleDegrees startingAngle = AngleDegrees.Create( (FInt)Context.QualityRandom.Next( 0, AngleDegrees.MaxValue.IntValue ) );
                AngleDegrees angleChangePerItem = AngleDegrees.MaxAngle / numberToPlace;
                for ( int j = 0; j < numberToPlace; j++ )
                {
                    if ( Side.GetCanBuildAnother( entityType ) != ArcenRejectionReason.Unknown )
                        break;
                    AngleDegrees angle = startingAngle + ( angleChangePerItem * j );
                    ArcenPoint point = formationCenterPoint.GetPointAtAngleAndDistance( angle, ringDistance );
                    point = Side.Combat.GetSafePlacementPoint( Context, entityType, point, 0, distanceBetweenRings / 2 );
                    if ( point == ArcenPoint.ZeroZeroPoint )
                        continue;
                    GameEntity newEntity = GameEntity.CreateNew( Side, entityType, point, Context );
                    newEntity.SelfBuildingMetalRemaining = (FInt)entityType.BalanceStats.SquadMetalCost;
                    remainingBudget -= entityType.BalanceStats.SquadPowerConsumption;
                }
            }

            // fill in the corners of the budget
            listToBuyFrom.Sort( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
            {
                return Right.BalanceStats.SquadPowerConsumption.CompareTo( Left.BalanceStats.SquadPowerConsumption );
            } );

            bool checkAgain = true;
            while ( remainingBudget > 0 && checkAgain )
            {
                checkAgain = false;
                for ( int i = 0; i < listToBuyFrom.Count; i++ )
                {
                    GameEntityTypeData entityType = listToBuyFrom[i];
                    if ( remainingBudget < entityType.BalanceStats.SquadPowerConsumption )
                        continue;
                    if ( Side.GetCanBuildAnother( entityType ) != ArcenRejectionReason.Unknown )
                        continue;
                    ArcenPoint point = Side.Combat.GetSafePlacementPoint( Context, entityType, formationCenterPoint, innerRingDistance, outerRingDistance );
                    if ( point == ArcenPoint.ZeroZeroPoint )
                        continue;
                    GameEntity newEntity = GameEntity.CreateNew( Side, entityType, point, Context );
                    newEntity.SelfBuildingMetalRemaining = (FInt)entityType.BalanceStats.SquadMetalCost;
                    remainingBudget -= entityType.BalanceStats.SquadPowerConsumption;
                    checkAgain = true;
                }
            }

            return budget - remainingBudget;
        }
    }
}
