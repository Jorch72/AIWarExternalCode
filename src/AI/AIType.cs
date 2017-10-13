using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public abstract class BaseAITypeImplementation : IAITypeImplementation
    {
        public IAIBudgetController BudgetController;
        public IAIThreatController ThreatController;
        public IAISpecialForcesController SpecialForcesController;

        public BaseAITypeImplementation(IAIBudgetController BudgetController, IAIThreatController ThreatController, IAISpecialForcesController SpecialForcesController)
        {
            this.BudgetController = BudgetController;
            this.ThreatController = ThreatController;
            this.SpecialForcesController = SpecialForcesController;
        }

        public void CheckForSpendingUnlockPoints( ArcenSimContext Context )
        {
            this.BudgetController.CheckForSpendingUnlockPoints( Context );
        }

        public ArcenEnumIndexedArray_AIBudgetType<FInt> GetSpendingRatios( WorldSide side )
        {
            return this.BudgetController.GetSpendingRatios( side );
        }

        public Planet GetCurrentTargetPlanet( ArcenSimContext Context, GameEntity Base, List<GameEntity> CurrentFleet )
        {
            return this.SpecialForcesController.GetCurrentTargetPlanet( Context, Base, CurrentFleet );
        }

        public GameEntityTypeData GetNextFleetShipToBuy( ArcenSimContext Context, GameEntity Base, List<GameEntity> CurrentFleet, List<BuildMenu> Menus )
        {
            return this.SpecialForcesController.GetNextFleetShipToBuy( Context, Base, CurrentFleet, Menus );
        }

        public GameEntityTypeData GetNextGuardianToBuy( ArcenSimContext Context, GameEntity Base, List<GameEntity> CurrentFleet, List<BuildMenu> Menus )
        {
            return this.SpecialForcesController.GetNextGuardianToBuy( Context, Base, CurrentFleet, Menus );
        }

        public FInt GetRaidDesirability( WorldSide AISide, Planet planet )
        {
            return this.ThreatController.GetRaidDesirability( AISide, planet );
        }

        public FInt GetRaidTraversalDifficulty( WorldSide AISide, Planet planet )
        {
            return this.ThreatController.GetRaidTraversalDifficulty( AISide, planet );
        }

        public virtual void DoPerSimStepLogic( WorldSide side, ArcenSimContext Context ) { }
    }

    public interface IAIBudgetController
    {
        ArcenEnumIndexedArray_AIBudgetType<FInt> GetSpendingRatios( WorldSide side );
        void CheckForSpendingUnlockPoints( ArcenSimContext Context );
    }

    public interface IAIThreatController
    {
        FInt GetRaidDesirability( WorldSide AISide, Planet planet );
        FInt GetRaidTraversalDifficulty( WorldSide AISide, Planet planet );
    }

    public interface IAISpecialForcesController
    {
        GameEntityTypeData GetNextGuardianToBuy( ArcenSimContext Context, GameEntity Base, List<GameEntity> CurrentFleet, List<BuildMenu> Menus );
        GameEntityTypeData GetNextFleetShipToBuy( ArcenSimContext Context, GameEntity Base, List<GameEntity> CurrentFleet, List<BuildMenu> Menus );
        Planet GetCurrentTargetPlanet( ArcenSimContext Context, GameEntity Base, List<GameEntity> CurrentFleet );
    }

    public class AITypeController_Vanilla : BaseAITypeImplementation
    {
        public AITypeController_Vanilla()
            : base( new AIBudgetController_Vanilla(), new AIThreatController_Vanilla(), new AISpecialForcesController_Vanilla() )
        { }

        public override void DoPerSimStepLogic( WorldSide side, ArcenSimContext Context )
        {
            // do nothing, just example
        }
    }
}
