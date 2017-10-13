using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using Arcen.Universal.Sprites;

namespace Arcen.AIW2.External
{
    public class Window_InGameTechTypeIconMenu : ToggleableWindowController
    {
        public static Window_InGameTechTypeIconMenu Instance;
        public Window_InGameTechTypeIconMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        public int CurrentTypeIndex = -1;
        public int LastMenuIndex = -1;
        public int LastTypeIndex = -1;
        public readonly List<List<bItem>> LastShownItems = new List<List<bItem>>();
        private float HeightPerRow = -1;

        public class bsItems : ImageButtonSetAbstractBase
        {
            public ArcenUI_ImageButtonSet Element;
            public override void OnUpdate()
            {
                if ( Instance.HeightPerRow <= 0 )
                    Instance.HeightPerRow = Element.ButtonHeight;
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return;
                ArcenUI_ImageButtonSet elementAsType = (ArcenUI_ImageButtonSet)Element;
                if ( elementAsType != null ) { } //prevent compiler warning
                Window_InGameTechTypeIconMenu windowController = (Window_InGameTechTypeIconMenu)Element.Window.Controller;
                if ( windowController != null ) { } //prevent compiler warning

                if ( Instance.LastMenuIndex != Window_InGameTechTabMenu.Instance.CurrentMenuIndex ||
                     Instance.LastTypeIndex != Instance.CurrentTypeIndex )
                {
                    Instance.LastMenuIndex = Window_InGameTechTabMenu.Instance.CurrentMenuIndex;
                    Instance.LastTypeIndex = Instance.CurrentTypeIndex;

                    for ( int i = 0; i < Instance.LastShownItems.Count; i++ )
                        Instance.LastShownItems[i].Clear();

                    int highestRowReached = 0;
                    if ( Instance.LastMenuIndex >= TechMenuTable.Instance.Rows.Count )
                    {
                        Instance.LastMenuIndex = 0;
                        Window_InGameTechTabMenu.Instance.CurrentMenuIndex = 0;
                    }
                    TechMenu menu = TechMenuTable.Instance.Rows[Instance.LastMenuIndex];
                    if ( menu != null )
                    {
                        float aspectRatioAdjustedButtonWidth = this.Element.ButtonWidth;
                        float aspectRatioAdjustedButtonHeight = this.Element.ButtonHeight;
                        if ( ArcenUI.Instance.PixelsPerPercent_X != ArcenUI.Instance.PixelsPerPercent_Y )
                            aspectRatioAdjustedButtonWidth *= ArcenUI.Instance.PixelsPerPercent_Y / ArcenUI.Instance.PixelsPerPercent_X;
                        //int totalColumns = Mathf.FloorToInt( this.Element.Width / aspectRatioAdjustedButtonWidth );
                        int currentImageListIndex = 0;

                        int shownColumnCount = 0;
                        for ( int typeIndex = 0; typeIndex < menu.Columns.Count; typeIndex++ )
                        {
                            List<TechTypeData> column = menu.Columns[typeIndex];
                            int buttonsInColumn = 0;
                            for ( int markIndex = 0; markIndex < column.Count; markIndex++ )
                            {
                                TechTypeData menuItem = column[markIndex];
                                if ( !localSide.CanSeeTechOnMenu( menuItem ) )
                                    continue;
                                buttonsInColumn = markIndex + 1;
                            }
                            highestRowReached = Math.Max( highestRowReached, buttonsInColumn );
                        }

                        for ( int typeIndex = 0; typeIndex < menu.Columns.Count; typeIndex++ )
                        {
                            if ( Instance.LastShownItems.Count <= typeIndex )
                                Instance.LastShownItems.Add( new List<bItem>() );
                            List<bItem> lastShownItemList = Instance.LastShownItems[typeIndex];

                            bool haveShownAnythingInThisColumn = false;
                            List<TechTypeData> column = menu.Columns[typeIndex];
                            if ( column.Count <= 0 )
                                continue;
                            for ( int markIndex = 0; markIndex < column.Count; markIndex++ )
                            {
                                if ( lastShownItemList.Count <= markIndex )
                                    lastShownItemList.Add( null );

                                TechTypeData menuItem = column[markIndex];
                                if ( !localSide.CanSeeTechOnMenu( menuItem ) )
                                    continue;
                                haveShownAnythingInThisColumn = true;

                                bItem item = null;
                                if ( currentImageListIndex < this.Element.Images.Count )
                                {
                                    item = Element.Images[currentImageListIndex].Controller as bItem;
                                    currentImageListIndex++;
                                }
                                else
                                {
                                    item = new bItem();
                                    Vector2 dummy = Mat.V2_Zero;
                                    Vector2 size;
                                    size.x = aspectRatioAdjustedButtonWidth;
                                    size.y = aspectRatioAdjustedButtonHeight;
                                    Element.AddImageButton( item, size, dummy );
                                    currentImageListIndex = Element.Images.Count;
                                }
                                item.ColumnIndex = shownColumnCount;
                                lastShownItemList[markIndex] = item;

                                float effectiveX = shownColumnCount * aspectRatioAdjustedButtonWidth;
                                float effectiveY = ( ( highestRowReached - 1 ) - markIndex ) * aspectRatioAdjustedButtonHeight;
                                
                                if ( menuItem.GameEntityTypesThatRequireThis.Count > 0 )
                                    item.SetTypeToTech( menuItem.GameEntityTypesThatRequireThis[0] );
                                item.Element.Alignment.XAlignment.Offset = effectiveX;
                                item.Element.Alignment.YAlignment.Offset = effectiveY;
                                item.Element.UpdatePositionAndSize();
                            }
                            if ( haveShownAnythingInThisColumn )
                                shownColumnCount++;
                        }

                        //hide any extras
                        for ( int i = currentImageListIndex; i < this.Element.Images.Count; i++ )
                        {
                            bItem item = this.Element.Images[i].Controller as bItem;
                            item.SetTypeToTech( null );
                        }
                    }
                    if ( Instance.HeightPerRow > 0 )
                    {
                        float currentExpansion = Instance.Window.SubContainer.Height - Instance.HeightPerRow;
                        float targetExpansion = Instance.HeightPerRow * ( highestRowReached - 1 );
                        float deltaExpansion = targetExpansion - currentExpansion;
                        if ( deltaExpansion != 0 )
                            Instance.Window.SubContainer.Height = Instance.HeightPerRow * highestRowReached;
                    }
                }
            }

            public override void SetElement( ArcenUI_Element Element )
            {
                this.Element = (ArcenUI_ImageButtonSet)Element;
            }
        }

        public class bItem : ShipIconButton
        {
            private GameEntityTypeData _TypeToTech;

            public bItem() : base(Mode.Tech){ }

            public override GameEntityTypeData TypeToBuild
            {
                get
                {
                    return _TypeToTech;
                }
            }

            public void SetTypeToTech(GameEntityTypeData Value)
            {
                _TypeToTech = Value;
            }
        }
    }
}