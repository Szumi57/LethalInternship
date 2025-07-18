using System;

namespace LethalInternship.SharedAbstractions.Events
{
    public interface IPluginRuntimeEvents
    {
        event EventHandler? InitialSyncCompleted;
    }
}
