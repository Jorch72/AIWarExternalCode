using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class SpecialFaction_AntiAIZombie : ISpecialFactionImplementation
    {
        public static SpecialFaction_AntiAIZombie Instance;
        public SpecialFaction_AntiAIZombie() { Instance = this; }

        public void SetStartingSideRelationships( WorldSide side )
        {
            for ( int i = 0; i < World_AIW2.Instance.Sides.Count; i++ )
            {
                WorldSide otherSide = World_AIW2.Instance.Sides[i];
                if ( side == otherSide )
                    continue;
                switch ( otherSide.Type )
                {
                    case WorldSideType.Player:
                        side.MakeFriendlyTo( otherSide );
                        otherSide.MakeFriendlyTo( side );
                        break;
                    case WorldSideType.AI:
                        side.MakeHostileTo( otherSide );
                        otherSide.MakeHostileTo( side );
                        break;
                }
            }
        }

        public ArcenEnumIndexedArray_AIBudgetType<FInt> GetSpendingRatios( WorldSide side )
        {
            ArcenEnumIndexedArray_AIBudgetType<FInt> result = new ArcenEnumIndexedArray_AIBudgetType<FInt>();

            result[AIBudgetType.Reinforcement] = FInt.One;

            return result;
        }

        public bool GetShouldAttackNormallyExcludedTarget( WorldSide side, GameEntity Target )
        {
            return false;
        }

        public void SeedStartingEntities( WorldSide side, Galaxy galaxy, ArcenSimContext Context, MapTypeData mapType )
        {
        }

        public void DoLongRangePlanning( WorldSide side, ArcenLongTermPlanningContext Context )
        {
        }

        public void DoPerSimStepLogic( WorldSide side, ArcenSimContext Context )
        {
        }
    }

    public class SpecialFaction_AntiPlayerZombie : ISpecialFactionImplementation
    {
        public static SpecialFaction_AntiPlayerZombie Instance;
        public SpecialFaction_AntiPlayerZombie() { Instance = this; }

        public void SetStartingSideRelationships( WorldSide side )
        {
            for ( int i = 0; i < World_AIW2.Instance.Sides.Count; i++ )
            {
                WorldSide otherSide = World_AIW2.Instance.Sides[i];
                if ( side == otherSide )
                    continue;
                switch ( otherSide.Type )
                {
                    case WorldSideType.Player:
                        side.MakeHostileTo( otherSide );
                        otherSide.MakeHostileTo( side );
                        break;
                    case WorldSideType.AI:
                        side.MakeFriendlyTo( otherSide );
                        otherSide.MakeFriendlyTo( side );
                        break;
                }
            }
        }

        public ArcenEnumIndexedArray_AIBudgetType<FInt> GetSpendingRatios( WorldSide side )
        {
            ArcenEnumIndexedArray_AIBudgetType<FInt> result = new ArcenEnumIndexedArray_AIBudgetType<FInt>();

            result[AIBudgetType.Reinforcement] = FInt.One;

            return result;
        }

        public bool GetShouldAttackNormallyExcludedTarget( WorldSide side, GameEntity Target )
        {
            return false;
        }

        public void SeedStartingEntities( WorldSide side, Galaxy galaxy, ArcenSimContext Context, MapTypeData mapType )
        {
        }

        public void DoLongRangePlanning( WorldSide side, ArcenLongTermPlanningContext Context )
        {
        }

        public void DoPerSimStepLogic( WorldSide side, ArcenSimContext Context )
        {
        }
    }

    public class SpecialFaction_AntiEveryoneZombie : ISpecialFactionImplementation
    {
        public static SpecialFaction_AntiEveryoneZombie Instance;
        public SpecialFaction_AntiEveryoneZombie() { Instance = this; }

        public void SetStartingSideRelationships( WorldSide side )
        {
            for ( int i = 0; i < World_AIW2.Instance.Sides.Count; i++ )
            {
                WorldSide otherSide = World_AIW2.Instance.Sides[i];
                if ( side == otherSide )
                    continue;
                switch ( otherSide.Type )
                {
                    case WorldSideType.Player:
                    case WorldSideType.AI:
                    case WorldSideType.SpecialFaction:
                        side.MakeHostileTo( otherSide );
                        otherSide.MakeHostileTo( side );
                        break;
                }
            }
        }

        public ArcenEnumIndexedArray_AIBudgetType<FInt> GetSpendingRatios( WorldSide side )
        {
            ArcenEnumIndexedArray_AIBudgetType<FInt> result = new ArcenEnumIndexedArray_AIBudgetType<FInt>();

            result[AIBudgetType.Reinforcement] = FInt.One;

            return result;
        }

        public bool GetShouldAttackNormallyExcludedTarget( WorldSide side, GameEntity Target )
        {
            return false;
        }

        public void SeedStartingEntities( WorldSide side, Galaxy galaxy, ArcenSimContext Context, MapTypeData mapType )
        {
        }

        public void DoLongRangePlanning( WorldSide side, ArcenLongTermPlanningContext Context )
        {
        }

        public void DoPerSimStepLogic( WorldSide side, ArcenSimContext Context )
        {
        }
    }
}
