using GameNetcodeStuff;
using LethalInternship.Core.Interns;
using LethalInternship.Core.Interns.AI.TimedTasks;
using LethalInternship.SharedAbstractions.Hooks.ModelReplacementAPIHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        public TimedOrderedInternBodiesDistanceListCheck OrderedInternDistanceListTimedCheck = null!;
        public List<IInternCullingBodyInfo> InternBodiesSpawned = null!;
        public IInternCullingBodyInfo[] OrderedInternBodiesInFOV = new IInternCullingBodyInfo[PluginRuntimeProvider.Context.Config.MaxInternsAvailable * 2];

        private List<int> heldInternsLocalPlayer = new List<int>();
        private float timerAnimationCulling;
        private float timerNoAnimationAfterLag;

        #region Animations culling

        private void CheckAnimationsCulling()
        {
            timerAnimationCulling += Time.deltaTime;
            if (timerAnimationCulling > 0.01f)
            {
                timerAnimationCulling = 0;
                UpdateAnimationsCulling();
            }
        }

        private void UpdateAnimationsCulling()
        {
            if (StartOfRound.Instance == null
                || StartOfRound.Instance.localPlayerController == null)
            {
                return;
            }

            if (timerNoAnimationAfterLag > 0f)
            {
                timerNoAnimationAfterLag += Time.deltaTime;
                if (timerNoAnimationAfterLag > 3f)
                {
                    timerNoAnimationAfterLag = 0f;
                }
                return;
            }

            if (timerNoAnimationAfterLag > 0f)
            {
                // No animation allowed
                List<IInternCullingBodyInfo> orderedInternBodiesDistanceListToDisable = OrderedInternDistanceListTimedCheck.GetOrderedInternDistanceList(InternBodiesSpawned);
                foreach (IInternCullingBodyInfo internCullingBodyInfo in orderedInternBodiesDistanceListToDisable)
                {
                    // Cut animation
                    internCullingBodyInfo.ResetBodyInfos();
                }
                return;
            }

            // Stop animation if we are losing frames
            if (timerNoAnimationAfterLag <= 0f && Time.deltaTime > 0.125f)
            {
                timerNoAnimationAfterLag += Time.deltaTime;
                return;
            }

            Array.Fill(OrderedInternBodiesInFOV, null);

            int indexAnyModel = 0;
            int indexNoModelReplacement = 0;
            int indexWithModelReplacement = 0;

            int indexAnyModelInFOV = 0;
            int indexNoModelReplacementInFOV = 0;
            int indexWithModelReplacementInFOV = 0;

            List<IInternCullingBodyInfo> orderedInternBodiesDistanceList = OrderedInternDistanceListTimedCheck.GetOrderedInternDistanceList(InternBodiesSpawned);
            foreach (IInternCullingBodyInfo internCullingBodyInfo in orderedInternBodiesDistanceList)
            {
                // Cut animation before deciding which intern can animate
                internCullingBodyInfo.ResetBodyInfos();

                internCullingBodyInfo.RankDistanceAnyModel = indexAnyModel++;
                if (internCullingBodyInfo.CheckIsInFOV())
                {
                    if (internCullingBodyInfo.HasModelReplacement)
                    {
                        internCullingBodyInfo.RankDistanceWithModelReplacementInFOV = indexWithModelReplacementInFOV++;
                    }
                    else
                    {
                        internCullingBodyInfo.RankDistanceNoModelReplacementInFOV = indexNoModelReplacementInFOV++;
                    }

                    internCullingBodyInfo.RankDistanceAnyModelInFOV = indexAnyModelInFOV;
                    internCullingBodyInfo.BodyInFOV = true;
                    OrderedInternBodiesInFOV[indexAnyModelInFOV] = internCullingBodyInfo;
                    indexAnyModelInFOV++;
                }

                // In or not in FOV
                if (internCullingBodyInfo.HasModelReplacement)
                {
                    internCullingBodyInfo.RankDistanceWithModelReplacement = indexWithModelReplacement++;
                }
                else
                {
                    internCullingBodyInfo.RankDistanceNoModelReplacement = indexNoModelReplacement++;
                }
            }

            timerAnimationCulling = 0f;
        }

        public void RegisterInternBodyForAnimationCulling(Component internBody, bool hasModelReplacement = false)
        {
            // Clean
            InternBodiesSpawned.RemoveAll(x => x.InternBody == null);

            // Register or re-init
            IInternCullingBodyInfo? internCullingBodyInfo = InternBodiesSpawned.FirstOrDefault(x => x.InternBody == internBody);
            if (internCullingBodyInfo == null)
            {
                InternBodiesSpawned.Add(new InternCullingBodyInfo(internBody, hasModelReplacement));
            }
            else
            {
                internCullingBodyInfo.Init(hasModelReplacement);
            }

            // Resizing, bodies info contains player controllers and ragdoll corpse
            if (InternBodiesSpawned.Count > OrderedInternBodiesInFOV.Length)
            {
                Array.Resize(ref OrderedInternBodiesInFOV, InternBodiesSpawned.Count);
            }
        }

        public IInternCullingBodyInfo? GetInternCullingBodyInfo(GameObject gameObject)
        {
            foreach (IInternCullingBodyInfo internCullingBodyInfo in InternBodiesSpawned)
            {
                if (internCullingBodyInfo == null
                    || internCullingBodyInfo.InternBody == null)
                {
                    continue;
                }

                if (internCullingBodyInfo.InternBody.gameObject == gameObject)
                {
                    return internCullingBodyInfo;
                }
            }

            return null;
        }

        public void RegisterHeldInternForLocalPlayer(int idInternController)
        {
            if (heldInternsLocalPlayer == null)
            {
                heldInternsLocalPlayer = new List<int>();
            }

            heldInternsLocalPlayer.Add(idInternController);
        }

        public void UnregisterHeldInternForLocalPlayer(int idInternController)
        {
            if (HeldInternsLocalPlayer == null)
            {
                return;
            }

            HeldInternsLocalPlayer.Remove(idInternController);
        }

        public void HideShowRagdollModel(PlayerControllerB internController, bool show)
        {
            if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
            {
                ModelReplacementAPIHook.HideShowRagdollWithModelReplacement?.Invoke(internController.gameObject, show);
            }
            else
            {
                HideShowInternControllerModel(internController.gameObject, show);
            }
        }

        public void HideShowInternControllerModel(GameObject internObject, bool show)
        {
            SkinnedMeshRenderer[] componentsInChildren = internObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].enabled = show;
            }
        }

        #endregion
    }
}
