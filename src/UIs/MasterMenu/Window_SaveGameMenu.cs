using System.Linq;
using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Arcen.AIW2.External
{
    /* We store the information about any given saved game
       in one of these structures. The ToString() method is
       used to set the save game name on disk, and there's an
       alternate constructor to parse ToString() output */
    public struct SaveGameData
    {
        public int seed; //If we insist that the user
                         //really provide a campaign name,
                         //the seed isn't really necessary
        public int secondsSinceGameStart;
        public string mapType;
        public string mapTypeShort;
        public string campaignName;
        public string saveName;
        public string masterAIType;
        public string difficulty;
        public DateTime lastModified;
        public char metadataStart;
        public char metadataDelim;
        bool debug;
        public SaveGameData( string saveName, int seed, int secondsSinceGameStart, string campaignName, DateTime dt, string masterAIType,
                            string difficulty )
        {
            debug = false;
            metadataStart = '~';
            metadataDelim = '#';
            this.mapType = ""; //to set the map and mapTypeShort, we call setFullMapType() or setShortMapType()
            this.mapTypeShort = "";
            this.saveName = saveName;
            this.seed = seed;
            this.secondsSinceGameStart = secondsSinceGameStart;
            this.campaignName = campaignName;
            this.lastModified = dt;
            this.masterAIType = masterAIType;
            this.difficulty = difficulty;
        }
        public SaveGameData( string fullSaveName, DateTime dt )
        {
            //This is for a save file name (ie it has metadata encoded in it). Parse it and populate the struct
            debug = false;
            metadataStart = '~';
            metadataDelim = '#';


            mapType = "Unknown";
            mapTypeShort = "UK";
            seed = -1;
            secondsSinceGameStart = -1;
            campaignName = "Unknown";
            masterAIType = "";
            difficulty = "";
            this.lastModified = dt;
            string[] splitNameAndMeta = fullSaveName.Split( metadataStart );
            this.saveName = splitNameAndMeta[0];
            if ( splitNameAndMeta.Length > 1 )
            {
                //if there is metadata, parse it now
                string[] tokens = ( splitNameAndMeta[1] ).Split( metadataDelim );
                if ( debug )
                {
                    string s = "numTokens: " + tokens.Length + " --> " + splitNameAndMeta[1];
                    ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                }
                for ( int i = 0; i < tokens.Length; i++ )
                {
                    if ( debug )
                    {
                        string s;
                        s = i + " :: " + tokens[i];
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }
                    //the first token should be a ~, which marks the end of the save name
                    //if that doesn't exist,
                    switch ( i )
                    {
                        case 0:
                            break;
                        case 1:
                            this.setFullMapType( tokens[i] );
                            break;
                        case 2:
                            this.seed = Convert.ToInt32( tokens[i] );
                            break;
                        case 3:
                            this.secondsSinceGameStart = Convert.ToInt32( tokens[i] );
                            break;
                        case 4:
                            this.campaignName = tokens[i];
                            break;
                        case 5:
                            this.masterAIType = tokens[i];
                            break;
                        case 6:
                            this.difficulty = tokens[i];
                            break;
                        default:
                            ArcenDebugging.ArcenDebugLogSingleLine( "BUG: too many tokens in SaveGameData constructor; next was " + tokens[i], Verbosity.DoNotShow );
                            break;
                    }
                }
                if ( this.campaignName == "" )
                {
                    this.campaignName = this.mapType + "." + this.seed; //the # indicates that the campaign type was not set by the user
                }

            }
        }
        public void setFullMapType( string shortMapType )
        {
            //Translates the shortType (what's part of the file name)
            //into the full name
            this.mapTypeShort = shortMapType;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "setting full map type based on short  " + shortMapType, Verbosity.DoNotShow );
            if ( shortMapType == "CL" )
                mapType = "Cluster";
            else if ( shortMapType == "LT" )
                mapType = "Lattice";
            else if ( shortMapType == "CO" )
                mapType = "Concentric";
            else if ( shortMapType == "SP" )
                mapType = "Spiral";
            else if ( shortMapType == "X0" )
                mapType = "X";
            else if ( shortMapType == "GR" )
                mapType = "Grid";
            else if ( shortMapType == "CR" )
                mapType = "Crosshatch";
            else if ( shortMapType == "MA" )
                mapType = "MazeA";
            else if ( shortMapType == "MB" )
                mapType = "MazeB";
            else if ( shortMapType == "MC" )
                mapType = "MazeC";
            else if ( shortMapType == "MD" )
                mapType = "MazeD";
            else if ( shortMapType == "CM" )
                mapType = "ClustersMicrocosm";
            else if ( shortMapType == "WH" )
                mapType = "Wheel";
            else if ( shortMapType == "HO" )
                mapType = "Honeycomb";
            else if ( shortMapType == "EN" )
                mapType = "Encapsulated";
            else if ( shortMapType == "NB" )
                mapType = "Nebula";
            else if ( shortMapType == "GE" )
                mapType = "Geode";
            else if ( shortMapType == "HA" )
                mapType = "Haystack";
            else if ( shortMapType == "DC" )
                mapType = "DreamCatcher";
            else if ( shortMapType == "SI" )
                mapType = "Simple";
            else if ( shortMapType == "CN" )
                mapType = "Constellation";
            else if ( shortMapType == "OC" )
                mapType = "Octopus";
            else if ( shortMapType == "CI" )
                mapType = "ClustersMini";
            else if ( shortMapType == "SS" )
                mapType = "Solar_Systems_Lite";
            else
                mapType = "Unknown";
        }
        public void setShortMapType( string mapType )
        {
            //transforms the full map type name into a
            //shortened version
            this.mapType = mapType;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "setting short map type based on short  " + mapType, Verbosity.DoNotShow );
            if ( mapType == "Clusters" )
                mapTypeShort = "CL";
            else if ( mapType == "Lattice" )
                mapTypeShort = "LT";
            else if ( mapType == "Concentric" )
                mapTypeShort = "CO";
            else if ( mapType == "Snake" )
                mapTypeShort = "SP";
            else if ( mapType == "X" )
                mapTypeShort = "X0";
            else if ( mapType == "Grid" )
                mapTypeShort = "GR";
            else if ( mapType == "Crosshatch" )
                mapTypeShort = "CR";
            else if ( mapType == "MazeA" )
                mapTypeShort = "MA";
            else if ( mapType == "MazeB" )
                mapTypeShort = "MB";
            else if ( mapType == "MazeC" )
                mapTypeShort = "MC";
            else if ( mapType == "MazeD" )
                mapTypeShort = "MD";
            else if ( mapType == "ClustersMicrocosm" )
                mapTypeShort = "CM";
            else if ( mapType == "Wheel" )
                mapTypeShort = "WH";
            else if ( mapType == "Honeycomb" )
                mapTypeShort = "HO";
            else if ( mapType == "Encapsulated" )
                mapTypeShort = "EN";
            else if ( mapType == "Nebula" )
                mapTypeShort = "NB";
            else if ( mapType == "Geode" )
                mapTypeShort = "GE";
            else if ( mapType == "Haystack" )
                mapTypeShort = "HA";
            else if ( mapType == "DreamCatcher" )
                mapTypeShort = "DC";
            else if ( mapType == "Simple" )
                mapTypeShort = "SI";
            else if ( mapType == "Constellation" )
                mapTypeShort = "CN";
            else if ( mapType == "Octopus" )
                mapTypeShort = "OC";
            else if ( mapType == "ClustersMini" )
                mapTypeShort = "CI";
            else if ( mapType == "Solar_Systems_Lite" )
                mapTypeShort = "SS";
            else
                mapTypeShort = "UK"; //unknown
        }
        public override String ToString()
        {
            //This generates the output that becomes
            //the filename for the save game
            string output = saveName;
            output += metadataStart;
            output += metadataDelim;
            output += mapTypeShort;
            output += metadataDelim;
            output += seed;
            output += metadataDelim;
            output += secondsSinceGameStart;
            output += metadataDelim;
            output += campaignName;
            output += metadataDelim;
            output += masterAIType;
            output += metadataDelim;
            output += difficulty;
            return output;
        }
    }

    internal static class SaveLoadMethods
    {
        /* This is shared between the SaveGameMenu and LoadGameMenu
           classes, hence having its own class. This function will
           read all the on-disk data and store it in a Dictionary
           whose keys are the campaign names, and whose value is
           a list of all Saved Games from that campaign */
        internal static Dictionary<string, List<SaveGameData>> parseOnDiskSaveGames()
        {
            bool debug = false;
            Dictionary<string, List<SaveGameData>> gameDict = new Dictionary<string, List<SaveGameData>>(); //this maps from a campaignName to the list of save games for that campaign
            string directoryPath = Engine_Universal.CurrentPlayerDataDirectory + "Save/";
            string[] files = Directory.GetFiles( directoryPath, "*" + Engine_Universal.SaveExtension );
            string[] fullSaveNames = new string[files.Length];
            for ( int i = 0; i < files.Length; i++ )
            {
                string file = files[i];

                string fullSaveName = Path.GetFileNameWithoutExtension( file );
                fullSaveNames[i] = fullSaveName;
                DateTime dt = File.GetLastWriteTime( file );
                SaveGameData saveGame = new SaveGameData( fullSaveName, dt );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Parsing save game " + i + " of " + files.Length + " --> " + saveGame.ToString() + "And adding to campaign " + saveGame.campaignName, Verbosity.DoNotShow );
                List<SaveGameData> list;
                if ( !gameDict.ContainsKey( saveGame.campaignName ) )
                {
                    //If this is the first save file seen from a given campaign,
                    //create the list
                    list = new List<SaveGameData>();
                    list.Add( saveGame );
                    gameDict[saveGame.campaignName] = list;
                }
                else
                {
                    //Add a saved game to a pre-existing campaign
                    gameDict[saveGame.campaignName].Add( saveGame );
                }
            }
            return gameDict;
        }
    }

    public class Window_SaveGameMenu : ToggleableWindowController
    {
        public static Window_SaveGameMenu Instance;
        //We use OverallCampaignName
        //so we can automatically fill in the campaign name
        //in the save game screen.
        //We reset it to "" in several extra places when closing/opening a game
        public string OverallCampaignName;
        public bool debug;
        public Window_SaveGameMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.OverallCampaignName = "";
            this.debug = false;
        }

        private bool NeedsUpdate;
        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( World_AIW2.Instance.InSetupPhase )
                return false;
            if ( !this.IsOpen )
                return false;
            return true;
        }

        private static string getCampaignLabel( SaveGameData SaveGame )
        {
            //used to print the Campaign information at the top of the Save Game screen
            string buffer = "";
            buffer += ( SaveGame.campaignName );
            buffer += ( "\n Map: " );
            buffer += ( SaveGame.mapType );

            buffer += ( "\n" );
            buffer += ( SaveGame.difficulty );
            buffer += ( " / " );
            buffer += ( SaveGame.masterAIType );

            return buffer;
        }
        public override void OnOpen()
        {
            this.NeedsUpdate = true;
            
            iCampaignName.Instance.CampaignName = Window_SaveGameMenu.Instance.OverallCampaignName;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Overall campaign is: " + Window_SaveGameMenu.Instance.OverallCampaignName, Verbosity.DoNotShow );
            //pause the game once we enter the save game menu. I think this is a nice
            //quality of life improvement for the player. If the game is already paused,
            //do nothing
            if ( !World_AIW2.Instance.IsPaused )
            {
                GameCommand command = GameCommand.Create( GameCommandType.TogglePause );
                World_AIW2.Instance.QueueGameCommand( command, true );
            }
        }

        public class bClose : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Close" );
            }
            public override MouseHandlingResult HandleClick()
            {
                if ( World_AIW2.Instance.IsPaused )
                {
                    //unpause on leaving save game menu. Note that the player
                    //can unpause the game deliberately while in the save game menu (with a hotkey)
                    //so make sure we only unpause if the game is currently paused
                    GameCommand command = GameCommand.Create( GameCommandType.TogglePause );
                    World_AIW2.Instance.QueueGameCommand( command, true );
                }
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bsSaveGameButtons : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                if ( !Instance.NeedsUpdate )
                    return;
                Instance.NeedsUpdate = false;
                bool debug = false;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Reading directory to generate buttons", Verbosity.DoNotShow );
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                elementAsType.ClearButtons();

                Dictionary<string, List<SaveGameData>> gameDict = SaveLoadMethods.parseOnDiskSaveGames();
                foreach ( KeyValuePair<string, List<SaveGameData>> entry in gameDict )
                {
                    //Don't show any saves from other campaigns
                    if ( entry.Key != Window_SaveGameMenu.Instance.OverallCampaignName || Window_SaveGameMenu.Instance.OverallCampaignName == "" )
                        continue;
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Parsing list from  " + entry.Key, Verbosity.DoNotShow );

                    List<SaveGameData> list = entry.Value;
                    //Sort the found saved games by elapsed in game time
                    list.Sort( delegate ( SaveGameData x, SaveGameData y )
                    {
                        return ( -x.secondsSinceGameStart.CompareTo( y.secondsSinceGameStart ) );
                    } );

                    //Allow for multiple columns
                    int maxHeightPerColumn = 80;
                    int gamesPerColumn = (int)maxHeightPerColumn / (int)elementAsType.ButtonHeight;
                    int distBetweenColumns = 5;

                    //This section here adds a big button at the top
                    //to display information about the campaign
                    Vector2 offset;
                    offset.y = 0;
                    offset.x = 40;
                    string output = getCampaignLabel( list[0] );
                    AddHeaderButton( elementAsType, output, offset );

                    int offsetForHeader = (int)elementAsType.ButtonHeight + 5;
                    //Now print the found saved games underneath the header
                    for ( int k = 0; k < list.Count; k++ )
                    {
                        offset.y = ( k % gamesPerColumn ) * elementAsType.ButtonHeight + offsetForHeader;
                        offset.x = ( k / gamesPerColumn ) * elementAsType.ButtonWidth + distBetweenColumns * ( k / gamesPerColumn );
                        AddSaveButton( elementAsType, list[k], offset );
                    }
                }
                elementAsType.ActuallyPutItemsBackInPoolThatAreStillCleared();
            }
            private static void AddSaveButton( ArcenUI_ButtonSet elementAsType, SaveGameData saveGame, Vector2 offset )
            {
                //This adds a given save game
                bSaveGameButton newButtonController = new bSaveGameButton( saveGame );
                Vector2 size;
                size.x = elementAsType.ButtonWidth;
                size.y = elementAsType.ButtonHeight;
                elementAsType.AddButton( newButtonController, size, offset );
            }

            private static void AddHeaderButton( ArcenUI_ButtonSet elementAsType, String Name, Vector2 offset )
            {
                /* This is for the campaign header */
                bSaveGameButton newButtonController = new bSaveGameButton( Name );
                newButtonController.doNothing = true; //this is a button, but don't do anything when its pressed
                Vector2 size;
                size.x = elementAsType.ButtonWidth + 2; //make it a bit larger
                size.y = elementAsType.ButtonHeight + 3;
                elementAsType.AddButton( newButtonController, size, offset );
            }
        }

        private class bSaveGameButton : ButtonAbstractBase
        {
            //Since the campaign header is also a button, detect that case
            //and don't do anything if the button is clicked
            public bool doNothing;
            public string SaveName = string.Empty;
            public SaveGameData saveGameDataName;
            public bSaveGameButton( string Filename )
            {
                this.SaveName = Filename;
                this.doNothing = false;
            }
            public bSaveGameButton( SaveGameData saveGame )
            {
                this.saveGameDataName = saveGame;
            }
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                if ( doNothing )
                {
                    base.GetTextToShow( buffer );
                    buffer.Add( SaveName );
                }
                else
                {
                    base.GetTextToShow( buffer );
                    buffer.Add( saveGameDataName.saveName );
                    buffer.Add( "\n In Game Time : " );
                    buffer.Add( Engine_Universal.ToHoursAndMinutesString( saveGameDataName.secondsSinceGameStart ) );
                }
            }

            public override MouseHandlingResult HandleClick()
            {
                if ( doNothing )
                    return MouseHandlingResult.PlayClickDeniedSound;
                //then you have to click the Save Game button to actually do the save
                iSaveGameName.Instance.SaveName = this.SaveName;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }
        public class iSaveGameName : InputAbstractBase
        {
            //This is the box where someone types in the saved game name
            public int maxSaveLen = 35;
            public static iSaveGameName Instance;
            public iSaveGameName() { Instance = this; }
            public string SaveName = string.Empty;
            public string CurrentValue = string.Empty;
            public override void HandleChangeInValue( string NewValue )
            {
                this.SaveName = NewValue;
            }
            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= maxSaveLen )
                    return '\0';
                //use a whitelist of approved characters only
                if ( Char.IsLetterOrDigit( addedChar ) ) //must be alphanumeric
                    return addedChar;
                if ( addedChar == '_' ) // || addedChar == ' ')
                    return addedChar;
                //Other things could be allowed later I suppose, but I went with
                //"simple" for now
                return '\0';
            }
            public override void OnUpdate()
            {
                ArcenUI_Input elementAsType = (ArcenUI_Input)Element;
                elementAsType.SetText( SaveName );
            }
        }
        public class iCampaignName : InputAbstractBase
        {
            //this is the box where one types in the campaign name
            public int maxCampaignLen = 20;
            public static iCampaignName Instance;
            public iCampaignName() { Instance = this; }
            public string CampaignName = Window_SaveGameMenu.Instance.OverallCampaignName;
            public string CurrentValue = string.Empty;
            public override void HandleChangeInValue( string NewValue )
            {
                this.CampaignName = NewValue;
            }
            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= maxCampaignLen )
                    return '\0';
                //use a whitelist of approved characters only
                if ( Char.IsLetterOrDigit( addedChar ) ) //must be alphanumeric
                    return addedChar;
                if ( addedChar == '_' )
                    return addedChar;
                //block everything except alphanumerics and _
                //for right now
                return '\0';
            }
            public override void OnUpdate()
            {
                ArcenUI_Input elementAsType = (ArcenUI_Input)Element;
                elementAsType.SetText( CampaignName );
            }
        }

        public class bSaveGameName : ButtonAbstractBase
        {
            //This button causes the save to happen
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Save Game" );
            }
            public override MouseHandlingResult HandleClick()
            {
                if ( iCampaignName.Instance.CampaignName.Trim().Length <= 0 )
                    iCampaignName.Instance.CampaignName = World_AIW2.Instance.Setup.MapType.InternalName + "." + World_AIW2.Instance.Setup.Seed;
                Window_SaveGameMenu.Instance.OverallCampaignName = iCampaignName.Instance.CampaignName;
                GameCommand command = GameCommand.Create( GameCommandType.SaveGame );
                //generate the saveGame entry from the name and state of the game
                DateTime dt = DateTime.Now;
                //Generate a SaveGameData from the saveGame and campaignName boxes,
                //along with game metadata
                SaveGameData data = new SaveGameData( iSaveGameName.Instance.SaveName, World_AIW2.Instance.Setup.Seed,
                                                       World_AIW2.Instance.GameSecond, iCampaignName.Instance.CampaignName,
                                                       dt, World_AIW2.Instance.Setup.MasterAIType.Name, World_AIW2.Instance.Setup.Difficulty.Name );

                data.setShortMapType( World_AIW2.Instance.Setup.MapType.InternalName );
                command.RelatedString = data.ToString();
                World_AIW2.Instance.QueueGameCommand( command, true );
                if ( World_AIW2.Instance.IsPaused )
                {
                    //unpause on leaving save game menu. Note that the player
                    //can unpause the game deliberately while in the save game menu (with a hotkey)
                    //so make sure we only unpause if the game is currently paused
                    command = GameCommand.Create( GameCommandType.TogglePause );
                    World_AIW2.Instance.QueueGameCommand( command, true );
                }

                Window_SaveGameMenu.Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }


        }
        public class tSaveHeader : TextAbstractBase
        {

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "Save Game:" );
            }
            public override void OnUpdate()
            {
            }

        }
        public class tCampaignHeader : TextAbstractBase
        {

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "Campaign Name:" );
            }
            public override void OnUpdate()
            {
            }

        }

        public class bSaveAndQuit : ButtonAbstractBase
        {
            //This button is like Save, but it also quits the game                                                                                                  
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Save And Quit" );
            }
            public override MouseHandlingResult HandleClick()
            {
                //debug = true;
                if ( iCampaignName.Instance.CampaignName.Trim().Length <= 0 )
                    iCampaignName.Instance.CampaignName = World_AIW2.Instance.Setup.MapType.InternalName + "." + World_AIW2.Instance.Setup.Seed;
                Window_SaveGameMenu.Instance.OverallCampaignName = iCampaignName.Instance.CampaignName;
                GameCommand command = GameCommand.Create( GameCommandType.SaveGame );
                //generate the saveGame entry from the name and state of the game                                                                                   
                DateTime dt = DateTime.Now;
                //Generate a SaveGameData from the saveGame and campaignName boxes,                                                                                 
                //along with game metadata                                                                                                                          
                SaveGameData data = new SaveGameData( iSaveGameName.Instance.SaveName, World_AIW2.Instance.Setup.Seed,
                                                       World_AIW2.Instance.GameSecond, iCampaignName.Instance.CampaignName,
                                                       dt, World_AIW2.Instance.Setup.MasterAIType.Name, World_AIW2.Instance.Setup.Difficulty.Name );

                data.setShortMapType( World_AIW2.Instance.Setup.MapType.InternalName );
                command.RelatedString = data.ToString();
                command.RelatedBool = true; // tells it to quit after completing the save
                World_AIW2.Instance.QueueGameCommand( command, true );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }


        }
    }
}
