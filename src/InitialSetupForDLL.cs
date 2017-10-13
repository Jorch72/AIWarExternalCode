using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using Arcen.AIW2.External;

namespace Arcen.AIW2.External
{
    public class InitialSetupForDLL : IInitialSetupForDLL
    {
        public void RunInitialSetup()
        {
            ArcenInput.PreInput = new PreInputHandler();
        }
    }

    public class PreInputHandler : IPreInputHandler
    {
        public void DoActionsJustBeforeInput()
        {
            //these are all things that need to be held down in order to stay on.
            //so we only clear them to false right before the input check
            ArcenInput_AIW2.ShouldShowShipRanges_Selected = false;
            ArcenInput_AIW2.ShouldShowShipRanges_Hovered = false;
            ArcenInput_AIW2.ShouldShowShipRanges_All = false;
            ArcenInput_AIW2.ShouldShowShipOrders = false;
        }
    }
}