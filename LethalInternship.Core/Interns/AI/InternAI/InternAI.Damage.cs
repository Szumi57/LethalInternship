using GameNetcodeStuff;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        public int MaxHealth = ConfigConst.DEFAULT_INTERN_MAX_HEALTH;
        
        private float healthRegenerateTimerMax;

        #region Damage intern from client players RPC

        /// <summary>
        /// Server side, call client to sync the damage to the intern coming from a player
        /// </summary>
        /// <param name="damageAmount"></param>
        /// <param name="hitDirection"></param>
        /// <param name="playerWhoHit"></param>
        [ServerRpc(RequireOwnership = false)]
        public void DamageInternFromOtherClientServerRpc(int damageAmount, Vector3 hitDirection, int playerWhoHit)
        {
            DamageInternFromOtherClientClientRpc(damageAmount, hitDirection, playerWhoHit);
        }

        /// <summary>
        /// Client side, update and apply the damage to the intern coming from a player
        /// </summary>
        /// <param name="damageAmount"></param>
        /// <param name="hitDirection"></param>
        /// <param name="playerWhoHit"></param>
        [ClientRpc]
        private void DamageInternFromOtherClientClientRpc(int damageAmount, Vector3 hitDirection, int playerWhoHit)
        {
            DamageInternFromOtherClient(damageAmount, hitDirection, playerWhoHit);
        }

        /// <summary>
        /// Update and apply the damage to the intern coming from a player
        /// </summary>
        /// <param name="damageAmount"></param>
        /// <param name="hitDirection"></param>
        /// <param name="playerWhoHit"></param>
        private void DamageInternFromOtherClient(int damageAmount, Vector3 hitDirection, int playerWhoHit)
        {
            if (NpcController == null)
            {
                return;
            }

            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

            if (NpcController.Npc.isPlayerControlled)
            {
                CentipedeAI[] array = FindObjectsByType<CentipedeAI>(FindObjectsSortMode.None);
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].clingingToPlayer == this)
                    {
                        return;
                    }
                }
                DamageIntern(damageAmount, CauseOfDeath.Bludgeoning, 0, false, default);
            }

            NpcController.Npc.movementAudio.PlayOneShot(StartOfRound.Instance.hitPlayerSFX);
            if (NpcController.Npc.health < MaxHealthPercent(6))
            {
                NpcController.Npc.DropBlood(hitDirection, true, false);
                NpcController.Npc.bodyBloodDecals[0].SetActive(true);
                NpcController.Npc.playersManager.allPlayerScripts[playerWhoHit].AddBloodToBody();
                NpcController.Npc.playersManager.allPlayerScripts[playerWhoHit].movementAudio.PlayOneShot(StartOfRound.Instance.bloodGoreSFX);
            }
        }

        #endregion

        #region Damage intern RPC

        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null!, bool playHitSFX = false, int hitID = -1)
        {
            // The HitEnemy function works with player controller instead
            return;
        }

        /// <summary>
        /// Sync the damage taken by the intern between server and clients
        /// </summary>
        /// <remarks>
        /// Better to call <see cref="PlayerControllerB.DamagePlayer"><c>PlayerControllerB.DamagePlayer</c></see> so prefixes from other mods can activate. (ex : peepers)
        /// The base game function will be ignored because the intern playerController is not owned because not spawned
        /// </remarks>
        /// <param name="damageNumber"></param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        /// <param name="fallDamage">Coming from a long fall ?</param>
        /// <param name="force">Force applied to the intern when taking the hit</param>
        public void SyncDamageIntern(int damageNumber,
                                     CauseOfDeath causeOfDeath = CauseOfDeath.Unknown,
                                     int deathAnimation = 0,
                                     bool fallDamage = false,
                                     Vector3 force = default)
        {
            PluginLoggerHook.LogDebug?.Invoke($"SyncDamageIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{InternId} {NpcController.Npc.playerUsername}");

            if (NpcController.Npc.isPlayerDead)
            {
                return;
            }
            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

            if (IsServer)
            {
                DamageInternClientRpc(damageNumber, causeOfDeath, deathAnimation, fallDamage, force);
            }
            else
            {
                DamageInternServerRpc(damageNumber, causeOfDeath, deathAnimation, fallDamage, force);
            }
        }

        /// <summary>
        /// Server side, call clients to update and apply the damage taken by the intern
        /// </summary>
        /// <param name="damageNumber"></param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        /// <param name="fallDamage">Coming from a long fall ?</param>
        /// <param name="force">Force applied to the intern when taking the hit</param>
        [ServerRpc]
        private void DamageInternServerRpc(int damageNumber,
                                           CauseOfDeath causeOfDeath,
                                           int deathAnimation,
                                           bool fallDamage,
                                           Vector3 force)
        {
            DamageInternClientRpc(damageNumber, causeOfDeath, deathAnimation, fallDamage, force);
        }

        /// <summary>
        /// Client side, update and apply the damage taken by the intern
        /// </summary>
        /// <param name="damageNumber"></param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        /// <param name="fallDamage">Coming from a long fall ?</param>
        /// <param name="force">Force applied to the intern when taking the hit</param>
        [ClientRpc]
        private void DamageInternClientRpc(int damageNumber,
                                           CauseOfDeath causeOfDeath,
                                           int deathAnimation,
                                           bool fallDamage,
                                           Vector3 force)
        {
            DamageIntern(damageNumber, causeOfDeath, deathAnimation, fallDamage, force);
        }

        /// <summary>
        /// Apply the damage to the intern, kill him if needed, or make critically injured
        /// </summary>
        /// <param name="damageNumber"></param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        /// <param name="fallDamage">Coming from a long fall ?</param>
        /// <param name="force">Force applied to the intern when taking the hit</param>
        private void DamageIntern(int damageNumber,
                                  CauseOfDeath causeOfDeath,
                                  int deathAnimation,
                                  bool fallDamage,
                                  Vector3 force)
        {
            PluginLoggerHook.LogDebug?.Invoke(@$"DamageIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{InternId} {NpcController.Npc.playerUsername},
                            damageNumber {damageNumber}, causeOfDeath {causeOfDeath}, deathAnimation {deathAnimation}, fallDamage {fallDamage}, force {force}");
            if (NpcController.Npc.isPlayerDead)
            {
                return;
            }
            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

            // Apply damage, if not killed, set the minimum health to 5
            if (NpcController.Npc.health - damageNumber <= 0
                && !NpcController.Npc.criticallyInjured
                && damageNumber < MaxHealthPercent(50)
                && MaxHealthPercent(10) != MaxHealthPercent(20))
            {
                NpcController.Npc.health = 1;
            }
            else
            {
                NpcController.Npc.health = Mathf.Clamp(NpcController.Npc.health - damageNumber, 0, MaxHealth);
            }
            NpcController.Npc.PlayQuickSpecialAnimation(0.7f);

            // Kill intern if necessary
            if (NpcController.Npc.health <= 0)
            {
                if (IsClientOwnerOfIntern())
                {
                    // Call the server to spawn dead bodies
                    KillInternSpawnBodyServerRpc(spawnBody: true);
                }
                // Kill on this client side only, since we are already in a rpc send to all clients
                KillIntern(force, spawnBody: true, causeOfDeath, deathAnimation, positionOffset: default);
            }
            else
            {
                // Critically injured
                if ((NpcController.Npc.health < MaxHealthPercent(10) || NpcController.Npc.health == 1)
                    && !NpcController.Npc.criticallyInjured)
                {
                    // Client side only, since we are already in an rpc send to all clients
                    MakeCriticallyInjured();
                }
                else
                {
                    // Limit sprinting when close to death
                    if (damageNumber >= MaxHealthPercent(10))
                    {
                        NpcController.Npc.sprintMeter = Mathf.Clamp(NpcController.Npc.sprintMeter + damageNumber / 125f, 0f, 1f);
                    }
                }
                if (fallDamage)
                {
                    NpcController.Npc.movementAudio.PlayOneShot(StartOfRound.Instance.fallDamageSFX, 1f);
                }
                else
                {
                    NpcController.Npc.movementAudio.PlayOneShot(StartOfRound.Instance.damageSFX, 1f);
                }

                // Audio, already in client rpc method so no sync necessary
                InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
                {
                    VoiceState = EnumVoicesState.Hit,
                    CanTalkIfOtherInternTalk = true,
                    WaitForCooldown = false,
                    CutCurrentVoiceStateToTalk = true,
                    CanRepeatVoiceState = true,

                    ShouldSync = false,
                    IsInternInside = NpcController.Npc.isInsideFactory,
                    AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
                });
            }

            NpcController.Npc.takingFallDamage = false;
            if (!NpcController.Npc.inSpecialInteractAnimation)
            {
                NpcController.Npc.playerBodyAnimator.SetTrigger(Const.PLAYER_ANIMATION_TRIGGER_DAMAGE);
            }
            NpcController.Npc.specialAnimationWeight = 1f;
            NpcController.Npc.PlayQuickSpecialAnimation(0.7f);
        }

        public void HealthRegen()
        {
            if (NpcController.Npc.health < MaxHealthPercent(20)
                || NpcController.Npc.health == 1)
            {
                if (NpcController.Npc.healthRegenerateTimer <= 0f)
                {
                    NpcController.Npc.healthRegenerateTimer = healthRegenerateTimerMax;
                    NpcController.Npc.health = NpcController.Npc.health + 1 > MaxHealth ? MaxHealth : NpcController.Npc.health + 1;
                    if (NpcController.Npc.criticallyInjured &&
                        (NpcController.Npc.health >= MaxHealthPercent(20) || MaxHealth == 1))
                    {
                        Heal();
                    }
                }
                else
                {
                    NpcController.Npc.healthRegenerateTimer -= Time.deltaTime;
                }
            }
        }

        /// <summary>
        /// Update the state of critically injured
        /// </summary>
        private void MakeCriticallyInjured()
        {
            NpcController.Npc.bleedingHeavily = true;
            NpcController.Npc.criticallyInjured = true;
            NpcController.Npc.hasBeenCriticallyInjured = true;
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_LIMP, true);
        }

        /// <summary>
        /// Heal the intern
        /// </summary>
        private void Heal()
        {
            NpcController.Npc.bleedingHeavily = false;
            NpcController.Npc.criticallyInjured = false;
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_LIMP, false);
        }

        #endregion

        private int MaxHealthPercent(int percentage)
        {
            return InternManager.Instance.MaxHealthPercent(percentage, MaxHealth);
        }
    }
}
