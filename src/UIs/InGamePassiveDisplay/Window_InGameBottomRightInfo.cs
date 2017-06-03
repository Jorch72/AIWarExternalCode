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
                else if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView )
                {
                    Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                    if ( planet == null )
                        return;

                    int playerSquadCount = 0;
                    int enemySquadCount = 0;
                    int playerShipCount = 0;
                    int enemyShipCount = 0;

                    planet.Combat.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity ship )
                    {
                        int count = 1 + ship.GetCurrentExtraShipsInSquad();
                        if ( ship.Side.GetIsFriendlyToLocalSide() )
                        {
                            playerShipCount += count;
                            playerSquadCount++;
                        }
                        else
                        {
                            enemyShipCount += count;
                            enemySquadCount++;
                        }
                        return DelReturn.Continue;
                    } );
                    
                    buffer.Add( playerShipCount )
                        .Add( " player ships (" )
                        .Add( playerSquadCount )
                        .Add( " squads)     " )
                        .Add( enemyShipCount )
                        .Add( " enemy ships (" )
                        .Add( enemySquadCount )
                        .Add( " squads)" )
                        ;
                }
            }

            public override void OnUpdate()
            {
            }
        }
    }
}