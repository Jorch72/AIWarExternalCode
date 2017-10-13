using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Arcen.AIW2.External
{
    public class Input_DebugHandler : IInputActionHandler
    {
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            HandleInner( Int1, InputActionType.InternalName );
        }

        public static void HandleInner( Int32 Int1, string InputActionInternalName )
        {
            if ( ArcenUI.CurrentlyShownWindowsWith_PreventsNormalInputHandlers.Count > 0 )
                return;

            switch ( InputActionInternalName )
            {
                case "Debug_ToggleConsole":
                    ArcenUI.Instance.ShowingConsole = !ArcenUI.Instance.ShowingConsole;
                    ArcenUI.Instance.UnitermConsoleCanvasObject.SetActive( ArcenUI.Instance.ShowingConsole );
                    if ( ArcenUI.Instance.ShowingConsole )
                        ArcenUI.Instance.UnitermTextbox.ActivateInputField();
                    break;
                case "Debug_ConsoleAutocomplete":
                    if ( ArcenUI.Instance.ShowingConsole )
                        Engine_AIW2.Instance.FrontEnd.Uniterm_AcceptAutocomplete();
                    break;
            }

            if ( ArcenUI.Instance.ShowingConsole )
                return;

            switch ( InputActionInternalName )
            {
                case "Debug_StartTestChamber":
                    {
                        Engine_AIW2.Instance.QuitGameAndGoBackToMainMenu();
                        TestChamberTable.Instance.Initialize( true );
                        GameSettings_AIW2.Current.LastSetup.MapType = MapTypeDataTable.Instance.GetRowByName( "TestChamber", false, null );
                        Engine_AIW2.Instance.InnerDoStartNewWorldOKLogic();
                        while ( Engine_Universal.WorkThreadIsRunning )
                            Thread.Sleep( 10 );
                        GameCommand command = GameCommand.Create( GameCommandType.TogglePause );
                        World_AIW2.Instance.QueueGameCommand( command, true );
                    }
                    break;
                case "Debug_InstantStopSim":
                    Engine_Universal.DebugTimeMode = DebugTimeMode.StopSim;
                    break;
                case "Debug_InstantStopSimAndVisualUpdates":
                    Engine_Universal.DebugTimeMode = DebugTimeMode.StopSimAndVisualUpdates;
                    break;
                case "Debug_ResumeFromInstantStop":
                    Engine_Universal.DebugTimeMode = DebugTimeMode.Normal;
                    break;
                case "Debug_RunExactlyNMoreSimSteps":
                    Engine_Universal.DebugSimStepsToRun = 1;
                    Engine_Universal.DebugTimeMode = DebugTimeMode.Normal;
                    break;
                case "Debug_RunNMoreVisualUpdateSeconds":
                    Engine_Universal.DebugVisualUpdateSecondsToRun = GameSettings.Current.DebugVisualUpdateSecondsIntervalLength;
                    switch ( Engine_Universal.DebugTimeMode )
                    {
                        case DebugTimeMode.StopSimAndVisualUpdates:
                            Engine_Universal.DebugTimeMode = DebugTimeMode.StopSim;
                            break;
                    }
                    break;
                case "Debug_BarfSquadData":
                    {
                        if ( !World_AIW2.Instance.IsPaused )
                        {
                            GameCommand command = GameCommand.Create( GameCommandType.TogglePause );
                            World_AIW2.Instance.QueueGameCommand( command, true );
                        }
                        ArcenCharacterBuffer buffer = new ArcenCharacterBuffer();
                        int count = 0;
                        Engine_AIW2.Instance.DoForSelected( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy, delegate ( GameEntity selected )
                        {
                            count++;
                            selected.VisualObj.WriteDebugDataTo( buffer );
                            return DelReturn.Continue;
                        } );
                        ArcenDebugging.ArcenDebugLogSingleLine( "Squad Data Dump from " + count + " entities:" + buffer.ToString(), Verbosity.DoNotShow );
                    }
                    break;
            }
        }
    }
}