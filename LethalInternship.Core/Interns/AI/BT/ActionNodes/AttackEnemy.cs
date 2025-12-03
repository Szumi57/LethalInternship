using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Items;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class AttackEnemy : IBTAction
    {
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
            //Transform enemyTarget = context.CurrentEnemy.eye == null ? context.CurrentEnemy.transform : context.CurrentEnemy.eye.transform;
            Vector3 enemyTarget = context.CurrentEnemy.transform.position + new Vector3(0f, 0.5f, 0f);
            //ai.NpcController.UpdateNowTurnBodyTowardsDirection(enemyTarget);
            ai.NpcController.OrderToLookAtMovingTarget(context.CurrentEnemy.transform);
            DrawUtil.DrawLine(ai.LineRendererUtil.GetLineRenderer(), ai.eye.position, enemyTarget, Color.red);

            // Attack
            PluginLoggerHook.LogDebug?.Invoke($"AttackEnemy weaponObject.GetType() {weaponObject.GetType()}");
            Shovel? shovel = weaponObject as Shovel;
            if (shovel != null)
            {
                weaponObject.UseItemOnClient(!shovel.reelingUp);
            }

            return BehaviourTreeStatus.Success;
        }
    }
}
