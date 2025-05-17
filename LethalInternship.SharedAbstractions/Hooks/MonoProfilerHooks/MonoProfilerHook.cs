namespace LethalInternship.SharedAbstractions.Hooks.MonoProfilerHooks
{
    public delegate void DumpMonoProfilerFileDelegate();

    public class MonoProfilerHook
    {
        public static DumpMonoProfilerFileDelegate? DumpMonoProfilerFile;
    }
}
