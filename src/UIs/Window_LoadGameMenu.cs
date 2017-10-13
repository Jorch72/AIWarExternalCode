using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_LoadGameMenu : WindowControllerAbstractBase
    {
        /* This window has 2 screens , the Campaign screen where one chooses a campaign
           to look at saved games from, and the Load screen where one actually loads a game.
           The code checks the Window_LoadGameMenu.Instance.showCampaignButtons variable to see which
           mode you are in. */
        public static Window_LoadGameMenu Instance;
        public Window_LoadGameMenu()
        {
            Instance = this;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
        }

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

        private bool IsOpen;
        private bool HasUpdatedSinceLastClose;
        private bool showCampaignButtons;
        private string campaignName; //which campaign we are showing
        public void Open()
        {
            if ( this.IsOpen )
                return;
            this.IsOpen = true;
            this.showCampaignButtons = true;
            this.campaignName = "";
        }

        public void Close()
        {
            if ( !this.IsOpen )
                return;
            this.IsOpen = false;
            this.HasUpdatedSinceLastClose = false;
            this.showCampaignButtons = true;
            this.campaignName = "";
        }

        public class bClose : ButtonAbstractBase
        {
            //Note that the Close button becomes a "back" button
            //depending on whether we are in the Campaign or Load screen

            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( !Window_LoadGameMenu.Instance.showCampaignButtons )
                {
                    base.GetTextToShow( Buffer );
                    Buffer.Add( "Back to Campaign" );
                }
                else
                {
                    base.GetTextToShow( Buffer );
                    Buffer.Add( "Close" );
                }
            }
            public override MouseHandlingResult HandleClick()
            {
                if ( Window_LoadGameMenu.Instance.showCampaignButtons )
                    Instance.Close();
                else
                {
                    Window_LoadGameMenu.Instance.showCampaignButtons = true;
                    Window_LoadGameMenu.Instance.HasUpdatedSinceLastClose = false;
                    Window_LoadGameMenu.Instance.campaignName = "";
                }
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class tLoadHeader : TextAbstractBase
        {
            /* Prints out different header depending on whether we are in
               the Campaign or Load screen */
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Window_LoadGameMenu.Instance.showCampaignButtons )
                {
                    Buffer.Add( "Click on a campaign to see the save games" );
                }
                else
                {
                    Buffer.Add( "Click a saved game to load from campaign " );
                    Buffer.Add( Window_LoadGameMenu.Instance.campaignName );
                }

            }
            public override void OnUpdate() { }
        }

        public class bsLoadGameButtons : ButtonSetAbstractBase
        {
            /* These buttons are either Campaign Buttons or Load buttons.
               I tried 2 different classes, but I couldn't figure out how
               to make the old buttons disappear once I changed screens.
               This way works though */
            public override void OnUpdate()
            {
                if ( Instance.HasUpdatedSinceLastClose )
                    return;
                bool debug = false;
                Instance.HasUpdatedSinceLastClose = true;

                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                elementAsType.ClearButtons();

                Dictionary<string, List<SaveGameData>> gameDict = SaveLoadMethods.parseOnDiskSaveGames();
                if ( !Instance.showCampaignButtons )
                {
                    //these are Load Game buttons
                    if ( Window_LoadGameMenu.Instance.campaignName == "" )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "WARNING: campaign name is null ", Verbosity.DoNotShow );
                    }
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Showing saved games in LoadGames from campaign " + Window_LoadGameMenu.Instance.campaignName, Verbosity.DoNotShow );
                    //get the saved games for this campaign
                    List<SaveGameData> list = gameDict[Window_LoadGameMenu.Instance.campaignName];
                    //sort saved games by elapsed in game time
                    list.Sort( delegate ( SaveGameData x, SaveGameData y )
                    {
                        return ( -x.secondsSinceGameStart.CompareTo( y.secondsSinceGameStart ) );
                    } );

                    //This code allows for multiple columns to automatically wrap
                    int maxHeightPerColumn = 80;
                    int xModForLoadButtons = -4; //Load buttons are a bit smaller than campaign buttons
                    int yModForLoadButtons = -4;
                    int gamesPerColumn = (int)maxHeightPerColumn / (int)( elementAsType.ButtonHeight + yModForLoadButtons );
                    int distBetweenColumns = 2;
                    Vector2 sizeForLoadButtons;
                    sizeForLoadButtons.x = elementAsType.ButtonWidth + xModForLoadButtons;
                    sizeForLoadButtons.y = elementAsType.ButtonHeight + yModForLoadButtons;
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "gamesPerColumn " + gamesPerColumn + "botton  height " + elementAsType.ButtonHeight + "maxHeight " + maxHeightPerColumn, Verbosity.DoNotShow );
                    for ( int k = 0; k < list.Count; k++ )
                    {
                        Vector2 offset;
                        offset.y = ( k % gamesPerColumn ) * ( elementAsType.ButtonHeight + yModForLoadButtons );
                        offset.x = ( k / gamesPerColumn ) * ( elementAsType.ButtonWidth + xModForLoadButtons ) + distBetweenColumns * ( k / gamesPerColumn );
                        AddLoadButton( elementAsType, list[k], offset, sizeForLoadButtons );
                    }
                }
                else
                {
                    //these are campaign buttons
                    List<SaveGameData> campaignList = new List<SaveGameData>();
                    foreach ( KeyValuePair<string, List<SaveGameData>> entry in gameDict )
                    {
                        //Campaign buttons are sorted by "Last save Wall Clock date"
                        //Find the furthest-in game from each campaign to check for the
                        //Wall Clock time and add it to the campaignList
                        List<SaveGameData> list = entry.Value;
                        list.Sort( delegate ( SaveGameData x, SaveGameData y )
                        {
                            return ( -x.secondsSinceGameStart.CompareTo( y.secondsSinceGameStart ) );
                        } );
                        campaignList.Add( list[0] );
                    }
                    //Now sort by Wall Clock
                    campaignList.Sort( delegate ( SaveGameData x, SaveGameData y )
                    {
                        return ( -x.lastModified.CompareTo( y.lastModified ) );
                    } );

                    //Allow columns to wrap nicely
                    int maxHeightPerColumn = 80;
                    int gamesPerColumn = (int)maxHeightPerColumn / (int)elementAsType.ButtonHeight;
                    int distBetweenColumns = 2;
                    for ( int k = 0; k < campaignList.Count; k++ )
                    {
                        Vector2 offset;
                        offset.y = ( k % gamesPerColumn ) * elementAsType.ButtonHeight;
                        offset.x = ( k / gamesPerColumn ) * elementAsType.ButtonWidth + distBetweenColumns * ( k / gamesPerColumn );
                        AddCampaignButton( elementAsType, campaignList[k], offset );
                    }
                }
                elementAsType.ActuallyPutItemsBackInPoolThatAreStillCleared();
            }
            private static void AddLoadButton( ArcenUI_ButtonSet elementAsType, SaveGameData saveGame, Vector2 offset, Vector2 size )
            {
                //Note we use a slightly different sized button for Load games as opposed to Campaign games
                //so we set the size elsewhere. We need to set the size earlier so we can correctly
                //know the changed button size when laying out the grid of games on screen
                bLoadGameButton newButtonController = new bLoadGameButton( saveGame );
                elementAsType.AddButton( newButtonController, size, offset );
            }
            private static void AddCampaignButton( ArcenUI_ButtonSet elementAsType, SaveGameData saveGame, Vector2 offset )
            {
                bCampaignGameButton newButtonController = new bCampaignGameButton( saveGame );
                Vector2 size;
                size.x = elementAsType.ButtonWidth;
                size.y = elementAsType.ButtonHeight;
                elementAsType.AddButton( newButtonController, size, offset );
            }

        }

        private class bLoadGameButton : ButtonAbstractBase
        {
            public SaveGameData SaveGame;
            public string SaveName = string.Empty;

            public bLoadGameButton( SaveGameData saveGame )
            {
                this.SaveGame = saveGame;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );

                buffer.Add( SaveGame.saveName );
                buffer.Add( "\n In Game Time : " );
                buffer.Add( Engine_Universal.ToHoursAndMinutesString( SaveGame.secondsSinceGameStart ) );
            }

            public override MouseHandlingResult HandleClick()
            {
                bool debug = false;
                string SaveName = this.SaveGame.ToString();
                string oldSaveName = this.SaveGame.saveName;
                string path = Engine_Universal.CurrentPlayerDataDirectory + "Save/" + SaveName + Engine_Universal.SaveExtension;
                string oldPath = Engine_Universal.CurrentPlayerDataDirectory + "Save/" + oldSaveName + Engine_Universal.SaveExtension;
                //if ( path.Contains( " " ) )
                //{
                //    path = "\"" + path + "\"";
                //}
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Trying to load from " + path, Verbosity.DoNotShow );
                if ( File.Exists( path ) )
                {
                    SFXItemTable.TryPlayItemByName_GUIOnly( "ButtonStartGame" );
                    Window_SaveGameMenu.Instance.OverallCampaignName = this.SaveGame.campaignName;
                    Engine_Universal.LoadGame( SaveName );
                    Instance.Close();
                }
                else if ( File.Exists( oldPath ) )
                {
                    SFXItemTable.TryPlayItemByName_GUIOnly( "ButtonStartGame" );
                    Window_SaveGameMenu.Instance.OverallCampaignName = this.SaveGame.campaignName;
                    Engine_Universal.LoadGame( oldSaveName );
                    Instance.Close();
                }
                else
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "File does not exist" + Environment.NewLine + "path=" + path + Environment.NewLine + "oldPath=" + oldPath, Verbosity.DoNotShow );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                return MouseHandlingResult.DoNotPlayClickSound;
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }
        private class bCampaignGameButton : ButtonAbstractBase
        {
            public SaveGameData SaveGame;
            public bCampaignGameButton( SaveGameData saveGame )
            {
                this.SaveGame = saveGame;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                buffer.Add( SaveGame.campaignName );
                buffer.Add( "\n Map: " );
                buffer.Add( SaveGame.mapType );

                buffer.Add( "\n" );
                buffer.Add( SaveGame.difficulty );
                buffer.Add( " / " );
                buffer.Add( SaveGame.masterAIType );

                buffer.Add( "\n ElapsedTime: " );
                buffer.Add( Engine_Universal.ToHoursAndMinutesString( SaveGame.secondsSinceGameStart ) );
                buffer.Add( "\n " );
                buffer.Add( SaveGame.lastModified.ToString() );
            }

            public override MouseHandlingResult HandleClick()
            {
                Instance.showCampaignButtons = false;
                Instance.HasUpdatedSinceLastClose = false;
                Instance.campaignName = SaveGame.campaignName;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }
    }
}
