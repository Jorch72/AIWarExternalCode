using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class DeathEffect_Zombification : IDeathEffectImplementation
    {
        public void HandleDeathWithEffectApplied( GameEntity Entity, int ThisDeathEffectDamageSustained, WorldSide SideThatDidTheKilling, WorldSide SideResponsibleForTheDeathEffect, ArcenSimContext Context )
        {
            if ( SideResponsibleForTheDeathEffect == null )
                return;
            if ( !Entity.GetMatches( EntityRollupType.MobileCombatants ) )
                return;
            ISpecialFactionImplementation implementationToSearchFor = null;            
            if ( SideResponsibleForTheDeathEffect.Type == WorldSideType.AI )
                implementationToSearchFor = SpecialFaction_AntiPlayerZombie.Instance;
            else if ( SideResponsibleForTheDeathEffect.Type == WorldSideType.Player )
                implementationToSearchFor = SpecialFaction_AntiAIZombie.Instance;
            else
                implementationToSearchFor = SpecialFaction_AntiEveryoneZombie.Instance;
            WorldSide destinationSide = World_AIW2.Instance.GetSideBySpecialFactionImplementation( implementationToSearchFor );
            if ( destinationSide == null )
                return;
            CombatSide sideForNewEntity = Entity.Combat.GetSideForWorldSide( destinationSide );
            GameEntity.CreateNew( sideForNewEntity, Entity.TypeData, Entity.WorldLocation, Context );
        }
    }
}
