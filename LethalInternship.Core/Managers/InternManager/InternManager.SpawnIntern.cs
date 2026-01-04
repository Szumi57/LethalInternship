using GameNetcodeStuff;
using LethalInternship.Core.Interns;
using LethalInternship.Core.Interns.AI;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.ModelReplacementAPIHooks;
using LethalInternship.SharedAbstractions.Hooks.PlayerControllerBHooks;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.NetworkSerializers;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        public Vector3 ItemDropShipPos { get => itemDropShipPos; set => itemDropShipPos = value; }
        private Vector3 itemDropShipPos;

        public RagdollGrabbableObject[] RagdollInternBodies = null!;

        #region Spawn Intern

        /// <summary>
        /// Rpc method on server spawning network object from intern prefab and calling the client
        /// </summary>
        /// <param name="spawnPosition">Where the interns will spawn</param>
        /// <param name="yRot">Rotation of the interns when spawning</param>
        /// <param name="isOutside">Spawning outside or inside the facility (used for initializing AI Nodes)</param>
        [ServerRpc(RequireOwnership = false)]
        public void SpawnInternServerRpc(SpawnInternsParamsNetworkSerializable spawnInternsParamsNetworkSerializable)
        {
            if (AllInternAIs == null || AllInternAIs.Length == 0)
            {
                PluginLoggerHook.LogError?.Invoke($"Fatal error : client #{NetworkManager.LocalClientId} no interns initialized ! Please check for previous errors in the console");
                return;
            }

            int identityID = -1;
            // Get selected identities
            int[] selectedIdentities = IdentityManager.Instance.GetIdentitiesToDrop();
            if (selectedIdentities.Length > 0)
            {
                identityID = selectedIdentities[0];
            }

            if (identityID < 0)
            {
                PluginLoggerHook.LogInfo?.Invoke($"Try to spawn intern, no more intern identities available.");
                return;
            }

            //IdentityManager.Instance.InternIdentities[identityID].Status = EnumStatusIdentity.Spawned;
            spawnInternsParamsNetworkSerializable.InternIdentityID = identityID;
            SpawnInternServer(spawnInternsParamsNetworkSerializable);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnThisInternServerRpc(int identityID, SpawnInternsParamsNetworkSerializable spawnInternsParamsNetworkSerializable)
        {
            if (AllInternAIs == null || AllInternAIs.Length == 0)
            {
                PluginLoggerHook.LogError?.Invoke($"Fatal error : client #{NetworkManager.LocalClientId} no interns initialized ! Please check for previous errors in the console.");
                return;
            }

            if (identityID < 0)
            {
                PluginLoggerHook.LogInfo?.Invoke($"Failed to spawn specific intern identity with id {identityID}.");
                return;
            }

            spawnInternsParamsNetworkSerializable.InternIdentityID = identityID;
            SpawnInternServer(spawnInternsParamsNetworkSerializable);
        }

        private void SpawnInternServer(SpawnInternsParamsNetworkSerializable spawnInternsParamsNetworkSerializable)
        {
            int indexNextPlayerObject = GetNextAvailablePlayerObject();
            if (indexNextPlayerObject < 0)
            {
                PluginLoggerHook.LogInfo?.Invoke($"No more intern can be spawned at the same time, see MaxInternsAvailable value : {PluginRuntimeProvider.Context.Config.MaxInternsAvailable}");
                return;
            }
            int indexNextIntern = indexNextPlayerObject - IndexBeginOfInterns;

            NetworkObject networkObject;
            IInternAI internAI = AllInternAIs[indexNextIntern];
            if (internAI == null
                || internAI.NetworkObject == null)
            {
                // Or spawn one (server only)
                GameObject internPrefab = Object.Instantiate<GameObject>(PluginRuntimeProvider.Context.InternNPCPrefab.enemyPrefab);
                internAI = internPrefab.GetComponent<IInternAI>();
                AllInternAIs[indexNextIntern] = internAI;

                networkObject = internPrefab.GetComponentInChildren<NetworkObject>();
                networkObject.Spawn(true);
            }
            else
            {
                // Use internAI if exists
                networkObject = AllInternAIs[indexNextIntern].NetworkObject;
            }

            // Get an identity for the intern
            internAI.InternIdentity = IdentityManager.Instance.InternIdentities[spawnInternsParamsNetworkSerializable.InternIdentityID];

            // Choose suit
            int suitID;
            if (PluginRuntimeProvider.Context.Config.ChangeSuitAutoBehaviour)
            {
                suitID = GameNetworkManager.Instance.localPlayerController.currentSuitID;
            }
            else
            {
                suitID = internAI.InternIdentity.SuitID ?? internAI.InternIdentity.GetRandomSuitID();
            }

            // Spawn ragdoll dead bodies of intern
            NetworkObject networkObjectRagdollBody = SpawnRagdollBodies((int)StartOfRound.Instance.allPlayerScripts[indexNextPlayerObject].playerClientId);

            // Send to client to spawn intern
            spawnInternsParamsNetworkSerializable.IndexNextIntern = indexNextIntern;
            spawnInternsParamsNetworkSerializable.IndexNextPlayerObject = indexNextPlayerObject;
            spawnInternsParamsNetworkSerializable.InternIdentityID = internAI.InternIdentity.IdIdentity;
            spawnInternsParamsNetworkSerializable.SuitID = suitID;

            //SpawnInternClientRpc(networkObject, networkObjectRagdollBody, spawnInternsParamsNetworkSerializable);
            StartCoroutine(WaitBeforeSpawnOnClient(networkObject, networkObjectRagdollBody, spawnInternsParamsNetworkSerializable));
        }

        IEnumerator WaitBeforeSpawnOnClient(NetworkObjectReference networkObjectReferenceInternAI,
                                            NetworkObjectReference networkObjectReferenceRagdollInternBody,
                                            SpawnInternsParamsNetworkSerializable spawnParamsNetworkSerializable)
        {
            yield return null;
            SpawnInternClientRpc(networkObjectReferenceInternAI, networkObjectReferenceRagdollInternBody, spawnParamsNetworkSerializable);
        }

        /// <summary>
        /// Get the index of the next <c>PlayerControllerB</c> not controlled and ready to be hooked to an <c>InternAI</c>
        /// </summary>
        /// <returns></returns>
        private int GetNextAvailablePlayerObject()
        {
            StartOfRound instance = StartOfRound.Instance;
            for (int i = IndexBeginOfInterns; i < instance.allPlayerScripts.Length; i++)
            {
                if (!instance.allPlayerScripts[i].isPlayerControlled)
                {
                    return i;
                }
            }
            return -1;
        }

        private NetworkObject SpawnRagdollBodies(int playerClientId)
        {
            StartOfRound instanceSOR = StartOfRound.Instance;
            NetworkObject networkObjectRagdoll;

            // Spawn grabbable ragdoll intern body of intern
            RagdollGrabbableObject? ragdollInternBody = RagdollInternBodies[playerClientId];
            if (ragdollInternBody == null)
            {
                GameObject gameObject = Object.Instantiate<GameObject>(instanceSOR.ragdollGrabbableObjectPrefab, instanceSOR.propsContainer);
                networkObjectRagdoll = gameObject.GetComponent<NetworkObject>();
                networkObjectRagdoll.Spawn(false);
                ragdollInternBody = gameObject.GetComponent<RagdollGrabbableObject>();
                ragdollInternBody.bodyID.Value = Const.INIT_RAGDOLL_ID;
                RagdollInternBodies[playerClientId] = ragdollInternBody;
            }
            else
            {
                networkObjectRagdoll = ragdollInternBody.gameObject.GetComponent<NetworkObject>();
                if (!networkObjectRagdoll.IsSpawned)
                {
                    networkObjectRagdoll.Spawn(false);
                }
            }
            return networkObjectRagdoll;
        }

        /// <summary>
        /// Client side, when receiving <c>NetworkObjectReference</c> for the <c>InternAI</c> spawned on server,
        /// adds it to its corresponding arrays
        /// </summary>
        /// <param name="networkObjectReferenceInternAI"><c>NetworkObjectReference</c> for the <c>InternAI</c> spawned on server</param>
        /// <param name="indexNextIntern">Corresponding index in <c>AllInternAIs</c> for the body <c>GameObject</c> at another index in <c>allPlayerObjects</c></param>
        /// <param name="indexNextPlayerObject">Corresponding index in <c>allPlayerObjects</c> for the body of intern</param>
        /// <param name="spawnPosition">Where the interns will spawn</param>
        /// <param name="yRot">Rotation of the interns when spawning</param>
        /// <param name="isOutside">Spawning outside or inside the facility (used for initializing AI Nodes)</param>
        [ClientRpc]
        private void SpawnInternClientRpc(NetworkObjectReference networkObjectReferenceInternAI,
                                          NetworkObjectReference networkObjectReferenceRagdollInternBody,
                                          SpawnInternsParamsNetworkSerializable spawnParamsNetworkSerializable)
        {
            PluginLoggerHook.LogInfo?.Invoke($"Client receive RPC to spawn intern... position : {spawnParamsNetworkSerializable.SpawnPosition}, yRot: {spawnParamsNetworkSerializable.YRot}");

            if (AllInternAIs == null || AllInternAIs.Length == 0)
            {
                PluginLoggerHook.LogError?.Invoke($"Fatal error : client #{NetworkManager.LocalClientId} no interns initialized ! Please check for previous errors in the console");
                return;
            }

            // Get internAI from server
            networkObjectReferenceInternAI.TryGet(out NetworkObject networkObjectInternAI);
            InternAI internAI = networkObjectInternAI.gameObject.GetComponent<InternAI>();
            AllInternAIs[spawnParamsNetworkSerializable.IndexNextIntern] = internAI;

            // Get ragdoll body from server
            networkObjectReferenceRagdollInternBody.TryGet(out NetworkObject networkObjectRagdollGrabbableObject);
            RagdollGrabbableObject ragdollBody = networkObjectRagdollGrabbableObject.gameObject.GetComponent<RagdollGrabbableObject>();

            // Check for identites correctness
            if (spawnParamsNetworkSerializable.InternIdentityID >= IdentityManager.Instance.InternIdentities.Length)
            {
                IdentityManager.Instance.ExpandWithNewDefaultIdentities(numberToAdd: 1);
            }

            InitInternSpawning(internAI, ragdollBody,
                               spawnParamsNetworkSerializable);
        }

        /// <summary>
        /// Initialize intern by initializing body (<c>PlayerControllerB</c>) and brain (<c>InternAI</c>) to default values.
        /// Attach the brain to the body, attach <c>InternAI</c> <c>Transform</c> to the <c>GameObject</c> of the <c>PlayerControllerB</c>.
        /// </summary>
        /// <param name="internAI"><c>InternAI</c> to initialize</param>
        /// <param name="indexNextPlayerObject">Corresponding index in <c>allPlayerObjects</c> for the body of intern</param>
        /// <param name="spawnPosition">Where the interns will spawn</param>
        /// <param name="yRot">Rotation of the interns when spawning</param>
        /// <param name="isOutside">Spawning outside or inside the facility (used for initializing AI Nodes)</param>
        private void InitInternSpawning(InternAI internAI, RagdollGrabbableObject ragdollBody,
                                        SpawnInternsParamsNetworkSerializable spawnParamsNetworkSerializable)
        {
            StartOfRound instance = StartOfRound.Instance;
            IInternIdentity internIdentity = IdentityManager.Instance.InternIdentities[spawnParamsNetworkSerializable.InternIdentityID];

            GameObject objectParent = instance.allPlayerObjects[spawnParamsNetworkSerializable.IndexNextPlayerObject];
            objectParent.transform.position = spawnParamsNetworkSerializable.SpawnPosition;
            objectParent.transform.rotation = Quaternion.Euler(new Vector3(0f, spawnParamsNetworkSerializable.YRot, 0f));

            PlayerControllerB internController = instance.allPlayerScripts[spawnParamsNetworkSerializable.IndexNextPlayerObject];
            internController.playerUsername = internIdentity.Name;
            internController.isPlayerDead = false;
            internController.isPlayerControlled = true;
            internController.playerActions = new PlayerActions();
            internController.health = PluginRuntimeProvider.Context.Config.InternMaxHealth;
            DisableInternControllerModel(objectParent, internController, enable: true, disableLocalArms: true);
            internController.isInsideFactory = !spawnParamsNetworkSerializable.IsOutside;
            internController.isMovementHindered = 0;
            internController.hinderedMultiplier = 1f;
            internController.criticallyInjured = false;
            internController.bleedingHeavily = false;
            internController.activatingItem = false;
            internController.twoHanded = false;
            internController.inSpecialInteractAnimation = false;
            internController.freeRotationInInteractAnimation = false;
            internController.disableSyncInAnimation = false;
            internController.disableLookInput = false;
            internController.inAnimationWithEnemy = null;
            internController.holdingWalkieTalkie = false;
            internController.speakingToWalkieTalkie = false;
            internController.isSinking = false;
            internController.isUnderwater = false;
            internController.sinkingValue = 0f;
            internController.sourcesCausingSinking = 0;
            internController.isClimbingLadder = false;
            internController.setPositionOfDeadPlayer = false;
            internController.mapRadarDotAnimator.SetBool(Const.MAPDOT_ANIMATION_BOOL_DEAD, false);
            internController.externalForceAutoFade = Vector3.zero;
            internController.voiceMuffledByEnemy = false;
            internController.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_LIMP, false);
            internController.climbSpeed = Const.CLIMB_SPEED;
            internController.usernameBillboardText.enabled = true;

            FieldInfo fieldInfo = typeof(PlayerControllerB).GetField("updatePositionForNewlyJoinedClient", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fieldInfo.SetValue(internController, true);

            // CleanLegsFromMoreEmotesMod
            CleanLegsFromMoreEmotesMod(internController);

            // internAI
            internAI.InternId = Array.IndexOf(AllInternAIs, internAI);
            internAI.creatureAnimator = internController.playerBodyAnimator;
            internAI.AdaptController(internController);
            internAI.eye = internController.GetComponentsInChildren<Transform>().First(x => x.name == "PlayerEye");
            internAI.InternIdentity = internIdentity;
            internAI.InternIdentity.Hp = spawnParamsNetworkSerializable.Hp == 0 ? PluginRuntimeProvider.Context.Config.InternMaxHealth : spawnParamsNetworkSerializable.Hp;
            internAI.InternIdentity.SuitID = spawnParamsNetworkSerializable.SuitID;
            internAI.InternIdentity.Status = EnumStatusIdentity.Spawned;
            internAI.SetEnemyOutside(spawnParamsNetworkSerializable.IsOutside);

            // Attach ragdoll body
            internAI.RagdollInternBody = new RagdollInternBody(ragdollBody);

            // Plug ai on intern body
            internAI.enabled = false;
            internAI.NetworkObject.AutoObjectParentSync = false;
            internAI.transform.parent = objectParent.transform;
            internAI.NetworkObject.AutoObjectParentSync = true;
            internAI.enabled = true;

            objectParent.SetActive(true);

            // Unsuscribe from events to prevent double trigger
            PlayerControllerBHook.OnDisable_ReversePatch?.Invoke(internController);

            // Destroy dead body of identity
            if (spawnParamsNetworkSerializable.ShouldDestroyDeadBody)
            {
                if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded
                    && internIdentity.BodyReplacementBase != null)
                {
                    ModelReplacementAPIHook.RemovePlayerModelReplacement?.Invoke(internIdentity.BodyReplacementBase);
                    internIdentity.BodyReplacementBase = null;
                }
                if (internIdentity.DeadBody != null)
                {
                    Object.Destroy(internIdentity.DeadBody.gameObject);
                    internIdentity.DeadBody = null;
                }
            }
            // Remove deadbody on controller
            if (internController.deadBody != null)
            {
                internController.deadBody = null;
            }

            // Register body for animation culling
            RegisterInternBodyForAnimationCulling(internController);

            // Switch suit
            internAI.ChangeSuitIntern(internController.playerClientId, internAI.InternIdentity.SuitID.Value);

            // Show model replacement
            if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
            {
                ModelReplacementAPIHook.HideShowModelReplacement?.Invoke(internAI.Npc, show: true);
            }

            // Radar name update
            foreach (var radarTarget in instance.mapScreen.radarTargets)
            {
                if (radarTarget != null
                    && radarTarget.transform == internController.transform)
                {
                    radarTarget.name = internController.playerUsername;
                    break;
                }
            }

            // Init intern
            PluginLoggerHook.LogDebug?.Invoke($"++ Intern with body {internController.playerClientId} with identity spawned: {internIdentity.ToString()}");
            internAI.Init((EnumSpawnAnimation)spawnParamsNetworkSerializable.enumSpawnAnimation);
        }

        /// <summary>
        /// Manual DisablePlayerModel, for compatibility with mod LethalPhones, does not trigger patch of DisablePlayerModel in LethalPhones
        /// </summary>
        public void DisableInternControllerModel(GameObject internObject, PlayerControllerB internController, bool enable = false, bool disableLocalArms = false)
        {
            HideShowInternControllerModel(internObject, enable);
            if (disableLocalArms)
            {
                internController.thisPlayerModelArms.enabled = false;
            }
        }

        private void CleanLegsFromMoreEmotesMod(PlayerControllerB internController)
        {
            GameObject? gameObject = internController.playerBodyAnimator.transform.Find("FistPersonLegs")?.gameObject;
            if (gameObject != null)
            {
                PluginLoggerHook.LogDebug?.Invoke($"{internController.playerUsername}: Cleaning legs from more emotes");
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        #endregion

        #region SpawnInternsFromDropShip

        /// <summary>
        /// Spawn intern from dropship after opening the doors, all around the dropship
        /// </summary>
        /// <param name="spawnPositions">Positions of spawn for interns</param>
        public void SpawnInternsFromDropShip(Transform[] spawnPositions)
        {
            StartCoroutine(SpawnInternsCoroutine(spawnPositions));
        }

        private IEnumerator SpawnInternsCoroutine(Transform[] spawnPositions)
        {
            yield return null;
            int pos = 0;
            int nbInternsToDropShip = IdentityManager.Instance.GetNbIdentitiesToDrop();
            for (int i = 0; i < nbInternsToDropShip; i++)
            {
                if (pos > 3)
                {
                    pos = 0;
                }
                Transform transform = spawnPositions[pos++];
                SpawnInternServerRpc(new SpawnInternsParamsNetworkSerializable()
                {
                    enumSpawnAnimation = (int)EnumSpawnAnimation.RagdollFromDropShipAndPlayerSpawnAnimation,
                    SpawnPosition = transform.position,
                    YRot = transform.eulerAngles.y,
                    IsOutside = true
                });

                yield return new WaitForSeconds(0.3f);
            }
        }

        #endregion
    }
}
