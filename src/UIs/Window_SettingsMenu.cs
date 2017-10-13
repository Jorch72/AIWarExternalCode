using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_SettingsMenu : ToggleableWindowController
    {
        private static readonly string TEXT_PREFAB_NAME = "HoverableText";
        private static readonly string BUTTON_PREFAB_NAME = "ButtonBlue";
        private static readonly string INPUT_PREFAB_NAME = "BasicTextbox";
        private static readonly string VERTICAL_SLIDER_PREFAB_NAME = "BasicVerticalSlider";
        private static readonly string HORIZONTAL_SLIDER_PREFAB_NAME = "BasicHorizontalSlider";
        private static readonly string DROPDOWN_PREFAB_NAME = "BasicDropdown";

        private int startingTableIndex = 0;
        private Rect mainAreaBounds = ArcenRectangle.CreateUnityRect( 3, 3, 94, 94 );
        private float rowHeight = 4;
        private float headerRowHeight = 5;

        public static Window_SettingsMenu Instance;
        public Window_SettingsMenu()
        {
            Instance = this;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.PreventsNormalInputHandlers = true;
        }

        public override void OnOpen()
        {
            ArcenSettingTable.Instance.CopyCurrentValuesToTemp();
            if ( World.Instance.IsLoaded && !World_AIW2.Instance.IsPaused )
            {
                GameCommand command = GameCommand.Create( GameCommandType.TogglePause );
                World_AIW2.Instance.QueueGameCommand( command, true );
            }
        }

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            Rect scrollbarRect = ArcenRectangle.CreateUnityRect( mainAreaBounds.x - 2, mainAreaBounds.y + 2, 2, mainAreaBounds.height - 5 );
            AddVerticalSlider( Set, typeof( sScrollbar ), 0, scrollbarRect );

            int rowsToShow = GetMaxRowsToShow();

            int rowCount = ArcenSettingTable.Instance.VisibleRows.Count;
            if ( sScrollbar.Instance != null )
            {
                int range = rowCount - rowsToShow;
                if ( range <= 0 )
                    startingTableIndex = 0;
                else
                {
                    ArcenUI_Slider slider = (ArcenUI_Slider)sScrollbar.Instance.Element;
                    startingTableIndex = Mathf.FloorToInt( range * slider.ReferenceSlider.value );
                }
            }

            float runningY = 3;

            Rect headerTextBounds = ArcenRectangle.CreateUnityRect( mainAreaBounds.xMin, runningY, 15, headerRowHeight );
            AddText( Set, typeof( tHeader ), 0, headerTextBounds );

            Rect closeButtonBounds = ArcenRectangle.CreateUnityRect( headerTextBounds.xMax + 2, runningY, 10, headerRowHeight );
            AddButton( Set, typeof( bCancel ), 0, closeButtonBounds );

            Rect saveButtonBounds = ArcenRectangle.CreateUnityRect( closeButtonBounds.xMax + 2, runningY, 10, headerRowHeight );
            AddButton( Set, typeof( bSave ), 0, saveButtonBounds );

            Rect resetButtonBounds = ArcenRectangle.CreateUnityRect( saveButtonBounds.xMax + 2, runningY, 10, headerRowHeight );
            AddButton( Set, typeof( bReset ), 0, resetButtonBounds );

            runningY += headerRowHeight;

            int rowsLeft = rowCount - startingTableIndex;
            rowsToShow = Math.Min( rowsLeft, rowsToShow );
            for ( int i = 0; i < rowsToShow; i++ )
            {
                ArcenSetting setting = ArcenSettingTable.Instance.VisibleRows[startingTableIndex + i];

                Rect nameBounds = ArcenRectangle.CreateUnityRect( mainAreaBounds.xMin, runningY, 50, rowHeight );
                AddText( Set, typeof( tSettingName ), i, nameBounds );

                Rect valueSettingControlBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax, runningY, 15, rowHeight );
                switch ( setting.Type )
                {
                    case ArcenSettingType.BoolToggle:
                        AddButton( Set, typeof( bToggle ), i, valueSettingControlBounds );
                        break;
                    case ArcenSettingType.IntTextbox:
                        AddInput( Set, typeof( iIntInput ), i, valueSettingControlBounds );
                        break;
                    case ArcenSettingType.FloatSlider:
                        AddHorizontalSlider( Set, typeof( sFloatSlider ), i, valueSettingControlBounds );
                        break;
                    case ArcenSettingType.IntDropdown:
                        AddDropdown( Set, typeof( dIntDropdown ), i, valueSettingControlBounds );
                        break;
                }

                Rect valueDescriptionBounds = ArcenRectangle.CreateUnityRect( valueSettingControlBounds.xMax, runningY, 30, rowHeight );
                AddText( Set, typeof( tSettingValueDescription ), i, valueDescriptionBounds );

                runningY += rowHeight;
            }
        }

        private int GetMaxRowsToShow()
        {
            return Mathf.FloorToInt( ( mainAreaBounds.height - headerRowHeight ) / rowHeight ) - 1;
        }

        private static ArcenUI_CreateElementDirective _AddBase( string prefabName, ArcenUI_SetOfCreateElementDirectives Set, Type ControllerType, int CodeDirectiveTag, Rect rect )
        {
            return Set.GetNextFree( prefabName, ControllerType, CodeDirectiveTag, rect.x, rect.y, rect.width, rect.height );
        }

        private static ArcenUI_CreateElementDirective AddText( ArcenUI_SetOfCreateElementDirectives Set, Type ControllerType, int CodeDirectiveTag, Rect rect )
        {
            return _AddBase( TEXT_PREFAB_NAME, Set, ControllerType, CodeDirectiveTag, rect );
        }

        private static ArcenUI_CreateElementDirective AddButton( ArcenUI_SetOfCreateElementDirectives Set, Type ControllerType, int CodeDirectiveTag, Rect rect )
        {
            return _AddBase( BUTTON_PREFAB_NAME, Set, ControllerType, CodeDirectiveTag, rect );
        }

        private static ArcenUI_CreateElementDirective AddInput( ArcenUI_SetOfCreateElementDirectives Set, Type ControllerType, int CodeDirectiveTag, Rect rect )
        {
            return _AddBase( INPUT_PREFAB_NAME, Set, ControllerType, CodeDirectiveTag, rect );
        }

        private static ArcenUI_CreateElementDirective AddVerticalSlider( ArcenUI_SetOfCreateElementDirectives Set, Type ControllerType, int CodeDirectiveTag, Rect rect )
        {
            return _AddBase( VERTICAL_SLIDER_PREFAB_NAME, Set, ControllerType, CodeDirectiveTag, rect );
        }

        private static ArcenUI_CreateElementDirective AddHorizontalSlider( ArcenUI_SetOfCreateElementDirectives Set, Type ControllerType, int CodeDirectiveTag, Rect rect )
        {
            return _AddBase( HORIZONTAL_SLIDER_PREFAB_NAME, Set, ControllerType, CodeDirectiveTag, rect );
        }

        private static ArcenUI_CreateElementDirective AddDropdown( ArcenUI_SetOfCreateElementDirectives Set, Type ControllerType, int CodeDirectiveTag, Rect rect )
        {
            return _AddBase( DROPDOWN_PREFAB_NAME, Set, ControllerType, CodeDirectiveTag, rect );
        }

        private static ArcenSetting GetSettingForController( ElementAbstractBase controller )
        {
            int tableIndex = Instance.startingTableIndex + controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag;
            if ( tableIndex >= ArcenSettingTable.Instance.VisibleRows.Count )
                return null;
            return ArcenSettingTable.Instance.VisibleRows[tableIndex];
        }

        public class tHeader : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "Settings Menu" );
            }
        }

        public class tSettingName : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                buffer.Add( setting.DisplayName );
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
            }
        }

        public class tSettingValueDescription : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                switch ( setting.Type )
                {
                    case ArcenSettingType.BoolToggle:
                        break;
                    case ArcenSettingType.FloatSlider:
                        buffer.Add( setting.TempValue_Float.ToString() );
                        buffer.Add( " (ranges from " ).Add( setting.MinFloatValue.ToString() );
                        if ( setting.MaxFloatValue > 0 && setting.MaxFloatValue > setting.MinFloatValue )
                            buffer.Add( " to " ).Add( setting.MaxFloatValue.ToString() );
                        buffer.Add( ")" );
                        break;
                    case ArcenSettingType.IntTextbox:
                        int valueAsInt;
                        if ( !Int32.TryParse( setting.TempValue_String, out valueAsInt ) )
                            buffer.Add( "Must be an integer." );
                        else if ( valueAsInt < setting.MinIntValue )
                            buffer.Add( "Must be at least " ).Add( setting.MinIntValue );
                        else if ( setting.MaxIntValue > 0 && setting.MaxIntValue > setting.MinIntValue && valueAsInt > setting.MaxIntValue )
                            buffer.Add( "Must be at most " ).Add( setting.MaxIntValue );
                        break;
                }
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
            }
        }

        public class bToggle : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                buffer.Add( setting.TempValue_Bool ? "On" : "Off" );
            }

            public override MouseHandlingResult HandleClick()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return MouseHandlingResult.None;

                setting.TempValue_Bool = !setting.TempValue_Bool;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
            }
        }

        public class iIntInput : InputAbstractBase
        {
            public override void HandleChangeInValue( string NewValue )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                setting.TempValue_String = NewValue;
            }

            //public override char ValidateInput( string input, int charIndex, char addedChar )
            //{
            //    if ( input.Length >= 10 )
            //        return '\0';
            //    if ( !char.IsDigit( addedChar ) )
            //        return '\0';

            //    ArcenSetting setting = GetSettingForController( this );
            //    if ( setting == null ) return '\0';

            //    string testInput = input.Insert( charIndex, addedChar.ToString() );
            //    int testInputAsType;
            //    if ( !Int32.TryParse( testInput, out testInputAsType ) )
            //        return '\0';

            //    if ( testInputAsType < setting.MinIntValue )
            //        return '\0';

            //    if ( setting.MaxIntValue > setting.MinIntValue && testInputAsType > setting.MaxIntValue )
            //        return '\0';

            //    return addedChar;
            //}

            public override void OnUpdate()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                ArcenUI_Input elementAsType = (ArcenUI_Input)Element;
                elementAsType.SetText( setting.TempValue_String );
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
            }
        }

        public class sFloatSlider : SliderAbstractBase
        {
            public override void OnUpdate()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                float currentValue = setting.TempValue_Float;
                float range = setting.MaxFloatValue - setting.MinFloatValue;
                if ( range == 0 ) range = 1;
                float currentPortion = ( currentValue - setting.MinFloatValue ) / range;

                ArcenUI_Slider elementAsType = (ArcenUI_Slider)Element;
                elementAsType.ReferenceSlider.value = currentPortion;
            }

            public override void OnChange( float NewValue )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                float range = setting.MaxFloatValue - setting.MinFloatValue;
                float adjustedNewValue = setting.MinFloatValue + ( range * NewValue );

                setting.TempValue_Float = adjustedNewValue;
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
            }
        }

        public class dIntDropdown : DropdownAbstractBase
        {
            public override void GetMainTextToShowPrefix( ArcenDoubleCharacterBuffer Buffer )
            {
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item )
            {
                if ( Item == null )
                    return;
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;

                int newValue = elementAsType.Items.IndexOf( Item );
                setting.TempValue_Int = newValue;
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanel.bPanel.Instance.SetText( setting.Description );
            }

            public override void HandleOverallMouseover() { }

            public override void HandleItemMouseover( IArcenUI_Dropdown_Option Item ) { }

            public override void OnUpdate()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)Element;

                if ( elementAsType.Items.Count <= 0 )
                {
                    for ( int i = 0; i < ArcenUI.SupportedResolutions.Count; i++ )
                        elementAsType.Items.Add( new ResolutionOption( ArcenUI.SupportedResolutions[i] ) );
                }

                if ( elementAsType.Items.Count > setting.TempValue_Int && setting.TempValue_Int != elementAsType.GetSelectedIndex() )
                    elementAsType.SetSelectedIndex( setting.TempValue_Int );
            }

            private class ResolutionOption : IArcenUI_Dropdown_Option
            {
                private Resolution Item;
                private string SavedName = string.Empty;

                public ResolutionOption( Resolution Item )
                {
                    this.Item = Item;
                }

                public object GetItem()
                {
                    return Item;
                }

                public string GetOptionName()
                {
                    if ( SavedName.Length <= 0 )
                        SavedName = Item.width + "x" + Item.height;
                    return SavedName;
                }

                public Sprite GetOptionSprite()
                {
                    return null;
                }
            }
        }

        public class sScrollbar : SliderAbstractBase
        {
            public static sScrollbar Instance;
            public sScrollbar() { Instance = this; }
        }

        public class bCancel : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "Cancel" );
            }

            public override MouseHandlingResult HandleClick()
            {
                ArcenSettingTable.Instance.CopyCurrentValuesToTemp(); // shouldn't matter, but tidies things up
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }

        public class bSave : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "Save" );
            }

            public override MouseHandlingResult HandleClick()
            {
                ArcenSettingTable.Instance.CopyTempValuesToCurrent();
                GameSettings.Current.SaveToDisk();
                GameSettings.Current.DoGraphicsSettingsNeedARefresh = true;
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }

        public class bReset : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "Set Defaults" );
            }

            public override MouseHandlingResult HandleClick()
            {
                int rowCount = ArcenSettingTable.Instance.VisibleRows.Count;
                for ( int i = 0; i < rowCount; i++ )
                {
                    ArcenSetting setting = ArcenSettingTable.Instance.VisibleRows[i];
                    switch ( setting.Type )
                    {
                        case ArcenSettingType.BoolToggle:
                            setting.TempValue_Bool = setting.DefaultBoolValue;
                            break;
                        case ArcenSettingType.FloatSlider:
                            setting.TempValue_Float = setting.DefaultFloatValue;
                            break;
                        case ArcenSettingType.IntTextbox:
                            setting.TempValue_Int = setting.DefaultIntValue;
                            setting.TempValue_String = setting.DefaultIntValue.ToString();
                            break;
                    }
                }
                return MouseHandlingResult.None;
            }
        }
    }
}