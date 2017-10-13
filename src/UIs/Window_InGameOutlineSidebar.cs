using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using Arcen.Universal.Sprites;

namespace Arcen.AIW2.External
{
    public class Window_InGameOutlineSidebar : WindowControllerAbstractBase
    {
        public static Window_InGameOutlineSidebar Instance;

        public static Sprite[] Sprite_MarkLevels = new Sprite[6];
        public static Sprite[] Sprite_Backgrounds = new Sprite[18];

        public const string BUNDLE_NAME = "arcenui";
        public const string BUNDLE_PATH_OFFICIAL_BASE = "assets/icons/officialgui/official_1_smallonly/{0}.png";
        public const string BUNDLE_PATH_SIDEBAR_BASE = "assets/arcenui/images/sidebar/{0}.png";

        public const int PREFER_LEFT = -1;
        public const int PREFER_RIGHT = 1;
        public const int PREFER_NEITHER = 0;

        public const int MAX_ENTITIES_PER_ICON = 17;

        public Window_InGameOutlineSidebar()
        {
            Instance = this;
            this.OnlyShowInGame = true;

            for ( int i = 1; i <= 5; i++ )
                Sprite_MarkLevels[i] = ArcenAssetBundleManager.LoadUnitySpriteFromBundle( BUNDLE_NAME, 
                    string.Format( BUNDLE_PATH_OFFICIAL_BASE, "Y_" + i + "_L" ) );

            for ( int i = 1; i < Sprite_Backgrounds.Length; i++ )
                Sprite_Backgrounds[i] = ArcenAssetBundleManager.LoadUnitySpriteFromBundle( BUNDLE_NAME,
                    string.Format( BUNDLE_PATH_SIDEBAR_BASE, "outlinedecal_border_" + i ) );
        }
        
        public class tEnemySummary : TextAbstractBase
        {
            public static tEnemySummary Instance;
            public tEnemySummary() { Instance = this; }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                WriteSquadAndShipCounts( Buffer, SideRelationship.SidesIAmHostileTowards );
            }

            public override void OnUpdate() { }
        }

        public class tPlayerSummary : TextAbstractBase
        {
            public static tPlayerSummary Instance;
            public tPlayerSummary() { Instance = this; }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                WriteSquadAndShipCounts( Buffer, SideRelationship.Self );
            }
            public override void OnUpdate() { }
        }

        public class tAllySummary : TextAbstractBase
        {
            public static tAllySummary Instance;
            public tAllySummary() { Instance = this; }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                WriteSquadAndShipCounts( Buffer, SideRelationship.SidesIAmFriendlyTowards );
            }
            public override void OnUpdate() { }
        }

        public static void WriteSquadAndShipCounts( ArcenDoubleCharacterBuffer Buffer, SideRelationship relationship )
        {
            Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            if ( planet == null )
                return;

            CombatSide localSide = planet.Combat.GetSideForWorldSide( World_AIW2.Instance.GetLocalPlayerSide() );
            
            int squadResult = 0;
            FInt strength = FInt.Zero;
            localSide.DoForRelatedSides( relationship, delegate ( CombatSide side )
            {
                if ( relationship == SideRelationship.SidesIAmFriendlyTowards && side == localSide )
                    return DelReturn.Continue; // when counting "ally" units, don't count my own units
                side.Entities.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity ship )
                {
                    int shipCount = 1 + ship.GetCurrentExtraShipsInSquad();
                    strength += ship.TypeData.BalanceStats.StrengthPerShip * shipCount;
                    strength += ship.GetStrengthOfContentsIfAny();
                    squadResult++;
                    return DelReturn.Continue;
                } );
                return DelReturn.Continue;
            } );

            if ( squadResult > 0 )
            {
                int strengthAsInt = strength.IntValue;
                string strengthSuffix = ArcenExternalUIUtilities.GetRoundedNumberWithSuffix( ref strengthAsInt );
                Buffer
                    .Add( squadResult )
                    .Add( " squads (" )
                    .Add( strengthAsInt )
                    .Add( strengthSuffix )
                    .Add( " strength)" )
                ;
            }
        }

        public class bsOutlineItems : ImageButtonSetAbstractBase
        {
            public ArcenUI_ImageButtonSet Element;
            public List<EntityGroup> groups = new List<EntityGroup>();
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                if ( localSide == null )
                    return;

                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( !planet.HumansHaveBasicIntel )
                    return;

                //float adjustedButtonWidth = 32 / ArcenUI.Instance.PixelsPerPercent_X;
                //float adjustedButtonHeight = 32 / ArcenUI.Instance.PixelsPerPercent_Y;

                float adjustedButtonWidth = this.Element.ButtonWidth;
                float adjustedButtonHeight = this.Element.ButtonHeight;
                if ( ArcenUI.Instance.PixelsPerPercent_X != ArcenUI.Instance.PixelsPerPercent_Y )
                    adjustedButtonWidth *= ArcenUI.Instance.PixelsPerPercent_Y / ArcenUI.Instance.PixelsPerPercent_X;
                int currentRow = 0;
                int currentColumn = 0;
                int totalColumns = Mathf.FloorToInt( this.Element.Width / adjustedButtonWidth );
                groups.Clear();

                planet.Combat.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity entity )
                 {
                     if ( entity.TypeData.DoesNotNeedSidebarIcon )
                         return DelReturn.Continue;
                     EntityGroup group;
                     EntityGroupSideType sideType = ( entity.Side.WorldSide.GetIsLocalSide() ? EntityGroupSideType.Mine :
                     entity.Side.WorldSide.GetIsFriendlyToLocalSide() ? EntityGroupSideType.Allied : EntityGroupSideType.Enemy );
                     for ( int i = groups.Count - 1; i >= 0; i-- )
                     {
                         group = groups[i];
                         if ( group.TypeData == entity.TypeData && group.Side == sideType && group.EntityCount < MAX_ENTITIES_PER_ICON )
                         {
                             group.EntityCount++;
                             group.ActualEntities.Add( entity );
                             return DelReturn.Continue;
                         }
                     }
                     group = EntityGroup.GetNextFromPool();
                     group.TypeData = entity.TypeData;
                     group.Side = sideType;
                     group.EntityCount = 1;
                     group.ActualEntities.Add( entity );
                     groups.Add( group );

                     return DelReturn.Continue;
                 } );

                groups.Sort( delegate ( EntityGroup Left, EntityGroup Right )
                 {
                     if ( Left.Side != Right.Side )
                     {
                         if ( Left.Side < Right.Side )
                             return PREFER_LEFT;
                         else
                             return PREFER_RIGHT;
                     }

                     if ( Left.TypeData != Right.TypeData && Left.TypeData.BalanceStats.StrengthPerShip != Right.TypeData.BalanceStats.StrengthPerShip )
                     {
                         if ( Left.TypeData.BalanceStats.StrengthPerShip.RawValue != Right.TypeData.BalanceStats.StrengthPerShip.RawValue )
                         {
                             if ( Left.TypeData.BalanceStats.StrengthPerShip.RawValue < Right.TypeData.BalanceStats.StrengthPerShip.RawValue )
                                 return PREFER_RIGHT;
                             else
                                 return PREFER_LEFT;
                         }
                     }

                     if ( Left.TypeData.CopiedFrom != Right.TypeData.CopiedFrom )
                     {
                         if ( Left.TypeData.CopiedFrom.RowIndex < Right.TypeData.CopiedFrom.RowIndex )
                             return PREFER_LEFT;
                         else
                             return PREFER_RIGHT;
                     }

                     if ( Left.TypeData != Right.TypeData )
                     {
                         if ( Left.TypeData.RowIndex < Right.TypeData.RowIndex )
                             return PREFER_LEFT;
                         else
                             return PREFER_RIGHT;
                     }

                     return PREFER_NEITHER;
                 } );
                
                int currentImageListIndex = 0;
                EntityGroupSideType lastSideType = EntityGroupSideType.Unknown;
                bOutlineItem item = null;
                bool placedEnemyLabel = false;
                bool placedPlayerLabel = false;
                bool placedAllyLabel = false;
                bool nextItemGetsNewRow = false;
                for ( int groupIndex = groups.Count - 1; groupIndex >= 0; groupIndex-- )
                {
                    if ( nextItemGetsNewRow )
                    {
                        currentRow++;
                        nextItemGetsNewRow = false;
                    }
                    EntityGroup group = groups[groupIndex];                    
                    if ( currentImageListIndex < this.Element.Images.Count )
                    {
                        item = Element.Images[currentImageListIndex].Controller as bOutlineItem;
                        currentImageListIndex++;
                    }
                    else
                    {
                        item = new bOutlineItem();
                        Vector2 dummy = Mat.V2_Zero;
                        Vector2 size;
                        size.x = adjustedButtonWidth;
                        size.y = adjustedButtonHeight;
                        Element.AddImageButton( item, size, dummy );
                        currentImageListIndex = Element.Images.Count;
                    }

                    item.EntityGroup = group;

                    bool getsOwnLine = false;
                    switch ( group.TypeData.SpecialType )
                    {
                        case SpecialEntityType.AIKingUnit:
                        case SpecialEntityType.HumanKingUnit:
                            getsOwnLine = true;
                            break;
                    }
                    //if ( group.TypeData.Tags.Contains( "Flagship" ) )
                    //    getsOwnLine = true;
                    if ( lastSideType != EntityGroupSideType.Unknown )
                    {
                        if ( lastSideType != group.Side )
                        {
                            currentColumn = 0;
                            currentRow += 2;
                        }
                        else if ( getsOwnLine && currentColumn != 0 )
                        {
                            currentColumn = 0;
                            currentRow++;
                        }
                    }
                    lastSideType = group.Side;
                    float elementX = this.Element.Alignment.XAlignment.Offset;
                    float elementY = this.Element.Alignment.YAlignment.Offset;
                    float x = elementX + ( currentColumn * adjustedButtonWidth );
                    float y = elementY + ( currentRow * adjustedButtonHeight );
                    if ( elementX != x || elementY != y )
                    {
                        item.Element.Alignment.XAlignment.Offset = x;
                        item.Element.Alignment.YAlignment.Offset = y;
                        item.Element.UpdatePositionAndSize();
                    }
                    if ( getsOwnLine )
                    {
                        currentColumn = 0;
                        nextItemGetsNewRow = true;
                    }
                    else
                    {
                        currentColumn++;
                        if ( currentColumn >= totalColumns )
                        {
                            nextItemGetsNewRow = true;
                            currentColumn = 0;
                        }
                    }

                    if(group.Side == EntityGroupSideType.Enemy && !placedEnemyLabel)
                    {
                        tEnemySummary.Instance.Element.Alignment.YAlignment.Offset = y - tEnemySummary.Instance.Element.Height;
                        placedEnemyLabel = true;
                    }
                    if ( group.Side == EntityGroupSideType.Mine && !placedPlayerLabel )
                    {
                        tPlayerSummary.Instance.Element.Alignment.YAlignment.Offset = y - tPlayerSummary.Instance.Element.Height;
                        placedPlayerLabel = true;
                    }
                    if ( group.Side == EntityGroupSideType.Allied && !placedAllyLabel )
                    {
                        tAllySummary.Instance.Element.Alignment.YAlignment.Offset = y - tAllySummary.Instance.Element.Height;
                        placedAllyLabel = true;
                    }
                }

                //hide any extras
                for ( int i = currentImageListIndex; i < this.Element.Images.Count; i++ )
                {
                    item = this.Element.Images[i].Controller as bOutlineItem;
                    item.EntityGroup = null;
                }
            }

            public override void SetElement( ArcenUI_Element Element )
            {
                this.Element = (ArcenUI_ImageButtonSet)Element;
            }
        }

        private class bOutlineItem : ImageButtonAbstractBase
        {
            public ArcenUI_ImageButton Element;
            public EntityGroup EntityGroup;

            public bOutlineItem() { }
            
            public const int INDEX_CLOAKED = 0;
            public const int INDEX_RELOADING = 1;
            public const int INDEX_SHIELDED = 2;
            public const int INDEX_UNDER_FIRE = 3;
            public const int INDEX_ICON_BORDER = 4;
            public const int INDEX_ICON = 5;
            public const int INDEX_FLAIR = 6;
            public const int INDEX_MARK_LEVEL = 7;

            public override void UpdateContent( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( this.EntityGroup == null )
                    return;

                int debugStage = -1;
                try
                {
                    debugStage = 0;
                    int cnt = this.EntityGroup.EntityCount;
                    if ( cnt > 0 && cnt  < Window_InGameOutlineSidebar.Sprite_Backgrounds.Length )
                        Image.UpdateWith( Window_InGameOutlineSidebar.Sprite_Backgrounds[cnt], true );
                    
                    debugStage = 1;
                    GameEntityTypeData typeData = this.EntityGroup.TypeData;

                    debugStage = 2;

                    SubImages[INDEX_ICON].WrapperedImage.UpdateWith( typeData.GUISprite_Icon_White, true );
                    //if ( this.EntityGroup.ActualEntities.Count <= 0 )
                    //    SubImages[INDEX_ICON].WrapperedImage.SetColor( ColorMath.White );
                    //else
                    //    SubImages[INDEX_ICON].WrapperedImage.SetColor( this.EntityGroup.ActualEntities[0].Side.WorldSide.TeamColor.UnityColor );
                    switch ( this.EntityGroup.Side )
                    {
                        case EntityGroupSideType.Mine:
                            SubImages[INDEX_ICON].WrapperedImage.UpdateWith( typeData.GUISprite_Icon_Mine, true );
                            break;
                        case EntityGroupSideType.Allied:
                            SubImages[INDEX_ICON].WrapperedImage.UpdateWith( typeData.GUISprite_Icon_Allied, true );
                            break;
                        default:
                            SubImages[INDEX_ICON].WrapperedImage.UpdateWith( typeData.GUISprite_Icon_Enemy, true );
                            break;
                    }

                    debugStage = 3;
                    SubImages[INDEX_ICON_BORDER].WrapperedImage.UpdateWith( typeData.GUISprite_IconBorder, true );
                    debugStage = 4;
                    SubImages[INDEX_FLAIR].WrapperedImage.UpdateWith( typeData.GUISprite_Flair, true );

                    debugStage = 5;

                    bool reloading = false;
                    bool underFire = false;
                    bool shielded = false;
                    bool cloaked = false;

                    List<GameEntity> entities = this.EntityGroup.ActualEntities;
                    GameEntity entity;
                    for ( int i = 0; i < entities.Count; i++ )
                    {
                        debugStage = 6;

                        entity = entities[i];
                        if ( entity.HasBeenRemovedFromSim )
                            continue;

                        debugStage = 7;

                        if ( !reloading )
                        {
                            for ( int j = 0; j < entity.Systems.Count; j++ )
                            {
                                debugStage = 5;
                                EntitySystem system = entity.Systems[j];
                                debugStage = 6;
                                if ( system.TimeUntilNextShot <= 0 )
                                    continue;
                                reloading = true;
                                break;
                            }
                        }
                        debugStage = 8;
                        if ( !underFire )
                            underFire = entity.RepairDelaySeconds > 0;
                        debugStage = 9;
                        if ( !shielded )
                            shielded = entity.ProtectingShieldIDs.Count > 0 || entity.GetCurrentShieldPoints() > 0;
                        debugStage = 10;
                        if ( !cloaked )
                            cloaked = entity.GetCurrentCloakingPoints() > 0;
                    }
                    debugStage = 11;
                    SubImages[INDEX_CLOAKED].WrapperedImage.UpdateToShowOrHide( cloaked ? ArcenUIWrapperedUnityImage.ShowOrHideStatus.Show :
                        ArcenUIWrapperedUnityImage.ShowOrHideStatus.Hide );
                    debugStage = 12;
                    SubImages[INDEX_RELOADING].WrapperedImage.UpdateToShowOrHide( reloading ? ArcenUIWrapperedUnityImage.ShowOrHideStatus.Show :
                        ArcenUIWrapperedUnityImage.ShowOrHideStatus.Hide );
                    debugStage = 13;
                    SubImages[INDEX_SHIELDED].WrapperedImage.UpdateToShowOrHide( shielded ? ArcenUIWrapperedUnityImage.ShowOrHideStatus.Show :
                        ArcenUIWrapperedUnityImage.ShowOrHideStatus.Hide );
                    debugStage = 14;
                    SubImages[INDEX_UNDER_FIRE].WrapperedImage.UpdateToShowOrHide( underFire ? ArcenUIWrapperedUnityImage.ShowOrHideStatus.Show :
                        ArcenUIWrapperedUnityImage.ShowOrHideStatus.Hide );

                    debugStage = 15;

                    int markLevel = typeData.Balance_MarkLevel.Ordinal;
                    if ( markLevel < 0 || markLevel > 5 )
                        markLevel = 0;
                    switch ( typeData.SpecialType )
                    {
                        case SpecialEntityType.AIKingUnit:
                        case SpecialEntityType.HumanKingUnit:
                            markLevel = 0;
                            break;
                    }
                    SubImages[INDEX_MARK_LEVEL].WrapperedImage.UpdateWith( markLevel <= 0 ? null : 
                        Window_InGameOutlineSidebar.Sprite_MarkLevels[markLevel], true );

                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Exception in WriteRawDrawInstructions at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }
            }

            public override void SetElement( ArcenUI_Element Element )
            {
                this.Element = (ArcenUI_ImageButton)Element;
            }

            public void DoForShips( GameEntity.ProcessorDelegate Processor )
            {
                if ( Processor == null )
                    return;
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                planet.Combat.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity entity )
                {
                    if ( this.EntityGroup == null )
                        return DelReturn.Continue;
                    if ( !this.EntityGroup.ActualEntities.Contains( entity ) )
                        return DelReturn.Continue; // just making sure it's still on the planet, alive, etc
                    if ( Processor( entity ) == DelReturn.Break )
                        return DelReturn.Break;
                    return DelReturn.Continue;
                } );
            }

            public override MouseHandlingResult HandleClick()
            {
                if ( this.EntityGroup == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                bool clearSelectionFirst = false;
                bool unselectingInstead = false;
                if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Additive ) )
                { }
                else if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Subtractive ) )
                {
                    unselectingInstead = true;
                }
                else
                {
                    clearSelectionFirst = true;
                }

                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                bool foundOne = false;

                this.DoForShips( delegate ( GameEntity entity )
                 {
                     if ( entity.Combat.Planet != planet )
                         return DelReturn.Continue;
                     if ( !foundOne )
                     {
                         foundOne = true;
                         if ( clearSelectionFirst )
                         {
                             if ( !entity.GetMayBeSelected() || entity.GetIsSelected() )
                                 Engine_AIW2.Instance.PresentationLayer.CenterPlanetViewOnEntity( entity, false );
                             Engine_AIW2.Instance.ClearSelection( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy );
                         }
                     }
                     if ( entity.GetMayBeSelected() )
                     {
                         if ( unselectingInstead )
                             entity.Unselect();
                         else
                             entity.Select();
                     }
                     return DelReturn.Continue;
                 } );
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                if ( this.EntityGroup != null )
                {
                    if ( this.EntityGroup.ActualEntities.Count == 1 )
                    {
                        if ( this.EntityGroup.ActualEntities.Count == 1 )
                        {
                            GameEntity.CurrentlyHoveredOver = this.EntityGroup.ActualEntities[0];
                            return;
                        }
                    }
                    GameEntityTypeData.CurrentlyHoveredOver = this.EntityGroup.TypeData;
                }
            }

            public override bool GetShouldBeHidden()
            {
                return this.EntityGroup == null || this.EntityGroup.EntityCount <= 0;
            }
        }

        public class EntityGroup
        {
            public EntityGroupSideType Side;
            public GameEntityTypeData TypeData;
            public int EntityCount = 0;
            public List<GameEntity> ActualEntities = new List<GameEntity>();

            private EntityGroup()
            { }

            private static List<EntityGroup> pool = new List<EntityGroup>();
            private static int lastPoolIndex = -1;
            public static void ResetPool()
            {
                if ( lastPoolIndex >= pool.Count )
                    lastPoolIndex = pool.Count - 1;

                for ( int i = 0; i <= lastPoolIndex; i++ )
                    pool[i].Reset();

                lastPoolIndex = -1;
            }

            public static EntityGroup GetNextFromPool()
            {
                lastPoolIndex++;
                if ( lastPoolIndex < pool.Count )
                    return pool[lastPoolIndex];
                EntityGroup group = new EntityGroup();
                pool.Add( group );
                return group;
            }

            private void Reset()
            {
                this.EntityCount = 0;
                this.ActualEntities.Clear();
            }
        }

        public enum EntityGroupSideType
        {
            Mine = 0,
            Enemy = 1,
            Allied,
            Unknown
        }
    }
}