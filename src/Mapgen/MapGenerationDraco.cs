using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using UnityEngine;

namespace Arcen.AIW2.External {
	public static class DracoUtilities {
		public interface IHasArcenPoint {
			ArcenPoint galaxyLocation { get; set; }
		}

		public static Planet FindNearestPlanetInList(List<Planet> outerRing, Planet plnt) {
			int dist = int.MaxValue;
			Planet ret = null;
			foreach(Planet pl in outerRing) {
				int d = pl.GalaxyLocation.GetDistanceTo(plnt.GalaxyLocation, false);
				if(d < dist) {
					dist = d;
					ret = pl;
				}
			}
			return ret;
		}

		//adds an ellipse of points
		public static List<ArcenPoint> addElipticalPoints(int numPoints, ArcenSimContext Context, ArcenPoint ellipseCenter, int ellipseMajorAxis,
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
				if(angle >= 90) x = -x;
				if(angle >= 270) x = -x;
				double y = x * Math.Sin(angleRad) / Math.Cos(angleRad);
				double xn = x * Math.Cos(rotationRad) - y * Math.Sin(rotationRad);
				double yn = x * Math.Sin(rotationRad) + y * Math.Cos(rotationRad);
				pointOnRing.X += (int)xn;
				pointOnRing.Y += (int)yn;
				pointsForThisCircle.Add(pointOnRing);
				pointsSoFar.Add(pointOnRing);
			}
			return pointsForThisCircle;
		}

		//adds a circle of points
		public static List<ArcenPoint> addCircularPoints(int numPoints, ArcenSimContext Context, ArcenPoint circleCenter, int circleRadius,
														   ref List<ArcenPoint> pointsSoFar) {
			float startingAngle = Context.QualityRandom.NextFloat(1, 359);
			List<ArcenPoint> pointsForThisCircle = new List<ArcenPoint>();
			for(int i = 0; i < numPoints; i++) {
				float angle = (360f / (float)numPoints) * (float)i; // yes, this is theoretically an MP-sync problem, but a satisfactory 360 arc was simply not coming from the FInt approximations and I'm figuring the actual full-sync at the beginning of the game should sync things up before they matter
				angle += startingAngle;
				if(angle >= 360f)
					angle -= 360f;
				ArcenPoint pointOnRing = circleCenter;
				pointOnRing.X += (int)Math.Round(circleRadius * (float)Math.Cos(angle * (Math.PI / 180f)));
				pointOnRing.Y += (int)Math.Round(circleRadius * (float)Math.Sin(angle * (Math.PI / 180f)));
				pointsForThisCircle.Add(pointOnRing);
				pointsSoFar.Add(pointOnRing);
			}
			return pointsForThisCircle;
		}

		/* This returns a matrix where matrix[i][j] == 1 means point i and point j should be connected 
		   Has the same algorithm as createMinimumSpanningTree, but a seperate implementation */
		public static int[,] createMinimumSpanningTreeLinks(ReadOnlyCollection<IHasArcenPoint> pointsForGraph) {
			int[,] connectionArray;
			connectionArray = new int[pointsForGraph.Count, pointsForGraph.Count];
			if(pointsForGraph.Count < 1) return connectionArray;
			for(int i = 0; i < pointsForGraph.Count; i++) {
				for(int j = 0; j < pointsForGraph.Count; j++) {
					connectionArray[i, j] = 0;
				}
			}
			List<int> verticesNotInTree = new List<int>();
			List<int> verticesInTree = new List<int>();
			// ArcenDebugging.ArcenDebugLogSingleLine("Creating minimum spanning tree now", Verbosity.DoNotShow);
			for(int i = 0; i < pointsForGraph.Count; i++)
				verticesNotInTree.Add(i);
			//Pick first element, then remove it from the list
			int pointIdx = verticesNotInTree[0];
			verticesNotInTree.RemoveAt(0);
			verticesInTree.Add(pointIdx);

			//initialize adjacency matrix for Prim's algorithm
			//the adjacency matrix contains entries as follows
			//pointIdxNotInTree <closest point in tree> <distance to closest point>
			//In the body of the algorithm we look at this matrix to figure out
			//which point to add to the tree next, then update it for the next iteration
			int[,] spanningAdjacencyMatrix;
			spanningAdjacencyMatrix = new int[pointsForGraph.Count, 3];
			for(int i = 0; i < pointsForGraph.Count; i++) {
				spanningAdjacencyMatrix[i, 0] = i;
				spanningAdjacencyMatrix[i, 1] = -1;
				spanningAdjacencyMatrix[i, 1] = 9999;
			}
			//loop until all vertices are in the tree
			while(verticesNotInTree.Count > 0) {
				//update the adjacency matrix
				//for each element NOT in the tree, find the closest
				//element in the tree
				for(int i = 0; i < verticesNotInTree.Count; i++) {
					int minDistance = 9999;
					for(int j = 0; j < verticesInTree.Count; j++) {
						int idxNotInTree = verticesNotInTree[i];
						int idxInTree = verticesInTree[j];
						ArcenPoint pointNotInTree = ((IHasArcenPoint)pointsForGraph[idxNotInTree]).galaxyLocation;
						ArcenPoint pointInTree = ((IHasArcenPoint)pointsForGraph[idxInTree]).galaxyLocation;
						int distance = Mat.DistanceBetweenPoints(pointNotInTree, pointInTree);
						if(distance < minDistance) {
							spanningAdjacencyMatrix[idxNotInTree, 1] = idxInTree;
							spanningAdjacencyMatrix[idxNotInTree, 2] = distance;
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
				for(int i = 0; i < verticesNotInTree.Count; i++) {
					pointIdx = verticesNotInTree[i];
					// s = System.String.Format( "To find closest edge, examine {0} of {1} (idx {4}), minDistance {2} dist for this point {3}",
					//                           i, verticesNotInTree.Count , minDistanceFound, spanningAdjacencyMatrix[pointIdx, 2], pointIdx);
					// ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
					if(spanningAdjacencyMatrix[pointIdx, 2] == 0) {
						//don't try to link a point to itself
						continue;
					}
					if(spanningAdjacencyMatrix[pointIdx, 2] < minDistanceFound) {
						minDistanceFound = spanningAdjacencyMatrix[pointIdx, 2];
						closestPointIdx = spanningAdjacencyMatrix[pointIdx, 1];
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
				connectionArray[pointToAdd, closestPointIdx] = 1;
				connectionArray[closestPointIdx, pointToAdd] = 1;
			}
			return connectionArray;
		}

		public static bool DoesPointOverlapPlanet(ArcenPoint pt, int v, ReadOnlyCollection<IHasArcenPoint> currentlist) {
			foreach(IHasArcenPoint ap in currentlist) {
				if(pt.GetDistanceTo(ap.galaxyLocation, false) <= v) {
					return true;
				}
			}
			return false;
		}

		public static byte[] ReadBitmapFile(string path, out int width, out int height) {
			FileStream f = File.OpenRead(path);
			byte[] info = new byte[54];
			f.Read(info, 0, 54);
			width = info[19] * 256 + info[18];
			height = info[23] * 256 + info[22];
			int w = (int)(Math.Ceiling(width * 3 / 4f) * 4);
			int size = w * height;
			ArcenDebugging.ArcenDebugLogSingleLine("size of image: " + width + "*" + height, Verbosity.DoNotShow);
			byte[] input = new byte[size];
			byte[] colors = new byte[size];
			f.Read(input, 0, size);
			f.Close();
			for(int c = 0; c < size; c++) {
				colors[c] = input[c];
			}
			return colors;
		}

		public static int GetProbabilityAt(byte[] colors, int x, int y, int w, int h) {
			w = (int)(Math.Ceiling(w * 3 / 4f) * 4);
			return Math.Max(Math.Max(colors[(y * w + x * 3)], colors[(y * w + x * 3) + 1]), colors[(y * w + x * 3) + 2]);
		}

		public static Color GetColorAt(byte[] colors, int x, int y, int w, int h) {
			w = (int)(Math.Ceiling(w * 3 / 4f) * 4);
			return new Color(colors[(y * w + x * 3) + 2], colors[(y * w + x * 3) + 1], colors[(y * w + x * 3)]);
		}
	}
	public class Mapgen_D18_Mesh : Mapgen_Base {

		//Galaxy and Context are always passed in. numberToSeed is the number of planets to create
		public override void Generate(Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType) {
			//numberToSeed = 30;  //lets override the number of planets desired to something small and manageable
			ArcenDebugging.ArcenDebugLogSingleLine("Welcome to the D18 Mesh generator\n", Verbosity.DoNotShow);
			//AngleDegrees thirty = AngleDegrees.Create(FInt.FromParts(30, 0));
			//AngleDegrees threethirty = AngleDegrees.Create(FInt.FromParts(330, 0));

			PlanetType planetType = PlanetType.Normal;
			List<Planet> allPlanets = new List<Planet>();
			List<Planet> openPlanets = new List<Planet>();
			ArcenPoint originPlanetPoint = Engine_AIW2.GalaxyCenter;
			Planet originPlanet = galaxy.AddPlanet(planetType, originPlanetPoint, Context);
			openPlanets.Add(originPlanet);
			ArcenDebugging.ArcenDebugLogSingleLine("populate PlanetPoints list\n", Verbosity.DoNotShow);
			for(int i = 0; i < numberToSeed - 1 && openPlanets.Count > 0; i++) {
				Planet extendFrom = openPlanets[Context.QualityRandom.Next(0, openPlanets.Count)];
				Planet newplanet = null;
				ArcenPoint newpos;
				bool recheck = false;
				int maxCheck = 30;
				do {
					recheck = false;
					newpos = extendFrom.GalaxyLocation.GetRandomPointWithinDistance(Context.QualityRandom, 30, 150);
					extendFrom.DoForLinkedNeighbors(delegate (Planet item) {
						FInt deg = extendFrom.GalaxyLocation.GetAngleToDegrees(item.GalaxyLocation).GetAbsoluteDeltaNeededToGetToOther(extendFrom.GalaxyLocation.GetAngleToDegrees(newpos));
						recheck = recheck || (deg < 30);
						return DelReturn.Continue;
					});
					int minDistSeen = int.MaxValue;
					recheck |= wouldCollideWithLinks(allPlanets, extendFrom, newpos); //GetWouldLinkCrossOverOtherPlanets(pl, newpos);
					if(!recheck) {
						foreach(Planet p in allPlanets) {
							int dist = p.GalaxyLocation.GetDistanceTo(newpos, false);
							recheck |= dist < 30;
							if(dist < minDistSeen) {
								minDistSeen = dist;
							}
						}
					}
					maxCheck--;
				} while(recheck && maxCheck > 0);
				if(recheck && maxCheck <= 0) {
					openPlanets.Remove(extendFrom);
					i--;
					continue;
				}
				planetType = Context.QualityRandom.NextFloat(0, 1) > 0.2 ? PlanetType.Normal : PlanetType.Star;
				newplanet = galaxy.AddPlanet(planetType, newpos, Context);
				newplanet.AddLinkTo(extendFrom);
				allPlanets.Add(newplanet);
				openPlanets.Add(newplanet);

				foreach(Planet plnt in openPlanets) {
					if(newplanet.GetLinkedNeighborCount() <= 4) {
						int dist = plnt.GalaxyLocation.GetDistanceTo(newplanet.GalaxyLocation, false);
						if(dist >= 30 && dist <= 150) {
							bool canConnect = true;
							FInt deg = newplanet.GalaxyLocation.GetAngleToDegrees(extendFrom.GalaxyLocation).GetAbsoluteDeltaNeededToGetToOther(newplanet.GalaxyLocation.GetAngleToDegrees(plnt.GalaxyLocation));
							canConnect = canConnect && deg >= 30;

							plnt.DoForLinkedNeighbors(delegate (Planet item) {
								deg = plnt.GalaxyLocation.GetAngleToDegrees(item.GalaxyLocation).GetAbsoluteDeltaNeededToGetToOther(plnt.GalaxyLocation.GetAngleToDegrees(newplanet.GalaxyLocation));
								canConnect = canConnect && deg >= 30;

								return DelReturn.Continue;
							});
							bool wouldCollide = wouldCollideWithLinks(allPlanets, plnt, newplanet);
							if(canConnect && !wouldCollide) {
								newplanet.AddLinkTo(plnt);
							}
						}
					}
				}
				if(extendFrom.GetLinkedNeighborCount() > 4) {
					openPlanets.Remove(extendFrom);
				}
				if(newplanet.GetLinkedNeighborCount() > 4) {
					openPlanets.Remove(newplanet);
				}
				openPlanets.RemoveAll(x => x.GetLinkedNeighborCount() > 4);
			}
			galaxy.AddPlanet(planetType, ArcenPoint.Create(1000, 1000), Context);
			return;
		}

		private bool wouldCollideWithLinks(List<Planet> toCheck, Planet one, Planet two) {
			bool ret = false;
			foreach(Planet plnt in toCheck) {
				plnt.DoForLinkedNeighbors(delegate (Planet item) {
					if(plnt == one || plnt == two || item == one || item == two) return DelReturn.Continue;
					ret |= Mat.LineSegmentIntersectsLineSegment(one.GalaxyLocation, two.GalaxyLocation, plnt.GalaxyLocation, item.GalaxyLocation, 10);
					return ret ? DelReturn.Break : DelReturn.Continue;
				});
				if(ret) return true;
			}
			return false;
		}

		private bool wouldCollideWithLinks(List<Planet> toCheck, Planet one, ArcenPoint two) {
			bool ret = false;
			foreach(Planet plnt in toCheck) {
				plnt.DoForLinkedNeighbors(delegate (Planet item) {
					if(plnt == one || item == one) return DelReturn.Continue;
					ret |= Mat.LineSegmentIntersectsLineSegment(one.GalaxyLocation, two, plnt.GalaxyLocation, item.GalaxyLocation, 10);
					return ret ? DelReturn.Break : DelReturn.Continue;
				});
				if(ret) return true;
			}
			return false;
		}
	}

	public class Mapgen_D18_LinkedRings : Mapgen_Base {
		//Galaxy and Context are always passed in. numberToSeed is the number of planets to create
		public override void Generate(Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType) {
			ArcenDebugging.ArcenDebugLogSingleLine("Welcome to the D18 Linked Rings generator\n", Verbosity.DoNotShow);
			//numberToSeed = 12;
			int numMoons = (numberToSeed / 10);
			if(numberToSeed > 24)
				numberToSeed -= numMoons * 2;
			else numMoons = 0;
			int outerRingCount = 12;
			if(numberToSeed > 80) {
				outerRingCount = (int)((numberToSeed / 80f) * 12f);
			}
			ArcenDebugging.ArcenDebugLogSingleLine("Base ring size " + outerRingCount, Verbosity.DoNotShow);
			//first guess on how many sub-rings we need
			int numCircles = (numberToSeed - outerRingCount) / 8;  //how many rings we can make of 8 stars
			int numPerStar = (numberToSeed - outerRingCount) / 6; //how many stars we'd need per ring
			int pointsPerLoop = 8;
			int numLoops = 6;
			if(numCircles <= 6) {
				numLoops = numCircles;
			}
			else {
				pointsPerLoop = numPerStar;
			}
			int numMainLoop = numberToSeed - (numLoops * pointsPerLoop);
			ArcenDebugging.ArcenDebugLogSingleLine("Galaxy needs (pass 1) " + numLoops + " loops and " + pointsPerLoop + " planets per loop.", Verbosity.DoNotShow);
			//re-evaluate
			numCircles = (numberToSeed - numMainLoop) / 8; //6
			numPerStar = (numberToSeed - numMainLoop) / (numMainLoop / 2); //8
			if(numCircles <= numMainLoop / 2) {
				numLoops = numCircles;
			}
			else {
				pointsPerLoop = numPerStar;
				numLoops = numMainLoop / 2;
			}
			numMainLoop = numberToSeed - (numLoops * pointsPerLoop);

			ArcenDebugging.ArcenDebugLogSingleLine("Galaxy needs (pass 2) " + numLoops + " loops and " + pointsPerLoop + " planets per loop.", Verbosity.DoNotShow);

			int sizeOfBigLoop = 350;
			int sizeOfSmallLoops = (int)Math.Ceiling(sizeOfBigLoop * Math.PI * 2 / numMainLoop * .8);
			int moonLoopSize = (int)Math.Ceiling(sizeOfSmallLoops * Math.PI * 2 / pointsPerLoop * .4);
			if(moonLoopSize < 20) {
				numMainLoop += numMoons * 2;
				numMoons = 0;
				sizeOfSmallLoops = (int)Math.Ceiling(sizeOfBigLoop * Math.PI * 2 / numMainLoop) - 25;
			}
			ArcenDebugging.ArcenDebugLogSingleLine("Small loops are " + sizeOfSmallLoops + " units around", Verbosity.DoNotShow);
			//galaxy.AddPlanet(PlanetType.Star, Engine_AIW2.GalaxyCenter, Context);
			List<ArcenPoint> planetPoints = new List<ArcenPoint>();
			//DracoUtilities.addElipticalPoints(30, Context, Engine_AIW2.GalaxyCenter, sizeOfBigLoop, (int)(sizeOfBigLoop*.7), 30f / 180f * Math.PI, ref planetPoints);
			BadgerUtilityMethods.addCircularPoints(Math.Max(numMainLoop, 12), Context, Engine_AIW2.GalaxyCenter, sizeOfBigLoop, ref planetPoints);
			Planet origin = null;
			Planet prev = null;
			List<Planet> outerRing = new List<Planet>();
			foreach(ArcenPoint pt in planetPoints) {
				Planet p = galaxy.AddPlanet(PlanetType.Normal, pt, Context);
				if(origin == null) origin = p;
				if(prev != null) prev.AddLinkTo(p);
				prev = p;
				outerRing.Add(p);
			}
			origin.AddLinkTo(prev);
			//outerRing[0].AddLinkTo(outerRing[outerRing.Count - 1]);
			if(numMainLoop <= 12) {
				ArcenDebugging.ArcenDebugLogSingleLine("Main loop has minimum number of planets. Returning\n", Verbosity.DoNotShow);
				return;
			}

			List<ArcenPoint> baseCircle = new List<ArcenPoint>();
			for(float q = 0; Math.Ceiling(q) < planetPoints.Count; q += (float)numMainLoop / numLoops) {
				List<Planet> eachRing = new List<Planet>();
				ArcenDebugging.ArcenDebugLogSingleLine("  Adding point #" + (int)q + "(" + q + ")", Verbosity.DoNotShow);
				ArcenPoint pt = outerRing[(int)q].GalaxyLocation;
				ArcenDebugging.ArcenDebugLogSingleLine("  Exapnding around " + pt, Verbosity.DoNotShow);
				prev = origin = null;
				List<ArcenPoint> newPoints = new List<ArcenPoint>();
				BadgerUtilityMethods.addCircularPoints(pointsPerLoop, Context, pt, sizeOfSmallLoops, ref newPoints);
				foreach(ArcenPoint npt in newPoints) {
					Planet p = galaxy.AddPlanet(PlanetType.Normal, npt, Context);
					if(origin == null) {
						origin = p;
						//p.AddLinkTo(outerRing[(int)q]);
					}
					if(prev != null) prev.AddLinkTo(p);
					prev = p;
					eachRing.Add(p);
				}
				origin.AddLinkTo(prev);
				foreach(Planet rng in eachRing) {
					Planet outer = DracoUtilities.FindNearestPlanetInList(outerRing, rng);
					if(outer.GalaxyLocation.GetDistanceTo(rng.GalaxyLocation, false) < sizeOfSmallLoops * 0.6f) {
						rng.AddLinkTo(outer);
					}
				}
			}

			ArcenDebugging.ArcenDebugLogSingleLine("Seeding " + numMoons + " moons", Verbosity.DoNotShow);
			ArcenDebugging.ArcenDebugLogSingleLine("  Moon loop size: " + moonLoopSize, Verbosity.DoNotShow);
			List<Planet> allMoons = new List<Planet>();
			List<Planet> inSubRing = new List<Planet>();
			inSubRing.AddRange(galaxy.Planets);
			inSubRing.RemoveAll(x => outerRing.Contains(x));
			List<Planet> tooClose = new List<Planet>();
			foreach(Planet p in inSubRing) {
				p.DoForPlanetsWithinXHops(Context, 2, delegate (Planet item, int Distance) {
					Planet inOutRing = DracoUtilities.FindNearestPlanetInList(outerRing, item);
					if(outerRing.Contains(item) || inOutRing.GalaxyLocation.GetDistanceTo(item.GalaxyLocation, false) < moonLoopSize * 1.5) {
						tooClose.Add(item);
					}
					return DelReturn.Continue;
				});
				/*p.DoForLinkedNeighbors(delegate (Planet item) {
					if(outerRing.Contains(item)) {
						tooClose.Add(item);
					}
					return DelReturn.Continue;
				});*/
			}
			inSubRing.RemoveAll(x => tooClose.Contains(x));
			for(int m = 0; m < numMoons; m += 2) {
				ArcenDebugging.ArcenDebugLogSingleLine("    Moon " + m, Verbosity.DoNotShow);
				prev = origin = null;
				Planet parent;
				int maxCheck = 20;
				do {
					int r = Context.QualityRandom.Next(0, inSubRing.Count);
					parent = inSubRing[r];
					maxCheck--;
				} while((allMoons.Contains(parent) || outerRing.Contains(parent)) && maxCheck >= 0);
				if(maxCheck < 0) {
					continue;
				}
				allMoons.Add(parent);
				parent.DoForLinkedNeighbors(delegate (Planet item) {
					allMoons.Add(item);
					return DelReturn.Continue;
				});
				List<ArcenPoint> newPoints = new List<ArcenPoint>();
				List<Planet> eachRing = new List<Planet>();
				BadgerUtilityMethods.addCircularPoints(6, Context, parent.GalaxyLocation, moonLoopSize, ref newPoints);
				foreach(ArcenPoint npt in newPoints) {
					Planet p = galaxy.AddPlanet(PlanetType.Normal, npt, Context);
					if(origin == null) {
						origin = p;
						//p.AddLinkTo(outerRing[(int)q]);
					}
					if(prev != null) prev.AddLinkTo(p);
					prev = p;
					allMoons.Add(p);
					eachRing.Add(p);
				}
				origin.AddLinkTo(prev);
				inSubRing.Remove(parent);
				foreach(Planet rng in eachRing) {
					Planet outer = DracoUtilities.FindNearestPlanetInList(inSubRing, rng);
					if(outer.GalaxyLocation.GetDistanceTo(rng.GalaxyLocation, false) < moonLoopSize * 1.9f) {
						rng.AddLinkTo(outer);
					}
				}
				inSubRing.Add(parent);
			}

			ArcenDebugging.ArcenDebugLogSingleLine("Galaxy complete! Galaxy has " + galaxy.Planets.Count + " planets.", Verbosity.DoNotShow);
			//ArcenDebugging.ArcenDebugLogSingleLine("Galaxy complete!\n", Verbosity.DoNotShow);

			foreach(Planet plnt in galaxy.Planets) {
				if(!outerRing.Contains(plnt)) {
					Planet conflictEndpoint1, conflictEndpoint2;
					if(DoesPlanetLieOnRing(outerRing, plnt, out conflictEndpoint1, out conflictEndpoint2)) {
						conflictEndpoint1.RemoveLinkTo(conflictEndpoint2);
						plnt.AddLinkTo(conflictEndpoint1);
						plnt.AddLinkTo(conflictEndpoint2);
					}
					if(!inSubRing.Contains(plnt) && DoesPlanetLieOnRing(inSubRing, plnt, out conflictEndpoint1, out conflictEndpoint2)) {
						conflictEndpoint1.RemoveLinkTo(conflictEndpoint2);
						plnt.AddLinkTo(conflictEndpoint1);
						plnt.AddLinkTo(conflictEndpoint2);
					}
				}
			}
		}

		private bool DoesPlanetLieOnRing(List<Planet> outerRing, Planet plnt, out Planet conflictEndpoint1, out Planet conflictEndpoint2) {
			bool ret = false;
			Planet o1 = null;
			Planet o2 = null;
			foreach(Planet rng in outerRing) {
				rng.DoForLinkedNeighbors(delegate (Planet item) {
					if(Mat.LineIntersectsRectangleContainingCircle(rng.GalaxyLocation, item.GalaxyLocation, plnt.GalaxyLocation, 10)) {
						o1 = rng;
						o2 = item;
						ret = true;
						return DelReturn.Break;
					}
					return DelReturn.Continue;
				});
			}
			conflictEndpoint1 = o1;
			conflictEndpoint2 = o2;
			return ret;
		}
	}

	public class Mapgen_D18_Ellipses : Mapgen_Base {
		//Galaxy and Context are always passed in. numberToSeed is the number of planets to create
		public override void Generate(Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType) {
			ArcenDebugging.ArcenDebugLogSingleLine("Welcome to the D18 Swirl generator\n", Verbosity.DoNotShow);
			float angleOffset = Context.QualityRandom.NextFloat((float)Math.PI);
			//numberToSeed = 12;
			int numPointsPerRing = numberToSeed / 8;
			int numRings = (numberToSeed / numPointsPerRing) + 1;
			int sizeOfBigLoop = 20;
			List<ArcenPoint> planetPoints = new List<ArcenPoint>();
			Planet origin = null;
			Planet prev = null;
			int angleMod = 0;
			for(int j = 0; j < numRings; j++) {
				angleMod += 2;
				float angle = j * 12 + angleMod;
				sizeOfBigLoop += 38 + j * 3;
				float mod = (sizeOfBigLoop + j * 5) / 175f;
				DracoUtilities.addElipticalPoints(Math.Max((int)(mod * numPointsPerRing), 7), Context, Engine_AIW2.GalaxyCenter, sizeOfBigLoop, (int)(sizeOfBigLoop * .7), angle / 180f * Math.PI + angleOffset, ref planetPoints);
				foreach(ArcenPoint p in planetPoints) {
					Planet q = galaxy.AddPlanet(PlanetType.Normal, p, Context);
					if(origin == null) origin = q;
					if(prev != null) prev.AddLinkTo(q);
					prev = q;
				}
				origin.AddLinkTo(prev);
				origin = prev = null;
				planetPoints = new List<ArcenPoint>();
			}
			List<Planet> allPlanets = new List<Planet>();
			allPlanets.AddRange(galaxy.Planets);
			foreach(Planet p in allPlanets) {
				List<Planet> nearby = allPlanets.FindAll(x => x.GalaxyLocation.GetDistanceTo(p.GalaxyLocation, false) < 48);
				foreach(Planet q in nearby) {
					bool linkValid = true;
					foreach(Planet plChk in allPlanets) {
						if(plChk != p && plChk != q) {
							plChk.DoForLinkedNeighbors(delegate (Planet item) {
								if(item != p && item != q) {
									linkValid &= !Mat.LineSegmentIntersectsLineSegment(p.GalaxyLocation, q.GalaxyLocation, plChk.GalaxyLocation, item.GalaxyLocation, 5);
								}
								return linkValid ? DelReturn.Continue : DelReturn.Break;
							});
						}
						if(!linkValid) break;
					}
					if(linkValid && p != q)
						p.AddLinkTo(q);
				}
			}
			/*List<Planet> toKill = new List<Planet>();
			foreach(Planet p in allPlanets) {
				//ArcenDebugging.ArcenDebugLogSingleLine("Planet " + p.GalaxyLocation + " has " + p.GetLinkedNeighborCount() + " outbound connections", Verbosity.DoNotShow);
				if(p.GetLinkedNeighborCount() == 2) {
					bool doesNeedRemove = true;
					p.DoForLinkedNeighbors(delegate (Planet item) {
						if(item.GetLinkedNeighborCount() <= 2) {
							p.DoForLinkedNeighbors(delegate (Planet item2) {
								if(item2 != p) {
									if(item2.GetLinkedNeighborCount() > 2) {
										doesNeedRemove = false;
									}
								}
								return DelReturn.Continue;
							});
						}
						else {
							doesNeedRemove = false;
						}
						return DelReturn.Continue;
					});
					if(doesNeedRemove) {
						toKill.Add(p);
						ArcenDebugging.ArcenDebugLogSingleLine("Planet " + p.GalaxyLocation + " needs to be removed", Verbosity.DoNotShow);
						//p.DoForLinkedNeighbors(delegate (Planet item) {
						//	p.RemoveLinkTo(item);
						//	return DelReturn.Continue;
						//});
					}
				}
			}
			ArcenDebugging.ArcenDebugLogSingleLine("Removing all " + toKill.Count + " unlinked planets...", Verbosity.DoNotShow);
			for(int k = 0; k < toKill.Count; k++) {
				Planet rem = toKill[k];
				ArcenDebugging.ArcenDebugLogSingleLine("  removing #" + k, Verbosity.DoNotShow);
				rem.DoForLinkedNeighbors(delegate (Planet item) {
					rem.RemoveLinkTo(item);
					return DelReturn.Continue;
				});
				//galaxy.Planets.Remove(rem);
				ArcenDebugging.ArcenDebugLogSingleLine("  #" + k  +" removed", Verbosity.DoNotShow);
			}*/
		}
	}

	public class Mapgen_D18_Bubbles : Mapgen_Base {
		private class BubbleArrangement : DracoUtilities.IHasArcenPoint {
			public int radius;
			public ArcenPoint center;
			public int numPlanets;
			public List<Planet> region;

			public ArcenPoint galaxyLocation {
				get {
					return center;
				}

				set {
					center = value;
				}
			}

			public BubbleArrangement(ArcenPoint _center, int _radius) {
				radius = _radius;
				center = _center;
				numPlanets = Math.Max((int)(radius / 9f), 5);
			}
		}
		public override void Generate(Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType) {
			ArcenDebugging.ArcenDebugLogSingleLine("Welcome to the D18 Bubbles generator\n", Verbosity.DoNotShow);

			int numBlobs = numberToSeed / 10;
			ArcenDebugging.ArcenDebugLogSingleLine("Creating " + numBlobs + " blobs", Verbosity.DoNotShow);
			List<BubbleArrangement> regionCenters = new List<BubbleArrangement>();
			int seededPlanets = 0;
			for(int s = 0; s < numBlobs; s++) {
				ArcenPoint pt = ArcenPoint.Create(Context.QualityRandom.Next(-300, 300), Context.QualityRandom.Next(-300, 300));
				BubbleArrangement bub = new BubbleArrangement(pt, Context.QualityRandom.Next(15, 75) + Context.QualityRandom.Next(15, 75));
				regionCenters.Add(bub);
				seededPlanets += bub.numPlanets;
			}
			if(seededPlanets < numberToSeed) {
				ArcenPoint pt = ArcenPoint.Create(Context.QualityRandom.Next(-500, 500), Context.QualityRandom.Next(-500, 500));
				BubbleArrangement bub = new BubbleArrangement(pt, 0);
				bub.numPlanets = Math.Max(numberToSeed - seededPlanets, 5);
				bub.radius = bub.numPlanets * 9;
				regionCenters.Add(bub);
			}
			bool needsRecheck = false;
			int maxDepth = 150;
			ArcenDebugging.ArcenDebugLogSingleLine("Spreading blobs out... ", Verbosity.DoNotShow);
			do {
				needsRecheck = false;
				//ArcenDebugging.ArcenDebugLogSingleLine("  Spreading groups out... " + maxDepth, Verbosity.DoNotShow);
				for(int i = 0; i < regionCenters.Count; i++) {
					for(int j = i + 1; j < regionCenters.Count; j++) {
						ArcenPoint pt1 = regionCenters[i].center;
						ArcenPoint pt2 = regionCenters[j].center;
						if(pt1 != pt2 && pt1.GetDistanceTo(pt2, false) <= regionCenters[i].radius + regionCenters[j].radius + 75) {
							if(pt1.GetDistanceTo(pt2, false) <= regionCenters[i].radius + regionCenters[j].radius + 50) {
								needsRecheck = true;
							}
							ArcenPoint rel = ArcenPoint.Create(pt1.X - pt2.X, pt1.Y - pt2.Y);
							double d = 1;// Math.Sqrt(rel.X * rel.X + rel.Y * rel.Y);
										 //ArcenDebugging.ArcenDebugLogSingleLine("    Shifting " + pt1 + " & " + pt2 + " by " + rel, Verbosity.DoNotShow);
							pt1.X += (int)(10 * Math.Sign(Math.Floor(rel.X / d)));
							pt2.X += -(int)(10 * Math.Sign(Math.Floor(rel.X / d)));
							pt1.Y += (int)(10 * Math.Sign(Math.Floor(rel.Y / d)));
							pt2.Y += -(int)(10 * Math.Sign(Math.Floor(rel.Y / d)));
							regionCenters[i].center = pt1;
							regionCenters[j].center = pt2;
						}
					}
				}
				maxDepth--;
			} while(needsRecheck && maxDepth > 0);
			//ArcenDebugging.ArcenDebugLogSingleLine("Spreading Complete", Verbosity.DoNotShow);
			Dictionary<ArcenPoint, List<Planet>> allPlanetsMap = new Dictionary<ArcenPoint, List<Planet>>();
			Planet origin, prev;
			foreach(BubbleArrangement pt in regionCenters) {
				origin = prev = null;
				List<ArcenPoint> cirlce = new List<ArcenPoint>();
				int rad = pt.radius;
				int planetsNeeded = pt.numPlanets;
				ArcenDebugging.ArcenDebugLogSingleLine("  Blob " + pt + " has radius " + rad + " and " + planetsNeeded + " planets", Verbosity.DoNotShow);
				DracoUtilities.addCircularPoints(planetsNeeded, Context, pt.center, rad, ref cirlce);
				pt.region = new List<Planet>();
				foreach(ArcenPoint pl in cirlce) {
					Planet plt = galaxy.AddPlanet(PlanetType.Normal, pl, Context);
					pt.region.Add(plt);
					if(origin == null) origin = plt;
					if(prev != null) prev.AddLinkTo(plt);
					prev = plt;
				}
				origin.AddLinkTo(prev);
				//allPlanetsMap.Add(pt, circPl);
			}
			//ArcenDebugging.ArcenDebugLogSingleLine("  Circles added", Verbosity.DoNotShow);

			int[,] connectionMatrix = DracoUtilities.createMinimumSpanningTreeLinks(new ReadOnlyCollection<DracoUtilities.IHasArcenPoint>(regionCenters.Cast<DracoUtilities.IHasArcenPoint>().ToList()));

			for(int i = 0; i < regionCenters.Count; i++) {
				for(int j = i + 1; j < regionCenters.Count; j++) {
					//List<Planet> region1, region2;
					//allPlanetsMap.TryGetValue(regionCenters[i], out region1);
					//allPlanetsMap.TryGetValue(regionCenters[j], out region2);
					Planet a = DracoUtilities.FindNearestPlanetInList(regionCenters[i].region, regionCenters[j].region[0]);
					Planet b = DracoUtilities.FindNearestPlanetInList(regionCenters[j].region, a);
					a = DracoUtilities.FindNearestPlanetInList(regionCenters[i].region, b);
					if(j >= regionCenters.Count)
						ArcenDebugging.ArcenDebugLogSingleLine("BUG! FIXME", Verbosity.DoNotShow);
					if(connectionMatrix[i, j] == 1) {
						if(regionCenters[i] == null | regionCenters[j] == null) {
							ArcenDebugging.ArcenDebugLogSingleLine("BUG! FIXME", Verbosity.DoNotShow);
						}
						else {
							a.AddLinkTo(b);
						}
					}
					else if(regionCenters[i].center.GetDistanceTo(regionCenters[j].center, false) < 150 + regionCenters[i].radius + regionCenters[j].radius) {
						//a.AddLinkTo(b);
						bool isvalid = true;
						foreach(Planet plChk in galaxy.Planets) {
							if(plChk != a && plChk != b) {
								plChk.DoForLinkedNeighbors(delegate (Planet item) {
									if(item.PlanetIndex > plChk.PlanetIndex && item != a && item != b) {
										isvalid &= !Mat.LineSegmentIntersectsLineSegment(a.GalaxyLocation, b.GalaxyLocation, plChk.GalaxyLocation, item.GalaxyLocation, 5);
									}
									return isvalid ? DelReturn.Continue : DelReturn.Break;
								});
							}
						}
						if(isvalid)
							a.AddLinkTo(b);
					}
				}
			}
			ArcenDebugging.ArcenDebugLogSingleLine("Galaxy complete! Galaxy has " + galaxy.Planets.Count + " planets.", Verbosity.DoNotShow);
		}
	}
	public class Mapgen_D18_DensityFile : Mapgen_Base {
		private class MSTArrangement : DracoUtilities.IHasArcenPoint {
			public Planet planet;
			public int bitmapVal;

			public ArcenPoint galaxyLocation {
				get {
					return planet.GalaxyLocation;
				}

				set {
					//Planet. = value;
				}
			}

			public MSTArrangement(Planet p, int v) {
				planet = p;
				bitmapVal = v;
			}
		}
		public override void Generate(Galaxy galaxy, ArcenSimContext Context, int numberToSeed, MapTypeData mapType) {
			numberToSeed = 120;
			int mapExtents = 300;
			if(numberToSeed > 80) {
				mapExtents = (int)Math.Round((Math.Sqrt(numberToSeed-80)/15 +1) * mapExtents);
			}
			string path = "./GameData/Configuration/MapType/map_c.bmp";
			ArcenDebugging.ArcenDebugLogSingleLine("Welcome to the D18 Density Map generator\n", Verbosity.DoNotShow);
			
			int width;
			int height;
			byte[] colors = DracoUtilities.ReadBitmapFile(path, out width, out height);
			List<MSTArrangement> allPoints = new List<MSTArrangement>();
			float xDiv = mapExtents * 2f / width;
			float yDiv = mapExtents * 2f / height;
			for(int n = 0; n < numberToSeed;) {
				ArcenPoint pt = ArcenPoint.Create(Context.QualityRandom.Next(-mapExtents, mapExtents), Context.QualityRandom.Next(-mapExtents, mapExtents));
				if(DracoUtilities.DoesPointOverlapPlanet(pt, 20, new ReadOnlyCollection<DracoUtilities.IHasArcenPoint>(allPoints.Cast<DracoUtilities.IHasArcenPoint>().ToList()))) {
					continue;
				}
				int y = DracoUtilities.GetProbabilityAt(colors, (int)Math.Floor((pt.X + mapExtents) / xDiv), (int)Math.Floor((pt.Y + mapExtents) / yDiv), width, height);
				int r = Context.QualityRandom.Next(256);
				//ArcenDebugging.ArcenDebugLogSingleLine("    " + pt + ": " + r + " > " + y, Verbosity.DoNotShow);
				if(r < y) {
					allPoints.Add(new MSTArrangement(galaxy.AddPlanet(PlanetType.Normal, pt, Context), y));
					n++;
				}
			}
			int[,] connectionMatrix = DracoUtilities.createMinimumSpanningTreeLinks(new ReadOnlyCollection<DracoUtilities.IHasArcenPoint>(allPoints.Cast<DracoUtilities.IHasArcenPoint>().ToList()));
			for(int i = 0; i < allPoints.Count; i++) {
				for(int j = i + 1; j < allPoints.Count; j++) {
					Planet a = allPoints[i].planet;
					Planet b = allPoints[j].planet;
					if(j >= allPoints.Count)
						ArcenDebugging.ArcenDebugLogSingleLine("BUG! FIXME", Verbosity.DoNotShow);
					if(connectionMatrix[i, j] == 1) {
						if(allPoints[i] == null | allPoints[j] == null) {
							ArcenDebugging.ArcenDebugLogSingleLine("BUG! FIXME", Verbosity.DoNotShow);
						}
						else {
							a.AddLinkTo(b);
						}
					}
					else if(allPoints[i].galaxyLocation.GetDistanceTo(allPoints[j].galaxyLocation, false) < (330 - Math.Max(allPoints[i].bitmapVal, allPoints[j].bitmapVal)) / 3 || (allPoints[i].galaxyLocation.GetDistanceTo(allPoints[j].galaxyLocation, false) > (70 + Math.Min(allPoints[i].bitmapVal, allPoints[j].bitmapVal)) / 6 && allPoints[i].galaxyLocation.GetDistanceTo(allPoints[j].galaxyLocation, false) < (16+Math.Min(allPoints[i].bitmapVal, allPoints[j].bitmapVal)) / 4)) {
						Color ci = DracoUtilities.GetColorAt(colors, (int)Math.Floor((allPoints[i].galaxyLocation.X + mapExtents) / xDiv), (int)Math.Floor((allPoints[i].galaxyLocation.Y + mapExtents) / yDiv), width, height);
						Color cj = DracoUtilities.GetColorAt(colors, (int)Math.Floor((allPoints[j].galaxyLocation.X + mapExtents) / xDiv), (int)Math.Floor((allPoints[j].galaxyLocation.Y + mapExtents) / yDiv), width, height);
						float H, S, V;
						Color.RGBToHSV(ci, out H, out S, out V);
						ci.r = (float)Math.Round(H * 32) / 32;
						ci.g = 0;
						ci.b = 0;
						Color.RGBToHSV(cj, out H, out S, out V);
						cj.r = (float)Math.Round(H * 32) / 32;
						cj.g = 0;
						cj.b = 0;
						//ArcenDebugging.ArcenDebugLogSingleLine("    a: " + allPoints[i].galaxyLocation + ": " + ci, Verbosity.DoNotShow);
						//ArcenDebugging.ArcenDebugLogSingleLine("    b: " + allPoints[j].galaxyLocation + ": " + cj, Verbosity.DoNotShow);
						if(ci == cj && allPoints[i].galaxyLocation.GetDistanceTo(allPoints[j].galaxyLocation, false) < 70) {
							bool isvalid = true;
							foreach(Planet plChk in galaxy.Planets) {
								if(plChk != a && plChk != b) {
									isvalid &= !Mat.LineIntersectsRectangleContainingCircle(a.GalaxyLocation, b.GalaxyLocation, plChk.GalaxyLocation, 10);
									plChk.DoForLinkedNeighbors(delegate (Planet item) {
										if(item.PlanetIndex > plChk.PlanetIndex && item != a && item != b) {
											isvalid &= !Mat.LineSegmentIntersectsLineSegment(a.GalaxyLocation, b.GalaxyLocation, plChk.GalaxyLocation, item.GalaxyLocation, 5);

										}
										return isvalid ? DelReturn.Continue : DelReturn.Break;
									});
								}
							}
							if(isvalid)
								a.AddLinkTo(b);
						}
					}
				}
			}
			List<Planet> lowConnectivity = new List<Planet>();
			List<Planet> semiLowConnectivity = new List<Planet>();
			foreach(Planet p in galaxy.Planets) {
				if(p.GetLinkedNeighborCount() == 1) {
					lowConnectivity.Add(p);
				}
				if(p.GetLinkedNeighborCount() <= 2) {
					semiLowConnectivity.Add(p);
				}
			}
			ArcenDebugging.ArcenDebugLogSingleLine(lowConnectivity.Count + " low connectivity nodes, attempting outbound linkages...", Verbosity.DoNotShow);
			//ArcenDebugging.ArcenDebugLogSingleLine("semi-low con: " + semiLowConnectivity.Count, Verbosity.DoNotShow);
			foreach(Planet p in lowConnectivity) {
				if(p.GetLinkedNeighborCount() > 1) continue;
				//ArcenDebugging.ArcenDebugLogSingleLine("  checking " + p.GalaxyLocation, Verbosity.DoNotShow);
				int dist = int.MaxValue;
				Planet ret = null;
				Color ci = DracoUtilities.GetColorAt(colors, (int)Math.Floor((p.GalaxyLocation.X + mapExtents) / xDiv), (int)Math.Floor((p.GalaxyLocation.Y + mapExtents) / yDiv), width, height);
				float H, S, V;
				Color.RGBToHSV(ci, out H, out S, out V);
				ci.r = (float)Math.Round(H * 32) / 32;
				ci.g = 0;
				ci.b = 0;
				//find closest in same color region without overlapping other planets
				foreach(Planet pl in semiLowConnectivity) {
					if(p == pl || p.GetIsDirectlyLinkedTo(pl)) continue;
					int d = pl.GalaxyLocation.GetDistanceTo(p.GalaxyLocation, false);
					if(d < dist) {
						Color cj = DracoUtilities.GetColorAt(colors, (int)Math.Floor((pl.GalaxyLocation.X + mapExtents) / xDiv), (int)Math.Floor((pl.GalaxyLocation.Y + mapExtents) / yDiv), width, height);
						Color.RGBToHSV(cj, out H, out S, out V);
						cj.r = (float)Math.Round(H * 32) / 32;
						cj.g = 0;
						cj.b = 0;
						if(ci == cj) {
							bool isValid = true;
							foreach(Planet plChk in galaxy.Planets) {
								if(plChk != p && plChk != pl && Mat.LineIntersectsRectangleContainingCircle(p.GalaxyLocation, pl.GalaxyLocation, plChk.GalaxyLocation, 15)) {
									isValid = false;
									break;
								}
							}
							if(isValid) {
								dist = d;
								ret = pl;
							}
						}
					}
				}
				if(ret != null) {
					p.AddLinkTo(ret);
				}
			}
			ArcenDebugging.ArcenDebugLogSingleLine("Galaxy complete! Galaxy has " + galaxy.Planets.Count + " planets.", Verbosity.DoNotShow);
		}
	}
}
