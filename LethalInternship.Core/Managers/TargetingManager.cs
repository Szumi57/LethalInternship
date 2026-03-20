using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Managers
{
    public class TargetingManager : MonoBehaviour
    {
        public static TargetingManager Instance { get; private set; } = null!;

        private TargetData? currentTarget;

        private RaycastHit[] buffer = new RaycastHit[16];
        private float maxDistanceRay = 300f;

        private float angleWeight = 1.0f;
        private float distanceWeight = 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance.gameObject);
            }

            Instance = this;
        }

        void Update()
        {
            if (StartOfRound.Instance == null
                || StartOfRound.Instance.localPlayerController == null)
            {
                return;
            }

            if (Time.frameCount % 2 != 0) return;

            UpdateTarget();
        }

        public TargetData? GetCurrentTarget()
        {
            return currentTarget;
        }

        public void UpdateTarget()
        {
            // Check for direct cast
            TargetData? directTarget = FindByRaycast();
            if (directTarget != null
                && directTarget.Value.IsTargetNotPointOfInterest())
            {
                // Pointed something directly
                PluginLoggerHook.LogDebug?.Invoke($"directTarget !! target intern ? {directTarget.Value.Intern?.Npc.playerUsername} target enemy ? {directTarget.Value.Enemy?.enemyType.enemyName}");

                currentTarget = directTarget;
                return;
            }
            else
            {
                //PluginLoggerHook.LogDebug?.Invoke($"directTarget null/empty");
            }

            // Check if already scanned something not too far (latching)
            if (currentTarget != null
                && currentTarget.Value.IsTargetNotEmpty()
                && IsPointedTargetStillValid(currentTarget.Value))
            {
                return;
            }

            // Else scan angle for something
            // -----------------------------

            // Scan for interns
            TargetData? internTarget = FindPointedIntern();
            if (internTarget != null)
            {
                PluginLoggerHook.LogDebug?.Invoke($"++ internTarget {internTarget.Value.Intern?.Npc.playerUsername}");
                currentTarget = internTarget;
                return;
            }

            // Scan for enemies
            TargetData? enemyTarget = FindPointedEnemy();
            if (enemyTarget != null)
            {
                PluginLoggerHook.LogDebug?.Invoke($"++ enemyTarget {enemyTarget.Value.Enemy?.enemyType.enemyName}");
                currentTarget = enemyTarget;
                return;
            }

            // Scan for items
            TargetData? enemyItem = FindPointedItem();
            if (enemyItem != null)
            {
                PluginLoggerHook.LogDebug?.Invoke($"++ Item {enemyItem.Value.Item?.itemProperties.itemName}");
                currentTarget = enemyItem;
                return;
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
            int count = Physics.RaycastNonAlloc(ray, buffer, maxDistanceRay);

            //for (int i = count - 1; i >= 0; i--)
            //{
            //    PluginLoggerHook.LogDebug?.Invoke($"?? {buffer[i].collider.gameObject.name} {buffer[i].collider.transform.parent?.name} {buffer[i].collider.transform.parent?.parent?.name} {buffer[i].collider.transform.parent?.parent?.parent?.name} {GetColliderIntern(buffer[i].collider)?.Npc.playerUsername} {buffer[i].distance}");
            //}
            for (int i = count - 1; i >= 0; i--)
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

                //PluginLoggerHook.LogDebug?.Invoke($"--> hit {buffer[i].collider.gameObject.name} {buffer[i].collider.transform.parent?.name} {buffer[i].collider.transform.parent?.parent?.name} {buffer[i].collider.transform.parent?.parent?.parent?.name} {GetColliderIntern(buffer[i].collider)?.Npc.playerUsername}");
                return BuildTarget(buffer[i]);
            }
            return null;
        }

        private bool IsPointedTargetStillValid(TargetData target)
        {
            Camera localPlayerCamera = StartOfRound.Instance.localPlayerController.gameplayCamera;
            Transform transform = target.Root.transform;

            float distance = (StartOfRound.Instance.localPlayerController.transform.position - transform.position).sqrMagnitude;
            float angle = Vector3.Angle(localPlayerCamera.transform.forward, (transform.position + new Vector3(0f, 1f, 0f)) - localPlayerCamera.transform.position);
            float allowedAngle = GetAllowedAngle(distance) + 2f; // anti flickering margin

            return angle <= allowedAngle
                && HasPlayerLineOfSightOn(target);
        }

        private TargetData? FindPointedIntern()
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

                float distance = internAI.NpcController.GetSqrDistanceWithLocalPlayer(internAI.Npc.transform.position);
                float angle = internAI.GetAngleFOVWithLocalPlayer(localPlayerCamera.transform, internAI.Npc.transform.position
                                                                                               + new Vector3(0f, 2f * PluginRuntimeProvider.Context.Config.InternSizeScale * 0.80f, 0f));
                float allowedAngle = GetAllowedAngle(distance);

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

        private TargetData? FindPointedEnemy()
        {
            EnemyAI? bestPointedEnemy = null;
            float bestScore = float.MaxValue;

            Camera localPlayerCamera = StartOfRound.Instance.localPlayerController.gameplayCamera;
            List<EnemyAI> enemies = InternManager.Instance.GetEnemiesList();
            foreach (EnemyAI enemy in enemies)
            {
                if (enemy.isEnemyDead)
                {
                    continue;
                }
                if (StartOfRound.Instance.localPlayerController.isInsideFactory == enemy.isOutside)
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

        private TargetData? FindPointedItem()
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

            float maxAngleClose = 20f; // degrees when very close
            float maxAngleFar = 4f;  // degrees when far

            float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
            return Mathf.Lerp(maxAngleClose, maxAngleFar, t);
        }

        private TargetData BuildTarget(RaycastHit hit)
        {
            return BuildTarget(hit.collider, hit.point, 0f);
        }

        private TargetData BuildTarget(Collider col, Vector3? hitPoint, float angle)
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

            return new TargetData
            {
                Root = col.gameObject,

                // Special target
                Intern = GetColliderIntern(col),
                Enemy = col.gameObject.GetComponentInParent<EnemyAI>(),
                Item = col.gameObject.GetComponentInParent<GrabbableObject>(),
                // PointOfInterest target
                PointOfInterest = pointOfInterest,

                Score = angle
            };
        }

        private IInternAI? GetColliderIntern(Collider col)
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
