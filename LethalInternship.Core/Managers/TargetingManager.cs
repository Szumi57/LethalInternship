using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace LethalInternship.Core.Managers
{
    public class TargetingManager : MonoBehaviour
    {
        private static TargetingManager _instance = null!;
        public static TargetingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject(nameof(TargetingManager));
                    _instance = go.AddComponent<TargetingManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public enum TargetType
        {
            None = 0,
            Intern = 1 << 0,
            Enemy = 1 << 1,
            Item = 1 << 2
        }
        public TargetType ActiveSearch { get; private set; } = TargetType.Intern;

        private TargetData? currentTarget;

        private RaycastHit[] buffer = new RaycastHit[16];
        private float maxDistanceRay = 300f;

        private float angleWeight = 1.0f;
        private float distanceWeight = 0.5f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            PluginLoggerHook.LogInfo?.Invoke("Initializing TargetingManager...");
        }

        void Update()
        {
            if (StartOfRound.Instance == null
                || StartOfRound.Instance.localPlayerController == null)
                return;

            if (UIManager.Instance.IsAnyMenuOpened)
                return;

            if (Time.frameCount % 2 != 0) return;

            UpdateTarget();
        }

        public TargetData? GetCurrentTarget()
        {
            return currentTarget;
        }

        public void SetActiveSearch(TargetType targetType)
        {
            ActiveSearch = targetType;
            currentTarget = null;
        }

        public void UpdateTarget()
        {
            // Check for direct cast
            TargetData? directTarget = FindByRaycast();
            if (directTarget != null
                && directTarget.Value.IsTargetNotPointOfInterest())
            {
                // Pointed something directly
                currentTarget = directTarget;
                //PluginLoggerHook.LogDebug?.Invoke($"?? directTarget {currentTarget}");
                return;
            }
            else
            {
                //PluginLoggerHook.LogDebug?.Invoke($"directTarget null/empty");
            }

            // Check if already scanned something not too far (latching)
            if (currentTarget != null
                && currentTarget.Value.IsTargetNotPointOfInterest()
                && IsPointedTargetStillValid(currentTarget.Value))
            {
                //PluginLoggerHook.LogDebug?.Invoke($"?? IsPointedTargetStillValid target {currentTarget}");
                return;
            }

            // Else scan angle for something
            // -----------------------------

            // Scan for interns
            if (ActiveSearch.HasFlag(TargetType.Intern))
            {
                TargetData? internTarget = FindPointedInternByAngle();
                if (internTarget != null)
                {
                    //PluginLoggerHook.LogDebug?.Invoke($"++ scan angle internTarget {internTarget.Value.Intern?.Npc.playerUsername}");
                    currentTarget = internTarget;
                    return;
                }
            }

            // Scan for enemies
            if (ActiveSearch.HasFlag(TargetType.Enemy))
            {
                TargetData? enemyTarget = FindPointedEnemyByAngle();
                if (enemyTarget != null)
                {
                    //PluginLoggerHook.LogDebug?.Invoke($"++ scan angle enemyTarget {enemyTarget.Value.Enemy?.enemyType.enemyName}");
                    currentTarget = enemyTarget;
                    return;
                }
            }

            // Scan for items
            if (ActiveSearch.HasFlag(TargetType.Item))
            {
                TargetData? enemyItem = FindPointedItemByAngle();
                if (enemyItem != null)
                {
                    //PluginLoggerHook.LogDebug?.Invoke($"++ scan angle Item {enemyItem.Value.Item?.itemProperties.itemName}");
                    currentTarget = enemyItem;
                    return;
                }
            }

            // Get back direct target
            if (directTarget != null
                && directTarget.Value.IsTargetNotEmpty())
            {
                // Pointed something directly
                //PluginLoggerHook.LogDebug?.Invoke($"directTarget 2 !! target intern ? {directTarget.Value.Intern?.Npc.playerUsername} target enemy ? {directTarget.Value.Enemy?.enemyType.enemyName}");
                currentTarget = directTarget;
            }
            else
            {
                currentTarget = null;
            }
        }

        private TargetData? FindByRaycast()
        {
            Camera cam = StartOfRound.Instance.localPlayerController.gameplayCamera;
            Vector3 origin = cam.transform.position;
            Vector3 forward = cam.transform.forward;

            Ray ray = new Ray(origin, forward);
            LayerMask layerMask = StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers;
            if (ActiveSearch.HasFlag(TargetType.Enemy))
                layerMask |= 1 << 19;// "19: Enemies" StartOfRound.allPlayersCollideWithMask
            if (ActiveSearch.HasFlag(TargetType.Item))
                layerMask |= 64;// "6: Props" PlayerControllerB.grabbableObjectsMask

            int count = Physics.RaycastNonAlloc(ray, buffer, maxDistanceRay, layerMask);

            // find the colliders
            int nearestIndex = -1;
            float nearestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (buffer[i].collider.GetComponentInParent<IIgnoreRaycast>() != null)
                {
                    continue;
                }

                if (buffer[i].collider.gameObject.name.StartsWith("LineOfS")
                    || buffer[i].collider.gameObject.name.StartsWith("Collision"))
                {
                    continue;
                }

                float d = buffer[i].distance;
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearestIndex = i;
                }
            }
            if (nearestIndex < 0)
            {
                return null;
            }

            //PluginLoggerHook.LogDebug?.Invoke($"??????");
            //for (int i = 0; i < count; i++)
            //{
            //    if (buffer[i].collider.GetComponentInParent<IIgnoreRaycast>() != null)
            //    {
            //        continue;
            //    }

            //    if (buffer[i].collider.gameObject.name.StartsWith("LineOfS")
            //        || buffer[i].collider.gameObject.name.StartsWith("Collision"))
            //    {
            //        continue;
            //    }

            //    PluginLoggerHook.LogDebug?.Invoke($"?? {buffer[i].collider.gameObject.name} \"{buffer[i].collider.gameObject.GetComponent<PlayerControllerB>()?.playerUsername}\" layer:{buffer[i].collider.gameObject.layer} \"{LayerMask.LayerToName(buffer[i].collider.gameObject.layer)}\" | {buffer[i].collider.transform.parent?.name} {buffer[i].collider.transform.parent?.parent?.name} {buffer[i].collider.transform.parent?.parent?.parent?.name} {GetInternFromCollider(buffer[i].collider)?.Npc.playerUsername} {buffer[i].distance}");
            //}

            //PluginLoggerHook.LogDebug?.Invoke($"--> hit {buffer[nearestIndex].collider.gameObject.name} \"{buffer[nearestIndex].collider.gameObject.GetComponent<PlayerControllerB>()?.playerUsername}\" layer:{buffer[nearestIndex].collider.gameObject.layer} \"{LayerMask.LayerToName(buffer[nearestIndex].collider.gameObject.layer)}\" | {buffer[nearestIndex].collider.transform.parent?.name} {buffer[nearestIndex].collider.transform.parent?.parent?.name} {buffer[nearestIndex].collider.transform.parent?.parent?.parent?.name} {GetInternFromCollider(buffer[nearestIndex].collider)?.Npc.playerUsername} {buffer[nearestIndex].distance}");
            return BuildTarget(buffer[nearestIndex]);
        }

        private bool IsPointedTargetStillValid(TargetData target)
        {
            if (target.Root == null) return false; // quit game while targeting

            Camera localPlayerCamera = StartOfRound.Instance.localPlayerController.gameplayCamera;
            Transform transform = target.Root.transform;

            float distance = (StartOfRound.Instance.localPlayerController.transform.position - transform.position).sqrMagnitude;
            float angle = Vector3.Angle(localPlayerCamera.transform.forward, (transform.position + new Vector3(0f, 1f, 0f)) - localPlayerCamera.transform.position);
            float allowedAngle = GetAllowedAngle(distance) + GetFlickerMargin(distance); // anti flickering margin

            return angle <= allowedAngle
                && HasPlayerLineOfSightOn(target);
        }

        private TargetData? FindPointedInternByAngle()
        {
            IInternAI? bestPointedIntern = null;
            float bestScore = float.MaxValue;

            Camera localPlayerCamera = StartOfRound.Instance.localPlayerController.gameplayCamera;
            IInternAI[] internAIs = InternManager.Instance.GetAliveAndSpawnInternsAI();
            foreach (IInternAI internAI in internAIs)
            {
                if (StartOfRound.Instance.localPlayerController.isInsideFactory != internAI.Npc.isInsideFactory)
                {
                    continue;
                }
                // No action if in spawning animation
                if (internAI.IsSpawningAnimationRunning())
                {
                    continue;
                }

                float distance = internAI.NpcController.GetSqrDistanceWithLocalPlayer();
                float angle = internAI.GetAngleFOVWithLocalPlayer(localPlayerCamera.transform, internAI.Npc.transform.position
                                                                                               + new Vector3(0f, 2f * PluginRuntimeProvider.Context.Config.InternSizeScale * 0.80f, 0f));
                float allowedAngle = GetAllowedAngle(distance);

                //Debug.Log($"angle {angle.ToString("00.0")}, distance {Mathf.Sqrt(distance).ToString("00.0")}");

                if (angle > allowedAngle)
                {
                    continue;
                }

                if (!HasPlayerLineOfSightOn(internAI.Npc.transform.position
                                            + new Vector3(0f, 2f * PluginRuntimeProvider.Context.Config.InternSizeScale * 0.80f, 0f)))
                {
                    continue;
                }

                // Score best pointed intern
                float score = angle * angleWeight + distance * distanceWeight;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestPointedIntern = internAI;
                }
            }

            if (bestPointedIntern != null)
            {
                return new TargetData { Root = bestPointedIntern.GameObject, Intern = bestPointedIntern };
            }
            else { return null; }
        }

        private TargetData? FindPointedEnemyByAngle()
        {
            EnemyAI? bestPointedEnemy = null;
            float bestScore = float.MaxValue;

            Camera localPlayerCamera = StartOfRound.Instance.localPlayerController.gameplayCamera;
            List<EnemyAI> enemies = InternManager.Instance.GetEnemiesList();
            foreach (EnemyAI enemy in enemies)
            {
                if (enemy == null
                    || enemy.transform == null)
                {
                    continue;
                }
                if (enemy.isEnemyDead
                    || StartOfRound.Instance.localPlayerController.isInsideFactory == enemy.isOutside)
                {
                    continue;
                }

                float distance = (StartOfRound.Instance.localPlayerController.transform.position - enemy.transform.position).sqrMagnitude;
                float angle = Vector3.Angle(localPlayerCamera.transform.forward, (enemy.transform.position + new Vector3(0f, 0.3f, 0f)) - localPlayerCamera.transform.position);
                float allowedAngle = GetAllowedAngle(distance);

                if (angle > allowedAngle)
                {
                    continue;
                }

                if (!HasPlayerLineOfSightOn(enemy.transform.position
                                            + new Vector3(0f, 0.3f, 0f)))
                {
                    continue;
                }

                // Score best pointed intern
                float score = angle * angleWeight + distance * distanceWeight;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestPointedEnemy = enemy;
                }
            }

            if (bestPointedEnemy != null)
            {
                return new TargetData { Root = bestPointedEnemy.gameObject, Enemy = bestPointedEnemy };
            }
            else { return null; }
        }

        private TargetData? FindPointedItemByAngle()
        {
            GrabbableObject? bestPointedItem = null;
            float bestScore = float.MaxValue;

            Camera localPlayerCamera = StartOfRound.Instance.localPlayerController.gameplayCamera;
            bool isPlayerInside = StartOfRound.Instance.localPlayerController.isInsideFactory;
            List<GrabbableObject> items = InternManager.Instance.LookingForItemsToGrabInMap();
            foreach (GrabbableObject item in items)
            {
                if (item == null)
                {
                    continue;
                }

                // Object not outside when ai inside and vice versa
                Vector3 gameObjectPosition = gameObject.transform.position;
                if (!isPlayerInside && gameObjectPosition.y < -100f)
                {
                    continue;
                }
                else if (isPlayerInside && gameObjectPosition.y > -80f)
                {
                    continue;
                }

                float distance = (StartOfRound.Instance.localPlayerController.transform.position - item.transform.position).sqrMagnitude;
                float angle = Vector3.Angle(localPlayerCamera.transform.forward, item.transform.position - localPlayerCamera.transform.position);
                float allowedAngle = GetAllowedAngle(distance);

                if (angle > allowedAngle)
                {
                    continue;
                }

                if (!HasPlayerLineOfSightOn(item.transform.position))
                {
                    continue;
                }

                // Score best pointed intern
                float score = angle * angleWeight + distance * distanceWeight;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestPointedItem = item;
                }
            }

            if (bestPointedItem != null)
            {
                return new TargetData { Root = bestPointedItem.gameObject, Item = bestPointedItem };
            }
            else { return null; }
        }

        private bool HasPlayerLineOfSightOn(TargetData target)
        {
            return HasPlayerLineOfSightOn(target.Root.transform.position);
        }

        private bool HasPlayerLineOfSightOn(Vector3 pos)
        {
            return !Physics.Linecast(StartOfRound.Instance.localPlayerController.gameplayCamera.transform.position,
                                     pos,
                                     StartOfRound.Instance.collidersAndRoomMaskAndDefault,
                                     QueryTriggerInteraction.Ignore);
        }

        private float GetAllowedAngle(float distance)
        {
            float minDistance = Mathf.Pow(1f, 2);   // very close
            float maxDistance = Mathf.Pow(15f, 2);  // far

            float maxAngleClose = 15f; // degrees when very close
            float maxAngleFar = 5f;  // degrees when far

            float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
            return Mathf.Lerp(maxAngleClose, maxAngleFar, t);
        }

        private float GetFlickerMargin(float distance)
        {
            float minDistance = Mathf.Pow(1f, 2);   // very close
            float maxDistance = Mathf.Pow(15f, 2);  // far

            float maxAngleClose = 5f; // degrees when very close
            float maxAngleFar = 3f;  // degrees when far

            float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
            return Mathf.Lerp(maxAngleClose, maxAngleFar, t);
        }

        private TargetData BuildTarget(RaycastHit hit)
        {
            return BuildTarget(hit.collider, hit.point, hit.distance, 0f);
        }

        private TargetData BuildTarget(Collider col, Vector3? hitPoint, float distance, float angle)
        {
            IPointOfInterest? pointOfInterest = UIManager.Instance.GetPointOfInterestInCenter();

            // No point of interest pointed
            if (pointOfInterest == null)
            {
                if (IsColliderFromVehicle(col))
                {
                    pointOfInterest = InternManager.Instance.GetPointOfInterestOrVehicleInterestPoint(col.gameObject.GetComponentInParent<VehicleController>());
                }
                else if (IsColliderFromShip(col))
                {
                    Transform? shipTransform = GetParentShip(col.gameObject.transform);
                    if (shipTransform != null)
                    {
                        pointOfInterest = InternManager.Instance.GetPointOfInterestOrShipInterestPoint(shipTransform);
                    }
                }
                else if (hitPoint.HasValue)
                {
                    pointOfInterest = InternManager.Instance.GetPointOfInterestOrDefaultInterestPoint(hitPoint.Value);
                }
            }

            // BuildTarget
            TargetData targetData = new TargetData();
            targetData.Root = col.gameObject;
            targetData.Distance = distance;
            targetData.PointOfInterest = pointOfInterest;

            // Intern
            IInternAI? internAI = GetInternFromCollider(col);
            if (internAI != null
                && !internAI.IsEnemyDead
                && internAI.NpcController != null
                && internAI.NpcController.Npc != null
                && !internAI.Npc.isPlayerDead
                && !internAI.IsSpawningAnimationRunning())
            {
                //PluginLoggerHook.LogDebug?.Invoke($"--> directTarget !! target intern ? {internAI.Npc.playerUsername}");
                targetData.Intern = internAI;
                return targetData;
            }

            // Enemy
            EnemyAI enemyAI = col.gameObject.GetComponentInParent<EnemyAI>();
            if (enemyAI != null)
            {
                //PluginLoggerHook.LogDebug?.Invoke($"--> directTarget !! target enemy ? {enemyAI?.enemyType.enemyName}");
                targetData.Enemy = enemyAI;
                return targetData;
            }

            // Item
            targetData.Item = col.gameObject.GetComponentInParent<GrabbableObject>();
            //if (targetData.Item != null) { PluginLoggerHook.LogDebug?.Invoke($"--> directTarget !! target Item ? {targetData.Item.itemProperties.itemName}"); }

            return targetData;
        }

        private IInternAI? GetInternFromCollider(Collider col)
        {
            PlayerControllerB? controller = col.GetComponentInParent<PlayerControllerB>();
            if (controller != null)
            {
                return InternManager.Instance.GetInternAI((int)controller.playerClientId);
            }

            return null;
        }

        private bool IsColliderFromVehicle(Collider? collider)
        {
            return collider?.gameObject.GetComponentInParent<VehicleController>();
        }

        private bool IsColliderFromShip(Collider? collider)
        {
            return IsParentShip(collider?.gameObject.transform);
        }

        private bool IsParentShip(Transform? transform)
        {
            if (transform == null)
            {
                return false;
            }

            if (transform.name == "HangarShip")
            {
                return true;
            }

            return IsParentShip(transform.parent);
        }

        private Transform? GetParentShip(Transform? transform)
        {
            if (transform == null)
            {
                return null;
            }

            if (transform.name == "HangarShip")
            {
                return transform;
            }

            return GetParentShip(transform.parent);
        }
    }
}
