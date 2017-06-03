using Arcen.Universal;
using Arcen.Universal.Uniterm;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arcen.AIW2.External
{
    public static class Cheats
    {
        private static ArcenSimContext Context
        {
            get
            {
                return Engine_AIW2.Instance.MainThreadContext;
            }
        }

        [Command( "destroy_selected", "", "", true, false )]
        public static void destroy_selected()
        {
            Engine_AIW2.Instance.DoForSelected( delegate ( GameEntity selected )
             {
                 selected.Die( Context );
                 return DelReturn.Continue;
             } );
        }

        [Command( "toggle_invincible", "", "", true, false )]
        public static void toggle_invincible()
        {
            Engine_AIW2.Instance.DoForSelected( delegate ( GameEntity selected )
            {
                selected.Debug_IgnoresDamage = !selected.Debug_IgnoresDamage;
                return DelReturn.Continue;
            } );
        }

        [Command( "set_shields", "param 1 - percent of max shield health to have after operation", "", true, false )]
        public static void set_shields( string PercentAsString)
        {
            int percentAsInt;
            if(!Int32.TryParse(PercentAsString,out percentAsInt))
            {
                ArcenUI.Instance.AddMessageToUnitermOutput( "Parameter must be an integer" );
                return;
            }
            Engine_AIW2.Instance.DoForSelected( delegate ( GameEntity selected )
            {
                int targetHealth = ( selected.TypeData.BalanceStats.ShieldPoints * percentAsInt ) / 100;
                selected.ShieldPointsLost = selected.TypeData.BalanceStats.ShieldPoints - targetHealth;
                return DelReturn.Continue;
            } );
        }

        [Command( "set_hull", "param 1 - percent of max hull health to have after operation (cannot trigger death)", "", true, false )]
        public static void set_hull( string PercentAsString )
        {
            int percentAsInt;
            if ( !Int32.TryParse( PercentAsString, out percentAsInt ) )
            {
                ArcenUI.Instance.AddMessageToUnitermOutput( "Parameter must be an integer" );
                return;
            }
            Engine_AIW2.Instance.DoForSelected( delegate ( GameEntity selected )
            {
                int targetHealth = ( selected.TypeData.BalanceStats.HullPoints * percentAsInt ) / 100;
                selected.HullPointsLost = selected.TypeData.BalanceStats.HullPoints - targetHealth;
                return DelReturn.Continue;
            } );
        }

        [Command( "revenge", "param 1 - multiples of normal 'if I have this much try to attack' to add to the reconquest budget", "", true, false )]
        public static void revenge( string MultipleAsString )
        {
            int multipleAsInt;
            if ( !Int32.TryParse( MultipleAsString, out multipleAsInt ) )
            {
                ArcenUI.Instance.AddMessageToUnitermOutput( "Parameter must be an integer" );
                return;
            }

            for ( int i = 0; i < World_AIW2.Instance.Sides.Count; i++ )
            {
                WorldSide side = World_AIW2.Instance.Sides[i];
                if ( side.Type != WorldSideType.AI )
                    continue;
                FInt threshold = side.GetSpecificBudgetThreshold( AIBudgetType.Reconquest );
                side.StoredStrengthByBudget[AIBudgetType.Reconquest] += threshold * multipleAsInt;
            }
        }

        [Command( "exterminate", "param 1 - name of the world side type to extinguish on the current planet; valid values are AI, Player, NaturalObject, though generally the last one won't have anything killable", "", true, false )]
        public static void exterminate( string SideTypeAsString )
        {
            WorldSideType sideType;
            try
            {
                sideType = (WorldSideType)Enum.Parse( typeof( WorldSideType ), SideTypeAsString );
            }
            catch
            {
                ArcenUI.Instance.AddMessageToUnitermOutput( "Parameter must be a valid WorldSideType" );
                return;
            }

            Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();

            CombatSide side = planet.Combat.GetFirstSideOfType( sideType );

            side.Entities.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity entity )
            {
                entity.Die( Context );
                return DelReturn.Continue;
            } );
        }
    }
}
