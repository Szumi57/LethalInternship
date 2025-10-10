namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IInstruction
    {
        int IdBatch { get; }
        int GroupId { get; }
        void Execute();
    }
}
