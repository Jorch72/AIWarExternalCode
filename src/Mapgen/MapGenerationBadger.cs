using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arcen.AIW2.External
{
  public enum LinkMethod
  {
    None,
    SpanningTree,
    Gabriel,
    RNG,
    SpanningTreeWithConnections
  }
    /* This is a helper class. It mostly contains routines for "Connect a list of planets
       according to a given algorithm" and "Place planets in an interesting but
       pleasing fashion", with a few others */

  internal static class BadgerUtilityMethods
  {

    public static int getSettingValueInt(string settingName)
    {
      //for a given setting, get the int value for it
      bool debug = false;
      ArcenSparseLookup<string, ArcenSetting> lookup = ArcenSettingTable.Instance.LookupByName;
      ArcenSetting setting = null;
      if(lookup.GetHasKey(settingName))
        setting = lookup[settingName];
      if(debug)
        ArcenDebugging.ArcenDebugLogSingleLine("setting " + settingName + ": retrieving value " + setting.DefaultIntValue, Verbosity.DoNotShow);

      int valueAsInt;
      if(setting == null)
        {
          ArcenDebugging.ArcenDebugLogSingleLine("BUG: setting is not defined ", Verbosity.DoNotShow);
          return -1;
        }
      if ( !Int32.TryParse( setting.TempValue_String, out valueAsInt ) )
        {
          if(debug)
            ArcenDebugging.ArcenDebugLogSingleLine("setting " + settingName + " is not currently initialized. Set it to the default " + setting.DefaultIntValue, Verbosity.DoNotShow);
          setting.TempValue_String = setting.DefaultIntValue.ToString();
          valueAsInt = setting.DefaultIntValue;
        }
      if(debug)
        ArcenDebugging.ArcenDebugLogSingleLine(settingName + " is " + valueAsInt, Verbosity.DoNotShow);
      return valueAsInt;
    }

    public static bool getSettingValueBool(string settingName)
    {
      //for a given setting, get the bool value for it
      ArcenSparseLookup<string, ArcenSetting> lookup = ArcenSettingTable.Instance.LookupByName;
      ArcenSetting setting = null;
      if(lookup.GetHasKey(settingName))
        setting = lookup[settingName];

      bool valueAsBool;
      if(setting == null)
        {
          ArcenDebugging.ArcenDebugLogSingleLine("BUG: setting is not defined ", Verbosity.DoNotShow);
          return false;
        }
      valueAsBool = setting.TempValue_Bool;
      return valueAsBool;
    }

    public static ArcenSetting getSettingByName(string settingName)
    {
      //return the ArcenSetting for the setting name
      ArcenSetting setting = null;
      ArcenSparseLookup<string, ArcenSetting> settingMap =  ArcenSettingTable.Instance.LookupByName;
      if(settingMap.GetHasKey(settingName))
        {
          setting = settingMap[settingName];
        }
      return setting;
    }

    internal static void removeSomeLinksBetweenPlanets(int maxToRemove, List<Planet> planetsForMap, ArcenSimContext Context)
      {
        //attempts to remove up to maxToRemove links at random
        int linksToRemove = maxToRemove;
        int linksRemovedSoFar = 0;
        int numAttempts = 0;
        int maxAttempts = 100;
        while(linksRemovedSoFar < linksToRemove)
          {
            numAttempts++;
            if(numAttempts > maxAttempts)
              break;
            int planetIdxForDel = Context.QualityRandom.NextWithInclusiveUpperBound( 0, planetsForMap.Count - 1 );
            Planet planetToDelLink = planetsForMap[planetIdxForDel];
            List<Planet> neighbors = BadgerUtilityMethods.getNeighborList(planetToDelLink);
            if(neighbors.Count < 2) //skip planets with only one neighbor
              continue; 
            int neighborIdx = Context.QualityRandom.NextWithInclusiveUpperBound( 0, neighbors.Count - 1 );
            Planet neighbor = neighbors[neighborIdx];
            planetToDelLink.RemoveLinkTo(neighbor);
            if(BadgerUtilityMethods.isGalaxyFullyConnected(planetsForMap))
              {
                linksRemovedSoFar++;
              }
            else
              planetToDelLink.AddLinkTo(neighbor); //since this unconnected the galaxy, put the link back
             }
      }
    internal static List<Planet> getNeighborList(Planet planet)
      {
        List<Planet> neighbors = new List<Planet>();
        planet.DoForLinkedNeighbors( delegate (Planet neighbor)
                                            {
                                              neighbors.Add(neighbor);
                                              return DelReturn.Continue;
                                            } );
        return neighbors;
      }
    internal static bool isGalaxyFullyConnected(List<Planet> planetsForMap)
    {
          //check for map connectivity (which will be done after stripping a few connections out)
          Planet firstPlanet = planetsForMap[0];
          List<Planet> connectedPlanets = new List<Planet>(); //only allocate one of these, then we'll clear it each time

          connectedPlanets.Add(firstPlanet);

          for(int i = 0; i < connectedPlanets.Count; i++)
            {
              Planet planetToCheck = connectedPlanets[i];
              planetToCheck.DoForLinkedNeighbors( delegate (Planet neighbor)
                                                  {
                                                    if(!connectedPlanets.Contains(neighbor))
                                                       connectedPlanets.Add(neighbor);
                                                    return DelReturn.Continue;
                                                  } );
            }
          if(connectedPlanets.Count == planetsForMap.Count)
            return true;
          return false;
    }
    //Note this needs to take a method of giving probabilities
    internal static LinkMethod getRandomLinkMethod(int percentSpanningTree, int percentGabriel,
                                                   int percentRNG, int percentSpanningTreeWithConnections,
                                                   ArcenSimContext Context)
    {
        if(percentSpanningTreeWithConnections + percentRNG + percentGabriel + percentSpanningTree != 100)
          {
            ArcenDebugging.ArcenDebugLogSingleLine("BUG! percentages given to getRandomLinkMethod do not add up to 100", Verbosity.DoNotShow);
          }
        LinkMethod val = LinkMethod.None;
        int linkingMethodRand = Context.QualityRandom.NextWithInclusiveUpperBound(0, 100);


        if(linkingMethodRand < percentGabriel)
          val = LinkMethod.Gabriel;
        else if(linkingMethodRand < percentGabriel + percentRNG)
          val = LinkMethod.RNG;
        else if(linkingMethodRand < percentGabriel + percentRNG + percentSpanningTree)
          val = LinkMethod.SpanningTree;
        else if(linkingMethodRand < percentGabriel + percentRNG + percentSpanningTree + percentSpanningTreeWithConnections)
          val = LinkMethod.SpanningTreeWithConnections;
        return val;
    }
    
    //adds a circle of points
    internal static List<ArcenPoint> addCircularPoints(int numPoints, ArcenSimContext Context, ArcenPoint circleCenter, int circleRadius,
                                                       ref List<ArcenPoint> pointsSoFar)
      {
        float startingAngle = Context.QualityRandom.NextFloat( 1, 359 );
        List<ArcenPoint> pointsForThisCircle = new List<ArcenPoint>();
        for ( int i = 0; i < numPoints; i++ )
          {
            float angle = ( 360f / (float)numPoints ) * (float)i; // yes, this is theoretically an MP-sync problem, but a satisfactory 360 arc was simply not coming from the FInt approximations and I'm figuring the actual full-sync at the beginning of the game should sync things up before they matter
            angle += startingAngle;
            if ( angle >= 360f )
              angle -= 360f;
            ArcenPoint pointOnRing = circleCenter;
            pointOnRing.X += (int)Math.Round( circleRadius * (float)Math.Cos( angle * ( Math.PI / 180f ) ) );
            pointOnRing.Y += (int)Math.Round( circleRadius * (float)Math.Sin( angle * ( Math.PI / 180f ) ) );
            pointsForThisCircle.Add(pointOnRing);
            pointsSoFar.Add(pointOnRing);
          }
        return pointsForThisCircle;
      }
    //adds an ellipse of points
    internal static List<ArcenPoint> addElipticalPoints(int numPoints, ArcenSimContext Context, ArcenPoint ellipseCenter, int ellipseMajorAxis,
                                                        int ellipseMinorAxis, double rotationRad, ref List<ArcenPoint> pointsSoFar) {
      float startingAngle = Context.QualityRandom.NextFloat(1, 359);
      List<ArcenPoint> pointsForThisCircle = new List<ArcenPoint>();
      for(int i = 0; i < numPoints; i++) {
        float angle = (360f / (float)numPoints) * (float)i; // yes, this is theoretically an MP-sync problem, but a satisfactory 360 arc was simply not coming from the FInt approximations and I'm figuring the actual full-sync at the beginning of the game should sync things up before they matter
        angle += startingAngle;
        if(angle >= 360f)
          angle -= 360f;
        double angleRad = angle / 180 * Math.PI;
        ArcenPoint pointOnRing = ellipseCenter;
        double tan = Math.Sin(angleRad) / Math.Cos(angleRad);
        double x = ellipseMajorAxis * ellipseMinorAxis / Math.Sqrt(ellipseMinorAxis * ellipseMinorAxis + ellipseMajorAxis * ellipseMajorAxis * tan * tan);
        //heck if I know why this has to be done, but the ellipse gets twisted without it
        if(angle >= 90) x = -x;
        if(angle >= 270) x = -x;
        double y = x * Math.Sin(angleRad) / Math.Cos(angleRad);
        double xn = x * Math.Cos(rotationRad) - y * Math.Sin(rotationRad);
        double yn = x * Math.Sin(rotationRad) + y * Math.Cos(rotationRad);
        pointOnRing.X += (int)xn;
        pointOnRing.Y += (int)yn;
        pointsForThisCircle.Add(pointOnRing);
        pointsSoFar.Add(pointOnRing);/**/
      }
      return pointsForThisCircle;
    }

    internal static List<ArcenPoint> addGrid()
      {
        return null;

      }
    
    //this version of AddPointsInCircle can provide some other points that must be avoided
    internal static List<ArcenPoint> addPointsInCircleWithExclusion(int numPoints, ArcenSimContext Context, ArcenPoint circleCenter, int circleRadius,
                                             int minDistanceBetweenPlanets, ref List<ArcenPoint> pointsSoFar, List<ArcenPoint> pointsToAvoid, int distanceFromAvoidance, int divisibleByX = 0)
    {
        //keeps track of previously added planets as well
        int numberFailuresAllowed = 1000;
        List<ArcenPoint> pointsForThisCircle = new List<ArcenPoint>();
        List<ArcenPoint> pointsToAvoidWithoutThisCenter = new List<ArcenPoint>(pointsToAvoid);
        pointsToAvoidWithoutThisCenter.Remove(circleCenter);
         for ( int i = 0; i < numPoints; i++)
           {
             ArcenPoint testPoint = circleCenter.GetRandomPointWithinDistance(Context.QualityRandom, 0, circleRadius);
             if(divisibleByX != 0)
               {
                 testPoint.X -= testPoint.X%divisibleByX;
                 testPoint.Y -= testPoint.Y%divisibleByX;
               }
             if ( UtilityMethods.HelperDoesPointListContainPointWithinDistance( pointsSoFar, testPoint, minDistanceBetweenPlanets))
               {
                 i--;
                 numberFailuresAllowed--;
                 if(numberFailuresAllowed <= 0)
                   {
                     numberFailuresAllowed = 1000;
                     minDistanceBetweenPlanets -= 10;
                   }
                 continue;
               }
                 if (UtilityMethods.HelperDoesPointListContainPointWithinDistance( pointsToAvoidWithoutThisCenter, testPoint, distanceFromAvoidance))
                   {
                     i--;
                     numberFailuresAllowed--;
                     if(numberFailuresAllowed <= 0)
                       {
                         numberFailuresAllowed = 1000;
                         distanceFromAvoidance -= 10;
                       }
                     continue;
                   }

             pointsForThisCircle.Add(testPoint);
             pointsSoFar.Add(testPoint);
           }
         return pointsForThisCircle;
    }

    internal static ArcenPoint  GetRandomPointWithinRectangle( ArcenPoint topL, ArcenPoint topR,
                                                                          ArcenPoint bottomL, ArcenPoint bottomR,
                                                                          ArcenSimContext Context)
    {
      int minLegalX = Math.Min( topL.X, bottomL.X);
      int maxLegalX = Math.Min( topR.X, bottomR.X);
      int minLegalY = Math.Min( bottomL.Y, bottomR.Y);
      int maxLegalY = Math.Min( topL.Y, topR.Y);
      int newX = Context.QualityRandom.Next( minLegalX, maxLegalX );
      int newY = Context.QualityRandom.Next( minLegalY, maxLegalY );
      ArcenPoint newPoint = ArcenPoint.Create( newX, newY );
      return newPoint;
    }

    //adds in a rectangle to roughly cover the screen
    //The X and Y values here were arrived at by crude trial and error
    internal static List<ArcenPoint> addPointsInStartScreen(int numPoints, ArcenSimContext Context, int minDistanceBetweenPlanets,
                                                            ref List<ArcenPoint> pointsSoFar, int divisibleByX = 1)
      {
            int x = 700;
            int y = 400;
            ArcenPoint galaxyCenter = Engine_AIW2.GalaxyCenter;
            ArcenPoint topL = ArcenPoint.Create(-x,y);
            ArcenPoint topR = ArcenPoint.Create(x ,y);
            ArcenPoint bottomL = ArcenPoint.Create(-x,y);
            ArcenPoint bottomR = ArcenPoint.Create(x,-y);
            return BadgerUtilityMethods.addPointsInRectangle(numPoints, Context, minDistanceBetweenPlanets, ref pointsSoFar,
                                                             divisibleByX, topL, topR, bottomL, bottomR);

      }

    internal static List<ArcenPoint> addPointsInRectangle(int numPoints, ArcenSimContext Context, int minDistanceBetweenPlanets,
                                                          ref List<ArcenPoint> pointsSoFar, int divisibleByX, ArcenPoint topL, ArcenPoint topR,
                                                          ArcenPoint bottomL, ArcenPoint bottomR)
      {
        //keeps track of previously added planets as well
        int numberFailuresAllowed = 1000;
        List<ArcenPoint> pointsForThisRectangle = new List<ArcenPoint>();
        for (int i = 0; i < numPoints; i++)
          {
            ArcenPoint testPoint = GetRandomPointWithinRectangle(topL, topR,
                                                                 bottomL, bottomR, Context);
            if (divisibleByX != 0)
              {
                testPoint.X -= testPoint.X % divisibleByX;
                testPoint.Y -= testPoint.Y % divisibleByX;
              }
            if (UtilityMethods.HelperDoesPointListContainPointWithinDistance(pointsSoFar, testPoint, minDistanceBetweenPlanets))
              {
                i--;
                numberFailuresAllowed--;
                if (numberFailuresAllowed <= 0)
                  {
                    numberFailuresAllowed = 1000;
                    minDistanceBetweenPlanets -= 10;
                    if (minDistanceBetweenPlanets < 0)
                      {
                        ArcenDebugging.ArcenDebugLogSingleLine("COULD NOT GENERATE A PLANET BAD JUJU GURU #253421", Verbosity.ShowAsError);
                        return pointsForThisRectangle;
                      }
                  }
                continue;
              }
            pointsForThisRectangle.Add(testPoint);
            pointsSoFar.Add(testPoint);
          }
        return pointsForThisRectangle;
    }

    //Add points within a circle defined by circleCenter and circleRadius
    //It gives a more ordered appearance when the points are on "more reasonable" numbers, so include the
    //divisible
    internal static List<ArcenPoint> addPointsInCircle(int numPoints, ArcenSimContext Context, ArcenPoint circleCenter, int circleRadius,
                                             int minDistanceBetweenPlanets, ref List<ArcenPoint> pointsSoFar, int divisibleByX = 0)
    {
      //keeps track of previously added planets as well
      int numberFailuresAllowed = 1000;
      List<ArcenPoint> pointsForThisCircle = new List<ArcenPoint>();
      bool debug = false;
      for (int i = 0; i < numPoints; i++)
      {
        if(debug)
          ArcenDebugging.ArcenDebugLogSingleLine("generating planet #" + i + " with minDistance " + minDistanceBetweenPlanets, Verbosity.ShowAsInfo);

        ArcenPoint testPoint = circleCenter.GetRandomPointWithinDistance(Context.QualityRandom, 0, circleRadius);
        if (divisibleByX != 0)
        {
          if(debug)
            {
              string s = System.String.Format("addPointsInCircle: previous {0},{1}",
                                       testPoint.X, testPoint.Y);
              ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            }
          testPoint.X -= testPoint.X % divisibleByX;
          testPoint.Y -= testPoint.Y % divisibleByX;
          if(debug)
            {
              string s = System.String.Format("addPointsInCircle: Adjusting planet to {0},{1}",
                                       testPoint.X, testPoint.Y);
              ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
              }
        }
        if (UtilityMethods.HelperDoesPointListContainPointWithinDistance(pointsSoFar, testPoint, minDistanceBetweenPlanets))
        {
          i--;
          numberFailuresAllowed--;
          if (numberFailuresAllowed <= 0)
          {
            //If we have exceeded the number of allowed failures,
            //decrease the minDistance and retry
            numberFailuresAllowed = 1000;
            minDistanceBetweenPlanets -= 10;
            if (minDistanceBetweenPlanets < 10)
            {
              ArcenDebugging.ArcenDebugLogSingleLine("COULD NOT GENERATE A PLANET BAD JUJU GURU #253421", Verbosity.ShowAsError);
              return pointsForThisCircle;
            }
          }
          continue;
        }
        pointsForThisCircle.Add(testPoint);
        pointsSoFar.Add(testPoint);
        if(debug)
          {

            string s = System.String.Format("addPointsInCircle: Adding planet {0} at location {1},{2}", i,
                                     testPoint.X, testPoint.Y);
            ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
          }
      }
      return pointsForThisCircle;
    }


    //the random connections can't be too close; they must be at least
    //minumum hops apart
    //if numRandomConnections == -1 then "use an appropriate number"
    internal static void addRandomConnections(List<Planet> planetsForMap, int numRandomConnections, ArcenSimContext Context, int minimumHops)
    {
      int firstPlanetIdx = 0;
      int secondPlanetIdx = 0;
      List<int> usedPlanetsForConnections = new List<int>();
      int maxRetries = 1000;
      int numRetries = 0;
      bool debug = false;
      if(numRandomConnections == -1)
        {
          if(planetsForMap.Count < 20)
            numRandomConnections = 1;
          else if(planetsForMap.Count < 40)
            numRandomConnections = 2;
          else if(planetsForMap.Count < 50)
            numRandomConnections = 3;
          else if(planetsForMap.Count < 60)
            numRandomConnections = 4;
          else if(planetsForMap.Count < 80)
            numRandomConnections = 5;
          else if(planetsForMap.Count < 100)
            numRandomConnections = 6;
          else
            numRandomConnections = 8;
        }
      if(debug)
        {
          string s = System.String.Format("Adding {0} random connections", numRandomConnections);
          ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow);
        }
      for(int i = 0; i < numRandomConnections; i++)
        {
          do{
            firstPlanetIdx = Context.QualityRandom.Next(0, planetsForMap.Count);
            //make sure we don't use this planet twice
            if(debug)
              {
                string s = System.String.Format("Attempt at first planet: {0} ", firstPlanetIdx);
                ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow);
              }

            for(int j = 0; j < usedPlanetsForConnections.Count; j++)
              {
                if(firstPlanetIdx == usedPlanetsForConnections[j])
                  firstPlanetIdx = -1;
              }
          }while(firstPlanetIdx == -1);
          if(debug)
            {
              string s = System.String.Format("First random planet: {0}", firstPlanetIdx);
              ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow);
            }

          //lets get all the planets within X hops of the first planet,
          //to make sure we get an interesting link
          int[] neighbors;
          minimumHops++; //increment this now to make the following loop easy
          do{
            minimumHops--; //decrease the hops until we get enough planets to work with
            neighbors = getNeighbors(firstPlanetIdx, minimumHops, planetsForMap);
          }while(planetsForMap.Count - neighbors.Length > numRandomConnections - i);

          //use the neighborhood generated above to find a potential planet
          //to link to the first planet
          do{
            secondPlanetIdx = Context.QualityRandom.Next(0, planetsForMap.Count);
            if(secondPlanetIdx == firstPlanetIdx)
              secondPlanetIdx = -1;
            for(int j = 0; j < usedPlanetsForConnections.Count; j++)
              {
                if(secondPlanetIdx == usedPlanetsForConnections[j])
                  secondPlanetIdx = -1;
              }
            if(isNeighborAlready(neighbors, planetsForMap.Count, secondPlanetIdx))
              secondPlanetIdx = -1;
          }while(secondPlanetIdx == -1);

          //Two potential planets are selected for random connections (neither has
          //a random connection yet)
          //Lets make sure a link between t hem does not cause any overlap in lines
          Planet firstPlanet = planetsForMap[firstPlanetIdx];
          Planet secondPlanet = planetsForMap[secondPlanetIdx];
          ArcenPoint p1 = firstPlanet.GalaxyLocation;
          ArcenPoint p2 = secondPlanet.GalaxyLocation;
          bool wouldLinkCrossOtherPlanets = false;
          for(int crossIdx1 = 0; crossIdx1 < planetsForMap.Count; crossIdx1++)
            {
              if((crossIdx1 == firstPlanetIdx) || (crossIdx1 == secondPlanetIdx))
                continue;
              ArcenPoint crossP1 = planetsForMap[crossIdx1].GalaxyLocation;
              for(int crossIdx2 = 0; crossIdx2 < planetsForMap.Count; crossIdx2++)
                {
                  if(crossIdx1 == crossIdx2)
                    continue;
                  if((crossIdx2 == firstPlanetIdx) || (crossIdx2 == secondPlanetIdx))
                    continue;
                  if(!planetsForMap[crossIdx1].GetIsDirectlyLinkedTo(planetsForMap[crossIdx2]))
                    continue;
                  ArcenPoint crossP2 = planetsForMap[crossIdx2].GalaxyLocation;

                  if(Mat.LineSegmentIntersectsLineSegment(p1, p2, crossP1, crossP2, 10))
                    {
                      if(debug)
                        {
                          string s = System.String.Format("Planets {0} ({4},{5}) and {1} ({6},{7}) connection overlaps with {2}-{3} ({8},{9})-({10},{11}). {12} planets found so far",
                                                   firstPlanetIdx, secondPlanetIdx, crossIdx1, crossIdx2,
                                                   p1.X, p1.Y, p2.X, p2.Y, crossP1.X, crossP1.Y, crossP2.X, crossP2.Y, i);
                          ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow);
                        }
                      wouldLinkCrossOtherPlanets = true;
                      crossIdx2 = planetsForMap.Count;
                      crossIdx1 = planetsForMap.Count;
                    }
                }
            }

          if(wouldLinkCrossOtherPlanets)
            {
              i--; //discard this pair, since there's an overlap
              neighbors = null; //lets not leak memory
            }
          else
            {
              //Found a valid link, create it
              firstPlanet.AddLinkTo(secondPlanet);
              usedPlanetsForConnections.Add(firstPlanetIdx);
              usedPlanetsForConnections.Add(secondPlanetIdx);
            }
          numRetries++;
          if(numRetries > maxRetries)
            {
              numRetries = 0;
              if(debug)
                {
                  string s = System.String.Format("Exceeded retry limit with {0} hop minimum; retry", minimumHops);
                  ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow);
                }
              if(minimumHops > 2)
                {
                  minimumHops--; //make it easier to find matches
                }
              else
                {
                  return;
                }
            }
        }
      }
    //this code is for addRandomConnections; we want to not link
    //planets already close to eachother
    internal static int[] getNeighbors(int planetIdx, int degreeOfNeighbors, List<Planet>planetsForMap)
    {
        bool debug = false;
        if(debug)
          {
            string s = System.String.Format("returning list of all planets {0} or fewer hops from {1}", degreeOfNeighbors, planetIdx);
            ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow);
          }
        Planet testPlanet = planetsForMap[planetIdx];
        int[] neighbors = new int[planetsForMap.Count];
        int neighborsSoFar = 0;
        for(int i = 0; i < planetsForMap.Count; i++)
          neighbors[i] = -1;
        //test for all immediate neighbors
        for(int i = 0; i < planetsForMap.Count; i++)
          {
            if(i == planetIdx)
              continue;
            Planet potentialNewNeighbor = planetsForMap[i];
            if(testPlanet.GetIsDirectlyLinkedTo(potentialNewNeighbor))
              {
                neighbors[neighborsSoFar] = i;
                neighborsSoFar++;
                if(debug)
                  {
                    string s = System.String.Format("{0} --> one hop {1} ({2} neighbors so far)", planetIdx, i, neighborsSoFar);
                    ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow);
                  }
              }
          }
        //now count down remaining degrees
        //note that for a large number of hops and a small number of planets, we might
        //get all the planets before we run out of hops
        while(degreeOfNeighbors > 1 && neighborsSoFar < planetsForMap.Count)
          {
            int newNeighbors = 0;
            for(int i = 0; i < neighborsSoFar; i++)
              {
                //now we check all the current neighbors to see who their neighbors are,
                //which will give us the next degree of neighborness

                int planetIdxForNeighbor = neighbors[i];
                if(debug)
                  {
                    string s = System.String.Format("Checking for connections to {0}", planetIdxForNeighbor);
                    ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow);
                  }
                for(int j = 0; j < planetsForMap.Count -1; j++)
                  {
                    //check this planet (a neighbor) for all connections that
                    //are not itself and are also not on the list
                    if(j == planetIdx)
                      continue;
                    if(isNeighborAlready(neighbors, neighborsSoFar + newNeighbors, j))
                      {
                        if(debug)
                          {
                            string s = System.String.Format(" {0} is on the list already, skip it", j);
                            ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow);
                          }
                        continue;
                      }

                    if(debug)
                      {
                        string s = System.String.Format("Checking {0} against {1} (out of {2} total planets). We have {3} neighbors so far", planetIdxForNeighbor, j, planetsForMap.Count, neighborsSoFar);
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow);
                      }

                    Planet currentNeighbor = planetsForMap[planetIdxForNeighbor];
                    Planet potentialNewNeighbor = planetsForMap[j];
                    if(currentNeighbor.GetIsDirectlyLinkedTo(potentialNewNeighbor))
                      {
                        if(debug)
                          {
                            string s = System.String.Format("{0} is directly linked to {1} (neighborsSoFar {2} newNeighbors {3}", planetIdxForNeighbor, j, neighborsSoFar, newNeighbors);
                            ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow);
                          }

                        neighbors[neighborsSoFar + newNeighbors] = j;
                        newNeighbors++;
                        if(debug)
                          {
                            string s = System.String.Format("{0} --> {1} hop {2}", planetIdxForNeighbor, degreeOfNeighbors, j);
                            ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow);
                          }
                      }
                  }
              }
            neighborsSoFar += newNeighbors;
            degreeOfNeighbors--;
          }
        return neighbors;
    }

    //checks if element is already in array. I don't always want to look at every
    //element
    internal static bool isNeighborAlready(int [] neighborList, int numElemToCheck, int element)
    {
      for(int i = 0; i < numElemToCheck; i++)
        {
          if(neighborList[i] == element)
            return true;
        }
      return false;
    }

    internal static List<Planet> convertPointsToPlanets(List<ArcenPoint> vertices, Galaxy galaxy, ArcenSimContext Context)
    {
      List<Planet> planetsForMap = new List<Planet>();
      for(int i = 0; i < vertices.Count; i++)
        {
          Planet planet = galaxy.AddPlanet(PlanetType.Normal, vertices[i], Context);
          planetsForMap.Add(planet);
        }
      return planetsForMap;
    }
    /* This returns a matrix where matrix[i][j] == 1 means point i and point j should be connected 
       Has the same algorithm as createMinimumSpanningTree, but it doesn't do the linking. */
    internal static int[,] createMinimumSpanningTreeLinks(List<ArcenPoint> pointsForGraph)
    {
        int [,] connectionArray;
        connectionArray = new int[pointsForGraph.Count, pointsForGraph.Count];
        for(int i = 0; i < pointsForGraph.Count; i++)
          {
            for(int j = 0; j < pointsForGraph.Count; j++)
              {
                connectionArray[i,j] = 0;
              }
          }
        List<int>verticesNotInTree = new List<int>();
        List<int>verticesInTree = new List<int>();

        // ArcenDebugging.ArcenDebugLogSingleLine("Creating minimum spanning tree now", Verbosity.DoNotShow);
        for(int i = 0; i < pointsForGraph.Count; i++)
          verticesNotInTree.Add(i);
        //Pick first element, then remove it from the list
        int pointIdx  = verticesNotInTree[0];
        verticesNotInTree.RemoveAt(0);
        verticesInTree.Add(pointIdx);

        //initialize adjacency matrix for Prim's algorithm
        //the adjacency matrix contains entries as follows
        //pointIdxNotInTree <closest point in tree> <distance to closest point>
        //In the body of the algorithm we look at this matrix to figure out
        //which point to add to the tree next, then update it for the next iteration
        int[,] spanningAdjacencyMatrix;
        spanningAdjacencyMatrix = new int[pointsForGraph.Count, 3];
        for(int i = 0; i < pointsForGraph.Count; i++)
          {
                spanningAdjacencyMatrix[i,0] = i;
                spanningAdjacencyMatrix[i,1] = -1;
                spanningAdjacencyMatrix[i,1] = 9999;
          }
        //loop until all vertices are in the tree
        while(verticesNotInTree.Count > 0)
          {
            //update the adjacency matrix
            //for each element NOT in the tree, find the closest
            //element in the tree
            for(int i = 0; i < verticesNotInTree.Count; i++)
              {
                int minDistance = 9999;
                for(int j = 0; j < verticesInTree.Count; j++)
                  {
                    int idxNotInTree = verticesNotInTree[i];
                    int idxInTree = verticesInTree[j];
                    ArcenPoint pointNotInTree = pointsForGraph[idxNotInTree];
                    ArcenPoint pointInTree = pointsForGraph[idxInTree];
                    int distance = Mat.DistanceBetweenPoints(pointNotInTree, pointInTree);
                    if(distance < minDistance)
                      {
                        spanningAdjacencyMatrix[idxNotInTree,1] = idxInTree;
                        spanningAdjacencyMatrix[idxNotInTree,2] = distance;
                        minDistance = distance;
                      }
                  }
              }

            //now pick the closest edge
            // s = System.String.Format("Examine the remaining {0} vertices to find which to add",
            //                          verticesNotInTree.Count);
            // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            int minDistanceFound = 9999;
            int closestPointIdx = -1;
            int pointToAdd = -1;
            for(int i = 0; i < verticesNotInTree.Count; i++)
              {
                pointIdx = verticesNotInTree[i];
                // s = System.String.Format( "To find closest edge, examine {0} of {1} (idx {4}), minDistance {2} dist for this point {3}",
                //                           i, verticesNotInTree.Count , minDistanceFound, spanningAdjacencyMatrix[pointIdx, 2], pointIdx);
                // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                if(spanningAdjacencyMatrix[pointIdx,2] == 0)
                  {
                    //don't try to link a point to itself
                    continue;
                  }
                if(spanningAdjacencyMatrix[pointIdx, 2] < minDistanceFound)
                  {
                    minDistanceFound = spanningAdjacencyMatrix[pointIdx,2];
                    closestPointIdx = spanningAdjacencyMatrix[pointIdx,1];
                    pointToAdd = pointIdx;
                  }
              }
            // s = System.String.Format( "Adding point idx {0} closest neighbor ({1}. distance {2} to tree", pointToAdd,
            //                           closestPointIdx, minDistanceFound);
            // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            //Now lets add this point to the Tree
            verticesNotInTree.Remove(pointToAdd);
            verticesInTree.Add(pointToAdd);
            spanningAdjacencyMatrix[pointToAdd, 2] = 9999;
            connectionArray[pointToAdd,closestPointIdx] = 1;
            connectionArray[closestPointIdx,pointToAdd] = 1;
          }
        return connectionArray;
    }
    /* This returns a matrix where matrix[i][j] == 1 means point i and point j should be connected 
       Has the same algorithm as createGabrielGraph, but a seperate implementation */
    internal static int[,]  createGabrielGraphLinks(List<ArcenPoint> pointsForGraph)
    {
        //Algorithm: for each node
        //                          find midpoint to another node
        //                          Check that no other planets are in the circle connecting the two nodes
        //                              If no other planets, link these two planets
        //see htts://en.wikipedia.org/wiki/Gabriel_graph
      int [,] connectionArray;
      connectionArray = new int[pointsForGraph.Count, pointsForGraph.Count];
      for(int i = 0; i < pointsForGraph.Count; i++)
        {
          for(int j = 0; j < pointsForGraph.Count; j++)
            {
              connectionArray[i,j] = 0;
            }
        }
        //Here i and j iterate over potential pairs. For each potential pair, iterate over k,
        //which is every other point, to make sure k is not too close to i and j
        for(int i = 0; i < pointsForGraph.Count ; i++)
          {
            // s = System.String.Format("Outer Loop: Point {0} of {1}", i, pointsForGraph.Count);
            // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            for(int j = 0; j < pointsForGraph.Count  ; j++)
              {
                if(j == i)
                  continue;

                ArcenPoint pointOne = pointsForGraph[i];
                ArcenPoint pointTwo = pointsForGraph[j];
                ArcenPoint midPoint = ArcenPoint.Create(
                                                        (pointOne.X + pointTwo.X)/2,
                                                        (pointOne.Y + pointTwo.Y)/2);
                int radiusOfCircle = Mat.DistanceBetweenPoints( pointOne, midPoint );

                bool isThereAnotherPoint = false;

                for(int k = 0; k < pointsForGraph.Count; k++)
                  {
                    //Now check each other planet to see if they would fall into the circle
                    //centered on the midpoint between i and j
                    if((k == i) || (k == j))
                      continue; //don't compare to yourself
                    int distanceFromMidpoint;
                    ArcenPoint pointForCircleCheck = pointsForGraph[k];
                    distanceFromMidpoint = Mat.DistanceBetweenPoints(pointForCircleCheck, midPoint);
                    if((distanceFromMidpoint - radiusOfCircle) <= 0)
                      {
//                        s = System.String.Format("Inner Loop: Compare {0}-->{1}. Planet {2} overlaps circle", i, j, k);
//                        ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                        isThereAnotherPoint = true;
                        k = pointsForGraph.Count; //don't bother checking any other planets, since we have one in the circle
                      }

                  }
                if(!isThereAnotherPoint)
                  {
                    // s = System.String.Format("Inner Loop: Putting link between {0} --> {1}", i, j);
                    // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

                    //if there were no other planets, link planetOne and planetTwo
                    connectionArray[i, j] = 1;
                    connectionArray[j, i] = 1;
                  }
              }
          }
        return connectionArray;
    }
    /* This returns a matrix where matrix[i][j] == 1 means point i and point j should be connected 
       Has the same algorithm as createGabrielGraph, but a seperate implementation */
    internal static int[,]  createRNGGraphLinks(List<ArcenPoint> pointsForGraph)
    {
      //Algorithm: for each pair of nodes i and j
        //           check if any other node k is closer to both i and j than they are to eachother
        //           if no such k exists, link i and j
        int [,] connectionArray;
        connectionArray = new int[pointsForGraph.Count, pointsForGraph.Count];
        for(int i = 0; i < pointsForGraph.Count; i++)
          {
            for(int j = 0; j < pointsForGraph.Count; j++)
              {
                connectionArray[i,j] = 0;
              }
        }

        for(int i = 0; i < pointsForGraph.Count ; i++)
          {
            //the minus one is because the last planet in the last can't compare itself to itself
            // s = System.String.Format("Outer Loop: point {0} of {1}", i, pointsForGraph.Count);
            // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            for(int j = 0; j < pointsForGraph.Count  ; j++)
              {
                if(i == j)
                  continue;
                // s = System.String.Format("  Middle Loop: Point {0} of {1}", j, pointsForGraph.Count);
                // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

                ArcenPoint pointOne = pointsForGraph[i];
                ArcenPoint pointTwo = pointsForGraph[j];
                int distanceBetweenPoints = Mat.DistanceBetweenPoints( pointOne, pointTwo );
                bool isThereAnotherPoint = false;
                for(int k = 0; k < pointsForGraph.Count; k++)
                  {
                    if((k == i) || (k == j))
                      continue;

                    ArcenPoint pointForCheck = pointsForGraph[k];
                    int distanceFromOne = Mat.DistanceBetweenPoints(pointForCheck, pointOne);
                    int distanceFromTwo = Mat.DistanceBetweenPoints(pointForCheck, pointTwo);
                    if((distanceFromOne < distanceBetweenPoints) && (distanceFromTwo < distanceBetweenPoints))
                      {
                         // s = System.String.Format("    Inner Loop: Compare {0}-->{1}. Point {2} is close enough to prevent a link", i, j, k);
                         // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                        isThereAnotherPoint = true;
                        k = pointsForGraph.Count; //don't bother checking any other planets
                      }
                    }
                if(!isThereAnotherPoint)
                  {
                    // s = System.String.Format("    Inner Loop: Putting link between {0} --> {1}", i, j);
                    // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

                    //if there were no other planets, link planetOne and planetTwo
                    connectionArray[i, j] = 1;
                    connectionArray[j, i] = 1;

                    // ArcenDebugging.ArcenDebugLogSingleLine("    link added sucessfully", Verbosity.DoNotShow);
                  }
              }
          }
        return connectionArray;
      }
    internal static  void createMinimumSpanningTree(List<Planet> planetsForMap)
    {
        List<int>verticesNotInTree = new List<int>();
        List<int>verticesInTree = new List<int>();
        // ArcenDebugging.ArcenDebugLogSingleLine("Creating minimum spanning tree now", Verbosity.DoNotShow);
        for(int i = 0; i < planetsForMap.Count; i++)
          verticesNotInTree.Add(i);
        //Pick first element, then remove it from the list
        int planetIdx  = verticesNotInTree[0];
        verticesNotInTree.RemoveAt(0);
        verticesInTree.Add(planetIdx);

        //initialize adjacency matrix for Prim's algorithm
        //the adjacency matrix contains entries as follows
        //planetIdxNotInTree <closest planet in tree> <distance to closest planet>
        //In the body of the algorithm we look at this matrix to figure out
        //which planet to add to the tree next, then update it for the next iteration
        int[,] spanningAdjacencyMatrix;
        spanningAdjacencyMatrix = new int[planetsForMap.Count, 3];
        for(int i = 0; i < planetsForMap.Count; i++)
          {
                spanningAdjacencyMatrix[i,0] = i;
                spanningAdjacencyMatrix[i,1] = -1;
                spanningAdjacencyMatrix[i,1] = 9999;
          }
        //loop until all vertices are in the tree
        while(verticesNotInTree.Count > 0)
          {
            //update the adjacency matrix
            //for each element NOT in the tree, find the closest
            //element in the tree
            for(int i = 0; i < verticesNotInTree.Count; i++)
              {
                int minDistance = 9999;
                for(int j = 0; j < verticesInTree.Count; j++)
                  {
                    int idxNotInTree = verticesNotInTree[i];
                    int idxInTree = verticesInTree[j];
                    Planet planetNotInTree = planetsForMap[idxNotInTree];
                    Planet planetInTree = planetsForMap[idxInTree];
                    int distance = Mat.DistanceBetweenPoints(planetNotInTree.GalaxyLocation, planetInTree.GalaxyLocation);
                    if(distance < minDistance)
                      {
                        spanningAdjacencyMatrix[idxNotInTree,1] = idxInTree;
                        spanningAdjacencyMatrix[idxNotInTree,2] = distance;
                        minDistance = distance;
                      }
                  }
              }

            //now pick the closest edge
            // s = System.String.Format("Examine the remaining {0} vertices to find which to add",
            //                          verticesNotInTree.Count);
            // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            int minDistanceFound = 9999;
            int closestPlanetIdx = -1;
            int planetToAdd = -1;
            for(int i = 0; i < verticesNotInTree.Count; i++)
              {
                planetIdx = verticesNotInTree[i];
                // s = System.String.Format( "To find closest edge, examine {0} of {1} (idx {4}), minDistance {2} dist for this planet {3}",
                //                           i, verticesNotInTree.Count , minDistanceFound, spanningAdjacencyMatrix[planetIdx, 2], planetIdx);
                // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                if(spanningAdjacencyMatrix[planetIdx,2] == 0)
                  {
                    //don't try to link a planet to itself
                    continue;
                  }
                if(spanningAdjacencyMatrix[planetIdx, 2] < minDistanceFound)
                  {
                    minDistanceFound = spanningAdjacencyMatrix[planetIdx,2];
                    closestPlanetIdx = spanningAdjacencyMatrix[planetIdx,1];
                    planetToAdd = planetIdx;
                  }
              }
            // s = System.String.Format( "Adding planet idx {0} closest neighbor ({1}. distance {2} to tree", planetToAdd,
            //                           closestPlanetIdx, minDistanceFound);
            // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            //Now lets add this planet to the Tree
            verticesNotInTree.Remove(planetToAdd);
            verticesInTree.Add(planetToAdd);
            spanningAdjacencyMatrix[planetToAdd, 2] = 9999;
            planetsForMap[closestPlanetIdx].AddLinkTo(planetsForMap[planetToAdd]);
          }
    }

    
    internal static void createGabrielGraph(List<Planet> planetsForMap)
    {
        //Algorithm: for each node
        //                          find midpoint to another node
        //                          Check that no other planets are in the circle connecting the two nodes
        //                              If no other planets, link these two planets
        //see htts://en.wikipedia.org/wiki/Gabriel_graph
        //Here i and j iterate over potential pairs. For each potential pair, iterate over k,
        //which is every other planet, to make sure k is not too close to i and j
        for(int i = 0; i < planetsForMap.Count ; i++)
          {
            // s = System.String.Format("Outer Loop: Planet {0} of {1}", i, planetsForMap.Count);
            // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            for(int j = 0; j < planetsForMap.Count  ; j++)
              {
                if(j == i)
                  continue;
                Planet planetOne = planetsForMap[i];
                Planet planetTwo = planetsForMap[j];
                ArcenPoint pointOne = planetOne.GalaxyLocation;
                ArcenPoint pointTwo = planetTwo.GalaxyLocation;
                ArcenPoint midPoint = ArcenPoint.Create(
                                                        (pointOne.X + pointTwo.X)/2,
                                                        (pointOne.Y + pointTwo.Y)/2);
                int radiusOfCircle = Mat.DistanceBetweenPoints( pointOne, midPoint );

                bool isThereAnotherPlanet = false;

                for(int k = 0; k < planetsForMap.Count; k++)
                  {
                    //Now check each other planet to see if they would fall into the circle
                    //centered on the midpoint between i and j
                    if((k == i) || (k == j))
                      continue; //don't compare to yourself
                    int distanceFromMidpoint;
                    Planet planetForCircleCheck = planetsForMap[k];
                    distanceFromMidpoint = Mat.DistanceBetweenPoints(planetForCircleCheck.GalaxyLocation, midPoint);
                    if((distanceFromMidpoint - radiusOfCircle) <= 0)
                      {
//                        s = System.String.Format("Inner Loop: Compare {0}-->{1}. Planet {2} overlaps circle", i, j, k);
//                        ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                        isThereAnotherPlanet = true;
                        k = planetsForMap.Count; //don't bother checking any other planets, since we have one in the circle
                      }

                  }
                if(!isThereAnotherPlanet)
                  {
                    // s = System.String.Format("Inner Loop: Putting link between {0} --> {1}", i, j);
                    // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

                    //if there were no other planets, link planetOne and planetTwo
                    planetOne.AddLinkTo(planetTwo);
                  }
              }
          }
    }
  
    internal static void createRNGGraph(List<Planet> planetsForMap)
      {
        //Algorithm: for each pair of nodes i and j
        //           check if any other node k is closer to both i and j than they are to eachother
        //           if no such k exists, link i and j
        for(int i = 0; i < planetsForMap.Count ; i++)
          {
            //the minus one is because the last planet in the last can't compare itself to itself
            // s = System.String.Format("Outer Loop: Planet {0} of {1}", i, planetsForMap.Count);
            // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            for(int j = 0; j < planetsForMap.Count  ; j++)
              {
                if(i == j)
                  continue;
                // s = System.String.Format("  Middle Loop: Planet {0} of {1}", j, planetsForMap.Count);
                // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

                Planet planetOne = planetsForMap[i];
                Planet planetTwo = planetsForMap[j];
                ArcenPoint pointOne = planetOne.GalaxyLocation;
                ArcenPoint pointTwo = planetTwo.GalaxyLocation;
                int distanceBetweenPoints = Mat.DistanceBetweenPoints( pointOne, pointTwo );
                bool isThereAnotherPlanet = false;
                for(int k = 0; k < planetsForMap.Count; k++)
                  {
                    if((k == i) || (k == j))
                      continue;

                    Planet planetForCheck = planetsForMap[k];
                    int distanceFromOne = Mat.DistanceBetweenPoints(planetForCheck.GalaxyLocation, pointOne);
                    int distanceFromTwo = Mat.DistanceBetweenPoints(planetForCheck.GalaxyLocation, pointTwo);
                    if((distanceFromOne < distanceBetweenPoints) && (distanceFromTwo < distanceBetweenPoints))
                      {
                         // s = System.String.Format("    Inner Loop: Compare {0}-->{1}. Planet {2} is close enough to prevent a link", i, j, k);
                         // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                        isThereAnotherPlanet = true;
                        k = planetsForMap.Count; //don't bother checking any other planets
                      }
                    }
                if(!isThereAnotherPlanet)
                  {
                    // s = System.String.Format("    Inner Loop: Putting link between {0} --> {1}", i, j);
                    // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

                    //if there were no other planets, link planetOne and planetTwo
                    planetOne.AddLinkTo(planetTwo);
                    // ArcenDebugging.ArcenDebugLogSingleLine("    link added sucessfully", Verbosity.DoNotShow);
                  }
              }
          }
//        ArcenDebugging.ArcenDebugLogSingleLine("Finished adding links", Verbosity.DoNotShow);
      }

    //This function will return an array of integers to make it easy
    //to divvy up a set of planets into regions
    //onlyOneOfLowestMin exists because sometimes you only want 1 of the
    //smallest group; if this is "true" then it will make sure we only
    //have at most one group of minPlanetsPerGroup
    internal static  List<int> allocatePlanetsIntoGroups(int minPlanetsPerGroup, int maxPlanetsPerGroup, int planetsToAllocate, bool onlyOneOfLowestMin, ArcenSimContext Context)
    {
        int maxPossibleGroups = planetsToAllocate/minPlanetsPerGroup;
        int planetsLeftToAllocate = planetsToAllocate;
        List<int> planetsPerGroup = new List<int>();
        
        // s = System.String.Format( "allocatePlanetsIntoGroups min {0} max {1} planetsToAllocate {2} maxPossibleGroups {3}", minPlanetsPerGroup, maxPlanetsPerGroup, planetsToAllocate, maxPlanetsPerGroup);
        // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

        for( int i = 0; i <= maxPossibleGroups; i++)
          {
            //Hand out batches of planets in groups until
            //we run out of planets
            // s = System.String.Format( "Iter {0} of {1}: {2} planets left", i, maxPossibleGroups, planetsLeftToAllocate);
            // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

            if(planetsLeftToAllocate == 0)
              {
//                ArcenDebugging.ArcenDebugLogSingleLine("No planets left; break", Verbosity.DoNotShow);
                break;
              }
            if(i == maxPossibleGroups -1 || planetsLeftToAllocate < maxPlanetsPerGroup)
              {
                //handle case for the last group, or once the number of remaining
                //planets drops low enough
  //              ArcenDebugging.ArcenDebugLogSingleLine("This is the last group", Verbosity.DoNotShow);
                planetsPerGroup.Add(planetsLeftToAllocate);
              }
            else if(planetsLeftToAllocate < minPlanetsPerGroup + maxPlanetsPerGroup)
              {
                //handle the case where we are close to the end (so we don't wind up with a really awkward
                //last case
                planetsPerGroup.Add(minPlanetsPerGroup);
              }
            else
              {
                //pick a nice friendly random number of planets
                int planetsForThisGroup = Context.QualityRandom.NextWithInclusiveUpperBound( minPlanetsPerGroup, maxPlanetsPerGroup );
                planetsPerGroup.Add(planetsForThisGroup);
                if(planetsForThisGroup == minPlanetsPerGroup && onlyOneOfLowestMin)
                  minPlanetsPerGroup++;
              }
            planetsLeftToAllocate -= planetsPerGroup[i];

            // s = System.String.Format( "Iter {0}: {1} planets left", i, planetsLeftToAllocate);
            // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
          }
        return planetsPerGroup;
    }
    //links currentPlanets and newPlanets
    //We will eventually have a couple styles of links (link nearest planets, link
    //nearest planet in one two nearest 2 in the other, link two different but nearby planets,
    //etc, but right now just link the two closest)
    internal static void linkPlanetLists(List<Planet>currentPlanets, List<Planet>newPlanets, ArcenPoint centerOfNewPlanets, bool combinePlanets = true)
      {
        //find the planet X in currentPlanets closest to centerOfNewPlanets,
        //then find the planet in newPlanets closest to X, then link them
        int[] distanceFromCurrentToNew = new int[currentPlanets.Count];
        int[] distanceFromNewToCurrent = new int[newPlanets.Count];
        bool debug = false;
        if(debug)
          {
            string s = System.String.Format("Comparing {0} planets (current) with {1} planets (new)", currentPlanets.Count, newPlanets.Count);
            ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
          }
        for(int i = 0; i < currentPlanets.Count; i++)
          {
            distanceFromCurrentToNew[i] = Mat.DistanceBetweenPoints(currentPlanets[i].GalaxyLocation, centerOfNewPlanets);
          }
        int closestCurrentIdx = findNextValueInList(distanceFromCurrentToNew, 0, currentPlanets.Count);
        int secondClosestCurrentIdx = findNextValueInList(distanceFromCurrentToNew, distanceFromCurrentToNew[closestCurrentIdx] + 1, currentPlanets.Count);;
        if(debug)
          {
            string s = System.String.Format("current Closest planet {0}, second closest {1}", closestCurrentIdx, secondClosestCurrentIdx);
            ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
          }
        Planet closestToNewPlanets = currentPlanets[closestCurrentIdx];
        Planet secondClosestToNewPlanets = currentPlanets[secondClosestCurrentIdx];
        //now find the closest planet in newPlanets to closestToNewPlanets

        for(int i = 0; i < newPlanets.Count; i++)
          {
            distanceFromNewToCurrent[i] = Mat.DistanceBetweenPoints(closestToNewPlanets.GalaxyLocation, newPlanets[i].GalaxyLocation);
          }
        int closestNewIdx = findNextValueInList(distanceFromNewToCurrent, 0, newPlanets.Count);
        int secondClosestNewIdx = findNextValueInList(distanceFromNewToCurrent, distanceFromNewToCurrent[closestNewIdx] + 1, newPlanets.Count);
        if(debug)
          {
            string s = System.String.Format("new Closest planet {0}, second closest {1}", closestNewIdx, secondClosestNewIdx);
            ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
          }
        closestToNewPlanets.AddLinkTo(newPlanets[closestNewIdx]);
      
      //        closestToNewPlanets.AddLinkTo(newPlanets[secondClosestNewIdx]);
      //secondClosestToNewPlanets.AddLinkTo(newPlanets[secondClosestNewIdx]);
      if (combinePlanets)
      {
        for (int i = 0; i < newPlanets.Count; i++)
        {
          currentPlanets.Add(newPlanets[i]);
        }
      }
      }
        //returns the index of the smallest distance that's larger than smallestDistanceSoFar
    //ie if our distances are 4, 5, 6, 7 and smallestDistanceSoFar == 5
    //then it would return 6
    internal static int findNextValueInList(int[]distanceFromCenter, int smallestDistanceSoFar, int numRegions)
      {
        int idx = -1;
        int bestFit = 9999;
        for(int i = 0; i < numRegions; i++)
          {
            //note must be >= smallestDistanceSoFar in case two regions are equidistant
            //this is allowable because we delete the entry for each match after it is made
            if(distanceFromCenter[i] >= smallestDistanceSoFar && distanceFromCenter[i] < bestFit)
              {
                idx = i;
                bestFit = distanceFromCenter[i];
              }
          }
        return idx;
      }
  }


//Graph generators work as follows:
//Select a random set of vertices (planets)
//Then for use a chosen method to link the planets

  class Mapgen_Graph : Mapgen_Base
  {
    List<ArcenPoint> vertices;

    bool gabriel;
    bool spanning;
    bool rng;
    bool debug = false;
    int radiusForPlanetPlacement;
    int numPlanetsDesired;
    public override void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
    {
      bool spanningRandomConnections = true;
      gabriel = false;
      spanning = false;
      rng = false;
      string mapName = mapType.InternalName;
      //numberToSeed =  BadgerUtilityMethods.getSettingValueInt("NumPlanets");
      //if(numberToSeed == 0)
      //  numberToSeed = 80;
      numPlanetsDesired = numberToSeed;
      int linkingMethodNum =  BadgerUtilityMethods.getSettingValueInt("SimpleLinkMethod");
      if(debug)
        ArcenDebugging.ArcenDebugLogSingleLine("Value from simpleLinkMethod was " + linkingMethodNum, Verbosity.DoNotShow);
      if(linkingMethodNum == 0)
        linkingMethodNum = 1;
      if ( linkingMethodNum == 1)
        {
          if(debug)
            {
              string s =  System.String.Format( "Using a random neighborhood graph for Simple,  {0} planets", numberToSeed );
              ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            }
          rng = true;
        }

      if ( linkingMethodNum == 2)
        {
          if(debug)
            {
              string s =  System.String.Format( "Using generating a gabriel graph for Dreamcatcher,  {0} planets", numberToSeed );
              ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            }
          gabriel = true;
        }
      if ( linkingMethodNum == 3)
        {
          if(debug)
            {

              string s =  System.String.Format( "Using a spanning tree for Constellation,  {0} planets", numberToSeed );
              ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            }
          spanning = true;
        }

      vertices = new List<ArcenPoint>();

      //Set up points. Do points only first
      //because this way if I want to use a later algorithm
      //that might delete certain entries, I don't have to try
      //to remove a planet from the Galaxy because I'm not sure
      //how to do that.
      setUpPoints( numberToSeed, Context);
      //Now we have matching lists of points and planets
      List<Planet> planetsForMap = BadgerUtilityMethods.convertPointsToPlanets(vertices, galaxy, Context);


      if(gabriel)
        {
          BadgerUtilityMethods.createGabrielGraph(planetsForMap);
          //remove a few links at random
          BadgerUtilityMethods.removeSomeLinksBetweenPlanets(10, planetsForMap, Context);
        }
      if(rng)
        {
          BadgerUtilityMethods.createRNGGraph(planetsForMap);
          //remove a few links at random
          BadgerUtilityMethods.removeSomeLinksBetweenPlanets(2, planetsForMap, Context);

        }
      if(spanning)
        {
          //create a minimal spanning tree using Prim's algorithm https://en.wikipedia.org/wiki/Prim%27s_algorithm
          BadgerUtilityMethods.createMinimumSpanningTree(planetsForMap);
          if(spanningRandomConnections)
            {
              int numRandomConnections;
              if(numberToSeed < 40)
                numRandomConnections = 2;
              else if(numberToSeed < 50)
                numRandomConnections = 3;
              else if(numberToSeed < 60)
                numRandomConnections = 4;
              else if(numberToSeed < 80)
                numRandomConnections = 5;
              else if(numberToSeed < 100)
                numRandomConnections = 6;
              else
                numRandomConnections = 7;
              int minimumHops = 3;
              if(numberToSeed < 40)
                 minimumHops = 2;
              BadgerUtilityMethods.addRandomConnections(planetsForMap, numRandomConnections, Context, minimumHops);
            }
        }
      }

    //this function is a great place for tuning the eventual map
    //There's a lot of worthwhile experimentation to do here
    public void setUpPoints(int numberToSeed, ArcenSimContext Context)
      {
        //My first pass for this just steals the code from intraClusterPlanetPoints
        if(debug)
          ArcenDebugging.ArcenDebugLogSingleLine("Generating poitns in setUpPoints", Verbosity.DoNotShow);
        int minimumDistanceBetweenPlanets = 50;
        bool circlePlacement = false; //we can distribute in either a circle or a rectangle
        if(circlePlacement)
          {
            if(numberToSeed < 40)
              radiusForPlanetPlacement = 400;
            else if(numberToSeed <= 60)
              radiusForPlanetPlacement = 450;
            else if(numberToSeed <= 80)
              radiusForPlanetPlacement = 550;
            else if(numberToSeed <= 100)
              radiusForPlanetPlacement = 600;
            else
              radiusForPlanetPlacement = 700;
            if(gabriel || rng)
              {
                //It looks better for these maps if things are more spread out
                radiusForPlanetPlacement += 100; 
              }
            //Here is my first pass (oneBigCircle) and a second attempt
            //where I spread things out a bit more
            bool useOneBigCircle = false;
            if(useOneBigCircle)
              {
                ArcenPoint Center =  Engine_AIW2.GalaxyCenter;

                BadgerUtilityMethods.addPointsInCircle(numberToSeed, Context, Center, radiusForPlanetPlacement,
                                                       minimumDistanceBetweenPlanets, ref vertices);
                if(debug)
                  ArcenDebugging.ArcenDebugLogSingleLine("points generated in a big circle", Verbosity.DoNotShow);
              }
            else
              {
                if(debug)
                  ArcenDebugging.ArcenDebugLogSingleLine("points generated from multiple circles", Verbosity.DoNotShow);
                // if(gabriel || rng)
                //   radiusForPlanetPlacement -= 100;
                ArcenPoint p1 = ArcenPoint.Create(0, 200);
                ArcenPoint p2 = ArcenPoint.Create(200, 0);
                ArcenPoint p3 = ArcenPoint.Create(-200, 0 );
                ArcenPoint p4 = ArcenPoint.Create(0, -200);
                int seedsPerCircle = numberToSeed / 4;
                int extraPoints = numberToSeed % 4;
                //             ArcenDebugging.ArcenDebugLogSingleLine("using p1", Verbosity.DoNotShow);
                BadgerUtilityMethods.addPointsInCircle(seedsPerCircle, Context, p1, radiusForPlanetPlacement,
                                                     minimumDistanceBetweenPlanets, ref vertices);
                //              ArcenDebugging.ArcenDebugLogSingleLine("using p2", Verbosity.DoNotShow);
                BadgerUtilityMethods.addPointsInCircle(seedsPerCircle, Context, p2, radiusForPlanetPlacement,
                                                       minimumDistanceBetweenPlanets, ref  vertices);
                //              ArcenDebugging.ArcenDebugLogSingleLine("using p3", Verbosity.DoNotShow);
                BadgerUtilityMethods.addPointsInCircle(seedsPerCircle, Context, p3, radiusForPlanetPlacement,
                                                       minimumDistanceBetweenPlanets, ref vertices);
                //              ArcenDebugging.ArcenDebugLogSingleLine("using p4", Verbosity.DoNotShow);
                BadgerUtilityMethods.addPointsInCircle(seedsPerCircle + extraPoints, Context, p4, radiusForPlanetPlacement,
                                                       minimumDistanceBetweenPlanets, ref vertices);
              }
            }
        else
          {
            BadgerUtilityMethods.addPointsInStartScreen(numberToSeed, Context, minimumDistanceBetweenPlanets, ref vertices);
          }
      }
  }


  public class Mapgen_Circles : Mapgen_Base
  {

    public override void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
    {
      bool linkNormally = true;
      bool linkRNG = false;
      bool linkSpanning = false;
      bool linkGabriel = false;
      //this map type is vaguely like solar systems, but it's intended to be a different
      //sort of style. The layout is
      //One central circle, surrounded by a bunch of smaller circles. Every point in the center connects to its
      //outer circle. So in a Solar System POV, the central circle are the Suns, the outer circle are its planets orbiting
      //but we have this layout (instead of the suns at the center of the orbiting planets) because it's much more readable
      //numberToSeed =  BadgerUtilityMethods.getSettingValueInt("NumPlanets");
      //if(numberToSeed == 0)
      //  numberToSeed = 80;
      string mapName = mapType.InternalName;
      int linkingMethodNum =  BadgerUtilityMethods.getSettingValueInt("SolarSystemsLinkMethod");
      if(linkingMethodNum == 0)
        linkingMethodNum = 1;
      if ( linkingMethodNum == 1) //the default
        linkNormally = true;
      if ( linkingMethodNum == 2)
        {
          linkNormally = false;
          linkRNG = true;
        }
      if ( linkingMethodNum == 3)
        {
          linkNormally = false;
          linkGabriel = true;
        }

      if ( linkingMethodNum == 4)
        {
          linkNormally = false;
          linkSpanning = true;
        }

      int planetsLeftToAllocate = numberToSeed;

      int minPlanetsPerCircle = 4; //it's kinda nice if there's a single circle of 4 planets,
                                   //but more than one such is boring. Once we have
                                   // one such circle, we bump this value up
      int maxPlanetsPerCircle = 9;
      //figure out how many circles there are, and how many planets are in each circle
      List<int> planetsPerCircle = BadgerUtilityMethods.allocatePlanetsIntoGroups(minPlanetsPerCircle,
                                                                               maxPlanetsPerCircle, numberToSeed, true,
                                                                               Context);

      int numberOfCircles = planetsPerCircle.Count;   
      //Now create each circle

      int outerCircleRadius = getRadiusOfOuterCircles(numberOfCircles);
      int innerCircleRadius = getRadiusOfSmallCircle(numberOfCircles);
      List<ArcenPoint> innerCirclePlanetPoints;
      //note that this function returns the locations of the inner circle planets
      //as an out parameter (otherwise it was hard to get the center of the outer circle to be on the same
      //line as the Sun)
      List<ArcenPoint> centerOfOuterCircles = getCenterOfOuterCircles(galaxy, Context,
                                                                      numberOfCircles, outerCircleRadius,
                                                                      Engine_AIW2.GalaxyCenter, out innerCirclePlanetPoints,
                                                                      innerCircleRadius);

      //create the inner circle (aka Suns)
      List<Planet> innerCirclePlanets = makeCircle(galaxy, Context, innerCirclePlanetPoints);
      if(linkNormally)
        BadgerUtilityMethods.createRNGGraph(innerCirclePlanets);
      for(int i = 0; i < centerOfOuterCircles.Count; i++)
        {
          //create all the outer planets for this circle
          List<Planet> planetsForThisCircle = makeCircle(galaxy, Context,
                                                                   planetsPerCircle[i], getRadiusOfSmallCircle(planetsPerCircle[i]),  centerOfOuterCircles[i]);
          if(linkNormally)
            {
              //link the planets of the solar system (aka outer circle)
              BadgerUtilityMethods.createRNGGraph(planetsForThisCircle);
              //link the inner planet (Sun) to the nearest member of its orbiting planet
              linkPlanetToNearestCircle(galaxy, innerCirclePlanets[i], planetsForThisCircle);
            }
        }
      if(linkSpanning)
        {
          BadgerUtilityMethods.createMinimumSpanningTree(galaxy.Planets);
          BadgerUtilityMethods.addRandomConnections(galaxy.Planets, 5, Context, 5);
        }
      if(linkGabriel)
        {
          BadgerUtilityMethods.createGabrielGraph(galaxy.Planets);
        }
      if(linkRNG)
        {
          BadgerUtilityMethods.createRNGGraph(galaxy.Planets);
        }

      return;
    }
    //Note that the center of the outer circle and its matching Sun
    //need to be on the same line, so we return the points on the inner circle as an out parameter
    private List<ArcenPoint> getCenterOfOuterCircles(Galaxy galaxy, ArcenSimContext Context, int outerCircles, int outerRadius, ArcenPoint circleCenter,
                                                     out List<ArcenPoint> innerCirclePoints, int innerRadius)
      {
        innerCirclePoints = new List<ArcenPoint>();
        //shamelessly stolen ;-)
        AngleDegrees angleBetweenRingPlanet = AngleDegrees.Create( (FInt)360 / (FInt)outerCircles );
        AngleDegrees ringAngle = AngleDegrees.Create( (FInt)Context.QualityRandom.NextWithInclusiveUpperBound( 10, 350 ) );

        List<ArcenPoint> outerCircleCenters = new List<ArcenPoint>();
        bool flipOffsetForNextRingPlanet = false;
        int offsetOuterFlip = 10;
        //We need to stagger the outer planet radii once there are enough of them
        if(outerCircles > 7)
          offsetOuterFlip = 100;
        if(outerCircles > 12)
          offsetOuterFlip = 200;

        for(int i = 0; i < outerCircles; i++)
          {
            ArcenPoint innerPoint = circleCenter.GetPointAtAngleAndDistance( ringAngle, innerRadius +  (flipOffsetForNextRingPlanet ? 15 : 30 ));
            ArcenPoint outerPoint = circleCenter.GetPointAtAngleAndDistance( ringAngle, outerRadius +  (flipOffsetForNextRingPlanet ? offsetOuterFlip : 0 ));
            outerCircleCenters.Add( outerPoint);
            innerCirclePoints.Add(innerPoint);
            flipOffsetForNextRingPlanet = !flipOffsetForNextRingPlanet;
            ringAngle = ringAngle.Add( angleBetweenRingPlanet );
          }
        return outerCircleCenters;
      }
    //maps the Sun to its corresponding planets
    private void linkPlanetToNearestCircle(Galaxy galaxy, Planet innerCirclePlanet, List<Planet> outerCircle)
    {
      //takes a planet in the inner circle and an outer circle. Link the inner planet and the closest outer planet
      int minDistanceSoFar=9999;
      int indexOfMinDistance = -1;
      for(int i = 0; i < outerCircle.Count; i++)
        {
          Planet outerPlanet = outerCircle[i];
          int distance = Mat.DistanceBetweenPoints(innerCirclePlanet.GalaxyLocation,
                                                   outerPlanet.GalaxyLocation);
          if(distance < minDistanceSoFar)
            {
              minDistanceSoFar = distance;
              indexOfMinDistance = i;
            }
        }
      innerCirclePlanet.AddLinkTo(outerCircle[indexOfMinDistance]);
    }

    //This function is overloaded; one version takes a circleCenter, finds all the planets and then
    //creates/links them
    //the other version starts knowing all the points, then only has to create the planets and link them
    private List<Planet> makeCircle(Galaxy galaxy, ArcenSimContext Context, int planetsOnCircle, int radius, ArcenPoint circleCenter)
    {
      //Generate a connected circle of planets around the circleCenter with a given radius
      PlanetType planetType = PlanetType.Normal;
      //7) Compute average angle for next step from 360/planets_left
      AngleDegrees angleBetweenRingPlanet = AngleDegrees.Create( (FInt)360 / (FInt)planetsOnCircle );

      //8) Pick Random Starting Angle from 0 to 359
      AngleDegrees ringAngle = AngleDegrees.Create( (FInt)Context.QualityRandom.NextWithInclusiveUpperBound( 10, 350 ) );

      List<Planet> ringPlanets = new List<Planet>();
      ArcenPoint linePoint;
      Planet newPlanetInRing;
      bool flipOffsetForNextRingPlanet = false;
      bool debug = false;
      if(debug)
        {
         string s = System.String.Format( "Creating Circle of {0}  planets centered on {1},{2}",//centered on ({1},{2}) at radius {3}",
                                    planetsOnCircle,  circleCenter.X, circleCenter.Y);
         ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
        }

      for(int i = 0; i < planetsOnCircle; i++)
        {
          //-- compute point on line from origin at angle at target radius +40
          linePoint = circleCenter.GetPointAtAngleAndDistance( ringAngle, radius +  (flipOffsetForNextRingPlanet ? 15 : 30 ));

          //-- place planet there
          newPlanetInRing = galaxy.AddPlanet( planetType, linePoint, Context );

          ringPlanets.Add( newPlanetInRing );


          flipOffsetForNextRingPlanet = !flipOffsetForNextRingPlanet;
          ringAngle = ringAngle.Add( angleBetweenRingPlanet );
        }
      return ringPlanets;
    }
    //overloaded version of makeAndConnectCircle for when we already know all the points
    private List<Planet> makeCircle(Galaxy galaxy, ArcenSimContext Context, List<ArcenPoint> planetPoints)
    {
      //Generate a connected circle of planets around the circleCenter with a given radius
      PlanetType planetType = PlanetType.Normal;
      List<Planet> ringPlanets = new List<Planet>();

      for(int i = 0; i < planetPoints.Count; i++)
        {
          //-- place planet there
          Planet newPlanetInRing = galaxy.AddPlanet( planetType, planetPoints[i], Context );
          ringPlanets.Add(newPlanetInRing);
        }
      return ringPlanets;
    }

    private int getRadiusOfSmallCircle(int smallCircle)
      {
        int radius;
        if(smallCircle < 4)
          radius = 45;
        else if(smallCircle < 5)
          radius = 55;
        else if(smallCircle < 6)
          radius = 65;
        else if(smallCircle < 7)
          radius = 75;
        else
          radius = 100;
        return radius;

      }
    private int getRadiusOfOuterCircles(int numberOfSmallCircles)
    {
      int radius;
      //stolen from Wheel
      if(numberOfSmallCircles < 3)
        {
          radius = 300;
        }
      else if(numberOfSmallCircles < 4)
        radius = 310;
      else if(numberOfSmallCircles < 5)
        radius = 320;
      else if(numberOfSmallCircles < 6)
        radius = 370;
      else if(numberOfSmallCircles < 7)
        radius = 390;
      else if(numberOfSmallCircles < 8)
        radius = 400;
      else if(numberOfSmallCircles < 8)
        radius = 410;
      else if(numberOfSmallCircles < 9)
        radius = 430;
      else if(numberOfSmallCircles < 10)
        radius = 440;
      else if(numberOfSmallCircles < 11)
        radius = 460;
      else
        radius = 490;

      return radius;
    }
  }

    public class Mapgen_Tutorial : Mapgen_Base
    {
    /*This class is intended as a teaching exercise. To create a new map generator,
      you must declase a class that implements IMapGenerator. It has a "Generate" function that is
      called by the main AI War 2 code that actually creates the map. The galaxy map is laid out on a giant grid.
      The center of the galaxy is (0,0) and you can have both positive and negative coordinates.

      The input to the Generate function are as follows. The "Galaxy" object is what you populate with Planets
      to create the map for your game. The Context is used to generate random numbers (and I'm sure many other things).
      The numberToSeed is the number of planets, and the mapType is something you (the coder)
      can use if you want to have multiple map types sharing a similar codebase.
      You can look at the RootLatticeGenerator for an example of one IMapGenerator sharing
      multiple mapTypes.

    This example code generates a bunch of random planets simply connected

    One critical thing to avoid is that all planets cannot be connected. If so then you will get an
    error. See Mantis bug 19086

    To have this entry appear as a selection option in the Game Start Screen, add an entry like 
     <map_type name="Tutorial" 
              display_name="ExampleForAspiringModders" <=== this will be the name that appears in Game Select
              description="~*~" <==== This doesn't do anything yet
              dll_name="AIWarExternalCode" <== must be External Code
              type_name="Arcen.AIW2.External.Mapgen_Tutorial" <=== the name of this class
  >
  </map_type>

to GameData/Configuration/MapType/YourFile_MapTypes.xml

*/
        public override void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
        {
          string s; //this is used for debugging printouts later in this function
          numberToSeed = 7;  //lets override the number of planets desired to something small and manageable
          ArcenDebugging.ArcenDebugLogSingleLine("Welcome to the test generator\n" , Verbosity.DoNotShow); //this message will go in PlayerData/ArcenDebugLog.txt
                                                                                                           //this is invaluable for debugging purposes

          //An ArcenPoint is a data structure containing (at least) an X,Y coordinate pair
          //creating a new planet requires an ArcenPoint
          //This test generator will hard code in some ArcenPoints,
          //then put planets at those points

          //The map itself is a cartesian coordinate plane centered on 0,0
          
          PlanetType planetType = PlanetType.Normal; //I think Normal is to say "not a Nomad".
                                                     //unless you know otherwise, always use PlanetType.Normal
          List<ArcenPoint> planetPoints = new List<ArcenPoint>();
          ArcenPoint originPlanetPoint = Engine_AIW2.GalaxyCenter; //this is 0,0.
          Planet originPlanet = galaxy.AddPlanet(planetType, originPlanetPoint, Context);
          ArcenDebugging.ArcenDebugLogSingleLine("populate PlanetPoints list\n" , Verbosity.DoNotShow);
           planetPoints.Add( ArcenPoint.Create( 0, 100)); //so this point has X=0 and Y=100
           planetPoints.Add( ArcenPoint.Create( 500, 0)); // X=500, Y=0
           planetPoints.Add( ArcenPoint.Create( 600, 100));
           planetPoints.Add( ArcenPoint.Create( -100, -100));
           planetPoints.Add( ArcenPoint.Create( 0, -600));
           planetPoints.Add( ArcenPoint.Create( -100, 0));

          numberToSeed = planetPoints.Count;//reset numberToSeed for this example
          int distance = Mat.DistanceBetweenPoints(planetPoints[0], planetPoints[1]); //This is how to check the distance between Points
          ArcenDebugging.ArcenDebugLogSingleLine("I have created my points\n" , Verbosity.DoNotShow);
          Planet previousPlanet = null;
          for(int i = 0; i < numberToSeed -1; i++)
            {
              //this uses a printf-style formatting string to generate a more elaborate debugging message
              s = System.String.Format("Adding planet {0} at location {1},{2}\n", i,
                                       planetPoints[i].X, planetPoints[i].Y);
              ArcenDebugging.ArcenDebugLogSingleLine(s , Verbosity.DoNotShow);
              //calling galaxy.AddPlanet adds a planet to the galaxy; it takes a planetType (probably "Normal"),
              //an ArcenPoint and the Context passed into the Generate functino
              Planet planet = galaxy.AddPlanet(planetType, planetPoints[i], Context);
              if(previousPlanet == null)
                planet.AddLinkTo( originPlanet); //if we have no previous planet, link this planet to the origin planet
              else
                planet.AddLinkTo(previousPlanet);
              previousPlanet = planet;

            }
          int numLinkedToOrigin = originPlanet.GetLinkedNeighborCount();
          bool isLinkedToOrigin = originPlanet.GetIsDirectlyLinkedTo(galaxy.Planets[2]); //checks if this is linked to origin planet
          s = System.String.Format("My galaxy has {0} planets. Return now\n", galaxy.Planets.Count);
          ArcenDebugging.ArcenDebugLogSingleLine(s , Verbosity.DoNotShow);
          return;
        }
    }
  
  public class Mapgen_Nebula : Mapgen_Base
  {
    /* The goal of this map type is to have a varierty of "regions" of planets,
       with a different layout of planets and a randomly chosen linking algorithm for each
       region. This gives a really organic feel to things.

       TODO: add some additional "seeding" algorithms to the initial planet placements (for example, maybe a circle?
       maybe a small grid?)
       Also add some "Link regions differently" code, and maybe consider linking adjacent regions (so it's not just
       always linked in a spanning tree, but maybe in a gabriel or something)
       Also add Spanning + random connections to the way of linking inside a region

       This galaxy type also implements ClustersMini, wherein the regions are tightly packed
    */

    bool debug = false;
    bool veryVerboseDebug = false;
    public override void Generate( Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType )
    {

      //numberToSeed =  BadgerUtilityMethods.getSettingValueInt("NumPlanets");
      //if(numberToSeed == 0)
      //  numberToSeed = 80;
      if(numberToSeed < 20)
        numberToSeed = 20;

      int percentForOneCircle = 20;
      int circleDone = -1;
      bool isAsteroid = false;
      bool addSomeExtraLinks = false; //for clustersMini,
                                      //which is normally linked via spanning tree
      string mapName = mapType.InternalName;
      if ( ArcenStrings.Equals( mapName, "Asteroid") ) 
        isAsteroid = true;

      int numClustersHint = 2;
      int nebulaConnectivity = 2; //tunes the connectivity algorithms for Nebula

      //These parameters are tuned based on the numClustersHint
      int minPlanetsPerRegion = 6;
      int maxPlanetsPerRegion = 17;
      int radiusPerRegion = 130;
      int distanceBetweenRegions = 360;
      int minDistanceBetweenPlanets = 60;

            //get the user requested settings
      nebulaSettingUpdates(out  numClustersHint, out nebulaConnectivity,  isAsteroid);
      if(isAsteroid)
        {
          if(numClustersHint == 1)
            {
              //Few clusters, so each one is larger
              minPlanetsPerRegion = 7;
              maxPlanetsPerRegion = 12;
              radiusPerRegion = 140;
              distanceBetweenRegions = 380;

            }
          if(numClustersHint == 2)
            {
              minPlanetsPerRegion = 5;
              maxPlanetsPerRegion = 10;
              radiusPerRegion = 130;
              distanceBetweenRegions = 360;
            }
          if(numClustersHint == 3)
            {
              minPlanetsPerRegion = 4;
              maxPlanetsPerRegion = 7;
              radiusPerRegion = 120;
              distanceBetweenRegions = 340;
            }
        }
      else
        {
          /* Nebula map mode */
          if(numClustersHint == 1)
            {
              //Few clusters, so each one is larger
              minPlanetsPerRegion = 11;
              maxPlanetsPerRegion = 21;
              radiusPerRegion = 220;
              distanceBetweenRegions = 340;

            }
          if(numClustersHint == 2)
            {
              minPlanetsPerRegion = 6;
              maxPlanetsPerRegion = 18;
              radiusPerRegion = 210;
              distanceBetweenRegions = 180;
            }
          if(numClustersHint == 3)
            {
              minPlanetsPerRegion = 5;
              maxPlanetsPerRegion = 13;
              radiusPerRegion = 190;
              distanceBetweenRegions = 170;
            }


        }
      if(isAsteroid)
        addSomeExtraLinks = BadgerUtilityMethods.getSettingValueBool("addBonusLinksAsteroid");

      if(debug)
        {
          string s = System.String.Format("minPlanetsPerRegion: " + minPlanetsPerRegion + " maxPlanetsPerRegion " + maxPlanetsPerRegion + " radiusPerRegion " + radiusPerRegion + " distanceBetweenRegions " + distanceBetweenRegions);
          ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
        }
      
      bool onlyOneOfSmallestRegion = false;

      int alignmentNumber = 10; //align all points on numbers divisible by this value. It makes things look more organized
      List<int> regionsOfPlanets = BadgerUtilityMethods.allocatePlanetsIntoGroups(minPlanetsPerRegion,
                                                                                  maxPlanetsPerRegion, numberToSeed, onlyOneOfSmallestRegion,
                                                                                  Context);
      if(debug)
        {
          string s = System.String.Format("Planets divvied between {0} regions --> ", regionsOfPlanets.Count);
          for(int i = 0; i < regionsOfPlanets.Count; i++)
            {
              s += "(" +i +"="+ regionsOfPlanets[i] + ") ";
            }
          ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
        }

      //now for each region, find a center point (chosen randomly)
      //then allocate the points
      List<ArcenPoint> regionCenters = new List<ArcenPoint>();
      BadgerUtilityMethods.addPointsInStartScreen(regionsOfPlanets.Count, Context,
                                                  distanceBetweenRegions, ref regionCenters,alignmentNumber);
      if(veryVerboseDebug)
        {
          for(int i = 0; i < regionCenters.Count; i++)
            {
              string s = System.String.Format("Region Center: {0}, {1}", regionCenters[i].X, regionCenters[i].Y);
              ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            }
        }
      List<ArcenPoint> allPoints = new List<ArcenPoint>();
      List<List<Planet>> allPlanetsPerRegion = new List<List<Planet>>();
      List<Planet> allPlanets = new List<Planet>();

      /* Tuning parameters for how to link the various Nebulae/Asteroids */
      
      //percentages for linking within a region
      
      //settings for the Nebula
      int percentSpanningTreeInRegion;
      int percentSpanningTreeWithConnectionsInRegion;
      int percentGabrielInRegion;
      int percentRNGInRegion;
      //percentages for linking the different regions (inter-region)
      LinkMethod regionLinkMethod = LinkMethod.Gabriel;
      getNebulaLinkingPercentages(isAsteroid, nebulaConnectivity, addSomeExtraLinks,
                                  out  percentSpanningTreeInRegion, out percentSpanningTreeWithConnectionsInRegion,
                                  out  percentGabrielInRegion, out  percentRNGInRegion,
                                  out  regionLinkMethod);
      
      

      for(int i = 0; i < regionCenters.Count; i++)
        {
          //For each region, add planets and then link the region together
          List<ArcenPoint> pointsForThisRegion;
          if(isAsteroid && circleDone == -1 &&
             regionsOfPlanets[i] > 4 && regionsOfPlanets[i] < 9 &&
             percentForOneCircle > Context.QualityRandom.Next(0,100)) // chance of a circle for Asteroids
            {
              if(debug)
                ArcenDebugging.ArcenDebugLogSingleLine("Adding a circle", Verbosity.DoNotShow);
              //sometimes we might want to just make a circle
              circleDone = i;
              List<ArcenPoint> temp = new List<ArcenPoint>();
              pointsForThisRegion = BadgerUtilityMethods.addCircularPoints(regionsOfPlanets[i], Context, regionCenters[i],
                                                      radiusPerRegion, ref temp);
            }
          else
            pointsForThisRegion = BadgerUtilityMethods.addPointsInCircleWithExclusion(regionsOfPlanets[i], Context, regionCenters[i], radiusPerRegion,
                                                                                      minDistanceBetweenPlanets, ref allPoints, regionCenters, radiusPerRegion + 20, alignmentNumber);

          List<Planet> planetsForThisRegion = BadgerUtilityMethods.convertPointsToPlanets(pointsForThisRegion, galaxy, Context);
          if(veryVerboseDebug)
            {
              string s = System.String.Format("Added planets for region {0} of {1}", i, regionCenters.Count);
              ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            }
          //Now pick a random method of linking these planets together

          LinkMethod method = BadgerUtilityMethods.getRandomLinkMethod(percentSpanningTreeInRegion, percentGabrielInRegion,
                                                                       percentRNGInRegion, percentSpanningTreeWithConnectionsInRegion,
                                                                       Context);
          if(method == LinkMethod.Gabriel)
            {
              BadgerUtilityMethods.createGabrielGraph(planetsForThisRegion);
              int maxToRemove = 2;
              if(regionsOfPlanets[i] < 8)
                maxToRemove = 1;
              if(circleDone == i)
                maxToRemove = 0;
              BadgerUtilityMethods.removeSomeLinksBetweenPlanets(maxToRemove, planetsForThisRegion, Context);
            }
          else if(method == LinkMethod.RNG)
            {
              //RNG
              BadgerUtilityMethods.createRNGGraph(planetsForThisRegion);
              int maxToRemove = 1;
              if(regionsOfPlanets[i] < 8)
                maxToRemove = 0;
              if(circleDone == i)
                maxToRemove = 0;

              BadgerUtilityMethods.removeSomeLinksBetweenPlanets(maxToRemove, planetsForThisRegion, Context);
            }
          else if(method == LinkMethod.SpanningTree)
            {
              //SpanningTree
              BadgerUtilityMethods.createMinimumSpanningTree(planetsForThisRegion);
            }
          else
            {
              //SpanningTree + random connections
              BadgerUtilityMethods.createMinimumSpanningTree(planetsForThisRegion);
              BadgerUtilityMethods.addRandomConnections(planetsForThisRegion, 1, Context, 2);
            }
          allPlanetsPerRegion.Add(planetsForThisRegion);
          allPlanets.AddRange(planetsForThisRegion);
          if(veryVerboseDebug)
            {
              string s = System.String.Format("Planets for region {0} of {1} are now linked", i, regionCenters.Count);
              ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            }
        }


      if(regionLinkMethod == LinkMethod.SpanningTree)
        {
          int[,] connectionMatrix = BadgerUtilityMethods.createMinimumSpanningTreeLinks(regionCenters);
          for(int i = 0; i < regionCenters.Count; i++)
            {
              for(int j = i + 1; j < regionCenters.Count; j++)
                {
                  if(j >= regionCenters.Count)
                    ArcenDebugging.ArcenDebugLogSingleLine("BUG! FIXME", Verbosity.DoNotShow);
                  if(connectionMatrix[i,j] == 1)
                    {
                      BadgerUtilityMethods.linkPlanetLists(allPlanetsPerRegion[i], allPlanetsPerRegion[j], regionCenters[j]);
                    }
                }
            }
        }
      else if(regionLinkMethod == LinkMethod.Gabriel)
        {
          int[,] connectionMatrix = BadgerUtilityMethods.createGabrielGraphLinks(regionCenters);
          for(int i = 0; i < regionCenters.Count; i++)
            {
              for(int j = i + 1; j < regionCenters.Count; j++)
                {
                  if(j >= regionCenters.Count)
                    ArcenDebugging.ArcenDebugLogSingleLine("BUG! FIXME", Verbosity.DoNotShow);
                  if(connectionMatrix[i,j] == 1)
                    {
                      BadgerUtilityMethods.linkPlanetLists(allPlanetsPerRegion[i], allPlanetsPerRegion[j], regionCenters[j]);
                    }
                }
            }
        }
      else if(regionLinkMethod == LinkMethod.RNG)
        {
          int[,] connectionMatrix = BadgerUtilityMethods.createRNGGraphLinks(regionCenters);
          for(int i = 0; i < regionCenters.Count; i++)
            {
              for(int j = i + 1; j < regionCenters.Count; j++)
                {
                  if(j >= regionCenters.Count)
                    ArcenDebugging.ArcenDebugLogSingleLine("BUG! FIXME", Verbosity.DoNotShow);
                  if(connectionMatrix[i,j] == 1)
                    {
                      BadgerUtilityMethods.linkPlanetLists(allPlanetsPerRegion[i], allPlanetsPerRegion[j], regionCenters[j]);
                    }
                }
            }
        }
      else if(regionLinkMethod == LinkMethod.SpanningTreeWithConnections)
        {
          int[,] connectionMatrix = BadgerUtilityMethods.createMinimumSpanningTreeLinks(regionCenters);
          for(int i = 0; i < regionCenters.Count; i++)
            {
              for(int j = i + 1; j < regionCenters.Count; j++)
                {
                  if(j >= regionCenters.Count)
                    ArcenDebugging.ArcenDebugLogSingleLine("BUG! FIXME", Verbosity.DoNotShow);
                  if(connectionMatrix[i,j] == 1)
                    {
                      BadgerUtilityMethods.linkPlanetLists(allPlanetsPerRegion[i], allPlanetsPerRegion[j], regionCenters[j]);
                    }
                }
            }
          //now lets link everything together at the end a bit better
          int minHops = numberToSeed/10;
          if(minHops > 6)
            minHops = 6;
          if(debug)
            {
              string s = System.String.Format("Adding a few random connections at the end");
              ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            }
          if(addSomeExtraLinks)
          BadgerUtilityMethods.addRandomConnections(allPlanets, 2, Context, 5);
        }
      else
        {
          ArcenDebugging.ArcenDebugLogSingleLine("BUG: linking regions with unknown algorithm", Verbosity.DoNotShow);
        }
    }

      void getNebulaLinkingPercentages(bool isAsteroid, int nebulaConnectivity, bool addSomeExtraLinks,
                                      out int percentSpanningTreeInRegion, out int percentSpanningTreeWithConnectionsInRegion,
                                      out int percentGabrielInRegion, out int percentRNGInRegion,
                                      out LinkMethod InterRegionLinkingMethod)
        {
           percentSpanningTreeInRegion = 10;
           percentSpanningTreeWithConnectionsInRegion = 10;
           percentGabrielInRegion = 40;
           percentRNGInRegion = 40;
           InterRegionLinkingMethod = LinkMethod.Gabriel;
          if(!isAsteroid )
            {
              if(nebulaConnectivity == 1)
                {
                  percentSpanningTreeInRegion = 20;
                  percentSpanningTreeWithConnectionsInRegion = 30;
                  percentGabrielInRegion = 20;
                  percentRNGInRegion = 30;
                  InterRegionLinkingMethod = LinkMethod.RNG;
                }
              if(nebulaConnectivity == 2)
                {
                  percentSpanningTreeInRegion = 10;
                  percentSpanningTreeWithConnectionsInRegion = 10;
                  percentGabrielInRegion = 40;
                  percentRNGInRegion = 40;
                  InterRegionLinkingMethod = LinkMethod.Gabriel;
                }
              if(nebulaConnectivity == 3)
                {
                  percentSpanningTreeInRegion = 5;
                  percentSpanningTreeWithConnectionsInRegion = 5;
                  percentGabrielInRegion = 45;
                  percentRNGInRegion = 45;
                  InterRegionLinkingMethod = LinkMethod.Gabriel;
               } 
            }
          if(isAsteroid)
            {
              percentSpanningTreeInRegion = 0;
              percentSpanningTreeWithConnectionsInRegion = 0;
              percentGabrielInRegion = 100;
              percentRNGInRegion = 0;

              InterRegionLinkingMethod = LinkMethod.SpanningTree;
              
              if(addSomeExtraLinks)
                {
                  InterRegionLinkingMethod = LinkMethod.SpanningTreeWithConnections;
                }
            }


        }
    
    void nebulaSettingUpdates(out int numClustersHint, out int nebulaConnectivity, bool isAsteroid)
      {
        numClustersHint = 2;
        if(isAsteroid)
          numClustersHint = 1;
        nebulaConnectivity = 2;
        ArcenSetting setting;
        
        if(isAsteroid)
          setting = BadgerUtilityMethods.getSettingByName("NumberOfAsteroids");
        else
          setting = BadgerUtilityMethods.getSettingByName("NumberOfNebulae");

        numClustersHint = setting.TempValue_Int;
        if(!isAsteroid)
          {
            setting = BadgerUtilityMethods.getSettingByName("NebulaeConnectivity");
            nebulaConnectivity = setting.TempValue_Int;
          }
        return;
      }

    // void nebulaSettingUpdates( out int minPlanetsPerRegion, out int maxPlanetsPerRegion,
    //                            out int radiusPerRegion, out int distanceBetweenRegions, bool isAsteroid)
    //   {

    //For this version, we passed inthe explicit region sizes. Was my original attempt
    //     //set defaults in case Settings aren't there
    //      minPlanetsPerRegion = 6;
    //      maxPlanetsPerRegion = 17;
    //      radiusPerRegion = 210;
    //      distanceBetweenRegions = 210;
    //      if(isAsteroid)
    //        {
    //          minPlanetsPerRegion = 4;
    //          maxPlanetsPerRegion = 10;
    //          radiusPerRegion = 130;
    //          distanceBetweenRegions = 360;
    //        }

    //     ArcenSetting setting;
    //     if(isAsteroid)
    //       setting = BadgerUtilityMethods.getSettingByName("MinPlanetsPerAsteroidRegion");
    //     else
    //       setting = BadgerUtilityMethods.getSettingByName("MinPlanetsPerNebulaRegion");

    //     if(setting != null && setting.TempValue_Int != 0)
    //       {
    //         minPlanetsPerRegion = setting.TempValue_Int;
    //       }
        
    //     if(isAsteroid)
    //       setting = BadgerUtilityMethods.getSettingByName("MaxPlanetsPerAsteroidRegion");
    //     else
    //       setting = BadgerUtilityMethods.getSettingByName("MaxPlanetsPerNebulaRegion");

    //     if(setting != null  && setting.TempValue_Int != 0)
    //       {
    //         maxPlanetsPerRegion = setting.TempValue_Int;
    //       }
    //     if(isAsteroid)
    //       setting = BadgerUtilityMethods.getSettingByName("radiusPerRegionAsteroid");
    //     else
    //       setting = BadgerUtilityMethods.getSettingByName("radiusPerRegionNebula");

    //     if(setting != null  && setting.TempValue_Int != 0)
    //       {
    //         radiusPerRegion = setting.TempValue_Int;
    //       }
    //     if(isAsteroid)
    //       setting = BadgerUtilityMethods.getSettingByName("distanceBetweenAsteroidRegion");
    //     else
    //       setting = BadgerUtilityMethods.getSettingByName("distanceBetweenNebulaRegion");
    //     if(setting != null  && setting.TempValue_Int != 0)
    //       {
    //         distanceBetweenRegions = setting.TempValue_Int;
    //       }
    //   }
    //returns the index of the smallest distance that's larger than smallestDistanceSoFar
    //ie if our distances are 4, 5, 6, 7 and smallestDistanceSoFar == 5
    //then it would return 6
    public int findNextValue(int[]distanceFromCenter, int smallestDistanceSoFar, int numRegions)
      {
        int idx = -1;
        int bestFit = 9999;
        for(int i = 0; i < numRegions; i++)
          {
            //note must be >= smallestDistanceSoFar in case two regions are equidistant
            //this is allowable because we delete the entry for each match after it is made
            if(distanceFromCenter[i] >= smallestDistanceSoFar && distanceFromCenter[i] < bestFit)
              {
                idx = i;
                bestFit = distanceFromCenter[i];
              }
          }
        return idx;
      }
  }

  public class Mapgen_Octopus : Mapgen_Base
  {
    /* This map type was suggested by Tadrinth on the forums. He couched it as 
       "Spiral galaxy: large cluster in the middle (Body), 8 arms coming off, each arm is a series of linked small clusters.  "
       which made me think of an octopus. So variables are named like it's an octopus

       It was initially coded by BadgerBadger, and then Tadrinth made some welcome tweaks.

       Original modification notes for Tadrinth:
       We figure out how many planets belong in each arm and how many planets go in the body.
       Planets are allocated via the "addPointsInCircle" because that's easily implemented (and because Keith
       had already written most of the pieces, so I could steal it readily).

       If you want two clusters per arm and a bit of a spiral then I suggest
       you allocate more planets per Arm (note the minPlanetsPerArm and maxPlanetsPerArm variables at the top),
       then allocate a second armCenter that's a bit further away from the body and at a slightly different angle

       You can connect gruops of planets via the linkPlanetLists function, so just call that first to link the two
       clusters in each arm
       */

    bool debug = false;
    public override void Generate(Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType)
    { 
      int symmetryFactor = 2;
      //numberToSeed =  BadgerUtilityMethods.getSettingValueInt("NumPlanets");
      //if(numberToSeed == 0)
      //  numberToSeed = 80;

      int userSetSymmetry = BadgerUtilityMethods.getSettingValueInt("OctopusNumArms");
      //original values: radius 70
      //                     distForThisArm 95
      //                        minDistanceBetweenPlanets 35

      int radius = 100;
      int distForThisArm = 105;
      //Tadrinth had the minDistance at 25, but that just feels too
      //cluttered to me; it's hard to read anything at max zoom
      int minDistanceBetweenPlanets = 45; 

      if (numberToSeed < 20)
      {
        radius = 70;
        distForThisArm = 80;
        symmetryFactor = 1;
      }
      else if (numberToSeed < 60)
      {
        radius = 90;
        distForThisArm = 100;

        symmetryFactor = 2;
      }
      else if (numberToSeed < 80)
      {
        radius = 100;
        distForThisArm = 120;

        symmetryFactor = Context.QualityRandom.NextWithInclusiveUpperBound(2,3);
      }
      else if(numberToSeed < 110)
      {
        radius = 130;
        distForThisArm = 145;
        symmetryFactor = Context.QualityRandom.NextWithInclusiveUpperBound(2, 4);
      }
      else
      {
        radius = 150;
        distForThisArm = 165;
        symmetryFactor = Context.QualityRandom.NextWithInclusiveUpperBound(3, 5);
      }

      if(userSetSymmetry != 0)
        symmetryFactor = userSetSymmetry;
      
      int minPlanetsPerCluster = Math.Max(numberToSeed / (symmetryFactor * 5), 2);

      // need at least symmetry three for multi-cluster method to look decent
      bool singleLargeCentralCluster = (symmetryFactor < 3) || Context.QualityRandom.NextBool();

      if(debug)
        ArcenDebugging.ArcenDebugLogSingleLine(string.Format("Generating a spiral galaxy with symmetry {0} and {1}", symmetryFactor, singleLargeCentralCluster ? "one large central cluster" : "a ring of small central clusters"), Verbosity.ShowAsInfo);


      ArcenPoint galacticCenter = Engine_AIW2.GalaxyCenter;

      int alignmentNumber = 10; //align all planets on points divisible by this value. It makes things look more organized

      List<int> centerClusterSizes = new List<int>();
      List<int> innerArmClusterSizes = new List<int>();
      List<int> outerArmClusterSizes = new List<int>();

      for (int i = 0; i < symmetryFactor; i++)
      {
        centerClusterSizes.Add(minPlanetsPerCluster);
        innerArmClusterSizes.Add(minPlanetsPerCluster);
        innerArmClusterSizes.Add(minPlanetsPerCluster);
        outerArmClusterSizes.Add(minPlanetsPerCluster);
        outerArmClusterSizes.Add(minPlanetsPerCluster);
      }

      int planetsRemaining = numberToSeed - symmetryFactor * minPlanetsPerCluster * 5;

      while (planetsRemaining > 0)
      {
        int percent = Context.QualityRandom.NextWithInclusiveUpperBound(1, 100);
        List<int> clusterSizesListToAddTo;
        if (percent > 70)
        {
          clusterSizesListToAddTo = outerArmClusterSizes;
        }
        else if (percent > 30)
        {
          clusterSizesListToAddTo = innerArmClusterSizes;
        }
        else
        {
          clusterSizesListToAddTo = singleLargeCentralCluster ? innerArmClusterSizes : centerClusterSizes;
        }

        int i = Context.QualityRandom.Next(0, clusterSizesListToAddTo.Count);
        clusterSizesListToAddTo[i] += 1;
        planetsRemaining -= 1;
      }

      if(debug)
        {
          foreach( var i in centerClusterSizes)
            ArcenDebugging.ArcenDebugLogSingleLine("center " + i, Verbosity.DoNotShow);

          foreach (var i in innerArmClusterSizes)
            ArcenDebugging.ArcenDebugLogSingleLine(" inner " + i, Verbosity.DoNotShow);

          foreach (var i in outerArmClusterSizes)
            ArcenDebugging.ArcenDebugLogSingleLine(" outer " + i, Verbosity.DoNotShow);
        }


      //allocate the points for the body
      List<ArcenPoint> allPoints = new List<ArcenPoint>();
      List<ArcenPoint> bodyCenters = new List<ArcenPoint>();

      List<List<ArcenPoint>> bodyPointLists = new List<List<ArcenPoint>>();

      List<ArcenPoint> innerArmCenters = new List<ArcenPoint>();
      List<List<ArcenPoint>> innerArmPointsList = new List<List<ArcenPoint>>();
      List<ArcenPoint> outerArmCenters = new List<ArcenPoint>();
      List<List<ArcenPoint>> outerArmPointsList = new List<List<ArcenPoint>>();
      List<List<Planet>> bodyPlanets = new List<List<Planet>>();

      //Figure out where to place the Arms; we split them evenly around the body
      //note that we update the armAngle for each iteration.

      AngleDegrees startingAngle = AngleDegrees.Create((FInt)Context.QualityRandom.NextWithInclusiveUpperBound(10, 350));
      AngleDegrees anglePerArm = AngleDegrees.Create((FInt)360 / (FInt)symmetryFactor);
      AngleDegrees subAnglePerArm = AngleDegrees.Create((FInt)360 / (FInt)symmetryFactor / (FInt)3);
      AngleDegrees fakeRotation = AngleDegrees.Create((FInt)20);

      AngleDegrees armAngle = startingAngle;

      List<Planet> bodyCluster = new List<Planet>();
      ArcenPoint center;
      LinkMethod linkingMethod = LinkMethod.Gabriel;

      if (singleLargeCentralCluster)
      {
        var totalCentralPlanets = 0;
        for(int i = 0; i < centerClusterSizes.Count; i++)
        {
          totalCentralPlanets += centerClusterSizes[i];
        }
        center = CreateClusterOfPlanets(bodyCluster, galaxy, Context, radius * 2, galacticCenter, minDistanceBetweenPlanets, alignmentNumber, totalCentralPlanets, ref allPoints, armAngle, linkingMethod, 0);
      }

      for (int i = 0; i < symmetryFactor; i++)
      {
        if(debug)
          ArcenDebugging.ArcenDebugLogSingleLine(string.Format("creating cluster {0}", i), Verbosity.DoNotShow);

        armAngle = armAngle.Add(anglePerArm);

        var firstArmAngle = armAngle.Add(fakeRotation);
        var secondArmAngle = firstArmAngle.Add(subAnglePerArm);
        if(debug)
          {
            ArcenDebugging.ArcenDebugLogSingleLine(string.Format("armAngle {0}", armAngle), Verbosity.DoNotShow);
            ArcenDebugging.ArcenDebugLogSingleLine(string.Format("first arm angle {0}", firstArmAngle), Verbosity.DoNotShow);
            ArcenDebugging.ArcenDebugLogSingleLine(string.Format("second arm angle {0}", secondArmAngle), Verbosity.DoNotShow);
          }


        //pick random method for linking for arm
        int percentGabriel = 80;
        int percentRNG = 10;
        int percentSpanningTree = 5;
        int percentSpanningTreeWithConnections = 5;

        if (!singleLargeCentralCluster)
        {
          bodyCluster = new List<Planet>();
          center = CreateClusterOfPlanets(bodyCluster, galaxy, Context, radius, galacticCenter, minDistanceBetweenPlanets, alignmentNumber, centerClusterSizes[i], ref allPoints, armAngle, linkingMethod, distForThisArm);
          bodyPlanets.Add(bodyCluster);
          bodyCenters.Add(center);
        }

        if(debug)
          ArcenDebugging.ArcenDebugLogSingleLine(string.Format("creating inner arm clusters {0}", i), Verbosity.DoNotShow);

        percentGabriel = 50;
        percentRNG = 30;
        percentSpanningTree = 10;
        percentSpanningTreeWithConnections = 10;
        linkingMethod = BadgerUtilityMethods.getRandomLinkMethod(percentSpanningTree, percentGabriel,
                                                                  percentRNG, percentSpanningTreeWithConnections,
                                                                  Context);
        var innerArm1 = new List<Planet>();
        var innerArm1Center = CreateClusterOfPlanets(innerArm1, galaxy, Context, radius, galacticCenter, minDistanceBetweenPlanets+15, alignmentNumber, innerArmClusterSizes[2 * i], ref allPoints, firstArmAngle, linkingMethod, distForThisArm * 2+20);

        if(debug)
          ArcenDebugging.ArcenDebugLogSingleLine(string.Format("creating second inner arm clusters {0}", i), Verbosity.DoNotShow);       
        var innerArm2 = new List<Planet>();
        var innerArm2Center = CreateClusterOfPlanets(innerArm2, galaxy, Context, radius, galacticCenter, minDistanceBetweenPlanets+15, alignmentNumber, innerArmClusterSizes[2 * i + 1], ref allPoints, secondArmAngle, linkingMethod, distForThisArm * 2+35);

        percentGabriel = 15;
        percentRNG = 15;
        percentSpanningTree = 60;
        percentSpanningTreeWithConnections = 10;
        linkingMethod = BadgerUtilityMethods.getRandomLinkMethod(percentSpanningTree, percentGabriel,
                                                                  percentRNG, percentSpanningTreeWithConnections,
                                                                  Context);

        if(debug)
          ArcenDebugging.ArcenDebugLogSingleLine(string.Format("creating outer arm clusters {0}", i), Verbosity.DoNotShow);

        linkingMethod = BadgerUtilityMethods.getRandomLinkMethod(percentSpanningTree, percentGabriel,
                                                                  percentRNG, percentSpanningTreeWithConnections,
                                                                  Context);
        var outerArm1 = new List<Planet>();
        var outerArm1Center = CreateClusterOfPlanets(outerArm1, galaxy, Context, radius + 30, galacticCenter, minDistanceBetweenPlanets + 40, alignmentNumber, outerArmClusterSizes[2 * i], ref allPoints, firstArmAngle.Add(fakeRotation), LinkMethod.SpanningTreeWithConnections, distForThisArm * 4);

        if(debug)
          {
            ArcenDebugging.ArcenDebugLogSingleLine(string.Format("linking outer arm clusters {0}", i), Verbosity.ShowAsInfo);

            ArcenDebugging.ArcenDebugLogSingleLine(string.Format("creating second outer arm clusters {0}", i), Verbosity.DoNotShow);
          }

        linkingMethod = BadgerUtilityMethods.getRandomLinkMethod(percentSpanningTree, percentGabriel,
                                                                  percentRNG, percentSpanningTreeWithConnections,
                                                                  Context);
        var outerArm2 = new List<Planet>();
        var outerArm2Center = CreateClusterOfPlanets(outerArm2, galaxy, Context, radius+30, galacticCenter, minDistanceBetweenPlanets+40, alignmentNumber, outerArmClusterSizes[2 * i + 1], ref allPoints, secondArmAngle.Add(fakeRotation), linkingMethod, distForThisArm * 4);

        // Link clusters together - inner to outer, body to inner
        BadgerUtilityMethods.linkPlanetLists(innerArm1, outerArm1, outerArm1Center, false);
        BadgerUtilityMethods.linkPlanetLists(bodyCluster, innerArm1, innerArm1Center, false);
        BadgerUtilityMethods.linkPlanetLists(innerArm2, outerArm2, outerArm2Center, false);
        BadgerUtilityMethods.linkPlanetLists(bodyCluster, innerArm2, innerArm2Center, false);
      }

      if (!singleLargeCentralCluster)
      {
        if(debug)
          ArcenDebugging.ArcenDebugLogSingleLine("linking central clusters together", Verbosity.DoNotShow);
        for (var i = 0; i < symmetryFactor - 1; i++)
        {
          BadgerUtilityMethods.linkPlanetLists(bodyPlanets[i], bodyPlanets[i + 1], bodyCenters[i + 1], false);

        }
        if(debug)
          ArcenDebugging.ArcenDebugLogSingleLine("linking last two central clusters together to make a ring", Verbosity.DoNotShow);
        BadgerUtilityMethods.linkPlanetLists(bodyPlanets[0], bodyPlanets[bodyPlanets.Count - 1], bodyCenters[bodyPlanets.Count - 1], false);
      }
    }

    private static ArcenPoint CreateClusterOfPlanets(List<Planet> cluster, Galaxy galaxy, ArcenSimContext Context, int radius, ArcenPoint galacticCenter, int minDistanceBetweenPlanets, int alignmentNumber, int clusterSize, ref List<ArcenPoint> allPoints, AngleDegrees armAngle, LinkMethod linkingMethod, int distForThisArm)
    {
      bool debug = false;
      if(debug)
        ArcenDebugging.ArcenDebugLogSingleLine(string.Format("CreateClusterOfPlanets - creating cluster\n size: {0}\nangle: {1}\n dist: {2}", clusterSize, armAngle, distForThisArm, linkingMethod), Verbosity.DoNotShow);

      var bodyCenter = galacticCenter.GetPointAtAngleAndDistance(armAngle, distForThisArm);
      List<ArcenPoint> pointsForArm = BadgerUtilityMethods.addPointsInCircle(clusterSize, Context, bodyCenter, radius,
                                        minDistanceBetweenPlanets, ref allPoints, alignmentNumber);
      if(debug)
        ArcenDebugging.ArcenDebugLogSingleLine(string.Format("CreateClusterOfPlanets - converting to planets", clusterSize, armAngle, distForThisArm), Verbosity.DoNotShow);

      List<Planet> planetsForThisArm = BadgerUtilityMethods.convertPointsToPlanets(pointsForArm, galaxy, Context);
      cluster.AddRange(planetsForThisArm);
      if(debug)
        ArcenDebugging.ArcenDebugLogSingleLine(string.Format("CreateClusterOfPlanets - linking\n link: {0}", linkingMethod), Verbosity.DoNotShow);

      if (linkingMethod == LinkMethod.Gabriel)
        BadgerUtilityMethods.createGabrielGraph(planetsForThisArm);
      else if (linkingMethod == LinkMethod.RNG)
        BadgerUtilityMethods.createRNGGraph(planetsForThisArm);
      else if (linkingMethod == LinkMethod.SpanningTreeWithConnections)
      {
          BadgerUtilityMethods.createMinimumSpanningTree(planetsForThisArm);
      }
      else
      {
        BadgerUtilityMethods.createMinimumSpanningTree(planetsForThisArm);
      }

      return bodyCenter;
    }
  }
}
