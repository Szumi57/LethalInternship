using LethalInternship.Core.Interns.AI.Items;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsAutoDefense : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.CurrentEnemy == null)
            {
                PluginLoggerHook.LogError?.Invoke("IsAutoDefense Condition, CurrentEnemy is null");
                return false;
            }

            HeldItem? weapon = ai.HeldItems.GetHeldWeaponAsHeldItem();
            if (weapon == null
                || !weapon.IsWeapon)
            {
                return false;
            }

            if (!CanKillEnemy(context.CurrentEnemy))
            {
                return false;
            }

            // Voice
            if (weapon.IsMeleeWeapon)
            {
                TryPlayAttackingStateVoiceAudio(ai, EnumVoicesState.AttackingWithMelee);
            }
            else if (weapon.IsRangedWeapon)
            {
                TryPlayAttackingStateVoiceAudio(ai, EnumVoicesState.AttackingWithGun);
            }

            return true; // todo: parametizasizasizasization
        }

        private bool CanKillEnemy(EnemyAI enemy)
        {
            switch (enemy.enemyType.enemyName) // using enemyName
            {
                // Killable
                case "Baboon hawk":
                case "Bunker Spider":
                case "Bush Wolf":
                case "Butler":
                case "Centipede":
                case "Crawler":
                case "Flowerman":
                case "ForestGiant":
                case "GiantKiwi":
                case "Hoarding bug":
                case "Maneater":
                case "Masked":
                case "Manticoil":
                case "MouthDog":
                case "Nutcracker":
                case "Tulip Snake":
                    return true;

                default:
                    // Not killable

                    // "Butler Bees":
                    // "Blob":
                    // "ImmortalSnail":
                    // "Red Locust Bees":
                    // "Earth Leviathan":
                    // "Clay Surgeon":
                    // "Puffer":
                    // "Spring":
                    // "Jester":
                    // "RadMech":
                    // "Docile Locust Bees":
                    // "Girl":
                    return false;
            }
        }

        private void TryPlayAttackingStateVoiceAudio(InternAI ai, EnumVoicesState enumVoicesState)
        {
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = enumVoicesState,
                CanTalkIfOtherInternTalk = true,
                WaitForCooldown = false,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = ai.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }
    }
}
