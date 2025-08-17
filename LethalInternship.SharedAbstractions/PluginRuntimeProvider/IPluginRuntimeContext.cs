using LethalInternship.SharedAbstractions.Configs;
using LethalInternship.SharedAbstractions.Inputs;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.PluginRuntimeProvider
{
    public interface IPluginRuntimeContext
    {
        string Plugin_Guid { get; }
        string Plugin_Version { get; }
        string Plugin_Name { get; }

        string ConfigPath { get; }
        string VoicesPath { get; }

        EnemyType InternNPCPrefab { get; }

        // UI
        bool UIAssetsLoaded { get; }

        GameObject MainUICommands { get; }

        GameObject WorldIconPrefab { get; }
        GameObject InputIconPrefab { get; }

        GameObject DefaultIconImagePrefab { get; }
        GameObject PointerIconImagePrefab { get; }
        GameObject PedestrianIconImagePrefab { get; }
        GameObject VehicleIconImagePrefab { get; }
        GameObject ShipIconImagePrefab { get; }
        GameObject MeetingPointIconImagePrefab { get; }
        GameObject GatheringPointIconImagePrefab { get; }
        GameObject AttackIconImagePrefab { get; }

        string DirectoryName { get; }
        IConfig Config { get; }
        ILethalInternshipInputs InputActionsInstance { get; }
        int PluginIrlPlayersCount { get; set; }

        bool IsModTooManyEmotesLoaded { get; }
        bool IsModModelReplacementAPILoaded { get; }
        bool IsModCustomItemBehaviourLibraryLoaded { get; }
        bool IsModMoreCompanyLoaded { get; }
        bool IsModReviveCompanyLoaded { get; }
        bool IsModBunkbedReviveLoaded { get; }
        bool IsModLethalMinLoaded { get; }
        bool IsModMipaLoaded { get; }
        bool IsModMonoProfilerLoaderLoaded { get; }
    }
}
