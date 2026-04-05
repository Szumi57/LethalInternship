using LethalInternship.Core.UI.TooltipBar;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.ItemBlocks
{
    public class ItemBlockUI : MonoBehaviour
    {
        private static int _uidCounter;

        public static System.Action<string> OnSelected = null!;

        public Transform Content = null!;
        public Image FrameImage = null!;

        private GameObject itemHologram = null!;
        private Quaternion restingRotation = Quaternion.identity;
        private int itemValue = 0;

        public int RuntimeUID { get; private set; }
        public bool IsUsed { get; private set; }
        public string ItemName { get; private set; } = string.Empty;

        private float holdTime = 0.5f;

        // Rotation animation
        private enum RotationAxis { X, Y, Z }
        private RotationAxis mainAxis = RotationAxis.X;
        private float duration = 1.5f;
        private float rotationSpeed = 360f; // degrees per second
        private float tiltAngle = 10f;
        private Coroutine rotationRoutine = null!;

        public void AssignRuntimeUID() => RuntimeUID = ++_uidCounter;
        public void UpdateInfos(ItemUIInfos itemInfos) { ItemName = itemInfos.ItemName; itemValue = itemInfos.ItemValue; }
        public void MarkUsed() => IsUsed = true;
        public void MarkUnused() => IsUsed = false;

        void OnEnable()
        {
            if (itemHologram != null)
                PlayAnimationRotation();
        }

        public void Setup(ItemUIInfos itemInfos)
        {
            Debug.Log($"Setup previous:{ItemName}, now {itemInfos.ItemName}");

            Item LCItem = StartOfRound.Instance.allItemsList.itemsList.FirstOrDefault(x => x.itemName == itemInfos.ItemName);
            if (LCItem == null)
            {
                PluginLoggerHook.LogError?.Invoke($"Cannot create block UI for item named : {itemInfos.ItemName}, item not found in StartOfRound.Instance.allItemsList.itemsList !");
                return;
            }

            this.ItemName = itemInfos.ItemName;
            this.itemValue = itemInfos.ItemValue;

            // Item Hologram
            itemHologram = Object.Instantiate<GameObject>(LCItem.spawnPrefab, Content);

            // Position in the container
            float y = (0.5f - ((RectTransform)Content).pivot.y) * ((RectTransform)Content).rect.height;
            itemHologram.transform.localPosition = new Vector3(0f, y, 0f);

            // Size
            itemHologram.transform.localScale = itemHologram.transform.localScale * 50f;

            GrabbableObject grabbableObject = itemHologram.GetComponent<GrabbableObject>();
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

            Object.Destroy(itemHologram.GetComponent<NetworkObject>());
            Object.Destroy(itemHologram.GetComponent<GrabbableObject>());
            Object.Destroy(itemHologram.GetComponent<Collider>());

            PlayAnimationRotation();
        }

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

        private void SetButtonHovered()
        {
            FrameImage.pixelsPerUnitMultiplier = 4f;
        }

        private void SetButtonNotHovered()
        {
            FrameImage.pixelsPerUnitMultiplier = 6f;
        }

        #region Events

        private void ActionValidated()
        {
            TooltipBarUI.Instance.Hide();
            OnSelected?.Invoke(this.ItemName);
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
            TooltipBarUI.Instance.ShowImmediate(string.Format(UIConst.TOOLTIPBAR_ITEM, this.ItemName, itemValue));
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
