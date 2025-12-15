using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VRInteractionRecording
{
    /// <summary>
    /// Allows dragging a resize handle to resize a UI panel vertically
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class PanelResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Resize Target")]
        [SerializeField]
        [Tooltip("The panel to resize (if null, will find parent panel)")]
        private RectTransform targetPanel;

        [Header("Resize Constraints")]
        [SerializeField]
        [Tooltip("Minimum height of the panel")]
        private float minHeight = 100f;

        [SerializeField]
        [Tooltip("Maximum height of the panel")]
        private float maxHeight = 800f;

        [SerializeField]
        [Tooltip("Anchor point for resizing (0 = bottom, 1 = top)")]
        private float anchorY = 0f; // 0 = resize from bottom, 1 = resize from top

        private RectTransform rectTransform;
        private Vector2 lastMousePosition;
        private Canvas canvas;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            
            // Find canvas
            canvas = GetComponentInParent<Canvas>();
            
            // Find target panel if not assigned
            if (targetPanel == null)
            {
                // Try to find parent panel
                Transform parent = transform.parent;
                while (parent != null)
                {
                    RectTransform parentRect = parent.GetComponent<RectTransform>();
                    if (parentRect != null && parentRect != rectTransform)
                    {
                        targetPanel = parentRect;
                        break;
                    }
                    parent = parent.parent;
                }
            }

            // Set up the handle to be a horizontal bar at the bottom
            if (rectTransform != null)
            {
                // Make it a thin horizontal bar
                rectTransform.anchorMin = new Vector2(0f, 0f);
                rectTransform.anchorMax = new Vector2(1f, 0f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(0f, 8f); // 8 pixels tall
                rectTransform.anchoredPosition = new Vector2(0f, 4f); // Position at bottom
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (targetPanel == null) return;
            
            lastMousePosition = GetMousePositionInCanvas(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (targetPanel == null) return;

            Vector2 currentMousePosition = GetMousePositionInCanvas(eventData);
            float deltaY = currentMousePosition.y - lastMousePosition.y;

            // Resize the panel
            float currentHeight = targetPanel.sizeDelta.y;
            float newHeight = currentHeight + deltaY;
            
            // Clamp to min/max
            newHeight = Mathf.Clamp(newHeight, minHeight, maxHeight);

            // Apply new height
            targetPanel.sizeDelta = new Vector2(targetPanel.sizeDelta.x, newHeight);

            // Adjust position if resizing from bottom
            if (anchorY < 0.5f)
            {
                // Resize from bottom - adjust position to keep bottom edge in place
                float heightDelta = newHeight - currentHeight;
                targetPanel.anchoredPosition = new Vector2(
                    targetPanel.anchoredPosition.x,
                    targetPanel.anchoredPosition.y + heightDelta * 0.5f
                );
            }

            lastMousePosition = currentMousePosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Nothing special needed on end drag
        }

        private Vector2 GetMousePositionInCanvas(PointerEventData eventData)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out localPoint
            );
            return localPoint;
        }

        /// <summary>
        /// Visual feedback - change color on hover (optional)
        /// </summary>
        private void OnEnable()
        {
            // Add a simple visual indicator
            Image image = GetComponent<Image>();
            if (image == null)
            {
                image = gameObject.AddComponent<Image>();
                image.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Semi-transparent gray
            }
        }
    }
}

