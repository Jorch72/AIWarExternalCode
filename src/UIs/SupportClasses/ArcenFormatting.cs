using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arcen.AIW2.External
{
    public static class ArcenExternalUIUtilities
    {
        public static string GetRoundedNumberWithSuffix( ref int NumericValue )
        {
            return GetRoundedNumberWithSuffix( ref NumericValue, false );
        }

        public static string GetRoundedNumberWithSuffix( ref int NumericValue, bool beAggressive )
        {
            string suffix = string.Empty;
            int millionsThreshold = beAggressive ? 1000000 : 100000000;
            int thousandsThreshold = beAggressive ? 1000 : 100000;
            if ( NumericValue >= millionsThreshold )
            {
                suffix = "m";
                NumericValue = (int)( NumericValue / 1000000f );
            }
            else if ( NumericValue >= thousandsThreshold )
            {
                suffix = "k";
                NumericValue = (int)( NumericValue / 1000f );
            }

            return suffix;
        }
        
        public static GameEntity GetEntityToUseForBuildMenu()
        {
            GameEntity possibleEntity = null;
            Engine_AIW2.Instance.DoForSelected( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, delegate ( GameEntity selected )
            { 
                if ( selected.TypeData.BuildMenus == null || selected.TypeData.BuildMenus.Count <= 0 )
                {
                    possibleEntity = null;
                    return DelReturn.Break;
                }
                if ( possibleEntity != null && possibleEntity.TypeData != selected.TypeData )
                {
                    possibleEntity = null;
                    return DelReturn.Break;
                }
                if ( possibleEntity == null )
                    possibleEntity = selected;
                return DelReturn.Continue;
            } );
            return possibleEntity;
        }
    }
}
