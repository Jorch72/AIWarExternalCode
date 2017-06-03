using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameControlGroupsMenu : ToggleableWindowController
    {
        public static Window_InGameControlGroupsMenu Instance;
        public Window_InGameControlGroupsMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        public class bToggleStandardGroupsMenu : WindowTogglingButtonController
        {
            public bToggleStandardGroupsMenu() : base( "Standard Groups", ">" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGameStandardGroupsMenu.Instance; }
        }

        public class bsControlGroups : ButtonSetAbstractBase
        {
            private DateTime TimeOfLastUpdate;

            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;

                if ( this.TimeOfLastUpdate < Engine_AIW2.Instance.TimeOfLastControlGroupChange )
                {
                    this.TimeOfLastUpdate = DateTime.Now;
                    elementAsType.ClearButtons();

                    int x = 0;
                    localSide.DoForControlGroups( delegate ( ControlGroup group )
                    {
                        bControlGroupItem newButtonController = new bControlGroupItem( group );
                        Vector2 offset;
                        offset.x = 0;
                        offset.y = x * elementAsType.ButtonHeight;
                        Vector2 size;
                        size.x = elementAsType.ButtonWidth;
                        size.y = elementAsType.ButtonHeight;
                        elementAsType.AddButton( newButtonController, size, offset );
                        x++;
                        return DelReturn.Continue;
                    } );

                    {
                        bCreateControlGroup newButtonController = new bCreateControlGroup();
                        Vector2 offset;
                        offset.x = 0;
                        offset.y = x * elementAsType.ButtonHeight;
                        Vector2 size;
                        size.x = elementAsType.ButtonWidth;
                        size.y = elementAsType.ButtonHeight;
                        elementAsType.AddButton( newButtonController, size, offset );
                        x++;
                    }

                    elementAsType.ActuallyDestroyButtonsThatAreStillCleared();
                }
            }
        }

        private class bControlGroupItem : ButtonAbstractBase
        {
            public ControlGroup ControlGroup;

            public bControlGroupItem( ControlGroup ControlGroup )
            {
                this.ControlGroup = ControlGroup;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( this.ControlGroup == null ? "NULL" : ControlGroup.Name );
            }

            public override void HandleClick()
            {
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

                bool isAssigningToGroup = Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.ModifyingControlGroup );

                if ( this.ControlGroup == null )
                    return;

                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                bool foundOne = false;

                if ( isAssigningToGroup )
                {
                    GameCommandType commandType = GameCommandType.SetControlGroupPopulation;
                    if ( unselectingInstead )
                        commandType = GameCommandType.RemoveFromControlGroupPopulation;
                    else if ( !clearSelectionFirst )
                        commandType = GameCommandType.AddToControlGroupPopulation;
                    else
                        commandType = GameCommandType.SetControlGroupPopulation;
                    GameCommand command = GameCommand.Create( commandType );
                    command.SentWithToggleSet_SetOrdersForProducedUnits = Engine_AIW2.Instance.SettingOrdersForProducedUnits;
                    command.RelatedControlGroup = this.ControlGroup;
                    Engine_AIW2.Instance.DoForSelected( delegate ( GameEntity selected )
                    {
                        command.RelatedEntityIDs.Add( selected.PrimaryKeyID );
                        return DelReturn.Continue;
                    } );
                    World_AIW2.Instance.QueueGameCommand( command );
                }
                else
                {
                    this.ControlGroup.DoForEntities( delegate ( GameEntity entity )
                    {
                        if ( entity.Combat.Planet != planet )
                            return DelReturn.Continue;
                        if ( !foundOne )
                        {
                            foundOne = true;
                            if ( clearSelectionFirst )
                            {
                                if ( entity.GetIsSelected() )
                                    Engine_AIW2.Instance.PresentationLayer.CenterPlanetViewOnEntity( entity, false );
                                Engine_AIW2.Instance.ClearSelection();
                            }
                        }
                        if ( unselectingInstead )
                            entity.Unselect();
                        else
                            entity.Select();
                        return DelReturn.Continue;
                    } );
                    if ( !foundOne && clearSelectionFirst )
                    {
                        Engine_AIW2.Instance.ClearSelection();
                        this.ControlGroup.DoForEntities( delegate ( GameEntity entity )
                        {
                            if ( !foundOne )
                            {
                                foundOne = true;
                                Engine_AIW2.Instance.PresentationLayer.ReactToLeavingPlanetView( planet );
                                planet = entity.Combat.Planet;
                                World_AIW2.Instance.SwitchViewToPlanet( planet );
                                Engine_AIW2.Instance.PresentationLayer.CenterPlanetViewOnEntity( entity, true );
                                Engine_AIW2.Instance.PresentationLayer.ReactToEnteringPlanetView( planet );
                            }
                            if ( entity.Combat.Planet != planet )
                                return DelReturn.Continue;
                            entity.Select();
                            return DelReturn.Continue;
                        } );
                    }
                    Window_InGameMasterMenu.Instance.CloseAllExpansions();
                }
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }

        private class bCreateControlGroup : ButtonAbstractBase
        {
            public bCreateControlGroup()
            {
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "(Create New)" );
            }

            public override void HandleClick()
            {
                GameCommand command = GameCommand.Create( GameCommandType.CreateNewControlGroup );
                command.RelatedSide = World_AIW2.Instance.GetLocalSide();
                World_AIW2.Instance.QueueGameCommand( command );
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }
    }
}