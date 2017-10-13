using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public class Window_InGameEscapeMenu : ToggleableWindowController
    {
        public static Window_InGameEscapeMenu Instance;
        public Window_InGameEscapeMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        public class bSettings : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Settings" );
            }
            public override MouseHandlingResult HandleClick()
            {
                Window_SettingsMenu.Instance.Open();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bSaveGame : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Save Game" );
            }
            public override MouseHandlingResult HandleClick()
            {
                Window_SaveGameMenu.Instance.Open();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bExitGame : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Quit Game" );
            }
            public override MouseHandlingResult HandleClick()
            {
                Engine_AIW2.QuitRequested = true;
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }
    }
}