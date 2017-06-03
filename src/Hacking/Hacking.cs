using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public abstract class BaseHackingImplementation : IHackingImplementation
    {
        public virtual bool GetCanBeHacked( GameEntity Target, GameEntity Hacker )
        {
            return true;
        }

        public virtual FInt GetCostToHack( GameEntity Target, GameEntity Hacker )
        {
            return (FInt)ExternalConstants.Instance.Balance_BaseHackingScale;
        }

        public virtual int GetTotalSecondsToHack( GameEntity Target, GameEntity Hacker )
        {
            return 60;
        }

        public virtual void DoOneSecondOfHackingLogic( GameEntity Target, GameEntity Hacker, ArcenSimContext Context )
        {
            if ( Hacker.ActiveHack_DurationThusFar >= this.GetTotalSecondsToHack( Target, Hacker ) )
            {
                WaveLogic.SendWave( Context, Target.Side.WorldSide, Target.Side.WorldSide.AITypeData.BudgetItems[AIBudgetType.Wave].NormalMenusToBuyFrom, Target.Side.WorldSide.GetSpecificBudgetThreshold( AIBudgetType.Wave ) * 5, Target, null );
                if ( DoSuccessfulCompletionLogic( Target, Hacker, Context ) )
                    Hacker.Side.WorldSide.StoredHacking -= this.GetCostToHack( Target, Hacker );
            }
            else
            {
                if ( Hacker.ActiveHack_DurationThusFar % 10 == 0 )
                    WaveLogic.SendWave( Context, Target.Side.WorldSide, Target.Side.WorldSide.AITypeData.BudgetItems[AIBudgetType.Wave].NormalMenusToBuyFrom, Target.Side.WorldSide.GetSpecificBudgetThreshold( AIBudgetType.Wave ) * 1, Target, null );
            }
        }

        public abstract bool DoSuccessfulCompletionLogic( GameEntity Target, GameEntity Hacker, ArcenSimContext Context );
    }

    public class Hacking_DownloadDesign : BaseHackingImplementation
    {
        public override bool GetCanBeHacked( GameEntity Target, GameEntity Hacker )
        {
            if ( !base.GetCanBeHacked( Target, Hacker ) )
                return false;
            if ( Target.StoredDesign != null )
            {
                TechTypeData tech = Target.StoredDesign.TechPrereq;
                if ( Hacker.Side.WorldSide.UnlockedTechs.Contains( tech ) )
                    return false;
            }
            else if( Target.TypeData.GrantsTemporarilyWhileOwned.Count > 0 )
            {
                bool foundOne = false;
                for(int i = 0; i < Target.TypeData.GrantsTemporarilyWhileOwned.Count;i++)
                {
                    if ( Hacker.Side.WorldSide.UnlockedTechs.Contains( Target.TypeData.GrantsTemporarilyWhileOwned[i] ) )
                        continue;
                    foundOne = true;
                    break;
                }
                if ( !foundOne )
                    return false;
            }
            else
                return false;
            return true;
        }

        public override bool DoSuccessfulCompletionLogic( GameEntity Target, GameEntity Hacker, ArcenSimContext Context )
        {
            if ( Target.StoredDesign != null )
            {
                TechTypeData tech = Target.StoredDesign.TechPrereq;
                if ( !Hacker.Side.WorldSide.UnlockedTechs.Contains( tech ) )
                {
                    Hacker.Side.WorldSide.UnlockedTechs.Add( tech );
                    return true;
                }
            }
            else if ( Target.TypeData.GrantsTemporarilyWhileOwned.Count > 0 )
            {
                bool foundOne = false;
                for ( int i = 0; i < Target.TypeData.GrantsTemporarilyWhileOwned.Count; i++ )
                {
                    TechTypeData tech = Target.TypeData.GrantsTemporarilyWhileOwned[i];
                    if ( Hacker.Side.WorldSide.UnlockedTechs.Contains( tech ) )
                        continue;
                    Hacker.Side.WorldSide.UnlockedTechs.Add( tech );
                    foundOne = true;
                }
                if ( foundOne )
                    return true;
            }
            return false;
        }
    }

    public class Hacking_CorruptDesign : BaseHackingImplementation
    {
        public override bool GetCanBeHacked( GameEntity Target, GameEntity Hacker )
        {
            if ( !base.GetCanBeHacked( Target, Hacker ) )
                return false;
            if ( Target.StoredDesign == null )
                return false;
            if ( World_AIW2.Instance.CorruptedAIDesigns.Contains( Target.StoredDesign ) )
                return false;
            return true;
        }

        public override bool DoSuccessfulCompletionLogic( GameEntity Target, GameEntity Hacker, ArcenSimContext Context )
        {
            if ( World_AIW2.Instance.CorruptedAIDesigns.Contains( Target.StoredDesign ) )
                return false;
            World_AIW2.Instance.CorruptedAIDesigns.Add( Target.StoredDesign );
            Target.Die( Context );
            return true;
        }

        public override void DoOneSecondOfHackingLogic( GameEntity Target, GameEntity Hacker, ArcenSimContext Context )
        {
            if ( Hacker.ActiveHack_DurationThusFar >= this.GetTotalSecondsToHack( Target, Hacker ) )
            {
                WaveLogic.SendWave( Context, Target.Side.WorldSide, Target.Side.WorldSide.AITypeData.BudgetItems[AIBudgetType.Wave].NormalMenusToBuyFrom, Target.Side.WorldSide.GetSpecificBudgetThreshold( AIBudgetType.Wave ) * 5, Target, Target.StoredDesign );
                if ( DoSuccessfulCompletionLogic( Target, Hacker, Context ) )
                    Hacker.Side.WorldSide.StoredHacking -= this.GetCostToHack( Target, Hacker );
            }
            else
            {
                if ( Hacker.ActiveHack_DurationThusFar % 10 == 0 )
                    WaveLogic.SendWave( Context, Target.Side.WorldSide, Target.Side.WorldSide.AITypeData.BudgetItems[AIBudgetType.Wave].NormalMenusToBuyFrom, Target.Side.WorldSide.GetSpecificBudgetThreshold( AIBudgetType.Wave ) * 1, Target, Target.StoredDesign );
            }
        }
    }

    public class Hacking_CrackMissileSilo : BaseHackingImplementation
    {
        public override bool DoSuccessfulCompletionLogic( GameEntity Target, GameEntity Hacker, ArcenSimContext Context )
        {
            if ( Target.WarheadContents.Count <= 0 )
                return false;
            for (int i = 0; i < Target.WarheadContents.Count;i++)
            {
                EntityContentsRecord record = Target.WarheadContents[i];
                Hacker.ChangeWarheadContents( record.ContainedType, record.NumberContained );
                Target.ChangeWarheadContents( record.ContainedType, -record.NumberContained );
            }
            Target.Die( Context );
            return true;
        }
    }

    public class Hacking_SubvertSuperTerminal : BaseHackingImplementation
    {
        public override int GetTotalSecondsToHack( GameEntity Target, GameEntity Hacker )
        {
            return 10 * 200;
        }

        public override void DoOneSecondOfHackingLogic( GameEntity Target, GameEntity Hacker, ArcenSimContext Context )
        {
            if ( Hacker.ActiveHack_DurationThusFar >= this.GetTotalSecondsToHack( Target, Hacker ) )
            {
            }
            else
            {
                if ( Hacker.ActiveHack_DurationThusFar % 10 == 0 )
                {
                    FInt AIPPerTick = -( ExternalConstants.Instance.Balance_BaseAIPScale * FInt.FromParts( 0, 100 ) );
                    World_AIW2.Instance.ChangeAIP( AIPPerTick, AIPChangeReason.Hacking, Target.TypeData, Context );
                    int ticksThusFar = Hacker.ActiveHack_DurationThusFar / 10;
                    int totalWholeAIPReduced = ( ticksThusFar * AIPPerTick ).IntValue;
                    FInt waveMultiplier = FInt.One;
                    FInt multiplierMultiplierPerAIP = FInt.FromParts( 1, 030 );
                    for ( int i = 0; i < totalWholeAIPReduced; i++ )
                        waveMultiplier *= multiplierMultiplierPerAIP;
                    WaveLogic.SendWave( Context, Target.Side.WorldSide, Target.Side.WorldSide.AITypeData.BudgetItems[AIBudgetType.Wave].NormalMenusToBuyFrom, Target.Side.WorldSide.GetSpecificBudgetThreshold( AIBudgetType.Wave ) * waveMultiplier, Target, null );
                }
            }
        }

        public override bool DoSuccessfulCompletionLogic( GameEntity Target, GameEntity Hacker, ArcenSimContext Context )
        {
            return true;
        }
    }
}
