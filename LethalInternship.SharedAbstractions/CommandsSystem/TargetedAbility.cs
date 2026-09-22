using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.SharedAbstractions.CommandsSystem
{
    public abstract class TargetedAbility : Ability
    {
        protected TargetedAbility(IEnumerable<IInternIdentity> identities) : base(identities)
        {
        }

        public abstract HashSet<GameAction> NotInterruptingActions { get; }
        public abstract HashSet<GameAction> SubmitActions { get; }
        public override bool RequiresTargeting => true;

        public override void Activate()
        {
            BeginTargeting();
        }

        protected abstract void BeginTargeting();
        public abstract Order? ResolveTarget(TargetData target);
    }
}
