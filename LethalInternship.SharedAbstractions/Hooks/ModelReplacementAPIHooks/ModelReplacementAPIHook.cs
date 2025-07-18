using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Hooks.ModelReplacementAPIHooks
{
    public delegate void RemoveInternModelReplacementDelegate(IInternAI internAI, bool forceRemove);
    public delegate Vector3? GetBillBoardPositionModelReplacementAPIDelegate(IInternAI internAI);
    public delegate void HideShowModelReplacementDelegate(PlayerControllerB body, bool show);
    public delegate void HideShowReplacementModelOnlyBodyDelegate(PlayerControllerB body, IInternAI internAI, bool show);
    public delegate void RemovePlayerModelReplacementDelegate(object bodyReplacementBase);
    public delegate void RemovePlayerModelReplacementFromControllerDelegate(PlayerControllerB internController);
    public delegate void HideShowRagdollWithModelReplacementDelegate(GameObject internObject, bool show);
    public delegate bool HasComponentModelReplacementAPIDelegate(GameObject gameObject);
    public delegate void CleanListBodyReplacementOnDeadBodiesDelegate();

    public static class ModelReplacementAPIHook
    {
        public static RemoveInternModelReplacementDelegate? RemoveInternModelReplacement;
        public static GetBillBoardPositionModelReplacementAPIDelegate? GetBillBoardPositionModelReplacementAPI;
        public static HideShowModelReplacementDelegate? HideShowModelReplacement;
        public static HideShowReplacementModelOnlyBodyDelegate? HideShowReplacementModelOnlyBody;
        public static RemovePlayerModelReplacementDelegate? RemovePlayerModelReplacement;
        public static RemovePlayerModelReplacementFromControllerDelegate? RemovePlayerModelReplacementFromController;
        public static HideShowRagdollWithModelReplacementDelegate? HideShowRagdollWithModelReplacement;
        public static HasComponentModelReplacementAPIDelegate? HasComponentModelReplacementAPI;
        public static CleanListBodyReplacementOnDeadBodiesDelegate? CleanListBodyReplacementOnDeadBodies;
    }
}
