using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public class Window_InGameBottomRightInfo : WindowControllerAbstractBase
    {
        public Window_InGameBottomRightInfo()
        {
            this.OnlyShowInGame = true;
        }

        public class tText : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                {
                    Galaxy currentGalaxy = Engine_AIW2.Instance.NonSim_GetGalaxyBeingCurrentlyViewed();
                    if ( currentGalaxy == null )
                        return;

                    int aiPlanets = 0;
                    int playerPlanets = 0;
                    int otherPlanets = 0;

                    Planet plan;
                    for ( int i = 0; i < currentGalaxy.Planets.Count; i++ )
                    {
                        plan = currentGalaxy.Planets[i];
                        if ( plan.GetIsControlledBySideType( WorldSideType.AI ) )
                            aiPlanets++;
                        else if ( plan.GetIsControlledBySideType( WorldSideType.Player ) )
                            playerPlanets++;
                        else
                            otherPlanets++;
                    }
                    
                    buffer.Add( playerPlanets );
                    buffer.Add( " player planets    " );
                    buffer.Add( aiPlanets );
                    buffer.Add( " ai planets    " );
                    buffer.Add( otherPlanets );
                    buffer.Add( " other planets" );
                }
            }

            public override void OnUpdate()
            {
            }
        }
    }
}