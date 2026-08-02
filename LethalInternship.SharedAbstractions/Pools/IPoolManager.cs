namespace LethalInternship.SharedAbstractions.Pools
{
    public interface IPoolManager
    {
        T Get<T>() where T : class, new();
        void Return<T>(T obj) where T : class, new();
        void LogStats();
    }
}
