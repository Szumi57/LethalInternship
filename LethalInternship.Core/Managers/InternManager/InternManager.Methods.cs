using GameNetcodeStuff;
using LethalInternship.Core.Interns.AI;
using LethalInternship.Core.Interns.AI.TimedTasks;
using LethalInternship.SharedAbstractions.Adapters;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        public Transform? ShipTransform
        {
            get
            {
                if (shipTransform == null)
                {
                    shipTransform = GameObject.Find("HangarShip").GetComponent<Transform>();
                }
                return shipTransform;
            }
        }
        private Transform? shipTransform = null!;

        public VehicleController? VehicleController { get => vehicleController; }
        private VehicleController? vehicleController;

        public List<IBodyReplacementBase> ListBodyReplacementOnDeadBodies { get => listBodyReplacementOnDeadBodies; set => listBodyReplacementOnDeadBodies = value; }
        private List<IBodyReplacementBase> listBodyReplacementOnDeadBodies = new List<IBodyReplacementBase>();

        public Dictionary<EnemyAI, INoiseListener> DictEnemyAINoiseListeners { get => dictEnemyAINoiseListeners; }
        private Dictionary<EnemyAI, INoiseListener> dictEnemyAINoiseListeners = new Dictionary<EnemyAI, INoiseListener>();

        public Dictionary<string, int> DictTagSurfaceIndex = new Dictionary<string, int>();
        public bool LandingStatusAllowed;

        private Coroutine registerItemsCoroutine = null!;

        private float timerIsAnInternScheduledToLand;
        private bool isAnInternScheduledToLand;

        private float timerSetInternInElevator;

        private float timerRegisterAINoiseListener;
        private List<EnemyAI> ListEnemyAINonNoiseListeners = new List<EnemyAI>();

        private TimedGetEnemies GetEnemiesTimed = new TimedGetEnemies();

        public void ResizePlayerVoiceMixers(int irlPlayersAndInternsCount)
        {
            // From moreCompany
            SoundManager instanceSM = SoundManager.Instance;
            Array.Resize<AudioMixerGroup>(ref instanceSM.playerVoiceMixers, irlPlayersAndInternsCount);
            AudioMixerGroup audioMixerGroup = Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault((AudioMixerGroup x) => x.name.StartsWith("VoicePlayer"));
            for (int i = IndexBeginOfInterns; i < irlPlayersAndInternsCount; i++)
            {
                instanceSM.playerVoiceMixers[i] = audioMixerGroup;
            }
        }

        /// <summary>
        /// Are interns bought and ready to drop on moon ?
        /// </summary>
        /// <returns><c>true</c> if a positive number of interns are scheduled to dropship on moon, <c>false</c> otherwise or on company moon</returns>
        public bool AreInternsScheduledToLand()
        {
            // no drop of interns on company building moon
            if (StartOfRound.Instance.currentLevel.levelID == Const.COMPANY_BUILDING_MOON_ID)
            {
                return false;
            }

            return isAnInternScheduledToLand && LandingStatusAllowed;
        }

        public void VehicleHasLanded()
        {
            vehicleController = Object.FindObjectOfType<VehicleController>();
            PluginLoggerHook.LogDebug?.Invoke($"Vehicle has landed : {vehicleController}");
        }

        public void ResetIdentities()
        {
            IdentityManager.Instance.InitIdentities(PluginRuntimeProvider.Context.Config.ConfigIdentities.configIdentities);
        }

        public void RegisterItems()
        {
            if (registerItemsCoroutine == null)
            {
                registerItemsCoroutine = StartCoroutine(RegisterItemsCoroutine());
            }
        }

        private IEnumerator RegisterItemsCoroutine()
        {
            if (HoarderBugAI.grabbableObjectsInMap == null)
            {
                yield break;
            }

            HoarderBugAI.grabbableObjectsInMap.Clear();
            yield return null;

            GrabbableObject[] array = Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            PluginLoggerHook.LogDebug?.Invoke($"Intern register grabbable object, found : {array.Length}");
            for (int i = 0; i < array.Length; i++)
            {
                GrabbableObject grabbableObject = array[i];
                if (!grabbableObject.grabbableToEnemies || grabbableObject.deactivated)
                {
                    continue;
                }

                Vector3 floorPosition;
                bool inShipRoom = false;
                bool inElevator = false;
                floorPosition = grabbableObject.GetItemFloorPosition(default(Vector3));
                if (StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(floorPosition))
                {
                    inElevator = true;
                    inShipRoom = true;
                }
                else if (StartOfRound.Instance.shipBounds.bounds.Contains(floorPosition))
                {
                    inElevator = true;
                }
                GameNetworkManager.Instance.localPlayerController.SetItemInElevator(inElevator, inShipRoom, grabbableObject);

                HoarderBugAI.grabbableObjectsInMap.Add(grabbableObject.gameObject);
                yield return null;
            }

            registerItemsCoroutine = null!;
            yield break;
        }

        private void CheckIsAnInternScheduledToLand()
        {
            timerIsAnInternScheduledToLand += Time.deltaTime;
            if (timerIsAnInternScheduledToLand > 1f)
            {
                timerIsAnInternScheduledToLand = 0f;
                isAnInternScheduledToLand = IdentityManager.Instance.IsAnIdentityToDrop();
            }
        }

        private void RegisterAINoiseListener(float deltaTime)
        {
            timerRegisterAINoiseListener += deltaTime;
            if (timerRegisterAINoiseListener < 1f)
            {
                return;
            }

            timerRegisterAINoiseListener = 0f;
            RoundManager instanceRM = RoundManager.Instance;
            foreach (EnemyAI spawnedEnemy in instanceRM.SpawnedEnemies)
            {
                if (ListEnemyAINonNoiseListeners.Contains(spawnedEnemy))
                {
                    continue;
                }
                else if (DictEnemyAINoiseListeners.ContainsKey(spawnedEnemy))
                {
                    continue;
                }

                INoiseListener noiseListener;
                if (spawnedEnemy.gameObject.TryGetComponent<INoiseListener>(out noiseListener))
                {
                    PluginLoggerHook.LogDebug?.Invoke($"new enemy noise listener, spawnedEnemy {spawnedEnemy}");
                    DictEnemyAINoiseListeners.Add(spawnedEnemy, noiseListener);
                }
                else
                {
                    PluginLoggerHook.LogDebug?.Invoke($"new enemy not noise listener, spawnedEnemy {spawnedEnemy}");
                    ListEnemyAINonNoiseListeners.Add(spawnedEnemy);
                }
            }
        }


        /// <summary>
        /// Get <c>InternAI</c> from <c>PlayerControllerB</c> <c>playerClientId</c>
        /// </summary>
        /// <param name="playerClientId"><c>playerClientId</c> of <c>PlayerControllerB</c></param>
        /// <returns><c>InternAI</c> if the <c>PlayerControllerB</c> has an <c>InternAI</c> associated, else returns null</returns>
        public IInternAI? GetInternAI(int playerClientId)
        {
            if (IndexBeginOfInterns == StartOfRound.Instance.allPlayerScripts.Length
                || playerClientId < IndexBeginOfInterns)
            {
                // Real player
                return null;
            }

            return AllInternAIs[playerClientId - IndexBeginOfInterns];
        }

        public IInternAI? GetInternAIByInternId(int internId)
        {
            return AllInternAIs[internId];
        }

        /// <summary>
        /// Get <c>InternAI</c> from <c>PlayerControllerB.playerClientId</c>, 
        /// only if the local client calling the method is the owner of <c>InternAI</c>
        /// </summary>
        /// <param name="index"></param>
        /// <returns><c>InternAI</c> if the <c>PlayerControllerB</c> has an <c>InternAI</c> associated and the local client is the owner, 
        /// else returns null</returns>
        public IInternAI? GetInternAIIfLocalIsOwner(int index)
        {
            IInternAI? internAI = GetInternAI(index);
            if (internAI != null
                && internAI.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId)
            {
                return internAI;
            }

            return null;
        }

        /// <summary>
        /// Get the <c>InternAI</c> that hold the <c>GrabbableObject</c>
        /// </summary>
        /// <param name="grabbableObject">Object held by the <c>InternAI</c> the method is looking for</param>
        /// <returns><c>InternAI</c> if holding the <c>GrabbableObject</c>, else returns null</returns>
        public IInternAI? GetInternAiOwnerOfObject(GrabbableObject grabbableObject)
        {
            foreach (var internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.IsEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled)
                {
                    continue;
                }

                if (internAI.IsHoldingItem(grabbableObject))
                {
                    return internAI;
                }
            }

            return null;
        }

        public IInternAI[] GetInternsAiHoldByPlayer(int idPlayerHolder)
        {
            List<IInternAI> results = new List<IInternAI>();

            foreach (var internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.IsEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled)
                {
                    continue;
                }

                if (internAI.RagdollInternBody.IsRagdollBodyHeldByPlayer(idPlayerHolder))
                {
                    results.Add(internAI);
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// Is the <c>PlayerControllerB.playerClientId</c> corresponding to an index of a intern in <c>allPlayerScripts</c>, 
        /// a <c>PlayerControllerB</c> that has <c>InternAI</c>
        /// </summary>
        /// <param name="id"><c>PlayerControllerB.playerClientId</c></param>
        /// <returns><c>true</c> if <c>PlayerControllerB</c> has <c>InternAI</c>, else <c>false</c></returns>
        public bool IsIdPlayerIntern(int id)
        {
            return id >= IndexBeginOfInterns;
        }

        /// <summary>
        /// Is the <c>PlayerControllerB</c> corresponding to an intern in <c>allPlayerScripts</c>, 
        /// a <c>PlayerControllerB</c> that has <c>InternAI</c>
        /// </summary>
        /// <returns><c>true</c> if <c>PlayerControllerB</c> has <c>InternAI</c>, else <c>false</c></returns>
        public bool IsPlayerIntern(PlayerControllerB? player)
        {
            if (player == null) return false;
            IInternAI? internAI = GetInternAI((int)player.playerClientId);
            return internAI != null;
        }

        /// <summary>
        /// Is the <c>PlayerControllerB</c> the local player or the body of an intern whose owner of <c>InternAI</c> is the local player ?
        /// </summary>
        /// <remarks>
        /// Used by the patches for deciding if the behaviour of the code still applies if the original game code encounters a 
        /// <c>PlayerControllerB</c> that is an intern
        /// </remarks>
        /// <param name="player"></param>
        public bool IsPlayerLocalOrInternOwnerLocal(PlayerControllerB player)
        {
            if (player == null)
            {
                return false;
            }
            if (player == GameNetworkManager.Instance.localPlayerController)
            {
                return true;
            }

            IInternAI? internAI = GetInternAI((int)player.playerClientId);
            if (internAI == null)
            {
                return false;
            }

            return internAI.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId;
        }

        /// <summary>
        /// Is the collider a <c>PlayerControllerB</c> that is the local player, or an intern that is owned by the local player ?
        /// </summary>
        public bool IsColliderFromLocalOrInternOwnerLocal(Collider collider)
        {
            PlayerControllerB player = collider.gameObject.GetComponent<PlayerControllerB>();
            return IsPlayerLocalOrInternOwnerLocal(player);
        }

        /// <summary>
        /// Is the <c>PlayerControllerB</c> the body of an intern whose owner of <c>InternAI</c> is the local player ?
        /// </summary>
        /// <remarks>
        /// Used by the patches for deciding if the behaviour of the code still applies if the original game code encounters a 
        /// <c>PlayerControllerB</c> who is an intern
        /// </remarks>
        /// <param name="player"></param>
        public bool IsPlayerInternOwnerLocal(PlayerControllerB player)
        {
            if (player == null)
            {
                return false;
            }

            IInternAI? internAI = GetInternAI((int)player.playerClientId);
            if (internAI == null)
            {
                return false;
            }

            return internAI.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId;
        }

        /// <summary>
        /// Is the <c>PlayerControllerB.playerClientId</c> the body of an intern whose owner of <c>InternAI</c> is the local player ?
        /// </summary>
        /// <remarks>
        /// Used by the patches for deciding if the behaviour of the code still applies if the original game code encounters a 
        /// <c>PlayerControllerB</c> that is an intern
        /// </remarks>
        /// <param name="idPlayer"><c>playerClientId</c> of <c>PlayerControllerB</c></param>
        public bool IsIdPlayerInternOwnerLocal(int idPlayer)
        {
            IInternAI? internAI = GetInternAI(idPlayer);
            if (internAI == null)
            {
                return false;
            }

            return internAI.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId;
        }

        public bool IsPlayerInternControlledAndOwner(PlayerControllerB player)
        {
            return IsPlayerInternOwnerLocal(player) && player.isPlayerControlled;
        }

        public bool IsAnInternAiOwnerOfObject(GrabbableObject grabbableObject)
        {
            IInternAI? internAI = GetInternAiOwnerOfObject(grabbableObject);
            if (internAI == null)
            {
                return false;
            }

            return true;
        }

        public IInternAI[] GetInternsAIOwnedByLocal()
        {
            return AllInternAIs.Where(x => x != null
                                        && x.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId)
                               .ToArray();
        }

        public IInternAI[] GetAliveAndSpawnInternsAIOwnedByLocal()
        {
            return AllInternAIs.Where(x => x != null
                                        && x.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId
                                        && !x.IsEnemyDead
                                        && x.NpcController != null
                                        && x.NpcController.Npc != null
                                        && !x.NpcController.Npc.isPlayerDead
                                        && x.NpcController.Npc.isPlayerControlled
                                        && x.InternIdentity.Status == EnumStatusIdentity.Spawned)
                               .ToArray();
        }

        public void SetInternsInElevatorLateUpdate(float deltaTime)
        {
            timerSetInternInElevator += deltaTime;
            if (timerSetInternInElevator < 0.5f)
            {
                return;
            }
            timerSetInternInElevator = 0f;

            foreach (InternAI internAI in AllInternAIs)
            {
                if (internAI == null)
                {
                    continue;
                }

                internAI.SetInternInElevator();
            }
        }

        public bool IsLocalPlayerNextToChillInterns()
        {
            foreach (var internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.IsEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled)
                {
                    continue;
                }

                if (internAI.OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsLocalPlayerHoldingInterns()
        {
            foreach (var internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.IsEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled)
                {
                    continue;
                }

                if (internAI.RagdollInternBody != null
                    && internAI.RagdollInternBody.IsRagdollBodyHeldByPlayer((int)GameNetworkManager.Instance.localPlayerController.playerClientId))
                {
                    return true;
                }
            }

            return false;
        }

        public int GetDamageFromSlimeIfIntern(PlayerControllerB player)
        {
            if (IsPlayerIntern(player))
            {
                return 5;
            }

            return 35;
        }

        public int MaxHealthPercent(int percentage, int maxHealth)
        {
            int healthPercent = (int)(((double)percentage / (double)100) * (double)maxHealth);
            return healthPercent < 1 ? 1 : healthPercent;
        }

        public List<EnemyAI> GetEnemiesList()
        {
            return GetEnemiesTimed.GetEnemiesList();
        }
    }
}
