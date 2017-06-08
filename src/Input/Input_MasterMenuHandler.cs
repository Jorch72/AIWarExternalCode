using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public class Input_MasterMenuHandler : IInputActionHandler
    {
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            if ( ArcenUI.Instance.ShowingConsole )
                return;
            switch ( InputActionType.InternalName )
            {
                case "TriggerCurrentMasterMenuAction_1":
                case "TriggerCurrentMasterMenuAction_2":
                case "TriggerCurrentMasterMenuAction_3":
                case "TriggerCurrentMasterMenuAction_4":
                case "TriggerCurrentMasterMenuAction_5":
                case "TriggerCurrentMasterMenuAction_6":
                case "TriggerCurrentMasterMenuAction_7":
                case "TriggerCurrentMasterMenuAction_8":
                case "TriggerCurrentMasterMenuAction_9":
                case "TriggerCurrentMasterMenuAction_10":
                    {
                        ArcenUI_Window window = GetCurrentBottommostMasterMenu();
                        {
                            ToggleableWindowController windowControllerThatIsOpen;
                            WindowTogglingButtonController buttonControllerThatWillCloseIt;
                            GetTopmostOpenMasterMenuWindow( window, out windowControllerThatIsOpen, out buttonControllerThatWillCloseIt );
                            if ( windowControllerThatIsOpen != null && windowControllerThatIsOpen.SupportsMasterMenuKeys )
                                window = windowControllerThatIsOpen.Window;
                        }
                        int targetButtonIndex = InputActionType.RelatedInt1 - 1;
                        IArcenUI_Button_Controller targetButtonController = GetButtonByIndex( window, targetButtonIndex );
                        if ( targetButtonController == null )
                            break;
                        targetButtonController.HandleClick();
                    }
                    break;
                case "MasterMenuBack":
                    {
                        ToggleableWindowController windowControllerThatIsOpen;
                        WindowTogglingButtonController buttonControllerThatWillCloseIt;
                        GetTopmostOpenMasterMenuWindow( GetCurrentBottommostMasterMenu(), out windowControllerThatIsOpen, out buttonControllerThatWillCloseIt );
                        if ( buttonControllerThatWillCloseIt == null || windowControllerThatIsOpen == null )
                        {
                            if ( Engine_AIW2.Instance.GetHasSelection() )
                                Engine_AIW2.Instance.ClearSelection();
                            break;
                        }
                        buttonControllerThatWillCloseIt.HandleClick();
                    }
                    break;
            }
        }

        private static ArcenUI_Window GetCurrentBottommostMasterMenu()
        {
            return Engine_AIW2.Instance.GetHasSelection() ? Window_InGameCommandsMenu.Instance.Window : Window_InGameBottomMenu.Instance.Window;
        }

        public static IArcenUI_Button_Controller GetButtonByIndex( ArcenUI_Window WindowToSearch, int targetButtonIndex )
        {
            bool reverseSearch = false;// WindowToSearch.Controller != Window_InGameMasterMenu.Instance;
            
            List<ArcenUI_Element> elements = WindowToSearch.Elements;
            int indexFound = -1;
            for ( int i = reverseSearch ? elements.Count - 1 : 0;
                  reverseSearch ? i >= 0 : i < elements.Count;
                  i += reverseSearch ? -1 : 1 )
            {
                ArcenUI_Element element = elements[i];
                if ( element.Type != ArcenUI_ElementType.Button )
                    continue;
                indexFound++;
                if ( indexFound < targetButtonIndex )
                    continue;
                ArcenUI_Button elementAsType = (ArcenUI_Button)element;
                return (IArcenUI_Button_Controller)elementAsType.Controller;
            }

            return null;
        }

        public static int GetIndexByButton( ArcenUI_Window WindowToSearch, IArcenUI_Button_Controller buttonController )
        {
            bool reverseSearch = false;// WindowToSearch.Controller != Window_InGameMasterMenu.Instance;
            
            List<ArcenUI_Element> elements = WindowToSearch.Elements;
            int indexFound = -1;
            for ( int i = reverseSearch ? elements.Count - 1 : 0;
                  reverseSearch ? i >= 0 : i < elements.Count;
                  i += reverseSearch ? -1 : 1 )
            {
                ArcenUI_Element element = elements[i];
                if ( element.Type != ArcenUI_ElementType.Button )
                    continue;
                indexFound++;
                if ( element.Controller != buttonController )
                    continue;
                return indexFound;
            }

            return -1;
        }
        
        public static bool GetIsWindowTheTopmostOpenMasterMenuWindow(ArcenUI_Window Window)
        {
            ArcenUI_Window bottomWindow = GetCurrentBottommostMasterMenu();

            ToggleableWindowController topmostWindow;
            WindowTogglingButtonController buttonThatOpensIt;
            if ( !( Window.Controller is ToggleableWindowController ) )
            {
                if(Window.Controller == bottomWindow.Controller )
                {
                    GetTopmostOpenMasterMenuWindow( bottomWindow, out topmostWindow, out buttonThatOpensIt );
                    if ( topmostWindow == null )
                        return true;
                }
                return false;
            }
            ToggleableWindowController controllerAsType = (ToggleableWindowController)Window.Controller;
            if ( !controllerAsType.IsOpen )
                return false;
            GetTopmostOpenMasterMenuWindow( bottomWindow, out topmostWindow, out buttonThatOpensIt );
            if ( topmostWindow == null || Window != topmostWindow.Window )
                return false;
            return true;
        }

        public static void GetTopmostOpenMasterMenuWindow( ArcenUI_Window WindowToSearch, out ToggleableWindowController windowControllerThatIsOpen, out WindowTogglingButtonController buttonControllerThatWillCloseIt )
        {
            bool reverseSearch = false;// WindowToSearch.Controller != Window_InGameMasterMenu.Instance;

            List<ArcenUI_Element> elements = WindowToSearch.Elements;
            windowControllerThatIsOpen = null;
            buttonControllerThatWillCloseIt = null;
            for ( int i = reverseSearch ? elements.Count - 1 : 0; 
                  reverseSearch ? i >= 0 : i < elements.Count; 
                  i += reverseSearch ? -1 : 1 )
            {
                ArcenUI_Element element = elements[i];
                if ( element.Type != ArcenUI_ElementType.Button )
                    continue;
                if ( !( element.Controller is WindowTogglingButtonController ) )
                    continue;
                WindowTogglingButtonController controllerAsType = (WindowTogglingButtonController)element.Controller;
                ToggleableWindowController relatedWindowController = controllerAsType.GetRelatedController();
                if ( !relatedWindowController.IsOpen )
                    continue;
                GetTopmostOpenMasterMenuWindow( relatedWindowController.Window, out windowControllerThatIsOpen, out buttonControllerThatWillCloseIt );
                if ( windowControllerThatIsOpen == null )
                {
                    windowControllerThatIsOpen = relatedWindowController;
                    buttonControllerThatWillCloseIt = controllerAsType;
                }
                return;
            }
        }
    }
}