using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameTracingMenu : ToggleableWindowController
    {
        public static Window_InGameTracingMenu Instance;
        public Window_InGameTracingMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.SupportsMasterMenuKeys = true;
        }

        public class bsMenuSelectionRow : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                WorldSide localSide = World_AIW2.Instance.GetLocalSide();
                if ( localSide == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)Element;
                //Window_InGameTracingMenu windowController = (Window_InGameTracingMenu)Element.Window.Controller;

                if ( elementAsType.Buttons.Count <= 0 )
                {
                    int x = 0;
                    for(int flagInt = 1; flagInt < (int)ArcenTracingFlags.Length; flagInt<<=1,x++ )
                    {
                        ArcenTracingFlags flag = (ArcenTracingFlags)flagInt;
                        bItem newButtonController = new bItem( flag );
                        Vector2 offset;
                        offset.x = 0;
                        offset.y = x * elementAsType.ButtonHeight;
                        Vector2 size;
                        size.x = elementAsType.ButtonWidth;
                        size.y = elementAsType.ButtonHeight;
                        elementAsType.AddButton( newButtonController, size, offset );
                    }
                }
            }
        }

        private class bItem : ButtonAbstractBase
        {
            public ArcenTracingFlags Flag;

            public bItem( ArcenTracingFlags Flag )
            {
                this.Flag = Flag;
            }

            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShow( buffer );
                buffer.Add( this.Flag.ToString() );
                buffer.Add( " (" );
                if ( Engine_AIW2.Instance.TracingFlags.Has( this.Flag ) )
                    buffer.Add( "On" );
                else
                    buffer.Add( "Off" );
                buffer.Add( ")" );
            }

            public override void HandleClick()
            {
                if ( Engine_AIW2.Instance.TracingFlags.Has( this.Flag ) )
                    Engine_AIW2.Instance.TracingFlags = Engine_AIW2.Instance.TracingFlags.Remove( this.Flag );
                else
                    Engine_AIW2.Instance.TracingFlags = Engine_AIW2.Instance.TracingFlags.Add( this.Flag );
                if ( this.Flag == ArcenTracingFlags.Performance )
                    Engine_Universal.TracePerformance = Engine_AIW2.Instance.TracingFlags.Has( ArcenTracingFlags.Performance );
            }

            public override void HandleMouseover() { }

            public override void OnUpdate()
            {
            }
        }
    }
}