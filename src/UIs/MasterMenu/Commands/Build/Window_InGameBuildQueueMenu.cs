using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameBuildQueueMenu : WindowControllerAbstractBase
    {
        public static Window_InGameBuildQueueMenu Instance;
        public Window_InGameBuildQueueMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;

            if ( !Window_InGameBuildTabMenu.Instance.GetShouldDrawThisFrame() )
                return false;

            return true;
        }

        public class bsItems : ImageButtonSetAbstractBase
        {
            public ArcenUI_ImageButtonSet Element;
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return;
                ArcenUI_ImageButtonSet elementAsType = (ArcenUI_ImageButtonSet)Element;

                if ( Window_InGameBuildTabMenu.Instance.EntityChangedSinceLastButtonSetUpdate_Queue )
                {
                    elementAsType.ClearButtons();

                    GameEntity entity = World_AIW2.Instance.GetEntityByID( Window_InGameBuildTabMenu.Instance.EntityID );
                    if ( entity != null )
                    {
                        float aspectRatioAdjustedButtonWidth = this.Element.ButtonWidth;
                        float aspectRatioAdjustedButtonHeight = this.Element.ButtonHeight;
                        if ( ArcenUI.Instance.PixelsPerPercent_X != ArcenUI.Instance.PixelsPerPercent_Y )
                            aspectRatioAdjustedButtonWidth *= ArcenUI.Instance.PixelsPerPercent_Y / ArcenUI.Instance.PixelsPerPercent_X;

                        float runningX = 0;
                        if ( entity.BuildQueue != null )
                        {
                            List<BuildQueueItem> items = entity.BuildQueue.Items;
                            for ( int x = 0; x < items.Count; x++ )
                            {
                                BuildQueueItem item = items[x];
                                bQueueItem newButtonController = new bQueueItem( entity.TypeData, item, x );
                                Vector2 offset;
                                offset.x = runningX;
                                offset.y = 0;
                                Vector2 size;
                                size.x = aspectRatioAdjustedButtonWidth;
                                size.y = aspectRatioAdjustedButtonHeight;
                                elementAsType.AddImageButton( newButtonController, size, offset );
                                runningX += size.x;
                            }
                        }
                    }

                    elementAsType.ActuallyPutItemsBackInPoolThatAreStillCleared();

                    Window_InGameBuildTabMenu.Instance.EntityChangedSinceLastButtonSetUpdate_Queue = false;
                    Window_InGameBuildTabMenu.Instance.MenuIndexChangedSinceLastButtonSetUpdate = true;
                }
            }

            public override void SetElement( ArcenUI_Element Element )
            {
                this.Element = (ArcenUI_ImageButtonSet)Element;
            }
        }

        public class bQueueItem : ShipIconButton
        {
            public BuildQueueItem Item;
            public int ItemIndex;

            public bQueueItem( GameEntityTypeData TypeDoingTheBuilding, BuildQueueItem Item, int ItemIndex )
                : base(Mode.Queue)
            {
                this.TypeDoingTheBuilding = TypeDoingTheBuilding;
                this.Item = Item;
                this.ItemIndex = ItemIndex;
            }

            public override GameEntityTypeData TypeToBuild
            {
                get
                {
                    return Item == null ? null : Item.TypeToBuild;
                }
            }

            public void SetBuildQueueItem( BuildQueueItem Value )
            {
                Item = Value;
            }

            public override void UpdateContent( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                Image.SetColor( ColorMath.FromRGB( 255, 0, 0 ) );

                base.UpdateContent( Image, SubImages, SubTexts );
            }
        }
    }
}