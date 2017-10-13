using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class TargetSorter_Assault : TargetSorter_Base
    {
        public override void DoPreSortLogic( EntitySystem Weapon, List<GameEntity> Targets, ArcenCharacterBuffer TracingBuffer, ArcenSimContext Context )
        {
            base.DoPreSortLogic( Weapon, Targets, TracingBuffer, Context );

            ControlGroup myControlGroup = Weapon.ParentEntity.EntitySpecificOrders.ControlGroup;

            for ( int i = 0; i < Targets.Count; i++ )
            {
                GameEntity target = Targets[i];
                if ( target.GetMatches( EntityRollupType.TractorSource ) && myControlGroup != null )
                {
                    for(int j = 0; j < target.CurrentlyStrongestTractorSourceHittingThese.Count;j++)
                    {
                        GameEntity tractoredUnit = World_AIW2.Instance.GetEntityByID( target.CurrentlyStrongestTractorSourceHittingThese[j] );
                        if ( myControlGroup != tractoredUnit.EntitySpecificOrders.ControlGroup )
                            continue;
                        ExternalData_GroupTargetSorting.Primitives data = target.Get_GroupTargetSorting_Primitives();
                        data.IsCurrentlyTractoringMembersOfMyControlGroup = true;
                        break;
                    }
                }
            }
        }

        public override TargetComparisonResult Compare( GameEntity Left, GameEntity Right )
        {
            TargetComparisonResult val;

            val = CompareValues( Left, Right, true, Left.Get_GroupTargetSorting_Primitives().IsCurrentlyTractoringMembersOfMyControlGroup, Right.Get_GroupTargetSorting_Primitives().IsCurrentlyTractoringMembersOfMyControlGroup, "IsCurrentlyTractoringMembersOfMyControlGroup", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, true, Left.GetMatches( EntityRollupType.ProjectsShield ), Right.GetMatches( EntityRollupType.ProjectsShield ), "ProjectsShield", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, true, Left.GetMatches( EntityRollupType.TractorSource ), Right.GetMatches( EntityRollupType.TractorSource ), "TractorSource", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            return TargetComparisonResult.NoPreference;
        }
    }

    public class TargetSorter_PreAssault : TargetSorter_Base
{
        public override TargetComparisonResult Compare( GameEntity Left, GameEntity Right )
        {
            TargetComparisonResult val;
            
            val = CompareValues( Left, Right, true, Left.GetMatches( EntityRollupType.TractorSource ), Right.GetMatches( EntityRollupType.TractorSource ), "TractorSource", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, true, Left.GetMatches( EntityRollupType.GravitySource ), Right.GetMatches( EntityRollupType.GravitySource ), "GravitySource", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            Balance_Range leftRange = Left.TypeData.Computed_LongestWeaponRange;
            Balance_Range rightRange = Right.TypeData.Computed_LongestWeaponRange;

            val = CompareValues( Left, Right, true, leftRange==SniperRange, rightRange==SniperRange, "SniperRange", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, true, leftRange == LongRange, rightRange == LongRange, "LongRange", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, true, Left.GetMatches( EntityRollupType.ProjectsShield ), Right.GetMatches( EntityRollupType.ProjectsShield ), "ProjectsShield", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            return TargetComparisonResult.NoPreference;
        }
    }

    public class TargetSorter_Siege : TargetSorter_Base
{
        public override TargetComparisonResult Compare( GameEntity Left, GameEntity Right )
        {
            TargetComparisonResult val;

            Balance_Range leftRange = Left.TypeData.Computed_LongestWeaponRange;
            Balance_Range rightRange = Right.TypeData.Computed_LongestWeaponRange;

            val = CompareValues( Left, Right, true, leftRange == SniperRange, rightRange == SniperRange, "SniperRange", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, true, leftRange == LongRange, rightRange == LongRange, "LongRange", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, true, Left.GetMatches( EntityRollupType.ProjectsShield ), Right.GetMatches( EntityRollupType.ProjectsShield ), "ProjectsShield", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, true, leftRange == MediumRange, rightRange == MediumRange, "MediumRange", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, true, leftRange == ShortRange, rightRange == ShortRange, "ShortRange", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            return TargetComparisonResult.NoPreference;
        }
    }

    public class TargetSorter_Defense : TargetSorter_Base
    {
        public override TargetComparisonResult Compare( GameEntity Left, GameEntity Right )
        {
            TargetComparisonResult val;

            val = CompareValues( Left, Right, true, Left.TypeData.Computed_AllDefensesCountered.Contains( ShieldDefense ), Right.TypeData.Computed_AllDefensesCountered.Contains( ShieldDefense ), "Anti-Shield", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, true, Left.TypeData.Computed_AllDefensesCountered.Contains( StructureDefense ), Right.TypeData.Computed_AllDefensesCountered.Contains( StructureDefense ), "Anti-Structure", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            return TargetComparisonResult.NoPreference;
        }
    }

    public class TargetSorter_Retreat : TargetSorter_Base
    {
        private FInt MyLowestSpeedMultiplier;
        private ArcenPoint MyPrimaryUnitLocation;

        public override void DoPreSortLogic( EntitySystem Weapon, List<GameEntity> Targets, ArcenCharacterBuffer TracingBuffer, ArcenSimContext Context )
        {
            base.DoPreSortLogic( Weapon, Targets, TracingBuffer, Context );

            MyLowestSpeedMultiplier = (FInt)999;
            MyPrimaryUnitLocation = ArcenPoint.ZeroZeroPoint;
            ControlGroup group = Weapon.ParentEntity.EntitySpecificOrders.ControlGroup;
            if ( group != null )
            {
                for ( int i = 0; i < group.EntityIDs.Count; i++ )
                {
                    GameEntity entity = World_AIW2.Instance.GetEntityByID( group.EntityIDs[i] );
                    if ( entity == null )
                        continue;
                    FInt thisMultiplier = entity.TypeData.Balance_Speed.SpeedMultiplier;
                    if ( thisMultiplier <= FInt.Zero )
                        continue;
                    MyLowestSpeedMultiplier = Mat.Min( MyLowestSpeedMultiplier, thisMultiplier );
                }

                GameEntity primaryEntity = group.GetPrimaryEntity();
                if(primaryEntity != null)
                    MyPrimaryUnitLocation = primaryEntity.WorldLocation;
            }
        }

        public override TargetComparisonResult Compare( GameEntity Left, GameEntity Right )
        {
            TargetComparisonResult val;

            FInt leftSpeedMult = Left.TypeData.Balance_Speed.SpeedMultiplier;
            FInt rightSpeedMult = Right.TypeData.Balance_Speed.SpeedMultiplier;
            bool leftCanCatchUs = leftSpeedMult >= MyLowestSpeedMultiplier;
            bool rightCanCatchUs = rightSpeedMult >= MyLowestSpeedMultiplier;
            
            val = CompareValues( Left, Right, true, leftCanCatchUs, rightCanCatchUs, "CanCatchUs", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            if ( leftCanCatchUs )
            {
                val = CompareValues( Left, Right, true, leftSpeedMult, rightSpeedMult, "SpeedMultiplier", TracingBuffer );
                if ( val != TargetComparisonResult.NoPreference ) return val;

                int leftDistance = Left.WorldLocation.GetDistanceTo( MyPrimaryUnitLocation, false );
                int rightDistance = Right.WorldLocation.GetDistanceTo( MyPrimaryUnitLocation, false );
                val = CompareValues( Left, Right, false, leftDistance, rightDistance, "DistanceToMyPrimaryUnit", TracingBuffer );
                if ( val != TargetComparisonResult.NoPreference ) return val;
            }

            val = CompareValues( Left, Right, true, Left.TypeData.Computed_AllDefensesCountered.Contains( ShieldDefense ), Right.TypeData.Computed_AllDefensesCountered.Contains( ShieldDefense ), "Anti-Shield", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, true, Left.TypeData.Computed_AllDefensesCountered.Contains( StructureDefense ), Right.TypeData.Computed_AllDefensesCountered.Contains( StructureDefense ), "Anti-Structure", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            return TargetComparisonResult.NoPreference;
        }
    }
}
