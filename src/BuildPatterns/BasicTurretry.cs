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
                    if ( !EntityRollupType.Combatants.Matches( entityData ) ) //&& !EntityRollupType.ProjectsShield.Matches( entityData )
                        continue;
                    if ( BuildingEntity.Side.GetCanBuildAnother( entityData ) != ArcenRejectionReason.Unknown )
                        continue;
                    turretTypes.Add( entityData );
                }
            }

            if ( turretTypes.Count <= 0 )
                return;

            int powerBudget = BuildingEntity.Side.NetPower;

            int budgetPerType = powerBudget / turretTypes.Count;

            ArcenPoint formationCenterPoint = BuildingEntity.WorldLocation;
            
            turretTypes.Sort( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
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

            int innerRingDistance = BuildingEntity.TypeData.Radius * 2;
            int outerRingDistance = ( ExternalConstants.Instance.Balance_AverageGravWellRadius * FInt.FromParts( 0, 100 ) ).IntValue;
            
            int distanceBetweenRings = ( outerRingDistance - innerRingDistance ) / turretTypes.Count;

            for ( int i = 0; i < turretTypes.Count; i++ )
            {
                GameEntityTypeData turretType = turretTypes[i];
                int numberToPlace = budgetPerType / turretType.BalanceStats.SquadPowerConsumption;
                numberToPlace = Math.Min( BuildingEntity.Side.GetRemainingCap( turretType ), numberToPlace );
                if ( numberToPlace <= 0 )
                    continue;
                int ringDistance = innerRingDistance + ( distanceBetweenRings * i );
                AngleDegrees startingAngle = AngleDegrees.Create( (FInt)Context.QualityRandom.Next( 0, AngleDegrees.MaxValue.IntValue ) );
                AngleDegrees angleChangePerItem = AngleDegrees.MaxAngle / numberToPlace;
                for ( int j = 0; j < numberToPlace; j++ )
                {
                    if ( BuildingEntity.Side.GetCanBuildAnother( turretType ) != ArcenRejectionReason.Unknown )
                        break;
                    AngleDegrees angle = startingAngle + ( angleChangePerItem * j );
                    ArcenPoint point = formationCenterPoint.GetPointAtAngleAndDistance( angle, ringDistance );
                    point = BuildingEntity.Combat.GetSafePlacementPoint( Context, turretType, point, 0, distanceBetweenRings / 2 );
                    if ( point == ArcenPoint.ZeroZeroPoint )
                        continue;
                    GameEntity newEntity = GameEntity.CreateNew( BuildingEntity.Side, turretType, point, Context );
                    newEntity.SelfBuildingMetalRemaining = (FInt)turretType.BalanceStats.SquadMetalCost;
                    powerBudget -= turretType.BalanceStats.SquadPowerConsumption;
                }
            }
            
            // fill in the corners of the budget
            turretTypes.Sort( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
             {
                 return Right.BalanceStats.SquadPowerConsumption.CompareTo( Left.BalanceStats.SquadPowerConsumption );
             } );

            bool checkAgain = true;
            while ( powerBudget > 0 && checkAgain )
            {
                checkAgain = false;
                for ( int i = 0; i < turretTypes.Count; i++ )
                {
                    GameEntityTypeData turretType = turretTypes[i];
                    if ( powerBudget < turretType.BalanceStats.SquadPowerConsumption )
                        continue;
                    if ( BuildingEntity.Side.GetCanBuildAnother( turretType ) != ArcenRejectionReason.Unknown )
                        continue;
                    ArcenPoint point = BuildingEntity.Combat.GetSafePlacementPoint( Context, turretType, formationCenterPoint, innerRingDistance, outerRingDistance );
                    if ( point == ArcenPoint.ZeroZeroPoint )
                        continue;
                    GameEntity newEntity = GameEntity.CreateNew( BuildingEntity.Side, turretType, point, Context );
                    newEntity.SelfBuildingMetalRemaining = (FInt)turretType.BalanceStats.SquadMetalCost;
                    powerBudget -= turretType.BalanceStats.SquadPowerConsumption;
                    checkAgain = true;
                }
            }
        }
    }
}
