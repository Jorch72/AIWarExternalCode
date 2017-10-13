using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public abstract class BaseConductImplementation : IConductImplementation
    {
        public bool IsEnabled;
        public void ReceiveNotificationOfEnabledStatus( bool IsEnabled )
        {
            this.IsEnabled = IsEnabled;
        }

        public virtual void DoPerSimStepLogic( ArcenSimContext Context ) { }
    }

    public class Conduct_StartWithFirstPlanetAlreadyCaptured : BaseConductImplementation
    {
        public static Conduct_StartWithFirstPlanetAlreadyCaptured Instance;
        public Conduct_StartWithFirstPlanetAlreadyCaptured() { Instance = this; }
    }
}
