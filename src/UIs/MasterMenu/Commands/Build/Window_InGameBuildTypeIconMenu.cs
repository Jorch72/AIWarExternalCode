using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using Arcen.Universal.Sprites;

namespace Arcen.AIW2.External
{
    public class Window_InGameBuildTypeIconMenu : ToggleableWindowController
    {
        public static Window_InGameBuildTypeIconMenu Instance;
        public Window_InGameBuildTypeIconMenu()
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
                Window_InGameBuildTypeIconMenu windowController = (Window_InGameBuildTypeIconMenu)Element.Window.Controller;
                if ( windowController != null ) { } //prevent compiler warning

                if ( Instance.LastMenuIndex != Window_InGameBuildTabMenu.Instance.CurrentMenuIndex ||
                     Instance.LastTypeIndex != Instance.CurrentTypeIndex )
                {
                    Instance.LastMenuIndex = Window_InGameBuildTabMenu.Instance.CurrentMenuIndex;
                    Instance.LastTypeIndex = Instance.CurrentTypeIndex;

                    for ( int i = 0; i < Instance.LastShownItems.Count; i++ )
                        Instance.LastShownItems[i].Clear();

                    GameEntity entity = World_AIW2.Instance.GetEntityByID( Window_InGameBuildTabMenu.Instance.EntityID );
                    if ( entity != null && entity.TypeData.BuildMenus.Count > 0 )
                    {
                        int highestRowReached = 0;
                        if ( Instance.LastMenuIndex >= entity.TypeData.BuildMenus.Count )
                        {
                            Instance.LastMenuIndex = 0;
                            Window_InGameBuildTabMenu.Instance.CurrentMenuIndex = 0;
                        }
                        BuildMenu menu = entity.TypeData.BuildMenus[Instance.LastMenuIndex];
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
                                List<BuildMenuItem> column = menu.Columns[typeIndex];
                                int buttonsInColumn = 0;
                                for ( int markIndex = 0; markIndex < column.Count; markIndex++ )
                                {
                                    BuildMenuItem menuItem = column[markIndex];
                                    if ( menuItem.EntityDataOrNull == null )
                                        continue;
                                    if ( localSide.GetCanBuildAtAll( menuItem.EntityDataOrNull ) != ArcenRejectionReason.Unknown )
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
                                List<BuildMenuItem> column = menu.Columns[typeIndex];
                                if ( column.Count <= 0 )
                                    continue;
                                for ( int markIndex = 0; markIndex < column.Count; markIndex++ )
                                {
                                    if ( lastShownItemList.Count <= markIndex )
                                        lastShownItemList.Add( null );

                                    BuildMenuItem menuItem = column[markIndex];
                                    if ( menuItem.EntityDataOrNull == null )
                                        continue;
                                    if ( localSide.GetCanBuildAtAll( menuItem.EntityDataOrNull ) != ArcenRejectionReason.Unknown )
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

                                    item.TypeDoingTheBuilding = entity.TypeData;
                                    item.SetTypeToBuild( menuItem.EntityDataOrNull );
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
                                item.SetTypeToBuild( null );
                                item.TypeDoingTheBuilding = null;
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
            }

            public override void SetElement( ArcenUI_Element Element )
            {
                this.Element = (ArcenUI_ImageButtonSet)Element;
            }
        }

        public class bItem : ShipIconButton
        {
            private GameEntityTypeData _TypeToBuild;

            public bItem() : base(Mode.Build) { }

            public override GameEntityTypeData TypeToBuild
            {
                get
                {
                    return _TypeToBuild;
                }
            }

            public void SetTypeToBuild(GameEntityTypeData Value)
            {
                _TypeToBuild = Value;
            }
        }
    }

    public abstract class ShipIconButton : ImageButtonAbstractBase
    {
        public ArcenUI_ImageButton Element;
        public GameEntityTypeData TypeDoingTheBuilding;
        public int ColumnIndex;
        public Mode ButtonMode;

        public enum Mode
        {
            None,
            Build,
            Queue,
            Tech
        }

        protected ShipIconButton(Mode ButtonMode)
        {
            this.ButtonMode = ButtonMode;
        }

        public const int INDEX_ICON_NO_FLAIR = 0;
        public const int INDEX_FLAIR = 1;
        public const int INDEX_MARK_LEVEL = 2;
        public const int INDEX_ICON_FLAIR_ONLY = 3;
        public const int INDEX_FUEL = 4;
        public const int INDEX_POWER = 5;
        public const int INDEX_SCIENCE = 6;
        public const int INDEX_LOCKED = 7;
        public const int INDEX_UNLOCKED = 8;
        public const int INDEX_ACTIVE_KEYBOARD_COLUMN = 9;

        public const int TEXT_INDEX_UPPER_LEFT = 0;
        public const int TEXT_INDEX_ABOVE_BUTTON = 1;

        public override void UpdateContent( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            GameEntityTypeData typeData = this.TypeToBuild;
            if ( typeData == null )
                return;

            WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
            Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            CombatSide localCombatSide = planet.Combat.GetSideForWorldSide( localSide );

            int debugStage = -1;
            try
            {
                debugStage = 1;

                debugStage = 2;
                SubImages[INDEX_ICON_NO_FLAIR].WrapperedImage.UpdateWith( null, true );
                SubImages[INDEX_ICON_FLAIR_ONLY].WrapperedImage.UpdateWith( typeData.GUISprite_Icon_White, true );

                // uncomment if you want a crazy experiment with icons varying in size over time
                //float size = ( World_AIW2.Instance.GameSecond * 10 ) % 100;
                //if ( size < 1 )
                //    size = 1;
                //SubImages[INDEX_ICON_FLAIR_ONLY].WrapperedImage.SetSize( size, size / 2 );

                // uncomment if you want to try vertically centering non-flair icons instead of leaving the gap at the bottom
                //if ( typeData.GUISprite_Flair == null )
                //    SubImages[INDEX_ICON_FLAIR_ONLY].WrapperedImage.SetOffset( 0, 5 );

                debugStage = 4;
                SubImages[INDEX_FLAIR].WrapperedImage.UpdateWith( typeData.GUISprite_Flair, true );

                debugStage = 5;
                bool showFuel = typeData.BalanceStats.SquadFuelConsumption > 0 && localSide.NetFuel < typeData.BalanceStats.SquadFuelConsumption;
                SubImages[INDEX_FUEL].WrapperedImage.UpdateToShowOrHide( showFuel );

                debugStage = 6;
                bool showPower = typeData.BalanceStats.SquadPowerConsumption > 0 && localCombatSide.NetPower < typeData.BalanceStats.SquadPowerConsumption;
                SubImages[INDEX_POWER].WrapperedImage.UpdateToShowOrHide( showPower );

                debugStage = 7;
                bool showScience = false;
                bool showLocked = false;
                bool showUnlocked = false;

                if ( this.ButtonMode == Mode.Tech )
                {
                    if ( typeData.TechPrereq == null )
                    {
                        // shouldn't really be on the tech menu anyway
                    }
                    else
                    {
                        if ( localSide.GetHasResearched( typeData.TechPrereq ) )
                            showUnlocked = true;
                        else if ( localSide.GetCanResearch( typeData.TechPrereq, true, false ) != ArcenRejectionReason.Unknown )
                            showLocked = true;
                        else if ( localSide.GetCanResearch( typeData.TechPrereq, false, false ) != ArcenRejectionReason.Unknown )
                            showScience = true;
                    }
                }

                SubImages[INDEX_SCIENCE].WrapperedImage.UpdateToShowOrHide( showScience );

                //uncomment if you want a crazy experiemnt replacing this icon with a fuel icon
                //SubImages[INDEX_SCIENCE].WrapperedImage.SetBundleAndPathInBundle( "arcenui", "assets/icons/officialgui/resources/fuel.png" );

                SubImages[INDEX_LOCKED].WrapperedImage.UpdateToShowOrHide( showLocked );
                Image.SetColor( showLocked ? ColorMath.LightGray : ColorMath.White );
                SubImages[INDEX_UNLOCKED].WrapperedImage.UpdateToShowOrHide( showUnlocked );

                debugStage = 8;
                bool showActiveKeyboardColumnIndicator = false;
                if ( this.ButtonMode == Mode.Build )
                    showActiveKeyboardColumnIndicator = this.ColumnIndex == Window_InGameBuildTypeIconMenu.Instance.CurrentTypeIndex;
                else if ( this.ButtonMode == Mode.Tech )
                    showActiveKeyboardColumnIndicator = this.ColumnIndex == Window_InGameTechTypeIconMenu.Instance.CurrentTypeIndex;
                SubImages[INDEX_ACTIVE_KEYBOARD_COLUMN].WrapperedImage.UpdateToShowOrHide( showActiveKeyboardColumnIndicator );

                debugStage = 15;
                int markLevel = typeData.Balance_MarkLevel.Ordinal;
                if ( markLevel < 0 || markLevel > 5 )
                    markLevel = 0;
                SubImages[INDEX_MARK_LEVEL].WrapperedImage.UpdateWith( markLevel <= 0 ? null : Window_InGameOutlineSidebar.Sprite_MarkLevels[markLevel], true );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in UpdateContent (image) at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            debugStage = -1;
            try
            {
                debugStage = 1;
                ArcenUIWrapperedTMProText text = SubTexts[TEXT_INDEX_UPPER_LEFT].Text;

                debugStage = 2;
                ArcenDoubleCharacterBuffer buffer = text.StartWritingToBuffer();

                if ( typeData != null )
                {
                    switch ( this.ButtonMode )
                    {
                        case Mode.Build:
                        case Mode.Queue:
                            if ( planet != null )
                            {
                                int remainingCap = localCombatSide.GetRemainingCap( typeData );

                                text.SetFontSize( 12 );
                                buffer.Add( remainingCap );

                                if ( remainingCap <= 0 )
                                    text.SetColor( ColorMath.LightRed );
                                else
                                    text.SetColor( ColorMath.White );
                            }
                            break;
                        case Mode.Tech:
                            if ( typeData.TechPrereq == null )
                                break;
                            if ( localSide.GetHasResearched( typeData.TechPrereq ) )
                                break;
                            int cost = typeData.TechPrereq.ScienceCost;
                            buffer.Add( cost );
                            if ( cost > localSide.StoredScience )
                                text.SetColor( ColorMath.LightRed );
                            else
                                text.SetColor( ColorMath.White );
                            break;
                    }
                }

                text.FinishWritingToBuffer();

                debugStage = 3;
                text = SubTexts[TEXT_INDEX_ABOVE_BUTTON].Text;

                debugStage = 4;
                buffer = text.StartWritingToBuffer();

                if ( this.ButtonMode == Mode.Queue )
                {
                    GameEntity builder = null;
                    Engine_AIW2.Instance.DoForSelected( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, delegate( GameEntity selected )
                    {
                        if ( selected.TypeData != this.TypeDoingTheBuilding )
                            return DelReturn.Continue;
                        if ( builder != null )
                        {
                        // only show time display when there's only one builder, otherwise it's not clear what should be shown
                        builder = null;
                            return DelReturn.Break;
                        }
                        builder = selected;
                        return DelReturn.Continue;
                    } );
                    debugStage = 5;
                    if ( builder != null && builder.BuildQueue != null )
                    {
                        BuildQueueItem item = builder.BuildQueue.GetQueueItemFor( typeData );
                        if ( item != null )
                        {
                            bool showTimer = item.MetalSpentOnCurrentIteration > 0 && item.NumberBuiltThisLoop < item.NumberToBuildEachLoop;
                            if ( showTimer )
                            {
                                FInt metalLeft = typeData.BalanceStats.SquadMetalCost - item.MetalSpentOnCurrentIteration;
                                FInt metalRate = builder.TypeData.MetalFlows[MetalFlowPurpose.BuildingShipsInternally].EffectiveThroughput;
                                if ( metalLeft > 0 && metalRate > 0 )
                                {
                                    FInt secondsLeft = metalLeft / metalRate;
                                    buffer.Add( Engine_Universal.ToHoursAndMinutesString( secondsLeft.IntValue ) );
                                }
                            }
                        }
                    }
                }

                debugStage = 6;
                text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in UpdateContent (text) at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }
        }

        public abstract GameEntityTypeData TypeToBuild
        {
            get;
        }

        public override void SetElement( ArcenUI_Element Element )
        {
            this.Element = (ArcenUI_ImageButton)Element;
        }

        public override MouseHandlingResult HandleClick()
        {
            if ( this.TypeToBuild == null )
                return MouseHandlingResult.PlayClickDeniedSound;
            else
            {
                if ( Window_InGameTechTypeIconMenu.Instance.IsOpen )
                {
                    WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                    if ( localSide.GetCanResearch( this.TypeToBuild.TechPrereq, false, false ) != ArcenRejectionReason.Unknown )
                        return MouseHandlingResult.PlayClickDeniedSound;
                    GameCommand command = GameCommand.Create( GameCommandType.UnlockTech );
                    command.RelatedSide = World_AIW2.Instance.GetLocalPlayerSide();
                    command.RelatedTech = this.TypeToBuild.TechPrereq;
                    World_AIW2.Instance.QueueGameCommand( command, true );
                    return MouseHandlingResult.None;
                }
                else
                {
                    if ( this.TypeToBuild.MetalFlows[MetalFlowPurpose.SelfConstruction] != null )
                        Engine_AIW2.Instance.PlacingEntityType = this.TypeToBuild;
                    else
                    {
                        GameCommand command = GameCommand.Create( GameCommandType.AlterBuildQueue );
                        command.RelatedEntityType = this.TypeToBuild;
                        command.RelatedMagnitude = 1;
                        if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Subtractive ) )
                            command.RelatedMagnitude = -command.RelatedMagnitude;

                        Engine_AIW2.Instance.DoForSelected( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, delegate ( GameEntity selected )
                        {
                            if ( selected.TypeData != this.TypeDoingTheBuilding )
                                return DelReturn.Continue;
                            command.RelatedEntityIDs.Add( selected.PrimaryKeyID );
                            return DelReturn.Continue;
                        } );

                        if ( command.RelatedEntityIDs.Count > 0 )
                            World_AIW2.Instance.QueueGameCommand( command, true );
                    }
                }
            }
            return MouseHandlingResult.None;
        }

        public override void HandleMouseover()
        {
            if ( this.TypeToBuild != null )
                GameEntityTypeData.CurrentlyHoveredOver = this.TypeToBuild;
        }

        public override bool GetShouldBeHidden()
        {
            return this.TypeToBuild == null;
        }
    }
}