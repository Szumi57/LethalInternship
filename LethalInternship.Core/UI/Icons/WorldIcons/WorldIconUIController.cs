using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace LethalInternship.Core.UI.Icons.WorldIcons
{
    [ExecuteInEditMode]
    public class WorldIconUIController : MonoBehaviour
    {
        private RectTransform rectTransformIcon = null!;
        private Animator animator = null!;

        private bool isIconInCenter;
        public bool IsIconInCenter { get => isIconInCenter; }

        private List<GameObject> ImagesTopPrefab = null!;
        private List<Image> ImagesTop = null!;

        private GameObject TopRow = null!;
        private Image ImageBottom = null!;

        private bool pingAnimationNextUpdate = false;

        // Start is called before the first frame update
        void Start()
        {
            rectTransformIcon = GetComponent<RectTransform>();
            animator = GetComponent<Animator>();

            ImageBottom = GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "PointerIconImage");
            TopRow = GetComponentsInChildren<Component>().FirstOrDefault(x => x.name == "Top").gameObject;
            UpdateImagesOnTop();
        }

        // Update is called once per frame
        private void Update()
        {
            if (pingAnimationNextUpdate)
            {
                TriggerPingAnimation();
                pingAnimationNextUpdate = false;
            }

            //if (rectTransformIcon == null)
            //{
            //    return;
            //}

            //if (rectTransformIcon.sizeDelta.x < MinWidth)
            //{
            //    rectTransformIcon.sizeDelta = new Vector2(MinWidth, rectTransformIcon.sizeDelta.y);
            //}
            //if (rectTransformIcon.sizeDelta.y < MinHeight)
            //{
            //    rectTransformIcon.sizeDelta = new Vector2(rectTransformIcon.sizeDelta.x, MinHeight);
            //}

            //rectTransformIcon.sizeDelta = new Vector2(MinWidth / MinHeight * rectTransformIcon.sizeDelta.y, rectTransformIcon.sizeDelta.y);
        }

        public void SetImagesOnTop(List<GameObject> images)
        {
            ImagesTopPrefab = images;
        }

        private void UpdateImagesOnTop()
        {
            if (ImagesTopPrefab == null
                || ImagesTopPrefab.Count == 0)
            {
                if (PluginRuntimeProvider.Context.DefaultIconImagePrefab != null) // Unity editor
                {
                    ImagesTop = new List<Image>() { Object.Instantiate(PluginRuntimeProvider.Context.DefaultIconImagePrefab).GetComponent<Image>() };
                    ImagesTop.First().transform.SetParent(TopRow.transform);
                }
            }
            else
            {
                foreach (var imageToDelete in TopRow.GetComponentsInChildren<Image>())
                {
                    Object.Destroy(imageToDelete.gameObject);
                }
                ImagesTop = new List<Image>();

                // Add images
                foreach (var image in ImagesTopPrefab)
                {
                    GameObject imageInstantiated = Object.Instantiate(image);
                    ImagesTop.Add(imageInstantiated.GetComponent<Image>());
                    imageInstantiated.transform.SetParent(TopRow.transform);
                }
            }
        }

        public void PlaceOnCanvas(Vector3 screenPos, RectTransform rectTransformCanvasParent)
        {
            if (rectTransformIcon == null)
            {
                return;
            }

            // Size
            if (screenPos.z != 0f)
            {
                float size = 1f / screenPos.z * 400f;
                //PluginLoggerHook.LogDebug?.Invoke($"size {size}, dist {screenPos.z}");
                if (size < 10f) { size = 10f; }
                if (size > 200f) { size = 200f; }
                if (screenPos.z < 5f)
                {
                    SetTransparency(screenPos.z / 5f * 0.5f);
                }
                else
                {
                    SetTransparency(1f);
                }

                // Size with distance
                rectTransformIcon.sizeDelta = new Vector2(size, size);
            }

            // Limit the image to screen borders
            if (screenPos.x - rectTransformIcon.sizeDelta.x * 0.5f < rectTransformCanvasParent.sizeDelta.x * -0.5f)
            {
                screenPos.x = rectTransformCanvasParent.sizeDelta.x * -0.5f + rectTransformIcon.sizeDelta.x * 0.5f;
            }
            if (screenPos.x + rectTransformIcon.sizeDelta.x * 0.5f > rectTransformCanvasParent.sizeDelta.x * 0.5f)
            {
                screenPos.x = rectTransformCanvasParent.sizeDelta.x * 0.5f - rectTransformIcon.sizeDelta.x * 0.5f;
            }

            if (screenPos.y < rectTransformCanvasParent.sizeDelta.y * -0.5f)
            {
                screenPos.y = rectTransformCanvasParent.sizeDelta.y * -0.5f;
            }
            if (screenPos.y + rectTransformIcon.sizeDelta.y > rectTransformCanvasParent.sizeDelta.y * 0.5f)
            {
                screenPos.y = rectTransformCanvasParent.sizeDelta.y * 0.5f - rectTransformIcon.sizeDelta.y;
            }
            // Position
            rectTransformIcon.localPosition = new Vector3(screenPos.x, screenPos.y + rectTransformIcon.sizeDelta.y * 0.5f, 0f);

            // Is icon in center
            float xLeft = rectTransformIcon.localPosition.x - rectTransformIcon.sizeDelta.x / 2;
            float xRight = rectTransformIcon.localPosition.x + rectTransformIcon.sizeDelta.x / 2;
            float yTop = rectTransformIcon.localPosition.y + rectTransformIcon.sizeDelta.y / 2;
            float yBottom = rectTransformIcon.localPosition.y - rectTransformIcon.sizeDelta.y / 2;

            isIconInCenter = xLeft < 0f && xRight > 0f && yTop > 0f && yBottom < 0f;
            animator.SetBool("IsHovered", isIconInCenter);
        }

        private void SetTransparency(float alpha)
        {
            if (ImagesTop != null)
            {
                foreach (var imageTop in ImagesTop)
                {
                    imageTop.color = new Color(imageTop.color.r, imageTop.color.g, imageTop.color.b, alpha);
                }
            }
            if (ImageBottom != null)
            {
                ImageBottom.color = new Color(ImageBottom.color.r, ImageBottom.color.g, ImageBottom.color.b, alpha);
            }
        }

        public void SetColor(Color color)
        {
            if (ImagesTop != null)
            {
                foreach (var imageTop in ImagesTop)
                {
                    imageTop.color = new Color(color.r, color.g, color.b, imageTop.color.a);
                }
            }
            if (ImageBottom != null)
            {
                ImageBottom.color = new Color(color.r, color.g, color.b, ImageBottom.color.a);
            }
        }

        public void TriggerPingAnimation()
        {
            if (animator == null)
            {
                pingAnimationNextUpdate = true;
                return;
            }

            animator.ResetTrigger("Ping");
            animator.SetTrigger("Ping");
        }
    }
}
