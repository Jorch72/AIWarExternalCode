using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public abstract class WindowControllerAbstractBase : IArcenUI_Window_Controller
    {
        public ArcenUI_Window Window;
        public bool ShouldCauseAllOtherWindowsToNotShow;
        public bool ShouldShowEvenWhenGUIHidden;
        public bool OnlyShowInGame;
        public bool SupportsMasterMenuKeys;
        private static readonly List<WindowControllerAbstractBase> CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow = new List<WindowControllerAbstractBase>();

        public bool GetShouldDrawThisFrame()
        {
            bool result = true;
            if ( !this.ShouldCauseAllOtherWindowsToNotShow && CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow.Count > 0 )
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
                if ( Input_MasterMenuHandler.GetIsWindowTheTopmostOpenMasterMenuWindow( this.Element.Window ) )
                {
                    int index = Input_MasterMenuHandler.GetIndexByButton( this.Element.Window, this );
                    if ( index >= 0 )
                        Buffer.Add( "(" ).Add( index + 1 ).Add( ")" );
                }
            }
        }

        public virtual void HandleClick() { }
        public virtual void HandleMouseover() { }
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
            base.GetTextToShow( Buffer );
            Buffer.Add( this.GetRelatedController().IsOpen ? this.TextWhenOpen : this.TextWhenClosed );
        }

        public override void HandleClick()
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
        }

        public override void HandleMouseover() { }
        public override void OnUpdate()
        {
            if ( this.Window == null )
                this.Window = Element.Window;
        }

        public abstract ToggleableWindowController GetRelatedController();
    }
}
