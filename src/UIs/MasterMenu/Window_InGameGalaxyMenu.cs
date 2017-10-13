using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameGalaxyMenu : ToggleableWindowController
    {
        public static Window_InGameGalaxyMenu Instance;
        public Window_InGameGalaxyMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;

            return true;
        }

        public class bToggleGalaxyMapDisplayModeMenu : WindowTogglingButtonController
        {
            public bToggleGalaxyMapDisplayModeMenu() : base( "Display", "^" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameGalaxyMapDisplayModeMenu.Instance; }
        }

        public class bFindPlanetScreen : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Find Planet" );
            }
            public override MouseHandlingResult HandleClick()
            {
                Window_FindPlanetMenu.Instance.Open();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }
    }
}