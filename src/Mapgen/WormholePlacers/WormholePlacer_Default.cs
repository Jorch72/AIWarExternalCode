using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arcen.AIW2.External
{
    public interface IWormholePlacer
    {
        ArcenPoint GetPointForWormhole( ArcenSimContext Context, Planet ThisPlanet, Planet PlanetThatWormholeWillGoTo );
    }

    public class WormholePlacer_Default : IWormholePlacer
    {
        public readonly int MinimumWormholeDistance;
        public readonly int MaximumWormholeDistance;

        public WormholePlacer_Default(int MinimumWormholeDistance, int MaximumWormholeDistance)
        {
            this.MinimumWormholeDistance = MinimumWormholeDistance;
            this.MaximumWormholeDistance = MaximumWormholeDistance;
        }

        public ArcenPoint GetPointForWormhole( ArcenSimContext Context, Planet ThisPlanet, Planet PlanetThatWormholeWillGoTo )
        {
            int wormholeRadius = Context.QualityRandom.Next( this.MinimumWormholeDistance, this.MaximumWormholeDistance );

            AngleDegrees angleToNeighbor = ThisPlanet.GalaxyLocation.GetAngleToDegrees( PlanetThatWormholeWillGoTo.GalaxyLocation );
            ArcenPoint wormholePoint = Engine_AIW2.Instance.CombatCenter.GetPointAtAngleAndDistance( angleToNeighbor, wormholeRadius );

            return wormholePoint;
        }
    }
}
