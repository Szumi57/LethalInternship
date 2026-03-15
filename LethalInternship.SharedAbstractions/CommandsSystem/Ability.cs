namespace LethalInternship.SharedAbstractions.CommandsSystem
{
    public abstract class Ability
    {
        public abstract bool RequiresTargeting { get; }
        public abstract void Activate();
    }
}
