using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public abstract class TargetSorterBase : IWeaponTargetSorter
    {
        protected static bool tracing;
        protected static ArcenCharacterBuffer TracingBuffer;

        public void Sort( EntitySystem Weapon, List<GameEntity> Targets, ArcenCharacterBuffer TracingBuffer )
        {
            tracing = TracingBuffer != null;
            TargetSorterBase.TracingBuffer = TracingBuffer;

            if ( tracing ) TracingBuffer.Add( "FindTarget:" ).Add( Weapon.ParentEntity.TypeData.InternalName ).Add( " : " ).Add( Weapon.TypeData.InternalName );

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

            Comparison<GameEntity> comparisonDelegate = DelegateHelper_FindTarget_Comparison;
            GameEntity champion = Targets[0];
            if ( tracing ) TracingBuffer.Add( "\n\t" ).Add( "starting with first item: " ).Add( champion.TypeData.InternalName );
            int championIndex = 0;
            for ( int i = 1; i < Targets.Count; i++ )
            {
                GameEntity challenger = Targets[i];
                if ( comparisonDelegate( champion, challenger ) <= 0 )
                    continue;
                champion = challenger;
                championIndex = i;
            }
            if ( championIndex != 0 )
            {
                GameEntity temp = Targets[0];
                Targets[0] = champion;
                Targets[championIndex] = temp;
            }
            //this.WorkingEntityList_GetTarget.Sort( comparisonDelegate );
            //GameEntity result = this.WorkingEntityList_GetTarget[0];

            //if ( debugging )
            //{
            //    for ( int i = 0; i < this.WorkingEntityList_GetTarget.Count; i++ )
            //    {
            //        GameEntity entity = this.WorkingEntityList_GetTarget[i];
            //        debugBuffer.Add( "\n" )
            //            .Add( i )
            //            .Add( ":" )
            //            .Add( entity.TypeData.InternalName )
            //            .Add( "\t" )
            //            .Add( entity.Working_FindTargetOnly_DebugBuffer.ToString() );
            //    }
            //}
        }

        protected abstract void AssignShotsToKillData( EntitySystem Weapon, GameEntity entity );

        protected abstract int DelegateHelper_FindTarget_Comparison( GameEntity Left, GameEntity Right );

        protected static Int32 CompareValues( GameEntity Left, GameEntity Right, bool SortDescending, Int32 leftValue, Int32 rightValue, String fieldName, ArcenCharacterBuffer debugBuffer )
        {
            int val = leftValue.CompareTo( rightValue );
            if ( val == 0 )
                return 0;
            bool leftWon = SortDescending ? val > 0 : val < 0;
            if ( val != 0 && debugBuffer != null )
                LogCompareValues( debugBuffer, Left, Right, fieldName, leftWon, leftValue.ToString(), rightValue.ToString() );
            return leftWon ? -1 : 1;
        }

        protected static Int32 CompareValues( GameEntity Left, GameEntity Right, bool SortDescending, FInt leftValue, FInt rightValue, String fieldName, ArcenCharacterBuffer debugBuffer )
        {
            int val = leftValue.CompareTo( rightValue );
            if ( val == 0 )
                return 0;
            bool leftWon = SortDescending ? val > 0 : val < 0;
            if ( val != 0 && debugBuffer != null )
                LogCompareValues( debugBuffer, Left, Right, fieldName, leftWon, leftValue.ReadableString, rightValue.ReadableString );
            return leftWon ? -1 : 1;
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

    public class StandardTargetSorter : TargetSorterBase
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

        protected override int DelegateHelper_FindTarget_Comparison( GameEntity Left, GameEntity Right )
        {
            int val;

            val = CompareValues( Left, Right, false, Left.Working_FindTargetOnly_ShotsToKill, Right.Working_FindTargetOnly_ShotsToKill, "ShotsToKill", TracingBuffer );
            if ( val != 0 ) return val;

            val = CompareValues( Left, Right, true, Left.TypeData.BalanceStats.StrengthPerShip, Right.TypeData.BalanceStats.StrengthPerShip, "StrengthPerShip", TracingBuffer );
            if ( val != 0 ) return val;

            val = CompareValues( Left, Right, false, Left.Working_FindTargetOnly_Distance, Right.Working_FindTargetOnly_Distance, "Distance", TracingBuffer );
            if ( val != 0 ) return val;

            if ( tracing ) Left.Working_FindTargetOnly_DebugBuffer.Add( "\n\t" ).Add( "no preference against " ).Add( Right.TypeData.InternalName );

            return 0;
        }
    }

    public class EngineWreckerTargetSorter : TargetSorterBase
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

        protected override int DelegateHelper_FindTarget_Comparison( GameEntity Left, GameEntity Right )
        {
            int val;

            val = CompareValues( Left, Right, false, Left.Working_FindTargetOnly_ShotsToKill, Right.Working_FindTargetOnly_ShotsToKill, "ShotsToKill", TracingBuffer );
            if ( val != 0 ) return val;

            val = CompareValues( Left, Right, true, Left.TypeData.BalanceStats.StrengthPerSquad, Right.TypeData.BalanceStats.StrengthPerSquad, "StrengthPerShip", TracingBuffer );
            if ( val != 0 ) return val;

            val = CompareValues( Left, Right, false, Left.Working_FindTargetOnly_Distance, Right.Working_FindTargetOnly_Distance, "Distance", TracingBuffer );
            if ( val != 0 ) return val;

            if ( tracing ) Left.Working_FindTargetOnly_DebugBuffer.Add( "\n\t" ).Add( "no preference against " ).Add( Right.TypeData.InternalName );

            return 0;
        }
    }

    public class DoNothingTargetSorter : IWeaponTargetSorter
    {
        public void Sort( EntitySystem Weapon, List<GameEntity> Targets, ArcenCharacterBuffer TracingBuffer )
        {
            return;
        }
    }
}
