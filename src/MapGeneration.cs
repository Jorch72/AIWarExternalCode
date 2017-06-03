using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arcen.AIW2.External
{
    internal static class UtilityMethods
    {
        internal static void Helper_ConnectPlanetLists( List<Planet> First, List<Planet> Second, bool SuppressCrossoverAvoidance, bool ReCallWithSuppressCrossoverAvoidanceIfThatIsTheOnlyWayToConnect )
        {
            if ( First.Count <= 0 || Second.Count <= 0 )
                return;

            Planet closestPlanetOfFirst = null;
            Planet closestPlanetOfSecond = null;
            int closestDistance = 0;
            for ( int i = 0; i < First.Count; i++ )
            {
                Planet planetFromFirst = First[i];
                for ( int j = 0; j < Second.Count; j++ )
                {
                    Planet planetFromSecond = Second[j];
                    int distance = Mat.DistanceBetweenPoints( planetFromFirst.GalaxyLocation, planetFromSecond.GalaxyLocation );
                    if ( closestPlanetOfFirst == null || distance < closestDistance )
                    {
                        if ( SuppressCrossoverAvoidance || !GetWouldLinkCrossOverOtherPlanets( planetFromFirst, planetFromSecond ) )
                        {
                            closestPlanetOfFirst = planetFromFirst;
                            closestPlanetOfSecond = planetFromSecond;
                            closestDistance = distance;
                        }
                    }
                }
            }

            if ( closestPlanetOfFirst != null && closestPlanetOfSecond != null )
                closestPlanetOfFirst.AddLinkTo( closestPlanetOfSecond );
            else
            if ( ReCallWithSuppressCrossoverAvoidanceIfThatIsTheOnlyWayToConnect && !SuppressCrossoverAvoidance )
                Helper_ConnectPlanetLists( First, Second, true, false );
        }

        internal static bool GetWouldLinkCrossOverOtherPlanets( Planet First, Planet Second )
        {
            List<Planet> PlanetsToNoNotCrossOver = First.ParentGalaxy.Planets;
            for ( int k = 0; k < PlanetsToNoNotCrossOver.Count; k++ )
            {
                Planet planetToNotHit = PlanetsToNoNotCrossOver[k];
                if ( planetToNotHit == First || planetToNotHit == Second )
                    continue;
                if ( Mat.LineIntersectsRectangleContainingCircle( First.GalaxyLocation, Second.GalaxyLocation, planetToNotHit.GalaxyLocation, planetToNotHit.TypeData.IntraStellarRadius ) )
                    return true;
            }
            return false;
        }

        internal static int Helper_GetNumberOfPairsInPairListInvolving( List<Pair<int, int>> PairList, int First )
        {
            int result = 0;

            Pair<int, int> item;
            for ( int i = 0; i < PairList.Count; i++ )
            {
                item = PairList[i];
                if ( item.LeftItem != First && item.RightItem != First )
                    continue;
                result++;
            }

            return result;
        }

        internal static bool Helper_GetDoesPairListContainPairInEitherDirection( List<Pair<int, int>> PairList, int First, int Second )
        {
            Pair<int, int> item;
            for ( int i = 0; i < PairList.Count; i++ )
            {
                item = PairList[i];
                if ( item.LeftItem == First && item.RightItem == Second )
                    return true;
                if ( item.RightItem == First && item.LeftItem == Second )
                    return true;
            }
            return false;
        }

        internal static bool HelperDoesPointListContainPointWithinDistance( List<ArcenPoint> PointList, ArcenPoint Point, int Distance )
        {
            for ( int i = 0; i < PointList.Count; i++ )
            {
                if ( Mat.ApproxDistanceBetweenPointsFast( PointList[i], Point, Distance ) > Distance )
                    continue;
                if ( Mat.DistanceBetweenPoints( PointList[i], Point ) > Distance )
                    continue;
                return true;
            }
            return false;
        }

        internal static void Helper_AddConnectionsWithinCluster( List<Planet> PlanetsInCluster, int HoldOffAtThisManyLinksPerPlanet, bool SuppressCrossoverAvoidance )
        {
            for ( int j = 0; j < PlanetsInCluster.Count; j++ )
            {
                Planet planetInCluster = PlanetsInCluster[j];
                for ( int k = 0; k < PlanetsInCluster.Count; k++ )
                {
                    if ( k == j )
                        continue;
                    Planet otherPlanetInCluster = PlanetsInCluster[k];
                    if ( otherPlanetInCluster.GetIsDirectlyLinkedTo( planetInCluster ) )
                        continue;
                    if ( otherPlanetInCluster.GetLinkedNeighborCount() >= HoldOffAtThisManyLinksPerPlanet )
                        continue;
                    if ( !SuppressCrossoverAvoidance && GetWouldLinkCrossOverOtherPlanets( planetInCluster, otherPlanetInCluster ) )
                        continue;
                    int distanceToOtherPlanetInCluster = Mat.DistanceBetweenPoints( planetInCluster.GalaxyLocation, otherPlanetInCluster.GalaxyLocation );

                    bool foundNearerPlanet = false;
                    for ( int l = 0; l < PlanetsInCluster.Count; l++ )
                    {
                        Planet yetAnotherPlanetInCluster = PlanetsInCluster[l];
                        if ( l == j || l == k )
                            continue;
                        if ( otherPlanetInCluster.GetIsDirectlyLinkedTo( yetAnotherPlanetInCluster ) )
                            continue;
                        int distanceFromOtherToYetAnother = Mat.DistanceBetweenPoints( otherPlanetInCluster.GalaxyLocation, yetAnotherPlanetInCluster.GalaxyLocation );
                        if ( distanceFromOtherToYetAnother >= distanceToOtherPlanetInCluster )
                            continue;
                        if ( !SuppressCrossoverAvoidance && GetWouldLinkCrossOverOtherPlanets( yetAnotherPlanetInCluster, otherPlanetInCluster ) )
                            continue;
                        foundNearerPlanet = true;
                        break;
                    }

                    if ( foundNearerPlanet )
                        continue;
                    planetInCluster.AddLinkTo( otherPlanetInCluster );
                }
            }
        }

        internal static void Helper_MakeIntraClusterConnections( List<Planet> ClusterPlanets, MapClusterStyle ClusterStyle )
        {
            switch ( ClusterStyle )
            {
                #region Simple
                case MapClusterStyle.Simple:
                //case MapClusterStyle.ConcentricWithSimpleLayout:
                case MapClusterStyle.CrosshatchWithSimpleLayout:
                    {
                        Helper_AddConnectionsWithinCluster( ClusterPlanets, 1, false );
                        int lastTotalDisconnectionCount = -1;
                        while ( true )
                        {
                            #region Next, find the planet in the cluster connected to the most other planets in the cluster
                            List<Planet> bestConnectedPlanets = null;
                            List<Planet> bestDisconnectedPlanets = null;
                            int totalDisconnectionCount = 0;
                            {
                                Planet planetInCluster;
                                Planet otherPlanetInCluster;
                                List<Planet> planetsConnectedToThisPlanet;
                                List<Planet> planetsNotConnectedToThisPlanet;
                                for ( int j = 0; j < ClusterPlanets.Count; j++ )
                                {
                                    planetInCluster = ClusterPlanets[j];
                                    planetsConnectedToThisPlanet = new List<Planet>();
                                    planetsConnectedToThisPlanet.Add( planetInCluster );
                                    planetsNotConnectedToThisPlanet = new List<Planet>();
                                    for ( int k = 0; k < ClusterPlanets.Count; k++ )
                                    {
                                        if ( k == j )
                                            continue;
                                        otherPlanetInCluster = ClusterPlanets[k];
                                        if ( planetInCluster.MapGenOnly_GetIsConnectedByAnyLinksTo( otherPlanetInCluster ) )
                                            planetsConnectedToThisPlanet.Add( otherPlanetInCluster );
                                        else
                                        {
                                            planetsNotConnectedToThisPlanet.Add( otherPlanetInCluster );
                                            totalDisconnectionCount++;
                                        }
                                    }
                                    if ( bestConnectedPlanets == null || planetsConnectedToThisPlanet.Count > bestConnectedPlanets.Count )
                                    {
                                        bestConnectedPlanets = planetsConnectedToThisPlanet;
                                        bestDisconnectedPlanets = planetsNotConnectedToThisPlanet;
                                    }
                                    if ( bestDisconnectedPlanets.Count <= 0 )
                                        break;
                                }
                            }
                            #endregion
                            if ( bestDisconnectedPlanets.Count <= 0 )
                                break;
                            bool suppressCrossoverAvoidance = false;
                            if ( lastTotalDisconnectionCount > 0 && lastTotalDisconnectionCount == totalDisconnectionCount )
                                suppressCrossoverAvoidance = true;
                            lastTotalDisconnectionCount = totalDisconnectionCount;
                            UtilityMethods.Helper_ConnectPlanetLists( bestConnectedPlanets, bestDisconnectedPlanets, suppressCrossoverAvoidance, false );
                        }
                        Helper_AddConnectionsWithinCluster( ClusterPlanets, 2, false );
                        Helper_AddConnectionsWithinCluster( ClusterPlanets, 3, false );
                    }
                    break;
                #endregion
                #region Concentric
                case MapClusterStyle.Concentric:
                    {
                        List<Planet> planetsInPreviousLayer = new List<Planet>();
                        planetsInPreviousLayer.Add( ClusterPlanets[0] );
                        ArcenPoint clusterCenter = ClusterPlanets[0].GalaxyLocation;
                        int nextLayerIsNoMoreThanThisFarOut = ( 60 + 30 );
                        List<Planet> planetsInNextLayer = new List<Planet>();
                        Planet planet;
                        int distanceToClusterCenter;
                        for ( int i = 1; i < ClusterPlanets.Count; i++ )
                        {
                            planet = ClusterPlanets[i];
                            distanceToClusterCenter = Mat.DistanceBetweenPoints( planet.GalaxyLocation, clusterCenter );
                            if ( distanceToClusterCenter < nextLayerIsNoMoreThanThisFarOut )
                            {
                                // just another one for the layer
                                planetsInNextLayer.Add( planet );
                                if ( planetsInNextLayer.Count > 1 )
                                    planetsInNextLayer[planetsInNextLayer.Count - 2].AddLinkTo( planetsInNextLayer[planetsInNextLayer.Count - 1] );
                            }
                            else
                            {
                                // starting new layer
                                if ( planetsInNextLayer.Count > 2 )
                                {
                                    if ( Mat.DistanceBetweenPoints( planetsInNextLayer[planetsInNextLayer.Count - 1].GalaxyLocation,
                                        planetsInNextLayer[0].GalaxyLocation ) < 70 )
                                        planetsInNextLayer[planetsInNextLayer.Count - 1].AddLinkTo( planetsInNextLayer[0] );
                                }
                                UtilityMethods.Helper_ConnectPlanetLists( planetsInPreviousLayer, planetsInNextLayer, false, true );
                                planetsInPreviousLayer.Clear();
                                planetsInPreviousLayer.AddRange( planetsInNextLayer );
                                planetsInNextLayer.Clear();
                                planetsInNextLayer.Add( planet );
                                nextLayerIsNoMoreThanThisFarOut += 60;
                            }
                        }
                        if ( planetsInNextLayer.Count > 0 )
                        {
                            if ( planetsInNextLayer.Count > 2 )
                            {
                                if ( Mat.DistanceBetweenPoints( planetsInNextLayer[planetsInNextLayer.Count - 1].GalaxyLocation,
                                    planetsInNextLayer[0].GalaxyLocation ) < 70 )
                                    planetsInNextLayer[planetsInNextLayer.Count - 1].AddLinkTo( planetsInNextLayer[0] );
                            }
                            // connect last layer
                            UtilityMethods.Helper_ConnectPlanetLists( planetsInPreviousLayer, planetsInNextLayer, false, true );
                            List<Planet> tempList = new List<Planet>();
                            for ( int i = 0; i < planetsInNextLayer.Count; i++ )
                            {
                                planet = planetsInNextLayer[i];
                                if ( planet.GetLinkedNeighborCount() > 1 )
                                    continue;
                                tempList.Clear();
                                tempList.Add( planet );
                                UtilityMethods.Helper_ConnectPlanetLists( planetsInPreviousLayer, tempList, false, true );
                                tempList.Clear();
                            }

                            planetsInPreviousLayer.Clear();
                            planetsInNextLayer.Clear();
                        }
                    }
                    break;
                #endregion
                #region Crosshatch
                case MapClusterStyle.Crosshatch:
                    {
                        int connectIfDistanceLessThan = 80;
                        Planet planet;
                        Planet otherPlanet;
                        for ( int i = 0; i < ClusterPlanets.Count; i++ )
                        {
                            planet = ClusterPlanets[i];
                            for ( int j = 0; j < ClusterPlanets.Count; j++ )
                            {
                                if ( i == j )
                                    continue;
                                otherPlanet = ClusterPlanets[j];
                                if ( planet.GetIsDirectlyLinkedTo( otherPlanet ) )
                                    continue;
                                if ( Mat.DistanceBetweenPoints( planet.GalaxyLocation, otherPlanet.GalaxyLocation ) > connectIfDistanceLessThan )
                                    continue;
                                planet.AddLinkTo( otherPlanet );
                            }
                        }
                    }
                    break;
                    #endregion
            }
        }

        internal static bool Helper_PickIntraClusterPlanetPoints( ArcenSimContext Context, List<ArcenPoint> PlanetPointListToFill, ArcenPoint ClusterCenter, int ClusterRadius, int ClusterPlanetCount, MapClusterStyle ClusterStyle )
        {
            switch ( ClusterStyle )
            {
                #region Simple
                case MapClusterStyle.Simple:
                    {
                        int minimumDistanceBetweenPlanets = 50;

                        if ( ClusterRadius < 120 )
                            minimumDistanceBetweenPlanets = 40;
                        if ( ClusterRadius < 100 )
                            minimumDistanceBetweenPlanets = 25;

                        int numberFailuresAllowed = 1000;
                        for ( int j = 0; j < ClusterPlanetCount; j++ )
                        {
                            ArcenPoint newPlanetPoint = ClusterCenter.GetRandomPointWithinDistance( Context.QualityRandom, 0, ClusterRadius );

                            if ( UtilityMethods.HelperDoesPointListContainPointWithinDistance( PlanetPointListToFill, newPlanetPoint, minimumDistanceBetweenPlanets ) )
                            {
                                j--;
                                numberFailuresAllowed--;
                                if ( numberFailuresAllowed <= 0 )
                                    return false;
                                continue;
                            }

                            PlanetPointListToFill.Add( newPlanetPoint );
                        }
                    }
                    break;
                #endregion
                #region Concentric
                case MapClusterStyle.Concentric:
                    //case MapClusterStyle.ConcentricWithSimpleLayout:
                    {
                        int minimumDistanceBetweenPlanets = 50;

                        PlanetPointListToFill.Add( ClusterCenter );

                        int distanceFromCenter = 0;
                        while ( PlanetPointListToFill.Count < ClusterPlanetCount )
                        {
                            distanceFromCenter += ( minimumDistanceBetweenPlanets + 10 );
                            if ( distanceFromCenter > ClusterRadius )
                                return false;
                            AngleDegrees angleToStartAt = AngleDegrees.Create( (FInt)Context.QualityRandom.Next( 1, 360 ) );
                            for ( int i = 0; i < 360; i++ )
                            {
                                AngleDegrees angle = angleToStartAt.Add( AngleDegrees.Create( (FInt)i ) );
                                ArcenPoint potentialPlanetPoint = ClusterCenter.GetPointAtAngleAndDistance( angle, distanceFromCenter );
                                if ( UtilityMethods.HelperDoesPointListContainPointWithinDistance( PlanetPointListToFill, potentialPlanetPoint, minimumDistanceBetweenPlanets ) )
                                    continue;
                                PlanetPointListToFill.Add( potentialPlanetPoint );
                                if ( PlanetPointListToFill.Count >= ClusterPlanetCount )
                                    break;
                            }
                        }
                    }
                    break;
                #endregion
                #region Crosshatch
                case MapClusterStyle.Crosshatch:
                case MapClusterStyle.CrosshatchWithSimpleLayout:
                    {
                        int minimumDistanceBetweenPlanets = 50;

                        int sideLengthInPlanets;
                        if ( ClusterPlanetCount >= 26 )
                            return false;
                        else if ( ClusterPlanetCount >= 17 )
                            sideLengthInPlanets = 5;
                        else if ( ClusterPlanetCount >= 10 )
                            sideLengthInPlanets = 4;
                        else if ( ClusterPlanetCount >= 5 )
                            sideLengthInPlanets = 3;
                        else
                            sideLengthInPlanets = 2;

                        int sideLengthInPixels = sideLengthInPlanets * minimumDistanceBetweenPlanets;

                        ArcenPoint topLeftCorner = ClusterCenter;
                        topLeftCorner.X -= ( sideLengthInPixels >> 1 );
                        topLeftCorner.Y -= ( sideLengthInPixels >> 1 );

                        ArcenPoint proposedPoint;
                        for ( int i = 0; i < sideLengthInPlanets; i++ )
                        {
                            for ( int j = 0; j < sideLengthInPlanets; j++ )
                            {
                                proposedPoint = topLeftCorner;
                                proposedPoint.X += ( i * minimumDistanceBetweenPlanets );
                                proposedPoint.Y += ( j * minimumDistanceBetweenPlanets );
                                PlanetPointListToFill.Add( proposedPoint );
                                if ( PlanetPointListToFill.Count >= ClusterPlanetCount )
                                    break;
                            }
                            if ( PlanetPointListToFill.Count >= ClusterPlanetCount )
                                break;
                        }

                        AngleDegrees rotationAngle = AngleDegrees.Create( (FInt)( Context.QualityRandom.Next( 10, 20 ) * ( Context.QualityRandom.NextBool() ? -1 : 1 ) ) );
                        for ( int i = 0; i < PlanetPointListToFill.Count; i++ )
                        {
                            proposedPoint = PlanetPointListToFill[i];
                            int currentPointDistanceToClusterCenter = Mat.DistanceBetweenPoints( proposedPoint, ClusterCenter );
                            AngleDegrees currentPointAngle = ClusterCenter.GetAngleToDegrees( proposedPoint ).Add( rotationAngle );
                            ArcenPoint rotatedPoint = ClusterCenter.GetPointAtAngleAndDistance( currentPointAngle, currentPointDistanceToClusterCenter );
                            PlanetPointListToFill[i] = rotatedPoint;
                        }
                    }
                    break;
                    #endregion
            }

            return true;
        }
    }

    public abstract class Mapgen_Base : IMapGenerator
    {
        public virtual void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
        {
        }

        public void SeedNormalEntities( Planet planet, ArcenSimContext Context, MapTypeData mapType )
        {
            int wormholeRadius = ( ExternalConstants.Instance.Balance_AverageGravWellRadius * FInt.FromParts( 0, 900 ) ).IntValue;

            int innerSystemMinimumRadius = ( ExternalConstants.Instance.Balance_AverageGravWellRadius * FInt.FromParts( 0, 150 ) ).IntValue;
            int innerSystemMaximumRadius = ( ExternalConstants.Instance.Balance_AverageGravWellRadius * FInt.FromParts( 0, 300 ) ).IntValue;

            ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
            AngleDegrees angleToAI = AngleDegrees.Create( (FInt)Context.QualityRandom.Next( 0, 360 ) );

            switch ( planet.PopulationType )
            {
                case PlanetPopulationType.NonHomeworld:
                case PlanetPopulationType.AIHomeworld:
                case PlanetPopulationType.HumanHomeworld:
                    {
                        GameEntityTypeData controllerData = GameEntityTypeDataTable.Instance.GetRandomRowOfSpecialType( Context, SpecialEntityType.Controller );
                        GameEntityTypeData warpGateData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarpGate" );
                        GameEntityTypeData warheadSupressorData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarheadSuppressor" );

                        int distanceToGate = ( warpGateData.Radius + controllerData.Radius ) * 2;

                        for ( int i = 0; i < planet.Combat.Sides.Count; i++ )
                        {
                            CombatSide side = planet.Combat.Sides[i];

                            switch ( side.WorldSide.Type )
                            {
                                case WorldSideType.AI:
                                    {
                                        ArcenPoint sideCenter = center.GetPointAtAngleAndDistance( angleToAI, Context.QualityRandom.Next( innerSystemMinimumRadius, innerSystemMaximumRadius ) );
                                        GameEntity.CreateNew( side, controllerData, sideCenter, Context );

                                        ArcenPoint entityPoint;

                                        entityPoint = sideCenter.GetRandomPointWithinDistance( Context.QualityRandom, distanceToGate, distanceToGate );
                                        GameEntity.CreateNew( side, warpGateData, entityPoint, Context );

                                        switch ( planet.PopulationType )
                                        {
                                            case PlanetPopulationType.HumanHomeworld:
                                                {
                                                    List<GameEntityTypeData> testShipDatas = GameEntityTypeDataTable.Instance.RowsByRollup[EntityRollupType.AITestShip];
                                                    for ( int j = 0; j < testShipDatas.Count; j++ )
                                                    {
                                                        GameEntityTypeData testShipData = testShipDatas[j];
                                                        entityPoint = sideCenter.GetRandomPointWithinDistance( Context.QualityRandom, innerSystemMinimumRadius, innerSystemMaximumRadius );
                                                        GameEntity.CreateNew( side, testShipData, entityPoint, Context );
                                                    }
                                                }
                                                break;
                                            case PlanetPopulationType.AIHomeworld:
                                                for ( int j = 0; j < 4; j++ )
                                                {
                                                    entityPoint = sideCenter.GetRandomPointWithinDistance( Context.QualityRandom, distanceToGate, distanceToGate );
                                                    GameEntity.CreateNew( side, warheadSupressorData, entityPoint, Context );
                                                }
                                                break;
                                            case PlanetPopulationType.NonHomeworld:
                                                if(planet.MarkLevel.Ordinal >= 4)
                                                {
                                                    for ( int j = 0; j < 2; j++ )
                                                    {
                                                        entityPoint = sideCenter.GetRandomPointWithinDistance( Context.QualityRandom, distanceToGate, distanceToGate );
                                                        GameEntity.CreateNew( side, warheadSupressorData, entityPoint, Context );
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    break;
            }

            switch ( planet.PopulationType )
            {
                #region HumanHomeworld
                case PlanetPopulationType.HumanHomeworld:
                    {
                        GameEntityTypeData humanKingUnitData = GameEntityTypeDataTable.Instance.GetRandomRowOfSpecialType( Context, SpecialEntityType.HumanKingUnit );

                        AngleDegrees angleToFirstPlayer = angleToAI.Add( AngleDegrees.Create( (FInt)160 ) );
                        AngleDegrees angleToSecondPlayer = angleToAI.Add( AngleDegrees.Create( (FInt)200 ) );
                        bool haveFoundFirstPlayer = false;

                        for ( int i = 0; i < planet.Combat.Sides.Count; i++ )
                        {
                            CombatSide side = planet.Combat.Sides[i];

                            switch ( side.WorldSide.Type )
                            {
                                case WorldSideType.Player:
                                    {
                                        ArcenPoint sideCenter = center.GetPointAtAngleAndDistance( haveFoundFirstPlayer ? angleToSecondPlayer : angleToFirstPlayer, Context.QualityRandom.Next( wormholeRadius, wormholeRadius ) );
                                        haveFoundFirstPlayer = true;

                                        GameEntity.CreateNew( side, humanKingUnitData, sideCenter, Context );

                                        List<GameEntityTypeData> testShipDatas = GameEntityTypeDataTable.Instance.RowsByRollup[EntityRollupType.PlayerTestShip];
                                        for ( int j = 0; j < testShipDatas.Count; j++ )
                                        {
                                            GameEntityTypeData testShipData = testShipDatas[j];
                                            AngleDegrees angleToTestUnit = angleToFirstPlayer.GetOpposite().Add( AngleDegrees.Create( (FInt)Context.QualityRandom.Next( -35, 35 ) ) );
                                            int distanceToTestUnit = Context.QualityRandom.Next( innerSystemMinimumRadius, innerSystemMaximumRadius );
                                            ArcenPoint otherPoint = sideCenter.GetPointAtAngleAndDistance( angleToTestUnit, distanceToTestUnit );
                                            GameEntity.CreateNew( side, testShipData, otherPoint, Context );
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    break;
                #endregion
                #region AIHomeworld
                case PlanetPopulationType.AIHomeworld:
                    {
                        GameEntityTypeData aiKingUnitData = GameEntityTypeDataTable.Instance.GetRandomRowOfSpecialType( Context, SpecialEntityType.AIKingUnit );

                        for ( int i = 0; i < planet.Combat.Sides.Count; i++ )
                        {
                            CombatSide side = planet.Combat.Sides[i];

                            switch ( side.WorldSide.Type )
                            {
                                case WorldSideType.AI:
                                    {
                                        ArcenPoint sideCenter = center.GetPointAtAngleAndDistance( angleToAI, Context.QualityRandom.Next( innerSystemMinimumRadius, innerSystemMaximumRadius ) );
                                        GameEntity.CreateNew( side, aiKingUnitData, sideCenter, Context );
                                    }
                                    break;
                            }
                        }
                    }
                    break;
                #endregion
                #region NonHomeworld
                case PlanetPopulationType.NonHomeworld:
                    {
                    }
                    break;
                    #endregion
            }

            #region Wormholes
            {
                GameEntityTypeData wormholeData = GameEntityTypeDataTable.Instance.GetRandomRowOfSpecialType( Context, SpecialEntityType.Wormhole );
                CombatSide side = planet.Combat.GetFirstSideOfType( WorldSideType.NaturalObject );

                planet.DoForLinkedNeighbors( delegate ( Planet neighbor )
                {
                    AngleDegrees angleToNeighbor = planet.GalaxyLocation.GetAngleToDegrees( neighbor.GalaxyLocation );
                    ArcenPoint wormholePoint = Engine_AIW2.Instance.CombatCenter.GetPointAtAngleAndDistance( angleToNeighbor, wormholeRadius );
                    GameEntity wormhole = GameEntity.CreateNew( side, wormholeData, wormholePoint, Context );
                    wormhole.LinkedPlanetIndex = neighbor.PlanetIndex;
                    return DelReturn.Continue;
                } );
            }
            #endregion

            SeedResourceSpot( planet, Context, wormholeRadius, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MetalGenerator" ) );
            SeedResourceSpot( planet, Context, wormholeRadius, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "FuelGenerator" ) );
            SeedResourceSpot( planet, Context, wormholeRadius, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScienceGenerator" ) );
            if ( planet.ResourceOutputs[ResourceType.Hacking] > 0 )
                SeedResourceSpot( planet, Context, wormholeRadius, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "HackingGenerator" ) );

            #region Guardians
            switch ( planet.PopulationType )
            {
                case PlanetPopulationType.AIHomeworld:
                case PlanetPopulationType.NonHomeworld:
                case PlanetPopulationType.HumanHomeworld:
                    {
                        planet.DoInitialOrReconquestDefenseSeeding( Context );
                    }
                    break;
            }
            #endregion
        }

        public void SeedResourceSpot( Planet planet, ArcenSimContext Context, Int32 wormholeRadius, GameEntityTypeData resourceSpotData )
        {
            CombatSide side = planet.Combat.GetFirstSideOfType( WorldSideType.NaturalObject );

            ArcenPoint spotPoint = ArcenPoint.ZeroZeroPoint;
            bool foundIt = false;
            int rechecks = 100;
            while ( !foundIt && rechecks > 0 )
            {
                spotPoint = Mat.GetRandomPointFromCircleCenter( Context.QualityRandom, Engine_AIW2.Instance.CombatCenter, resourceSpotData.Radius * 4, wormholeRadius );
                rechecks--;
                foundIt = true;
                planet.Combat.DoForEntities( GameEntityCategorySet.ShipsAndNaturalObjects, delegate ( GameEntity entity )
                {
                    if ( !entity.GetIsWithinRangeOf( spotPoint, resourceSpotData.Radius * 4, false ) )
                        return DelReturn.Continue;
                    foundIt = false;
                    return DelReturn.Break;
                } );
            }
            GameEntity.CreateNew( side, resourceSpotData, spotPoint, Context );
        }

        public void SeedSpecialEntities( Galaxy galaxy, ArcenSimContext Context, MapTypeData MapData )
        {
            List<Planet> baseListPlanetsToSeedOn = new List<Planet>();
            for ( int i = 0; i < galaxy.Planets.Count; i++ )
            {
                Planet planet = galaxy.Planets[i];
                if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld )
                    continue;
                baseListPlanetsToSeedOn.Add( planet );
            }

            List<Planet> planetsToSeedOn = new List<Planet>();

            Planet humanHomeworld = galaxy.GetFirstPlanetOf( PlanetPopulationType.HumanHomeworld );

            int innerSystemMinimumRadius = ( ExternalConstants.Instance.Balance_AverageGravWellRadius * FInt.FromParts( 0, 150 ) ).IntValue;
            int innerSystemMaximumRadius = ( ExternalConstants.Instance.Balance_AverageGravWellRadius * FInt.FromParts( 0, 300 ) ).IntValue;

            #region Sensor Scramblers
            {
                planetsToSeedOn.Clear();
                planetsToSeedOn.AddRange( baseListPlanetsToSeedOn );
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "SensorScrambler" );

                int revealedAreaAroundHumanHomeworld = 3;
                int desiredOtherPocketSize = 3;

                if ( humanHomeworld != null )
                {
                    humanHomeworld.DoForPlanetsWithinXHops( Context, revealedAreaAroundHumanHomeworld - 1, delegate ( Planet planet, int distance )
                    {
                        planetsToSeedOn.Remove( planet );
                        if ( distance != revealedAreaAroundHumanHomeworld - 1 )
                            return DelReturn.Continue;
                        planet.Mapgen_SeedAIEntity( Context, entityData, PlanetSeedingZone.InnerSystem );
                        return DelReturn.Continue;
                    } );
                }

                int numberToSeed = Math.Max( 1, planetsToSeedOn.Count / 4 );
                while ( numberToSeed > 0 )
                {
                    numberToSeed--;
                    Planet planet = planetsToSeedOn[Context.QualityRandom.Next( 0, planetsToSeedOn.Count )];
                    planetsToSeedOn.Remove( planet );
                    planet.Mapgen_SeedAIEntity( Context, entityData, PlanetSeedingZone.InnerSystem );
                }

                EntityRollupType rollup = EntityRollupType.ScramblesSensors;
                int passesToRun = 3;
                for ( int i = 0; i < passesToRun; i++ )
                {
                    galaxy.Mapgen_RemoveEntitiesIfNotEnclosingAnyPockets( Context, revealedAreaAroundHumanHomeworld, rollup, entityData );
                    while ( galaxy.Mapgen_ExpandSmallestPocketNotContainingThis( Context, entityData, rollup, revealedAreaAroundHumanHomeworld, desiredOtherPocketSize ) ) ;
                    while ( galaxy.Mapgen_SplitUpBiggestPocketNotContainingThis( Context, entityData, rollup, revealedAreaAroundHumanHomeworld, desiredOtherPocketSize ) ) ;
                    galaxy.Mapgen_RemoveEntitiesIfNotEnclosingAnyPockets( Context, revealedAreaAroundHumanHomeworld, rollup, entityData );
                }
            }
            #endregion
            
            galaxy.Mapgen_SeedSpecialEntities( Context, "AdvancedFactory", 1 );
            galaxy.Mapgen_SeedSpecialEntities( Context, "AdvancedStarshipConstructor", 1 );
            galaxy.Mapgen_SeedSpecialEntities( Context, "ExperimentalFabricator", 5 );
            galaxy.Mapgen_SeedSpecialEntities( Context, "ExperimentalTurretController", 4 );
            galaxy.Mapgen_SeedSpecialEntities( Context, "DataCenter", 3 );
            galaxy.Mapgen_SeedSpecialEntities( Context, "Coprocessor", 4 );
            galaxy.Mapgen_SeedSpecialEntities( Context, "SuperTerminal", 1 );
            galaxy.Mapgen_SeedSpecialEntities( Context, "AdvancedResearchStation", 4 );
            galaxy.Mapgen_SeedSpecialEntities( Context, "NuclearWarheadSilo", 2 );
            galaxy.Mapgen_SeedSpecialEntities( Context, "EMPWarheadSilo", 2 );
            galaxy.Mapgen_SeedSpecialEntities( Context, "SpecialForcesSecretNinjaHideout", Math.Max( 1, galaxy.Planets.Count / 10 ) );
            galaxy.Mapgen_SeedSpecialEntities( Context, "NormalPlanetNastyPick", Math.Max( 1, galaxy.Planets.Count / 2 ) );
            galaxy.Mapgen_SeedSpecialEntities( Context, World_AIW2.Instance.GetNeutralSide(), "Flagship", 1, 3, 3, 3, -1 );
            galaxy.Mapgen_SeedSpecialEntities( Context, World_AIW2.Instance.GetNeutralSide(), "Flagship", 1, 4, 5, 3, -1 );
            galaxy.Mapgen_SeedSpecialEntities( Context, World_AIW2.Instance.GetNeutralSide(), "Flagship", 1, 6, 7, 3, -1 );
            galaxy.Mapgen_SeedSpecialEntities( Context, World_AIW2.Instance.GetNeutralSide(), "Flagship", 1, 3, -1, 5, 6 );
            galaxy.Mapgen_SeedSpecialEntities( Context, World_AIW2.Instance.GetNeutralSide(), "Flagship", 1, 3, -1, 3, 4 );
        }
    }

    public class Mapgen_Honeycomb : Mapgen_Base
    {
        public override void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
        {
            InnerGenerate( galaxy, Context, numberToSeed, PlanetType.Normal, mapType );
        }

        protected void InnerGenerate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, PlanetType planetType, MapTypeData mapType )
        {
            int minRings = 0;
            int cellsAtRing = 0;
            for ( ; minRings < maxCellsByRingCount.Length; minRings++ )
            {
                cellsAtRing = maxCellsByRingCount[minRings];
                if ( cellsAtRing >= numberToSeed )
                    break;
            }

            if ( ArcenStrings.Equals( mapType.InternalName, "Honeycomb" ) )
            {
                int extraCellCount = cellsAtRing - numberToSeed;

                FInt extraCellRatio = ( (FInt)extraCellCount / (FInt)numberToSeed );
                if ( extraCellRatio < FInt.FromParts( 0, 100 ) )
                {
                    if ( Context.QualityRandom.Next( 0, 2 ) == 0 )
                        minRings++;
                }
                else if ( extraCellRatio < FInt.FromParts( 0, 200 ) )
                {
                    if ( Context.QualityRandom.Next( 0, 3 ) == 0 )
                        minRings++;
                }
                else if ( extraCellRatio < FInt.FromParts( 0, 300 ) )
                {
                    if ( Context.QualityRandom.Next( 0, 4 ) == 0 )
                        minRings++;
                }
            }

            if ( minRings > 8 )
                minRings = 8;

            int numberOfRows = ( minRings * 2 ) - 1;

            //2) Determine number of points on central horizontal (just rings times 2, minus 1)
            int numberOfColumnsOnCentralRow = numberOfRows;

            ArcenPoint[][] pointRows = new ArcenPoint[numberOfRows][];

            int distanceBetweenPoints = planetType.GetData().InterStellarRadius * 2;

            ArcenRectangle seedingArea;
            seedingArea.Width = numberOfRows * distanceBetweenPoints;
            seedingArea.Height = numberOfRows * distanceBetweenPoints;
            seedingArea.X = Engine_AIW2.GalaxyCenter.X - seedingArea.Width / 2;
            seedingArea.Y = Engine_AIW2.GalaxyCenter.Y - seedingArea.Height / 2;

            //int centerX = seedingArea.CalculateCenterPoint().X;
            int centerY = seedingArea.CalculateCenterPoint().Y;

            int centralRowIndex = minRings - 1;
            pointRows[centralRowIndex] = Helper_GetHexagonalCoordinatesForRow( numberOfColumnsOnCentralRow, centerY, distanceBetweenPoints, seedingArea.X );

            int rowY = centerY;
            int numberOfCellsOnRow = numberOfColumnsOnCentralRow;
            FInt offset = FInt.Zero;
            for ( int i = centralRowIndex + 1; i < pointRows.Length; i++ )
            {
                numberOfCellsOnRow -= 1;
                rowY += distanceBetweenPoints;
                offset += FInt.FromParts( 0, 500 );
                pointRows[i] = Helper_GetHexagonalCoordinatesForRow( numberOfCellsOnRow, rowY, distanceBetweenPoints, seedingArea.X + ( offset * distanceBetweenPoints ).IntValue );
            }

            rowY = centerY;
            numberOfCellsOnRow = numberOfColumnsOnCentralRow;
            offset = FInt.Zero;
            for ( int i = centralRowIndex - 1; i >= 0; i-- )
            {
                numberOfCellsOnRow -= 1;
                rowY -= distanceBetweenPoints;
                offset += FInt.FromParts( 0, 500 );
                pointRows[i] = Helper_GetHexagonalCoordinatesForRow( numberOfCellsOnRow, rowY, distanceBetweenPoints, seedingArea.X + ( offset * distanceBetweenPoints ).IntValue );
            }

            // remove excess points
            ArcenPoint[] pointRow;
            ArcenPoint point;
            {
                int totalPoints = 0;
                for ( int i = 0; i < pointRows.Length; i++ )
                    totalPoints += pointRows[i].Length;
                if ( ArcenStrings.Equals( mapType.InternalName, "Honeycomb" ) )
                {
                    int randomRowIndex;
                    int randomCellIndex;
                    while ( totalPoints > numberToSeed )
                    {
                        randomRowIndex = Context.QualityRandom.Next( 0, pointRows.Length );
                        pointRow = pointRows[randomRowIndex];
                        randomCellIndex = Context.QualityRandom.Next( 0, pointRow.Length );
                        point = pointRow[randomCellIndex];
                        if ( point.X == 0 && point.Y == 0 )
                            continue;
                        pointRow[randomCellIndex] = ArcenPoint.ZeroZeroPoint;
                        totalPoints--;
                    }
                }
                else
                {
                    int numberToRemove = totalPoints - numberToSeed;
                    int numberToRemoveFromTop = numberToRemove / 2;
                    int numberToRemoveFromBottom = numberToRemoveFromTop;
                    if ( numberToRemoveFromTop + numberToRemoveFromBottom < numberToRemove )
                        numberToRemoveFromTop++;
                    totalPoints -= HoneycombHelper_RemovePointsFromTopOrBottom( pointRows, centralRowIndex, numberToRemoveFromTop, true );
                    totalPoints -= HoneycombHelper_RemovePointsFromTopOrBottom( pointRows, centralRowIndex, numberToRemoveFromBottom, false );
                }
            }

            //5) place planets at all points
            Planet[][] planetRows = new Planet[numberOfRows][];
            Planet[] planetRow;
            for ( int rowIndex = 0; rowIndex < pointRows.Length; rowIndex++ )
            {
                pointRow = pointRows[rowIndex];
                planetRows[rowIndex] = planetRow = new Planet[pointRow.Length];
                for ( int columnIndex = 0; columnIndex < pointRow.Length; columnIndex++ )
                {
                    point = pointRow[columnIndex];
                    if ( point.X == 0 && point.Y == 0 )
                        continue;
                    planetRow[columnIndex] = galaxy.AddPlanet( planetType, point, Context );
                }
            }

            //6) for each row:
            Planet cellPlanet;
            Planet[] nextPlanetRow;
            Planet planetToLink;
            if ( ArcenStrings.Equals( mapType.InternalName, "SolarSnake" ) )
            {
                Planet lastPlanet = null;
                bool goingRight = true;
                for ( int rowIndex = 0; rowIndex < planetRows.Length; rowIndex++ )
                {
                    planetRow = planetRows[rowIndex];
                    if ( goingRight )
                    {
                        for ( int columnIndex = 0; columnIndex < planetRow.Length; columnIndex++ )
                        {
                            cellPlanet = planetRow[columnIndex];
                            if ( cellPlanet == null )
                                continue;
                            if ( lastPlanet != null )
                                lastPlanet.AddLinkTo( cellPlanet );
                            lastPlanet = cellPlanet;
                        }
                    }
                    else
                    {
                        for ( int columnIndex = planetRow.Length - 1; columnIndex >= 0; columnIndex-- )
                        {
                            cellPlanet = planetRow[columnIndex];
                            if ( cellPlanet == null )
                                continue;
                            if ( lastPlanet != null )
                                lastPlanet.AddLinkTo( cellPlanet );
                            lastPlanet = cellPlanet;
                        }
                    }
                    goingRight = !goingRight;
                }
            }
            else
            {
                for ( int rowIndex = 0; rowIndex < planetRows.Length; rowIndex++ )
                {
                    planetRow = planetRows[rowIndex];
                    //-- for each cell:
                    for ( int columnIndex = 0; columnIndex < planetRow.Length; columnIndex++ )
                    {
                        cellPlanet = planetRow[columnIndex];
                        if ( cellPlanet == null )
                            continue;
                        //--- connect to the next cell
                        if ( columnIndex + 1 < planetRow.Length )
                        {
                            planetToLink = planetRow[columnIndex + 1];
                            if ( planetToLink != null )
                                cellPlanet.AddLinkTo( planetToLink );
                        }
                        if ( rowIndex + 1 < planetRows.Length )
                        {
                            nextPlanetRow = planetRows[rowIndex + 1];
                            //--- if row below has a cell with the same index, connect to it
                            if ( columnIndex < nextPlanetRow.Length )
                            {
                                planetToLink = nextPlanetRow[columnIndex];
                                if ( planetToLink != null )
                                    cellPlanet.AddLinkTo( planetToLink );
                            }
                            if ( rowIndex < centralRowIndex )
                            {
                                //--- if row below has a cell with the same index + 1, connect to it
                                if ( columnIndex + 1 < nextPlanetRow.Length )
                                {
                                    planetToLink = nextPlanetRow[columnIndex + 1];
                                    if ( planetToLink != null )
                                        cellPlanet.AddLinkTo( planetToLink );
                                }
                            }
                            else
                            {
                                if ( columnIndex > 0 && columnIndex - 1 < nextPlanetRow.Length )
                                {
                                    planetToLink = nextPlanetRow[columnIndex - 1];
                                    if ( planetToLink != null )
                                        cellPlanet.AddLinkTo( planetToLink );
                                }
                            }
                        }
                    }
                }
            }

            Planet firstPlanet = galaxy.Planets[0];

            List<Planet> planetsNotFoundInCurrentSearch = new List<Planet>();
            List<Planet> planetsFoundInCurrentSearch = new List<Planet>();
            bool needToCheck = true;
            Planet planetToCheck;
            int lastUnconnected = -1;
            while ( needToCheck )
            {
                needToCheck = false;
                planetsNotFoundInCurrentSearch.Clear();
                planetsFoundInCurrentSearch.Clear();

                planetsNotFoundInCurrentSearch.AddRange( galaxy.Planets );

                planetsFoundInCurrentSearch.Add( firstPlanet );
                planetsNotFoundInCurrentSearch.Remove( firstPlanet );

                for ( int i = 0; i < planetsFoundInCurrentSearch.Count; i++ )
                {
                    planetToCheck = planetsFoundInCurrentSearch[i];
                    planetToCheck.DoForLinkedNeighbors( delegate ( Planet linkedPlanet )
                    {
                        if ( planetsFoundInCurrentSearch.Contains( linkedPlanet ) )
                            return DelReturn.Continue;
                        planetsFoundInCurrentSearch.Add( linkedPlanet );
                        planetsNotFoundInCurrentSearch.Remove( linkedPlanet );
                        return DelReturn.Continue;
                    } );
                }

                if ( planetsNotFoundInCurrentSearch.Count > 0 )
                {
                    needToCheck = true;
                    bool suppressCrossoverAvoidance = false;
                    if ( lastUnconnected > 0 && lastUnconnected == planetsNotFoundInCurrentSearch.Count )
                        suppressCrossoverAvoidance = true;
                    lastUnconnected = planetsNotFoundInCurrentSearch.Count;
                    UtilityMethods.Helper_ConnectPlanetLists( planetsFoundInCurrentSearch, planetsNotFoundInCurrentSearch, suppressCrossoverAvoidance, false );
                }
            }
        }

        private int HoneycombHelper_RemovePointsFromTopOrBottom( ArcenPoint[][] pointRows, int centralRowIndex, int numberToRemoveFromTop, bool testingTop )
        {
            int numberRemoved = 0;
            ArcenPoint[] pointRow;
            int rowOffset = 0;
            int columnOffset = 0;
            bool testingLeft = true;
            while ( numberToRemoveFromTop > 0 )
            {
                if ( rowOffset >= centralRowIndex )
                    break;
                int rowIndex = testingTop ? rowOffset : pointRows.Length - 1 - rowOffset;
                pointRow = pointRows[rowIndex];
                int length = pointRow.Length;
                int columnIndex = testingLeft ? columnOffset : length - 1 - columnOffset;
                if ( pointRow[columnIndex] != ArcenPoint.ZeroZeroPoint )
                {
                    pointRow[columnIndex] = ArcenPoint.ZeroZeroPoint;
                    numberToRemoveFromTop--;
                    numberRemoved++;
                }
                if ( testingLeft )
                    testingLeft = false;
                else
                {
                    testingLeft = true;
                    columnOffset++;
                    FInt maxOffset = (FInt)length / 2;
                    if ( columnOffset > maxOffset )
                    {
                        rowOffset++;
                        columnOffset = 0;
                        continue;
                    }
                }
            }
            return numberRemoved;
        }

        private readonly int[] maxCellsByRingCount = new int[] { 0, 1, 7, 19, 37, 61, 91, 127 };
        private ArcenPoint[] Helper_GetHexagonalCoordinatesForRow( int CellCount, int RowHeight, int DistanceBetweenPoints, int StartingX )
        {
            ArcenPoint[] result = new ArcenPoint[CellCount];

            int pointX = StartingX;

            for ( int i = 0; i < CellCount; i++ )
            {
                result[i] = ArcenPoint.Create( pointX, RowHeight );
                pointX += DistanceBetweenPoints;
            }

            return result;
        }
    }

    //public class Mapgen_SolarSnake : Mapgen_Honeycomb, IMapGenerator
    //{
    //    public override void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
    //    {
    //        numberToSeed = 20;
    //        PlanetType planetType = PlanetType.Star;

    //        string mapName = mapType.InternalName;

    //        base.InnerGenerate( galaxy, Context, numberToSeed, planetType, mapType );

    //        for ( int i = 0; i < galaxy.Planets.Count; i++ )
    //        {
    //            Planet planet = galaxy.Planets[i];
    //            planet.StarOnly_Design = SolarSystemDesignTable.Instance.Rows[Context.QualityRandom.Next( 0, SolarSystemDesignTable.Instance.Rows.Count )];
    //            planet.StarOnly_RemainingPlanetDesigns.AddRange( planet.StarOnly_Design.Planets );
    //            planet.StarOnly_Arrangement = (SolarSystemArrangementType)Context.QualityRandom.Next( 1, (int)SolarSystemArrangementType.Length );
    //        }

    //        #region build list of connections
    //        List<RefPair<Planet, Planet>> connections = new List<RefPair<Planet, Planet>>();
    //        for ( int i = 0; i < galaxy.Planets.Count; i++ )
    //        {
    //            Planet planet = galaxy.Planets[i];
    //            planet.DoForLinkedNeighbors( delegate ( Planet neighbor )
    //            {
    //                Planet left;
    //                Planet right;
    //                if ( planet.PlanetIndex < neighbor.PlanetIndex )
    //                {
    //                    left = planet;
    //                    right = neighbor;
    //                }
    //                else
    //                {
    //                    left = neighbor;
    //                    right = planet;
    //                }
    //                for ( int j = 0; j < connections.Count; j++ )
    //                {
    //                    RefPair<Planet, Planet> pair = connections[j];
    //                    if ( pair.LeftItem != left )
    //                        continue;
    //                    if ( pair.RightItem != right )
    //                        continue;
    //                    return DelReturn.Continue;
    //                }
    //                connections.Add( RefPair<Planet, Planet>.Create( left, right ) );
    //                return DelReturn.Continue;
    //            } );
    //        }
    //        #endregion
    //        {
    //            #region remove a portion of connections, if indicated by map type
    //            FInt connectionsToRemoveRatio = FInt.Zero;
    //            if ( ArcenStrings.Equals( mapName, "SolarSimple" ) )
    //                connectionsToRemoveRatio = FInt.FromParts( 0, 500 );
    //            else if ( ArcenStrings.Equals( mapName, "SolarWide" ) )
    //                connectionsToRemoveRatio = FInt.FromParts( 0, 200 );
    //            else if ( ArcenStrings.Equals( mapName, "SolarMaze" ) )
    //                connectionsToRemoveRatio = FInt.FromParts( 0, 800 );
    //            if ( connectionsToRemoveRatio > FInt.Zero )
    //            {
    //                int connectionsToRemove = ( connections.Count * connectionsToRemoveRatio ).GetNearestIntPreferringLower();
    //                List<RefPair<Planet, Planet>> removableConnections = new List<RefPair<Planet, Planet>>();
    //                List<Planet> planetsInFlood = new List<Planet>();
    //                for ( int i = 0; i < connectionsToRemove; i++ )
    //                {
    //                    removableConnections.Clear();
    //                    for ( int j = 0; j < connections.Count; j++ )
    //                    {
    //                        RefPair<Planet, Planet> pair = connections[j];
    //                        planetsInFlood.Clear();
    //                        galaxy.Planets[0].DoForPlanetsWithinXHops( Context, -1, pair.LeftItem, pair.RightItem, delegate ( Planet planet, int Distance )
    //                        {
    //                            if ( planetsInFlood.Contains( planet ) )
    //                                return DelReturn.Continue;
    //                            planetsInFlood.Add( planet );
    //                            return DelReturn.Continue;
    //                        }, null );
    //                        if ( planetsInFlood.Count < galaxy.Planets.Count )
    //                            continue;
    //                        removableConnections.Add( pair );
    //                    }
    //                    if ( removableConnections.Count <= 0 )
    //                        break;
    //                    RefPair<Planet, Planet> connectionToRemove = removableConnections[Context.QualityRandom.Next( 0, removableConnections.Count )];
    //                    removableConnections.Remove( connectionToRemove );
    //                    connections.Remove( connectionToRemove );
    //                    connectionToRemove.LeftItem.RemoveLinkTo( connectionToRemove.RightItem );
    //                }
    //            }
    //            #endregion
    //        }
    //        #region seed planets to function as wormhole connections to other stars
    //        planetType = PlanetType.Normal;
    //        for ( int i = 0; i < connections.Count; i++ )
    //        {
    //            RefPair<Planet, Planet> connection = connections[i];
    //            AngleDegrees angle = connection.LeftItem.GalaxyLocation.GetAngleToDegrees( connection.RightItem.GalaxyLocation );
    //            Planet leftConnectionPlanet = connection.LeftItem;
    //            Planet rightConnectionPlanet = connection.RightItem;
    //            for ( int j = connection.LeftItem.StarOnly_RemainingPlanetDesigns.Count - 1; j >= 0; j-- )
    //            {
    //                PlanetDesign design = connection.LeftItem.StarOnly_RemainingPlanetDesigns[j];
    //                ArcenPoint proposedPoint = connection.LeftItem.GalaxyLocation.GetPointAtAngleAndDistance( angle, design.OrbitalRadius );
    //                if ( galaxy.CheckForOverlapWithExistingLines( leftConnectionPlanet, proposedPoint, rightConnectionPlanet, rightConnectionPlanet.GalaxyLocation, true, true ) )
    //                    continue;
    //                Planet newPlanet = galaxy.AddPlanet( planetType, proposedPoint, Context );
    //                connection.LeftItem.StarOnly_AddPlanet( newPlanet );
    //                connection.LeftItem.StarOnly_RemainingPlanetDesigns.RemoveAt( j );
    //                leftConnectionPlanet = newPlanet;
    //                break;
    //            }
    //            if ( leftConnectionPlanet == connection.LeftItem )
    //            {
    //                Planet bestPlanet = null;
    //                FInt bestAngleAbsoluteDelta = FInt.Zero;
    //                connection.LeftItem.StarOnly_DoForPlanets( delegate ( Planet planet )
    //                {
    //                    AngleDegrees thisAngle = connection.LeftItem.GalaxyLocation.GetAngleToDegrees( planet.GalaxyLocation );
    //                    FInt angleAbsoluteDelta = thisAngle.GetAbsoluteDeltaNeededToGetToOther( angle );
    //                    if ( bestPlanet != null && thisAngle.GetAbsoluteDeltaNeededToGetToOther( angle ) >= bestAngleAbsoluteDelta )
    //                        return DelReturn.Continue;
    //                    if ( galaxy.CheckForOverlapWithExistingLines( planet, planet.GalaxyLocation, rightConnectionPlanet, rightConnectionPlanet.GalaxyLocation, true, true ) )
    //                        return DelReturn.Continue;
    //                    bestPlanet = planet;
    //                    bestAngleAbsoluteDelta = angleAbsoluteDelta;
    //                    return DelReturn.Continue;
    //                } );
    //                if ( bestPlanet != null )
    //                    leftConnectionPlanet = bestPlanet;
    //            }

    //            angle = connection.RightItem.GalaxyLocation.GetAngleToDegrees( leftConnectionPlanet.GalaxyLocation );
    //            for ( int j = connection.RightItem.StarOnly_RemainingPlanetDesigns.Count - 1; j >= 0; j-- )
    //            {
    //                PlanetDesign design = connection.RightItem.StarOnly_RemainingPlanetDesigns[j];
    //                ArcenPoint proposedPoint = connection.RightItem.GalaxyLocation.GetPointAtAngleAndDistance( angle, design.OrbitalRadius );
    //                if ( galaxy.CheckForOverlapWithExistingLines( rightConnectionPlanet, proposedPoint, leftConnectionPlanet, leftConnectionPlanet.GalaxyLocation, true, true ) )
    //                    continue;
    //                Planet newPlanet = galaxy.AddPlanet( planetType, proposedPoint, Context );
    //                connection.RightItem.StarOnly_AddPlanet( newPlanet );
    //                connection.RightItem.StarOnly_RemainingPlanetDesigns.RemoveAt( j );
    //                rightConnectionPlanet = newPlanet;
    //                break;
    //            }
    //            if ( rightConnectionPlanet == connection.RightItem )
    //            {
    //                Planet bestPlanet = null;
    //                FInt bestAngleAbsoluteDelta = FInt.Zero;
    //                connection.RightItem.StarOnly_DoForPlanets( delegate ( Planet planet )
    //                {
    //                    AngleDegrees thisAngle = connection.RightItem.GalaxyLocation.GetAngleToDegrees( planet.GalaxyLocation );
    //                    FInt angleAbsoluteDelta = thisAngle.GetAbsoluteDeltaNeededToGetToOther( angle );
    //                    if ( bestPlanet != null && thisAngle.GetAbsoluteDeltaNeededToGetToOther( angle ) >= bestAngleAbsoluteDelta )
    //                        return DelReturn.Continue;
    //                    if ( galaxy.CheckForOverlapWithExistingLines( planet, planet.GalaxyLocation, leftConnectionPlanet, leftConnectionPlanet.GalaxyLocation, true, true ) )
    //                        return DelReturn.Continue;
    //                    bestPlanet = planet;
    //                    bestAngleAbsoluteDelta = angleAbsoluteDelta;
    //                    return DelReturn.Continue;
    //                } );
    //                if ( bestPlanet != null )
    //                    rightConnectionPlanet = bestPlanet;
    //            }

    //            if ( leftConnectionPlanet != connection.LeftItem ||
    //                 rightConnectionPlanet != connection.RightItem )
    //            {
    //                connection.LeftItem.RemoveLinkTo( connection.RightItem );
    //                leftConnectionPlanet.AddLinkTo( rightConnectionPlanet );
    //            }

    //            if ( leftConnectionPlanet != connection.LeftItem && !leftConnectionPlanet.GetIsDirectlyLinkedTo( connection.LeftItem ) )
    //                connection.LeftItem.AddLinkTo( leftConnectionPlanet );
    //            if ( rightConnectionPlanet != connection.RightItem && !rightConnectionPlanet.GetIsDirectlyLinkedTo( connection.RightItem ) )
    //                connection.RightItem.AddLinkTo( rightConnectionPlanet );
    //        }
    //        #endregion
    //        #region seed planets that stars should have, but were not needed to establish the wormhole connections
    //        {
    //            for ( int i = 0; i < galaxy.Planets.Count; i++ )
    //            {
    //                Planet star = galaxy.Planets[i];
    //                for ( int j = star.StarOnly_RemainingPlanetDesigns.Count - 1; j >= 0; j-- )
    //                {
    //                    PlanetDesign design = star.StarOnly_RemainingPlanetDesigns[j];
    //                    int hitsLeft = 100;
    //                    while ( hitsLeft > 0 )
    //                    {
    //                        hitsLeft--;
    //                        ArcenPoint proposedPoint = star.GalaxyLocation.GetRandomPointWithinDistance( Context.QualityRandom, design.OrbitalRadius, design.OrbitalRadius );
    //                        if ( galaxy.CheckForTooCloseToExistingLines( proposedPoint, planetType, true ) )
    //                            continue;
    //                        Planet newPlanet = galaxy.AddPlanet( planetType, proposedPoint, Context );
    //                        star.StarOnly_AddPlanet( newPlanet );
    //                        star.StarOnly_RemainingPlanetDesigns.RemoveAt( j );
    //                        star.AddLinkTo( newPlanet );
    //                        break;
    //                    }
    //                }
    //            }
    //        }
    //        #endregion
    //        #region re-arrange intra-solar-system connections
    //        {
    //            for ( int i = 0; i < galaxy.Planets.Count; i++ )
    //            {
    //                Planet star = galaxy.Planets[i];
    //                if ( star.StarOnly_Arrangement == SolarSystemArrangementType.None )
    //                    continue;
    //                switch ( star.StarOnly_Arrangement )
    //                {
    //                    case SolarSystemArrangementType.SolarHub:
    //                        // default map logic already has it set up this way
    //                        break;
    //                    #region SolarSnake
    //                    case SolarSystemArrangementType.SolarSnake:
    //                        {
    //                            List<Planet> planetsToLink_All = new List<Planet>();
    //                            star.StarOnly_DoForPlanets( delegate ( Planet planet )
    //                            {
    //                                star.RemoveLinkTo( planet );
    //                                planetsToLink_All.Add( planet );
    //                                return DelReturn.Continue;
    //                            } );
    //                            Planet planetToLinkFrom = star;
    //                            while ( planetsToLink_All.Count > 0 )
    //                            {
    //                                Planet bestPlanet = null;
    //                                int bestDistance = 0;
    //                                for ( int j = 0; j < planetsToLink_All.Count; j++ )
    //                                {
    //                                    Planet planet = planetsToLink_All[j];
    //                                    int distance = planetToLinkFrom.GalaxyLocation.GetDistanceTo( planet.GalaxyLocation, false );
    //                                    if ( bestPlanet != null && distance >= bestDistance )
    //                                        continue;
    //                                    if ( galaxy.CheckForOverlapWithExistingLines( planetToLinkFrom, planetToLinkFrom.GalaxyLocation, planet, planet.GalaxyLocation, true, true ) )
    //                                        continue;
    //                                    bestPlanet = planet;
    //                                    bestDistance = distance;
    //                                }
    //                                if ( bestPlanet == null )
    //                                {
    //                                    for ( int j = 0; j < planetsToLink_All.Count; j++ )
    //                                    {
    //                                        Planet planet = planetsToLink_All[j];
    //                                        if ( planet.StarOnly_Arrangement == SolarSystemArrangementType.None )
    //                                            planet.AddLinkTo( star );
    //                                    }
    //                                    break;
    //                                }
    //                                planetToLinkFrom.AddLinkTo( bestPlanet );
    //                                planetToLinkFrom = bestPlanet;
    //                                planetsToLink_All.Remove( bestPlanet );
    //                            }
    //                        }
    //                        break;
    //                    #endregion
    //                    case SolarSystemArrangementType.PlanetRing:
    //                        {
    //                            List<Planet> planetsToLink_All = new List<Planet>();
    //                            Planet planetClosestToStar = null;
    //                            {
    //                                int distance = 0;
    //                                star.StarOnly_DoForPlanets( delegate ( Planet planet )
    //                                {
    //                                    star.RemoveLinkTo( planet );
    //                                    planetsToLink_All.Add( planet );
    //                                    int thisDistance = planet.GalaxyLocation.GetDistanceTo( star.GalaxyLocation, false );
    //                                    if ( planetClosestToStar == null || distance > thisDistance )
    //                                    {
    //                                        planetClosestToStar = planet;
    //                                        distance = thisDistance;
    //                                    }
    //                                    return DelReturn.Continue;
    //                                } );
    //                            }
    //                            planetsToLink_All.Sort( delegate ( Planet left, Planet right )
    //                            {
    //                                return star.GalaxyLocation.GetAngleToDegrees( left.GalaxyLocation ).CompareTo( star.GalaxyLocation.GetAngleToDegrees( right.GalaxyLocation ) );
    //                            } );
    //                            Planet firstPlanet = null;
    //                            Planet lastPlanet = null;
    //                            for ( int j = 0; j < planetsToLink_All.Count; j++ )
    //                            {
    //                                Planet planet = planetsToLink_All[j];
    //                                if ( firstPlanet == null )
    //                                    firstPlanet = planet;
    //                                else
    //                                    lastPlanet.AddLinkTo( planet );
    //                                lastPlanet = planet;
    //                            }
    //                            if ( lastPlanet != null )
    //                                lastPlanet.AddLinkTo( firstPlanet );
    //                            if ( planetClosestToStar != null )
    //                                planetClosestToStar.AddLinkTo( star );
    //                        }
    //                        break;
    //                    case SolarSystemArrangementType.RandomConnectionsSmall:
    //                    case SolarSystemArrangementType.RandomConnectionsLarge:
    //                    case SolarSystemArrangementType.MaxConnections:
    //                        {
    //                            List<Planet> planetsToLink_All = new List<Planet>();
    //                            star.StarOnly_DoForPlanets( delegate ( Planet planet )
    //                            {
    //                                star.RemoveLinkTo( planet );
    //                                planetsToLink_All.Add( planet );
    //                                return DelReturn.Continue;
    //                            } );
    //                            planetsToLink_All.Add( star );

    //                            int numberOfConnections = planetsToLink_All.Count;

    //                            switch ( star.StarOnly_Arrangement )
    //                            {
    //                                case SolarSystemArrangementType.RandomConnectionsSmall:
    //                                    numberOfConnections /= 2;
    //                                    break;
    //                                case SolarSystemArrangementType.RandomConnectionsLarge:
    //                                    numberOfConnections *= 1;
    //                                    break;
    //                                case SolarSystemArrangementType.MaxConnections:
    //                                    numberOfConnections *= 10;
    //                                    break;
    //                            }

    //                            for ( int cIndex = 0; cIndex < numberOfConnections; cIndex++ )
    //                            {
    //                                Planet bestLeftPlanet = null;
    //                                Planet bestRightPlanet = null;
    //                                int bestDistance = 0;
    //                                for ( int j = 0; j < planetsToLink_All.Count; j++ )
    //                                {
    //                                    Planet planet = planetsToLink_All[j];
    //                                    for ( int k = j + 1; k < planetsToLink_All.Count; k++ )
    //                                    {
    //                                        Planet otherPlanet = planetsToLink_All[k];
    //                                        if ( planet.GetIsDirectlyLinkedTo( otherPlanet ) )
    //                                            continue;
    //                                        int distance = planet.GalaxyLocation.GetDistanceTo( otherPlanet.GalaxyLocation, false );
    //                                        if ( bestLeftPlanet != null && distance >= bestDistance )
    //                                            continue;
    //                                        if ( galaxy.CheckForOverlapWithExistingLines( planet, planet.GalaxyLocation, otherPlanet, otherPlanet.GalaxyLocation, true, true ) )
    //                                            continue;
    //                                        bestLeftPlanet = planet;
    //                                        bestRightPlanet = otherPlanet;
    //                                        bestDistance = distance;
    //                                    }
    //                                }
    //                                if ( bestLeftPlanet == null )
    //                                    break;
    //                                bestLeftPlanet.AddLinkTo( bestRightPlanet );
    //                            }

    //                            List<Planet> planetsInSystemConnectedToStar = new List<Planet>();
    //                            List<Planet> planetsInSystemStillNeedingConnectionToStar = new List<Planet>();
    //                            planetsInSystemStillNeedingConnectionToStar.AddRange( planetsToLink_All );
    //                            star.DoForPlanetsWithinXHops( Context, planetsToLink_All.Count, delegate ( Planet planet, int Distance )
    //                            {
    //                                if ( planet != star && planet.PlanetOnly_StarIndex != star.PlanetIndex )
    //                                    return DelReturn.Continue;
    //                                planetsInSystemStillNeedingConnectionToStar.Remove( planet );
    //                                planetsInSystemConnectedToStar.Add( planet );
    //                                return DelReturn.Break;
    //                            } );
    //                            for ( int j = 0; j < planetsInSystemStillNeedingConnectionToStar.Count; j++ )
    //                            {
    //                                Planet planet = planetsInSystemStillNeedingConnectionToStar[j];
    //                                Planet bestPlanet = null;
    //                                int bestDistance = 0;
    //                                for ( int k = 0; k < planetsInSystemConnectedToStar.Count; k++ )
    //                                {
    //                                    Planet otherPlanet = planetsInSystemConnectedToStar[k];
    //                                    int distance = planet.GalaxyLocation.GetDistanceTo( otherPlanet.GalaxyLocation, false );
    //                                    if ( bestPlanet != null && distance >= bestDistance )
    //                                        continue;
    //                                    if ( galaxy.CheckForOverlapWithExistingLines( planet, planet.GalaxyLocation, otherPlanet, otherPlanet.GalaxyLocation, true, true ) )
    //                                        continue;
    //                                    bestPlanet = otherPlanet;
    //                                    bestDistance = distance;
    //                                }
    //                                if ( bestPlanet == null )
    //                                    bestPlanet = star;
    //                                planet.AddLinkTo( bestPlanet );
    //                                planetsInSystemConnectedToStar.Add( planet );
    //                            }
    //                        }
    //                        break;
    //                }
    //            }
    //        }
    //        #endregion
    //    }
    //}

    //public class Mapgen_SolarSimple : IMapGenerator
    //{
    //    public void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
    //    {

    //    }
    //}

    //public class Mapgen_SolarWide : IMapGenerator
    //{
    //    public void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
    //    {

    //    }
    //}

    //public class Mapgen_SolarMaze : IMapGenerator
    //{
    //    public void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
    //    {

    //    }
    //}

    public class RootLatticeTypeGenerator : Mapgen_Base
    {
        public override void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
        {
            int noise = 0;
            int numberOfExtraConnections = 0;
            bool interconnectEverything = false;
            bool doAxialConnections = true;
            bool isMaze = false;
            bool maze_EasyMode = false;
            bool maze_Angles = false;
            string mapName = mapType.InternalName;
            if ( ArcenStrings.Equals( mapName, "Lattice" ) )
            {
                noise = 10;
                numberOfExtraConnections = numberToSeed / 4;
            }
            else if ( ArcenStrings.Equals( mapName, "Crosshatch" ) )
            {
                interconnectEverything = true;
            }
            else if ( ArcenStrings.StartsWith( mapName, "Maze" ) )
            {
                doAxialConnections = false;
                isMaze = true;
                if ( ArcenStrings.Equals( mapName, "MazeA" ) )
                { }
                else if ( ArcenStrings.Equals( mapName, "MazeB" ) )
                {
                    maze_EasyMode = true;
                }
                else if ( ArcenStrings.Equals( mapName, "MazeC" ) )
                {
                    maze_Angles = true;
                }
                else if ( ArcenStrings.Equals( mapName, "MazeD" ) )
                {
                    maze_Angles = true;
                    maze_EasyMode = true;
                }
            }
            else if ( ArcenStrings.Equals( mapName, "Grid" ) )
            {
            }

            int spacing = 80;
            int squareLength = (int)Math.Ceiling( (float)Math.Sqrt( numberToSeed ) );
            ArcenRectangle seedingRect;
            seedingRect.Width = ( squareLength - 1 ) * spacing;
            seedingRect.Height = seedingRect.Width;
            seedingRect.X = Engine_AIW2.GalaxyCenter.X - ( seedingRect.Width / 2 );
            seedingRect.Y = Engine_AIW2.GalaxyCenter.Y - ( seedingRect.Height / 2 );

            Planet[,] planets = new Planet[squareLength, squareLength];
            for ( int spotX = 0; spotX < squareLength; spotX++ )
            {
                for ( int spotY = 0; spotY < squareLength; spotY++ )
                {
                    ArcenPoint nextPoint;
                    nextPoint.X = seedingRect.X + ( spotX * spacing );
                    nextPoint.Y = seedingRect.Y + ( spotY * spacing );

                    if ( noise > 0 )
                    {
                        nextPoint.X += Context.QualityRandom.Next( -noise, noise );
                        nextPoint.Y += Context.QualityRandom.Next( -noise, noise );
                    }

                    PlanetType planetType = PlanetType.Normal;

                    Planet nextPlanet = galaxy.AddPlanet( planetType, nextPoint, Context );
                    planets[spotX, spotY] = nextPlanet;
                    nextPlanet.Mapgen_WorkingGridX = spotX;
                    nextPlanet.Mapgen_WorkingGridY = spotY;
                    if ( doAxialConnections )
                    {
                        if ( spotX > 0 )
                        {
                            nextPlanet.AddLinkTo( planets[spotX - 1, spotY] );
                            if ( interconnectEverything )
                            {
                                if ( spotY > 0 )
                                    nextPlanet.AddLinkTo( planets[spotX - 1, spotY - 1] );
                                if ( spotY < squareLength - 1 )
                                    nextPlanet.AddLinkTo( planets[spotX - 1, spotY + 1] );
                            }
                        }
                        if ( spotY > 0 )
                            nextPlanet.AddLinkTo( planets[spotX, spotY - 1] );
                    }
                }
            }

            if ( numberOfExtraConnections > 0 )
            {
                List<Planet> planetsWithExtraConnection = new List<Planet>();
                for ( int i = 0; i < numberOfExtraConnections; i++ )
                {
                    Planet bestPlanet1 = null;
                    Planet bestPlanet2 = null;
                    int bestDistance = 0;
                    for ( int j = 0; j < galaxy.Planets.Count; j++ )
                    {
                        Planet planet = galaxy.Planets[j];
                        if ( planetsWithExtraConnection.Contains( planet ) )
                            continue;
                        for ( int k = 0; k < galaxy.Planets.Count; k++ )
                        {
                            if ( k == j )
                                continue;
                            Planet otherPlanet = galaxy.Planets[k];
                            if ( planetsWithExtraConnection.Contains( otherPlanet ) )
                                continue;
                            if ( planet.GetIsDirectlyLinkedTo( otherPlanet ) )
                                continue;
                            int distance = planet.GalaxyLocation.GetDistanceTo( otherPlanet.GalaxyLocation, false );
                            if ( bestPlanet1 != null && distance > bestDistance )
                                continue;
                            bestPlanet1 = planet;
                            bestPlanet2 = otherPlanet;
                            bestDistance = distance;
                        }
                    }
                    if ( bestPlanet1 == null )
                        break;
                    bestPlanet1.AddLinkTo( bestPlanet2 );
                    planetsWithExtraConnection.Add( bestPlanet1 );
                    planetsWithExtraConnection.Add( bestPlanet2 );
                }
            }

            if ( isMaze )
                RenderMethodMazeRecursiveBacktracker( Context, galaxy, planets, maze_EasyMode, maze_Angles );
        }


        #region RenderMethodMazeRecursiveBacktracker
        private static void RenderMethodMazeRecursiveBacktracker( ArcenSimContext Context, Galaxy galaxy, Planet[,] planets, bool IncludeRandomExtras, bool IncludeAngles )
        {
            int numberPerRowCol = planets.GetLength( 0 );
            bool[,] visited = new bool[numberPerRowCol, numberPerRowCol];

            int startingX = Context.QualityRandom.Next( 0, numberPerRowCol );
            int startingY = Context.QualityRandom.Next( 0, numberPerRowCol );
            RenderMethodMazeRecursiveBacktracker_AddCell( Context, startingX, startingY, planets, visited, numberPerRowCol, null, IncludeAngles );

            if ( IncludeRandomExtras )
            {
                int numberRandomExtras = galaxy.Planets.Count / 10;
                if ( numberRandomExtras < 1 )
                    numberRandomExtras = 1;

                for ( int i = 1; i < numberRandomExtras; i++ )
                {
                    Planet planet = galaxy.Planets[Context.QualityRandom.Next( 0, galaxy.Planets.Count )];
                    FillWorkingAdjacentPlanetsGrid( planet, planets, visited, numberPerRowCol, true, false, IncludeAngles );
                    if ( workingPlanets.Count > 0 )
                        planet.AddLinkTo( workingPlanets[Context.QualityRandom.Next( 0, workingPlanets.Count )] );
                    else
                        i--;
                }
            }
        }

        private static void RenderMethodMazeRecursiveBacktracker_AddCell( ArcenSimContext Context, int CellX, int CellY,
            Planet[,] planets, bool[,] visited, int numberPerRowCol, Planet PriorPlanet, bool IncludeAngles )
        {
            if ( PriorPlanet != null )
                planets[CellX, CellY].AddLinkTo( PriorPlanet );
            visited[CellX, CellY] = true;
            FillWorkingAdjacentPlanetsGrid( planets[CellX, CellY], planets, visited, numberPerRowCol, false, false, IncludeAngles );

            if ( workingPlanets.Count > 0 )
            {
                Planet newPlanet = workingPlanets[Context.QualityRandom.Next( 0, workingPlanets.Count )];
                RenderMethodMazeRecursiveBacktracker_AddCell( Context, newPlanet.Mapgen_WorkingGridX, newPlanet.Mapgen_WorkingGridY, planets,
                    visited, numberPerRowCol, planets[CellX, CellY], IncludeAngles );
                //now we backtrack to this method
                RenderMethodMazeRecursiveBacktracker_AddCell( Context, CellX, CellY, planets, visited, numberPerRowCol, null, IncludeAngles );
            }
        }
        #endregion

        #region FillWorkingAdjacentPlanetsGrid
        private static readonly List<Planet> workingPlanets = new List<Planet>();
        private static void FillWorkingAdjacentPlanetsGrid( Planet planet, Planet[,] planets, bool[,] visited, int numberPerRowCol,
            bool CareAboutLinksInsteadOfVisited, bool WantAlreadyVisited, bool IncludeAngles )
        {
            workingPlanets.Clear();
            if ( planet.Mapgen_WorkingGridX > 0 )
            {
                if ( CareAboutLinksInsteadOfVisited )
                {
                    if ( !planet.GetIsDirectlyLinkedTo( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY] ) )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY] );
                }
                else
                {
                    if ( visited[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY] == WantAlreadyVisited )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY] );
                }
            }
            if ( planet.Mapgen_WorkingGridY > 0 )
            {
                if ( CareAboutLinksInsteadOfVisited )
                {
                    if ( !planet.GetIsDirectlyLinkedTo( planets[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY - 1] ) )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY - 1] );
                }
                else
                {
                    if ( visited[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY - 1] == WantAlreadyVisited )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY - 1] );
                }
            }
            if ( planet.Mapgen_WorkingGridX + 1 < numberPerRowCol )
            {
                if ( CareAboutLinksInsteadOfVisited )
                {
                    if ( !planet.GetIsDirectlyLinkedTo( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY] ) )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY] );
                }
                else
                {
                    if ( visited[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY] == WantAlreadyVisited )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY] );
                }
            }
            if ( planet.Mapgen_WorkingGridY + 1 < numberPerRowCol )
            {
                if ( CareAboutLinksInsteadOfVisited )
                {
                    if ( !planet.GetIsDirectlyLinkedTo( planets[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY + 1] ) )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY + 1] );
                }
                else
                {
                    if ( visited[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY + 1] == WantAlreadyVisited )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY + 1] );
                }
            }

            if ( IncludeAngles )
            {
                if ( planet.Mapgen_WorkingGridX > 0 )
                {
                    if ( planet.Mapgen_WorkingGridY > 0 )
                    {
                        if ( CareAboutLinksInsteadOfVisited )
                        {
                            if ( !planet.GetIsDirectlyLinkedTo( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY - 1] ) )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY - 1] );
                        }
                        else
                        {
                            if ( visited[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY - 1] == WantAlreadyVisited )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY - 1] );
                        }
                    }
                    if ( planet.Mapgen_WorkingGridY + 1 < numberPerRowCol )
                    {
                        if ( CareAboutLinksInsteadOfVisited )
                        {
                            if ( !planet.GetIsDirectlyLinkedTo( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY + 1] ) )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY + 1] );
                        }
                        else
                        {
                            if ( visited[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY + 1] == WantAlreadyVisited )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY + 1] );
                        }
                    }
                }

                if ( planet.Mapgen_WorkingGridX + 1 < numberPerRowCol )
                {
                    if ( planet.Mapgen_WorkingGridY > 0 )
                    {
                        if ( CareAboutLinksInsteadOfVisited )
                        {
                            if ( !planet.GetIsDirectlyLinkedTo( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY - 1] ) )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY - 1] );
                        }
                        else
                        {
                            if ( visited[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY - 1] == WantAlreadyVisited )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY - 1] );
                        }
                    }
                    if ( planet.Mapgen_WorkingGridY + 1 < numberPerRowCol )
                    {
                        if ( CareAboutLinksInsteadOfVisited )
                        {
                            if ( !planet.GetIsDirectlyLinkedTo( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY + 1] ) )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY + 1] );
                        }
                        else
                        {
                            if ( visited[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY + 1] == WantAlreadyVisited )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY + 1] );
                        }
                    }
                }
            }
        }
        #endregion
    }

    public class Mapgen_Concentric : Mapgen_Base
    {
        public override void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
        {
            int numberOfRings;
            {
                if ( numberToSeed <= 10 )
                    numberOfRings = 1;
                else if ( numberToSeed <= 20 )
                    numberOfRings = 2;
                else if ( numberToSeed <= 30 )
                    numberOfRings = 3;
                else if ( numberToSeed <= 40 )
                    numberOfRings = 3;
                else if ( numberToSeed <= 50 )
                    numberOfRings = Context.QualityRandom.NextWithInclusiveUpperBound( 3, 4 );
                else if ( numberToSeed <= 60 )
                    numberOfRings = Context.QualityRandom.NextWithInclusiveUpperBound( 4, 5 );
                else if ( numberToSeed <= 70 )
                    numberOfRings = Context.QualityRandom.NextWithInclusiveUpperBound( 5, 6 );
                else if ( numberToSeed <= 80 )
                    numberOfRings = Context.QualityRandom.NextWithInclusiveUpperBound( 5, 7 );
                else if ( numberToSeed <= 90 )
                    numberOfRings = Context.QualityRandom.NextWithInclusiveUpperBound( 5, 7 );
                else if ( numberToSeed <= 100 )
                    numberOfRings = Context.QualityRandom.NextWithInclusiveUpperBound( 6, 8 );
                else if ( numberToSeed <= 110 )
                    numberOfRings = Context.QualityRandom.NextWithInclusiveUpperBound( 6, 8 );
                else //if ( numberToSeed <= 120 )
                    numberOfRings = Context.QualityRandom.NextWithInclusiveUpperBound( 6, 8 );
            }

            int ringGapMinimum;
            int ringGapMaximum;

            //if ( numberOfRings >= 7 )
            //{
            //    ringGapMinimum = 38;
            //    ringGapMaximum = 46;
            //}
            //else if ( numberOfRings == 6 )
            //{
            //    ringGapMinimum = 46;
            //    ringGapMaximum = 54;
            //}
            //else if ( numberOfRings == 5 )
            //{
            //    ringGapMinimum = 54;
            //    ringGapMaximum = 62;
            //}
            //else 
            if ( numberOfRings >= 4 )
            {
                ringGapMinimum = 70;
                ringGapMaximum = 85;
            }
            else if ( numberOfRings == 3 )
            {
                ringGapMinimum = 95;
                ringGapMaximum = 110;
            }
            else //if ( numberOfRings <= 2 )
            {
                ringGapMinimum = 120;
                ringGapMaximum = 150;
            }

            // determine number of planets for each ring, totalling NumberOfPlanets - 1
            // basically all rings need at least 8 planets to prevent line crossover,
            // and each ring N has a random chance for up to N+1 extra planets if there are more left
            // after that, if there are still unallocated planets, repeat the random add
            int numberOfPlanetsToAllocate = numberToSeed - 1;
            int[] numberOfPlanetsByRing = new int[numberOfRings];
            {
                for ( int i = 0; i < numberOfRings; i++ )
                {
                    numberOfPlanetsByRing[i] = 8;
                    numberOfPlanetsToAllocate -= numberOfPlanetsByRing[i];
                }

                while ( numberOfPlanetsToAllocate > 0 )
                {
                    for ( int i = numberOfRings - 1; i >= 0; i-- )
                    {
                        if ( numberOfPlanetsToAllocate <= 0 )
                            break;

                        int minimumPlanetsToRandomlyAllocate = 0;
                        int maximumPlanetsToRandomlyAllocate = i + 1;
                        if ( maximumPlanetsToRandomlyAllocate > numberOfPlanetsToAllocate )
                            maximumPlanetsToRandomlyAllocate = numberOfPlanetsToAllocate;

                        int additionalPlanetsToAllocate = Context.QualityRandom.NextWithInclusiveUpperBound( minimumPlanetsToRandomlyAllocate, maximumPlanetsToRandomlyAllocate );
                        numberOfPlanetsByRing[i] += additionalPlanetsToAllocate;
                        numberOfPlanetsToAllocate -= additionalPlanetsToAllocate;
                    }
                }
            }

            bool isSpiral = ArcenStrings.Equals( mapType.InternalName, "Snake" );

            ArcenPoint originPlanetPoint = Engine_AIW2.GalaxyCenter;
            PlanetType planetType = PlanetType.Normal;
            Planet originPlanet = galaxy.AddPlanet( planetType, originPlanetPoint, Context );

            // starting from ring 1 (excludes the origin)
            int ringRadiusFromOrigin = 0;
            List<Planet>[] planetsPlacedByRingThatAreNotConnectedOutsideTheRing = new List<Planet>[numberOfRings];
            for ( int i = 0; i < numberOfRings; i++ )
            {
                ringRadiusFromOrigin += Context.QualityRandom.NextWithInclusiveUpperBound( ringGapMinimum, ringGapMaximum );

                planetsPlacedByRingThatAreNotConnectedOutsideTheRing[i] = new List<Planet>();
                Planet firstPlanetPlacedOnRing = null;
                Planet lastPlanetPlacedOnRing = null;
                int ringPlanetCount = numberOfPlanetsByRing[i];
                for ( int j = 0; j < ringPlanetCount; j++ )
                {
                    AngleDegrees angle = AngleDegrees.Create( ( (FInt)360 / (FInt)ringPlanetCount ) * (FInt)j );

                    ArcenPoint pointOnRing = originPlanetPoint.GetPointAtAngleAndDistance( angle, ringRadiusFromOrigin );

                    Planet planet = galaxy.AddPlanet( planetType, pointOnRing, Context );

                    planetsPlacedByRingThatAreNotConnectedOutsideTheRing[i].Add( planet );

                    if ( lastPlanetPlacedOnRing != null )
                        lastPlanetPlacedOnRing.AddLinkTo( planet );

                    if ( firstPlanetPlacedOnRing == null )
                        firstPlanetPlacedOnRing = planet;

                    lastPlanetPlacedOnRing = planet;
                }

                if ( lastPlanetPlacedOnRing != null && firstPlanetPlacedOnRing != null )
                {
                    if ( !isSpiral )
                        lastPlanetPlacedOnRing.AddLinkTo( firstPlanetPlacedOnRing );
                    else
                    {
                        if ( i == 0 )
                            firstPlanetPlacedOnRing.AddLinkTo( originPlanet );
                        else
                        {
                            List<Planet> previousRing = planetsPlacedByRingThatAreNotConnectedOutsideTheRing[i - 1];                            
                            firstPlanetPlacedOnRing.AddLinkTo( previousRing[previousRing.Count-1] );
                        }
                    }
                }
            }

            if ( !isSpiral )
            {
                // starting from the origin planet (ring 0) to the second-to-last-ring, for each ring N
                if ( planetsPlacedByRingThatAreNotConnectedOutsideTheRing.Length > 0 &&
                    planetsPlacedByRingThatAreNotConnectedOutsideTheRing[0] != null &&
                    planetsPlacedByRingThatAreNotConnectedOutsideTheRing[0].Count > 0 )
                {
                    int indexWithinRingOfPlanetToConnectToOrigin;
                    if ( planetsPlacedByRingThatAreNotConnectedOutsideTheRing[0].Count == 1 )
                        indexWithinRingOfPlanetToConnectToOrigin = 0;
                    else
                        indexWithinRingOfPlanetToConnectToOrigin = Context.QualityRandom.Next( 0, planetsPlacedByRingThatAreNotConnectedOutsideTheRing[0].Count );
                    Planet planetToConnectToOrigin = planetsPlacedByRingThatAreNotConnectedOutsideTheRing[0][indexWithinRingOfPlanetToConnectToOrigin];
                    if ( originPlanet != null && planetToConnectToOrigin != null )
                    {
                        originPlanet.AddLinkTo( planetToConnectToOrigin );
                        planetsPlacedByRingThatAreNotConnectedOutsideTheRing[0].Remove( planetToConnectToOrigin );
                    }
                }
                for ( int i = 0; i < numberOfRings - 1; i++ )
                {
                    List<Planet> eligiblePlanetsOnThisRing = planetsPlacedByRingThatAreNotConnectedOutsideTheRing[i];
                    List<Planet> eligiblePlanetsOnNextRing = planetsPlacedByRingThatAreNotConnectedOutsideTheRing[i + 1];

                    // determine number of connections to ring N+1;
                    // basically random between 1 and (min(number_of_planets_in_ring_with_no_connections_outside_the_ring,same_for_other_ring) / 3)
                    int minimumConnectionsToNextOutermostRing = 2;
                    int maximumConnectionsToNextOutermostRing = ( ( eligiblePlanetsOnThisRing.Count * 4 ) / 10 );
                    if ( maximumConnectionsToNextOutermostRing > ( ( eligiblePlanetsOnNextRing.Count * 4 ) / 10 ) )
                        maximumConnectionsToNextOutermostRing = ( ( eligiblePlanetsOnNextRing.Count * 4 ) / 10 );

                    int numberOfConnectionsToNextOutermostRing;
                    if ( minimumConnectionsToNextOutermostRing >= maximumConnectionsToNextOutermostRing )
                        numberOfConnectionsToNextOutermostRing = minimumConnectionsToNextOutermostRing;
                    else
                        numberOfConnectionsToNextOutermostRing = Context.QualityRandom.NextWithInclusiveUpperBound( minimumConnectionsToNextOutermostRing, maximumConnectionsToNextOutermostRing );

                    for ( int j = 0; j < numberOfConnectionsToNextOutermostRing; j++ )
                    {
                        if ( eligiblePlanetsOnThisRing.Count == 0 )
                            break;

                        Planet planetOnThisRing;
                        if ( eligiblePlanetsOnThisRing.Count == 1 )
                        {
                            planetOnThisRing = eligiblePlanetsOnThisRing[0];
                        }
                        else
                        {
                            // pick random planet p1 on ring N that doesn't have any connections outside the ring
                            planetOnThisRing = eligiblePlanetsOnThisRing
                                [Context.QualityRandom.Next( 0, eligiblePlanetsOnThisRing.Count )];
                        }

                        // pick planet p2 on ring N+1 that is closest to p1
                        Planet closestPlanetOnNextRing = null;
                        int currentBestDistance = 0;
                        for ( int k = 0; k < eligiblePlanetsOnNextRing.Count; k++ )
                        {
                            Planet planetOnNextRing = eligiblePlanetsOnNextRing[k];
                            int thisDistance = Mat.ApproxDistanceBetweenPointsFast( planetOnThisRing.GalaxyLocation, planetOnNextRing.GalaxyLocation, -1 );
                            if ( closestPlanetOnNextRing == null || thisDistance < currentBestDistance )
                            {
                                closestPlanetOnNextRing = planetOnNextRing;
                                currentBestDistance = thisDistance;
                            }
                        }

                        if ( closestPlanetOnNextRing != null )
                        {
                            // connect p1 to p2
                            planetOnThisRing.AddLinkTo( closestPlanetOnNextRing );

                            planetsPlacedByRingThatAreNotConnectedOutsideTheRing[i].Remove( planetOnThisRing );
                            eligiblePlanetsOnThisRing.Remove( planetOnThisRing );

                            planetsPlacedByRingThatAreNotConnectedOutsideTheRing[i + 1].Remove( closestPlanetOnNextRing );
                            eligiblePlanetsOnNextRing.Remove( closestPlanetOnNextRing );
                        }
                    }
                }
            }
        }
    }

    public class Mapgen_X : Mapgen_Base
    {
        private void InnerGenerate( Galaxy galaxy, ArcenSimContext Context, int sizeOfSubTree, MapTypeData mapType, ArcenPoint CenterOfSubTree, FInt distanceToChildren, AngleDegrees startingAngleOffset, Planet Parent )
        {
            PlanetType planetType = PlanetType.Normal;
            int planetsToPlace = sizeOfSubTree;

            Planet center = galaxy.AddPlanet( planetType, CenterOfSubTree, Context );
            planetsToPlace--;

            if ( Parent != null )
                Parent.AddLinkTo( center );

            if ( planetsToPlace <= 0 )
                return;

            AngleDegrees startingAngleOffsetForChildren = startingAngleOffset.Add( AngleDegrees.Create( (FInt)45 ) );
            FInt distanceToChildrenForChildren = distanceToChildren / FInt.FromParts( 2, 250 );

            int numberOfChildren = Math.Min( 4, planetsToPlace );
            AngleDegrees degreesBetweenChildren = AngleDegrees.Create( ( (FInt)360 ) / numberOfChildren );
            int nodesPerChildTree = planetsToPlace / numberOfChildren;
            int extraNodesForChildTrees = planetsToPlace % numberOfChildren;
            AngleDegrees angleToNextChild = startingAngleOffset;
            for(int i = 0; i < numberOfChildren;i++)
            {
                ArcenPoint childPoint = CenterOfSubTree.GetPointAtAngleAndDistance( angleToNextChild, distanceToChildren.IntValue );
                int nodesForThisSubTree = nodesPerChildTree;
                if(extraNodesForChildTrees > 0)
                {
                    nodesForThisSubTree++;
                    extraNodesForChildTrees--;
                }
                InnerGenerate( galaxy, Context, nodesForThisSubTree, mapType, childPoint, distanceToChildrenForChildren, startingAngleOffsetForChildren, center );
                angleToNextChild += degreesBetweenChildren;
            }
        }

        public override void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
        {
            AngleDegrees startingAngleOffset = AngleDegrees.Create( (FInt)( -45 ) );
            InnerGenerate( galaxy, Context, numberToSeed, mapType, Engine_AIW2.GalaxyCenter, (FInt)300, startingAngleOffset, null );
        }
    }

    public class Mapgen_ClustersRoot : Mapgen_Base
    {
        public override void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
        {
            int triesLeft = 100;
            while ( triesLeft > 0 )
            {
                triesLeft--;
                if ( InnerGenerate( galaxy, Context, numberToSeed, mapType ) )
                    return;
            }
        }

        private bool InnerGenerate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
        {
            bool UseSimpleForClusterInternalLayout = true;
            int clusterRadius = 200;
            int clusterSpacing = 50;
            string mapName = mapType.InternalName;
            if ( ArcenStrings.Equals( mapName, "ClustersMicrocosm" ) )
            {
                UseSimpleForClusterInternalLayout = false;
                clusterRadius = 150;
            }
            bool iterationSucceeded = false;
            List<ArcenPoint> clusterCenters = new List<ArcenPoint>();
            List<MapClusterStyle> clusterStyles = new List<MapClusterStyle>();
            List<List<ArcenPoint>> planetPointsByCluster = new List<List<ArcenPoint>>();
            PlanetType planetType = PlanetType.Normal;
            while ( !iterationSucceeded )
            {
                iterationSucceeded = true;
                #region determine number of clusters
                int numberOfClusters;
                {
                    if ( numberToSeed <= 10 )
                        numberOfClusters = 2;
                    else if ( numberToSeed <= 20 )
                        numberOfClusters = 2;
                    else if ( numberToSeed <= 30 )
                        numberOfClusters = Context.QualityRandom.NextWithInclusiveUpperBound( 2, 3 );
                    else if ( numberToSeed <= 40 )
                        numberOfClusters = Context.QualityRandom.NextWithInclusiveUpperBound( 2, 3 );
                    else if ( numberToSeed <= 50 )
                        numberOfClusters = Context.QualityRandom.NextWithInclusiveUpperBound( 3, 4 );
                    else if ( numberToSeed <= 60 )
                        numberOfClusters = Context.QualityRandom.NextWithInclusiveUpperBound( 3, 4 );
                    else if ( numberToSeed <= 70 )
                        numberOfClusters = Context.QualityRandom.NextWithInclusiveUpperBound( 4, 5 );
                    else if ( numberToSeed <= 80 )
                        numberOfClusters = Context.QualityRandom.NextWithInclusiveUpperBound( 4, 6 );
                    else if ( numberToSeed <= 90 )
                        numberOfClusters = Context.QualityRandom.NextWithInclusiveUpperBound( 5, 6 );
                    else if ( numberToSeed <= 100 )
                        numberOfClusters = Context.QualityRandom.NextWithInclusiveUpperBound( 5, 6 );
                    else if ( numberToSeed <= 110 )
                        numberOfClusters = 6;
                    else //if ( numberToSeed <= 120 )
                        numberOfClusters = 6;
                }
                #endregion

                int clustersWide = 3;
                int clustersHigh = 2;

                ArcenRectangle overallArea;
                overallArea.Width = ( clusterRadius * 2 * clustersWide ) + ( clusterSpacing * ( clustersWide - 1 ) );
                overallArea.Height = ( clusterRadius * 2 * clustersHigh ) + ( clusterSpacing * ( clustersHigh - 1 ) );
                overallArea.X = Engine_AIW2.GalaxyCenter.X - overallArea.Width / 2;
                overallArea.Y = Engine_AIW2.GalaxyCenter.Y - overallArea.Height / 2;

                clusterCenters.Clear();
                ArcenPoint galacticCenterPoint;
                #region Pick points for each cluster
                {
                    ArcenPoint topLeftPossiblePoint = ArcenPoint.Create(
                        overallArea.X + clusterRadius,
                        overallArea.Y + clusterRadius );

                    ArcenPoint bottomRightPossiblePoint = ArcenPoint.Create(
                        overallArea.Right - clusterRadius,
                        overallArea.Bottom - clusterRadius );

                    List<ArcenPoint> possibleClusterSpots = new List<ArcenPoint>();
                    possibleClusterSpots.Add( topLeftPossiblePoint );
                    possibleClusterSpots.Add( ArcenPoint.Create(
                        topLeftPossiblePoint.X,
                        bottomRightPossiblePoint.Y ) );
                    possibleClusterSpots.Add( ArcenPoint.Create(
                        bottomRightPossiblePoint.X,
                        topLeftPossiblePoint.Y ) );
                    possibleClusterSpots.Add( bottomRightPossiblePoint );

                    galacticCenterPoint = ArcenPoint.Create(
                        ( topLeftPossiblePoint.X + bottomRightPossiblePoint.X ) / 2,
                        ( topLeftPossiblePoint.Y + bottomRightPossiblePoint.Y ) / 2 );

                    possibleClusterSpots.Add( ArcenPoint.Create(
                        galacticCenterPoint.X,
                        topLeftPossiblePoint.Y ) );

                    possibleClusterSpots.Add( ArcenPoint.Create(
                        galacticCenterPoint.X,
                        bottomRightPossiblePoint.Y ) );

                    Engine_Universal.Randomize<ArcenPoint>( possibleClusterSpots, Context.QualityRandom, 3 );

                    //int numberFailuresAllowed = 1000;
                    ArcenPoint newClusterCenter;
                    for ( int i = 0; i < numberOfClusters; i++ )
                    {
                        newClusterCenter = possibleClusterSpots[i];
                        //if ( this.HelperDoesPointListContainPointWithinDistance( clusterCenters, newClusterCenter, minimumDistanceBetweenClusters ) )
                        //{
                        //    i--;
                        //    numberFailuresAllowed--;
                        //    if ( numberFailuresAllowed <= 0 )
                        //    {
                        //        return false;
                        //    }
                        //    continue;
                        //}

                        clusterCenters.Add( newClusterCenter );
                    }
                }
                #endregion

                clusterStyles.Clear();
                planetPointsByCluster.Clear();
                #region For each cluster, pick points for planets
                {
                    int averagenumberToSeedPerCluster = numberToSeed / clusterCenters.Count;
                    int remainder = numberToSeed - ( averagenumberToSeedPerCluster * clusterCenters.Count );
                    if ( remainder < 0 )
                        remainder = 0;

                    int maxAllowedPlanetsPerCluster = ( averagenumberToSeedPerCluster * 4 ) / 3;
                    if ( maxAllowedPlanetsPerCluster > 20 )
                        maxAllowedPlanetsPerCluster = 20;

                    int maxAdditionalPlanetsPerCluster = maxAllowedPlanetsPerCluster - averagenumberToSeedPerCluster;
                    int additionalPlanetsUsedByLastCluster = 0;

                    for ( int i = 0; i < clusterCenters.Count; i++ )
                    {
                        ArcenPoint clusterCenter = clusterCenters[i];

                        planetPointsByCluster.Add( new List<ArcenPoint>() );

                        int numberToSeedForCluster = averagenumberToSeedPerCluster;
                        if ( additionalPlanetsUsedByLastCluster > 0 )
                        {
                            numberToSeedForCluster -= additionalPlanetsUsedByLastCluster;
                            additionalPlanetsUsedByLastCluster = 0;
                        }
                        else if ( i < ( clusterCenters.Count - 1 ) && maxAdditionalPlanetsPerCluster > 0 )
                        {
                            additionalPlanetsUsedByLastCluster = Context.QualityRandom.NextWithInclusiveUpperBound( 0, maxAdditionalPlanetsPerCluster );
                            numberToSeedForCluster += additionalPlanetsUsedByLastCluster;
                        }

                        if ( i == ( clusterCenters.Count - 1 ) )
                        {
                            numberToSeedForCluster += remainder;
                        }

                        MapClusterStyle clusterStyle;
                        if ( UseSimpleForClusterInternalLayout )
                            clusterStyle = MapClusterStyle.Simple;
                        else
                            clusterStyle = (MapClusterStyle)Context.QualityRandom.Next( (int)MapClusterStyle.None + 1, (int)MapClusterStyle.Length );
                        clusterStyles.Add( clusterStyle );

                        if ( !UtilityMethods.Helper_PickIntraClusterPlanetPoints( Context, planetPointsByCluster[i], clusterCenter, clusterRadius, numberToSeedForCluster, clusterStyle ) )
                        {
                            iterationSucceeded = false;
                            break;
                        }
                    }
                }
                if ( !iterationSucceeded )
                    break;
                #endregion
            }
            if ( !iterationSucceeded )
                return false;

            // CANNOT early-out from this point on, MUST generate a usable map

            List<List<Planet>> planetsByCluster = new List<List<Planet>>();
            #region For each cluster, populate with planets
            {
                int totalPlanetsPlaced = 0;
                List<ArcenPoint> planetPointList;
                MapClusterStyle clusterStyle;
                for ( int i = 0; i < planetPointsByCluster.Count; i++ )
                {
                    planetPointList = planetPointsByCluster[i];
                    planetsByCluster.Add( new List<Planet>() );
                    #region Place Planets
                    {
                        for ( int j = 0; j < planetPointList.Count; j++ )
                        {
                            planetsByCluster[i].Add( galaxy.AddPlanet( planetType, planetPointList[j], Context ) );
                            totalPlanetsPlaced++;
                        }
                    }
                    #endregion
                    #region Add intra-cluster connections
                    clusterStyle = clusterStyles[i];
                    UtilityMethods.Helper_MakeIntraClusterConnections( planetsByCluster[i], clusterStyle );
                    #endregion
                }
            }
            #endregion

            #region For each cluster, pick up to 3 connections to other nearby clusters
            {
                List<Pair<int, int>> linkedClusterIndices = new List<Pair<int, int>>();
                for ( int i = 0; i < clusterCenters.Count; i++ )
                {
                    ArcenPoint clusterCenter = clusterCenters[i];
                    List<Planet> clusterPlanetList = planetsByCluster[i];

                    int closestClusterIndex = -1;
                    int distanceToClosestCluster = 0;
                    int secondClosestClusterIndex = -1;
                    int distanceToSecondClosestCluster = 0;

                    for ( int j = 0; j < clusterCenters.Count; j++ )
                    {
                        if ( i == j )
                            continue;
                        if ( UtilityMethods.Helper_GetDoesPairListContainPairInEitherDirection( linkedClusterIndices, i, j ) )
                            continue;
                        ArcenPoint otherClusterCenter = clusterCenters[j];
                        //List<Planet> otherClusterPlanetList = planetsByCluster[j];
                        int distanceToOtherCluster = Mat.DistanceBetweenPoints( clusterCenter, otherClusterCenter );
                        int testDistance = distanceToClosestCluster / 2;
                        bool foundHit = false;
                        for ( int k = 0; k < clusterCenters.Count; k++ )
                        {
                            if ( k == i || k == j )
                                continue;
                            ArcenPoint thirdClusterCenter = clusterCenters[k];
                            List<Planet> thirdClusterPlanets = planetsByCluster[k];
                            for ( int planetIndex = 0; planetIndex < thirdClusterPlanets.Count; planetIndex++ )
                            {
                                Planet planet = thirdClusterPlanets[planetIndex];
                                planet.DoForLinkedNeighbors( delegate ( Planet neighbor )
                                 {
                                     if ( !Mat.LineSegmentIntersectsLineSegment( clusterCenter, otherClusterCenter, planet.GalaxyLocation, neighbor.GalaxyLocation, planet.TypeData.IntraStellarRadius ) )
                                         return DelReturn.Continue;
                                     foundHit = true;
                                     return DelReturn.Break;
                                 } );
                                if ( foundHit )
                                    break;
                            }
                            if ( foundHit )
                                break;
                        }
                        if ( foundHit )
                            continue;
                        if ( closestClusterIndex == -1 || distanceToOtherCluster < distanceToClosestCluster )
                        {
                            closestClusterIndex = j;
                            distanceToClosestCluster = distanceToOtherCluster;
                        }
                        else if ( secondClosestClusterIndex == -1 || distanceToOtherCluster < distanceToSecondClosestCluster )
                        {
                            secondClosestClusterIndex = j;
                            distanceToSecondClosestCluster = distanceToOtherCluster;
                        }
                    }

                    if ( UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, i ) < 3 &&
                         closestClusterIndex != -1 &&
                         ( UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, i ) < 2 ||
                           UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, closestClusterIndex ) < 3 ) )
                    {
                        UtilityMethods.Helper_ConnectPlanetLists( clusterPlanetList, planetsByCluster[closestClusterIndex], false, true );
                        linkedClusterIndices.Add( Pair<int, int>.Create( i, closestClusterIndex ) );
                    }
                    if ( UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, i ) < 2 &&
                         secondClosestClusterIndex != -1 &&
                         UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, secondClosestClusterIndex ) < 2 )
                    {
                        UtilityMethods.Helper_ConnectPlanetLists( clusterPlanetList, planetsByCluster[secondClosestClusterIndex], false, true );
                        linkedClusterIndices.Add( Pair<int, int>.Create( i, secondClosestClusterIndex ) );
                    }
                }
            }
            #endregion
            return true;
        }
    }

    public class Mapgen_Wheel : Mapgen_Base
    {
        public override void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
        {
            //1) Place Center
            ArcenPoint originPlanetPoint = Engine_AIW2.GalaxyCenter;
            PlanetType planetType = PlanetType.Normal;
            Planet originPlanet = galaxy.AddPlanet( planetType, originPlanetPoint, Context );

            //2) Pick Number Of Spokes from 3 to 8
            //3) Pick Radius from 130 to 250
            int numberOfSpokes;
            int radius;
            {
                if ( numberToSeed <= 10 )
                {
                    radius = 200;
                }
                else if ( numberToSeed <= 20 )
                {
                    radius = 210;
                }
                else if ( numberToSeed <= 30 )
                {
                    radius = 220;
                }
                else if ( numberToSeed <= 40 )
                {
                    radius = 230;
                }
                else if ( numberToSeed <= 50 )
                {
                    radius = 240;
                }
                else if ( numberToSeed <= 60 )
                {
                    radius = 250;
                }
                else if ( numberToSeed <= 70 )
                {
                    radius = 260;
                }
                else if ( numberToSeed <= 80 )
                {
                    radius = 270;
                }
                else if ( numberToSeed <= 90 )
                {
                    radius = 270;
                }
                else if ( numberToSeed <= 100 )
                {
                    radius = 270;
                }
                else if ( numberToSeed <= 110 )
                {
                    radius = 280;
                }
                else //if ( numberToSeed <= 120 )
                {
                    radius = 280;
                }
            }

            FInt roughNumberOfPlanetsToExpectOnRing = ( (FInt)radius * 6 ) / 40;
            FInt targetNumberOfPlanetsForSpokes = ( numberToSeed - 1 ) - roughNumberOfPlanetsToExpectOnRing;
            if ( targetNumberOfPlanetsForSpokes < ( numberToSeed / 2 ) )
                targetNumberOfPlanetsForSpokes = (FInt)numberToSeed / 2;

            FInt minimumNumberOfSpokes = targetNumberOfPlanetsForSpokes / 9;
            FInt maximumNumberOfSpokes = targetNumberOfPlanetsForSpokes / 5;
            if ( minimumNumberOfSpokes > 7 )
                numberOfSpokes = 8;
            else
            {
                int minimum = minimumNumberOfSpokes.GetNearestIntPreferringHigher();
                if ( minimum < 4 )
                    minimum = 4;
                int maximum = maximumNumberOfSpokes.GetNearestIntPreferringLower();
                if ( maximum > 8 )
                    maximum = 8;
                if ( minimum > maximum )
                    numberOfSpokes = minimum;
                else
                    numberOfSpokes = Context.QualityRandom.NextWithInclusiveUpperBound( minimum, maximum );
            }

            FInt numberOfPlanetsPerSpoke = targetNumberOfPlanetsForSpokes / numberOfSpokes;
            FInt distanceBetweenPlanetsOnSpoke = ( radius - 60 ) / numberOfPlanetsPerSpoke;

            //4) Pick Random Starting Angle
            AngleDegrees startingAngle = AngleDegrees.Create( (FInt)Context.QualityRandom.NextWithInclusiveUpperBound( 10, 350 ) );

            AngleDegrees anglePerSpoke = AngleDegrees.Create( (FInt)360 / (FInt)numberOfSpokes );

            //5) For each spoke
            List<Planet> secondToLastPlanetsInEachSpoke = new List<Planet>();
            List<Planet> lastPlanetsInEachSpoke = new List<Planet>();
            {
                Planet lastPlanetPlacedInSpoke;
                Planet secondToLastPlanetPlacedInSpoke;
                Planet newPlanetInSpoke;
                AngleDegrees spokeAngle = startingAngle;
                ArcenPoint linePoint;
                ArcenPoint perpendicularLinePoint;
                bool flipLineDisplacement;
                for ( int i = 0; i < numberOfSpokes; i++ )
                {
                    lastPlanetPlacedInSpoke = null;
                    secondToLastPlanetPlacedInSpoke = null;
                    flipLineDisplacement = false;
                    FInt distanceFromOrigin = (FInt)80;

                    for ( int j = 0; j < numberOfPlanetsPerSpoke; j++ )
                    {
                        //-- compute point on line from origin at angle at radius 60
                        linePoint = originPlanetPoint.GetPointAtAngleAndDistance( spokeAngle, distanceFromOrigin.IntValue );

                        //-- compute point on line perpendicular to that, at +20 along that line
                        perpendicularLinePoint = linePoint.GetPointAtAngleAndDistance( originPlanetPoint.GetAngleToDegrees( linePoint ).Add( AngleDegrees.Create( (FInt)90 ) ),
                             flipLineDisplacement ? -25 : 25 );

                        //-- place planet there
                        newPlanetInSpoke = galaxy.AddPlanet( planetType, perpendicularLinePoint, Context );

                        //--- connect to last two planets placed on this spoke
                        if ( lastPlanetPlacedInSpoke != null )
                            lastPlanetPlacedInSpoke.AddLinkTo( newPlanetInSpoke );
                        if ( secondToLastPlanetPlacedInSpoke != null )
                            secondToLastPlanetPlacedInSpoke.AddLinkTo( newPlanetInSpoke );
                        //--- if fewer than 2 already in spoke, also connect to origin
                        else
                            originPlanet.AddLinkTo( newPlanetInSpoke );
                        secondToLastPlanetPlacedInSpoke = lastPlanetPlacedInSpoke;
                        lastPlanetPlacedInSpoke = newPlanetInSpoke;

                        //-- flip next to be placed at -20 instead of +20
                        flipLineDisplacement = !flipLineDisplacement;

                        //--increment radius
                        distanceFromOrigin += distanceBetweenPlanetsOnSpoke;

                        //---if radius now >= target overall radius, increment angle by 360/spoke_count and go to next spoke
                        if ( distanceFromOrigin >= radius )
                            break;
                    }

                    if ( secondToLastPlanetPlacedInSpoke != null )
                        secondToLastPlanetsInEachSpoke.Add( secondToLastPlanetPlacedInSpoke );
                    if ( lastPlanetPlacedInSpoke != null )
                        lastPlanetsInEachSpoke.Add( lastPlanetPlacedInSpoke );
                    spokeAngle = spokeAngle.Add( anglePerSpoke );
                }
            }

            //6) Compute number of planets left to place
            int numberOfPlanetsForRing = numberToSeed - galaxy.Planets.Count;

            //7) Compute average angle for next step from 360/planets_left
            AngleDegrees angleBetweenRingPlanet = AngleDegrees.Create( (FInt)360 / (FInt)numberOfPlanetsForRing );

            //8) Pick Random Starting Angle from 0 to 359
            AngleDegrees ringAngle = AngleDegrees.Create( (FInt)Context.QualityRandom.NextWithInclusiveUpperBound( 10, 350 ) );

            //9) for i from 0 to number of planets left to place
            bool useSimpleRingConstruction = numberToSeed < 40;
            List<Planet> innerRingPlanets = new List<Planet>();
            {
                List<Planet> ringPlanets = new List<Planet>();
                ArcenPoint linePoint;
                Planet newPlanetInRing;
                bool flipOffsetForNextRingPlanet = false;
                for ( int i = 0; i < numberOfPlanetsForRing; i++ )
                {
                    //-- compute point on line from origin at angle at target radius +40
                    linePoint = originPlanetPoint.GetPointAtAngleAndDistance( ringAngle, radius + ( !useSimpleRingConstruction && flipOffsetForNextRingPlanet ? 30 : 60 ) );

                    //-- place planet there
                    newPlanetInRing = galaxy.AddPlanet( planetType, linePoint, Context );

                    //--- connect to last two planets placed on ring
                    //--- if this is second-to-last or last planet in ring, connect to first planet in ring
                    //--- if this is last planet in ring, connect to second planet in ring
                    if ( !useSimpleRingConstruction )
                    {
                        if ( ringPlanets.Count >= 1 )
                            ringPlanets[ringPlanets.Count - 1].AddLinkTo( newPlanetInRing );
                        if ( ringPlanets.Count >= 2 )
                            ringPlanets[ringPlanets.Count - 2].AddLinkTo( newPlanetInRing );

                        if ( i >= numberOfPlanetsForRing - 2 && ringPlanets.Count >= 1 )
                            ringPlanets[0].AddLinkTo( newPlanetInRing );
                        if ( i >= numberOfPlanetsForRing - 1 && ringPlanets.Count >= 2 )
                            ringPlanets[1].AddLinkTo( newPlanetInRing );

                        ringPlanets.Add( newPlanetInRing );
                        if ( flipOffsetForNextRingPlanet )
                            innerRingPlanets.Add( newPlanetInRing );
                    }
                    else
                    {
                        if ( ringPlanets.Count >= 1 )
                            ringPlanets[ringPlanets.Count - 1].AddLinkTo( newPlanetInRing );

                        if ( i >= numberOfPlanetsForRing - 1 && ringPlanets.Count >= 1 )
                            ringPlanets[0].AddLinkTo( newPlanetInRing );

                        ringPlanets.Add( newPlanetInRing );
                        innerRingPlanets.Add( newPlanetInRing );
                    }

                    //-- flip offset to +20
                    flipOffsetForNextRingPlanet = !flipOffsetForNextRingPlanet;
                    ringAngle = ringAngle.Add( angleBetweenRingPlanet );
                }
            }

            //10) for each spoke
            //-- for the last planet, connect to the two nearest ring planets
            //-- for the second-to-last planet, connect to the nearest ring planet.
            if ( numberToSeed > 10 )
            {
                Planet spokeEndPlanet;
                if ( !useSimpleRingConstruction )
                {
                    for ( int i = 0; i < secondToLastPlanetsInEachSpoke.Count; i++ )
                    {
                        spokeEndPlanet = secondToLastPlanetsInEachSpoke[i];
                        innerRingPlanets.Sort(
                            delegate ( Planet Left, Planet Right )
                            {
                                return Mat.DistanceBetweenPoints( Left.GalaxyLocation, spokeEndPlanet.GalaxyLocation )
                                    .CompareTo(
                                       Mat.DistanceBetweenPoints( Right.GalaxyLocation, spokeEndPlanet.GalaxyLocation )
                                    );
                            } );
                        if ( innerRingPlanets.Count >= 1 )
                            spokeEndPlanet.AddLinkTo( innerRingPlanets[0] );
                    }
                }
                for ( int i = 0; i < lastPlanetsInEachSpoke.Count; i++ )
                {
                    spokeEndPlanet = lastPlanetsInEachSpoke[i];
                    innerRingPlanets.Sort(
                        delegate ( Planet Left, Planet Right )
                        {
                            return Mat.DistanceBetweenPoints( Left.GalaxyLocation, spokeEndPlanet.GalaxyLocation )
                                .CompareTo(
                                   Mat.DistanceBetweenPoints( Right.GalaxyLocation, spokeEndPlanet.GalaxyLocation )
                                );
                        } );
                    if ( innerRingPlanets.Count >= 1 )
                        spokeEndPlanet.AddLinkTo( innerRingPlanets[0] );
                    if ( !useSimpleRingConstruction && innerRingPlanets.Count >= 2 )
                        spokeEndPlanet.AddLinkTo( innerRingPlanets[1] );
                }
            }

            // emergency connection test
            {
                Planet planet;
                for ( int i = 0; i < galaxy.Planets.Count; i++ )
                {
                    planet = galaxy.Planets[i];
                    if ( planet.GetLinkedNeighborCount() > 0 )
                        continue;
                    lastPlanetsInEachSpoke.Sort(
                        delegate ( Planet Left, Planet Right )
                        {
                            return Mat.DistanceBetweenPoints( Left.GalaxyLocation, planet.GalaxyLocation )
                                .CompareTo(
                                   Mat.DistanceBetweenPoints( Right.GalaxyLocation, planet.GalaxyLocation )
                                );
                        } );
                    if ( lastPlanetsInEachSpoke.Count >= 1 )
                        planet.AddLinkTo( lastPlanetsInEachSpoke[0] );
                }
            }
        }
    }

    public class Mapgen_Encapsulated : Mapgen_Base
    {
        public override void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
        {
            while ( !RenderMethodCookie( Context, galaxy, numberToSeed ) ) ;
        }

        private static bool RenderMethodCookie( ArcenSimContext Context, Galaxy galaxy, int NumberOfPlanets )
        {
            #region determine number of clusters
            int minimumSmallClusterSize = 6;
            int normalLargeClusterSize = 14;
            int numberOfRimPlanets;
            List<int> smallClusterSizes = new List<int>();
            List<int> largeClusterSizes = new List<int>();
            {
                switch ( NumberOfPlanets )
                {
                    case 10:
                        numberOfRimPlanets = 5;
                        smallClusterSizes.Add( 5 );
                        break;
                    case 15:
                        numberOfRimPlanets = 7;
                        smallClusterSizes.Add( 4 );
                        smallClusterSizes.Add( 4 );
                        break;
                    case 20:
                        smallClusterSizes.Add( minimumSmallClusterSize );
                        smallClusterSizes.Add( minimumSmallClusterSize );
                        numberOfRimPlanets = NumberOfPlanets - ( minimumSmallClusterSize * 2 );
                        break;
                    case 25:
                        if ( Context.QualityRandom.NextBool() )
                        {
                            smallClusterSizes.Add( minimumSmallClusterSize );
                            smallClusterSizes.Add( minimumSmallClusterSize );
                            smallClusterSizes.Add( minimumSmallClusterSize );
                            numberOfRimPlanets = NumberOfPlanets - ( minimumSmallClusterSize * 3 );
                        }
                        else
                        {
                            largeClusterSizes.Add( normalLargeClusterSize );
                            numberOfRimPlanets = NumberOfPlanets - normalLargeClusterSize;
                        }
                        break;
                    default:
                        numberOfRimPlanets = ( (FInt)NumberOfPlanets * FInt.FromParts( 0, 400 ) ).IntValue;
                        int planetsRemaining = NumberOfPlanets - numberOfRimPlanets;
                        while ( planetsRemaining > 10 )
                        {
                            bool pickLarge = false;
                            if ( largeClusterSizes.Count < 4 )
                            {
                                if ( smallClusterSizes.Count >= 5 )
                                    pickLarge = true;
                                else if ( Context.QualityRandom.NextBool() )
                                    pickLarge = true;
                            }

                            if ( pickLarge )
                            {
                                int clusterSize = Math.Min( normalLargeClusterSize, planetsRemaining );
                                largeClusterSizes.Add( clusterSize );
                                planetsRemaining -= clusterSize;
                            }
                            else
                            {
                                int clusterSize = Math.Min( minimumSmallClusterSize, planetsRemaining );
                                smallClusterSizes.Add( clusterSize );
                                planetsRemaining -= clusterSize;
                            }
                        }
                        if ( planetsRemaining >= minimumSmallClusterSize )
                        {
                            int clusterSize = Math.Min( minimumSmallClusterSize, planetsRemaining );
                            smallClusterSizes.Add( clusterSize );
                            planetsRemaining -= clusterSize;
                        }
                        if ( planetsRemaining > 0 )
                        {
                            numberOfRimPlanets += planetsRemaining;
                            planetsRemaining = 0;
                        }
                        break;
                }
            }
            #endregion

            int largeClusterRadius = 100;
            int smallClusterRadius = 50;

            ArcenPoint galacticCenter = Engine_AIW2.GalaxyCenter;
            int galacticRadius = 340;

            List<ArcenPoint> largeClusterCenters = new List<ArcenPoint>();
            #region Pick points for each large cluster
            {
                int permittedRadius = galacticRadius - ( (FInt)largeClusterRadius * FInt.FromParts( 1, 500 ) ).IntValue;
                //int lastRadiusToTest = 0;
                //int radiusToTest = largeClusterRadius;
                int numberFailuresAllowedBeforeRadiusIncrease = 1000;
                for ( int i = 0; i < largeClusterSizes.Count; i++ )
                {
                    ArcenPoint newClusterCenter = galacticCenter.GetRandomPointWithinDistance( Context.QualityRandom, 0, permittedRadius );

                    bool foundCollision = false;
                    for ( int j = 0; j < largeClusterCenters.Count; j++ )
                    {
                        ArcenPoint existingClusterCenter = largeClusterCenters[j];
                        int threshold = largeClusterRadius + largeClusterRadius;
                        if ( Mat.ApproxDistanceBetweenPointsFast( newClusterCenter, existingClusterCenter, threshold ) < threshold )
                        {
                            foundCollision = true;
                            break;
                        }
                    }
                    if ( foundCollision )
                    {
                        i--;
                        numberFailuresAllowedBeforeRadiusIncrease--;
                        if ( numberFailuresAllowedBeforeRadiusIncrease <= 0 )
                        {
                            //lastRadiusToTest = radiusToTest;
                            //radiusToTest += largeClusterRadius / 2;
                            //if ( radiusToTest >= galacticRadius )
                            return false;
                        }
                        continue;
                    }

                    largeClusterCenters.Add( newClusterCenter );
                    numberFailuresAllowedBeforeRadiusIncrease = 100;
                }
            }
            #endregion

            List<ArcenPoint> smallClusterCenters = new List<ArcenPoint>();
            #region Pick points for each small cluster
            {
                int permittedRadius = galacticRadius - ( (FInt)smallClusterRadius * FInt.FromParts( 1, 500 ) ).IntValue;
                //int lastRadiusToTest = 0;
                //int radiusToTest = smallClusterRadius;
                int numberFailuresAllowedBeforeRadiusIncrease = 1000;
                for ( int i = 0; i < smallClusterSizes.Count; i++ )
                {
                    ArcenPoint newClusterCenter = galacticCenter.GetRandomPointWithinDistance( Context.QualityRandom, 0, permittedRadius );

                    bool foundCollision = false;
                    for ( int j = 0; j < largeClusterCenters.Count; j++ )
                    {
                        ArcenPoint existingClusterCenter = largeClusterCenters[j];
                        int threshold = largeClusterRadius + smallClusterRadius;
                        if ( Mat.ApproxDistanceBetweenPointsFast( newClusterCenter, existingClusterCenter, threshold ) < threshold )
                        {
                            foundCollision = true;
                            break;
                        }
                    }
                    if ( !foundCollision )
                    {
                        for ( int j = 0; j < smallClusterCenters.Count; j++ )
                        {
                            ArcenPoint existingClusterCenter = smallClusterCenters[j];
                            int threshold = smallClusterRadius + smallClusterRadius;
                            if ( Mat.ApproxDistanceBetweenPointsFast( newClusterCenter, existingClusterCenter, threshold ) < threshold )
                            {
                                foundCollision = true;
                                break;
                            }
                        }
                    }
                    if ( foundCollision )
                    {
                        i--;
                        numberFailuresAllowedBeforeRadiusIncrease--;
                        if ( numberFailuresAllowedBeforeRadiusIncrease <= 0 )
                        {
                            //lastRadiusToTest = radiusToTest;
                            //radiusToTest += smallClusterRadius / 2;
                            //if ( radiusToTest >= galacticRadius )
                            return false;
                        }
                        continue;
                    }

                    smallClusterCenters.Add( newClusterCenter );
                    numberFailuresAllowedBeforeRadiusIncrease = 100;
                }
            }
            #endregion

            List<int> clusterSizes = new List<int>();
            List<ArcenPoint> clusterCenters = new List<ArcenPoint>();
            for ( int i = 0; i < largeClusterSizes.Count; i++ )
            {
                clusterSizes.Add( largeClusterSizes[i] );
                clusterCenters.Add( largeClusterCenters[i] );
            }
            for ( int i = 0; i < smallClusterSizes.Count; i++ )
            {
                clusterSizes.Add( smallClusterSizes[i] );
                clusterCenters.Add( smallClusterCenters[i] );
            }

            List<MapClusterStyle> clusterStyles = new List<MapClusterStyle>();
            List<List<ArcenPoint>> planetPointsByCluster = new List<List<ArcenPoint>>();
            #region For each cluster, pick points for planets
            {
                for ( int i = 0; i < clusterCenters.Count; i++ )
                {
                    ArcenPoint clusterCenter = clusterCenters[i];

                    planetPointsByCluster.Add( new List<ArcenPoint>() );

                    int numberOfPlanetsForCluster = clusterSizes[i];

                    MapClusterStyle clusterStyle = MapClusterStyle.Simple;
                    clusterStyles.Add( clusterStyle );

                    int clusterRadius = numberOfPlanetsForCluster > minimumSmallClusterSize
                        ? largeClusterRadius
                        : smallClusterRadius
                        ;

                    if ( !UtilityMethods.Helper_PickIntraClusterPlanetPoints( Context, planetPointsByCluster[i], clusterCenter, clusterRadius, numberOfPlanetsForCluster, clusterStyle ) )
                        return false;
                }
            }
            #endregion

            // CANNOT early-out from this point on, MUST generate a usable map

            PlanetType planetType = PlanetType.Normal;

            List<List<Planet>> planetsByCluster = new List<List<Planet>>();
            #region For each cluster, populate with planets
            {
                int totalPlanetsPlaced = 0;
                List<ArcenPoint> planetPointList;
                MapClusterStyle clusterStyle;
                for ( int i = 0; i < planetPointsByCluster.Count; i++ )
                {
                    planetPointList = planetPointsByCluster[i];
                    planetsByCluster.Add( new List<Planet>() );
                    #region Place Planets
                    {
                        for ( int j = 0; j < planetPointList.Count; j++ )
                        {
                            planetsByCluster[i].Add( galaxy.AddPlanet( planetType, planetPointList[j], Context ) );
                            totalPlanetsPlaced++;
                        }
                    }
                    #endregion
                    #region Add intra-cluster connections
                    clusterStyle = clusterStyles[i];
                    UtilityMethods.Helper_MakeIntraClusterConnections( planetsByCluster[i], clusterStyle );
                    #endregion
                }
            }
            #endregion

            #region For each cluster, pick up to 3 connections to other nearby clusters
            {
                List<Pair<int, int>> linkedClusterIndices = new List<Pair<int, int>>();
                ArcenPoint clusterCenter;
                List<Planet> clusterPlanetList;
                ArcenPoint otherClusterCenter;
                //List<Planet> otherClusterPlanetList;
                int distanceToOtherCluster;
                int closestClusterIndex;
                int distanceToClosestCluster;
                int secondClosestClusterIndex;
                int distanceToSecondClosestCluster;
                for ( int i = 0; i < clusterCenters.Count; i++ )
                {
                    clusterCenter = clusterCenters[i];
                    clusterPlanetList = planetsByCluster[i];

                    closestClusterIndex = -1;
                    distanceToClosestCluster = 0;
                    secondClosestClusterIndex = -1;
                    distanceToSecondClosestCluster = 0;

                    int distanceToClosestClusterIncludingAlreadyConnected = -1;
                    for ( int j = 0; j < clusterCenters.Count; j++ )
                    {
                        if ( i == j )
                            continue;
                        otherClusterCenter = clusterCenters[j];
                        //otherClusterPlanetList = planetsByCluster[j];
                        distanceToOtherCluster = Mat.DistanceBetweenPoints( clusterCenter, otherClusterCenter );
                        if ( distanceToClosestClusterIncludingAlreadyConnected == -1 || distanceToOtherCluster < distanceToClosestClusterIncludingAlreadyConnected )
                            distanceToClosestClusterIncludingAlreadyConnected = distanceToOtherCluster;
                    }
                    int maxThresholdForConnection = ( (FInt)distanceToClosestClusterIncludingAlreadyConnected * FInt.FromParts( 1, 50 ) ).IntValue;

                    for ( int j = 0; j < clusterCenters.Count; j++ )
                    {
                        if ( i == j )
                            continue;
                        if ( UtilityMethods.Helper_GetDoesPairListContainPairInEitherDirection( linkedClusterIndices, i, j ) )
                            continue;
                        otherClusterCenter = clusterCenters[j];
                        //otherClusterPlanetList = planetsByCluster[j];
                        distanceToOtherCluster = Mat.DistanceBetweenPoints( clusterCenter, otherClusterCenter );
                        if ( distanceToOtherCluster > maxThresholdForConnection )
                            continue;
                        if ( closestClusterIndex == -1 || distanceToOtherCluster < distanceToClosestCluster )
                        {
                            closestClusterIndex = j;
                            distanceToClosestCluster = distanceToOtherCluster;
                        }
                        else if ( secondClosestClusterIndex == -1 || distanceToOtherCluster < distanceToSecondClosestCluster )
                        {
                            secondClosestClusterIndex = j;
                            distanceToSecondClosestCluster = distanceToOtherCluster;
                        }
                    }

                    if ( UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, i ) < 3 &&
                         closestClusterIndex != -1 &&
                         ( UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, i ) < 2 ||
                           UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, closestClusterIndex ) < 3 ) )
                    {
                        UtilityMethods.Helper_ConnectPlanetLists( clusterPlanetList, planetsByCluster[closestClusterIndex], false, true );
                        linkedClusterIndices.Add( Pair<int, int>.Create( i, closestClusterIndex ) );
                    }
                    if ( UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, i ) < 2 &&
                         secondClosestClusterIndex != -1 &&
                         UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, secondClosestClusterIndex ) < 2 )
                    {
                        UtilityMethods.Helper_ConnectPlanetLists( clusterPlanetList, planetsByCluster[secondClosestClusterIndex], false, true );
                        linkedClusterIndices.Add( Pair<int, int>.Create( i, secondClosestClusterIndex ) );
                    }
                }
            }
            #endregion

            #region Place Outer Rim
            List<Planet> rimPlanets = new List<Planet>();
            if ( numberOfRimPlanets > 0 ) // it will be, but just making sure
            {
                float startingAngle = Context.QualityRandom.NextFloat( 1, 359 );

                Planet firstPlanetPlacedOnRing = null;
                Planet lastPlanetPlacedOnRing = null;
                for ( int j = 0; j < numberOfRimPlanets; j++ )
                {
                    float angle = ( 360f / (float)numberOfRimPlanets ) * (float)j; // yes, this is theoretically an MP-sync problem, but a satisfactory 360 arc was simply not coming from the FInt approximations and I'm figuring the actual full-sync at the beginning of the game should sync things up before they matter
                    angle += startingAngle;
                    if ( angle >= 360f )
                        angle -= 360f;

                    ArcenPoint pointOnRing = galacticCenter;
                    pointOnRing.X += (int)Math.Round( galacticRadius * (float)Math.Cos( angle * ( Math.PI / 180f ) ) );
                    pointOnRing.Y += (int)Math.Round( galacticRadius * (float)Math.Sin( angle * ( Math.PI / 180f ) ) );

                    Planet planet = galaxy.AddPlanet( planetType, pointOnRing, Context );

                    rimPlanets.Add( planet );

                    if ( lastPlanetPlacedOnRing != null )
                        lastPlanetPlacedOnRing.AddLinkTo( planet );

                    if ( firstPlanetPlacedOnRing == null )
                        firstPlanetPlacedOnRing = planet;

                    lastPlanetPlacedOnRing = planet;
                }
                if ( lastPlanetPlacedOnRing != null && firstPlanetPlacedOnRing != null )
                    lastPlanetPlacedOnRing.AddLinkTo( firstPlanetPlacedOnRing );
            }
            #endregion

            #region Connect Outer Rim to clusters
            List<int> closestClusterIndexByRimPlanet = new List<int>();
            for ( int i = 0; i < rimPlanets.Count; i++ )
            {
                Planet rimPlanet = rimPlanets[i];
                int bestDistanceSoFar = -1;
                int bestIndexSoFar = -1;
                for ( int j = 0; j < planetsByCluster.Count; j++ )
                {
                    List<Planet> cluster = planetsByCluster[j];
                    int distanceToCluster = -1;
                    for ( int k = 0; k < cluster.Count; k++ )
                    {
                        Planet clusterPlanet = cluster[k];
                        int distance = Mat.ApproxDistanceBetweenPointsFast( rimPlanet.GalaxyLocation, clusterPlanet.GalaxyLocation, -1 );
                        if ( distanceToCluster == -1 ||
                             distanceToCluster > distance )
                            distanceToCluster = distance;
                    }

                    if ( bestIndexSoFar == -1 ||
                       bestDistanceSoFar > distanceToCluster )
                    {
                        bestDistanceSoFar = distanceToCluster;
                        bestIndexSoFar = j;
                    }
                }
                closestClusterIndexByRimPlanet.Add( bestIndexSoFar );
            }

            List<int> closestRimPlanetIndexByCluster = new List<int>();
            for ( int i = 0; i < planetsByCluster.Count; i++ )
            {
                List<Planet> cluster = planetsByCluster[i];
                int bestDistanceSoFar = -1;
                int bestIndexSoFar = -1;
                for ( int j = 0; j < rimPlanets.Count; j++ )
                {
                    Planet rimPlanet = rimPlanets[j];
                    int distanceToPlanet = -1;
                    for ( int k = 0; k < cluster.Count; k++ )
                    {
                        Planet clusterPlanet = cluster[k];
                        int distance = Mat.ApproxDistanceBetweenPointsFast( rimPlanet.GalaxyLocation, clusterPlanet.GalaxyLocation, -1 );
                        if ( distanceToPlanet == -1 ||
                             distanceToPlanet > distance )
                            distanceToPlanet = distance;
                    }

                    if ( bestIndexSoFar == -1 ||
                       bestDistanceSoFar > distanceToPlanet )
                    {
                        bestDistanceSoFar = distanceToPlanet;
                        bestIndexSoFar = j;
                    }
                }
                closestRimPlanetIndexByCluster.Add( bestIndexSoFar );
            }

            for ( int i = 0; i < rimPlanets.Count; i++ )
            {
                Planet rimPlanet = rimPlanets[i];
                int closestClusterIndex = closestClusterIndexByRimPlanet[i];
                int clustersRimPlanetIndex = closestRimPlanetIndexByCluster[closestClusterIndex];
                if ( clustersRimPlanetIndex != i )
                    continue;
                List<Planet> cluster = planetsByCluster[closestClusterIndex];
                Planet closestPlanetInCluster = null;
                int distanceToClosest = -1;
                for ( int j = 0; j < cluster.Count; j++ )
                {
                    Planet clusterPlanet = cluster[j];
                    int distance = Mat.ApproxDistanceBetweenPointsFast( rimPlanet.GalaxyLocation, clusterPlanet.GalaxyLocation, -1 );
                    if ( closestPlanetInCluster == null ||
                        distanceToClosest > distance )
                    {
                        closestPlanetInCluster = clusterPlanet;
                        distanceToClosest = distance;
                    }
                }
                rimPlanet.AddLinkTo( closestPlanetInCluster );
            }
            #endregion

            return true;
        }
    }

    public enum MapClusterStyle
    {
        None,
        Simple,
        Concentric,
        //ConcentricWithSimpleLayout,
        Crosshatch,
        CrosshatchWithSimpleLayout,
        Length
    }

    public class Mapgen_TestChamber : IMapGenerator
    {
        public void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
        {
            galaxy.AddPlanet( PlanetType.Normal, Engine_AIW2.GalaxyCenter, Context );
        }

        public void SeedNormalEntities( Planet planet, ArcenSimContext Context, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.CombatCenter;

            if ( TestChamberTable.Instance.Rows.Count <= 0 )
                return;

            TestChamber chamber = TestChamberTable.Instance.Rows[0];

            for ( int i = 0; i < chamber.Instructions.Count; i++ )
            {
                TestChamberInstruction instruction = chamber.Instructions[i];
                CombatSide side = planet.Combat.GetFirstSideOfType( instruction.SideType );
                ArcenPoint point = center;
                point.X += instruction.SpawnXOffset;
                point.Y += instruction.SpawnYOffset;
                GameEntity.CreateNew( side, instruction.EntityType, point, Context );
            }
        }

        public void SeedSpecialEntities( Galaxy galaxy, ArcenSimContext Context, MapTypeData MapData )
        {
        }
    }
}