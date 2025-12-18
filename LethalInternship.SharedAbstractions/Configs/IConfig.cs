using LethalInternship.SharedAbstractions.NetworkSerializers;

namespace LethalInternship.SharedAbstractions.Configs
{
    public interface IConfig
    {
        // Internship program
        int MaxInternsAvailable { get; }
        int InternPrice { get; }
        int InternMaxHealth { get; }
        float InternSizeScale { get; }
        float InternSpeed { get; }

        string TitleInHelpMenu { get; }
        string SubTitleInHelpMenu { get; }

        bool CanSpectateInterns { get; }
        bool RadarEnabled { get; }

        // Identity  
        bool SpawnIdentitiesRandomly { get; }

        // Behaviour

        //bool CanLosePlayer { get; }
        bool CanUseWeapons { get; }
        bool FollowCrouchWithPlayer { get; }
        bool ChangeSuitAutoBehaviour { get; }
        int NbMaxCanCarry { get; }
        bool GrabItemsNearEntrances { get; }
        bool GrabBeesNest { get; }
        bool GrabDeadBodies { get; }
        bool GrabManeaterBaby { get; }
        bool GrabWheelbarrow { get; }
        bool GrabShoppingCart { get; }
        bool GrabKiwiBabyItem { get; }
        bool GrabApparatus { get; }
        bool TeleportedInternDropItems { get; }

        // Voice
        string VolumeVoicesMultiplierInterns { get; }
        int Talkativeness { get; }
        bool AllowSwearing { get; }
        string VolumeFootstepMultiplierInterns { get; }

        // Perf
        int MaxDefaultModelAnimatedInterns { get; }
        int MaxModelReplacementModelAnimatedInterns { get; }
        int MaxFootStepAudioInterns { get; }

        // Debug
        bool EnableDebugLog { get; }

        // Config identities
        ConfigIdentities ConfigIdentities { get; }

        string GetTitleInternshipProgram();
        float GetVolumeVoicesMultiplierInterns();
        float GetVolumeFootstepMultiplierInterns();
    }
}
