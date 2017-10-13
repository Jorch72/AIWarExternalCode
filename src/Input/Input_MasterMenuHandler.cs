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
            if ( ArcenUI.CurrentlyShownWindowsWith_PreventsNormalInputHandlers.Count > 0 )
                return;
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
                        int targetButtonIndex = InputActionType.RelatedInt1 - 1;
                        if ( Window_InGameBuildTypeIconMenu.Instance.IsOpen )
                        {
                            if ( Window_InGameBuildTypeIconMenu.Instance.CurrentTypeIndex < 0 )
                                Window_InGameBuildTypeIconMenu.Instance.CurrentTypeIndex = targetButtonIndex;
                            else
                            {
                                if ( Window_InGameBuildTypeIconMenu.Instance.LastShownItems.Count > Window_InGameBuildTypeIconMenu.Instance.CurrentTypeIndex )
                                {
                                    List<Window_InGameBuildTypeIconMenu.bItem> buttonList = Window_InGameBuildTypeIconMenu.Instance.LastShownItems[Window_InGameBuildTypeIconMenu.Instance.CurrentTypeIndex];
                                    if ( buttonList.Count > targetButtonIndex )
                                    {
                                        Window_InGameBuildTypeIconMenu.bItem button = buttonList[targetButtonIndex];
                                        if ( button != null )
                                            button.HandleClick();
                                    }
                                }
                            }
                        }
                        else if ( Window_InGameTechTypeIconMenu.Instance.IsOpen )
                        {
                            if ( Window_InGameTechTypeIconMenu.Instance.CurrentTypeIndex < 0 )
                                Window_InGameTechTypeIconMenu.Instance.CurrentTypeIndex = targetButtonIndex;
                            else
                            {
                                if ( Window_InGameTechTypeIconMenu.Instance.LastShownItems.Count > Window_InGameTechTypeIconMenu.Instance.CurrentTypeIndex )
                                {
                                    List<Window_InGameTechTypeIconMenu.bItem> buttonList = Window_InGameTechTypeIconMenu.Instance.LastShownItems[Window_InGameTechTypeIconMenu.Instance.CurrentTypeIndex];
                                    if ( buttonList.Count > targetButtonIndex )
                                    {
                                        Window_InGameTechTypeIconMenu.bItem button = buttonList[targetButtonIndex];
                                        if ( button != null )
                                            button.HandleClick();
                                    }
                                }
                            }
                        }
                        else
                        {
                            ArcenUI_Window window = GetCurrentBottommostMasterMenu();
                            {
                                ToggleableWindowController windowControllerThatIsOpen;
                                WindowTogglingButtonController buttonControllerThatWillCloseIt;
                                GetTopmostOpenMasterMenuWindow( window, out windowControllerThatIsOpen, out buttonControllerThatWillCloseIt );
                                if ( windowControllerThatIsOpen != null && windowControllerThatIsOpen.SupportsMasterMenuKeys )
                                    window = windowControllerThatIsOpen.Window;
                            }
                            IArcenUI_Button_Controller targetButtonController = GetButtonByIndex( window, targetButtonIndex );
                            if ( targetButtonController == null )
                                break;
                            targetButtonController.HandleClick();
                        }
                    }
                    break;
                case "OpenSystemMenu":
                    {
                        for(int i = 0; i < 20;i++ )
                            BackUpOneStepInMasterMenu();
                        Window_InGameBottomMenu.bToggleMasterMenu.Instance.HandleClick();
                    }
                    break;
                case "MasterMenuBack":
                    {
                        BackUpOneStepInMasterMenu();
                    }
                    break;
            }
        }

        private static void BackUpOneStepInMasterMenu()
        {
            if ( Window_InGameBuildTypeIconMenu.Instance.IsOpen )
            {
                if ( Window_InGameBuildTypeIconMenu.Instance.CurrentTypeIndex >= 0 )
                    Window_InGameBuildTypeIconMenu.Instance.CurrentTypeIndex = -1;
                else
                    Window_InGameBuildTypeIconMenu.Instance.Close();
                return;
            }
            if ( Window_InGameTechTypeIconMenu.Instance.IsOpen )
            {
                if ( Window_InGameTechTypeIconMenu.Instance.CurrentTypeIndex >= 0 )
                    Window_InGameTechTypeIconMenu.Instance.CurrentTypeIndex = -1;
                else
                    Window_InGameTechTypeIconMenu.Instance.Close();
                return;
            }

            ToggleableWindowController windowControllerThatIsOpen;
            WindowTogglingButtonController buttonControllerThatWillCloseIt;
            GetTopmostOpenMasterMenuWindow( GetCurrentBottommostMasterMenu(), out windowControllerThatIsOpen, out buttonControllerThatWillCloseIt );
            if ( buttonControllerThatWillCloseIt == null || windowControllerThatIsOpen == null )
            {
                if ( Engine_AIW2.Instance.GetHasSelection( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy ) )
                    Engine_AIW2.Instance.ClearSelection( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy );
            }
            else
                buttonControllerThatWillCloseIt.HandleClick();
        }

        private static ArcenUI_Window GetCurrentBottommostMasterMenu()
        {
            if ( Engine_AIW2.Instance.GetHasSelection( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy ) )
            {
                ArcenUI_Window result = Window_InGameCommandsMenu.Instance.Window;
                return result;
            }
            else
            {
                ArcenUI_Window result = Window_InGameBottomMenu.Instance.Window;
                return result;
            }
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