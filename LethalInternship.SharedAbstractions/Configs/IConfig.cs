using LethalInternship.SharedAbstractions.NetworkSerializers;

namespace LethalInternship.SharedAbstractions.Configs
{
    public interface IConfig
    {
        public int MaxInternsAvailable { get; }
        public int InternPrice { get; }
        public int InternMaxHealth { get; }
        public float InternSizeScale { get; }

        public string TitleInHelpMenu { get; }
        public string SubTitleInHelpMenu { get; }

        public bool CanSpectateInterns { get; }
        public bool RadarEnabled { get; }

        public bool SpawnIdentitiesRandomly { get; }

        public bool FollowCrouchWithPlayer { get; }
        public bool ChangeSuitAutoBehaviour { get; }
        public bool GrabItemsNearEntrances { get; }
        public bool GrabBeesNest { get; }
        public bool GrabDeadBodies { get; }
        public bool GrabManeaterBaby { get; }
        public bool GrabWheelbarrow { get; }
        public bool GrabShoppingCart { get; }

        public bool TeleportedInternDropItems { get; }

        // Voic
        public string VolumeVoicesMultiplierInterns { get; }
        public int Talkativeness { get; }
        public bool AllowSwearing { get; }
        public string VolumeFootstepMultiplierInterns { get; }

        // Perf
        public int MaxDefaultModelAnimatedInterns { get; }
        public int MaxModelReplacementModelAnimatedInterns { get; }
        public int MaxFootStepAudioInterns { get; }

        // Debug
        public bool EnableDebugLog { get; }

        // Config identities
        public ConfigIdentities ConfigIdentities { get; }

        public string GetTitleInternshipProgram();
        public float GetVolumeVoicesMultiplierInterns();
        public float GetVolumeFootstepMultiplierInterns();
    }
}
