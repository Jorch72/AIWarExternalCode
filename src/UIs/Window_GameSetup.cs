 using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_GameSetup : WindowControllerAbstractBase
    {
        public static Window_GameSetup Instance;
        public MapTypeData currentMapType;
        public Window_GameSetup()
        {
          currentMapType = null;
          Instance = this;
        }
        // public override void OnOpen()
        // {
        //     ArcenSettingTable.Instance.CopyCurrentValuesToTemp();
        // }
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
            public override MouseHandlingResult HandleClick()
            {
                Window_SaveGameMenu.Instance.OverallCampaignName = "";
                Input_MainHandler.HandleInner( 0, "TogglePause" );
                return MouseHandlingResult.None;
             }

            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bQuitGame : ButtonAbstractBase
        {
            public static bQuitGame Instance;
            public bQuitGame() { Instance = this; }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Exit To Main Menu" );
            }
            public override MouseHandlingResult HandleClick()
            {
                Window_SaveGameMenu.Instance.OverallCampaignName = "";
                Engine_AIW2.Instance.QuitGameAndGoBackToMainMenu();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bRandomSeed : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Random" );
            }
            public override MouseHandlingResult HandleClick()
            {
                WorldSetup setupToSend = new WorldSetup();
                World_AIW2.Instance.Setup.CopyTo( setupToSend );
                setupToSend.Seed = Engine_Universal.PermanentQualityRandom.Next( 1, 999999999 );
                GameCommand command = GameCommand.Create( GameCommandType.SetupOnly_ChangeSetup );
                command.RelatedSetup = setupToSend;
                World_AIW2.Instance.QueueGameCommand( command, true );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bUseSeed : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Generate" );
            }
            public override MouseHandlingResult HandleClick()
            {
                string seedString = iSeed.Instance.CurrentValue;
                int seed;
                if ( !Int32.TryParse( seedString, out seed ) )
                    return MouseHandlingResult.PlayClickDeniedSound;
                WorldSetup setupToSend = new WorldSetup();
                World_AIW2.Instance.Setup.CopyTo( setupToSend );
                setupToSend.Seed = seed;
                GameCommand command = GameCommand.Create( GameCommandType.SetupOnly_ChangeSetup );
                command.RelatedSetup = setupToSend;
                World_AIW2.Instance.QueueGameCommand( command, true );
                ArcenSettingTable.Instance.CopyTempValuesToCurrent();
                GameSettings.Current.SaveToDisk();
                
                return MouseHandlingResult.None;
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
                if ( input.Length >= 10 )
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
                Window_GameSetup.Instance.currentMapType = ItemAsType;
                WorldSetup setupToSend = new WorldSetup();
                World_AIW2.Instance.Setup.CopyTo( setupToSend );
                setupToSend.MapType = ItemAsType;
                GameCommand command = GameCommand.Create( GameCommandType.SetupOnly_ChangeSetup );
                command.RelatedSetup = setupToSend;
                World_AIW2.Instance.QueueGameCommand( command, true );
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
                        //the following lines display the correct map name
                        //for this map type (aka this row)
                        DropdownEntryFor_MapTypeData<MapTypeData> option = new DropdownEntryFor_MapTypeData<MapTypeData>( row );
                        elementAsType.Items.Add( option );
                    }
                    elementAsType.SetSelectedItem( World_AIW2.Instance.Setup.MapType );
                }
            }
        }

        public class bsSpecialFactions : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                if ( elementAsType.Buttons.Count > 0 )
                    return;


                for(int i = 0; i < SpecialFactionDataTable.Instance.Rows.Count;i++)
                {
                    SpecialFactionData row = SpecialFactionDataTable.Instance.Rows[i];
                    if ( row.AlwaysIncluded )
                        continue;
                    bSpecialFaction newButtonController = new bSpecialFaction( row );
                    Vector2 offset;
                    offset.x = 0;
                    offset.y = elementAsType.Buttons.Count * elementAsType.ButtonHeight;
                    Vector2 size;
                    size.x = elementAsType.ButtonWidth;
                    size.y = elementAsType.ButtonHeight;
                    elementAsType.AddButton( newButtonController, size, offset );
                }
            }
        }

        private class bSpecialFaction : ButtonAbstractBase
        {
            public SpecialFactionData Data;

            public bSpecialFaction(  SpecialFactionData Data )
            {
                this.Data = Data;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                if ( World_AIW2.Instance.Setup.SpecialFactions.Contains( Data ) )
                    buffer.Add( "(On) " );
                else
                    buffer.Add( "(Off) " );
                buffer.Add( Data.Name );
            }

            public override MouseHandlingResult HandleClick()
            {
                WorldSetup setupToSend = new WorldSetup();
                World_AIW2.Instance.Setup.CopyTo( setupToSend );
                if ( World_AIW2.Instance.Setup.SpecialFactions.Contains( Data ) )
                    setupToSend.SpecialFactions.Remove( Data );
                else
                    setupToSend.SpecialFactions.Add( Data );
                GameCommand command = GameCommand.Create( GameCommandType.SetupOnly_ChangeSetup );
                command.RelatedSetup = setupToSend;
                World_AIW2.Instance.QueueGameCommand( command, true );
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }

        public class bsConducts : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                if ( elementAsType.Buttons.Count > 0 )
                    return;

                for ( int i = 0; i < ConductTypeTable.Instance.Rows.Count; i++ )
                {
                    ConductType row = ConductTypeTable.Instance.Rows[i];
                    bConduct newButtonController = new bConduct( row );
                    Vector2 offset;
                    offset.x = 0;
                    offset.y = elementAsType.Buttons.Count * elementAsType.ButtonHeight;
                    Vector2 size;
                    size.x = elementAsType.ButtonWidth;
                    size.y = elementAsType.ButtonHeight;
                    elementAsType.AddButton( newButtonController, size, offset );
                }
            }
        }

        private class bConduct : ButtonAbstractBase
        {
            public ConductType Data;

            public bConduct( ConductType Data )
            {
                this.Data = Data;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                if ( World_AIW2.Instance.Setup.Conducts.Contains( Data ) )
                    buffer.Add( "(On) " );
                else
                    buffer.Add( "(Off) " );
                buffer.Add( Data.Name );
            }

            public override MouseHandlingResult HandleClick()
            {
                WorldSetup setupToSend = new WorldSetup();
                World_AIW2.Instance.Setup.CopyTo( setupToSend );
                if ( World_AIW2.Instance.Setup.Conducts.Contains( Data ) )
                    setupToSend.Conducts.Remove( Data );
                else
                    setupToSend.Conducts.Add( Data );
                GameCommand command = GameCommand.Create( GameCommandType.SetupOnly_ChangeSetup );
                command.RelatedSetup = setupToSend;
                World_AIW2.Instance.QueueGameCommand( command, true );
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }
      /* This code handles per-map-type additional options */
      public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
      {
        bool localDebug = false;
        MapTypeData mapType = Window_GameSetup.Instance.currentMapType;

        //Check for the conduct to enable map settings
        for ( int i = 0; i < ConductTypeTable.Instance.Rows.Count; i++ )
          {
           ConductType row = ConductTypeTable.Instance.Rows[i];
           if(localDebug)
             ArcenDebugging.ArcenDebugLogSingleLine("Window_MapSetup: conduct <" + row.Name + ">", Verbosity.DoNotShow);

           if ( ArcenStrings.Equals( row.Name, "Show Map Setting Options") )
             {
               if ( !World_AIW2.Instance.Setup.Conducts.Contains( row ) )
                 {
                   if(localDebug)
                     ArcenDebugging.ArcenDebugLogSingleLine("Window_MapSetup: conducts does not contain " + row.Name, Verbosity.DoNotShow);

                   return;
                 }
               else
                 if(localDebug)
                   ArcenDebugging.ArcenDebugLogSingleLine("Window_MapSetup: conducts contains " + row.Name, Verbosity.DoNotShow);

             }
          }

        if(mapType == null)
          {
            //It seems like the mapType is actually null when you initially enter the GameSetup screen
            //This means you need to change the map type from the first one in order to see any options
            return;
          }
        string mapName = mapType.InternalName;
        if(localDebug)
          ArcenDebugging.ArcenDebugLogSingleLine("Window_MapSetup: Map type is " + mapName, Verbosity.DoNotShow);

        //Dimensions for the Map Settings options
        int settingsWidth = 55;
        int settingsHeight = 50;
        int settingsStartX = 03;
        int settingsStartY = 50;

        //Handle the header (the header just says "Here are Map Settings for $maptype"
        int headerHeight = 10;
        int headerWidth = 45;

        //Note for modders: (startX, startY, width, height) are ther CreateUNityRect args
        Rect mapTypeAreaBounds = ArcenRectangle.CreateUnityRect( settingsStartX, settingsStartY, settingsWidth, settingsHeight); 
        Rect headerTextBounds = ArcenRectangle.CreateUnityRect( settingsStartX, settingsStartY - headerHeight, headerWidth, headerHeight );
        AddText( Set, typeof( tMapSettingsHeader ), 0, headerTextBounds );

        //Get the list of settings for the current map type
        List<string> settingNamesForMap = getSettingNamesForMap(mapName);

        //Handle each setting
        int heightPerSetting = 5;
        int descriptionWidth = 25;
        int paddingWidth = 8; //the space between the description and the setting itself
        int settingWidth = 12;
        for(int i = 0; i < settingNamesForMap.Count; i++)
          {

            string settingName = settingNamesForMap[i];
            ArcenSetting thisSetting = getSettingByName(settingName);
            int CodeDirective = thisSetting.RowIndex; //this is given to the settingHandler code so it can find the right setting
            if(localDebug)
              ArcenDebugging.ArcenDebugLogSingleLine("Window_MapSetup: parsing " + settingName , Verbosity.DoNotShow);

            if(thisSetting == null)
              {
                if(localDebug)
                  ArcenDebugging.ArcenDebugLogSingleLine("Window_MapSetup: setting " + settingName + " is null. Skip it", Verbosity.DoNotShow);
                continue;
              }
            
            //foreach setting, I need to print the name and then a button to let you modify that setting
            Rect settingNameRect= ArcenRectangle.CreateUnityRect(settingsStartX, settingsStartY + heightPerSetting*i, descriptionWidth, heightPerSetting);
            AddText(Set, typeof( tSettingName), CodeDirective, settingNameRect);

            Rect settingButtonRect =  ArcenRectangle.CreateUnityRect(settingsStartX + descriptionWidth + paddingWidth, settingsStartY + heightPerSetting*i, settingWidth, heightPerSetting);

            //A couple settings need to be handled differently, as dropdowns
            if ( ArcenStrings.Equals( settingName, "SimpleLinkMethod") ) 
            {
              if(localDebug)
                  ArcenDebugging.ArcenDebugLogSingleLine("Window_MapSetup: handling " + settingName +" seperately ", Verbosity.DoNotShow);

              AddDropdown( Set, typeof( SimpleLinkDropdown ), CodeDirective, settingButtonRect );
            }
            else if (ArcenStrings.Equals( settingName, "SolarSystemsLinkMethod") )
              {
                if(localDebug)
                  ArcenDebugging.ArcenDebugLogSingleLine("Window_MapSetup: handling " + settingName +" seperately ", Verbosity.DoNotShow);

                AddDropdown( Set, typeof( SolarLinkDropdown ), CodeDirective, settingButtonRect );
              }
            else if (ArcenStrings.Equals( settingName, "NumberOfNebulae")  || ArcenStrings.Equals( settingName, "NumberOfAsteroids") ||
                     ArcenStrings.Equals( settingName, "NebulaeConnectivity"))
              {
                if(localDebug)
                  ArcenDebugging.ArcenDebugLogSingleLine("Window_MapSetup: handling " + settingName +" seperately ", Verbosity.DoNotShow);

                AddDropdown( Set, typeof( LowMedHighDropdown ), CodeDirective, settingButtonRect );
              }

            else if(ArcenStrings.Equals( settingName, "OctopusNumArms") )
              {
                if(localDebug)
                  ArcenDebugging.ArcenDebugLogSingleLine("Window_MapSetup: handling " + settingName +" seperately ", Verbosity.DoNotShow);

                AddDropdown( Set, typeof( dIntDropdownOctopus ), CodeDirective, settingButtonRect );
              }
            else
              {
                if(localDebug)
                  ArcenDebugging.ArcenDebugLogSingleLine("Window_MapSetup: " + settingName + " is not a dropdown, so handle normally", Verbosity.DoNotShow);

                //This is the default case; check the setting and add whatever is appropriate
                switch ( thisSetting.Type )
                  {
                  case ArcenSettingType.BoolHidden:
                    AddButton( Set, typeof( bToggle ), CodeDirective, settingButtonRect );
                    break;
                  case ArcenSettingType.IntHidden:
                    AddInput( Set, typeof( iIntInput ), CodeDirective, settingButtonRect );
                    break;
                  case ArcenSettingType.FloatHidden:
                    AddHorizontalSlider( Set, typeof( sFloatSlider ), CodeDirective, settingButtonRect );
                    break;
                  case ArcenSettingType.IntDropdown:
                    //Note this bit currently doesn't work, but I'm leaving it for reference for when we (hopefully) get IntDropdownHidden
                    AddDropdown( Set, typeof( dIntDropdown ), CodeDirective, settingButtonRect );
                    break;
                  default:
                    ArcenDebugging.ArcenDebugLogSingleLine("element has no setting type", Verbosity.DoNotShow);
                    break;
                  }
              }
          }
      }
      
      public class tSettingName : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                buffer.Add( setting.DisplayName );
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
            }
        }

      private static ArcenSetting GetSettingForController( ElementAbstractBase controller )
        {
          //Called by the classes that handle each setting (button, floatslider, etc)
          //This maps from the CodeDirective to the actual ArcenSetting
            int tableIndex = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag;
            if ( tableIndex >= ArcenSettingTable.Instance.Rows.Count )
                return null;
            return ArcenSettingTable.Instance.Rows[tableIndex];
        }

      public class bToggle : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                buffer.Add( setting.TempValue_Bool ? "On" : "Off" );
            }

            public override MouseHandlingResult HandleClick()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return MouseHandlingResult.None;

                setting.TempValue_Bool = !setting.TempValue_Bool;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
            }
        }
      public class sFloatSlider : SliderAbstractBase
        {
            public override void OnUpdate()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                float currentValue = setting.TempValue_Float;
                float range = setting.MaxFloatValue - setting.MinFloatValue;
                if ( range == 0 ) range = 1;
                float currentPortion = ( currentValue - setting.MinFloatValue ) / range;

                ArcenUI_Slider elementAsType = (ArcenUI_Slider)Element;
                elementAsType.ReferenceSlider.value = currentPortion;
            }

            public override void OnChange( float NewValue )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                float range = setting.MaxFloatValue - setting.MinFloatValue;
                float adjustedNewValue = setting.MinFloatValue + ( range * NewValue );

                setting.TempValue_Float = adjustedNewValue;
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
            }
        }

      public class SimpleLinkDropdown : DropdownAbstractBase
      {
        //NOTES: it looks like IArcenUI_Dropdown_Option is an interface class
        //one must define GetItem(), GetOptionName() and so on

        public override void GetMainTextToShowPrefix( ArcenDoubleCharacterBuffer Buffer )
        {
          //this would appear before the name of any item in the dropdown
          //Like "Map Type: <dropdown Entry>"
        }

        public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item )
        {
          if ( Item == null )
            return;
          ArcenSetting setting = GetSettingForController( this );
          if ( setting == null ) return;

//          ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;

  //        int newValue = elementAsType.Items.IndexOf( Item );

          setting.TempValue_Int = (int)Item.GetItem(); //this returns the int value in Item (a SimpleLinkMethod)
          setting.TempValue_String = setting.TempValue_Int.ToString();
//          ArcenDebugging.ArcenDebugLogSingleLine("changed tempValue to " + setting.TempValue_Int, Verbosity.DoNotShow);
        }
        public override void HandleMouseover()
        {
          ArcenSetting setting = GetSettingForController( this );
          if ( setting == null ) return;
          if ( setting.Description.Length > 0 )
            Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
        }
        public override void HandleOverallMouseover() { }
        public override void HandleItemMouseover( IArcenUI_Dropdown_Option Item )
        {
          int method = (int)Item.GetItem();
          if(method == 1)
            {
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Connect planets via Relative Neighborhood Graph" );
            }
          if(method == 2)
            {
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Connect planets via Gabriel Graph" );
            }
          if(method == 3)
            {
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Connect planets via Spanning Tree Graph" );
            }
          else
            Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Bug: improper value in tooltip" );

        }

        public override void OnUpdate()
            {
              //I think this tells the code what to show when someone clicks on the dropdown menu
              //to change selections. So it has to print all the available options each iteration
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;
//                elementAsType.Items.Clear(); //I bet this list contains all the IArcenUI_Dropdown_Options
                
                //for dIntDropdown this for loop is necessary because it's run on the first
                //time this is displayed, and it populated the Items list which contains all the Resolutions
                //for the future, we just use the index into this array to figure out which resolution to show
                 if ( elementAsType.Items.Count <= 0 )
                  {
                      for ( int i = 1; i < 4; i++ )
                        {
  //                        ArcenDebugging.ArcenDebugLogSingleLine("adding simpleLinkMethod " + i, Verbosity.DoNotShow);
                          elementAsType.Items.Add( new SimpleLinkMethod( i ) );
                        }
                  }
                //I'm not sure what these two lines do in other dropdowns, but it works fine without them
//                 if ( setting.TempValue_Int != elementAsType.GetSelectedIndex() )
//                   elementAsType.SetSelectedIndex( setting.TempValue_Int );
                 
            }
        
        private class SimpleLinkMethod : IArcenUI_Dropdown_Option
        {
          //1 == rng (aka simple)
          //2 == gabriel (aka dreamcatcher)
          //3 == spanning tree (aka constellation)

          public int linkMethod;
          private string SavedName = string.Empty;

          public SimpleLinkMethod( int method )
          {
            this.linkMethod = method;
          }

          public object GetItem()
          {
            return linkMethod;
          }

          public string GetOptionName()
          {
            if ( SavedName.Length <= 0 )
              {
                if(linkMethod == 1)
                  SavedName = "Simple";
                else if(linkMethod == 2)
                  SavedName = "Dreamcatcher";
                else if(linkMethod == 3)
                  SavedName = "Constellation";
                else
                  SavedName = "Unknown: " + linkMethod; //this is a bug
              }
            return SavedName;
          }

          public Sprite GetOptionSprite()
          {
            return null;
          }
        }

      }
      public class SolarLinkDropdown : DropdownAbstractBase
      {
        //NOTES: it looks like IArcenUI_Dropdown_Option is an interface class
        //one must define GetItem(), GetOptionName() and so on

        public override void GetMainTextToShowPrefix( ArcenDoubleCharacterBuffer Buffer )
        {
          //this would appear before the name of any item in the dropdown
          //Like "Map Type: <dropdown Entry>"
        }

        public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item )
        {
          if ( Item == null )
            return;
          ArcenSetting setting = GetSettingForController( this );
          if ( setting == null ) return;

//          ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;

  //        int newValue = elementAsType.Items.IndexOf( Item );

          setting.TempValue_Int = (int)Item.GetItem(); //this returns the int value in Item (a SimpleLinkMethod)
          setting.TempValue_String = setting.TempValue_Int.ToString();
//          ArcenDebugging.ArcenDebugLogSingleLine("changed tempValue to " + setting.TempValue_Int, Verbosity.DoNotShow);
        }
        public override void HandleMouseover()
        {
          ArcenSetting setting = GetSettingForController( this );
          if ( setting == null ) return;
          if ( setting.Description.Length > 0 )
            Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
        }
        public override void HandleOverallMouseover() { }
        public override void HandleItemMouseover( IArcenUI_Dropdown_Option Item )
        {
          //This doesn't seem to do anything, but it's still useful as documentation
          int method = (int)Item.GetItem();
          if(method == 1)
            {
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Connect planets via Solar Systems method" );
            }
          else if(method == 2)
            {
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Connect planets via Random Neighborhood Graph" );
            }
          else if(method == 3)
            {
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Connect planets via Gabriel Graph" );
            }
          else if(method == 4)
            {
              Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Connect planets via Spanning Tree Graph" );
            }
          else
            Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "BUG: improper value" );

        }

        public override void OnUpdate()
            {
              //I think this tells the code what to show when someone clicks on the dropdown menu
              //to change selections. So it has to print all the available options each iteration
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;
//                elementAsType.Items.Clear(); //I bet this list contains all the IArcenUI_Dropdown_Options
                
                //for dIntDropdown this for loop is necessary because it's run on the first
                //time this is displayed, and it populated the Items list which contains all the Resolutions
                //for the future, we just use the index into this array to figure out which resolution to show
                 if ( elementAsType.Items.Count <= 0 )
                   {
                       for ( int i = 1; i < 5; i++ )
                         {
                           //ArcenDebugging.ArcenDebugLogSingleLine("adding simpleLinkMethod " + i, Verbosity.DoNotShow);
                           elementAsType.Items.Add( new SolarLinkMethod( i ) );
                         }
                   }
                //I'm not sure what these two lines do in other dropdowns, but it works fine without them
//                 if ( setting.TempValue_Int != elementAsType.GetSelectedIndex() )
//                   elementAsType.SetSelectedIndex( setting.TempValue_Int );
                 
            }
        
        private class SolarLinkMethod : IArcenUI_Dropdown_Option
        {
          //1 == normal (aka solar systems lite)
          //2 == gabriel (aka geode)
          //3 == RNG (aka geode2)
          //3 == spanning tree (aka haystack)

          public int linkMethod;
          private string SavedName = string.Empty;

          public SolarLinkMethod( int method )
          {
            this.linkMethod = method;
          }

          public object GetItem()
          {
            return linkMethod;
          }

          public string GetOptionName()
          {
            if ( SavedName.Length <= 0 )
              {
                if(linkMethod == 1)
                  SavedName = "Solar System";
                else if(linkMethod == 2)
                  SavedName = "Geode";
                else if(linkMethod == 3)
                  SavedName = "Spiderweb";
                else if(linkMethod == 4)
                  SavedName = "Haystack";

                else
                  SavedName = "Unknown: " + linkMethod; //this is a bug
              }
            return SavedName;
          }

          public Sprite GetOptionSprite()
          {
            return null;
          }
        }

      }
      public class LowMedHighDropdown : DropdownAbstractBase
      {
        //NOTES: it looks like IArcenUI_Dropdown_Option is an interface class
        //one must define GetItem(), GetOptionName() and so on

        public override void GetMainTextToShowPrefix( ArcenDoubleCharacterBuffer Buffer )
        {
          //this would appear before the name of any item in the dropdown
          //Like "Map Type: <dropdown Entry>"
        }

        public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item )
        {
          if ( Item == null )
            return;
          ArcenSetting setting = GetSettingForController( this );
          if ( setting == null ) return;

//          ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;

  //        int newValue = elementAsType.Items.IndexOf( Item );

          setting.TempValue_Int = (int)Item.GetItem(); //this returns the int value in Item (a SimpleLinkMethod)
          setting.TempValue_String = setting.TempValue_Int.ToString();
//          ArcenDebugging.ArcenDebugLogSingleLine("changed tempValue to " + setting.TempValue_Int, Verbosity.DoNotShow);
        }
        public override void HandleMouseover()
        {
          ArcenSetting setting = GetSettingForController( this );
          if ( setting == null ) return;
          if ( setting.Description.Length > 0 )
            Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
        }
        public override void HandleOverallMouseover() { }
        public override void HandleItemMouseover( IArcenUI_Dropdown_Option Item )
        {
          //This doesn't seem to do anything
        }

        public override void OnUpdate()
            {
              //I think this tells the code what to show when someone clicks on the dropdown menu
              //to change selections. So it has to print all the available options each iteration
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;
//                elementAsType.Items.Clear(); //I bet this list contains all the IArcenUI_Dropdown_Options
                
                //for dIntDropdown this for loop is necessary because it's run on the first
                //time this is displayed, and it populated the Items list which contains all the Resolutions
                //for the future, we just use the index into this array to figure out which resolution to show
                 if ( elementAsType.Items.Count <= 0 )
                   {
                       for ( int i = 1; i < 4; i++ )
                         {
//                           ArcenDebugging.ArcenDebugLogSingleLine("adding simpleLinkMethod " + i, Verbosity.DoNotShow);
                           elementAsType.Items.Add( new LowMedHigh( i ) );
                         }
                   }
                //I'm not sure what these two lines do in other dropdowns, but it works fine without them
//                 if ( setting.TempValue_Int != elementAsType.GetSelectedIndex() )
//                   elementAsType.SetSelectedIndex( setting.TempValue_Int );
                 
            }
        
        private class LowMedHigh : IArcenUI_Dropdown_Option
        {
          //1 == normal (aka solar systems lite)
          //2 == gabriel (aka geode)
          //3 == RNG (aka geode2)
          //3 == spanning tree (aka haystack)

          public int value;
          private string SavedName = string.Empty;

          public LowMedHigh( int val )
          {
            this.value = val;
          }

          public object GetItem()
          {
            return value;
          }

          public string GetOptionName()
          {
            if ( SavedName.Length <= 0 )
              {
                if(value == 1)
                  SavedName = "Low";
                else if(value == 2)
                  SavedName = "Medium";
                else if(value == 3)
                  SavedName = "High";
                else
                  SavedName = "Unknown: " + value; //this is a bug
              }
            return SavedName;
          }

          public Sprite GetOptionSprite()
          {
            return null;
          }
        }

      }
        public class dIntDropdown : DropdownAbstractBase
        {
            public override void GetMainTextToShowPrefix( ArcenDoubleCharacterBuffer Buffer )
            {
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item )
            {
                if ( Item == null )
                    return;
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;

                int newValue = elementAsType.Items.IndexOf( Item );
                setting.TempValue_Int = newValue;
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
            }

            public override void HandleOverallMouseover() { }

            public override void HandleItemMouseover( IArcenUI_Dropdown_Option Item ) { }

            public override void OnUpdate()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                //So it seems elementAsType is the thing that
                //controls the value being shown on the screen
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;

                
                if ( elementAsType.Items.Count > setting.TempValue_Int && setting.TempValue_Int != elementAsType.GetSelectedIndex() )
                    elementAsType.SetSelectedIndex( setting.TempValue_Int );
            }
        }

      

    
      public class dIntDropdownOctopus : DropdownAbstractBase
      {
        public override void GetMainTextToShowPrefix( ArcenDoubleCharacterBuffer Buffer )
        {
        }

        public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item )
        {
          if ( Item == null )
            return;
          ArcenSetting setting = GetSettingForController( this );
          if ( setting == null ) return;

          ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;

          int newValue = elementAsType.Items.IndexOf( Item );
          setting.TempValue_Int = newValue;
          setting.TempValue_String = setting.TempValue_Int.ToString();
        }

        public override void HandleMouseover()
        {
          ArcenSetting setting = GetSettingForController( this );
          if ( setting == null ) return;
          if ( setting.Description.Length > 0 )
            Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
        }

        public override void HandleOverallMouseover() { }

        public override void HandleItemMouseover( IArcenUI_Dropdown_Option Item ) { }

        public override void OnUpdate()
        {
          ArcenSetting setting = GetSettingForController( this );
          if ( setting == null ) return;

          //So it seems elementAsType is the thing that
          //controls the value being shown on the screen
          ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;
          if ( elementAsType.Items.Count <= 0 )
                   {
                       for ( int i = 0; i < 5; i++ )
                         {
//                           ArcenDebugging.ArcenDebugLogSingleLine("adding arm " + i, Verbosity.DoNotShow);
                           elementAsType.Items.Add( new OctopusNum( i ) );
                         }
                   }
        
          if ( elementAsType.Items.Count > setting.TempValue_Int && setting.TempValue_Int != elementAsType.GetSelectedIndex() )
            elementAsType.SetSelectedIndex( setting.TempValue_Int );
        }
        private class OctopusNum : IArcenUI_Dropdown_Option
        {
          //0 == default
          //any other number overrides the number of arms

          public int num;
          private string SavedName = string.Empty;

          public OctopusNum( int numArms )
          {
            this.num = numArms;
          }

          public object GetItem()
          {
            return num;
          }

          public string GetOptionName()
          {
            if ( SavedName.Length <= 0 )
              {
                if(num == 0)
                  SavedName = "Default";
                else
                  SavedName = num.ToString();
              }
            return SavedName;
          }
          public Sprite GetOptionSprite()
          {
            return null;
          }

        }
      }
      public class iIntInput : InputAbstractBase
        {
            public override void HandleChangeInValue( string NewValue )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                setting.TempValue_String = NewValue;
            }

            //public override char ValidateInput( string input, int charIndex, char addedChar )
            //{
            //    if ( input.Length >= 10 )
            //        return '\0';
            //    if ( !char.IsDigit( addedChar ) )
            //        return '\0';

            //    ArcenSetting setting = GetSettingForController( this );
            //    if ( setting == null ) return '\0';

            //    string testInput = input.Insert( charIndex, addedChar.ToString() );
            //    int testInputAsType;
            //    if ( !Int32.TryParse( testInput, out testInputAsType ) )
            //        return '\0';

            //    if ( testInputAsType < setting.MinIntValue )
            //        return '\0';

            //    if ( setting.MaxIntValue > setting.MinIntValue && testInputAsType > setting.MaxIntValue )
            //        return '\0';

            //    return addedChar;
            //}

            public override void OnUpdate()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null )
                  {
                    ArcenDebugging.ArcenDebugLogSingleLine("iIntInput: setting null", Verbosity.DoNotShow);
                    return;
                  }
                if(setting.TempValue_String == String.Empty)
                  {
                    setting.TempValue_String = setting.DefaultIntValue.ToString();
                  }

                ArcenUI_Input elementAsType = (ArcenUI_Input)Element;
                elementAsType.SetText( setting.TempValue_String );
                Int32.TryParse( setting.TempValue_String, out setting.TempValue_Int);
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
            }
        }

      public void printSetting(ArcenSetting setting)
            {
              //for debugging
              bool localDebug = true;
              if(setting != null)
                {
                  int index = setting.RowIndex;
                  if(localDebug)
                    ArcenDebugging.ArcenDebugLogSingleLine("Window_MapSetup: description <" + setting.Description + "> index " + index, Verbosity.DoNotShow);
                }
              else
                {
                  if(localDebug)
                    ArcenDebugging.ArcenDebugLogSingleLine("Window_MapSetup: setting is null", Verbosity.DoNotShow);
                }
            }
          public ArcenSetting getSettingByName(string settingName)
            {
              ArcenSetting setting = null;
              ArcenSparseLookup<string, ArcenSetting> settingMap =  ArcenSettingTable.Instance.LookupByName;
              if(settingMap.GetHasKey(settingName))
                {
                  setting = settingMap[settingName];
                }
              if(setting != null)
                {
                  int index = setting.RowIndex;
                }
              return setting;
            }

        public class tMapSettingsHeader : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
              MapTypeData mapType = Window_GameSetup.Instance.currentMapType;
              string mapName = "(null)";
              if(mapType != null)
                mapName = mapType.InternalName;
              buffer.Add( "Map Type Options for " + mapName);
            }
        }
        private static readonly string TEXT_PREFAB_NAME = "HoverableText";
        private static readonly string BUTTON_PREFAB_NAME = "ButtonBlue";
        private static readonly string INPUT_PREFAB_NAME = "BasicTextbox";
//        private static readonly string VERTICAL_SLIDER_PREFAB_NAME = "BasicVerticalSlider";
        private static readonly string HORIZONTAL_SLIDER_PREFAB_NAME = "BasicHorizontalSlider";
        private static readonly string DROPDOWN_PREFAB_NAME = "BasicDropdown";
      private static ArcenUI_CreateElementDirective _AddBase( string prefabName, ArcenUI_SetOfCreateElementDirectives Set, Type ControllerType, int CodeDirectiveTag, Rect rect )
        {
            return Set.GetNextFree( prefabName, ControllerType, CodeDirectiveTag, rect.x, rect.y, rect.width, rect.height );
        }

      private static ArcenUI_CreateElementDirective AddText( ArcenUI_SetOfCreateElementDirectives Set, Type ControllerType, int CodeDirectiveTag, Rect rect )
        {
            return _AddBase( TEXT_PREFAB_NAME, Set, ControllerType, CodeDirectiveTag, rect );
        }
      private static ArcenUI_CreateElementDirective AddInput( ArcenUI_SetOfCreateElementDirectives Set, Type ControllerType, int CodeDirectiveTag, Rect rect )
        {
            return _AddBase( INPUT_PREFAB_NAME, Set, ControllerType, CodeDirectiveTag, rect );
        }
      private static ArcenUI_CreateElementDirective AddButton( ArcenUI_SetOfCreateElementDirectives Set, Type ControllerType, int CodeDirectiveTag, Rect rect )
        {
            return _AddBase( BUTTON_PREFAB_NAME, Set, ControllerType, CodeDirectiveTag, rect );
        }
        private static ArcenUI_CreateElementDirective AddHorizontalSlider( ArcenUI_SetOfCreateElementDirectives Set, Type ControllerType, int CodeDirectiveTag, Rect rect )
        {
            return _AddBase( HORIZONTAL_SLIDER_PREFAB_NAME, Set, ControllerType, CodeDirectiveTag, rect );
        }

        private static ArcenUI_CreateElementDirective AddDropdown( ArcenUI_SetOfCreateElementDirectives Set, Type ControllerType, int CodeDirectiveTag, Rect rect )
        {
            return _AddBase( DROPDOWN_PREFAB_NAME, Set, ControllerType, CodeDirectiveTag, rect );
        }

      List<string> getSettingNamesForMap(string mapName)
        {
          //To add support for a new setting, one adds it to the XML, then add
          //the name here (under the if() for the right maps), then add the code to handle that
          //setting to the C# that builds the map
          List<string> names = new List<string>();
          names.Clear();
          if ( ArcenStrings.Equals( mapName, "Nebula") )
            {
              //names.Add("NumPlanets");
              //names.Add("NumGolems");
              names.Add("NumberOfNebulae");
              names.Add("NebulaeConnectivity");
              // names.Add("MinPlanetsPerNebulaRegion");
              // names.Add("MaxPlanetsPerNebulaRegion"); //more add others as necessary
              // names.Add("radiusPerRegionNebula");
              // names.Add("distanceBetweenNebulaRegion");
            }
          if ( ArcenStrings.Equals( mapName, "Simple") )
            {
              //names.Add("NumPlanets");
              //names.Add("NumGolems");
              names.Add("SimpleLinkMethod");
            }
          if ( ArcenStrings.Equals( mapName, "Octopus") )
            {
              //names.Add("NumPlanets");
              //names.Add("NumGolems");
              names.Add("OctopusNumArms");
            }

          if ( ArcenStrings.Equals( mapName, "Solar_Systems_Lite") )
            {
              //names.Add("NumPlanets");
              //names.Add("NumGolems");
              names.Add("SolarSystemsLinkMethod");
            }

          if ( ArcenStrings.Equals( mapName, "Asteroid") )
            {
              //names.Add("NumPlanets");
              //names.Add("NumGolems");
//              names.Add("MinPlanetsPerAsteroidRegion");
//              names.Add("MaxPlanetsPerAsteroidRegion"); //more add others as necessary
//              names.Add("radiusPerRegionAsteroid");
//              names.Add("distanceBetweenAsteroidRegion");
              names.Add("NumberOfAsteroids");
              names.Add("addBonusLinksAsteroid");
            }

          return names;
        }
    }

       
  // public class dLinkSelectorDropdown : DropdownAbstractBase
  // {
  //   //allows the user to choose between various planet/region linking mechanisms
  //   public override void GetMainTextToShowPrefix( ArcenDoubleCharacterBuffer Buffer )
  //   {
  //   }

  //   public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item )
  //   {
  //     if ( Item == null )
  //       return;
  //     ArcenSetting setting = GetSettingForController( this );
  //     if ( setting == null ) return;
     
  //     ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;
     
  //     int newValue = elementAsType.Items.IndexOf( Item );
  //     setting.TempValue_Int = newValue;
  //   }

  //   public override void HandleMouseover()
  //   {
  //     ArcenSetting setting = GetSettingForController( this );
  //     if ( setting == null ) return;
  //     if ( setting.Description.Length > 0 )
  //       Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
  //   }
   
  //   public override void HandleOverallMouseover() { }
   
  //   public override void HandleItemMouseover( IArcenUI_Dropdown_Option Item ) { }

  //   public override void OnUpdate()
  //   {
  //     ArcenSetting setting = GetSettingForController( this );
  //     if ( setting == null ) return;
     
  //     ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;

  //     if ( elementAsType.Items.Count <= 0 )
  //       {
  //         for ( int i = 0; i < ArcenUI.SupportedResolutions.Count; i++ )
  //           elementAsType.Items.Add( new ResolutionOption( ArcenUI.SupportedResolutions[i] ) );
  //       }

  //     if ( elementAsType.Items.Count > setting.TempValue_Int && setting.TempValue_Int != elementAsType.GetSelectedIndex() )
  //       elementAsType.SetSelectedIndex( setting.TempValue_Int );
  //   }

  // }

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
