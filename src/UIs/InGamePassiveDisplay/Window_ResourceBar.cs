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

            public override void HandleMouseover()
            {
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Time elapsed in game" );
            }

        }

        public class tMetal : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return;
                buffer.Add( "<sprite name=\"Metal\">" );
                buffer.Add( localSide.StoredMetal.IntValue.ToString( "#,##0" ) );

                if ( localSide.LastFrame_TotalMetalFlowRequested > 0 )
                  {
                    buffer.Add( "   (" );
                    int amountSpentLastFrame = localSide.LastFrame_MetalSpent.IntValue;
                    int incomeLastFrame = localSide.LastFrame_MetalProduced.GetNearestIntPreferringHigher();
                    int netIncome = incomeLastFrame - amountSpentLastFrame;
                    if(netIncome > 0)
                      buffer.Add( "+" );
                    buffer.Add( netIncome);
                    buffer.Add( " net)" );
                    if ( localSide.LastFrame_MetalFlowRequestPortionMet < FInt.One &&
                        localSide.LastFrame_MetalProduced > FInt.Zero )
                    {
                        buffer.Add( " (" );
                        int framesLeft = ( localSide.LastFrame_TotalMetalFlowProjectedRequests / localSide.LastFrame_MetalProduced ).GetNearestIntPreferringHigher();
                        int secondsLeft = ( framesLeft * World_AIW2.Instance.SimulationProfile.SecondsPerFrameSim ).GetNearestIntPreferringHigher();
                        buffer.Add( Engine_Universal.ToHoursAndMinutesString( secondsLeft ) );
                        buffer.Add( " est)" ); //estimated time left
                        buffer.Add( "\n required: " ).Add( localSide.LastFrame_TotalMetalFlowProjectedRequests.IntValue ).Add( "\nproduced: " ).Add( localSide.LastFrame_MetalProduced.GetNearestIntPreferringHigher() );
                    }
                }
            }

            public override void OnUpdate()
            {
            }

            public override void HandleMouseover()
            {
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Metal: Required to build things" );
            }

        }

        public class tFuel : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return;
                Buffer.Add( "<sprite name=\"Fuel\">" ).Add( localSide.NetFuel.ToString( "#,##0" ) );
            }

            public override void OnUpdate()
            {
            }

            public override void HandleMouseover()
            {
              WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
              if ( localSide == null )
                return;

              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Available Fuel. Fuel is a global resource required to power fleetships and starships.\nUsed/Total: (" + localSide.FuelConsumption.ToString() + "/" + localSide.FuelProduction + ")" );
            }

        }

        public class tPower : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return;
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet == null )
                    return;
                CombatSide side = planet.Combat.GetSideForWorldSide( localSide );
                Buffer.Add( "<sprite name=\"Power\">" );
                Buffer.Add( side.NetPower.ToString( "#,##0" ) );
            }

            public override void OnUpdate()
            {
            }

            public override void HandleMouseover()
            {
              WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
              if ( localSide == null )
                return;
              Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
              if ( planet == null )
                return;

              CombatSide side = planet.Combat.GetSideForWorldSide( localSide );
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Available Power. Power is per-planet and is required to build structures.\nUsed/Total: (" + side.PowerConsumption.ToString() + "/" + side.PowerProduction.ToString() + ")" );
            }
        }

        public class tScience : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return;
                Buffer.Add( "<sprite name=\"Science\">" ).Add( localSide.StoredScience.IntValue.ToString( "#,##0" ) );
            }

            public override void OnUpdate()
            {
            }

            public override void HandleMouseover()
            {
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Science: unlocks higher level units and turrets" );
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

            public override void HandleMouseover()
            {
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "AI Progress: Measures AI Agressiveness" );
            }

        }

        public class tHacking : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return;
                Buffer.Add( "<sprite name=\"Hacking\">" ).Add( localSide.StoredHacking.IntValue.ToString( "#,##0" ) );
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

            public override void HandleMouseover()
            {
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Hacking: Used to exploit flaws in the AI internal network" );
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
                     if(secondsLeft < 60)
                       Buffer.Add( " <color=#ff0000>" );
                     Buffer.Add( "Next wave in " );
                     Buffer.Add( Engine_Universal.ToHoursAndMinutesString( secondsLeft.IntValue ) );
                     Buffer.Add( " (" ).Add( currentAmount.ReadableString ).Add( "/" ).Add( threshold.ReadableString ).Add( ")" );
                     if(secondsLeft < 60)
                       Buffer.Add( "</color>" );
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
                if ( !Engine_Universal.DebugOutputOn )
                    return;
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
                    threat += planet.Combat.GetLocalPlayerSide().DataByStance[SideStance.Hostile].ThreatStrength;
                }
                Buffer.Add( "Threat: " ).Add( threat.IntValue );
            }

            public override void OnUpdate()
            {
            }

            public override void HandleMouseover()
            {
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Threat: AI forces actively waiting to strike" );
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
                    attack += planet.Combat.GetLocalPlayerSide().DataByStance[SideStance.Hostile].ThreatStrength;
                }
                Buffer.Add( "Attack: " ).Add( attack.IntValue );
            }

            public override void OnUpdate()
            {
            }

            public override void HandleMouseover()
            {
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Attack: AI forces on your planets" );
            }

        }
    }
}