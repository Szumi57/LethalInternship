using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface INpcController
    {
        PlayerControllerB Npc { get; }
        IInternCullingBodyInfo InternCullingBodyInfo { get; set; }

        bool IsCameraDisabled { get; set; }
        bool IsJumping { get; set; }
        bool IsFallingFromJump { get; set; }
        float CrouchMeter { get; set; }
        bool IsWalking { get; set; }
        float PlayerSlidingTimer { get; set; }
        bool DisabledJetpackControlsThisFrame { get; set; }
        bool StartedJetpackControls { get; set; }
        float TimeSinceTakingGravityDamage { get; set; }
        bool TeleportingThisFrame { get; set; }
        float PreviousFrameDeltaTime { get; set; }
        float CameraUp { get; set; }
        float UpdatePlayerLookInterval { get; set; }
        bool UpdatePositionForNewlyJoinedClient { get; set; }
        public int PlayerMask { get; set; }

        bool IsTouchingGround { get; set; }
        EnemyAI? EnemyInAnimationWith { get; set; }
        bool IsControllerInCruiser { get; set; }
        bool HasToMove { get; }
        Vector3 MoveVector { get; }
        Vector3 NearEntitiesPushVector { get; set; }
        List<PlayerPhysicsRegion> CurrentInternPhysicsRegions { get; }
        bool GrabbedObjectValidated { get; set; }

        OccludeAudio OccludeAudioComponent { get; set; }
        AudioLowPassFilter AudioLowPassFilterComponent { get; set; }
        AudioHighPassFilter AudioHighPassFilterComponent { get; set; }

        void Awake();
        void Update();
        void LateUpdate();

        void OrderToMove();
        void OrderToStopMoving();
        void OrderToSprint();
        void OrderToStopSprint();
        void OrderToToggleCrouch();
        void OrderToLookForward();
        void OrderToLookAtPosition(Vector3 positionToLookAt);
        void OrderToLookAtPlayer(Vector3 positionPlayerEyeToLookAt);
        void OrderToLookAtMovingTarget(Transform movingTargetToLookAt);

        bool CheckConditionsForSinkingInQuicksandIntern();
        void PlayFootstep(bool isServer);
        void ShowFullNameBillboard();
        void MimicEmotes(PlayerControllerB playerToMimic);
        void PerformDefaultEmote(int emoteNumberToMimic);
        void PerformTooManyEmote(int tooManyEmoteID);
        void StopPerformingTooManyEmote();
        Vector3 GetBillBoardPosition(GameObject bodyModel, Vector3 lastPosition);
        void SetTurnBodyTowardsDirectionWithPosition(Vector3 positionDirection);
        void SetTurnBodyTowardsDirection(Vector3 direction);
        void ApplyUpdateInternAnimationsNotOwner(int animationState, float animationSpeed);
        void RefreshBillBoardPosition();
        void ReParentNotSpawnedTransform(Transform newParent);
        void SetAnimationBoolForItem(string animationString, bool value);
        void PlayAudibleNoiseIntern(Vector3 noisePosition,
                                    float noiseRange = 10f,
                                    float noiseLoudness = 0.5f,
                                    int timesPlayedInSameSpot = 0,
                                    bool noiseIsInsideClosedShip = false,
                                    int noiseID = 0);
        void UpdateNowTurnBodyTowardsDirection(Vector3 direction);
        void StopAnimations();

        float GetSqrDistanceWithLocalPlayer(Vector3 internBodyPos);
        Bounds GetBoundsModel(GameObject model);
    }
}
