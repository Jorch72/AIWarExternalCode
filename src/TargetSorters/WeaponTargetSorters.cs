using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public abstract class TargetSorter_Base : ITargetSorterImplementation
    {
        protected static bool tracing;
        protected static ArcenCharacterBuffer TracingBuffer;
        protected static Balance_Range SniperRange;
        protected static Balance_Range LongRange;
        protected static Balance_Range MediumRange;
        protected static Balance_Range ShortRange;
        protected static Balance_Defense ShieldDefense;
        protected static Balance_Defense StructureDefense;
        public bool IsGroupSorter;
        
        public void LoadCustomData( ArcenDynamicTableRow Row )
        {
            CustomDataSet set = Row.GetCustomData( "TargetSorter" );
            this.IsGroupSorter = set.GetBool( "is_group_sorter" );
        }

        public virtual void DoPreSortLogic( EntitySystem Weapon, List<GameEntity> Targets, ArcenCharacterBuffer TracingBuffer, ArcenSimContext Context )
        {
            tracing = TracingBuffer != null;
            TargetSorter_Base.TracingBuffer = TracingBuffer;

            if ( tracing ) TracingBuffer.Add( "FindTarget:" ).Add( Weapon.ParentEntity.TypeData.InternalName ).Add( " : " ).Add( Weapon.TypeData.InternalName );

            if ( SniperRange == null )
            {
                SniperRange = Balance_RangeTable.Instance.GetRowByName( "Sniper", false, null );
                LongRange = Balance_RangeTable.Instance.GetRowByName( "Long", false, null );
                MediumRange = Balance_RangeTable.Instance.GetRowByName( "Medium", false, null );
                ShortRange = Balance_RangeTable.Instance.GetRowByName( "Short", false, null );

                ShieldDefense = Balance_DefenseTable.Instance.GetRowByName( "Shields", false, null );
                StructureDefense = Balance_DefenseTable.Instance.GetRowByName( "Structure", false, null );
            }
        }

        public virtual void DoPostSortLogic( EntitySystem Weapon, List<GameEntity> Targets, ArcenCharacterBuffer TracingBuffer, ArcenSimContext Context )
        {
            tracing = false;
            TracingBuffer = null;
        }

        public abstract TargetComparisonResult Compare( GameEntity Left, GameEntity Right );

        protected static TargetComparisonResult CompareValues( GameEntity Left, GameEntity Right, bool SortDescending, bool leftValue, bool rightValue, String fieldName, ArcenCharacterBuffer debugBuffer )
        {
            if ( leftValue == rightValue )
                return TargetComparisonResult.NoPreference;
            bool leftWon = SortDescending ? leftValue : !leftValue;
            if ( debugBuffer != null )
                LogCompareValues( debugBuffer, Left, Right, fieldName, leftWon, leftValue.ToString(), rightValue.ToString() );
            return leftWon ? TargetComparisonResult.PreferLeft : TargetComparisonResult.PreferRight;
        }

        protected static TargetComparisonResult CompareValues( GameEntity Left, GameEntity Right, bool SortDescending, Int32 leftValue, Int32 rightValue, String fieldName, ArcenCharacterBuffer debugBuffer )
        {
            int val = leftValue.CompareTo( rightValue );
            if ( val == 0 )
                return TargetComparisonResult.NoPreference;
            bool leftWon = SortDescending ? val > 0 : val < 0;
            if ( debugBuffer != null )
                LogCompareValues( debugBuffer, Left, Right, fieldName, leftWon, leftValue.ToString(), rightValue.ToString() );
            return leftWon ? TargetComparisonResult.PreferLeft : TargetComparisonResult.PreferRight;
        }

        protected static TargetComparisonResult CompareValues( GameEntity Left, GameEntity Right, bool SortDescending, FInt leftValue, FInt rightValue, String fieldName, ArcenCharacterBuffer debugBuffer )
        {
            int val = leftValue.CompareTo( rightValue );
            if ( val == 0 )
                return TargetComparisonResult.NoPreference;
            bool leftWon = SortDescending ? val > 0 : val < 0;
            if ( debugBuffer != null )
                LogCompareValues( debugBuffer, Left, Right, fieldName, leftWon, leftValue.ReadableString, rightValue.ReadableString );
            return leftWon ? TargetComparisonResult.PreferLeft : TargetComparisonResult.PreferRight;
        }

        protected static void LogCompareValues( ArcenCharacterBuffer debugBuffer, GameEntity Left, GameEntity Right, String fieldName, bool LeftWon, String leftValueString, String rightValueString )
        {
            GameEntity winner = LeftWon ? Left : Right;
            GameEntity loser = LeftWon ? Right : Left;
            ArcenCharacterBuffer buffer = debugBuffer;
            buffer.Add( "\n\t" )
                .Add( "preferring " )
                .Add( winner.TypeData.InternalName )
                .Add( " to " )
                .Add( loser.TypeData.InternalName )
                .Add( " because " )
                .Add( fieldName )
                .Add( " is " )
                .Add( LeftWon ? leftValueString : rightValueString )
                .Add( " to " )
                .Add( LeftWon ? rightValueString : leftValueString );
        }
    }

    public abstract class TargetSorter_StandardBase : TargetSorter_Base
    {
        public override void DoPreSortLogic( EntitySystem Weapon, List<GameEntity> Targets, ArcenCharacterBuffer TracingBuffer, ArcenSimContext Context )
        {
            base.DoPreSortLogic( Weapon, Targets, TracingBuffer, Context );

            for ( int i = 0; i < Targets.Count; i++ )
            {
                GameEntity entity = Targets[i];
                entity.Working_FindTargetOnly_Distance = Weapon.ParentEntity.GetDistanceTo( entity, false );
                AssignShotsToKillData( Weapon, entity );
                if ( tracing )
                {
                    if ( entity.Working_FindTargetOnly_DebugBuffer == null )
                        entity.Working_FindTargetOnly_DebugBuffer = new ArcenCharacterBuffer();
                    entity.Working_FindTargetOnly_DebugBuffer.Clear();
                }
            }
        }

        protected abstract void AssignShotsToKillData( EntitySystem Weapon, GameEntity entity );
    }

    public class TargetSorter_Standard : TargetSorter_StandardBase
    {
        protected override void AssignShotsToKillData( EntitySystem Weapon, GameEntity entity )
        {
            int damageIWouldDo = Weapon.GetAttackPowerAgainst( entity );
            if ( damageIWouldDo <= 0 )
                entity.Working_FindTargetOnly_ShotsToKill = 9999;
            else
            {
                entity.Working_FindTargetOnly_ShotsToKill = ( ( (FInt)entity.GetCurrentHullPoints() + (FInt)entity.EstimatedTotalShieldPointsOfProtectors ) / damageIWouldDo ).GetNearestIntPreferringHigher();
                entity.Working_FindTargetOnly_ShotsToKill = Math.Max( 1, entity.Working_FindTargetOnly_ShotsToKill );
            }
        }

        public override TargetComparisonResult Compare( GameEntity Left, GameEntity Right )
        {
            TargetComparisonResult val;

            val = CompareValues( Left, Right, false, Left.Working_FindTargetOnly_ShotsToKill, Right.Working_FindTargetOnly_ShotsToKill, "ShotsToKill", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, true, Left.TypeData.BalanceStats.StrengthPerShip, Right.TypeData.BalanceStats.StrengthPerShip, "StrengthPerShip", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, false, Left.Working_FindTargetOnly_Distance, Right.Working_FindTargetOnly_Distance, "Distance", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            if ( tracing ) Left.Working_FindTargetOnly_DebugBuffer.Add( "\n\t" ).Add( "no preference against " ).Add( Right.TypeData.InternalName );

            return TargetComparisonResult.NoPreference;
        }
    }

    public class TargetSorter_EngineWrecker : TargetSorter_StandardBase
    {
        protected override void AssignShotsToKillData( EntitySystem Weapon, GameEntity entity )
        {
            int damageIWouldDo = Weapon.GetEngineAttackPowerAgainst( entity );
            if ( damageIWouldDo <= 0 )
                entity.Working_FindTargetOnly_ShotsToKill = 9999;
            else
            {
                entity.Working_FindTargetOnly_ShotsToKill = ( (FInt)entity.GetCurrentEngineHealth() / damageIWouldDo ).GetNearestIntPreferringHigher();
                entity.Working_FindTargetOnly_ShotsToKill = Math.Max( 1, entity.Working_FindTargetOnly_ShotsToKill );
            }
        }

        public override TargetComparisonResult Compare( GameEntity Left, GameEntity Right )
        {
            TargetComparisonResult val;

            val = CompareValues( Left, Right, false, Left.Working_FindTargetOnly_ShotsToKill, Right.Working_FindTargetOnly_ShotsToKill, "ShotsToKill", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, true, Left.TypeData.BalanceStats.StrengthPerSquad, Right.TypeData.BalanceStats.StrengthPerSquad, "StrengthPerShip", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            val = CompareValues( Left, Right, false, Left.Working_FindTargetOnly_Distance, Right.Working_FindTargetOnly_Distance, "Distance", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            if ( tracing ) Left.Working_FindTargetOnly_DebugBuffer.Add( "\n\t" ).Add( "no preference against " ).Add( Right.TypeData.InternalName );

            return TargetComparisonResult.NoPreference;
        }
    }

    public class TargetSorter_Melee : TargetSorter_StandardBase
    {
        protected override void AssignShotsToKillData( EntitySystem Weapon, GameEntity entity )
        {
            int damageIWouldDo = Weapon.GetAttackPowerAgainst( entity );
            if ( damageIWouldDo <= 0 )
                entity.Working_FindTargetOnly_ShotsToKill = 9999;
            else
            {
                entity.Working_FindTargetOnly_ShotsToKill = ( ( (FInt)entity.GetCurrentHullPoints() + (FInt)entity.EstimatedTotalShieldPointsOfProtectors ) / damageIWouldDo ).GetNearestIntPreferringHigher();
                entity.Working_FindTargetOnly_ShotsToKill = Math.Max( 1, entity.Working_FindTargetOnly_ShotsToKill );
            }
        }

        public override TargetComparisonResult Compare( GameEntity Left, GameEntity Right )
        {
            TargetComparisonResult val;

            val = CompareValues( Left, Right, false, Left.Working_FindTargetOnly_Distance, Right.Working_FindTargetOnly_Distance, "Distance", TracingBuffer );
            if ( val != TargetComparisonResult.NoPreference ) return val;

            if ( tracing ) Left.Working_FindTargetOnly_DebugBuffer.Add( "\n\t" ).Add( "no preference against " ).Add( Right.TypeData.InternalName );

            return TargetComparisonResult.NoPreference;
        }
    }

    public class TargetSorter_DoNothing : ITargetSorterImplementation
    {
        public void LoadCustomData( ArcenDynamicTableRow Row ) { }

        public TargetComparisonResult Compare( GameEntity Left, GameEntity Right ) { return TargetComparisonResult.NoPreference; }

        public void DoPostSortLogic( EntitySystem Weapon, List<GameEntity> Target, ArcenCharacterBuffer TracingBuffer, ArcenSimContext Context ) { }

        public void DoPreSortLogic( EntitySystem Weapon, List<GameEntity> Target, ArcenCharacterBuffer TracingBuffer, ArcenSimContext Context ) { }
    }
}
