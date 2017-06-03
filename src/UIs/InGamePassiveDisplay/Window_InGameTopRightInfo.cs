using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public class Window_InGameTopRightInfo : WindowControllerAbstractBase
    {
        public Window_InGameTopRightInfo()
        {
            this.OnlyShowInGame = true;
        }

        public class tText : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet == null )
                    return;
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                {
                    buffer.Add( "Galaxy Map\nCurrent Planet: " );
                    buffer.Add( planet.Name );
                }
                else if(Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView)
                {
                    buffer.Add( "Viewing Planet: " );
                    buffer.Add( planet.Name );
                }
            }

            public override void OnUpdate()
            {
            }
        }
    }
}