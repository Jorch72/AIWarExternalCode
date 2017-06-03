using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace Arcen.AIW2.External
{
    public class Window_PausedInfo : WindowControllerAbstractBase
    {
        public Window_PausedInfo()
        {
            this.OnlyShowInGame = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( !World_AIW2.Instance.IsPaused )
                return false;
            return true;
        }

        public class tText : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( World_AIW2.Instance.IsPaused )
                    Buffer.Add( "PAUSED!" );
            }

            public override void OnUpdate()
            {
                //ArcenUI_Text elementAsType = (ArcenUI_Text)Element;
                //if ( DateTime.Now.TimeOfDay.TotalSeconds / 4 < 2 )
                //    elementAsType.SetColor( ColorMath.LightGreen );
                //else
                //    elementAsType.SetColor( ColorMath.LightRed );
            }
        }
    }
}