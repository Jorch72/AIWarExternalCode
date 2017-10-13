using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arcen.AIW2.External
{
    public static class AIUtilityMethods
    {
        public static void Helper_SendThreatOnRaid( List<GameEntity> threatShipsNotAssignedElsewhere, WorldSide worldSide, Galaxy galaxy, Planet planet, ArcenLongTermPlanningContext Context )
        {
            List<Planet> potentialAttackTargets = new List<Planet>();
            List<Planet> planetsToCheckInFlood = new List<Planet>();
            planetsToCheckInFlood.Add( planet );
            planet.AIPlanning_CheapestRaidPathToHereComesFrom = planet;
            for ( int k = 0; k < planetsToCheckInFlood.Count; k++ )
            {
                Planet floodPlanet = planetsToCheckInFlood[k];
                floodPlanet.DoForLinkedNeighbors( delegate ( Planet neighbor )
                {
                    FInt totalCostFromOriginToNeighbor = floodPlanet.AIPlanning_CheapestRaidPathToHereCost + 1;
                    if ( !potentialAttackTargets.Contains( neighbor ) )
                        potentialAttackTargets.Add( neighbor );
                    if ( neighbor.AIPlanning_CheapestRaidPathToHereComesFrom != null &&
                         neighbor.AIPlanning_CheapestRaidPathToHereCost <= totalCostFromOriginToNeighbor )
                        return DelReturn.Continue;
                    neighbor.AIPlanning_CheapestRaidPathToHereComesFrom = floodPlanet;
                    neighbor.AIPlanning_CheapestRaidPathToHereCost = totalCostFromOriginToNeighbor;
                    planetsToCheckInFlood.Add( neighbor );
                    return DelReturn.Continue;
                } );
            }
            if ( potentialAttackTargets.Count <= 0 )
                return;

            Planet threatTarget = potentialAttackTargets[Context.QualityRandom.Next( 0, potentialAttackTargets.Count )];

            List<Planet> path = new List<Planet>();
            Planet workingPlanet = threatTarget;
            while ( workingPlanet != planet )
            {
                path.Insert( 0, workingPlanet );
                workingPlanet = workingPlanet.AIPlanning_CheapestRaidPathToHereComesFrom;
            }
            if ( path.Count > 0 )
            {
                GameCommand command = GameCommand.Create( GameCommandType.SetWormholePath );
                for ( int k = 0; k < threatShipsNotAssignedElsewhere.Count; k++ )
                    command.RelatedEntityIDs.Add( threatShipsNotAssignedElsewhere[k].PrimaryKeyID );
                for ( int k = 0; k < path.Count; k++ )
                    command.RelatedPlanetIndices.Add( path[k].PlanetIndex );
                Context.QueueCommandForSendingAtEndOfContext( command );
            }
        }
    }
}
