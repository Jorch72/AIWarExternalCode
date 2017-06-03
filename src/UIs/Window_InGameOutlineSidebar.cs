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
        private readonly ArcenSprite Sprite_Border;
        private readonly ArcenSprite Sprite_Reloading;
        private readonly ArcenSprite Sprite_UnderFire;
        private readonly ArcenSprite Sprite_Shielded;
        private readonly ArcenSprite Sprite_Cloaked;

        public Window_InGameOutlineSidebar()
        {
            Instance = this;
            this.OnlyShowInGame = true;

            this.Sprite_Border = ArcenSprite.Get( "Official/OutlineDecal_Border" );
            this.Sprite_Reloading = ArcenSprite.Get( "Official/OutlineDecal_Reloading" );
            this.Sprite_UnderFire = ArcenSprite.Get( "Official/OutlineDecal_UnderFire" );
            this.Sprite_Shielded = ArcenSprite.Get( "Official/OutlineDecal_Shielded" );
            this.Sprite_Cloaked = ArcenSprite.Get( "Official/OutlineDecal_Cloaked" );
        }

        private const int PREFER_LEFT = -1;
        private const int PREFER_RIGHT = 1;
        private const int PREFER_NEITHER = 0;


        public class bsOutlineItems : IArcenUI_RawImageSet_Controller
        {
            private ArcenUI_RawImageSet Element;
            public List<EntityGroup> groups = new List<EntityGroup>();
            private readonly ArcenSparseLookup<GameEntityTypeData, List<bOutlineItem>> tempItemsByBaseType = new ArcenSparseLookup<GameEntityTypeData, List<bOutlineItem>>();
            public void OnUpdate()
            {
                //WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                //if ( localSide == null )
                //    return;

                //for(int i = 0; i < Element.RawImages.Count;i++)
                //{
                //    bOutlineItem buttonAsType = (bOutlineItem)Element.RawImages[i].Controller;
                //    buttonAsType.ClearForNextUpdate();
                //}

                //Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                //if ( !planet.HumansHaveBasicIntel )
                //    return;

                //float columnWidth = 32 / ArcenUI.Instance.PixelsPerPercent_X;
                //float rowHeight = 32 / ArcenUI.Instance.PixelsPerPercent_Y;
                //float iconWidth = columnWidth * 0.9f;
                //float iconHeight = rowHeight * 0.9f;
                //int currentRow = 0;
                //int currentColumn = 0;
                //int totalColumns = Mathf.FloorToInt( this.Element.Width / columnWidth );
                //groups.Clear();

                //planet.Combat.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity entity )
                // {
                //     EntityGroup group;
                //     for ( int i = groups.Count - 1; i >= 0; i-- )
                //     {
                //         group = groups[i];
                //         if ( group.TypeData == entity.TypeData && group.Side == entity.Side.WorldSide )
                //         {
                //             group.EntityCount++;
                //             group.ActualEntities.Add( entity );
                //             return DelReturn.Continue;
                //         }
                //     }
                //     group = EntityGroup.GetNextFromPool();
                //     group.TypeData = entity.TypeData;
                //     group.Side = entity.Side.WorldSide;
                //     group.EntityCount = 1;
                //     group.ActualEntities.Add( entity );
                //     groups.Add( group );

                //     return DelReturn.Continue;
                // } );

                //groups.Sort( delegate ( EntityGroup Left, EntityGroup Right )
                // {
                //     if ( Left.Side.Type != Right.Side.Type )
                //     {
                //         if ( Left.Side.Type < Right.Side.Type )
                //             return PREFER_LEFT;
                //         else
                //             return PREFER_RIGHT;
                //     }

                //     if ( Left.Side != Right.Side )
                //     {
                //         if ( Left.Side.SideIndex < Right.Side.SideIndex )
                //             return PREFER_LEFT;
                //         else
                //             return PREFER_RIGHT;
                //     }

                //     if ( Left.TypeData != Right.TypeData && Left.TypeData.BalanceStats.StrengthPerShip != Right.TypeData.BalanceStats.StrengthPerShip )
                //     {
                //         if ( Left.TypeData.BalanceStats.StrengthPerShip.RawValue != Right.TypeData.BalanceStats.StrengthPerShip.RawValue )
                //         {
                //             if ( Left.TypeData.BalanceStats.StrengthPerShip.RawValue < Right.TypeData.BalanceStats.StrengthPerShip.RawValue )
                //                 return PREFER_RIGHT;
                //             else
                //                 return PREFER_LEFT;
                //         }
                //     }

                //     if ( Left.TypeData.CopiedFrom != Right.TypeData.CopiedFrom )
                //     {
                //         if ( Left.TypeData.CopiedFrom.RowIndex < Right.TypeData.CopiedFrom.RowIndex )
                //             return PREFER_LEFT;
                //         else
                //             return PREFER_RIGHT;
                //     }

                //     if ( Left.TypeData != Right.TypeData )
                //     {
                //         if ( Left.TypeData.RowIndex < Right.TypeData.RowIndex )
                //             return PREFER_LEFT;
                //         else
                //             return PREFER_RIGHT;
                //     }

                //     return PREFER_NEITHER;
                // } );

                //int maxCount = 1;
                //float iconsToDisplay = groups.Count;
                //while ( iconsToDisplay > 50 )
                //{
                //    maxCount *= 2;
                //    iconsToDisplay /= 2f;
                //}
                //for ( int i = 0; i < Element.RawImages.Count; i++ )
                //    ( (bOutlineItem)Element.RawImages[i].Controller ).MaxCount = maxCount;

                //int pairCount = tempItemsByBaseType.GetPairCount();
                //for(int i = 0; i < pairCount;i++ )
                //{
                //    tempItemsByBaseType.GetPairByIndex( i ).Value.Clear();
                //}
                //tempItemsByBaseType.Clear();
                //int emptyIndex = 0;
                //WorldSideType lastSideType = WorldSideType.Length;
                //for ( int groupIndex = groups.Count - 1; groupIndex >= 0; groupIndex-- )
                //{
                //    EntityGroup group = groups[groupIndex];
                //    GameEntityTypeData baseType = group.TypeData;
                //    if ( baseType.CopiedFrom != null )
                //        baseType = baseType.CopiedFrom;
                //    if ( !tempItemsByBaseType.GetHasKey( baseType ) )
                //        tempItemsByBaseType[baseType] = new List<bOutlineItem>();
                //    List<bOutlineItem> itemsByBaseType = tempItemsByBaseType[baseType];
                //    bool foundAPlace = false;
                //    for ( int i = 0; i < itemsByBaseType.Count; i++ )
                //    {
                //        bOutlineItem buttonAsType = itemsByBaseType[i];
                //        if ( buttonAsType.TryToAdd( group, false ) )
                //        {
                //            foundAPlace = true;
                //            break;
                //        }
                //    }
                //    if ( foundAPlace )
                //        continue;
                //    bOutlineItem item = null;
                //    for ( ; emptyIndex < Element.RawImages.Count; emptyIndex++ )
                //    {
                //        bOutlineItem buttonAsType = (bOutlineItem)Element.RawImages[emptyIndex].Controller;
                //        if ( buttonAsType.EntityGroups.Count > 0 )
                //            continue;
                //        if ( buttonAsType.TryToAdd( group, true ) )
                //        {
                //            item = buttonAsType;
                //            itemsByBaseType.Add( item );
                //            break;
                //        }
                //    }
                //    if ( item == null )
                //    {
                //        item = new bOutlineItem();
                //        item.MaxCount = maxCount;
                //        item.TryToAdd( group, true );
                //        itemsByBaseType.Add( item );
                //        Vector2 dummy = ArcenPoint.ZeroZeroPoint.ToVector2();
                //        Vector2 size;
                //        size.x = iconWidth;
                //        size.y = iconHeight;
                //        Element.AddRawImage( item, size, dummy );
                //    }
                //    bool getsOwnLine = false;
                //    switch ( group.TypeData.SpecialType )
                //    {
                //        case SpecialEntityType.AIKingUnit:
                //        case SpecialEntityType.HumanKingUnit:
                //            getsOwnLine = true;
                //            break;
                //    }
                //    if ( group.TypeData.Tags.Contains( "Flagship" ) )
                //        getsOwnLine = true;
                //    if ( lastSideType != WorldSideType.Length )
                //    {
                //        if ( lastSideType != group.Side.Type )
                //        {
                //            currentColumn = 0;
                //            currentRow += 2;
                //        }
                //        else if ( getsOwnLine && currentColumn != 0 )
                //        {
                //            currentColumn = 0;
                //            currentRow++;
                //        }
                //    }
                //    lastSideType = group.Side.Type;
                //    float x = this.Element.X + ( currentColumn * columnWidth );
                //    float y = this.Element.Y + ( currentRow * rowHeight );
                //    if ( item.Element.X != x || item.Element.Y != y )
                //    {
                //        item.Element.X = x;
                //        item.Element.Y = y;
                //        item.Element.UpdatePositionAndSize();
                //    }
                //    if ( getsOwnLine )
                //    {
                //        currentColumn = 0;
                //        currentRow++;
                //    }
                //    else
                //    {
                //        currentColumn++;
                //        if ( currentColumn >= totalColumns )
                //        {
                //            currentRow++;
                //            currentColumn = 0;
                //        }
                //    }
                //}
            }

            public void SetElement( ArcenUI_Element Element )
            {
                this.Element = (ArcenUI_RawImageSet)Element;
            }
        }

        private class bOutlineItem : IArcenUI_RawImage_Controller
        {
            public ArcenUI_RawImage Element;
            public readonly List<EntityGroup> EntityGroups = new List<EntityGroup>();
            public int Count;
            public int MaxCount = 1;

            public bOutlineItem() { }

            public void ClearForNextUpdate()
            {
                //lock ( this.EntityGroups )
                {
                    this.EntityGroups.Clear();
                    this.Count = 0;
                }
            }

            public void WriteRawDrawInstructions( ArcenRawDrawInstructionList Instructions )
            {
                //lock ( this.EntityGroups )
                {
                    int debugStage = -1;
                    try
                    {
                        for ( int i = 0; i < this.EntityGroups.Count; i++ )
                        {
                            debugStage = 0;
                            EntityGroup group = this.EntityGroups[i];
                            if ( group == null || group.TypeData == null )
                                continue;
                            debugStage = 1;
                            //CHRIS_TODO: re add
                            //if ( entity.HasBeenRemovedFromSim )
                            //    continue;
                            debugStage = 2;
                            GameEntityTypeData typeData = group.TypeData;
                            debugStage = 3;
                            if ( typeData.SpriteIcon == null || typeData.SpriteIcon.Parent == null )
                                continue;
                            debugStage = 4;
                            bool getsBorder = true;
                            //bool reloading = false;
                            //for ( int j = 0; j < entity.Systems.Count; j++ )
                            //{
                            //    debugStage = 5;
                            //    EntitySystem system = entity.Systems[j];
                            //    debugStage = 6;
                            //    if ( system.TimeUntilNextShot <= 0 )
                            //        continue;
                            //    reloading = true;
                            //    break;
                            //}
                            debugStage = 7;
                            //bool underFire = entity.RepairDelaySeconds > 0;
                            debugStage = 8;
                            //bool shielded = entity.ProtectingShieldIDs.Count > 0 || entity.GetCurrentShieldPoints() > 0;
                            debugStage = 9;
                            //bool cloaked = entity.GetCurrentCloakingPoints() > 0;

                            ArcenSprite placeholder = Window_InGameOutlineSidebar.Instance.Sprite_Border;
                            Material placeholderMat = null;
                            debugStage = 10;

                            ArcenSpriteMaterial material = typeData.SpriteIcon.Parent.DefaultMaterial; //CHRIS_TODO entity.Side.WorldSide.TeamColor.GetIconMaterial( typeData.SpriteIcon.Parent, true );
                            if ( material == null )
                                continue;
                            debugStage = 11;

                            AddLayer( Instructions, placeholder, placeholderMat, getsBorder, typeData.SpriteIcon, material.Mat );
                            debugStage = 12;
                            AddLayer( Instructions, placeholder, placeholderMat, getsBorder, placeholder, null );
                            debugStage = 13;
                            //AddLayer( Instructions, placeholder, placeholderMat, reloading, Window_InGameOutlineSidebar.Instance.Sprite_Reloading, null );
                            //debugStage = 14;
                            //AddLayer( Instructions, placeholder, placeholderMat, underFire, Window_InGameOutlineSidebar.Instance.Sprite_UnderFire, null );
                            //debugStage = 15;
                            //AddLayer( Instructions, placeholder, placeholderMat, shielded, Window_InGameOutlineSidebar.Instance.Sprite_Shielded, null );
                            //debugStage = 16;
                            //AddLayer( Instructions, placeholder, placeholderMat, cloaked, Window_InGameOutlineSidebar.Instance.Sprite_Cloaked, null );
                            //debugStage = 17;
                            break;
                        }
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLog( "Exception in WriteRawDrawInstructions at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                    }
                }
            }

            private static void AddLayer( ArcenRawDrawInstructionList Instructions, ArcenSprite placeholder, Material placeholderMat, bool useRealImage, ArcenSprite realImage, Material realMat )
            {
                Instructions.StartNewInstruction();
                if ( useRealImage )
                {
                    Instructions.SetSpriteOnCurrentInstruction( realImage );
                    Instructions.SetMatOnCurrentInstruction( realMat );
                }
                else
                {
                    Instructions.SetSpriteOnCurrentInstruction( placeholder );
                    Instructions.SetMatOnCurrentInstruction( placeholderMat );
                }
            }

            public void SetElement( ArcenUI_Element Element )
            {
                this.Element = (ArcenUI_RawImage)Element;
            }

            public bool TryToAdd( EntityGroup group, bool AllowTakingBlankElement )
            {
                if ( this.EntityGroups.Count <= 0 && AllowTakingBlankElement )
                {
                    this.EntityGroups.Add( group );
                    AddToCounts( group );
                    return true;
                }
                if ( this.Count >= this.MaxCount )
                    return false;
                for ( int i = 0; i < this.EntityGroups.Count; i++ )
                {
                    EntityGroup Other = this.EntityGroups[i];
                    if ( Other.TypeData != group.TypeData &&
                        Other.TypeData.CopiedFrom != group.TypeData &&
                        Other.TypeData != group.TypeData.CopiedFrom &&
                        Other.TypeData.CopiedFrom != group.TypeData.CopiedFrom )
                        continue;
                    if ( Other.Side != group.Side )
                        continue;
                    this.EntityGroups.Add( group );
                    AddToCounts( group );
                    return true;
                }
                return false;
            }

            private void AddToCounts( EntityGroup group )
            {
                this.Count += group.EntityCount;
            }

            public void DoForShips( GameEntity.ProcessorDelegate Processor )
            {
                if ( Processor == null )
                    return;
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                planet.Combat.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity entity )
                {
                    bool containsAny = false;
                    EntityGroup group;
                    for ( int i = this.EntityGroups.Count - 1; i >= 0; i-- )
                    {
                        group = this.EntityGroups[i];
                        if ( group.ActualEntities.Contains( entity ))
                        {
                            containsAny = true;
                            break;
                        }
                    }
                    if ( !containsAny ) // just making sure it's still on the planet, alive, etc
                        return DelReturn.Continue;
                    if ( Processor( entity ) == DelReturn.Break )
                        return DelReturn.Break;
                    return DelReturn.Continue;
                } );
            }

            public void HandleClick()
            {
                if ( this.EntityGroups.Count <= 0 )
                    return;

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
                             Engine_AIW2.Instance.ClearSelection();
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
            }

            public void HandleMouseover()
            {
                if ( this.EntityGroups.Count > 0 )
                {
                    if ( this.EntityGroups.Count == 1 )
                    {
                        EntityGroup group = this.EntityGroups[0];
                        if ( group.ActualEntities.Count == 1 )
                        {
                            GameEntity.CurrentlyHoveredOver = group.ActualEntities[0];
                            return;
                        }
                    }
                    GameEntityTypeData.CurrentlyHoveredOver = this.EntityGroups[0].TypeData;
                }
            }

            public void OnUpdate()
            {
            }
        }

        public class EntityGroup
        {
            public WorldSide Side;
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
    }
}