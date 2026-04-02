using System.Collections;
using UnityEngine;

namespace LethalInternship.Core.UI.Others
{
    public class CameraFocusUI : MonoBehaviour
    {
        public static CameraFocusUI Instance { get; private set; } = null!;

        private Camera cam = null!;

        private float focusSpeed = 4f;
        private float returnSpeed = 4f;

        CameraState initialState;
        Coroutine currentRoutine = null!;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnEnable()
        {
            if (StartOfRound.Instance != null
                && StartOfRound.Instance.localPlayerController != null)
            {
                cam = StartOfRound.Instance.localPlayerController.gameplayCamera;
            }
        }

        public void FocusOnIntern(Transform target)
        {
            if (currentRoutine != null)
                StopCoroutine(currentRoutine);

            initialState.Capture(cam);
            currentRoutine = StartCoroutine(FocusRotationOnly(target));
        }

        public void ReturnToInitial()
        {
            initialState.Apply(cam);
        }

        IEnumerator FocusRotationOnly(Transform target)
        {
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
