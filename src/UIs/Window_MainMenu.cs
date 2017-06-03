using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_MainMenu : WindowControllerAbstractBase
    {
        public static Window_MainMenu Instance;
        public Window_MainMenu()
        {
            Instance = this;
        }

        public string TargetIP = string.Empty;

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( World.Instance.IsLoaded )
                return false;
            if ( World_AIW2.Instance.InSetupPhase )
                return false;
            return true;
        }

        public class bStartGame : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Start New Game" );
            }
            public override void HandleClick() { Input_MainHandler.HandleInner( 0, "DebugGenerateMap" ); }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bConnectToServer : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Debug: Connect To " ).Add( Instance.TargetIP ).Add( "\n" ).Add( "(change in PlayerData/AlphaMPTarget.txt)" );
            }
            public override void HandleClick() { Input_MainHandler.HandleInner( 0, "DebugConnectToLocalServer" ); }
            public override void HandleMouseover() { }
            public override void OnUpdate()
            {
                string filename = "AlphaMPTarget.txt";
                string path = Engine_Universal.CurrentPlayerDataDirectory + filename;
                if ( !File.Exists( path ) )
                    return;
                string rawText = File.ReadAllText( path );
                Instance.TargetIP = rawText.Trim();
            }
        }

        public class bLoadGame : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Load Game" );
            }
            public override void HandleClick() { Window_LoadGameMenu.Instance.Open(); }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bOpenReleaseNotes : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Open Release Notes\n(In External Browser)" );
            }
            public override void HandleClick() { Process.Start( "https://wiki.arcengames.com/index.php?title=AI_War_2:AI_War_2#Release_History" ); }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bExitApplication : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Exit Game" );
            }
            public override void HandleClick() { Engine_Universal.ForceClose(); }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bTextWithInlineImagesTest : IArcenUI_TextWithInlineImages_Controller
        {
            protected ArcenUI_TextWithInlineImages element;

            public void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                //Buffer.Add( "boo!" );
            }

            public void OnUpdate()
            {
                
            }

            public void SetElement( ArcenUI_Element Element )
            {
                this.element = (ArcenUI_TextWithInlineImages)Element;
            }
        }
    }
}