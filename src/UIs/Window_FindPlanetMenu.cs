using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arcen.AIW2.External
{
    public class Window_FindPlanetMenu : ToggleableWindowController
    {
        public static Window_FindPlanetMenu Instance;
        public static Dictionary<string, string> otherStructures;
        public Window_FindPlanetMenu()
        {
            /*
              The otherStructures dictionary allows the FindPlanet menu
              to find things that aren't planets (like the DysonSphere)

              key == what the user needs to type in
              value == tag for the structure

              Note right now it can only find things visible in the galaxy map, but that restriction
              is easily removed if desired

              Eventually we'll show the Keys for otherStructures in a mouseover or something
            */
            otherStructures = new Dictionary<string, string>( StringComparer.InvariantCultureIgnoreCase );
            otherStructures.Add( "DysonSphere", "Dyson" );
            otherStructures.Add( "Devourer", "Devourer" );
            otherStructures.Add( "SuperTerminal", "SuperTerminal" );
            otherStructures.Add( "Nanocaust", "NanobotHive" );
            otherStructures.Add( "ZenithTrader", "ZenithTrader" );

            Instance = this;
            this.OnlyShowInGame = true;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
        }

        public class bCloseFindPlanet : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Close" );
            }
            public override MouseHandlingResult HandleClick()
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class iPlanetName : InputAbstractBase
        {
            //when this char is entered in the box,
            //attempt to do auto-completion; ie if I've
            //typed a partial name in and only one planet
            //starts that way, just complete the name for me
            public static char autoCompleteChar = '@';

            public static iPlanetName Instance;
            public iPlanetName() { Instance = this; }
            private static readonly string DefaultText = "Enter Planet Name Here";
            public string PlanetName = DefaultText;
            public string CurrentValue = string.Empty;
            private bool MoveToEndOfLineOnNextUpdate;

            public override void HandleChangeInValue( string NewValue )
            {
                this.PlanetName = NewValue;
            }
            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= 13 )
                {
                    if ( !ArcenStrings.Equals( input, DefaultText ) )
                        return '\0';
                }
                if ( addedChar == '\t' )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Got a tab", Verbosity.DoNotShow );
                }
                if ( addedChar == '\r' || addedChar == '\n' )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Got a return", Verbosity.DoNotShow );
                }
                if ( !Char.IsLetterOrDigit( addedChar ) ) //must be alphanumeric
                {
                    if ( addedChar == autoCompleteChar )
                        attemptAutoCompletion();
                    return '\0';
                }
                if ( ArcenStrings.Equals( input, DefaultText ) )
                {
                    this.PlanetName = string.Empty + addedChar;
                    return '\0';
                }
                return addedChar;
            }

            private void attemptAutoCompletion()
            {
                //algorithm: take this.PlanetName and see if
                // it is a unique prefix to any planet name.
                //if so, update this.PlanetName to be the name of that planet
                List<string> matchingPlanetNames = new List<string>();
                Galaxy galaxy = Engine_AIW2.Instance.NonSim_GetGalaxyBeingCurrentlyViewed();
                for ( int i = 0; i < galaxy.Planets.Count; i++ )
                {
                    Planet planet = galaxy.Planets[i];
                    if ( !planet.HumansHaveBasicIntel )
                        continue;
                    string name = planet.Name;
                    var hasPrefix = name.StartsWith( this.PlanetName, StringComparison.InvariantCultureIgnoreCase );
                    if ( hasPrefix == true )
                        matchingPlanetNames.Add( name );
                }
                if ( matchingPlanetNames.Count == 1 )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Unique solution", Verbosity.DoNotShow );
                    this.PlanetName = matchingPlanetNames[0];
                    MoveToEndOfLineOnNextUpdate = true;
                }
                else if ( matchingPlanetNames.Count > 1 )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine( "multiple solution", Verbosity.DoNotShow );
                }
                else
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine( "no solution", Verbosity.DoNotShow );
                }

            }
            public override void OnUpdate()
            {
                ArcenUI_Input elementAsType = (ArcenUI_Input)Element;
                elementAsType.SetText( PlanetName );
                if ( MoveToEndOfLineOnNextUpdate )
                {
                    MoveToEndOfLineOnNextUpdate = false;
                    elementAsType.ReferenceInputField.MoveToEndOfLine( false, false );
                }
            }
        }

        public class bFindPlanet : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Find Planet" );
            }
            public override MouseHandlingResult HandleClick()
            {
                Galaxy galaxy = Engine_AIW2.Instance.NonSim_GetGalaxyBeingCurrentlyViewed();

                for ( int i = 0; i < galaxy.Planets.Count; i++ )
                {
                    Planet planet = galaxy.Planets[i];
                    if ( !planet.HumansHaveBasicIntel )
                        continue;
                    bool centerOnThisPlanet = false;

                    if ( string.Equals( planet.Name, iPlanetName.Instance.PlanetName, StringComparison.CurrentCultureIgnoreCase ) )
                    {
                        //Engine_AIW2.Instance.PresentationLayer.ReactToLeavingPlanetView( Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() );
                        centerOnThisPlanet = true;
                    }
                    //check the otherStructures dictionary
                    else if ( Window_FindPlanetMenu.otherStructures.ContainsKey( iPlanetName.Instance.PlanetName ) )
                    {
                        string tagForStructure = Window_FindPlanetMenu.otherStructures[iPlanetName.Instance.PlanetName];
                        planet.Combat.DoForEntities( EntityRollupType.DrawsInGalaxyView, delegate ( GameEntity entity )
                        {
                            if ( entity.TypeData.GetHasTag( tagForStructure ) )
                                centerOnThisPlanet = true;
                            return DelReturn.Continue;
                        } );
                    }
                    if ( centerOnThisPlanet )
                    {
                        World_AIW2.Instance.SwitchViewToPlanet( planet );
                        Engine_AIW2.Instance.SetCurrentGameViewMode( GameViewMode.GalaxyMapView );
                        Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( planet, false );
                        break;
                    }
                }
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class tAutoCompletion : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "Use the following character to attempt autocomplete: " );
                Buffer.Add( iPlanetName.autoCompleteChar );
            }

            public override void OnUpdate() { }
        }

        public override void OnOpen()
        {
            //TODO: make this actually select the element so that the user can type in the planet name without having to click on the element or navigate to it using WASD (which seems to work, oddly)
            ArcenUI_Input elementAsType = (ArcenUI_Input)iPlanetName.Instance.Element;
            elementAsType.ReferenceInputField.ActivateInputField();
            elementAsType.ReferenceInputField.Select();
        }
    }
}
