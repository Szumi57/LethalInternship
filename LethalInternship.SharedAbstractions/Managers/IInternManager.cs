using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.Adapters;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.NetworkSerializers;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IInternManager
    {
        GameObject ManagerGameObject { get; }

        int IndexBeginOfInterns { get; }
        int AllEntitiesCount { get; }
        Vector3 ItemDropShipPos { get; set; }
        List<int> HeldInternsLocalPlayer { get; set; }
        bool IsServer { get; }
        Dictionary<EnemyAI, INoiseListener> DictEnemyAINoiseListeners { get; }
        List<IBodyReplacementBase> ListBodyReplacementOnDeadBodies { get; set; }
        Transform? ShipTransform { get; }
        VehicleController? VehicleController { get; }

        IInternAI? GetInternAI(int playerClientId);
        IInternAI? GetInternAIByInternId(int internId);
        void ManagePoolOfInterns();
        void SyncEndOfRoundInterns();

        bool IsPlayerIntern(PlayerControllerB player);
        bool IsIdPlayerIntern(int id);
        bool IsIdPlayerInternOwnerLocal(int idPlayer);
        bool AreInternsScheduledToLand();
        bool IsPlayerLocalOrInternOwnerLocal(PlayerControllerB player);
        bool IsColliderFromLocalOrInternOwnerLocal(Collider collider);
        bool IsPlayerInternOwnerLocal(PlayerControllerB player);
        bool IsPlayerInternControlledAndOwner(PlayerControllerB player);
        bool IsAnInternAiOwnerOfObject(GrabbableObject grabbableObject);

        int GetDamageFromSlimeIfIntern(PlayerControllerB player);
        IInternAI? GetInternAIIfLocalIsOwner(int index);
        IInternAI[] GetInternsAIOwnedByLocal();
        IInternAI[] GetAliveAndSpawnInternsAIOwnedByLocal();
        IInternAI? GetInternAiOwnerOfObject(GrabbableObject grabbableObject);
        IInternAI[] GetInternsAiHoldByPlayer(int idPlayerHolder);

        void SyncLoadedJsonIdentitiesServerRpc(ulong clientId);
        void SetInternsInElevatorLateUpdate(float deltaTime);
        void UpdateAllInternsVoiceEffects();
        void ResetIdentities();
        void SpawnInternsFromDropShip(Transform[] spawnPositions);
        void TeleportOutInterns(ShipTeleporter teleporter, Random shipTeleporterSeed);
        void VehicleHasLanded();
        void SpawnThisInternServerRpc(int identityID, SpawnInternsParamsNetworkSerializable spawnInternsParamsNetworkSerializable);
        void SyncGroupCreditsForNotOwnerTerminalServerRpc(int newGroupCredits, int numItemsInShip);
        void UpdateReviveCountServerRpc(int id);
        IInternCullingBodyInfo? GetInternCullingBodyInfo(GameObject gameObject);
        void HideShowRagdollModel(PlayerControllerB internController, bool show);
        void UpdateReviveCompanyRemainingRevivesServerRpc(string identityName);
        int MaxHealthPercent(int percentage, int maxHealth);
        bool DidAnInternJustTalkedClose(int idInternTryingToTalk);
        void PlayAudibleNoiseForIntern(int internID,
                                       Vector3 noisePosition,
                                       float noiseRange = 10f,
                                       float noiseLoudness = 0.5f,
                                       int noiseID = 0);
        void HideShowInternControllerModel(GameObject internObject, bool show);

        // Shovel
        bool ShouldShovelIgnoreIntern(Shovel shovel, Transform transform);

        List<EnemyAI> GetEnemiesList();

        bool ShouldIgnoreInternsEndScreen(PlayerControllerB player);
    }
}
