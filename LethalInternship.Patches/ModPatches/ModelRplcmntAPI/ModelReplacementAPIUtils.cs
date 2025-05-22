using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.Hooks.ModelReplacementAPIHooks;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using ModelReplacement;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    public class ModelReplacementAPIUtils
    {
        public static void Init()
        {
            ModelReplacementAPIHook.RemoveInternModelReplacement = RemoveInternModelReplacement;
            ModelReplacementAPIHook.GetBillBoardPositionModelReplacementAPI = GetBillBoardPositionModelReplacementAPI;
            ModelReplacementAPIHook.HideShowModelReplacement = HideShowModelReplacement;
            ModelReplacementAPIHook.HideShowReplacementModelOnlyBody = HideShowReplacementModelOnlyBody;
            ModelReplacementAPIHook.RemovePlayerModelReplacement = RemovePlayerModelReplacement;
            ModelReplacementAPIHook.RemovePlayerModelReplacementFromController = RemovePlayerModelReplacementFromController;
            ModelReplacementAPIHook.HideShowRagdollWithModelReplacement = HideShowRagdollWithModelReplacement;
            ModelReplacementAPIHook.HasComponentModelReplacementAPI = HasComponentModelReplacementAPI;
            ModelReplacementAPIHook.CleanListBodyReplacementOnDeadBodies = CleanListBodyReplacementOnDeadBodies;
        }

        public static void RemoveInternModelReplacement(PlayerControllerB player, bool forceRemove = false)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)player.playerClientId);
            if (internAI == null)
            {
                return;
            }

            RemoveInternModelReplacement(internAI, forceRemove);
        }

        public static void RemoveInternModelReplacement(IInternAI internAI, bool forceRemove = false)
        {
            BodyReplacementBase[] bodiesReplacementBase = internAI.ListModelReplacement.Select(x => (BodyReplacementBase)x).ToArray();
            //PluginLoggerHook.LogDebug?.Invoke($"RemovePlayerModelReplacement bodiesReplacementBase.Length {bodiesReplacementBase.Length}");
            foreach (BodyReplacementBase bodyReplacementBase in bodiesReplacementBase)
            {
                if (!forceRemove && BodyReplacementBasePatch.ListBodyReplacementOnDeadBodies.Contains(bodyReplacementBase))
                {
                    continue;
                }

                internAI.ListModelReplacement.Remove(bodyReplacementBase);
                bodyReplacementBase.IsActive = false;
                UnityEngine.Object.Destroy(bodyReplacementBase);
            }
        }

        public static Vector3? GetBillBoardPositionModelReplacementAPI(IInternAI internAI)
        {
            BodyReplacementBase? bodyReplacement = internAI.Npc.gameObject.GetComponent<BodyReplacementBase>();
            if (bodyReplacement == null)
            {
                return null;
            }

            GameObject? model = bodyReplacement.replacementModel;
            if (model == null)
            {
                return null;
            }

            return internAI.GetBillBoardPosition(model);
        }

        public static void HideShowModelReplacement(PlayerControllerB body, bool show)
        {
            body.gameObject
                .GetComponent<BodyReplacementBase>()?
                .SetAvatarRenderers(show);
        }

        public static void HideShowReplacementModelOnlyBody(PlayerControllerB body, IInternAI internAI, bool show)
        {
            body.thisPlayerModel.enabled = show;
            body.thisPlayerModelLOD1.enabled = show;
            body.thisPlayerModelLOD2.enabled = show;

            int layer = show ? 0 : 31;
            body.thisPlayerModel.gameObject.layer = layer;
            body.thisPlayerModelLOD1.gameObject.layer = layer;
            body.thisPlayerModelLOD2.gameObject.layer = layer;
            body.thisPlayerModelArms.gameObject.layer = layer;

            BodyReplacementBase? bodyReplacement = body.gameObject.GetComponent<BodyReplacementBase>();
            if (bodyReplacement == null)
            {
                internAI.HideShowLevelStickerBetaBadge(show);
                return;
            }

            GameObject? model = bodyReplacement.replacementModel;
            if (model == null)
            {
                return;
            }

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = show;
            }
        }

        public static void RemovePlayerModelReplacementFromController(PlayerControllerB internController)
        {
            RemovePlayerModelReplacement(internController.GetComponent<BodyReplacementBase>());
        }

        public static void RemovePlayerModelReplacement(object bodyReplacementBase)
        {
            Object.DestroyImmediate((BodyReplacementBase)bodyReplacementBase);
        }

        public static void HideShowRagdollWithModelReplacement(GameObject internObject, bool show)
        {
            BodyReplacementBase? bodyReplacement = internObject.GetComponent<BodyReplacementBase>();
            if (bodyReplacement == null)
            {
                InternManagerProvider.Instance.HideShowInternControllerModel(internObject, show);
                return;
            }

            GameObject? model = bodyReplacement.replacementDeadBody;
            if (model == null)
            {
                InternManagerProvider.Instance.HideShowInternControllerModel(internObject, show);
                return;
            }

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = show;
            }
        }

        public static bool HasComponentModelReplacementAPI(GameObject gameObject)
        {
            return gameObject.GetComponent<BodyReplacementBase>();
        }

        public static void CleanListBodyReplacementOnDeadBodies()
        {
            for (int i = 0; i < BodyReplacementBasePatch.ListBodyReplacementOnDeadBodies.Count; i++)
            {
                var bodyReplacementBase = BodyReplacementBasePatch.ListBodyReplacementOnDeadBodies[i];
                if (bodyReplacementBase == null
                    || bodyReplacementBase.deadBody == null)
                {
                    continue;
                }

                if (!StartOfRound.Instance.shipBounds.bounds.Contains(bodyReplacementBase.deadBody.transform.position))
                {
                    bodyReplacementBase.IsActive = false;
                    UnityEngine.Object.Destroy(bodyReplacementBase);
                    BodyReplacementBasePatch.ListBodyReplacementOnDeadBodies[i] = null!;
                }
            }
            BodyReplacementBasePatch.ListBodyReplacementOnDeadBodies = BodyReplacementBasePatch.ListBodyReplacementOnDeadBodies.Where(x => x != null
                                                                                                                                        && x.deadBody != null).ToList();
        }
    }
}
