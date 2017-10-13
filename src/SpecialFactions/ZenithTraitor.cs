using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class SpecialFaction_ZenithTraitor : ISpecialFactionImplementation
    {
        public static SpecialFaction_ZenithTraitor Instance;
        public SpecialFaction_ZenithTraitor() { Instance = this; }

        public static readonly string TRADER_TAG = "ZenithTrader";

        public void SetStartingSideRelationships( WorldSide side )
        {
            for ( int i = 0; i < World_AIW2.Instance.Sides.Count; i++ )
            {
                WorldSide otherSide = World_AIW2.Instance.Sides[i];
                if ( side == otherSide )
                    continue;
                switch ( otherSide.Type )
                {
                    case WorldSideType.Player:
                    case WorldSideType.AI:
                        side.MakeFriendlyTo( otherSide );
                        otherSide.MakeFriendlyTo( side );
                        break;
                    case WorldSideType.SpecialFaction:
                        if ( otherSide.SpecialFactionData != null && otherSide.SpecialFactionData.Implementation == SpecialFaction_Devourer.Instance )
                        {
                            side.MakeHostileTo( otherSide );
                            otherSide.MakeHostileTo( side );
                        }
                        else
                        {
                            side.MakeFriendlyTo( otherSide );
                            otherSide.MakeFriendlyTo( side );
                        }
                        break;
                }
            }
        }

        public ArcenEnumIndexedArray_AIBudgetType<FInt> GetSpendingRatios( WorldSide side )
        {
            ArcenEnumIndexedArray_AIBudgetType<FInt> result = new ArcenEnumIndexedArray_AIBudgetType<FInt>();

            result[AIBudgetType.Reinforcement] = FInt.One;

            return result;
        }

        public bool GetShouldAttackNormallyExcludedTarget( WorldSide side, GameEntity Target )
        {
            return false;
        }

        public void SeedStartingEntities( WorldSide side, Galaxy galaxy, ArcenSimContext Context, MapTypeData mapType )
        {
            galaxy.Mapgen_SeedSpecialEntities( Context, side, TRADER_TAG, 1 );
        }

        public void DoLongRangePlanning( WorldSide side, ArcenLongTermPlanningContext Context )
        {
            Galaxy galaxy = World_AIW2.Instance.SetOfGalaxies.Galaxies[0];

            side.Entities.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity entity )
            {
                if ( entity.LongRangePlanningData == null )
                    return DelReturn.Continue; // if created after the start of this planning cycle, skip

                if ( !entity.TypeData.GetHasTag( TRADER_TAG ) )
                    return DelReturn.Continue; // if not the main unit, skip (shouldn't happen, but if somebody mods in a reclamator gun for the thing, etc)

                if ( entity.LongRangePlanningData.FinalDestinationPlanetIndex != -1 &&
                     entity.LongRangePlanningData.FinalDestinationPlanetIndex != entity.LongRangePlanningData.CurrentPlanetIndex )
                    return DelReturn.Continue; // if heading somewhere else, skip

                Planet planet = World_AIW2.Instance.GetPlanetByIndex( entity.LongRangePlanningData.CurrentPlanetIndex );

                List<GameEntity> threatShipsNotAssignedElsewhere = new List<GameEntity>();
                threatShipsNotAssignedElsewhere.Add( entity );
                // it's not really a raid, but the logic of "pick somewhere random and go there" suffices
                FactionUtilityMethods.Helper_SendThreatOnRaid( threatShipsNotAssignedElsewhere, side, galaxy, planet, true, Context );

                return DelReturn.Continue;
            } );
        }

        public void DoPerSimStepLogic( WorldSide side, ArcenSimContext Context )
        {
        }
    }
}
