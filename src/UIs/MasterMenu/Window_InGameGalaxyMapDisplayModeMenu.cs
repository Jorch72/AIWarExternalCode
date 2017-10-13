using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameGalaxyMapDisplayModeMenu : ToggleableWindowController
    {
        public static Window_InGameGalaxyMapDisplayModeMenu Instance;
        public Window_InGameGalaxyMapDisplayModeMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        public class bsItems : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                //Window_InGameGalaxyMapDisplayModeMenu windowController = (Window_InGameGalaxyMapDisplayModeMenu)Element.Window.Controller;

                if ( elementAsType.Buttons.Count <= 0 )
                {
                    int x = 0;
                    for(int i = 0; i < GalaxyMapDisplayModeTable.Instance.Rows.Count;i++ )
                    {
                        GalaxyMapDisplayMode mode = GalaxyMapDisplayModeTable.Instance.Rows[i];
                        bItem newButtonController = new bItem( mode );
                        Vector2 offset;
                        offset.x = x * elementAsType.ButtonWidth;
                        offset.y = 0;
                        Vector2 size;
                        size.x = elementAsType.ButtonWidth;
                        size.y = elementAsType.ButtonHeight;
                        elementAsType.AddButton( newButtonController, size, offset );
                        x++;
                    }
                }
            }
        }

        private class bItem : ButtonAbstractBase
        {
            public GalaxyMapDisplayMode Mode;

            public bItem( GalaxyMapDisplayMode Mode )
            {
                this.Mode = Mode;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                if ( PlayerAccount_AIW2.Local.CurrentGalaxyMapDisplayMode == this.Mode )
                    buffer.Add( "*" );
                buffer.Add( this.Mode.Name );
            }

            public override MouseHandlingResult HandleClick()
            {
                PlayerAccount_AIW2.Local.CurrentGalaxyMapDisplayMode = this.Mode;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }
    }
}