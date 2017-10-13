using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public abstract class WindowControllerAbstractBase : IArcenUI_Window_Controller
    {
        public ArcenUI_Window Window;
        public bool ShouldCauseAllOtherWindowsToNotShow;
        public bool IsAtMouseTooltip;
        public bool ShouldShowEvenWhenGUIHidden;
        public bool OnlyShowInGame;
        public bool SupportsMasterMenuKeys;
        public bool PreventsNormalInputHandlers;
        private static readonly List<WindowControllerAbstractBase> CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow = new List<WindowControllerAbstractBase>();

        public bool GetShouldDrawThisFrame()
        {
            bool result = true;
            if ( !this.ShouldCauseAllOtherWindowsToNotShow && !this.IsAtMouseTooltip && CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow.Count > 0 )
                result = false;
            if ( ArcenUI.Instance.InHideGUIMode && !this.ShouldShowEvenWhenGUIHidden )
                result = false;
            if ( this.OnlyShowInGame )
            {
                if ( !World.Instance.IsLoaded )
                    result = false;
                if ( !World_AIW2.Instance.HasEverBeenUnpaused )
                    result = false;
            }
            if ( result )
                result = this.GetShouldDrawThisFrame_Subclass();
            if ( this.ShouldCauseAllOtherWindowsToNotShow )
            {
                if ( result )
                {
                    if ( !CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow.Contains( this ) )
                        CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow.Add( this );
                }
                else
                {
                    if ( CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow.Contains( this ) )
                        CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow.Remove( this );
                }
            }
            if ( !result )
                this.OnShowingRefused();
            return result;
        }

        public virtual bool GetShouldDrawThisFrame_Subclass()
        {
            return true;
        }

        public void SetWindow( ArcenUI_Window Window )
        {
            this.Window = Window;
        }

        public void CloseWindowsOtherThanThisOne( ToggleableWindowController controller )
        {
            for ( int i = 0; i < this.Window.Elements.Count; i++ )
            {
                ArcenUI_Element element = this.Window.Elements[i];
                if ( !( element.Controller is WindowTogglingButtonController ) )
                    continue;
                WindowTogglingButtonController otherControllerAsType = (WindowTogglingButtonController)element.Controller;
                ToggleableWindowController otherRelatedController = otherControllerAsType.GetRelatedController();
                if ( otherRelatedController == controller )
                    continue;
                if ( !otherRelatedController.IsOpen )
                    continue;
                otherRelatedController.Close();
            }
        }

        public virtual void OnShowingRefused() { }

        public virtual void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set ) { }

        bool IArcenUI_Window_Controller.PreventsNormalInputHandlers
        {
            get
            {
                return PreventsNormalInputHandlers;
            }
        }
    }

    public abstract class ToggleableWindowController : WindowControllerAbstractBase
    {
        public bool IsOpen;

        public void Open()
        {
            if ( this.IsOpen )
                return;
            this.IsOpen = true;
            this.OnOpen();
        }

        public void Close()
        {
            if ( !this.IsOpen )
                return;
            this.IsOpen = false;
            if ( this.Window != null )
            {
                for ( int i = 0; i < this.Window.Elements.Count; i++ )
                {
                    ArcenUI_Element element = this.Window.Elements[i];
                    if ( !( element.Controller is WindowTogglingButtonController ) )
                        continue;
                    WindowTogglingButtonController otherControllerAsType = (WindowTogglingButtonController)element.Controller;
                    ToggleableWindowController otherRelatedController = otherControllerAsType.GetRelatedController();
                    if ( !otherRelatedController.IsOpen )
                        continue;
                    otherRelatedController.Close();
                }
            }
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( !this.IsOpen )
                return false;
            return true;
        }

        public override void OnShowingRefused()
        {
            if ( !this.IsOpen )
                return;
            this.Close();
        }

        public virtual void OnOpen() { }
    }

    public abstract class ElementAbstractBase : IArcenUI_Element_Controller
    {
        public ArcenUI_Element Element;
        public WindowControllerAbstractBase WindowController;

        public virtual void OnUpdate() { }
        public virtual bool GetShouldBeHidden() { return false; }
        public virtual void HandleMouseover() { }

        public void SetElement( ArcenUI_Element Element )
        {
            this.Element = Element;
            this.WindowController = (WindowControllerAbstractBase)Element.Window.Controller;
        }
    }

    public abstract class TextAbstractBase : ElementAbstractBase, IArcenUI_Text_Controller
    {
        public abstract void GetTextToShow( ArcenDoubleCharacterBuffer Buffer );
    }

    public abstract class ButtonAbstractBase : ElementAbstractBase, IArcenUI_Button_Controller
    {
        public virtual void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
        {
            if ( this.WindowController.SupportsMasterMenuKeys )
            {
                if ( Input_MasterMenuHandler.GetIsWindowTheTopmostOpenMasterMenuWindow( this.Element.Window ) 
                     && !Window_InGameBuildTypeIconMenu.Instance.IsOpen
                     && !Window_InGameTechTypeIconMenu.Instance.IsOpen )
                {
                    int index = Input_MasterMenuHandler.GetIndexByButton( this.Element.Window, this );
                    if ( index >= 0 )
                        Buffer.Add( "(" ).Add( index + 1 ).Add( ")" );
                }
            }
        }

        public virtual MouseHandlingResult HandleClick() { return MouseHandlingResult.None; }
        public virtual void DoAnyCustomButtonStuff( ArcenUI_Button Button ) { }
    }

    public abstract class ImageButtonAbstractBase : IArcenUI_ImageButton_Controller
    {
        public abstract void UpdateContent( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts );

        public virtual MouseHandlingResult HandleClick() { return MouseHandlingResult.None; }

        public virtual void HandleMouseover() { }

        public virtual void OnUpdate() { }
        public virtual bool GetShouldBeHidden() { return false; }
        public virtual void SetElement( ArcenUI_Element Element ) { }
        public virtual void HandleSubImageMouseover( ArcenUI_Image.SubImage SubImage ) { }
        public virtual void HandleSubTextMouseover( SubText SubText ) { }
    }

    public abstract class SliderAbstractBase : ElementAbstractBase, IArcenUI_Slider_Controller
    {
        public virtual void DoAnyCustomSliderStuff( ArcenUI_Slider Slider ) { }
        public virtual MouseHandlingResult HandleClick() { return MouseHandlingResult.None; }
        public virtual void OnChange( float NewValue ) { }
    }

    public abstract class ButtonSetAbstractBase : ElementAbstractBase, IArcenUI_ButtonSet_Controller
    {
    }

    public abstract class DropdownAbstractBase : ElementAbstractBase, IArcenUI_Dropdown_Controller
    {
        public virtual void GetMainTextToShowPrefix( ArcenDoubleCharacterBuffer Buffer ) { }

        public virtual void HandleItemMouseover( IArcenUI_Dropdown_Option Item ) { }

        public virtual void HandleOverallMouseover() { }

        public virtual void HandleSelectionChanged( IArcenUI_Dropdown_Option Item ) { }
    }

    public abstract class InputAbstractBase : ElementAbstractBase, IArcenUI_Input_Controller
    {
        public virtual void HandleChangeInValue( string NewValue ) { }

        public virtual char ValidateInput( string input, int charIndex, char addedChar ) { return addedChar; }
    }
    
    public abstract class ImageSetAbstractBase : IArcenUI_ImageSet_Controller
    {
        public virtual void OnUpdate() { }
        public virtual bool GetShouldBeHidden() { return false; }
        public virtual void SetElement( ArcenUI_Element Element ) { }
        public virtual void HandleMouseover() { }
    }

    public abstract class ImageButtonSetAbstractBase : IArcenUI_ImageButtonSet_Controller
    {
        public virtual void OnUpdate() { }
        public virtual bool GetShouldBeHidden() { return false; }
        public virtual void SetElement( ArcenUI_Element Element ) { }
        public virtual void HandleMouseover() { }
    }

    public abstract class ImageAbstractBase : IArcenUI_Image_Controller
    {
        public virtual void OnUpdate() { }
        public virtual bool GetShouldBeHidden() { return false; }
        public virtual void SetElement( ArcenUI_Element Element ) { }
        public virtual void HandleClick() { }
        public virtual void HandleMouseover() { }
        public virtual void UpdateImages( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages ) { }
    }

    public abstract class WindowTogglingButtonController : ButtonAbstractBase
    {
        private ArcenUI_Window Window;
        private string TextWhenClosed = string.Empty;
        private string TextWhenOpen = string.Empty;

        public WindowTogglingButtonController( string TextWhenClosed, string TextWhenOpen )
        {
            this.TextWhenClosed = TextWhenClosed;
            this.TextWhenOpen = TextWhenOpen;
        }

        public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
        {
            bool toggledWindowIsShown = this.GetRelatedController().IsOpen;
            if ( this.GetShouldSuppressOpenIndicatorEvenIfToggledWindowIsShown() )
                toggledWindowIsShown = false;
            base.GetTextToShow( Buffer );
            Buffer.Add( toggledWindowIsShown ? this.TextWhenOpen : this.TextWhenClosed );
        }

        public override MouseHandlingResult HandleClick()
        {
            ToggleableWindowController controller = this.GetRelatedController();
            if ( controller.IsOpen )
                controller.Close();
            else
            {
                controller.Open();
                if ( this.Window != null && this.Window.Controller is WindowControllerAbstractBase )
                    ( (WindowControllerAbstractBase)this.Window.Controller ).CloseWindowsOtherThanThisOne( controller );
            }
            return MouseHandlingResult.None;
        }

        public override void HandleMouseover() { }
        public override void OnUpdate()
        {
            if ( this.Window == null )
                this.Window = Element.Window;
        }

        public abstract ToggleableWindowController GetRelatedController();

        public virtual bool GetShouldSuppressOpenIndicatorEvenIfToggledWindowIsShown() { return false; }
    }
}
