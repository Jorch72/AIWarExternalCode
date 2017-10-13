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
                int debugStage = 0;
                try
                {
                    debugStage = 1;
                    if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView && 
                         Planet.CurrentlyHoveredOver != null &&
                         GameEntityTypeData.CurrentlyHoveredOver == null &&
                         GameEntity.CurrentlyHoveredOver == null )
                    {
                        debugStage = 2;
                        Planet relatedPlanet = Planet.CurrentlyHoveredOver;
                        if ( relatedPlanet != null )
                        {
                            debugStage = 3;
                            buffer.Add( "<u>Hovered over planet:</u>\n" );
                            WorldSide owner = relatedPlanet.GetControllingSide();
                            if ( owner.Type == WorldSideType.AI )
                                relatedPlanet.MarkLevel.WriteStartColorHexTo( buffer );

                            debugStage = 4;
                            buffer.Add( relatedPlanet.Name );

                            debugStage = 5;
                            if ( owner.Type == WorldSideType.AI )
                            {
                                buffer.Add( "  " ).Add( relatedPlanet.MarkLevel.Abbreviation );
                                relatedPlanet.MarkLevel.WriteEndColorHexTo( buffer );
                            }

                            debugStage = 6;
                            buffer.Add( " <color=#8b8b8b>(" );
                            buffer.Add( relatedPlanet.GalaxyLocation.ToString() );
                            buffer.Add( ")</color>" );
                            if ( owner.Type == WorldSideType.NaturalObject )
                                buffer.Add( "\nPlanet is neutral territory" );
                            else
                            {
                                buffer.Add( "\nPlanet owned by: " );
                                buffer.Add( " (" );
                                buffer.Add( owner.Type.ToString() );
                                buffer.Add( "-" );
                                buffer.Add( owner.TeamColor.InternalName );
                                buffer.Add( ")" );
                            }

                            debugStage = 7;
                            bool isFirstResource = true;
                            for ( ResourceType resource = ResourceType.None + 1; resource < ResourceType.Length; resource++ )
                            {
                                if ( isFirstResource )
                                {
                                    buffer.Add( "\n" );
                                    isFirstResource = false;
                                }
                                else
                                    buffer.Add( "  " );
                                buffer.Add( "<color=#ffee8e>" ).Add( resource.ToString() ).Add( ":</color> " ).Add( relatedPlanet.ResourceOutputs[resource] );
                            }
                        }
                    }

                    debugStage = 30;
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Exception in lower left tooltip text generation at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }
            }

            private static void DoEntityTypeDataPartOfTooltip( ArcenDoubleCharacterBuffer buffer, GameEntityTypeData relatedEntityData )
            {
                for ( int i = 0; i < relatedEntityData.SystemEntries.Count; i++ )
                {
                    SystemEntry entry = relatedEntityData.SystemEntries[i];
                    if ( entry.SubEntries.Count <= 0 )
                        continue;
                    SystemEntry.SubEntry subEntry = entry.SubEntries[0];
                    if ( subEntry.SystemData.Category == EntitySystemCategory.Weapon )
                    {
                        Balance_WeaponType weaponType = subEntry.SystemData.Balance_WeaponType;
                        buffer.Add( "\n" ).Add( "<color=#ffee8e>Weapon:</color> " ).Add( weaponType.InternalName ).Add( "    Range: " ).Add( subEntry.BalanceStats.Range );

                        buffer.Add( "\n" );
                        if ( relatedEntityData.Balance_ShipsPerSquad > 1 )
                            buffer.Add( "Squad " );
                        buffer.Add( "DPS: " ).Add( subEntry.GetDPS().IntValue );
                        if ( weaponType.CounterType.Counters.Count > 0 )
                        {
                            buffer.Add( "    Strong Against" );
                            for ( int j = 0; j < weaponType.CounterType.Counters.Count; j++ )
                                buffer.Add( " " ).Add( weaponType.CounterType.Counters[j].InternalName );
                        }
                    }
                }

                buffer.Add( "\n" ).Add( "Defense: " ).Add( relatedEntityData.Balance_Defense.InternalName );
                if ( relatedEntityData.BalanceStats.Speed > 0 )
                    buffer.Add( "    " ).Add( "Speed: " ).Add( relatedEntityData.BalanceStats.Speed );
            }

            private static ArcenDoubleCharacterBuffer AddSpacingOrLine( ref bool HasAlreadyHadNewline, ArcenDoubleCharacterBuffer buffer )
            {
                if ( HasAlreadyHadNewline )
                    buffer.Add( "    " );
                else
                {
                    HasAlreadyHadNewline = true;
                    buffer.Add( "\n" );
                }
                return buffer;
            }

            public override void OnUpdate()
            {
            }
        }
    }
}