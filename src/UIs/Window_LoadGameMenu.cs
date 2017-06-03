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

        public void Open()
        {
            if ( this.IsOpen )
                return;
            this.IsOpen = true;
        }

        public void Close()
        {
            if ( !this.IsOpen )
                return;
            this.IsOpen = false;
            this.HasUpdatedSinceLastClose = false;
        }

        public class bClose : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Close" );
            }
            public override void HandleClick() { Instance.Close(); }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bsLoadGameButtons : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                if ( Instance.HasUpdatedSinceLastClose )
                    return;
                Instance.HasUpdatedSinceLastClose = true;

                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                elementAsType.ClearButtons();

                string directoryPath = Engine_Universal.CurrentPlayerDataDirectory + "Save/";
                string[] files = Directory.GetFiles( directoryPath, "*" + Engine_Universal.SaveExtension );

                for ( int i = 0; i < files.Length; i++ )
                {
                    string file = files[i];
                    string saveName = Path.GetFileNameWithoutExtension( file );
                    bLoadGameButton newButtonController = new bLoadGameButton( saveName );
                    Vector2 offset;
                    offset.x = 0;
                    offset.y = i * elementAsType.ButtonHeight;
                    Vector2 size;
                    size.x = elementAsType.ButtonWidth;
                    size.y = elementAsType.ButtonHeight;
                    elementAsType.AddButton( newButtonController, size, offset );
                }

                elementAsType.ActuallyDestroyButtonsThatAreStillCleared();
            }
        }

        private class bLoadGameButton : ButtonAbstractBase
        {
            public string SaveName = string.Empty;

            public bLoadGameButton( string Filename )
            {
                this.SaveName = Filename;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                buffer.Add( SaveName );
            }

            public override void HandleClick()
            {
                string path = Engine_Universal.CurrentPlayerDataDirectory + "Save/" + this.SaveName + Engine_Universal.SaveExtension;
                if ( !File.Exists( path ) )
                    return;
                Engine_Universal.LoadGame( this.SaveName );
                Instance.Close();
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }
    }
}