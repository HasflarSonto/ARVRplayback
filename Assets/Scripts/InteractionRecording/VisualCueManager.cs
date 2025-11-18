using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRInteractionRecording
{
    /// <summary>
    /// Manages visual cues for interaction guidance
    /// Handles object highlighting and ghost object display
    /// </summary>
    public class VisualCueManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Material used for highlighting objects")]
        private Material highlightMaterial;

        [SerializeField]
        [Tooltip("Material used for ghost objects (green transparent)")]
        private Material ghostMaterial;

        [SerializeField]
        [Tooltip("Color for highlighting (if using color-based highlighting)")]
        private Color highlightColor = new Color(1f, 1f, 0f, 1f); // Yellow

        [SerializeField]
        [Tooltip("Color for ghost objects")]
        private Color ghostColor = new Color(0f, 1f, 0f, 0.5f); // Green with transparency

        [SerializeField]
        [Tooltip("Highlight method to use")]
        private HighlightMethod highlightMethod = HighlightMethod.Outline;

        [SerializeField]
        [Tooltip("Scale factor for ghost objects (1.0 = same size)")]
        private float ghostScale = 1.0f;

        [SerializeField]
        [Tooltip("Enable pulsing animation for highlights")]
        private bool enablePulsing = true;

        [SerializeField]
        [Tooltip("Pulse speed for highlight animation")]
        private float pulseSpeed = 2f;

        private Dictionary<GameObject, HighlightData> highlightedObjects = new Dictionary<GameObject, HighlightData>();
        private Dictionary<GameObject, GameObject> ghostObjects = new Dictionary<GameObject, GameObject>();

        private void Update()
        {
            if (enablePulsing)
            {
                UpdatePulsing();
            }
        }

        /// <summary>
        /// Highlights an object to indicate it should be interacted with
        /// </summary>
        public void HighlightObject(GameObject obj)
        {
            if (obj == null) return;

            // Remove existing highlight if any
            if (highlightedObjects.ContainsKey(obj))
            {
                RemoveHighlight(obj);
            }

            HighlightData highlightData = new HighlightData();
            highlightData.originalObject = obj;

            switch (highlightMethod)
            {
                case HighlightMethod.Outline:
                    ApplyOutlineHighlight(obj, highlightData);
                    break;
                case HighlightMethod.Color:
                    ApplyColorHighlight(obj, highlightData);
                    break;
                case HighlightMethod.Material:
                    ApplyMaterialHighlight(obj, highlightData);
                    break;
            }

            highlightedObjects[obj] = highlightData;
        }

        /// <summary>
        /// Removes highlight from an object
        /// </summary>
        public void RemoveHighlight(GameObject obj)
        {
            if (obj == null || !highlightedObjects.ContainsKey(obj)) return;

            HighlightData highlightData = highlightedObjects[obj];

            switch (highlightMethod)
            {
                case HighlightMethod.Outline:
                    RemoveOutlineHighlight(obj, highlightData);
                    break;
                case HighlightMethod.Color:
                    RemoveColorHighlight(obj, highlightData);
                    break;
                case HighlightMethod.Material:
                    RemoveMaterialHighlight(obj, highlightData);
                    break;
            }

            highlightedObjects.Remove(obj);
        }

        /// <summary>
        /// Clears all highlights
        /// </summary>
        public void ClearAllHighlights()
        {
            List<GameObject> objectsToRemove = new List<GameObject>(highlightedObjects.Keys);
            foreach (GameObject obj in objectsToRemove)
            {
                RemoveHighlight(obj);
            }
        }

        /// <summary>
        /// Shows a ghost object at the target location
        /// </summary>
        public void ShowGhostObject(GameObject originalObject, Vector3 targetPosition, Quaternion targetRotation)
        {
            if (originalObject == null) return;

            // Hide existing ghost if any
            HideGhostObject(originalObject);

            // Create ghost object
            GameObject ghost = CreateGhostObject(originalObject, targetPosition, targetRotation);
            ghostObjects[originalObject] = ghost;
        }

        /// <summary>
        /// Hides the ghost object for a given original object
        /// </summary>
        public void HideGhostObject(GameObject originalObject)
        {
            if (originalObject == null || !ghostObjects.ContainsKey(originalObject)) return;

            GameObject ghost = ghostObjects[originalObject];
            if (ghost != null)
            {
                Destroy(ghost);
            }
            ghostObjects.Remove(originalObject);
        }

        /// <summary>
        /// Hides all ghost objects
        /// </summary>
        public void HideAllGhosts()
        {
            List<GameObject> objectsToRemove = new List<GameObject>(ghostObjects.Keys);
            foreach (GameObject obj in objectsToRemove)
            {
                HideGhostObject(obj);
            }
        }

        /// <summary>
        /// Creates a ghost object duplicate
        /// </summary>
        private GameObject CreateGhostObject(GameObject original, Vector3 position, Quaternion rotation)
        {
            GameObject ghost = new GameObject($"{original.name}_Ghost");

            // Copy mesh
            MeshFilter originalMeshFilter = original.GetComponent<MeshFilter>();
            MeshRenderer originalMeshRenderer = original.GetComponent<MeshRenderer>();

            if (originalMeshFilter != null && originalMeshRenderer != null)
            {
                MeshFilter ghostMeshFilter = ghost.AddComponent<MeshFilter>();
                MeshRenderer ghostMeshRenderer = ghost.AddComponent<MeshRenderer>();

                ghostMeshFilter.mesh = originalMeshFilter.mesh;
                ghostMeshRenderer.material = GetGhostMaterial(originalMeshRenderer.material);
            }
            else
            {
                // If no mesh, try to copy all renderers
                Renderer[] renderers = original.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    GameObject ghostChild = new GameObject($"{renderer.name}_Ghost");
                    ghostChild.transform.SetParent(ghost.transform);
                    ghostChild.transform.localPosition = renderer.transform.localPosition;
                    ghostChild.transform.localRotation = renderer.transform.localRotation;
                    ghostChild.transform.localScale = renderer.transform.localScale;

                    MeshFilter mf = renderer.GetComponent<MeshFilter>();
                    MeshRenderer mr = renderer.GetComponent<MeshRenderer>();

                    if (mf != null && mr != null)
                    {
                        MeshFilter ghostMF = ghostChild.AddComponent<MeshFilter>();
                        MeshRenderer ghostMR = ghostChild.AddComponent<MeshRenderer>();

                        ghostMF.mesh = mf.mesh;
                        ghostMR.material = GetGhostMaterial(mr.material);
                    }
                }
            }

            // Set position and rotation
            ghost.transform.position = position;
            ghost.transform.rotation = rotation;
            ghost.transform.localScale = original.transform.localScale * ghostScale;

            // Disable colliders and interaction
            Collider[] colliders = ghost.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            return ghost;
        }

        /// <summary>
        /// Gets or creates ghost material
        /// </summary>
        private Material GetGhostMaterial(Material originalMaterial)
        {
            if (ghostMaterial != null)
            {
                return ghostMaterial;
            }

            // Create a simple unlit material with green tint
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = ghostColor;
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Alpha
            mat.renderQueue = 3000; // Transparent queue

            return mat;
        }

        /// <summary>
        /// Applies outline-based highlighting
        /// </summary>
        private void ApplyOutlineHighlight(GameObject obj, HighlightData data)
        {
            // Try to use outline shader if available, otherwise use color
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            data.originalMaterials = new List<Material>();

            foreach (Renderer renderer in renderers)
            {
                data.originalMaterials.Add(renderer.material);
                
                // Create highlight material
                Material highlightMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                highlightMat.color = highlightColor;
                renderer.material = highlightMat;
            }
        }

        /// <summary>
        /// Removes outline highlighting
        /// </summary>
        private void RemoveOutlineHighlight(GameObject obj, HighlightData data)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            int matIndex = 0;

            foreach (Renderer renderer in renderers)
            {
                if (matIndex < data.originalMaterials.Count)
                {
                    renderer.material = data.originalMaterials[matIndex];
                    matIndex++;
                }
            }
        }

        /// <summary>
        /// Applies color-based highlighting
        /// </summary>
        private void ApplyColorHighlight(GameObject obj, HighlightData data)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            data.originalMaterials = new List<Material>();

            foreach (Renderer renderer in renderers)
            {
                data.originalMaterials.Add(renderer.material);
                
                // Tint the material
                Material highlightMat = new Material(renderer.material);
                highlightMat.color = highlightColor;
                renderer.material = highlightMat;
            }
        }

        /// <summary>
        /// Removes color highlighting
        /// </summary>
        private void RemoveColorHighlight(GameObject obj, HighlightData data)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            int matIndex = 0;

            foreach (Renderer renderer in renderers)
            {
                if (matIndex < data.originalMaterials.Count)
                {
                    renderer.material = data.originalMaterials[matIndex];
                    matIndex++;
                }
            }
        }

        /// <summary>
        /// Applies material-based highlighting
        /// </summary>
        private void ApplyMaterialHighlight(GameObject obj, HighlightData data)
        {
            if (highlightMaterial == null)
            {
                ApplyColorHighlight(obj, data);
                return;
            }

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            data.originalMaterials = new List<Material>();

            foreach (Renderer renderer in renderers)
            {
                data.originalMaterials.Add(renderer.material);
                renderer.material = highlightMaterial;
            }
        }

        /// <summary>
        /// Removes material highlighting
        /// </summary>
        private void RemoveMaterialHighlight(GameObject obj, HighlightData data)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            int matIndex = 0;

            foreach (Renderer renderer in renderers)
            {
                if (matIndex < data.originalMaterials.Count)
                {
                    renderer.material = data.originalMaterials[matIndex];
                    matIndex++;
                }
            }
        }

        /// <summary>
        /// Updates pulsing animation
        /// </summary>
        private void UpdatePulsing()
        {
            float pulseValue = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0 to 1
            float alpha = Mathf.Lerp(0.5f, 1f, pulseValue);

            foreach (var kvp in highlightedObjects)
            {
                GameObject obj = kvp.Key;
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

                foreach (Renderer renderer in renderers)
                {
                    if (renderer.material != null)
                    {
                        Color color = renderer.material.color;
                        color.a = alpha;
                        renderer.material.color = color;
                    }
                }
            }
        }

        /// <summary>
        /// Highlight methods available
        /// </summary>
        public enum HighlightMethod
        {
            Outline,
            Color,
            Material
        }

        /// <summary>
        /// Data structure for tracking highlights
        /// </summary>
        private class HighlightData
        {
            public GameObject originalObject;
            public List<Material> originalMaterials = new List<Material>();
        }
    }
}

