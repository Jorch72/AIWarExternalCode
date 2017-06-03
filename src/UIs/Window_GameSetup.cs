using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_GameSetup : WindowControllerAbstractBase
    {
        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( !World_AIW2.Instance.InSetupPhase )
                return false;
            if ( World_AIW2.Instance.HasEverBeenUnpaused )
                return false;
            return true;
        }

        public class bStartGame : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Start Game" );
            }
            public override void HandleClick() { Input_MainHandler.HandleInner( 0, "TogglePause" ); }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bQuitGame : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Exit To Main Menu" );
            }
            public override void HandleClick() { Engine_AIW2.Instance.QuitGameAndGoBackToMainMenu(); }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bRandomSeed : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Randomize Seed" );
            }
            public override void HandleClick()
            {
                WorldSetup setupToSend = new WorldSetup();
                World_AIW2.Instance.Setup.CopyTo( setupToSend );
                setupToSend.Seed = Engine_Universal.PermanentQualityRandom.Next( 1, 999999999 );
                GameCommand command = GameCommand.Create( GameCommandType.SetupOnly_ChangeSetup );
                command.RelatedSetup = setupToSend;
                World_AIW2.Instance.QueueGameCommand( command );
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bUseSeed : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Use This Seed" );
            }
            public override void HandleClick()
            {
                string seedString = iSeed.Instance.CurrentValue;
                int seed;
                if ( !Int32.TryParse( seedString, out seed ) )
                    return;
                WorldSetup setupToSend = new WorldSetup();
                World_AIW2.Instance.Setup.CopyTo( setupToSend );
                setupToSend.Seed = seed;
                GameCommand command = GameCommand.Create( GameCommandType.SetupOnly_ChangeSetup );
                command.RelatedSetup = setupToSend;
                World_AIW2.Instance.QueueGameCommand( command );
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class iSeed : InputAbstractBase
        {
            public static iSeed Instance;
            public iSeed() { Instance = this; }

            private string LastSeedSeen = string.Empty;
            public string CurrentValue = string.Empty;

            public override void HandleChangeInValue( string NewValue )
            {
                this.CurrentValue = NewValue;
            }

            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= 9 )
                    return '\0';
                if ( !char.IsDigit( addedChar ) )
                    return '\0';
                return addedChar;
            }

            public override void OnUpdate()
            {
                string seedAsString = World_AIW2.Instance.Setup.Seed.ToString();
                if ( this.LastSeedSeen != seedAsString )
                {
                    this.LastSeedSeen = seedAsString;
                    this.CurrentValue = seedAsString;
                    ArcenUI_Input elementAsType = (ArcenUI_Input)Element;
                    elementAsType.SetText( seedAsString );
                }
            }
        }

        public class dMapType : DropdownAbstractBase
        {
            public override void GetMainTextToShowPrefix( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "Map Type: " );
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item )
            {
                if ( Item == null )
                    return;
                MapTypeData ItemAsType = (MapTypeData)Item.GetItem();

                WorldSetup setupToSend = new WorldSetup();
                World_AIW2.Instance.Setup.CopyTo( setupToSend );
                setupToSend.MapType = ItemAsType;
                GameCommand command = GameCommand.Create( GameCommandType.SetupOnly_ChangeSetup );
                command.RelatedSetup = setupToSend;
                World_AIW2.Instance.QueueGameCommand( command );
            }

            public override void HandleOverallMouseover() { }

            public override void HandleItemMouseover( IArcenUI_Dropdown_Option Item ) { }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;

                bool foundMismatch = false;
                for ( int i = 0; i < MapTypeDataTable.Instance.Rows.Count; i++ )
                {
                    MapTypeData row = MapTypeDataTable.Instance.Rows[i];
                    if(elementAsType.Items.Count <= i)
                    {
                        foundMismatch = true;
                        break;
                    }
                    IArcenUI_Dropdown_Option option = elementAsType.Items[i];
                    MapTypeData optionItemAsType = (MapTypeData)option.GetItem();
                    if ( row == optionItemAsType )
                        continue;
                    foundMismatch = true;
                    break;
                }

                if ( foundMismatch )
                {
                    elementAsType.Items.Clear();

                    for ( int i = 0; i < MapTypeDataTable.Instance.Rows.Count; i++ )
                    {
                        MapTypeData row = MapTypeDataTable.Instance.Rows[i];
                        DropdownEntryFor_MapTypeData<MapTypeData> option = new DropdownEntryFor_MapTypeData<MapTypeData>( row );
                        elementAsType.Items.Add( option );
                    }
                    elementAsType.SetSelectedItem( World_AIW2.Instance.Setup.MapType );
                }
            }
        }
    }

    public class DropdownEntryFor_MapTypeData<T> : IArcenUI_Dropdown_Option
        where T : ArcenDynamicTableRow
    {
        public T Row;

        public DropdownEntryFor_MapTypeData( T Row )
        {
            this.Row = Row;
        }

        public object GetItem()
        {
            return Row;
        }

        public string GetOptionName()
        {
            return Row.GetDisplayName();
        }

        public Sprite GetOptionSprite()
        {
            return null;
        }
    }
}