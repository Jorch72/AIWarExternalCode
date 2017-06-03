using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public class Window_ResourceBar : WindowControllerAbstractBase
    {
        public Window_ResourceBar()
        {
            this.OnlyShowInGame = true;
        }

        public class tTime : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "Time:" );
                Buffer.Add( Engine_Universal.ToHoursAndMinutesString( World_AIW2.Instance.GameSecond ) );
            }

            public override void OnUpdate()
            {
            }
        }

        public class tMetal : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                buffer.Add( "Metal: " );
                buffer.Add( localSide.StoredMetal.IntValue.ToString( "#,##0" ) );
                if ( localSide.LastFrame_TotalMetalFlowRequested > 0 )
                {
                    buffer.Add( "\n" );
                    FInt percentProduced = localSide.LastFrame_MetalFlowRequestPortionMet * 100;
                    buffer.Add( percentProduced.IntValue );
                    buffer.Add( "%" );
                    if ( localSide.LastFrame_MetalFlowRequestPortionMet < FInt.One &&
                        localSide.LastFrame_MetalProduced > FInt.Zero )
                    {
                        buffer.Add( " (" );
                        int framesLeft = ( localSide.LastFrame_TotalMetalFlowProjectedRequests / localSide.LastFrame_MetalProduced ).GetNearestIntPreferringHigher();
                        int secondsLeft = ( framesLeft * World_AIW2.Instance.SimulationProfile.SecondsPerFrameSim ).GetNearestIntPreferringHigher();
                        buffer.Add( Engine_Universal.ToHoursAndMinutesString( secondsLeft ) );
                        buffer.Add( ")" );
                        buffer.Add( "\n" ).Add( localSide.LastFrame_TotalMetalFlowProjectedRequests.IntValue ).Add( " / " ).Add( localSide.LastFrame_MetalProduced.ReadableString );
                    }
                }
            }

            public override void OnUpdate()
            {
            }
        }

        public class tFuel : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                Buffer.Add( "Fuel: " ).Add( localSide.NetFuel.ToString( "#,##0" ) );
            }

            public override void OnUpdate()
            {
            }
        }

        public class tPower : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet == null )
                    return;
                CombatSide side = planet.Combat.GetSideForWorldSide( localSide );
                Buffer.Add( "Power: " );
                Buffer.Add( side.NetPower.ToString( "#,##0" ) );
            }

            public override void OnUpdate()
            {
            }
        }

        public class tScience : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                Buffer.Add( "Science: " ).Add( localSide.StoredScience.IntValue.ToString( "#,##0" ) );
            }

            public override void OnUpdate()
            {
            }
        }

        public class tAIProgress : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "AIP: " ).Add( World_AIW2.Instance.AIProgress_Effective.IntValue.ToString( "#,##0" ) );
            }

            public override void OnUpdate()
            {
            }
        }

        public class tHacking : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                Buffer.Add( "Hacking: " ).Add( localSide.StoredHacking.IntValue.ToString( "#,##0" ) );
                GameEntity hacker = localSide.Entities.GetFirstMatching( EntityRollupType.KingUnits );
                if ( hacker == null || hacker.ActiveHack == null )
                    return;
                GameEntity target = World_AIW2.Instance.GetEntityByID( hacker.ActiveHack_Target );
                if ( target == null )
                    return;
                int secondsSoFar = hacker.ActiveHack_DurationThusFar;
                int totalDuration = hacker.ActiveHack.Implementation.GetTotalSecondsToHack( target, hacker );
                int secondsLeft = totalDuration - secondsSoFar;
                Buffer.Add( "\n" ).Add( Engine_Universal.ToHoursAndMinutesString( secondsLeft ) );
            }

            public override void OnUpdate()
            {
            }
        }

        public class tWavePrediction : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                World_AIW2.Instance.DoForSides( delegate ( WorldSide side )
                 {
                     if ( side.Type != WorldSideType.AI )
                         return DelReturn.Continue;
                     FInt threshold = side.GetSpecificBudgetThreshold( AIBudgetType.Wave );
                     FInt perSecond = side.GetSpecificBudgetStrengthGainPerSecond( AIBudgetType.Wave );
                     if ( perSecond <= FInt.Zero )
                         return DelReturn.Continue;
                     FInt currentAmount = side.StoredStrengthByBudget[AIBudgetType.Wave];
                     FInt amountLeft = threshold - currentAmount;
                     FInt secondsLeft = amountLeft / perSecond;
                     Buffer.Add( "Next wave in " );
                     Buffer.Add( Engine_Universal.ToHoursAndMinutesString( secondsLeft.IntValue ) );
                     Buffer.Add( " (" ).Add( currentAmount.ReadableString ).Add( "/" ).Add( threshold.ReadableString ).Add( ")" );
                     return DelReturn.Break;
                 } );
            }

            public override void OnUpdate()
            {
            }
        }

        public class tCPAPrediction : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                World_AIW2.Instance.DoForSides( delegate ( WorldSide side )
                {
                    if ( side.Type != WorldSideType.AI )
                        return DelReturn.Continue;
                    FInt threshold = side.GetSpecificBudgetThreshold( AIBudgetType.CPA );
                    FInt perSecond = side.GetSpecificBudgetStrengthGainPerSecond( AIBudgetType.CPA );
                    if ( perSecond <= FInt.Zero )
                        return DelReturn.Continue;
                    FInt currentAmount = side.StoredStrengthByBudget[AIBudgetType.CPA];
                    FInt amountLeft = threshold - currentAmount;
                    FInt secondsLeft = amountLeft / perSecond;
                    buffer.Add( "Next CPA in " );
                    buffer.Add( Engine_Universal.ToHoursAndMinutesString( secondsLeft.IntValue ) );
                    buffer.Add( " (" ).Add( currentAmount.ReadableString ).Add( "/" ).Add( threshold.ReadableString ).Add(")");
                    return DelReturn.Break;
                } );
            }

            public override void OnUpdate()
            {
            }
        }

        public class tThreat : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                Galaxy galaxy = Engine_AIW2.Instance.NonSim_GetGalaxyBeingCurrentlyViewed();
                if ( galaxy == null )
                    return;
                FInt threat = FInt.Zero;
                for(int i = 0; i <  galaxy.Planets.Count;i++)
                {
                    Planet planet = galaxy.Planets[i];
                    if ( planet.GetController().Side.WorldSide.Type == WorldSideType.Player )
                        continue;
                    threat += planet.AIThreatStrength;
                }
                Buffer.Add( "Threat: " ).Add( threat.IntValue );
            }

            public override void OnUpdate()
            {
            }
        }

        public class tAttack : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                Galaxy galaxy = Engine_AIW2.Instance.NonSim_GetGalaxyBeingCurrentlyViewed();
                if ( galaxy == null )
                    return;
                FInt attack = FInt.Zero;
                for ( int i = 0; i < galaxy.Planets.Count; i++ )
                {
                    Planet planet = galaxy.Planets[i];
                    if ( planet.GetController().Side.WorldSide.Type != WorldSideType.Player )
                        continue;
                    attack += planet.AIThreatStrength;
                }
                Buffer.Add( "Attack: " ).Add( attack.IntValue );
            }

            public override void OnUpdate()
            {
            }
        }
    }
}