using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_SaveGameMenu : ToggleableWindowController
    {
        public static Window_SaveGameMenu Instance;
        public Window_SaveGameMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
        }

        private bool NeedsUpdate;

        public override void OnOpen()
        {
            this.NeedsUpdate = true;
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

        public class bsSaveGameButtons : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                if ( !Instance.NeedsUpdate )
                    return;
                Instance.NeedsUpdate = false;

                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                elementAsType.ClearButtons();

                string directoryPath = Engine_Universal.CurrentPlayerDataDirectory + "Save/";
                string[] files = Directory.GetFiles( directoryPath, "*" + Engine_Universal.SaveExtension );

                for ( int i = 0; i < files.Length; i++ )
                {
                    string file = files[i];
                    string saveName = Path.GetFileNameWithoutExtension( file );
                    AddSaveButton( elementAsType, saveName, i );
                }

                string newSaveName = "NewSave_" + ( files.Length + 1 );
                AddSaveButton( elementAsType, newSaveName, files.Length );

                elementAsType.ActuallyDestroyButtonsThatAreStillCleared();
            }

            private static void AddSaveButton( ArcenUI_ButtonSet elementAsType, String saveName, int index )
            {
                bSaveGameButton newButtonController = new bSaveGameButton( saveName );
                Vector2 offset;
                offset.x = 0;
                offset.y = index * elementAsType.ButtonHeight;
                Vector2 size;
                size.x = elementAsType.ButtonWidth;
                size.y = elementAsType.ButtonHeight;
                elementAsType.AddButton( newButtonController, size, offset );
            }
        }

        private class bSaveGameButton : ButtonAbstractBase
        {
            public string SaveName = string.Empty;

            public bSaveGameButton( string Filename )
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
                World.Instance.SaveWorldToDisk( this.SaveName );
                Window_SaveGameMenu.Instance.Close();
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }
    }
}