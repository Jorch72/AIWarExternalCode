using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arcen.AIW2.External
{
    public static class FactionUtilityMethods
    {
        public static void Helper_SendThreatOnRaid( List<GameEntity> threatShipsNotAssignedElsewhere, WorldSide worldSide, Galaxy galaxy, Planet planet, bool IgnorePathCosts, ArcenLongTermPlanningContext Context )
        {
            for ( int k = 0; k < galaxy.Planets.Count; k++ )
            {
                Planet otherPlanet = galaxy.Planets[k];
                otherPlanet.FactionPlanning_CheapestRaidPathToHereComesFrom = null;
                otherPlanet.FactionPlanning_CheapestRaidPathToHereCost = FInt.Zero;
            }
            List<Planet> potentialAttackTargets = new List<Planet>();
            List<Planet> planetsToCheckInFlood = new List<Planet>();
            planetsToCheckInFlood.Add( planet );
            planet.FactionPlanning_CheapestRaidPathToHereComesFrom = planet;
            for ( int k = 0; k < planetsToCheckInFlood.Count; k++ )
            {
                Planet floodPlanet = planetsToCheckInFlood[k];
                floodPlanet.DoForLinkedNeighbors( delegate ( Planet neighbor )
                {
                    FInt totalCostFromOriginToNeighbor = floodPlanet.FactionPlanning_CheapestRaidPathToHereCost + 1;
                    if ( !potentialAttackTargets.Contains( neighbor ) )
                        potentialAttackTargets.Add( neighbor );
                    if ( neighbor.FactionPlanning_CheapestRaidPathToHereComesFrom != null &&
                         neighbor.FactionPlanning_CheapestRaidPathToHereCost <= totalCostFromOriginToNeighbor )
                        return DelReturn.Continue;
                    neighbor.FactionPlanning_CheapestRaidPathToHereComesFrom = floodPlanet;
                    neighbor.FactionPlanning_CheapestRaidPathToHereCost = totalCostFromOriginToNeighbor;
                    planetsToCheckInFlood.Add( neighbor );
                    return DelReturn.Continue;
                } );
            }
            if ( potentialAttackTargets.Count <= 0 )
                return;

            if ( !IgnorePathCosts )
            {
                potentialAttackTargets.Sort( delegate ( Planet Left, Planet Right )
                {
                    return Left.AIPlanning_CheapestRaidPathToHereCost.CompareTo( Right.AIPlanning_CheapestRaidPathToHereCost );
                } );

                int lastIndexToRetain = potentialAttackTargets.Count / 4;
                for ( int k = lastIndexToRetain + 1; k < potentialAttackTargets.Count; k++ )
                    potentialAttackTargets.RemoveAt( k-- );
            }

            Planet threatTarget = potentialAttackTargets[Context.QualityRandom.Next( 0, potentialAttackTargets.Count )];

            List<Planet> path = new List<Planet>();
            Planet workingPlanet = threatTarget;
            while ( workingPlanet != planet )
            {
                path.Insert( 0, workingPlanet );
                workingPlanet = workingPlanet.FactionPlanning_CheapestRaidPathToHereComesFrom;
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
