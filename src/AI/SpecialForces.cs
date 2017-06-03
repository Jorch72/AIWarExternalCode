using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class AISpecialForcesController_Vanilla : IAISpecialForcesController
    {
        private ArcenRandomDrawBag<GameEntityTypeData> bag = new ArcenRandomDrawBag<GameEntityTypeData>();

        public GameEntityTypeData GetNextFleetShipToBuy( ArcenSimContext Context, GameEntity Base, List<GameEntity> CurrentFleet, List<BuildMenu> Menus )
        {
            return InnerPickNextBuy( Context, Menus );
        }

        public GameEntityTypeData GetNextGuardianToBuy( ArcenSimContext Context, GameEntity Base, List<GameEntity> CurrentFleet, List<BuildMenu> Menus )
        {
            return InnerPickNextBuy( Context, Menus );
        }

        private GameEntityTypeData InnerPickNextBuy( ArcenSimContext Context, List<BuildMenu> Menus )
        {
            bag.Clear();
            for ( int i = 0; i < Menus.Count; i++ )
            {
                BuildMenu menu = Menus[i];
                for ( int j = 0; j < menu.List.Count; j++ )
                {
                    int timesToAdd = 0;
                    GameEntityTypeData buyableType = menu.List[j];
                    if ( buyableType.Balance_MarkLevel.RequiredAIPLevel > World_AIW2.Instance.AIProgress_Effective )
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
            GameEntityTypeData result = bag.PickRandomItemAndReplace( Context.QualityRandom );
            bag.Clear();
            return result;
        }

        private static FInt bestTargetFound_Danger;
        private static int bestTargetFound_Index;
        public Planet GetCurrentTargetPlanet( ArcenSimContext Context, GameEntity hideout, List<GameEntity> hideoutFleet )
        {
            #region Tracing
            bool tracing = SpecialForcesPlanning.tracing;
            ArcenCharacterBuffer tracingBuffer = SpecialForcesPlanning.tracingBuffer;
            #endregion
            Planet hideoutPlanet = World_AIW2.Instance.GetPlanetByIndex( hideout.LongRangePlanningData.CurrentPlanetIndex );
            Planet currentTargetPlanet = World_AIW2.Instance.GetPlanetByIndex( hideout.LongRangePlanningData.SpecialTargetPlanetIndex );

            FInt hideoutFleetStrength = FInt.Zero;
            hideoutFleet.Clear();
            for(int i = 0; i < hideoutFleet.Count;i++ )
            {
                GameEntity entity = hideoutFleet[i];
                if ( entity.LongRangePlanningData.CoordinatorID != hideout.PrimaryKeyID )
                    continue;
                hideoutFleet.Add( entity );
                hideoutFleetStrength += entity.TypeData.BalanceStats.StrengthPerSquad + entity.LongRangePlanningData.StrengthOfContents;
            }
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "SpecialForcesRouting considering hideout on " ).Add( hideoutPlanet.Name ).Add( "; fleet strength = " ).Add( hideoutFleetStrength.ReadableString );
            #endregion
            bestTargetFound_Index = -1;
            bestTargetFound_Danger = FInt.Zero;
            hideoutPlanet.DoForPlanetsWithinXHops( Context, 3, delegate ( Planet planet, int Distance )
            {
                FInt danger = planet.LongRangePlanningData.HumanTotalStrength - planet.LongRangePlanningData.AITotalStrength;
                if ( danger < FInt.Zero && -danger < planet.LongRangePlanningData.AITotalStrength / 2 )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "rejecting target " ).Add( planet.Name ).Add( " because danger too low: " ).Add( danger.ReadableString );
                    #endregion
                    return DelReturn.Continue;
                }
                if ( bestTargetFound_Index >= 0 && bestTargetFound_Danger >= danger )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "rejecting target " ).Add( planet.Name ).Add( " because danger lower than current best target: " ).Add( danger.ReadableString );
                    #endregion
                    return DelReturn.Continue;
                }
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "current best target = " ).Add( planet.Name ).Add( "; danger: " ).Add( danger.ReadableString );
                #endregion
                bestTargetFound_Index = planet.PlanetIndex;
                bestTargetFound_Danger = danger;
                return DelReturn.Continue;
            },
             delegate ( Planet planet )
             {
                 if ( planet.LongRangePlanningData.ControllingSide.Type != hideout.LongRangePlanningData.Side.WorldSide.Type )
                 {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Refusing to flood into " ).Add( planet.Name ).Add( " because not controlled by same side" );
                    #endregion
                    return PropogationEvaluation.No;
                 }
                 FInt danger = planet.LongRangePlanningData.HumanTotalStrength - planet.LongRangePlanningData.AITotalStrength;
                 if ( danger >= hideoutFleetStrength * 2 )
                 {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Refusing to flood into " ).Add( planet.Name ).Add( " because too much danger: " ).Add( danger.ReadableString );
                    #endregion
                    return PropogationEvaluation.No;
                 }
                 if ( danger >= hideoutFleetStrength * 1 )
                 {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Refusing to flood through " ).Add( planet.Name ).Add( " because too much danger: " ).Add( danger.ReadableString );
                    #endregion
                    return PropogationEvaluation.SelfButNotNeighbors;
                 }
                 return PropogationEvaluation.Yes;
             } );

            FInt currentTargetPlanetDanger = currentTargetPlanet.LongRangePlanningData.HumanTotalStrength - currentTargetPlanet.LongRangePlanningData.AITotalStrength;
            if ( bestTargetFound_Index < 0 || bestTargetFound_Danger <= ( currentTargetPlanetDanger * 2 ) )
            {
                bestTargetFound_Index = currentTargetPlanet.PlanetIndex;
                bestTargetFound_Danger = currentTargetPlanetDanger;
            }

            return World_AIW2.Instance.GetPlanetByIndex( bestTargetFound_Index );
        }
    }
}