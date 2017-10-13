using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameTechTabMenu : ToggleableWindowController
    {
        public static Window_InGameTechTabMenu Instance;
        public Window_InGameTechTabMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        public int CurrentMenuIndex;

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;

            return true;
        }

        public class bsItems : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return;
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet != null ) { } //prevent compiler warning
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                if ( elementAsType != null ) { } //prevent compiler warning
                Window_InGameTechTabMenu windowController = (Window_InGameTechTabMenu)Element.Window.Controller;
                if ( windowController != null ) { } //prevent compiler warning

                if ( elementAsType.Buttons.Count <= 0 )
                {
                    elementAsType.ClearButtons();

                    List<TechMenu> menus = TechMenuTable.Instance.Rows;
                    int x = 0;
                    for ( int i = 0; i < menus.Count; i++ )
                    {
                        TechMenu item = menus[i];
                        if ( item.DoNotShowOnTechMenu )
                            continue;
                        bItem newButtonController = new bItem( i );
                        Vector2 offset;
                        offset.x = x * elementAsType.ButtonWidth;
                        offset.y = 0;
                        Vector2 size;
                        size.x = elementAsType.ButtonWidth;
                        size.y = elementAsType.ButtonHeight;
                        elementAsType.AddButton( newButtonController, size, offset );
                        x++;
                    }

                    elementAsType.ActuallyPutItemsBackInPoolThatAreStillCleared();
                }
            }
        }

        private class bItem : WindowTogglingButtonController
        {
            public int MenuIndex;

            public bItem( int MenuIndex )
                 : base( string.Empty, "^" )
            {
                this.MenuIndex = MenuIndex;
            }

            private TechMenu GetMenu()
            {
                if ( this.MenuIndex < 0 || this.MenuIndex >= TechMenuTable.Instance.Rows.Count )
                    return null;
                return TechMenuTable.Instance.Rows[this.MenuIndex];
            }

            public override bool GetShouldSuppressOpenIndicatorEvenIfToggledWindowIsShown()
            {
                if ( MenuIndex != Window_InGameTechTabMenu.Instance.CurrentMenuIndex )
                    return true;
                return false;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                TechMenu menu = this.GetMenu();
                if ( menu == null )
                    Buffer.Add( "NULL" );
                else
                    Buffer.Add( menu.Abbreviation );
            }

            public override MouseHandlingResult HandleClick()
            {
                bool justSwitching = Instance.CurrentMenuIndex >= 0 && MenuIndex != Instance.CurrentMenuIndex;
                Instance.CurrentMenuIndex = this.MenuIndex;
                if ( !Window_InGameTechTypeIconMenu.Instance.IsOpen )
                {
                    Window_InGameTechTypeIconMenu.Instance.LastMenuIndex = -1;
                    Window_InGameTechTypeIconMenu.Instance.LastTypeIndex = -1;
                }
                else
                {
                    if ( justSwitching )
                        return MouseHandlingResult.None; // skip the HandleClick at the end
                }
                base.HandleClick();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }

            public override ToggleableWindowController GetRelatedController() { return Window_InGameTechTypeIconMenu.Instance; }
        }
    }
}