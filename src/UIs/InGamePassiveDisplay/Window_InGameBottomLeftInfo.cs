using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public class Window_InGameBottomLeftInfo : WindowControllerAbstractBase
    {
        public Window_InGameBottomLeftInfo()
        {
            this.OnlyShowInGame = true;
        }

        public class tText : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                {
                    if ( Planet.CurrentlyHoveredOver == null )
                        return;

                    Planet relatedPlanet = Planet.CurrentlyHoveredOver;
                    if ( relatedPlanet != null )
                    {
                        buffer.Add( "Hovered over planet: " );
                        buffer.Add( relatedPlanet.Name );
                        buffer.Add( " (" );
                        buffer.Add( relatedPlanet.GalaxyLocation.ToString() );
                        buffer.Add( ")" );
                        for ( ResourceType resource = ResourceType.None + 1; resource < ResourceType.Length; resource++ )
                            buffer.Add( "\n" ).Add( resource.ToString() ).Add( ": " ).Add( relatedPlanet.ResourceOutputs[resource] );
                    }

                    GameEntity relatedEntity = GameEntity.CurrentlyHoveredOver;
                    if ( relatedEntity != null )
                    {
                        if ( !buffer.GetIsEmpty() )
                            buffer.Add( "\n" );
                        buffer.Add( "Hovered over entity icon: " );
                        relatedEntity.WriteQualifiedNameTo( buffer );
                        buffer.Add( " (" );
                        buffer.Add( relatedEntity.Side.WorldSide.Type.ToString() );
                        buffer.Add( "-" );
                        buffer.Add( relatedEntity.Side.WorldSide.TeamColor.InternalName );
                        buffer.Add( ")" );
                        buffer.Add( "\nID:" ).Add( (int)relatedEntity.PrimaryKeyID );
                    }
                }

                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView )
                {
                    GameEntity relatedEntity = GameEntity.CurrentlyHoveredOver;
                    GameEntityTypeData relatedEntityData = GameEntityTypeData.CurrentlyHoveredOver;
                    if ( relatedEntity != null )
                    {
                        buffer.Add( "Hovered over entity: " );
                        relatedEntity.WriteQualifiedNameTo( buffer );
                        buffer.Add( " (" );
                        buffer.Add( relatedEntity.Side.WorldSide.Type.ToString() );
                        buffer.Add( "-" );
                        buffer.Add( relatedEntity.Side.WorldSide.TeamColor.InternalName );
                        buffer.Add( ")" );
                        buffer.Add( "\n" ).Add( relatedEntity.WorldLocation.ToString() );
                        if ( relatedEntity.TypeData.Category == GameEntityCategory.Ship )
                        {
                            buffer.Add( "\n" ).Add("Hull: ").Add( relatedEntity.GetCurrentHullPoints() ).Add( "/" ).Add( relatedEntity.TypeData.BalanceStats.HullPoints );
                            if ( relatedEntity.TypeData.BalanceStats.ShieldPoints > 0 )
                            {
                                int shieldHealth = relatedEntity.GetCurrentShieldPoints();
                                if ( shieldHealth <= 0 )
                                    buffer.Add( "<color=#ff6151>" );
                                buffer.Add( " " ).Add( "Shields: " ).Add( shieldHealth ).Add( "/" ).Add( relatedEntity.TypeData.BalanceStats.ShieldPoints );
                                if ( shieldHealth <= 0 )
                                    buffer.Add( "</color>" );
                            }
                            if ( relatedEntity.EngineHealthLost > 0 )
                            {
                                int maxEngineHealth = relatedEntity.TypeData.BalanceStats.SquadEngineHealth;
                                if ( maxEngineHealth > 0 )
                                {
                                    int engineHealth = relatedEntity.GetCurrentEngineHealth();
                                    if ( engineHealth <= 0 )
                                        buffer.Add( "<color=#ff6151>" );
                                    buffer.Add( " " ).Add( "Engines: " ).Add( engineHealth ).Add( "/" ).Add( maxEngineHealth );
                                    if ( engineHealth <= 0 )
                                        buffer.Add( "</color>" );
                                }
                            }
                            if ( relatedEntity.TypeData.Balance_ShipsPerSquad > 1 )
                                buffer.Add( " (Squad: " ).Add( relatedEntity.GetCurrentExtraShipsInSquad() + 1 ).Add( "/" ).Add( relatedEntity.TypeData.Balance_ShipsPerSquad ).Add( ")" );
                            if ( relatedEntity.SelfBuildingMetalRemaining > FInt.Zero )
                                buffer.Add( "\n" ).Add( "Self-building:" ).Add( relatedEntity.SelfBuildingMetalRemaining.IntValue ).Add( "/" ).Add( relatedEntity.TypeData.BalanceStats.SquadMetalCost );
                            DoEntityTypeDataPartOfTooltip( buffer, relatedEntity.TypeData );
                        }
                    }
                    else if ( relatedEntityData != null )
                    {
                        buffer.Add( relatedEntityData.Name );
                        if ( relatedEntityData.Category == GameEntityCategory.Ship )
                        {
                            buffer.Add( "\n" ).Add( "Hull: " ).Add( relatedEntityData.BalanceStats.HullPoints );
                            if ( relatedEntityData.BalanceStats.ShieldPoints > 0 )
                                buffer.Add( "\n" ).Add( "Shields: " ).Add( relatedEntityData.BalanceStats.ShieldPoints );
                            DoEntityTypeDataPartOfTooltip( buffer, relatedEntityData );
                            if ( relatedEntityData.Balance_ShipsPerSquad > 1 )
                            {
                                buffer.Add( "\n" ).Add( "Squad Size: " ).Add( relatedEntityData.Balance_ShipsPerSquad );
                                buffer.Add( "\n" ).Add( "Squad Cap: " );
                            }
                            else
                                buffer.Add( "\n" ).Add( "Ship Cap: " );
                            buffer.Add( relatedEntityData.BalanceStats.SquadsPerCap );
                        }
                    }
                }
            }

            private static void DoEntityTypeDataPartOfTooltip( ArcenDoubleCharacterBuffer buffer, GameEntityTypeData relatedEntityData )
            {
                buffer.Add( "\n" ).Add( "Defense: " ).Add( relatedEntityData.Balance_Defense.InternalName );
                for ( int i = 0; i < relatedEntityData.SystemEntries.Count; i++ )
                {
                    SystemEntry entry = relatedEntityData.SystemEntries[i];
                    if ( entry.SubEntries.Count <= 0 )
                        continue;
                    SystemEntry.SubEntry subEntry = entry.SubEntries[0];
                    if ( subEntry.SystemData.Category == EntitySystemCategory.Weapon )
                    {
                        Balance_WeaponType weaponType = subEntry.SystemData.Balance_WeaponType;
                        buffer.Add( "\n" ).Add( "Weapon: " ).Add( weaponType.InternalName ).Add( " Range: " ).Add( subEntry.BalanceStats.Range );
                        if ( relatedEntityData.Balance_ShipsPerSquad > 1 )
                            buffer.Add( " Squad" );
                        buffer.Add( " DPS: " ).Add( subEntry.GetDPS().IntValue );
                        if ( weaponType.CounterType.Counters.Count > 0 )
                        {
                            buffer.Add( " Strong Against" );
                            for ( int j = 0; j < weaponType.CounterType.Counters.Count; j++ )
                                buffer.Add( " " ).Add( weaponType.CounterType.Counters[j].InternalName );
                        }
                    }
                }
                if ( relatedEntityData.BalanceStats.Speed > 0 )
                    buffer.Add( "\n" ).Add( "Speed: " ).Add( relatedEntityData.BalanceStats.Speed );
                if ( relatedEntityData.BalanceStats.SquadMetalCost > 0 )
                    buffer.Add( "\n" ).Add( "Metal Cost: " ).Add( relatedEntityData.BalanceStats.SquadMetalCost );
                if ( relatedEntityData.BalanceStats.SquadFuelConsumption > 0 )
                    buffer.Add( "\n" ).Add( "Fuel Cost: " ).Add( relatedEntityData.BalanceStats.SquadFuelConsumption );
                if ( relatedEntityData.BalanceStats.SquadPowerConsumption > 0 )
                    buffer.Add( "\n" ).Add( "Power Cost: " ).Add( relatedEntityData.BalanceStats.SquadPowerConsumption );
            }

            public override void OnUpdate()
            {
            }
        }
    }
}