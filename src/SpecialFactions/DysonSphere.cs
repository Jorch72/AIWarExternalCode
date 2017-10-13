using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class SpecialFaction_DysonSphere : ISpecialFactionImplementation
    {
        public static SpecialFaction_DysonSphere Instance;
        public SpecialFaction_DysonSphere() { Instance = this; }

        public static readonly string DYSON_SPHERE_TAG = "Dyson";

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
                    case WorldSideType.SpecialFaction:
                        side.MakeHostileTo( otherSide );
                        otherSide.MakeHostileTo( side );
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
            galaxy.Mapgen_SeedSpecialEntities( Context, side, DYSON_SPHERE_TAG, 1 );
        }

        public void DoLongRangePlanning( WorldSide side, ArcenLongTermPlanningContext Context )
        {
            ArcenSparseLookup<Planet, List<GameEntity>> unassignedThreatShipsByPlanet = new ArcenSparseLookup<Planet, List<GameEntity>>();
            side.Entities.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity entity )
            {
                if ( entity.LongRangePlanningData == null )
                    return DelReturn.Continue; // if created after the start of this planning cycle, skip

                Planet planet = World_AIW2.Instance.GetPlanetByIndex( entity.LongRangePlanningData.CurrentPlanetIndex );
                if ( entity.TypeData.GetHasTag( DYSON_SPHERE_TAG ) )
                {
                    // the sphere itself
                }
                else
                {
                    // something the sphere spawned

                    if ( entity.LongRangePlanningData.FinalDestinationPlanetIndex != -1 &&
                         entity.LongRangePlanningData.FinalDestinationPlanetIndex != entity.LongRangePlanningData.CurrentPlanetIndex )
                        return DelReturn.Continue; // if heading somewhere else, skip

                    if ( !unassignedThreatShipsByPlanet.GetHasKey( planet ) )
                        unassignedThreatShipsByPlanet[planet] = new List<GameEntity>();
                    unassignedThreatShipsByPlanet[planet].Add( entity );
                }
                return DelReturn.Continue;
            } );

            int pairCount = unassignedThreatShipsByPlanet.GetPairCount();
            for(int i = 0; i < pairCount;i++)
            {
                ArcenSparseLookupPair<Planet, List<GameEntity>> pair = unassignedThreatShipsByPlanet.GetPairByIndex( i );

                FactionUtilityMethods.Helper_SendThreatOnRaid( pair.Value, side, World_AIW2.Instance.SetOfGalaxies.Galaxies[0], pair.Key, false, Context );
            }
        }

        public void DoPerSimStepLogic( WorldSide side, ArcenSimContext Context )
        {
            bool haveHumanOccupiers = false;
            bool haveAIOccupiers = false;
            side.Entities.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity entity )
            {
                if ( entity.LongRangePlanningData == null )
                    return DelReturn.Continue; // if created after the start of this planning cycle, skip

                Planet planet = World_AIW2.Instance.GetPlanetByIndex( entity.LongRangePlanningData.CurrentPlanetIndex );
                if ( !entity.TypeData.GetHasTag( DYSON_SPHERE_TAG ) )
                    return DelReturn.Continue;

                if ( planet.GetIsControlledBySideType( WorldSideType.Player ) )
                    haveHumanOccupiers = true;
                if ( planet.GetIsControlledBySideType( WorldSideType.AI ) )
                    haveAIOccupiers = true;

                return DelReturn.Continue;
            } );

            if ( haveHumanOccupiers )
            {
                World_AIW2.Instance.DoForSides( delegate ( WorldSide otherSide )
                 {
                     if ( side == otherSide )
                         return DelReturn.Continue;
                     switch( otherSide.Type)
                     {
                         case WorldSideType.NaturalObject:
                             break;
                         case WorldSideType.AI:
                             if ( side.GetIsFriendlyTowards( otherSide ) )
                                 break;
                             side.MakeFriendlyTo( otherSide );
                             otherSide.MakeFriendlyTo( side );
                             break;
                         default:
                             if ( side.GetIsHostileTowards( otherSide ) )
                                 break;
                             side.MakeHostileTo( otherSide );
                             otherSide.MakeHostileTo( side );
                             break;
                     }
                     return DelReturn.Continue;
                 } );
            }
            else if ( !haveAIOccupiers )
            {
                World_AIW2.Instance.DoForSides( delegate ( WorldSide otherSide )
                {
                    if ( side == otherSide )
                        return DelReturn.Continue;
                    switch ( otherSide.Type )
                    {
                        case WorldSideType.NaturalObject:
                            break;
                        case WorldSideType.Player:
                            if ( side.GetIsFriendlyTowards( otherSide ) )
                                break;
                            side.MakeFriendlyTo( otherSide );
                            otherSide.MakeFriendlyTo( side );
                            break;
                        default:
                            if ( side.GetIsHostileTowards( otherSide ) )
                                break;
                            side.MakeHostileTo( otherSide );
                            otherSide.MakeHostileTo( side );
                            break;
                    }
                    return DelReturn.Continue;
                } );
            }
            else
            {
                World_AIW2.Instance.DoForSides( delegate ( WorldSide otherSide )
                {
                    if ( side == otherSide )
                        return DelReturn.Continue;
                    if ( otherSide.Type == WorldSideType.NaturalObject )
                        return DelReturn.Continue;
                    if ( side.GetIsHostileTowards( otherSide ) )
                        return DelReturn.Continue;
                    side.MakeHostileTo( otherSide );
                    otherSide.MakeHostileTo( side );
                    return DelReturn.Continue;
                } );
            }
        }
    }
}
