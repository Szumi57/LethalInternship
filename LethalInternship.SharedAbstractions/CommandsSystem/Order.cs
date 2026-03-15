using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.SharedAbstractions.CommandsSystem
{
    public abstract class Order
    {
        public abstract void ApplyTo(IInternAI intern);
    }
}
