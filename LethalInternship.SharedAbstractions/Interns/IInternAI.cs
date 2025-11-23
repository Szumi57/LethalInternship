using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.Adapters;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IInternAI
    {
        INpcController NpcController { get; }
        PlayerControllerB Npc { get; }
        IInternIdentity InternIdentity { get; set; }
        IRagdollInternBody RagdollInternBody { get; set; }
        bool AnimationCoroutineRagdollingRunning { get; }
        List<IBodyReplacementBase> ListModelReplacement { get; }

        GameObject GameObject { get; }
        ulong OwnerClientId { get; }
        bool IsOwner { get; }
        NetworkObject NetworkObject { get; }
        Transform Transform { get; }

        bool IsSpawned { get; }
        bool IsEnemyDead { get; }

        IPointOfInterest? GetPointOfInterest();
        void SetCommandToFollowPlayer(bool playVoice = true);
        void SetCommandToScavenging();
        void SetCommandTo(IPointOfInterest pointOfInterest, bool playVoice = true);

        void AdaptController(PlayerControllerB playerControllerB);
        void UpdateController();

        void SyncJump();
        void SyncLandFromJump(bool fallHard);
        void DropItem(GrabbableObject itemToDrop);
        void DropFirstPickedUpItem();
        void DropLastPickedUpItem();
        void DropTwoHandItem();
        void DropAllItems(bool waitBetweenItems = true);
        void StopSinkingState();
        void SyncDamageIntern(int damageNumber,
                              CauseOfDeath causeOfDeath = CauseOfDeath.Unknown,
                              int deathAnimation = 0,
                              bool fallDamage = false,
                              Vector3 force = default);
        void DamageInternFromOtherClientServerRpc(int damageAmount, Vector3 hitDirection, int playerWhoHit);
        void SyncKillIntern(Vector3 bodyVelocity,
                            bool spawnBody = true,
                            CauseOfDeath causeOfDeath = CauseOfDeath.Unknown,
                            int deathAnimation = 0,
                            Vector3 positionOffset = default);
        void UpdateInternSpecialAnimationValue(bool specialAnimation, float timed, bool climbingLadder);
        void SyncDeadBodyPositionServerRpc(Vector3 newBodyPosition);
        void StartPerformingEmoteInternServerRpc(int emoteID);
        void TeleportIntern(Vector3 pos, bool? setOutside = null, bool isUsingEntrance = false);
        bool IsSpawningAnimationRunning();
        bool AreHandsFree();
        bool AreFreeSlotsAvailable();
        bool CanHoldItem(GrabbableObject grabbableObject);
        bool IsHoldingItem(GrabbableObject grabbableObject);
        void UpdateItemOffsetsWhileHeld();
        bool IsHoldingTwoHandedItem();
        void UpdateItemRotation(GrabbableObject grabbableObject);
        bool IsClientOwnerOfIntern();
        void SyncStopPerformingEmote();
        void SyncChangeSinkingState(bool startSinking, float sinkingSpeed = 0f, int audioClipIndex = 0);
        void SyncDisableJetpackMode();
        void UpdateInternAnimationServerRpc(int animationState, float animationSpeed);
        void SyncUpdateInternRotationAndLook(string stateIndicator, Vector3 direction, int intEnumObjectsLookingAt, Vector3 playerEyeToLookAt, Vector3 positionToLookAt);
        void SyncUpdateInternPosition(Vector3 newPos, bool inElevator, bool inShipRoom, bool exhausted, bool isPlayerGrounded);
        void SyncSetFaceUnderwaterServerRpc(bool isUnderwater);
        string GetSizedBillboardStateIndicator();
        void HealthRegen();
        void PerformTooManyEmoteInternServerRpc(int tooManyEmoteID);
        void StopPerformTooManyEmoteInternServerRpc();
        void HideShowLevelStickerBetaBadge(bool show);
        void ChangeSuitInternServerRpc(ulong idInternController, int suitID);
        void SyncReleaseIntern(PlayerControllerB playerGrabberController);
        void SyncAssignTargetAndSetMovingTo(PlayerControllerB newTarget);
        void GrabInternServerRpc(ulong idPlayerGrabberController);
        void GiveItemToInternServerRpc(ulong playerClientIdGiver, NetworkObjectReference networkObjectReference);
        void PlayAudioServerRpc(string smallPathAudioClip, int enumTalkativeness);

        // Npc adapter
        Vector3 GetBillBoardPosition(GameObject bodyModel);

        float GetAngleFOVWithLocalPlayer(Transform localPlayerCameraTransform, Vector3 internBodyPos);

        float GetClosestPlayerDistance();

        // GiantKiwi
        void SyncWatchingThreatGiantKiwiServerRpc(NetworkObjectReference giantKiwiNOR);
        void SyncAttackingThreatGiantKiwiServerRpc(NetworkObjectReference giantKiwiNOR);

        // RadMech
        void SyncSetTargetToThreatServerRpc(NetworkObjectReference radMechNOR, Vector3 lastSeenPos);
    }
}