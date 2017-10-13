using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class AIThreatController_Vanilla : IAIThreatController
    {
        private static FInt DelegateHelper_result;
        private static WorldSide DelegateHelper_side;
        private static Planet DelegateHelper_planet;
        public FInt GetRaidDesirability( WorldSide AISide, Planet planet )
        {
            DelegateHelper_result = FInt.Zero;

            planet.Combat.DoForEntities( EntityRollupType.KingUnits, delegate ( GameEntity entity )
             {
                 if ( entity.TypeData.SpecialType == SpecialEntityType.HumanKingUnit )
                     DelegateHelper_result += 10000;
                 return DelReturn.Continue;
             } );

            DelegateHelper_side = AISide;
            DelegateHelper_planet = planet;
            planet.Combat.DoForEntities( EntityRollupType.Claimables, delegate ( GameEntity entity )
             {
                 if ( !entity.Side.WorldSide.GetIsHostileTowards( DelegateHelper_side ) )
                     return DelReturn.Continue;
                 if ( entity.TypeData.ResourceProduction[ResourceType.Fuel] > 0 )
                     DelegateHelper_result += (FInt)DelegateHelper_planet.ResourceOutputs[ResourceType.Fuel] * 100;
                 if ( entity.TypeData.Balance_PlanetSecondsOfMetalToClaim > 0 )
                     DelegateHelper_result += (FInt)DelegateHelper_planet.ResourceOutputs[ResourceType.Metal] * 10;
                 return DelReturn.Continue;
             } );
            DelegateHelper_side = null;
            DelegateHelper_planet = null;

            return DelegateHelper_result;
        }

        public FInt GetRaidTraversalDifficulty( WorldSide AISide, Planet planet )
        {
            FInt result = (FInt)ExternalConstants.Instance.Balance_StrengthPerCap / 20; // basic difficulty cost of travelling a hop

            result += planet.Combat.GetSideForWorldSide( AISide ).DataByStance[SideStance.Hostile].TotalStrength;

            return result;
        }
    }
}
