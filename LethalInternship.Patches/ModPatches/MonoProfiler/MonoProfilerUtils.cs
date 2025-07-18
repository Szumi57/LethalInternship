using LethalInternship.SharedAbstractions.Hooks.MonoProfilerHooks;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using MonoProfiler;
using System.IO;

namespace LethalInternship.Patches.ModPatches.MonoProfiler
{
    public class MonoProfilerUtils
    {
        public static void Init()
        {
            MonoProfilerHook.DumpMonoProfilerFile = DumpMonoProfilerFile;
        }

        public static void DumpMonoProfilerFile()
        {
            try
            {
                FileInfo dumpFile = MonoProfilerPatcher.RunProfilerDump();
                PluginLoggerHook.LogDebug?.Invoke("-----------------------Saved profiler dump to " + dumpFile.FullName);
            }
            catch
            {
                PluginLoggerHook.LogDebug?.Invoke("Could not dump profiler file. Ignore if not wanted.");
            }
        }
    }
}
