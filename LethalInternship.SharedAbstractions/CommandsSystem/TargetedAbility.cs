namespace LethalInternship.SharedAbstractions.CommandsSystem
{
    public abstract class TargetedAbility : Ability
    {
        public override bool RequiresTargeting => true;

        public override void Activate()
        {
            BeginTargeting();
        }

        protected abstract void BeginTargeting();
        public abstract Order? ResolveTarget(TargetData target);
    }
}
