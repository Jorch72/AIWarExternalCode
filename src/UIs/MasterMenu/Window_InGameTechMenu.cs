using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameTechMenu : ToggleableWindowController
    {
        public static Window_InGameTechMenu Instance;
        public Window_InGameTechMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        private int _CurrentMenuIndex;
        private int CurrentMenuIndex
        {
            get { return _CurrentMenuIndex; }
            set
            {
                if ( _CurrentMenuIndex == value )
                    return;
                _CurrentMenuIndex = value;
                MenuIndexChangedSinceLastButtonSetUpdate = true;
            }
        }
        
        private bool MenuIndexChangedSinceLastButtonSetUpdate;

        public class bsMenuGrid : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                Window_InGameTechMenu windowController = (Window_InGameTechMenu)Element.Window.Controller;

                if ( windowController.MenuIndexChangedSinceLastButtonSetUpdate )
                {
                    elementAsType.ClearButtons();
                    
                    TechMenu menu = TechMenuTable.Instance.Rows[windowController.CurrentMenuIndex];
                    if ( menu != null )
                    {
                        int shownColumnCount = 0;
                        for ( int x = 0; x < menu.Columns.Count; x++ )
                        {
                            bool haveShownAnythingInThisColumn = false;
                            List<TechTypeData> column = menu.Columns[x];
                            for ( int y = 0; y < column.Count; y++ )
                            {
                                TechTypeData item = column[y];
                                if ( item.NotOnMainTechMenu )
                                    continue;
                                if ( item.Prerequisite != null )
                                {
                                    TechTypeData prereq = item.Prerequisite;
                                    bool foundOne = false;
                                    while ( prereq != null )
                                    {
                                        if ( ( prereq.Prerequisite == null && !prereq.NotOnMainTechMenu ) || localSide.GetHasResearched( prereq ) )
                                        {
                                            foundOne = true;
                                            break;
                                        }
                                        prereq = prereq.Prerequisite;
                                    }
                                    if ( !foundOne )
                                        continue;
                                }
                                //if ( localSide.GetCanResearch( item ) != ArcenRejectionReason.Unknown &&
                                //     !localSide.UnlockedTechs.Contains( item ) )
                                //    continue;
                                haveShownAnythingInThisColumn = true;
                                bTechItem newButtonController = new bTechItem( item );
                                Vector2 offset;
                                offset.x = shownColumnCount * elementAsType.ButtonWidth;
                                offset.y = y * elementAsType.ButtonHeight;
                                Vector2 size;
                                size.x = elementAsType.ButtonWidth;
                                size.y = elementAsType.ButtonHeight;
                                elementAsType.AddButton( newButtonController, size, offset );
                            }
                            if ( haveShownAnythingInThisColumn )
                                shownColumnCount++;
                        }
                    }

                    elementAsType.ActuallyDestroyButtonsThatAreStillCleared();

                    windowController.MenuIndexChangedSinceLastButtonSetUpdate = false;
                }
            }
        }

        private class bTechItem : ButtonAbstractBase
        {
            public TechTypeData Item;

            public bTechItem( TechTypeData Item )
            {
                this.Item = Item;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );

                if ( this.Item == null )
                {
                    buffer.Add( "NULL" );
                    return;
                }
                buffer.Add( this.Item.Name );
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide.UnlockedTechs.Contains( this.Item ) )
                    buffer.Add( "\n" ).Add( "(Unlocked)" );
                else
                {
                    buffer.Add( "\n" ).Add( "(" ).Add( this.Item.ScienceCost ).Add( ")" );

                    if ( localSide.GetCanResearch( this.Item, false, false ) == ArcenRejectionReason.SideDoesNotHavePrerequisiteTech )
                        buffer.Add( "\n" ).Add( "(Locked)" );
                }
            }

            public override void HandleClick()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide.GetCanResearch( this.Item, false, false ) != ArcenRejectionReason.Unknown )
                    return;
                GameCommand command = GameCommand.Create( GameCommandType.UnlockTech );
                command.RelatedSide = World_AIW2.Instance.GetLocalSide();
                command.RelatedTech = this.Item;
                World_AIW2.Instance.QueueGameCommand( command );
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }

        public class bsMenuSelectionRow : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                Window_InGameTechMenu windowController = (Window_InGameTechMenu)Element.Window.Controller;

                if ( elementAsType.Buttons.Count <= 0 )
                {
                    elementAsType.ClearButtons();

                    List<TechMenu> menus = TechMenuTable.Instance.Rows;
                    for ( int x = 0; x < menus.Count; x++ )
                    {
                        TechMenu item = menus[x];
                        bMenuSelectionItem newButtonController = new bMenuSelectionItem( x );
                        Vector2 offset;
                        offset.x = x * elementAsType.ButtonWidth;
                        offset.y = 0;
                        Vector2 size;
                        size.x = elementAsType.ButtonWidth;
                        size.y = elementAsType.ButtonHeight;
                        elementAsType.AddButton( newButtonController, size, offset );
                    }
                    
                    windowController.MenuIndexChangedSinceLastButtonSetUpdate = true;
                }
            }
        }

        private class bMenuSelectionItem : ButtonAbstractBase
        {
            public int MenuIndex;

            public bMenuSelectionItem( int MenuIndex )
            {
                this.MenuIndex = MenuIndex;
            }

            private TechMenu GetMenu()
            {
                if ( this.MenuIndex < 0 || this.MenuIndex >= TechMenuTable.Instance.Rows.Count )
                    return null;
                return TechMenuTable.Instance.Rows[this.MenuIndex];
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

            public override void HandleClick()
            {
                Window_InGameTechMenu.Instance.CurrentMenuIndex = this.MenuIndex;
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }
    }
}