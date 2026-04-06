using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.TooltipBar;
using LethalInternship.SharedAbstractions.Constants;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.ItemBlocks
{
    public class ItemBlockUI : MonoBehaviour
    {
        public static System.Action<GrabbableObject> OnSelected = null!;

        public Transform Content = null!;
        public Image FrameImage = null!;

        private GameObject itemHologram = null!;
        private Quaternion restingRotation = Quaternion.identity;
        private GrabbableObject itemGrabbableObject = null!;

        private RectTransform viewport = null!;
        private RectTransform itemFrameRect = null!;
        private Renderer[] hologramRenderers = null!;

        public string ItemName => itemGrabbableObject.itemProperties.itemName;
        public int ItemValue => itemGrabbableObject.scrapValue;

        private float holdTime = 0.5f;

        // Rotation animation
        private enum RotationAxis { X, Y, Z }
        private RotationAxis mainAxis = RotationAxis.X;
        private float duration = 1.5f;
        private float rotationSpeed = 360f; // degrees per second
        private float tiltAngle = 10f;
        private Coroutine rotationRoutine = null!;

        void Awake()
        {
            viewport = this.transform.parent.parent.GetComponent<RectTransform>();
            this.transform.parent.parent.parent.GetComponent<ScrollRect>().onValueChanged.AddListener(OnScroll);
        }

        void OnEnable()
        {
            if (itemHologram == null)
            {
                return;
            }

            PlayAnimationRotation();
            StartCoroutine(FitNextFrame());
            StartCoroutine(CheckVisibilityNextFrame());
        }

        private void SetButtonHovered()
        {
            FrameImage.pixelsPerUnitMultiplier = 4f;
        }

        private void SetButtonNotHovered()
        {
            FrameImage.pixelsPerUnitMultiplier = 6f;
        }

        public void Setup(GrabbableObject grabbableObject)
        {
            // Real item ref
            itemGrabbableObject = grabbableObject;

            // Item Hologram
            itemHologram = Object.Instantiate<GameObject>(grabbableObject.itemProperties.spawnPrefab, Content);
            hologramRenderers = itemHologram.GetComponentsInChildren<Renderer>(true);

            // Position in the container
            itemFrameRect = (RectTransform)Content;
            float y = (0.5f - itemFrameRect.pivot.y) * itemFrameRect.rect.height;
            itemHologram.transform.localPosition = new Vector3(0f, y, 0f);

            restingRotation = Quaternion.Euler(grabbableObject.itemProperties.restingRotation);
            itemHologram.transform.rotation = restingRotation;

            // Rendered hologram
            Renderer[] componentsInChildren = itemHologram.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                if (componentsInChildren[i].gameObject.layer != 22)
                {
                    Material[] sharedMaterials = componentsInChildren[i].sharedMaterials;
                    componentsInChildren[i].rendererPriority = 70;
                    for (int j = 0; j < sharedMaterials.Length; j++)
                    {
                        sharedMaterials[j] = HUDManager.Instance.hologramMaterial;
                    }
                    componentsInChildren[i].sharedMaterials = sharedMaterials;
                    componentsInChildren[i].gameObject.layer = 5;
                }
            }

            // Clean gameobject just for hologram
            Object.Destroy(itemHologram.GetComponent<NetworkObject>());
            Object.Destroy(itemHologram.GetComponent<GrabbableObject>());
            Object.Destroy(itemHologram.GetComponent<Collider>());
            GameObject? scanNode = itemHologram.GetComponentsInChildren<Transform>().Where(x => x.name == "ScanNode").FirstOrDefault()?.gameObject;
            if (scanNode != null)
                Object.Destroy(scanNode);

            // Start coroutine only when active, ex : event when grabbing object triggers Setup while "not active"
            if (UIManager.Instance.IsCommandsOneOpened)
            {
                PlayAnimationRotation();
                StartCoroutine(FitNextFrame());
                StartCoroutine(CheckVisibilityNextFrame());
            }
        }

        #region Animation

        public void PlayAnimationRotation()
        {
            if (rotationRoutine != null)
                StopCoroutine(rotationRoutine);

            rotationRoutine = StartCoroutine(RotateRoutine());
        }

        IEnumerator RotateRoutine()
        {
            float elapsed = 0f;
            Quaternion startRotation = itemHologram.transform.localRotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Rotation
                float main = rotationSpeed * elapsed;

                // Animated tilt
                float tilt = Mathf.Sin(t * Mathf.PI) * tiltAngle;

                Vector3 euler = GetEuler(main, tilt);
                itemHologram.transform.localRotation = startRotation * Quaternion.Euler(euler);

                yield return null;
            }

            // Snap final propre
            itemHologram.transform.localRotation = restingRotation;
            rotationRoutine = null!;
        }

        private Vector3 GetEuler(float main, float tilt)
        {
            switch (mainAxis)
            {
                case RotationAxis.X:
                    return new Vector3(main, tilt, tilt * 0.5f);

                case RotationAxis.Z:
                    return new Vector3(tilt, tilt * 0.5f, main);

                case RotationAxis.Y:
                default:
                    return new Vector3(tilt, main, tilt * 0.5f);
            }
        }

        #endregion

        #region Scale maths

        public float GetScaleFittingToFrame(Transform root,
                                            float targetSize,
                                            float margin = 0.9f)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
                return 1f;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 size = bounds.size;

            float maxDimension = Mathf.Max(size.x, size.y, size.z);

            if (maxDimension <= 0f)
                return 1f;

            return (targetSize / maxDimension) * margin;
        }

        IEnumerator FitNextFrame()
        {
            yield return null;

            itemHologram.transform.localScale *= GetScaleFittingToFrame(itemHologram.transform, targetSize: 0.38f, margin: 0.9f);
        }

        #endregion

        #region Visibility maths

        private void UpdateVisibility()
        {
            if (viewport == null
                || itemFrameRect == null
                || hologramRenderers == null)
            {
                return;
            }

            bool visible = IsVisible(itemFrameRect, viewport);

            foreach (var r in hologramRenderers)
            {
                if (r != null)
                {
                    r.enabled = visible;
                }
            }
        }

        private bool IsVisible(RectTransform itemFrame, RectTransform viewport)
        {
            Vector3[] itemCorners = new Vector3[4];
            Vector3[] viewCorners = new Vector3[4];

            itemFrame.GetWorldCorners(itemCorners);
            viewport.GetWorldCorners(viewCorners);

            Rect itemRect = RectFromCorners(itemCorners);
            Rect viewRect = RectFromCorners(viewCorners);

            if (!itemRect.Overlaps(viewRect))
                return false;

            float visibleArea = IntersectionArea(itemRect, viewRect);
            float totalArea = itemRect.width * itemRect.height;

            if (totalArea <= 0f)
                return false;

            float ratio = visibleArea / totalArea;

            return ratio >= (2f / 3f);
        }

        IEnumerator CheckVisibilityNextFrame()
        {
            yield return null;
            UpdateVisibility();
        }

        private Rect RectFromCorners(Vector3[] corners)
        {
            Vector3 bottomLeft = corners[0];
            Vector3 topRight = corners[2];
            return new Rect(bottomLeft, topRight - bottomLeft);
        }

        private float IntersectionArea(Rect a, Rect b)
        {
            float xMin = Mathf.Max(a.xMin, b.xMin);
            float xMax = Mathf.Min(a.xMax, b.xMax);
            float yMin = Mathf.Max(a.yMin, b.yMin);
            float yMax = Mathf.Min(a.yMax, b.yMax);

            if (xMax <= xMin || yMax <= yMin)
                return 0f;

            return (xMax - xMin) * (yMax - yMin);
        }

        #endregion

        #region Events

        private void OnScroll(Vector2 v)
        {
            UpdateVisibility();
        }

        private void ActionValidated()
        {
            TooltipBarUI.Instance.Hide();
            OnSelected?.Invoke(itemGrabbableObject);
        }

        public void PointerDown()
        {
            TooltipBarUI.Instance.StartHold(holdTime, ActionValidated);
        }

        public void PointerUp()
        {
            TooltipBarUI.Instance.StopHold();
            SetButtonNotHovered();
        }

        public void MouseOver()
        {
            TooltipBarUI.Instance.ShowImmediate(string.Format(UIConst.TOOLTIPBAR_ITEM, ItemName, ItemValue));
            SetButtonHovered();
        }

        public void MouseLeave()
        {
            TooltipBarUI.Instance.Hide();
            SetButtonNotHovered();
        }

        #endregion
    }
}
