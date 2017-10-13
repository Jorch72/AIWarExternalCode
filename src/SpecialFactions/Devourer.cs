using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class SpecialFaction_Devourer : ISpecialFactionImplementation
    {
        public static SpecialFaction_Devourer Instance;
        public SpecialFaction_Devourer() { Instance = this; }

        public static readonly string DEVOURER_TAG = "Devourer";

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
            galaxy.Mapgen_SeedSpecialEntities( Context, side, DEVOURER_TAG, 1 );
        }

        public void DoLongRangePlanning( WorldSide side, ArcenLongTermPlanningContext Context )
        {
            Galaxy galaxy = World_AIW2.Instance.SetOfGalaxies.Galaxies[0];

            side.Entities.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity entity )
            {
                if ( entity.LongRangePlanningData == null )
                    return DelReturn.Continue; // if created after the start of this planning cycle, skip

                if ( !entity.TypeData.GetHasTag( DEVOURER_TAG ) )
                    return DelReturn.Continue; // if not the Big D, skip (shouldn't happen, but if somebody mods in a reclamator gun for the thing, etc)

                if ( entity.LongRangePlanningData.FinalDestinationPlanetIndex != -1 &&
                     entity.LongRangePlanningData.FinalDestinationPlanetIndex != entity.LongRangePlanningData.CurrentPlanetIndex )
                    return DelReturn.Continue; // if heading somewhere else, skip

                Planet planet = World_AIW2.Instance.GetPlanetByIndex( entity.LongRangePlanningData.CurrentPlanetIndex );

                List<GameEntity> threatShipsNotAssignedElsewhere = new List<GameEntity>();
                threatShipsNotAssignedElsewhere.Add( entity );
                FactionUtilityMethods.Helper_SendThreatOnRaid( threatShipsNotAssignedElsewhere, side, galaxy, planet, true, Context );

                return DelReturn.Continue;
            } );
        }

        public void DoPerSimStepLogic( WorldSide side, ArcenSimContext Context )
        {
            //NOTE: this is not "real" logic, just a demo of how to use custom xml data; it will fire right after you start a new game, if you have the Devourer enabled
            //if ( World_AIW2.Instance.GameSecond <= 1 && World_AIW2.Instance.IsFirstFrameOfSecond )
            //{
            //    //this just dumps all custom data attached to the external constants object, regardless of who put it there
            //    //Useful if something isn't working right and you want to check to see if you have a typo in an attribute name, etc.
            //    ArcenDebugging.ArcenDebugLogSingleLine( ExternalConstants.Instance.DebugCustomData_GetAllKeysInAllNamespaces(), Verbosity.ShowAsInfo );
                
            //    //this corresponds to the "custom_int_examplemod_test_custom_number" attribute in the external constants xml file; here are the pieces:
            //    //"custom_" always have to have this prefix
            //    //"int_" tells it this is a 32-bit int value; other valid options are bool, float, FInt, and string
            //    //"examplemod_" is whatever you want to put in there, with no _ characters in the middle, and this functions as the "namespace" to differentiate the field from others in case some other modder has a "test_custom_number"
            //    //"test_custom_number" is just the rest of the attribute name, and functions as the actual name of the field
            //    CustomDataSet externalConstantsCustomData = ExternalConstants.Instance.GetCustomData( "examplemod" );
            //    int testCustomNumber = externalConstantsCustomData.GetInt( "test_custom_number" );
            //    ArcenDebugging.ArcenDebugLogSingleLine( testCustomNumber.ToString(), Verbosity.ShowAsInfo );
            //}

            //NOTE: there's no "real" logic here, it's just a demo of how to use external data
            if ( !DoomData.DoDebugTestingLogic )
                return;
            if ( !World_AIW2.Instance.IsFirstFrameOfSecond )
                return;

            DoomData.Primitives doomDataPrimitives = World.Instance.GetDoomData_Primitives();
            if ( World.Instance.GetDoomData_DoomedPlanetIndices() == null )
                World.Instance.SetDoomData_DoomedPlanetIndices( new List<int>() );
            List<int> doomedPlanetIndices = World.Instance.GetDoomData_DoomedPlanetIndices();

            if ( doomDataPrimitives.SecondsUntilNextDoomPlanetPick == 0 )
                doomDataPrimitives.SecondsUntilNextDoomPlanetPick = 10;
            doomDataPrimitives.SecondsUntilNextDoomPlanetPick--;

            if ( doomDataPrimitives.SecondsUntilNextDoomPlanetPick <= 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Picking Planet To Doom", Verbosity.DoNotShow );
                doomDataPrimitives.SecondsUntilNextDoomPlanetPick = 5;

                List<Planet> candidates = new List<Planet>();
                List<Planet> allPlanets = World_AIW2.Instance.SetOfGalaxies.Galaxies[0].Planets;
                for ( int i = 0; i < allPlanets.Count; i++ )
                {
                    Planet planet = allPlanets[i];
                    if ( doomedPlanetIndices.ContainsValueType( planet.PlanetIndex ) )
                        continue;
                    candidates.Add( planet );
                }

                if ( candidates.Count > 0 )
                {
                    Planet target = candidates[Context.QualityRandom.Next( 0, candidates.Count )];
                    ArcenDebugging.ArcenDebugLogSingleLine( "Dooming " + target.Name, Verbosity.DoNotShow );
                    doomedPlanetIndices.Add( target.PlanetIndex );
                }
            }

            if ( doomedPlanetIndices.Count > 0 )
            {
                if ( doomDataPrimitives.SecondsUntilNextDoomAttack == 0 )
                    doomDataPrimitives.SecondsUntilNextDoomAttack = 11;
                doomDataPrimitives.SecondsUntilNextDoomAttack--;

                if ( doomDataPrimitives.SecondsUntilNextDoomAttack <= 0 )
                {
                    doomDataPrimitives.SecondsUntilNextDoomAttack = 6;

                    int targetIndex = doomedPlanetIndices[Context.QualityRandom.Next( 0, doomedPlanetIndices.Count )];
                    Planet target = World_AIW2.Instance.GetPlanetByIndex( targetIndex );

                    // cause some entities to spawn on the target planet, or elsewhere and have them travel to the target, etc
                    ArcenDebugging.ArcenDebugLogSingleLine( "Doom attack against " + target.Name, Verbosity.DoNotShow );

                    doomDataPrimitives.LastDoomAttackLaunchedAgainstPlayer = target.GetControllingSide().Type == WorldSideType.Player;
                }
            }
        }
    }
}
