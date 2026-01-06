using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Items;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class AttackEnemy : IBTAction
    {
        private long? timer = null;
        private long lastTimeCalculate;

        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.CurrentEnemy == null)
            {
                PluginLoggerHook.LogError?.Invoke("AttackEnemy Action, CurrentEnemy is null");
                return BehaviourTreeStatus.Failure;
            }

            HeldItem? weapon = ai.HeldItems.GetHeldWeaponAsHeldItem();
            if (weapon == null)
            {
                PluginLoggerHook.LogWarning?.Invoke("AttackEnemy Action, holding no weapon");
                return BehaviourTreeStatus.Failure;
            }
            if (!weapon.IsWeapon)
            {
                PluginLoggerHook.LogWarning?.Invoke("AttackEnemy Action, weapon is not usable");
                return BehaviourTreeStatus.Failure;
            }

            GrabbableObject? weaponObject = ai.HeldItems.GetHeldWeapon();
            if (weaponObject == null)
            {
                PluginLoggerHook.LogError?.Invoke("AttackEnemy Action, weaponObject is null");
                return BehaviourTreeStatus.Failure;
            }

            // Turn towards
            Transform transformToLookat = context.CurrentEnemy.eye != null ? context.CurrentEnemy.eye.transform : context.CurrentEnemy.transform;
            ai.NpcController.OrderToLookAtMovingTarget(transformToLookat);

            // Shovel attack
            Shovel? shovel = weaponObject as Shovel;
            if (shovel != null)
            {
                weaponObject.UseItemOnClient(buttonDown: !shovel.reelingUp);
                return BehaviourTreeStatus.Success;
            }
            // Knife attack
            KnifeItem? knife = weaponObject as KnifeItem;
            if (knife != null)
            {
                weaponObject.UseItemOnClient(buttonDown: true);
                return BehaviourTreeStatus.Success;
            }

            // Shotgun
            ShotgunItem? shotgunItem = weaponObject as ShotgunItem;
            if (shotgunItem != null)
            {
                timer ??= (int)Random.Range(1000f, 2000f) * TimeSpan.TicksPerMillisecond;
                if (DateTime.Now.Ticks - lastTimeCalculate > timer)
                {
                    lastTimeCalculate = DateTime.Now.Ticks;
                }
                else
                {
                    timer = null;
                    lastTimeCalculate = 0;

                    shotgunItem.ShootGun(shotgunItem.shotgunRayPoint.position, shotgunItem.shotgunRayPoint.forward);
                    return BehaviourTreeStatus.Success;
                }
            }

            return BehaviourTreeStatus.Success;
        }
    }
}
