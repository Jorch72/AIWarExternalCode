using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameEntityTooltipPanel : WindowControllerAbstractBase
    {
        public Window_InGameEntityTooltipPanel()
        {
            this.OnlyShowInGame = true;
        }

        public class bPanel : ImageButtonAbstractBase
        {
            public static bPanel Instance;
            public bPanel() { Instance = this; }

            public ArcenUI_ImageButton Element;
            public GameEntityTypeData TypeDoingTheBuilding;
            public int ColumnIndex;
            public Mode PanelMode;
            public bool HaveGottenScaleInfo;
            public float XScale;
            public float YScale;

            private ImageId LastImageIDWritten;
            private TextId LastTextIDWritten;
            private bool LastWrittenWasImageInsteadOfText;
            private ArcenUI_Image.SubImageGroup SubImages;
            private SubTextGroup SubTexts;

            public enum Mode
            {
                None,
                Build,
                Tech,
                ActualUnit,
            }

            public enum ImageId
            {
                MarkLevel,
                Locked,
                Unlocked,
                Science,
                Fuel,
                Power,
                Metal,
                Unused1,
                Cap,
                Attack,
                Range,
                EngineDamage,
                Paralysis,
                Ion,
                Implosion,
                Unused2,
                Unused3,
                Nuclear,
                Defense,
                Speed,
                EngineDamageResistance,
                ParalysisResistance,
                IonResistance,
                ImplosionResistance,
                NuclearResistance,
                Cloak,
                Tachyon,
                Tractor,
                TractorResistance,
                Gravity,
                GravityResistance,
                Strength,
            }

            public enum TextId
            {
                PanelSummary,
                SubjectSummary,
                Science,
                FuelOrPower,
                Metal,
                Cap,
                Attack,
                Defense,
                Description,
                Strength,
            }

            public override void UpdateContent( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup _SubImages, SubTextGroup _SubTexts )
            {
                this.SubImages = _SubImages;
                this.SubTexts = _SubTexts;
                GameEntityTypeData typeData = this.TypeToBuild;
                if ( typeData == null )
                    return;

                WorldSide localSide = World_AIW2.Instance.GetLocalPlayerSide();
                //Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                //CombatSide localCombatSide = planet.Combat.GetSideForWorldSide( localSide );

                if ( GameEntity.CurrentlyHoveredOver != null )
                    this.PanelMode = Mode.ActualUnit;
                else if ( Window_InGameTechTabMenu.Instance.IsOpen )
                    this.PanelMode = Mode.Tech;
                else if ( Window_InGameBuildTabMenu.Instance.IsOpen )
                    this.PanelMode = Mode.Build;

                TextId.SubjectSummary.Set( typeData.Name, string.Empty );

                try
                {
                    int markLevel = typeData.Balance_MarkLevel == null ? 0 : typeData.Balance_MarkLevel.Ordinal;
                    if ( markLevel <= 0 )
                        ImageId.MarkLevel.Hide();
                    else
                        ImageId.MarkLevel.Set( Window_InGameOutlineSidebar.Sprite_MarkLevels[markLevel], string.Empty );

                    if ( this.PanelMode == Mode.Tech && typeData.TechPrereq != null )
                    {
                        bool unlocked = localSide.GetHasResearched( typeData.TechPrereq );
                        ImageId.Locked.ChangeVisibility( !unlocked );
                        ImageId.Unlocked.ChangeVisibility( unlocked );
                        ImageId.Science.Show();
                        TextId.Science.Set( typeData.TechPrereq.ScienceCost, string.Empty );
                    }
                    else
                    {
                        ImageId.Locked.Hide();
                        ImageId.Unlocked.Hide();
                        ImageId.Science.Hide();
                        TextId.Science.Hide();
                    }

                    if ( typeData.BalanceStats.SquadFuelConsumption > 0 )
                    {
                        ImageId.Fuel.Show();
                        ImageId.Power.Hide();
                        TextId.FuelOrPower.Set( typeData.BalanceStats.SquadFuelConsumption, string.Empty );
                    }
                    else if ( typeData.BalanceStats.SquadPowerConsumption > 0 )
                    {
                        ImageId.Fuel.Hide();
                        ImageId.Power.Show();
                        TextId.FuelOrPower.Set( typeData.BalanceStats.SquadPowerConsumption, string.Empty );
                    }
                    else
                    {
                        ImageId.Fuel.Hide();
                        ImageId.Power.Hide();
                        TextId.FuelOrPower.Hide();
                    }

                    if ( typeData.BalanceStats.SquadMetalCost > 0 )
                    {
                        ImageId.Metal.Show();
                        TextId.Metal.Set( typeData.BalanceStats.SquadMetalCost, string.Empty );
                    }
                    else
                    {
                        ImageId.Metal.Hide();
                        TextId.Metal.Hide();
                    }

                    int cap = typeData.BalanceStats.SquadsPerCap;
                    if ( cap <= 0 )
                    {
                        ImageId.Cap.Hide();
                        TextId.Cap.Hide();
                    }
                    else
                    {
                        ImageId.Cap.Show();
                        TextId.Cap.Set( cap, string.Empty );
                    }

                    ImageId.Strength.Show();
                    TextId.Strength.Set( typeData.BalanceStats.StrengthPerSquad.IntValue, string.Empty );

                    SystemEntry.SubEntry mainOffensiveWeaponSystemSubEntry = null;
                    EntitySystemTypeData cloakSystem = null;
                    EntitySystemTypeData tachyonSystem = null;
                    EntitySystemTypeData tractorSystem = null;
                    EntitySystemTypeData gravitySystem = null;
                    for (int i = 0; i < typeData.SystemEntries.Count;i++)
                    {
                        SystemEntry entry = typeData.SystemEntries[i];
                        if ( entry.SubEntries.Count <= 0 )
                            continue;
                        SystemEntry.SubEntry subEntry = entry.SubEntries[0];
                        if ( mainOffensiveWeaponSystemSubEntry == null && subEntry.GetDPS() > 0 )
                            mainOffensiveWeaponSystemSubEntry = subEntry;
                        EntitySystemTypeData systemData = subEntry.SystemData;
                        if ( cloakSystem == null && subEntry.BalanceStats.SquadCloakingPoints > 0 )
                            cloakSystem = systemData;
                        if ( tachyonSystem == null && subEntry.BalanceStats.TachyonPoints > 0 )
                            tachyonSystem = systemData;
                        if ( tractorSystem == null && subEntry.BalanceStats.TractorPoints > 0 )
                            tractorSystem = systemData;
                        if ( gravitySystem == null && subEntry.BalanceStats.GravityPoints > 0 )
                            gravitySystem = systemData;
                    }

                    if ( mainOffensiveWeaponSystemSubEntry != null  )
                    {
                        SystemEntry.SubEntry systemSubEntry = mainOffensiveWeaponSystemSubEntry;
                        EntitySystemTypeData systemData = systemSubEntry.SystemData;
                        Balance_WeaponType weaponData = systemData.Balance_WeaponType;

                        ImageId.Attack.Set( systemData.Balance_WeaponType.CounterType );
                        TextId.Attack.Set( systemSubEntry.GetDPS().IntValue, systemData.Balance_WeaponType.CounterType.StatTooltip );
                        ImageId.Range.Set( weaponData.Range );
                        ImageId.EngineDamage.Set( weaponData.Balance_EngineDamageType );
                        ImageId.Paralysis.Set( weaponData.Balance_ParalysisClass );
                        ImageId.Ion.Set( weaponData.Balance_IonClass );
                        ImageId.Implosion.Set( weaponData.Balance_ImplosionClass );
                        ImageId.Nuclear.Set( systemData.Balance_NuclearClass );
                    }
                    else
                    {
                        ImageId.Attack.Hide();
                        TextId.Attack.Hide();
                        ImageId.Range.Hide();
                        ImageId.EngineDamage.Hide();
                        ImageId.Paralysis.Hide();
                        ImageId.Ion.Hide();
                        ImageId.Implosion.Hide();
                        ImageId.Nuclear.Hide();
                    }

                    ImageId.Defense.Set( typeData.Balance_Defense );

                    int totalMaxHP = ( typeData.BalanceStats.HullPoints + typeData.BalanceStats.ShieldPoints ) * typeData.Balance_ShipsPerSquad;
                    GameEntity entity = GameEntity.CurrentlyHoveredOver;
                    if ( this.PanelMode == Mode.ActualUnit && entity != null && ( entity.HullPointsLost > 0 || entity.ShieldPointsLost > 0 || entity.SquadShipsLost > 0 || entity.SelfBuildingMetalRemaining > 0 ) )
                    {
                        float percent;
                        if ( entity.SelfBuildingMetalRemaining <= 0 )
                        {
                            int totalCurrentHP = ( typeData.BalanceStats.HullPoints + typeData.BalanceStats.ShieldPoints ) * ( typeData.Balance_ShipsPerSquad - 1 );
                            totalCurrentHP += ( entity.GetCurrentHullPoints() + entity.GetCurrentShieldPoints() );
                            percent = ( (float)totalCurrentHP / (float)totalMaxHP ) * 100;
                        }
                        else
                        {
                            percent = ( 1f - ( (float)entity.SelfBuildingMetalRemaining / (float)typeData.BalanceStats.SquadMetalCost ) ) * 100;
                        }
                        string percentMask;
                        if( entity.SelfBuildingMetalRemaining > 0 || entity.HasNotYetBeenFullyClaimed )
                            percentMask = "#,##0.0";
                        else
                            percentMask = "#,##0";
                        int totalHPForDisplay = totalMaxHP;
                        string suffix = ArcenExternalUIUtilities.GetRoundedNumberWithSuffix( ref totalHPForDisplay, true );
                        ArcenCharacterBuffer textBuffer = new ArcenCharacterBuffer();
                        textBuffer.Add( totalHPForDisplay.ToString( "#,##0" ) );
                        textBuffer.Add( suffix );
                        textBuffer.Add( " (" );
                        textBuffer.Add( percent.ToString( percentMask ) );
                        textBuffer.Add( "%)" );
                        string text = textBuffer.ToString();
                        TextId.Defense.Set( text, typeData.Balance_Defense.StatTooltip );
                    }
                    else
                        TextId.Defense.Set( totalMaxHP, typeData.Balance_Defense.StatTooltip );

                    ImageId.Speed.Set( typeData.Balance_Speed );
                    ImageId.EngineDamageResistance.Set( typeData.Balance_EngineHealthType );
                    ImageId.ParalysisResistance.Set( typeData.Balance_ParalysisResistance );
                    ImageId.IonResistance.Set( typeData.Balance_IonResistance );
                    ImageId.ImplosionResistance.Set( typeData.Balance_ImplosionResistance );
                    ImageId.NuclearResistance.Set( typeData.Balance_NuclearResistance );

                    ImageId.Cloak.Set( cloakSystem == null ? null : cloakSystem.Balance_CloakingType );
                    ImageId.Tachyon.Set( tachyonSystem == null ? null : tachyonSystem.Balance_TachyonType );
                    ImageId.Tractor.Set( tractorSystem == null ? null : tractorSystem.Balance_TractorType );
                    ImageId.TractorResistance.Set( typeData.Balance_TractorResistanceType );
                    ImageId.Gravity.Set( gravitySystem == null ? null : gravitySystem.Balance_GravityType );
                    ImageId.GravityResistance.Set( typeData.Balance_GravityResistanceType );

                    TextId.Description.Set( typeData.Description, string.Empty );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Exception in UpdateContent after " + ( LastWrittenWasImageInsteadOfText ? "image " + LastImageIDWritten : "text " + LastTextIDWritten ) + ":" + e.ToString(), Verbosity.ShowAsError );
                }
            }

            public void SetSprite( ImageId imageId, Sprite sprite, string tooltip )
            {
                SubImages[(int)imageId].WrapperedImage.UpdateWith( sprite, true );
                SubImages[(int)imageId].Tooltip = tooltip;
                LastImageIDWritten = imageId;
                LastWrittenWasImageInsteadOfText = true;
            }

            public void SetSprite( ImageId imageId, string bundleName, string path, string tooltip )
            {
                SubImages[(int)imageId].WrapperedImage.SetBundleAndPathInBundle( bundleName, path );
                SubImages[(int)imageId].Tooltip = tooltip;
                LastImageIDWritten = imageId;
                LastWrittenWasImageInsteadOfText = true;
            }

            public void SetShown( ImageId imageId, bool shown )
            {
                SubImages[(int)imageId].WrapperedImage.UpdateToShowOrHide( shown );
                LastImageIDWritten = imageId;
                LastWrittenWasImageInsteadOfText = true;
            }

            public void HideText( TextId textId )
            {
                SubTexts[(int)textId].Text.StartWritingToBuffer();
                SubTexts[(int)textId].Text.FinishWritingToBuffer();
                LastTextIDWritten = textId;
                LastWrittenWasImageInsteadOfText = false;
            }

            public void SetText( TextId textId, string Text, string tooltip )
            {
                ArcenDoubleCharacterBuffer buffer = SubTexts[(int)textId].Text.StartWritingToBuffer();
                buffer.Add( Text );
                SubTexts[(int)textId].Text.FinishWritingToBuffer();
                SubTexts[(int)textId].Tooltip = tooltip;
                LastTextIDWritten = textId;
                LastWrittenWasImageInsteadOfText = false;
            }

            public void SetText( TextId textId, int NumericValue, string tooltip )
            {
                string suffix = ArcenExternalUIUtilities.GetRoundedNumberWithSuffix( ref NumericValue, false );

                ArcenDoubleCharacterBuffer buffer = SubTexts[(int)textId].Text.StartWritingToBuffer();
                buffer.Add( NumericValue );
                if ( suffix.Length > 0 )
                    buffer.Add( suffix );
                SubTexts[(int)textId].Text.FinishWritingToBuffer();
                SubTexts[(int)textId].Tooltip = tooltip;
                LastTextIDWritten = textId;
                LastWrittenWasImageInsteadOfText = false;
            }

            public GameEntityTypeData TypeToBuild
            {
                get
                {
                    GameEntityTypeData innerResult;
                    if ( GameEntity.CurrentlyHoveredOver != null && ( GameEntityTypeData.CurrentlyHoveredOver == null || GameEntityTypeData.SecondsSinceLastSet_CurrentlyHoveredOver >= GameEntity.SecondsSinceLastSet_CurrentlyHoveredOver ) )
                        innerResult = GameEntity.CurrentlyHoveredOver.TypeData;
                    else
                        innerResult = GameEntityTypeData.CurrentlyHoveredOver;
                    if ( innerResult == null || innerResult.Category != GameEntityCategory.Ship )
                        return null;
                    return innerResult;
                }
            }

            public override void SetElement( ArcenUI_Element Element )
            {
                this.Element = (ArcenUI_ImageButton)Element;
            }

            public override MouseHandlingResult HandleClick()
            {
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return this.TypeToBuild == null;
            }

            public override void OnUpdate()
            {
                if (!this.HaveGottenScaleInfo && this.Element != null )
                {
                    this.HaveGottenScaleInfo = true;
                    //float referenceWidth = 300;
                    float referenceHeight = 345;
                    float requestedHeight = this.Element.Height * ArcenUI.Instance.PixelsPerPercent_Y;
                    float scaleRatio = requestedHeight / referenceHeight;
                    this.XScale = scaleRatio;
                    this.YScale = scaleRatio;
                    //Rect rect = this.Element.ReferenceImage.rectTransform.rect;
                    //rect.width = referenceWidth * this.XScale;
                    //rect.height = referenceHeight * this.YScale;
                    //this.Element.ReferenceImage.rectTransform.SetWidth( rect.width );
                    //this.Element.ReferenceImage.rectTransform.SetHeight( rect.height );

                    //if ( ArcenUI.Instance.PixelsPerPercent_X != ArcenUI.Instance.PixelsPerPercent_Y )
                    //{
                    //    float adjustmentMultiplier = ArcenUI.Instance.PixelsPerPercent_Y / ArcenUI.Instance.PixelsPerPercent_X;
                    //    Vector3 mainImageScale = this.Element.ReferenceImage.transform.localScale;
                    //    mainImageScale.x *= adjustmentMultiplier;
                    //    this.Element.ReferenceImage.transform.localScale = mainImageScale;
                    //    referenceWidth *= adjustmentMultiplier;
                    //}

                    //Rect rect = this.Element.ReferenceImage.rectTransform.rect;
                    //this.XScale = rect.width / referenceWidth;
                    //this.YScale = rect.height / referenceHeight;

                    for ( int i = 0; i < this.Element.SubImages.Length; i++ )
                    {
                        ArcenUI_Image.SubImage image = this.Element.SubImages[i];

                        switch ( i )
                        {
                            case (int)ImageId.Unused1:
                            case (int)ImageId.Unused2:
                            case (int)ImageId.Unused3:
                                continue; // horrible hack, it's because these are actually duplicate references and all kinds of bad things will happen if they get multiplied more than once
                        }

                        Vector3 scale = image.Img.transform.localScale;
                        scale.x *= this.XScale;
                        scale.y *= this.YScale;
                        image.Img.transform.localScale = scale;

                        Vector3 position = image.Img.transform.localPosition;
                        position.x *= this.XScale;
                        position.y *= this.YScale;
                        image.Img.transform.localPosition = position;
                    }

                    for ( int i = 0; i < this.Element.SubTexts.Length; i++ )
                    {
                        SubText text = this.Element.SubTexts[i];

                        Vector3 scale = text.ReferenceText.transform.localScale;
                        scale.x *= this.XScale;
                        scale.y *= this.YScale;
                        text.ReferenceText.transform.localScale = scale;

                        Vector3 position = text.ReferenceText.transform.localPosition;
                        position.x *= this.XScale;
                        position.y *= this.YScale;
                        text.ReferenceText.transform.localPosition = position;
                    }
                }
                base.OnUpdate();
            }

            public override void HandleMouseover()
            {
                if ( GameEntity.CurrentlyHoveredOver != null )
                    GameEntity.SecondsSinceLastSet_CurrentlyHoveredOver = 0;
            }

            public override void HandleSubImageMouseover( ArcenUI_Image.SubImage SubImage )
            {
                int index = this.SubImages.IndexOf( SubImage );
                if ( index < 0 )
                    return;
                ImageId id = (ImageId)index;

                string tooltip = SubImage.Tooltip;

                switch ( id )
                {
                    case ImageId.Attack:
                    case ImageId.Defense:
                        tooltip = tooltip.Replace( "{0}", ExternalConstants.Instance.Balance_DamageMultiplierWhenCounteringDefense.ReadableString );
                        break;
                }

                string key = "EntityTooltipPanel_" + id;
                if ( !Language.Current.Contains( key ) )
                    return;
                Window_AtMouseTooltipPanel.bPanel.Instance.SetText( Language.Current.GetValue( key ) + tooltip );
            }

            public override void HandleSubTextMouseover( SubText SubText )
            {
                int index = this.SubTexts.IndexOf( SubText );
                if ( index < 0 )
                    return;
                TextId id = (TextId)index;
                string idAsString = id.ToString();

                string tooltip = SubText.Tooltip;

                switch ( id )
                {
                    case TextId.FuelOrPower:
                        GameEntityTypeData typeData = this.TypeToBuild;
                        if ( typeData == null )
                            return;
                        else if ( typeData.BalanceStats.SquadFuelConsumption > 0 )
                            idAsString = "Fuel";
                        else if ( typeData.BalanceStats.SquadPowerConsumption > 0 )
                            idAsString = "Power";
                        else
                            return;
                        break;
                    case TextId.Attack:
                    case TextId.Defense:
                        tooltip = tooltip.Replace( "{0}", ExternalConstants.Instance.Balance_DamageMultiplierWhenCounteringDefense.ReadableString );
                        break;
                }

                string key = "EntityTooltipPanel_" + idAsString;
                if ( !Language.Current.Contains( key ) )
                    return;
                Window_AtMouseTooltipPanel.bPanel.Instance.SetText( Language.Current.GetValue( key ) + tooltip );
            }
        }
    }

    public static class TooltipImageIDExtensionMethods
    {
        public static void Set(this Window_InGameEntityTooltipPanel.bPanel.ImageId ImageId, Sprite sprite, string tooltip)
        {
            Window_InGameEntityTooltipPanel.bPanel.Instance.SetSprite( ImageId, sprite, tooltip );
        }

        public static void Set( this Window_InGameEntityTooltipPanel.bPanel.ImageId ImageId, string BundleName, string PathInBundle, string tooltip )
        {
            Window_InGameEntityTooltipPanel.bPanel.Instance.SetSprite( ImageId, BundleName, PathInBundle, tooltip );
        }

        public static void Set( this Window_InGameEntityTooltipPanel.bPanel.ImageId ImageId, IArcenStatWithIcon Stat )
        {
            if ( Stat == null || Stat.StatIconBundle.Length <= 0 || Stat.StatIconPath.Length <= 0 )
                ImageId.Hide();
            else
                Window_InGameEntityTooltipPanel.bPanel.Instance.SetSprite( ImageId, Stat.StatIconBundle, Stat.StatIconPath, Stat.StatTooltip );
        }

        public static void ChangeVisibility( this Window_InGameEntityTooltipPanel.bPanel.ImageId ImageId, bool Shown )
        {
            Window_InGameEntityTooltipPanel.bPanel.Instance.SetShown( ImageId, Shown );
        }

        public static void Show( this Window_InGameEntityTooltipPanel.bPanel.ImageId ImageId )
        {
            Window_InGameEntityTooltipPanel.bPanel.Instance.SetShown( ImageId, true );
        }

        public static void Hide( this Window_InGameEntityTooltipPanel.bPanel.ImageId ImageId )
        {
            Window_InGameEntityTooltipPanel.bPanel.Instance.SetShown( ImageId, false );
        }
    }

    public static class TooltipTextIDExtensionMethods
    {
        public static void Set( this Window_InGameEntityTooltipPanel.bPanel.TextId TextId, string text, string tooltip )
        {
            Window_InGameEntityTooltipPanel.bPanel.Instance.SetText( TextId, text, tooltip );
        }

        public static void Set( this Window_InGameEntityTooltipPanel.bPanel.TextId TextId, int numericValue, string tooltip )
        {
            Window_InGameEntityTooltipPanel.bPanel.Instance.SetText( TextId, numericValue, tooltip );
        }

        public static void Hide( this Window_InGameEntityTooltipPanel.bPanel.TextId TextId )
        {
            Window_InGameEntityTooltipPanel.bPanel.Instance.HideText( TextId );
        }
    }
}
