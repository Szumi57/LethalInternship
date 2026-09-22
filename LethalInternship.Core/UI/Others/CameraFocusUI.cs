using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Collections;
using UnityEngine;

namespace LethalInternship.Core.UI.Others
{
    public class CameraFocusUI
    {
        private static CameraFocusUI _instance = null!;
        public static CameraFocusUI Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CameraFocusUI();

                return _instance;
            }
        }
        private CameraFocusUI() { }

        private Camera cam = null!;

        private float focusSpeed = 4f;
        //private float returnSpeed = 4f;

        CameraState initialState;
        Coroutine currentRoutine = null!;

        private bool UpdateCam()
        {
            if (StartOfRound.Instance == null
                || StartOfRound.Instance.localPlayerController == null)
            {
                PluginLoggerHook.LogWarning?.Invoke("CameraFocusUI : no local Player available !");
                return false;
            }

            cam = StartOfRound.Instance.localPlayerController.gameplayCamera;
            if (cam == null)
            {
                PluginLoggerHook.LogWarning?.Invoke("CameraFocusUI : no local Player gameplayCamera available !");
                return false;
            }

            return true;
        }

        public void FocusOnIntern(Transform target)
        {
            if (!UpdateCam())
                return;

            if (currentRoutine != null)
                UIManager.Instance.StopCoroutine(currentRoutine);

            initialState.Capture(cam);
            currentRoutine = UIManager.Instance.StartCoroutine(FocusRotationOnly(target));
        }

        public void ReturnToInitial()
        {
            if (!UpdateCam())
                return;

            if (currentRoutine != null)
            {
                UIManager.Instance.StopCoroutine(currentRoutine);
                currentRoutine = null!;
            }

            initialState.Apply(cam);
        }

        IEnumerator FocusRotationOnly(Transform target)
        {
            if (!UpdateCam())
                yield break;

            Vector3 targetPos = target.position + new Vector3(0f, 1.5f, 0f);

            Vector3 camWorldPos = cam.transform.position;
            Quaternion targetWorldRot = Quaternion.LookRotation(targetPos - camWorldPos);

            Quaternion targetLocalRot = Quaternion.Inverse(cam.transform.parent.rotation) * targetWorldRot;

            Quaternion startLocalRot = cam.transform.localRotation;
            float startFov = cam.fieldOfView;
            float targetFov = Mathf.Clamp(startFov - 15f, 25f, 60f);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * focusSpeed;

                cam.transform.localRotation =
                    Quaternion.Slerp(startLocalRot, targetLocalRot, t);

                cam.fieldOfView =
                    Mathf.Lerp(startFov, targetFov, t);

                yield return null;
            }
        }

        struct CameraState
        {
            public float fov;

            public void Capture(Camera cam)
            {
                fov = cam.fieldOfView;
            }

            public void Apply(Camera cam)
            {
                cam.transform.localRotation = Quaternion.identity;
                cam.fieldOfView = fov;
            }
        }
    }
}
