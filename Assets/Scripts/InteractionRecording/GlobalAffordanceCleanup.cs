using UnityEngine;

namespace VRInteractionRecording
{
    /// <summary>
    /// Global cleanup script to disable all broken Affordance System components in the scene
    /// Add this to any GameObject (preferably a manager or the scene root) to clean up all Affordance errors
    /// </summary>
    public class GlobalAffordanceCleanup : MonoBehaviour
    {
        [Header("Cleanup Settings")]
        [SerializeField]
        [Tooltip("If true, automatically runs cleanup on Awake (runs before Start)")]
        private bool autoCleanupOnAwake = true;

        [SerializeField]
        [Tooltip("If true, also runs cleanup on Start (in case components are added later)")]
        private bool autoCleanupOnStart = true;

        [SerializeField]
        [Tooltip("If true, also disables AnchorVisuals components")]
        private bool disableAnchorVisuals = true;

        [SerializeField]
        [Tooltip("If true, also disables broken LazyFollow components")]
        private bool disableLazyFollow = true;

        [SerializeField]
        [Tooltip("If true, continuously monitors and disables new Affordance components")]
        private bool continuousCleanup = true;

        private float lastCleanupTime = 0f;
        private const float CLEANUP_INTERVAL = 1f; // Clean up every second

        private void Awake()
        {
            if (autoCleanupOnAwake)
            {
                CleanupAllAffordances();
            }
        }

        private void Start()
        {
            if (autoCleanupOnStart)
            {
                // Delay slightly to catch components that initialize after Awake
                Invoke(nameof(CleanupAllAffordances), 0.1f);
            }
            
            // Clean up WebView Keyboard Manager on scene start
            CleanupWebViewKeyboardManager();
        }

        private void Update()
        {
            if (continuousCleanup && Time.time - lastCleanupTime >= CLEANUP_INTERVAL)
            {
                CleanupAllAffordances();
                lastCleanupTime = Time.time;
            }
        }

        /// <summary>
        /// Finds and disables all broken Affordance components in the scene
        /// </summary>
        [ContextMenu("Cleanup All Affordances")]
        public void CleanupAllAffordances()
        {
            int disabledCount = 0;

            // Find all MonoBehaviour components in the scene (more direct approach)
            MonoBehaviour[] allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (MonoBehaviour comp in allComponents)
            {
                if (comp == null) continue;
                if (!comp.enabled) continue; // Skip already disabled components

                System.Type compType = comp.GetType();
                if (compType == null) continue;

                string typeName = compType.Name;
                string fullTypeName = compType.FullName;
                string assemblyName = compType.Assembly.GetName().Name;

                // Check if it's an Affordance System component from XR Interaction Toolkit
                bool isAffordanceComponent = false;

                // Check by namespace/assembly first (most reliable)
                if (assemblyName != null && assemblyName.Contains("XR.Interaction.Toolkit"))
                {
                    // Check base types for generic types
                    System.Type baseType = compType.BaseType;
                    while (baseType != null)
                    {
                        string baseTypeName = baseType.Name;
                        string baseFullName = baseType.FullName;
                        
                        if (baseFullName != null && (
                            baseFullName.Contains("AffordanceSystem") ||
                            baseFullName.Contains("AffordanceStateProvider") ||
                            baseFullName.Contains("AffordanceStateReceiver") ||
                            baseTypeName.Contains("BaseAsyncAffordanceStateReceiver") ||
                            baseTypeName.Contains("BaseAffordanceStateProvider")
                        ))
                        {
                            isAffordanceComponent = true;
                            break;
                        }
                        baseType = baseType.BaseType;
                    }

                    // Also check the type itself
                    if (!isAffordanceComponent && fullTypeName != null && (
                        fullTypeName.Contains("AffordanceSystem") ||
                        fullTypeName.Contains("AffordanceStateProvider") ||
                        fullTypeName.Contains("AffordanceStateReceiver") ||
                        fullTypeName.Contains("BaseAsyncAffordanceStateReceiver") ||
                        fullTypeName.Contains("BaseAffordanceStateProvider")
                    ))
                    {
                        isAffordanceComponent = true;
                    }
                }

                // Also check by type name patterns (catch-all)
                if (!isAffordanceComponent && (
                    typeName.Contains("AffordanceStateProvider") ||
                    typeName.Contains("AffordanceStateReceiver") ||
                    typeName.Contains("BaseAsyncAffordanceStateReceiver") ||
                    typeName.Contains("BaseAffordanceStateProvider") ||
                    (fullTypeName != null && fullTypeName.Contains("AffordanceSystem"))
                ))
                {
                    isAffordanceComponent = true;
                }

                if (isAffordanceComponent)
                {
                    comp.enabled = false;
                    disabledCount++;
                    // Log all for debugging
                    Debug.Log($"GlobalAffordanceCleanup: Disabled {typeName} (Full: {fullTypeName}) on {comp.gameObject.name}");
                }

                // Also disable AnchorVisuals if requested
                if (disableAnchorVisuals && (typeName == "AnchorVisuals" || (fullTypeName != null && fullTypeName.Contains("AnchorVisuals"))))
                {
                    if (comp.enabled)
                    {
                        comp.enabled = false;
                        disabledCount++;
                        if (disabledCount <= 5)
                        {
                            Debug.Log($"GlobalAffordanceCleanup: Disabled AnchorVisuals on {comp.gameObject.name}");
                        }
                    }
                }

                // Disable broken LazyFollow components if requested
                if (disableLazyFollow && (typeName == "LazyFollow" || (fullTypeName != null && fullTypeName.Contains("LazyFollow"))))
                {
                    // Check if LazyFollow has null references by trying to access its properties
                    try
                    {
                        var targetProperty = compType.GetProperty("target");
                        if (targetProperty != null)
                        {
                            var targetValue = targetProperty.GetValue(comp);
                            if (targetValue == null)
                            {
                                // LazyFollow has null target - disable it
                                comp.enabled = false;
                                disabledCount++;
                                if (disabledCount <= 10)
                                {
                                    Debug.Log($"GlobalAffordanceCleanup: Disabled LazyFollow with null target on {comp.gameObject.name}");
                                }
                            }
                        }
                    }
                    catch
                    {
                        // If we can't check, disable it anyway to be safe
                        comp.enabled = false;
                        disabledCount++;
                        if (disabledCount <= 10)
                        {
                            Debug.Log($"GlobalAffordanceCleanup: Disabled LazyFollow (error checking) on {comp.gameObject.name}");
                        }
                    }
                }
            }

            if (disabledCount > 0)
            {
                Debug.Log($"GlobalAffordanceCleanup: Disabled {disabledCount} Affordance/AnchorVisuals/LazyFollow components");
            }
            else
            {
                // Debug: Log if we're not finding any components
                Debug.Log("GlobalAffordanceCleanup: No Affordance components found (this is normal if they're already disabled)");
            }
        }

        /// <summary>
        /// Cleans up WebView Keyboard Manager GameObject that Vuplex creates
        /// Note: We don't disable it because Vuplex needs it - we just ensure it's properly initialized
        /// </summary>
        [ContextMenu("Cleanup WebView Keyboard Manager")]
        public void CleanupWebViewKeyboardManager()
        {
            // Don't disable the keyboard manager - Vuplex needs it
            // The "not cleaned up" warning is harmless, it's just Unity warning about DontDestroyOnLoad objects
            // We'll leave it enabled so Vuplex can access it properly
            GameObject keyboardManager = GameObject.Find("WebView Keyboard Manager");
            if (keyboardManager != null)
            {
                // Ensure it's active so Vuplex can use it
                keyboardManager.SetActive(true);
                Debug.Log("GlobalAffordanceCleanup: WebView Keyboard Manager found and kept active (Vuplex requires it)");
            }
        }

        private void OnDestroy()
        {
            // Clean up WebView Keyboard Manager when this component is destroyed
            CleanupWebViewKeyboardManager();
        }
    }
}

