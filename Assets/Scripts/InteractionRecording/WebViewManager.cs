using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Threading.Tasks;

namespace VRInteractionRecording
{
    /// <summary>
    /// Serializable message structure for WebView communication
    /// </summary>
    [Serializable]
    public class SerializableMessage
    {
        public string type;
        public string taskJSON;  // JSON string, not object (JsonUtility can't handle nested objects)
        public string recordingJSON;  // JSON string, not object
        public float totalDuration;
    }

    /// <summary>
    /// Manages WebView display and communication with JavaScript
    /// Replaces Video Player component in EditPanel2
    /// </summary>
    public class WebViewManager : MonoBehaviour
    {
        [Header("WebView Setup")]
        [SerializeField]
        [Tooltip("Auto-detect: Uses StreamingAssets for builds, localhost for editor. Or set custom URL.")]
        private bool autoDetectURL = true;

        [SerializeField]
        [Tooltip("Custom URL (only used if Auto Detect URL is false). Use http://localhost:8000/timeline-editor.html for development")]
        private string customWebViewURL = "http://localhost:8000/timeline-editor.html";

        [SerializeField]
        [Tooltip("Quad/Plane GameObject where WebView texture will be displayed")]
        private GameObject webViewDisplayQuad;

        [Header("Timeline Integration")]
        [SerializeField]
        [Tooltip("Timeline slider that controls the number display")]
        private Slider timelineSlider;

        [SerializeField]
        [Tooltip("Update frequency for sending slider value to webview (updates per second)")]
        private float updateFrequency = 30f;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Enable debug logging")]
        private bool enableDebugLog = true;

        // WebView component reference (will be set when Vuplex is added)
        private UnityEngine.Object webViewComponent;
        private float lastUpdateTime = 0f;
        private float timeBetweenUpdates;
        
        // WebView initialization and message queue
        private bool isWebViewReady = false;
        private System.Collections.Generic.Queue<string> pendingMessages = new System.Collections.Generic.Queue<string>();

        // For testing without Vuplex - simple number display
        private TextMeshPro testDisplayText;
        private bool isShowingJSON = false;
        private string currentJSON = "";
        private float totalDuration = 0f; // Total duration of the recording for step highlighting
        
        // Diagnostic display
        private TextMeshPro diagnosticText;
        private string diagnosticMessage = "";

        private void Awake()
        {
            // Ensure KeyboardManager exists early to prevent null reference errors
            EnsureKeyboardManagerExists();
        }

        private void Start()
        {
            timeBetweenUpdates = 1f / updateFrequency;

            // Disable old VideoPlayer components to prevent errors
            DisableOldVideoComponents();

            // Note: Affordance cleanup is handled globally by GlobalAffordanceCleanup in SimpleInteractionUIController

            // Try to find WebView component (Vuplex or other)
            FindWebViewComponent();

            // If not found, try to create it automatically
            if (webViewComponent == null)
            {
                TryCreateWebViewComponent();
            }
            
            // Set KeyboardEnabled early for any existing WebView components
            DisableKeyboardOnExistingWebView();
            
            // Position existing WebView to match Quad
            PositionWebViewToMatchQuad();

            LogDebug("WebViewManager: Starting initialization...");
            string urlToUse = GetWebViewURL();
            Debug.LogError($"[WebViewManager] ⚠️ Using URL: {urlToUse}"); // Always log this
            LogDebug($"WebViewManager: Using URL: {urlToUse}");

            // Create diagnostic display
            CreateDiagnosticDisplay();
            
            // If no WebView found, create test display
            if (webViewComponent == null)
            {
                SetDiagnosticMessage("❌ No WebView component found!\n\nAdd CanvasWebViewPrefab component\nto this GameObject or a child.");
                Debug.LogError("[WebViewManager] ❌ No WebView component found - creating test display");
                LogDebug("WebViewManager: No WebView component found - creating test display");
                CreateTestDisplay();
                LogDebug("WebViewManager: No WebView component found. Using test display. Install Vuplex WebView for full functionality.");
            }
            else
            {
                SetDiagnosticMessage($"✅ WebView found: {webViewComponent.GetType().Name}\n🔄 Initializing...");
                Debug.LogError($"[WebViewManager] ✅ WebView component found: {webViewComponent.GetType().Name} - initializing");
                LogDebug("WebViewManager: WebView component found - initializing");
                InitializeWebView();
            }

            // Subscribe to slider changes
            if (timelineSlider != null)
            {
                timelineSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }
        }

        private void Update()
        {
            // Throttle updates to avoid spamming webview
            if (Time.time - lastUpdateTime >= timeBetweenUpdates)
            {
                if (timelineSlider != null)
                {
                    SendSliderValueToWebView(timelineSlider.value);
                }
                lastUpdateTime = Time.time;
            }

            // Periodically try to process pending messages if WebView becomes ready
            // This ensures messages don't get stuck in queue if timing is off
            if (pendingMessages.Count > 0)
            {
                if (isWebViewReady && webViewComponent != null)
                {
                    Debug.LogError($"🔄 Update() detected {pendingMessages.Count} pending messages - processing now (Time: {Time.time:F2}s)");
                    ProcessPendingMessages();
                }
                else
                {
                    // Log occasionally that we're still waiting
                    if (Time.frameCount % 300 == 0) // Every ~5 seconds at 60fps
                    {
                        Debug.LogError($"⏳ Still waiting for WebView... {pendingMessages.Count} messages in queue. isWebViewReady={isWebViewReady}");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the appropriate URL for WebView based on platform and settings
        /// </summary>
        private string GetWebViewURL()
        {
            if (!autoDetectURL)
            {
                return customWebViewURL;
            }

            // Vuplex supports streaming-assets:// URL scheme for StreamingAssets
            // Format: streaming-assets://WebContent/timeline-editor.html
            // This works on all platforms (editor, Android, etc.)
            string streamingAssetsURL = "streaming-assets://WebContent/timeline-editor.html";

            // Verify file exists in editor
            #if UNITY_EDITOR
            string streamingAssetsPath = System.IO.Path.Combine(Application.streamingAssetsPath, "WebContent", "timeline-editor.html");
            if (System.IO.File.Exists(streamingAssetsPath))
            {
                LogDebug($"WebViewManager: Found StreamingAssets file, using: {streamingAssetsURL}");
                return streamingAssetsURL;
            }
            else
            {
                // Fall back to localhost for development
                LogDebug("WebViewManager: StreamingAssets not found, using localhost (start server with: python3 -m http.server 8000)");
                return "http://localhost:8000/timeline-editor.html";
            }
            #else
            // On device, always use streaming-assets:// (works on Android/Quest 3)
            LogDebug($"WebViewManager: Using StreamingAssets URL: {streamingAssetsURL}");
            return streamingAssetsURL;
            #endif
        }

        /// <summary>
        /// Disables old VideoPlayer components to prevent errors
        /// </summary>
        private void DisableOldVideoComponents()
        {
            // Disable VideoPlayer component
            VideoPlayer videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer != null)
            {
                videoPlayer.enabled = false;
                LogDebug("WebViewManager: Disabled VideoPlayer component");
            }

            // Disable VideoTimeScrubControl if it exists
            System.Type videoTimeScrubType = System.Type.GetType("Unity.VRTemplate.VideoTimeScrubControl, Assembly-CSharp");
            if (videoTimeScrubType == null)
            {
                videoTimeScrubType = System.Type.GetType("Unity.SpatialFramework.UI.VideoTimeScrubControl, Assembly-CSharp");
            }

            if (videoTimeScrubType != null)
            {
                MonoBehaviour videoTimeScrub = GetComponent(videoTimeScrubType) as MonoBehaviour;
                if (videoTimeScrub != null)
                {
                    videoTimeScrub.enabled = false;
                    LogDebug("WebViewManager: Disabled VideoTimeScrubControl component");
                }
            }
        }

        /// <summary>
        /// Finds WebView component (Vuplex or other)
        /// </summary>
        private void FindWebViewComponent()
        {
            // Try to find Vuplex WebView
            System.Type vuplexType = System.Type.GetType("Vuplex.WebView.CanvasWebViewPrefab, Vuplex.WebView");
            if (vuplexType != null)
            {
                webViewComponent = GetComponent(vuplexType);
                if (webViewComponent == null)
                {
                    webViewComponent = GetComponentInChildren(vuplexType);
                }
                if (webViewComponent == null)
                {
                    // Try to find in parent
                    if (transform.parent != null)
                    {
                        webViewComponent = transform.parent.GetComponent(vuplexType);
                    }
                }
                if (webViewComponent == null)
                {
                    // Try to find anywhere in scene
                    webViewComponent = FindFirstObjectByType(vuplexType);
                }
                if (webViewComponent != null)
                {
                    LogDebug("WebViewManager: Found Vuplex WebView component");

                    // Enable drag mode on existing WebView
                    var dragModeProperty = vuplexType.GetProperty("DragMode");
                    if (dragModeProperty != null)
                    {
                        System.Type dragModeEnumType = System.Type.GetType("Vuplex.WebView.DragMode, Vuplex.WebView");
                        if (dragModeEnumType != null)
                        {
                            var dragWithinPageValue = System.Enum.Parse(dragModeEnumType, "DragWithinPage");
                            dragModeProperty.SetValue(webViewComponent, dragWithinPageValue);
                            Debug.LogError("[WebViewManager] ✅ Enabled DragMode = DragWithinPage on existing WebView");
                        }
                    }
                }
            }

            // Try to find 3D WebView
            if (webViewComponent == null)
            {
                System.Type webView3DType = System.Type.GetType("Gree.WebView.WebView, Gree.WebView");
                if (webView3DType != null)
                {
                    webViewComponent = GetComponent(webView3DType);
                    if (webViewComponent == null)
                    {
                        webViewComponent = GetComponentInChildren(webView3DType);
                    }
                    if (webViewComponent != null)
                    {
                        LogDebug("WebViewManager: Found 3D WebView component");
                    }
                }
            }
        }

        /// <summary>
        /// Ensures KeyboardManager.Instance exists early to prevent null reference errors
        /// </summary>
        private void EnsureKeyboardManagerExists()
        {
            try
            {
                System.Type keyboardManagerType = System.Type.GetType("Vuplex.WebView.Internal.KeyboardManager, Vuplex.WebView");
                if (keyboardManagerType != null)
                {
                    var instanceProperty = keyboardManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (instanceProperty != null)
                    {
                        // Access Instance to trigger lazy initialization
                        var instance = instanceProperty.GetValue(null);
                        if (instance != null)
                        {
                            LogDebug("WebViewManager: KeyboardManager.Instance initialized");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogDebug($"WebViewManager: Could not initialize KeyboardManager: {e.Message}");
            }
        }

        /// <summary>
        /// Positions WebView to match Quad's size and position
        /// </summary>
        private void PositionWebViewToMatchQuad()
        {
            if (webViewComponent == null) return;
            
            // Find the Quad GameObject
            RectTransform quadRectTransform = null;
            if (webViewDisplayQuad != null)
            {
                quadRectTransform = webViewDisplayQuad.GetComponent<RectTransform>();
            }
            if (quadRectTransform == null)
            {
                Transform quadTransform = transform.Find("Quad");
                if (quadTransform != null)
                {
                    quadRectTransform = quadTransform.GetComponent<RectTransform>();
                    webViewDisplayQuad = quadTransform.gameObject;
                }
            }
            
            if (quadRectTransform == null)
            {
                LogDebug("WebViewManager: No Quad found - WebView will use default positioning");
                return;
            }
            
            // Get WebView's RectTransform
            System.Type vuplexType = webViewComponent.GetType();
            RectTransform webViewRectTransform = (webViewComponent as MonoBehaviour)?.GetComponent<RectTransform>();
            
            if (webViewRectTransform != null)
            {
                // Match Quad's horizontal positioning but center vertically in the panel
                // Keep same width (left/right anchors) but adjust vertical anchors to center
                webViewRectTransform.anchorMin = new Vector2(quadRectTransform.anchorMin.x, 0.15f); // Start 15% from bottom
                webViewRectTransform.anchorMax = new Vector2(quadRectTransform.anchorMax.x, 0.85f); // End 15% from top (centered)
                webViewRectTransform.anchoredPosition = Vector2.zero; // Reset since anchors handle positioning
                webViewRectTransform.sizeDelta = Vector2.zero; // No size delta needed with anchors
                webViewRectTransform.pivot = quadRectTransform.pivot;
                webViewRectTransform.localScale = quadRectTransform.localScale;
                webViewRectTransform.localRotation = quadRectTransform.localRotation;
                
                // Ensure WebView is behind the Quad in sibling order
                int quadIndex = quadRectTransform.GetSiblingIndex();
                webViewRectTransform.SetSiblingIndex(quadIndex);
                
                // Set sort order - keep it low to not block UI, but ensure it's visible
                Canvas webViewCanvas = (webViewComponent as MonoBehaviour)?.GetComponent<Canvas>();
                if (webViewCanvas == null)
                {
                    webViewCanvas = (webViewComponent as MonoBehaviour)?.GetComponentInChildren<Canvas>();
                }
                if (webViewCanvas != null)
                {
                    // CRITICAL: For nested Canvas, we need HIGHER sorting order to receive input
                    // The WebView Canvas must be ABOVE the parent Canvas to receive raycasts
                    Canvas parentCanvas = GetComponentInParent<Canvas>();
                    if (parentCanvas != null && parentCanvas != webViewCanvas)
                    {
                        // Set WebView Canvas sorting order HIGHER than parent to ensure it receives input
                        webViewCanvas.sortingOrder = parentCanvas.sortingOrder + 1;
                        LogDebug($"WebViewManager: Set WebView Canvas sorting order to {webViewCanvas.sortingOrder} (parent is {parentCanvas.sortingOrder})");
                    }
                    else
                    {
                        // Use 1 (above default UI) if no parent Canvas
                        webViewCanvas.sortingOrder = 1;
                        LogDebug("WebViewManager: Set WebView Canvas sorting order to 1");
                    }
                    
                    // CRITICAL: Set Event Camera FIRST before adding TrackedDeviceGraphicRaycaster
                    // TrackedDeviceGraphicRaycaster needs the eventCamera to be set during initialization
                    Camera mainCamera = Camera.main;
                    if (mainCamera == null)
                    {
                        mainCamera = FindFirstObjectByType<Camera>();
                    }
                    if (mainCamera != null)
                    {
                        // For Screen Space Camera and World Space, set worldCamera
                        if (webViewCanvas.renderMode == RenderMode.ScreenSpaceCamera || webViewCanvas.renderMode == RenderMode.WorldSpace)
                        {
                            if (webViewCanvas.worldCamera != mainCamera)
                            {
                                webViewCanvas.worldCamera = mainCamera;
                                LogDebug($"WebViewManager: Set WebView Canvas Event Camera to: {mainCamera.name}");
                            }
                        }
                        // For Screen Space Overlay, we can't set worldCamera, but we ensure the canvas is configured
                        else if (webViewCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        {
                            LogDebug($"WebViewManager: Canvas is Screen Space Overlay - Event Camera not needed, but ensure XRUIInputModule is present");
                        }
                    }

                    // CRITICAL: Now add TrackedDeviceGraphicRaycaster AFTER eventCamera is set
                    // Only add at runtime to avoid Unity XR Toolkit build errors
                    #if !UNITY_EDITOR || UNITY_EDITOR
                    // Check if TrackedDeviceGraphicRaycaster exists
                    var trackedRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
                    if (trackedRaycasterType != null)
                    {
                        var trackedRaycaster = webViewCanvas.GetComponent(trackedRaycasterType);
                        if (trackedRaycaster == null && Application.isPlaying)
                        {
                            // Add TrackedDeviceGraphicRaycaster for XR Interaction Toolkit (only at runtime)
                            trackedRaycaster = webViewCanvas.gameObject.AddComponent(trackedRaycasterType);
                            LogDebug("WebViewManager: Added TrackedDeviceGraphicRaycaster to WebView Canvas (required for XR Interaction Toolkit)");
                        }
                        // Enable it if it exists
                        if (trackedRaycaster != null)
                        {
                            var enabledProperty = trackedRaycasterType.GetProperty("enabled");
                            if (enabledProperty != null)
                            {
                                enabledProperty.SetValue(trackedRaycaster, true);
                                LogDebug("WebViewManager: Enabled TrackedDeviceGraphicRaycaster on WebView Canvas");
                            }
                        }
                    }
                    else
                    {
                        // Fallback: Use regular GraphicRaycaster if XR Interaction Toolkit not available
                        UnityEngine.UI.GraphicRaycaster raycaster = webViewCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                        if (raycaster == null && Application.isPlaying)
                        {
                            raycaster = webViewCanvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                            LogDebug("WebViewManager: Added GraphicRaycaster to WebView Canvas (fallback)");
                        }
                        if (raycaster != null && !raycaster.enabled)
                        {
                            raycaster.enabled = true;
                            LogDebug("WebViewManager: Enabled GraphicRaycaster on WebView Canvas");
                        }
                    }
                    #endif
                    
                    // CRITICAL: Check parent Canvas for blocking graphics
                    if (parentCanvas != null && parentCanvas != webViewCanvas)
                    {
                        CheckParentCanvasForBlocking(parentCanvas);
                    }
                }
                
                LogDebug("WebViewManager: Positioned WebView centered vertically in panel");
            }
            
            // CRITICAL: Ensure EventSystem has XRUIInputModule
            EnsureEventSystemHasXRInputModule();
        }
        
        /// <summary>
        /// Checks parent Canvas for graphics that might block raycasts to child Canvas
        /// </summary>
        private void CheckParentCanvasForBlocking(Canvas parentCanvas)
        {
            // Check if parent Canvas has GraphicRaycaster that might block
            UnityEngine.UI.GraphicRaycaster parentRaycaster = parentCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (parentRaycaster != null)
            {
                // Check blockingObjects setting
                if (parentRaycaster.blockingObjects != GraphicRaycaster.BlockingObjects.None)
                {
                    LogDebug($"WebViewManager: Parent Canvas '{parentCanvas.name}' has blocking raycaster - this might block WebView clicks");
                    Debug.LogWarning($"[WebViewManager] ⚠️ Parent Canvas '{parentCanvas.name}' has blocking raycaster. Consider setting blockingObjects to 'None' or ensuring WebView Canvas has higher sorting order.");
                }
            }
            
            // Check for Image components on parent Canvas that might block raycasts
            Image[] blockingImages = parentCanvas.GetComponentsInChildren<Image>();
            foreach (Image img in blockingImages)
            {
                // If image is on the parent Canvas itself (not a child), it might block
                if (img.transform.parent == parentCanvas.transform && img.raycastTarget)
                {
                    // Check if this image is covering the WebView area
                    RectTransform imgRect = img.GetComponent<RectTransform>();
                    if (imgRect != null)
                    {
                        // If image covers most of the canvas, it might block
                        if (imgRect.anchorMin.x < 0.1f && imgRect.anchorMax.x > 0.9f &&
                            imgRect.anchorMin.y < 0.1f && imgRect.anchorMax.y > 0.9f)
                        {
                            LogDebug($"WebViewManager: Found blocking Image '{img.name}' on parent Canvas - consider disabling raycastTarget");
                            Debug.LogWarning($"[WebViewManager] ⚠️ Image '{img.name}' on parent Canvas might block WebView clicks. Consider setting raycastTarget = false.");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Ensures EventSystem exists and has XRUIInputModule (not StandaloneInputModule)
        /// </summary>
        private void EnsureEventSystemHasXRInputModule()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogWarning("[WebViewManager] ⚠️ No EventSystem found in scene! WebView interaction will not work.");
                LogDebug("WebViewManager: No EventSystem found - WebView clicks will not work");
                return;
            }
            
            var xrInputModuleType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
            if (xrInputModuleType != null)
            {
                var xrInputModule = eventSystem.GetComponent(xrInputModuleType);
                if (xrInputModule == null)
                {
                    // Check for StandaloneInputModule (conflicts with XR)
                    StandaloneInputModule standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
                    if (standaloneModule != null)
                    {
                        Debug.LogWarning("[WebViewManager] ⚠️ Found StandaloneInputModule - this conflicts with XR! Removing it.");
                        Destroy(standaloneModule);
                    }
                    
                    // Add XRUIInputModule
                    eventSystem.gameObject.AddComponent(xrInputModuleType);
                    Debug.LogError("[WebViewManager] ✅ Added XRUIInputModule to EventSystem (required for XR Interaction Toolkit)");
                    LogDebug("WebViewManager: Added XRUIInputModule to EventSystem");
                }
                else
                {
                    LogDebug("WebViewManager: EventSystem already has XRUIInputModule");
                }
            }
            else
            {
                Debug.LogWarning("[WebViewManager] ⚠️ XR Interaction Toolkit not found - cannot add XRUIInputModule");
                LogDebug("WebViewManager: XR Interaction Toolkit not found - WebView interaction may not work in VR");
            }
        }

        /// <summary>
        /// Disables keyboard on any existing WebView components found in the scene
        /// </summary>
        private void DisableKeyboardOnExistingWebView()
        {
            System.Type vuplexType = System.Type.GetType("Vuplex.WebView.CanvasWebViewPrefab, Vuplex.WebView");
            if (vuplexType != null)
            {
                MonoBehaviour[] allWebViews = FindObjectsByType(vuplexType, FindObjectsInactive.Include, FindObjectsSortMode.None) as MonoBehaviour[];
                if (allWebViews != null)
                {
                    foreach (MonoBehaviour webView in allWebViews)
                    {
                        if (webView != null)
                        {
                            var keyboardEnabledProperty = vuplexType.GetProperty("KeyboardEnabled");
                            if (keyboardEnabledProperty != null)
                            {
                                try
                                {
                                    keyboardEnabledProperty.SetValue(webView, false);
                                    LogDebug($"WebViewManager: Disabled keyboard on {webView.gameObject.name}");
                                }
                                catch (Exception e)
                                {
                                    LogDebug($"WebViewManager: Could not disable keyboard on {webView.gameObject.name}: {e.Message}");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tries to create CanvasWebViewPrefab component automatically
        /// </summary>
        private void TryCreateWebViewComponent()
        {
            System.Type vuplexType = System.Type.GetType("Vuplex.WebView.CanvasWebViewPrefab, Vuplex.WebView");
            if (vuplexType == null)
            {
                Debug.LogError("[WebViewManager] ❌ Vuplex.WebView.CanvasWebViewPrefab type not found! Is Vuplex package installed?");
                return;
            }

            // Check if we're on a Canvas (CanvasWebViewPrefab needs a Canvas)
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[WebViewManager] ❌ No Canvas found! CanvasWebViewPrefab needs to be on a Canvas GameObject.");
                return;
            }

            // Find the Quad GameObject to match its size/position
            RectTransform quadRectTransform = null;
            if (webViewDisplayQuad != null)
            {
                quadRectTransform = webViewDisplayQuad.GetComponent<RectTransform>();
                if (quadRectTransform == null)
                {
                    // Try to find Quad in children
                    Transform quadTransform = transform.Find("Quad");
                    if (quadTransform != null)
                    {
                        quadRectTransform = quadTransform.GetComponent<RectTransform>();
                    }
                }
            }
            else
            {
                // Try to find Quad in children
                Transform quadTransform = transform.Find("Quad");
                if (quadTransform != null)
                {
                    quadRectTransform = quadTransform.GetComponent<RectTransform>();
                    webViewDisplayQuad = quadTransform.gameObject;
                }
            }

            // Try to instantiate CanvasWebViewPrefab
            try
            {
                var instantiateMethod = vuplexType.GetMethod("Instantiate", new Type[0]);
                if (instantiateMethod != null)
                {
                    Debug.LogError("[WebViewManager] 🔧 Attempting to create CanvasWebViewPrefab automatically...");
                    var prefab = instantiateMethod.Invoke(null, null) as MonoBehaviour;
                    if (prefab != null)
                    {
                        // Parent it to the same parent as Quad, or to Canvas if Quad not found
                        Transform parentTransform = quadRectTransform != null ? quadRectTransform.parent : canvas.transform;
                        prefab.transform.SetParent(parentTransform, false);
                        
                        // Match Quad's RectTransform if it exists, but center vertically
                        RectTransform webViewRectTransform = prefab.GetComponent<RectTransform>();
                        if (webViewRectTransform != null && quadRectTransform != null)
                        {
                            // Match horizontal positioning but center vertically in the panel
                            webViewRectTransform.anchorMin = new Vector2(quadRectTransform.anchorMin.x, 0.15f); // Start 15% from bottom
                            webViewRectTransform.anchorMax = new Vector2(quadRectTransform.anchorMax.x, 0.85f); // End 15% from top (centered)
                            webViewRectTransform.anchoredPosition = Vector2.zero; // Reset since anchors handle positioning
                            webViewRectTransform.sizeDelta = Vector2.zero; // No size delta needed with anchors
                            webViewRectTransform.pivot = quadRectTransform.pivot;
                            webViewRectTransform.localScale = quadRectTransform.localScale;
                            webViewRectTransform.localRotation = quadRectTransform.localRotation;
                            
                            // Set sibling index to be behind the Quad (so Quad's renderer is on top if needed)
                            int quadIndex = quadRectTransform.GetSiblingIndex();
                            webViewRectTransform.SetSiblingIndex(quadIndex);
                            
                            Debug.LogError("[WebViewManager] ✅ Positioned CanvasWebViewPrefab centered vertically");
                        }
                        else if (webViewRectTransform != null)
                        {
                            // No Quad found, position it relative to this GameObject, centered vertically
                            RectTransform thisRectTransform = GetComponent<RectTransform>();
                            if (thisRectTransform != null)
                            {
                                // Position it as a child of this GameObject, centered vertically
                                webViewRectTransform.anchorMin = new Vector2(0.1f, 0.15f);
                                webViewRectTransform.anchorMax = new Vector2(0.9f, 0.85f); // Centered vertically
                                webViewRectTransform.sizeDelta = Vector2.zero;
                                webViewRectTransform.anchoredPosition = Vector2.zero;
                            }
                            else
                            {
                                // Fallback: use default positioning, centered vertically
                                webViewRectTransform.anchorMin = new Vector2(0f, 0.15f);
                                webViewRectTransform.anchorMax = new Vector2(1f, 0.85f); // Centered vertically
                                webViewRectTransform.sizeDelta = Vector2.zero;
                                webViewRectTransform.anchoredPosition = Vector2.zero;
                            }
                        }
                        
                        // Set lower sort order to ensure it doesn't block UI elements like sliders
                        Canvas webViewCanvas = prefab.GetComponent<Canvas>();
                        if (webViewCanvas == null)
                        {
                            webViewCanvas = prefab.GetComponentInChildren<Canvas>();
                        }
                        if (webViewCanvas != null)
                        {
                            // CRITICAL: For nested Canvas, set HIGHER sorting order to receive input
                            Canvas parentCanvas = GetComponentInParent<Canvas>();
                            if (parentCanvas != null && parentCanvas != webViewCanvas)
                            {
                                webViewCanvas.sortingOrder = parentCanvas.sortingOrder + 1;
                                Debug.LogError($"[WebViewManager] ✅ Set WebView Canvas sorting order to {webViewCanvas.sortingOrder} (parent is {parentCanvas.sortingOrder})");
                            }
                            else
                            {
                                webViewCanvas.sortingOrder = 1; // Above default UI
                                Debug.LogError("[WebViewManager] ✅ Set WebView Canvas sorting order to 1");
                            }
                            
                            // CRITICAL: Set Event Camera FIRST before adding TrackedDeviceGraphicRaycaster
                            // TrackedDeviceGraphicRaycaster needs the eventCamera to be set during initialization
                            if (webViewCanvas.renderMode == RenderMode.ScreenSpaceCamera || webViewCanvas.renderMode == RenderMode.WorldSpace)
                            {
                                Camera mainCamera = Camera.main;
                                if (mainCamera == null)
                                {
                                    mainCamera = FindFirstObjectByType<Camera>();
                                }
                                if (mainCamera != null && webViewCanvas.worldCamera != mainCamera)
                                {
                                    webViewCanvas.worldCamera = mainCamera;
                                    Debug.LogError($"[WebViewManager] ✅ Set WebView Canvas Event Camera to: {mainCamera.name}");
                                }
                            }

                            // CRITICAL: Now add TrackedDeviceGraphicRaycaster AFTER eventCamera is set
                            // Only add at runtime to avoid Unity XR Toolkit build errors
                            #if !UNITY_EDITOR || UNITY_EDITOR
                            var trackedRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
                            if (trackedRaycasterType != null)
                            {
                                var trackedRaycaster = webViewCanvas.GetComponent(trackedRaycasterType);
                                if (trackedRaycaster == null && Application.isPlaying)
                                {
                                    // Add TrackedDeviceGraphicRaycaster for XR Interaction Toolkit (only at runtime)
                                    trackedRaycaster = webViewCanvas.gameObject.AddComponent(trackedRaycasterType);
                                    Debug.LogError("[WebViewManager] ✅ Added TrackedDeviceGraphicRaycaster to WebView Canvas (required for XR Interaction Toolkit)");
                                }
                                // Enable it if it exists
                                if (trackedRaycaster != null)
                                {
                                    var enabledProperty = trackedRaycasterType.GetProperty("enabled");
                                    if (enabledProperty != null)
                                    {
                                        enabledProperty.SetValue(trackedRaycaster, true);
                                        Debug.LogError("[WebViewManager] ✅ Enabled TrackedDeviceGraphicRaycaster on WebView Canvas");
                                    }
                                }
                            }
                            else
                            {
                                // Fallback: Use regular GraphicRaycaster
                                UnityEngine.UI.GraphicRaycaster raycaster = webViewCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                                if (raycaster == null && Application.isPlaying)
                                {
                                    raycaster = webViewCanvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                                    Debug.LogError("[WebViewManager] ✅ Added GraphicRaycaster to WebView Canvas (fallback)");
                                }
                                if (raycaster != null && !raycaster.enabled)
                                {
                                    raycaster.enabled = true;
                                    Debug.LogError("[WebViewManager] ✅ Enabled GraphicRaycaster on WebView Canvas");
                                }
                            }
                            #endif
                        }
                        
                        // Disable keyboard to prevent null reference errors
                        var keyboardEnabledProperty = vuplexType.GetProperty("KeyboardEnabled");
                        if (keyboardEnabledProperty != null)
                        {
                            keyboardEnabledProperty.SetValue(prefab, false);
                            Debug.LogError("[WebViewManager] ✅ Disabled keyboard on CanvasWebViewPrefab");
                        }

                        // Enable drag mode for VR interaction support
                        var dragModeProperty = vuplexType.GetProperty("DragMode");
                        if (dragModeProperty != null)
                        {
                            // Get DragMode enum type
                            System.Type dragModeEnumType = System.Type.GetType("Vuplex.WebView.DragMode, Vuplex.WebView");
                            if (dragModeEnumType != null)
                            {
                                // Try DragWithinPage mode first (allows dragging elements within the page)
                                var dragWithinPageValue = System.Enum.Parse(dragModeEnumType, "DragWithinPage");
                                dragModeProperty.SetValue(prefab, dragWithinPageValue);
                                Debug.LogError("[WebViewManager] ✅ Enabled DragMode = DragWithinPage for VR drag support");
                            }
                            else
                            {
                                Debug.LogWarning("[WebViewManager] ⚠️ DragMode enum type not found");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[WebViewManager] ⚠️ DragMode property not found on CanvasWebViewPrefab");
                        }

                        webViewComponent = prefab;
                        Debug.LogError("[WebViewManager] ✅ Created CanvasWebViewPrefab automatically!");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebViewManager] ❌ Error creating CanvasWebViewPrefab: {e.Message}");
            }
        }

        /// <summary>
        /// Initializes WebView with URL
        /// </summary>
        private async void InitializeWebView()
        {
            Debug.LogError("═══════════════════════════════════════════");
            Debug.LogError("🔧 InitializeWebView() CALLED");
            Debug.LogError($"   Time: {Time.time:F2}s");

            if (webViewComponent == null)
            {
                Debug.LogError("❌ No WebView component - using test display");
                LogDebug("WebViewManager: No WebView component found - using test display");
                return;
            }

            Debug.LogError($"✅ WebView component exists: {webViewComponent.GetType().Name}");

            try
            {
                // Use reflection to call WebView methods (works with Vuplex)
                System.Type webViewType = webViewComponent.GetType();
                LogDebug($"WebViewManager: WebView type: {webViewType.Name}");

                // Get the URL to use
                string urlToLoad = GetWebViewURL();
                Debug.LogError($"📍 URL to load: {urlToLoad}");
                LogDebug($"WebViewManager: URL to load: {urlToLoad}");

                // For Vuplex CanvasWebViewPrefab, we need to wait for initialization first
                // IMPORTANT: Wait for initialization BEFORE getting the WebView property
                var waitUntilInitializedMethod = webViewType.GetMethod("WaitUntilInitialized");
                if (waitUntilInitializedMethod != null)
                {
                    Debug.LogError("⏳ Waiting for WebView to initialize...");
                    LogDebug("WebViewManager: Waiting for WebView to initialize...");
                    var task = waitUntilInitializedMethod.Invoke(webViewComponent, null) as Task;
                    if (task != null)
                    {
                        await task;
                        Debug.LogError($"✅ WebView initialized! (Time: {Time.time:F2}s)");
                        LogDebug("WebViewManager: WebView initialized!");
                    }
                }
                else
                {
                    Debug.LogError("⚠️ WaitUntilInitialized method not found");
                }
                
                // For Vuplex CanvasWebViewPrefab, we need to get the WebView property AFTER initialization
                UnityEngine.Object actualWebView = webViewComponent;
                System.Type actualWebViewType = webViewType;
                
                // Try to get the WebView property (for CanvasWebViewPrefab)
                var webViewProperty = webViewType.GetProperty("WebView");
                if (webViewProperty != null)
                {
                    actualWebView = webViewProperty.GetValue(webViewComponent) as UnityEngine.Object;
                    if (actualWebView != null)
                    {
                        LogDebug("WebViewManager: Got WebView from CanvasWebViewPrefab");
                        actualWebViewType = actualWebView.GetType();
                    }
                    else
                    {
                        LogDebug("WebViewManager: WebView property is null - may not be initialized yet");
                        // Try again after a delay
                        Invoke(nameof(RetryWebViewInitialization), 1f);
                        return;
                    }
                }
                
                // IMPORTANT: Use actualWebView (the IWebView instance) for LoadUrl/LoadHtml, not the prefab
                if (actualWebView == null)
                {
                    SetDiagnosticMessage("❌ WebView instance is null!\nCheck initialization.");
                    Debug.LogError("[WebViewManager] ❌ WebView instance is null - cannot load content");
                    LogDebug("WebViewManager: WebView instance is null - cannot load content");
                    return;
                }
                
                // Try LoadUrl first (works on device with streaming-assets://)
                var loadUrlMethod = actualWebViewType.GetMethod("LoadUrl", new Type[] { typeof(string) });
                if (loadUrlMethod != null)
                {
                    SetDiagnosticMessage($"🔄 Loading URL:\n{urlToLoad}");
                    Debug.LogError($"🔄 Attempting to load URL: {urlToLoad} (Time: {Time.time:F2}s)");
                    LogDebug($"WebViewManager: Attempting to load URL: {urlToLoad}");
                    try
                    {
                        loadUrlMethod.Invoke(actualWebView, new object[] { urlToLoad });
                        SetDiagnosticMessage("✅ URL loaded!\nWaiting for content...");
                        Debug.LogError($"✅ LoadUrl called successfully (Time: {Time.time:F2}s)");
                        LogDebug($"WebViewManager: LoadUrl called successfully");

                        // Clear diagnostic after a delay
                        Invoke(nameof(ClearDiagnostic), 3f);
                    }
                    catch (Exception loadEx)
                    {
                        SetDiagnosticMessage($"❌ URL load failed:\n{loadEx.Message}\n\nTrying HTML fallback...");
                        Debug.LogError($"❌ Error loading URL: {loadEx.Message}\n{loadEx.StackTrace}");
                        LogDebug($"WebViewManager: URL load failed: {loadEx.Message}");
                        // Try loading HTML directly as fallback
                        TryLoadHTMLDirectly(actualWebView, actualWebViewType);
                    }
                }
                else
                {
                    Debug.LogError("⚠️ LoadUrl method not found - trying LoadHtml directly");
                    LogDebug("WebViewManager: LoadUrl method not found - trying LoadHtml directly");
                    TryLoadHTMLDirectly(actualWebView, actualWebViewType);
                }

                // Try to set up message handler for Vuplex
                // IMPORTANT: MessageEmitted is on the CanvasWebViewPrefab, not the WebView property
                // Check on the prefab type (webViewType), not actualWebViewType
                var messageEmittedEvent = webViewType.GetEvent("MessageEmitted");
                if (messageEmittedEvent != null)
                {
                    var handlerType = messageEmittedEvent.EventHandlerType;
                    var handler = Delegate.CreateDelegate(handlerType, this, typeof(WebViewManager).GetMethod("OnVuplexMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
                    messageEmittedEvent.AddEventHandler(webViewComponent, handler); // Use webViewComponent (prefab), not actualWebView
                    Debug.LogError("✅ MessageEmitted event handler set up");
                    LogDebug("WebViewManager: MessageEmitted event handler set up on CanvasWebViewPrefab");

                    // Mark as ready after message handler is set up and URL is loaded
                    // Give it time for the URL to load and JavaScript to initialize
                    // AND for Vuplex to inject its APIs into the JavaScript context
                    Debug.LogError($"⏰ Scheduling MarkWebViewReady() in 4 seconds (will execute at ~{Time.time + 4f:F2}s)");
                    Invoke(nameof(MarkWebViewReady), 4f); // Extra time for Vuplex API injection
                }
                else
                {
                    // Try SetMessageHandler as fallback (on actual WebView)
                    var setMessageHandlerMethod = actualWebViewType.GetMethod("SetMessageHandler", new Type[] { typeof(Action<string>) });
                    if (setMessageHandlerMethod != null)
                    {
                        setMessageHandlerMethod.Invoke(actualWebView, new object[] { new Action<string>(OnWebViewMessage) });
                        Debug.LogError("✅ SetMessageHandler set up");
                        LogDebug("WebViewManager: SetMessageHandler set up on WebView");

                        // Mark as ready after message handler is set up
                        // Increased delay for Vuplex API injection
                        Debug.LogError($"⏰ Scheduling MarkWebViewReady() in 3 seconds (will execute at ~{Time.time + 3f:F2}s)");
                        Invoke(nameof(MarkWebViewReady), 3f);
                    }
                    else
                    {
                        Debug.LogError("⚠️ No message handler method found - will mark ready after delay");
                        LogDebug("WebViewManager: No message handler method found - will mark ready after delay");
                        // Mark as ready anyway after a delay to allow URL to load
                        // Increased delay for Vuplex API injection
                        Debug.LogError($"⏰ Scheduling MarkWebViewReady() in 4 seconds (will execute at ~{Time.time + 4f:F2}s)");
                        Invoke(nameof(MarkWebViewReady), 4f);
                    }
                }

                Debug.LogError("═══════════════════════════════════════════");
            }
            catch (Exception e)
            {
                Debug.LogError($"WebViewManager: Error initializing WebView: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Handler for Vuplex MessageEmitted event
        /// </summary>
        private void OnVuplexMessage(object sender, EventArgs e)
        {
            // Extract message from event args
            var messageProperty = e.GetType().GetProperty("Value");
            if (messageProperty != null)
            {
                string message = messageProperty.GetValue(e) as string;
                OnWebViewMessage(message);
            }
        }

        /// <summary>
        /// Fallback: Loads HTML directly from StreamingAssets file
        /// </summary>
        private void TryLoadHTMLDirectly(UnityEngine.Object actualWebView, System.Type webViewType)
        {
            try
            {
                string htmlPath = System.IO.Path.Combine(Application.streamingAssetsPath, "WebContent", "timeline-editor.html");
                Debug.LogError($"[WebViewManager] 🔄 Trying to load HTML from: {htmlPath}");
                SetDiagnosticMessage($"🔄 Loading HTML from:\n{htmlPath}");
                
                if (System.IO.File.Exists(htmlPath))
                {
                    string htmlContent = System.IO.File.ReadAllText(htmlPath);
                    Debug.LogError($"[WebViewManager] ✅ HTML file found ({htmlContent.Length} chars)");
                    
                    var loadHtmlMethod = webViewType.GetMethod("LoadHtml", new Type[] { typeof(string) });
                    if (loadHtmlMethod == null)
                    {
                        // Try alternative method name
                        loadHtmlMethod = webViewType.GetMethod("LoadHTML", new Type[] { typeof(string) });
                    }
                    if (loadHtmlMethod != null)
                    {
                        SetDiagnosticMessage("🔄 Loading HTML directly...");
                        Debug.LogError("[WebViewManager] 🔄 Loading HTML directly via LoadHtml()");
                        LogDebug("WebViewManager: Loading HTML directly via LoadHtml()");
                        
                        loadHtmlMethod.Invoke(actualWebView, new object[] { htmlContent });
                        
                        SetDiagnosticMessage("✅ HTML loaded!\n(Note: Editor shows mock view)");
                        Debug.LogError("[WebViewManager] ✅ LoadHtml called successfully");
                        
                        // In editor, the mock webview won't actually render, but LoadHtml was called
                        #if UNITY_EDITOR
                        Debug.LogError("[WebViewManager] ⚠️ Editor mock webview - content won't render. Build to device to see actual webview.");
                        #endif
                        
                        Invoke(nameof(ClearDiagnostic), 5f);
                    }
                    else
                    {
                        SetDiagnosticMessage("❌ LoadHtml method not found");
                        Debug.LogError("[WebViewManager] ❌ LoadHtml method not found");
                    }
                }
                else
                {
                    SetDiagnosticMessage($"❌ HTML file not found:\n{htmlPath}");
                    Debug.LogError($"[WebViewManager] ❌ HTML file not found at: {htmlPath}");
                }
            }
            catch (Exception e)
            {
                SetDiagnosticMessage($"❌ Error loading HTML:\n{e.Message}");
                Debug.LogError($"[WebViewManager] ❌ Error loading HTML directly: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Creates a test display when WebView is not available
        /// </summary>
        private void CreateTestDisplay()
        {
            if (webViewDisplayQuad == null)
            {
                // Try to find Quad in children
                webViewDisplayQuad = transform.Find("Quad")?.gameObject;
            }

            if (webViewDisplayQuad != null)
            {
                // Create a simple text display for testing
                GameObject textObj = new GameObject("TestDisplay");
                textObj.transform.SetParent(webViewDisplayQuad.transform);
                textObj.transform.localPosition = Vector3.zero;
                textObj.transform.localRotation = Quaternion.identity;
                textObj.transform.localScale = Vector3.one * 0.01f;

                testDisplayText = textObj.AddComponent<TextMeshPro>();
                testDisplayText.text = "WebView Not Found\nCheck Console\nfor details";
                testDisplayText.fontSize = 36;
                testDisplayText.color = Color.red; // Red text to indicate error
                testDisplayText.alignment = TextAlignmentOptions.Center;

                // Set white background on Quad
                Renderer quadRenderer = webViewDisplayQuad.GetComponent<Renderer>();
                if (quadRenderer != null)
                {
                    Material whiteMat = new Material(Shader.Find("Standard"));
                    whiteMat.color = Color.white;
                    quadRenderer.material = whiteMat;
                }

                Debug.LogError("[WebViewManager] ⚠️ Created test display - WebView component not found!");
                LogDebug("WebViewManager: Created test display");
            }
            else
            {
                Debug.LogError("[WebViewManager] ❌ Cannot create test display - webViewDisplayQuad is null!");
            }
        }

        /// <summary>
        /// Called when slider value changes
        /// </summary>
        private void OnSliderValueChanged(float value)
        {
            SendSliderValueToWebView(value);
        }

        /// <summary>
        /// Sends slider value to WebView
        /// </summary>
        private void SendSliderValueToWebView(float normalizedValue)
        {
            // Update test display if WebView not available
            if (testDisplayText != null && !isShowingJSON)
            {
                testDisplayText.text = normalizedValue.ToString("F2");
                return;
            }

            // Send to actual WebView
            if (webViewComponent == null) return;

            try
            {
                // Calculate current time from normalized value
                float currentTime = normalizedValue * totalDuration;

                // Debug: Log occasionally to verify values
                if (UnityEngine.Random.value < 0.05f) // 5% sample
                {
                    Debug.Log($"📤 Unity Slider → WebView: normalized={normalizedValue:F3}, currentTime={currentTime:F3}s, totalDuration={totalDuration:F2}s");
                }

                // Create message JSON with both normalized value and current time
                // Use InvariantCulture to ensure decimal points (not commas) in JSON
                string message = $"{{\"type\":\"sliderValue\",\"value\":{normalizedValue.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"currentTime\":{currentTime.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"totalDuration\":{totalDuration.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}";

                // For Vuplex, we need to get the WebView property first
                System.Type webViewType = webViewComponent.GetType();
                UnityEngine.Object actualWebView = webViewComponent;
                
                // Get the WebView property (for CanvasWebViewPrefab)
                var webViewProperty = webViewType.GetProperty("WebView");
                if (webViewProperty != null)
                {
                    actualWebView = webViewProperty.GetValue(webViewComponent) as UnityEngine.Object;
                    if (actualWebView != null)
                    {
                        webViewType = actualWebView.GetType();
                    }
                }
                
                // Use reflection to call PostMessage (Vuplex method)
                var postMessageMethod = webViewType.GetMethod("PostMessage", new Type[] { typeof(string) });
                if (postMessageMethod != null)
                {
                    postMessageMethod.Invoke(actualWebView, new object[] { message });
                }
            }
            catch (Exception e)
            {
                LogDebug($"WebViewManager: Error sending message to WebView: {e.Message}");
            }
        }

        /// <summary>
        /// Called when WebView sends a message to Unity
        /// </summary>
        private void OnWebViewMessage(string message)
        {
            LogDebug($"WebViewManager: Received message from WebView: {message}");
            Debug.LogError($"═══════════════════════════════════════════");
            Debug.LogError($"📨 WebView Message Received: {message}");

            // Parse JSON message
            try
            {
                // Simple JSON parsing (you might want to use a proper JSON library)
                if (message.Contains("\"type\""))
                {
                    // Extract the type field
                    var messageObj = JsonUtility.FromJson<SerializableMessage>(message);
                    Debug.LogError($"📋 Message type: {messageObj.type}");

                    // Get RecordingPlaybackEditor reference
                    var playbackEditor = FindFirstObjectByType<RecordingPlaybackEditor>();

                    if (playbackEditor == null)
                    {
                        Debug.LogError("❌ RecordingPlaybackEditor not found!");
                        return;
                    }

                    // Handle playback control messages
                    switch (messageObj.type)
                    {
                        case "play":
                            Debug.LogError("▶️ Play command received - calling Play()");
                            playbackEditor.Play();
                            break;

                        case "pause":
                            Debug.LogError("⏸️ Pause command received - calling Pause()");
                            playbackEditor.Pause();
                            break;

                        case "stop":
                            Debug.LogError("⏹️ Stop command received - calling StopEditPlayback()");
                            playbackEditor.StopEditPlayback();
                            break;

                        case "updatePlacement":
                            Debug.LogError("📍 Placement update received from timeline editor");
                            HandlePlacementUpdate(message);
                            break;

                        case "putdownTimestampChanged":
                            Debug.LogError("⏱️ PutDown timestamp changed from timeline editor");
                            HandlePutDownTimestampChange(message, playbackEditor);
                            break;

                        default:
                            Debug.LogError($"⚠️ Unknown message type: {messageObj.type}");
                            break;
                    }

                    Debug.LogError($"═══════════════════════════════════════════");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error parsing message: {e.Message}");
                LogDebug($"WebViewManager: Error parsing message: {e.Message}");
            }
        }

        /// <summary>
        /// Handles placement update messages from timeline editor
        /// Updates the visual placement indicator position when PlaceExact is dragged
        /// </summary>
        private void HandlePlacementUpdate(string message)
        {
            try
            {
                var updateMsg = JsonUtility.FromJson<PlacementUpdateMessage>(message);

                Debug.LogError($"📍 Placement Update:");
                Debug.LogError($"   Object: {updateMsg.objectId}");
                Debug.LogError($"   Timestamp: {updateMsg.timestamp:F2}s");
                Debug.LogError($"   Position: ({updateMsg.position.x:F2}, {updateMsg.position.y:F2}, {updateMsg.position.z:F2})");

                // Get VisualCueManager to update placement indicator
                var visualCueManager = FindFirstObjectByType<VRInteractionRecording.VisualCueManager>();
                if (visualCueManager != null)
                {
                    // Find the object by ID
                    var objectStateManager = FindFirstObjectByType<VRInteractionRecording.ObjectStateManager>();
                    if (objectStateManager != null && objectStateManager.InteractableObjects.ContainsKey(updateMsg.objectId))
                    {
                        GameObject targetObject = objectStateManager.InteractableObjects[updateMsg.objectId].gameObject;

                        // Update the green placement indicator position
                        Vector3 newPosition = new Vector3(updateMsg.position.x, updateMsg.position.y, updateMsg.position.z);
                        Quaternion newRotation = new Quaternion(updateMsg.rotation.x, updateMsg.rotation.y, updateMsg.rotation.z, updateMsg.rotation.w);

                        // Use reflection to call UpdatePlacementIndicator if it exists
                        var method = visualCueManager.GetType().GetMethod("UpdatePlacementIndicator",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (method != null)
                        {
                            method.Invoke(visualCueManager, new object[] { targetObject, newPosition, newRotation });
                            Debug.LogError($"✅ Updated placement indicator for {updateMsg.objectId}");
                        }
                        else
                        {
                            Debug.LogError($"⚠️ UpdatePlacementIndicator method not found on VisualCueManager");
                        }
                    }
                    else
                    {
                        Debug.LogError($"❌ Object {updateMsg.objectId} not found in InteractableObjects");
                    }
                }
                else
                {
                    Debug.LogError("❌ VisualCueManager not found!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error handling placement update: {e.Message}");
            }
        }

        private void HandlePutDownTimestampChange(string message, RecordingPlaybackEditor playbackEditor)
        {
            try
            {
                var msg = JsonUtility.FromJson<PutDownTimestampMessage>(message);

                Debug.LogError($"⏱️ PutDown Timestamp Update:");
                Debug.LogError($"   Object: {msg.objectId}");
                Debug.LogError($"   New Timestamp: {msg.timestamp:F3}s");

                // Call public method on RecordingPlaybackEditor to update the green highlight
                var method = playbackEditor.GetType().GetMethod("UpdatePutDownTimestamp",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (method != null)
                {
                    method.Invoke(playbackEditor, new object[] { msg.objectId, msg.timestamp });
                    Debug.LogError($"✅ Called UpdatePutDownTimestamp({msg.objectId}, {msg.timestamp:F3}s)");
                }
                else
                {
                    Debug.LogError("❌ UpdatePutDownTimestamp method not found on RecordingPlaybackEditor");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error handling putdown timestamp change: {e.Message}");
            }
        }

        [System.Serializable]
        private class PutDownTimestampMessage
        {
            public string type;
            public string objectId;
            public float timestamp;
        }

        [System.Serializable]
        private class PlacementUpdateMessage
        {
            public string type;
            public string objectId;
            public float timestamp;
            public Vector3Data position;
            public QuaternionData rotation;
        }

        [System.Serializable]
        private class Vector3Data
        {
            public float x;
            public float y;
            public float z;
        }

        [System.Serializable]
        private class QuaternionData
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }

        /// <summary>
        /// Loads a new URL in the WebView
        /// </summary>
        public void LoadURL(string url)
        {
            customWebViewURL = url;
            autoDetectURL = false;
            if (webViewComponent != null)
            {
                InitializeWebView();
            }
        }

        /// <summary>
        /// Sends a custom message to WebView
        /// </summary>
        public void SendMessageToWebView(string type, object data)
        {
            if (webViewComponent == null) return;

            try
            {
                string jsonData = JsonUtility.ToJson(data);
                string message = $"{{\"type\":\"{type}\",\"data\":{jsonData}}}";

                // For Vuplex, we need to get the WebView property first
                System.Type webViewType = webViewComponent.GetType();
                UnityEngine.Object actualWebView = webViewComponent;
                
                // Get the WebView property (for CanvasWebViewPrefab)
                var webViewProperty = webViewType.GetProperty("WebView");
                if (webViewProperty != null)
                {
                    actualWebView = webViewProperty.GetValue(webViewComponent) as UnityEngine.Object;
                    if (actualWebView != null)
                    {
                        webViewType = actualWebView.GetType();
                    }
                }
                
                var postMessageMethod = webViewType.GetMethod("PostMessage", new Type[] { typeof(string) });
                if (postMessageMethod != null)
                {
                    postMessageMethod.Invoke(actualWebView, new object[] { message });
                }
            }
            catch (Exception e)
            {
                LogDebug($"WebViewManager: Error sending message: {e.Message}");
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[WebViewManager] {message}");
            }
        }

        /// <summary>
        /// Creates a diagnostic text display to show status on screen
        /// </summary>
        private void CreateDiagnosticDisplay()
        {
            if (webViewDisplayQuad == null)
            {
                webViewDisplayQuad = transform.Find("Quad")?.gameObject;
            }

            if (webViewDisplayQuad != null)
            {
                // Create diagnostic text
                GameObject diagObj = new GameObject("DiagnosticDisplay");
                diagObj.transform.SetParent(webViewDisplayQuad.transform);
                diagObj.transform.localPosition = new Vector3(0, 0.3f, -0.01f); // Slightly in front
                diagObj.transform.localRotation = Quaternion.identity;
                diagObj.transform.localScale = Vector3.one * 0.01f;

                diagnosticText = diagObj.AddComponent<TextMeshPro>();
                diagnosticText.text = "Initializing...";
                diagnosticText.fontSize = 24;
                diagnosticText.color = Color.yellow;
                diagnosticText.alignment = TextAlignmentOptions.Center;
            }
        }

        /// <summary>
        /// Updates diagnostic message displayed on screen
        /// </summary>
        private void SetDiagnosticMessage(string message)
        {
            diagnosticMessage = message;
            if (diagnosticText != null)
            {
                diagnosticText.text = message;
            }
            Debug.LogError($"[WebViewManager] {message}");
        }

        /// <summary>
        /// Test method to verify WebView is working - call from Inspector or code
        /// </summary>
        [ContextMenu("Test WebView Load")]
        public void TestWebViewLoad()
        {
            Debug.Log("=== WebViewManager Test ===");
            Debug.Log($"WebView Component: {(webViewComponent != null ? webViewComponent.GetType().Name : "NULL")}");
            Debug.Log($"Auto Detect URL: {autoDetectURL}");
            Debug.Log($"Custom URL: {customWebViewURL}");
            Debug.Log($"Resolved URL: {GetWebViewURL()}");
            Debug.Log($"StreamingAssets Path: {Application.streamingAssetsPath}");
            Debug.Log($"Test Display Text: {(testDisplayText != null ? "EXISTS" : "NULL")}");
            Debug.Log($"Is Showing JSON: {isShowingJSON}");
            
            // Check if StreamingAssets file exists
            string streamingAssetsPath = System.IO.Path.Combine(Application.streamingAssetsPath, "WebContent", "index.html");
            Debug.Log($"StreamingAssets file exists: {System.IO.File.Exists(streamingAssetsPath)}");
            if (System.IO.File.Exists(streamingAssetsPath))
            {
                Debug.Log($"StreamingAssets file path: {streamingAssetsPath}");
            }
            
            if (webViewComponent == null)
            {
                Debug.LogWarning("No WebView component found! Check if Vuplex/3D WebView is installed.");
            }
            else
            {
                Debug.Log("WebView component found - attempting to reload...");
                InitializeWebView();
            }
        }

        /// <summary>
        /// Displays JSON text on the screen
        /// </summary>
        public void DisplayJSON(string jsonText, float duration = 0f, RecordingData recordingData = null)
        {
            Debug.LogError("═══════════════════════════════════════════");
            Debug.LogError("📤 DisplayJSON() CALLED");
            Debug.LogError($"   JSON length: {jsonText?.Length ?? 0}");
            Debug.LogError($"   Duration: {duration}");
            Debug.LogError($"   RecordingData: {(recordingData != null ? $"{recordingData.interactionEvents.Count} events" : "null")}");
            Debug.LogError("════════════════════════════════════════════════════════════");
            Debug.LogError("📥 RECEIVED JSONTEXT PARAMETER:");
            Debug.LogError(jsonText);
            Debug.LogError("════════════════════════════════════════════════════════════");
            Debug.LogError("═══════════════════════════════════════════");

            isShowingJSON = true;
            currentJSON = jsonText;
            totalDuration = duration;

            // Update test display if WebView not available
            if (testDisplayText != null)
            {
                Debug.LogError("⚠️ Using test display (TextMeshPro) instead of WebView");
                // Format JSON for display (wrap text, smaller font)
                testDisplayText.fontSize = 16; // Smaller font for JSON
                testDisplayText.color = Color.black;
                testDisplayText.alignment = TextAlignmentOptions.TopLeft;
                testDisplayText.textWrappingMode = TextWrappingModes.Normal; // Enable word wrapping

                // Truncate if too long (TextMeshPro has limits)
                string displayText = jsonText;
                if (displayText.Length > 2000)
                {
                    displayText = displayText.Substring(0, 2000) + "\n... (truncated)";
                }

                testDisplayText.text = displayText;

                LogDebug("WebViewManager: Displaying JSON in test display");
                Debug.LogError("✅ JSON displayed in TextMeshPro");
                return;
            }

            Debug.LogError($"   testDisplayText is null, using WebView");

            // Send to actual WebView
            if (webViewComponent != null)
            {
                Debug.LogError($"✅ webViewComponent exists");
                Debug.LogError($"   isWebViewReady: {isWebViewReady}");
                Debug.LogError($"   pendingMessages count: {pendingMessages.Count}");

                try
                {
                    // Convert recordingData to JSON string
                    string recordingJSON = "null";
                    if (recordingData != null)
                    {
                        Debug.LogError($"✅ Recording data included: {recordingData.interactionEvents.Count} events");
                        recordingJSON = JsonUtility.ToJson(recordingData);
                        Debug.LogError($"✅ Recording serialized to JSON: {recordingJSON.Length} chars");
                    }
                    else
                    {
                        Debug.LogError("⚠️ No recording data to include");
                    }

                    // JsonUtility.ToJson doesn't properly serialize strings containing JSON
                    // So we manually construct the JSON with escaped strings
                    string escapedTaskJSON = jsonText.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
                    string escapedRecordingJSON = recordingJSON.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

                    string message = $"{{\"type\":\"loadTimelineData\",\"taskJSON\":\"{escapedTaskJSON}\",\"recordingJSON\":\"{escapedRecordingJSON}\",\"totalDuration\":{duration.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}";

                    Debug.LogError($"✅ Message created: {message.Length} chars");

                    // COPY THE EXACT PATTERN FROM SendSliderValueToWebView (which works!)
                    try
                    {
                        System.Type webViewType = webViewComponent.GetType();
                        UnityEngine.Object actualWebView = webViewComponent;

                        // Get the WebView property (for CanvasWebViewPrefab)
                        var webViewProperty = webViewType.GetProperty("WebView");
                        if (webViewProperty != null)
                        {
                            actualWebView = webViewProperty.GetValue(webViewComponent) as UnityEngine.Object;
                            if (actualWebView != null)
                            {
                                webViewType = actualWebView.GetType();
                            }
                        }

                        // Use reflection to call PostMessage (Vuplex method) - SAME AS SLIDER
                        var postMessageMethod = webViewType.GetMethod("PostMessage", new Type[] { typeof(string) });
                        if (postMessageMethod != null)
                        {
                            Debug.LogError("✅ Calling PostMessage (same as slider)...");
                            postMessageMethod.Invoke(actualWebView, new object[] { message });
                            Debug.LogError("✅ PostMessage SUCCESS!");
                        }
                        else
                        {
                            Debug.LogError("❌ PostMessage method not found!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ Error sending JSON: {ex.Message}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ ERROR in DisplayJSON: {e.Message}");
                    Debug.LogError($"   Stack: {e.StackTrace}");
                    LogDebug($"WebViewManager: Error sending JSON to WebView: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("❌ webViewComponent is NULL!");
            }

            Debug.LogError("═══════════════════════════════════════════");
        }

        /// <summary>
        /// Formats JSON string for display (wraps long lines)
        /// </summary>
        private string FormatJSONForDisplay(string json)
        {
            // Return JSON as-is - the HTML will format it with JSON.stringify
            // This is just for the test display (TextMeshPro)
            return json;
        }

        /// <summary>
        /// Clears JSON display and returns to slider display
        /// </summary>
        public void ClearJSONDisplay()
        {
            isShowingJSON = false;
            currentJSON = "";

            if (testDisplayText != null)
            {
                testDisplayText.text = "0.00";
                testDisplayText.fontSize = 72;
                testDisplayText.color = Color.black;
                testDisplayText.alignment = TextAlignmentOptions.Center;
            }
        }

        /// <summary>
        /// Clears diagnostic message after delay
        /// </summary>
        private void ClearDiagnostic()
        {
            if (diagnosticText != null)
            {
                diagnosticText.text = "";
            }
        }

        /// <summary>
        /// Marks WebView as ready and processes pending messages
        /// IMPORTANT: Must wait for page load to finish before sending messages
        /// </summary>
        private async void MarkWebViewReady()
        {
            Debug.LogError("═══════════════════════════════════════════");
            Debug.LogError($"⏳ MarkWebViewReady() CALLED at Time: {Time.time:F2}s");
            Debug.LogError($"   Pending messages in queue: {pendingMessages.Count}");

            // CRITICAL: Must wait for page to finish loading before sending PostMessage
            // Otherwise messages are lost!
            try
            {
                System.Type webViewType = webViewComponent.GetType();
                var webViewProperty = webViewType.GetProperty("WebView");
                if (webViewProperty != null)
                {
                    UnityEngine.Object actualWebView = webViewProperty.GetValue(webViewComponent) as UnityEngine.Object;
                    if (actualWebView != null)
                    {
                        System.Type actualWebViewType = actualWebView.GetType();
                        var waitForNextPageMethod = actualWebViewType.GetMethod("WaitForNextPageLoadToFinish");
                        if (waitForNextPageMethod != null)
                        {
                            Debug.LogError($"✅ Found WaitForNextPageLoadToFinish - waiting... (Time: {Time.time:F2}s)");
                            var task = waitForNextPageMethod.Invoke(actualWebView, null) as System.Threading.Tasks.Task;
                            if (task != null)
                            {
                                await task;
                                Debug.LogError($"✅ Page load finished! Now ready to send messages (Time: {Time.time:F2}s)");
                            }
                            else
                            {
                                Debug.LogError("⚠️ WaitForNextPageLoadToFinish returned null task");
                            }
                        }
                        else
                        {
                            Debug.LogError("⚠️ WaitForNextPageLoadToFinish not found - using delay fallback");
                            // Add a 1 second delay as fallback
                            await System.Threading.Tasks.Task.Delay(1000);
                        }
                    }
                    else
                    {
                        Debug.LogError("❌ actualWebView is null in MarkWebViewReady");
                    }
                }
                else
                {
                    Debug.LogError("⚠️ WebView property not found - no page load wait");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error waiting for page load: {e.Message}");
                Debug.LogError($"   Stack: {e.StackTrace}");
            }

            isWebViewReady = true;
            Debug.LogError($"✅✅✅ WebView NOW READY at Time: {Time.time:F2}s ✅✅✅");
            Debug.LogError($"   Processing {pendingMessages.Count} pending messages...");
            ProcessPendingMessages();
            Debug.LogError($"   Pending messages after processing: {pendingMessages.Count}");
            LogDebug("WebViewManager: WebView marked as ready");
            Debug.LogError("═══════════════════════════════════════════");
        }

        /// <summary>
        /// Processes all pending messages in the queue
        /// </summary>
        private void ProcessPendingMessages()
        {
            if (!isWebViewReady || webViewComponent == null)
            {
                Debug.LogError($"⚠️ ProcessPendingMessages called but cannot process: isWebViewReady={isWebViewReady}, webViewComponent={(webViewComponent != null ? "exists" : "null")}");
                return;
            }

            Debug.LogError($"🔄 ProcessPendingMessages: Processing {pendingMessages.Count} messages...");

            int processedCount = 0;
            int totalToProcess = pendingMessages.Count;

            // First, send a status update that we're processing the queue
            if (totalToProcess > 0)
            {
                SendStatusUpdate($"🔄 Unity: Processing {totalToProcess} queued message(s)...");
            }

            while (pendingMessages.Count > 0)
            {
                string message = pendingMessages.Dequeue();
                Debug.LogError($"   📤 Processing queued message {++processedCount}/{totalToProcess} (remaining: {pendingMessages.Count})");
                SendMessageToWebViewInternal(message);
                LogDebug($"WebViewManager: Processed queued message (remaining: {pendingMessages.Count})");
            }

            Debug.LogError($"✅ ProcessPendingMessages complete: {processedCount} messages sent");

            if (processedCount > 0)
            {
                SendStatusUpdate($"✅ Unity: Sent {processedCount} queued message(s)!");
            }
        }

        /// <summary>
        /// Sends a simple status update message to the webpage
        /// </summary>
        private void SendStatusUpdate(string statusMessage)
        {
            if (webViewComponent == null || !isWebViewReady) return;

            try
            {
                string statusJson = $"{{\"type\":\"statusUpdate\",\"message\":\"{statusMessage.Replace("\"", "\\\"")}\",\"time\":{Time.time}}}";
                SendMessageToWebViewInternal(statusJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending status update: {e.Message}");
            }
        }

        /// <summary>
        /// Internal method to send a message to WebView
        /// </summary>
        private void SendMessageToWebViewInternal(string message)
        {
            Debug.LogError("───────────────────────────────────────────");
            Debug.LogError("🚀 SendMessageToWebViewInternal() CALLED");
            Debug.LogError($"   Message length: {message?.Length ?? 0} chars");

            if (webViewComponent == null)
            {
                Debug.LogError("❌ webViewComponent is NULL!");
                LogDebug("WebViewManager: Cannot send message - WebView component is null");
                return;
            }

            Debug.LogError($"✅ webViewComponent exists: {webViewComponent.GetType().Name}");

            try
            {
                // For Vuplex, we need to get the WebView property first
                System.Type webViewType = webViewComponent.GetType();
                Debug.LogError($"   Component type: {webViewType.Name}");
                UnityEngine.Object actualWebView = webViewComponent;

                // Get the WebView property (for CanvasWebViewPrefab)
                var webViewProperty = webViewType.GetProperty("WebView");
                if (webViewProperty != null)
                {
                    Debug.LogError("✅ Found 'WebView' property");
                    actualWebView = webViewProperty.GetValue(webViewComponent) as UnityEngine.Object;
                    if (actualWebView != null)
                    {
                        webViewType = actualWebView.GetType();
                        Debug.LogError($"✅ Got actual WebView: {webViewType.Name}");
                    }
                    else
                    {
                        Debug.LogError("❌ WebView property is NULL - not initialized yet!");
                        LogDebug("WebViewManager: WebView property is null - cannot send message. WebView may not be initialized yet.");
                        // Queue the message for retry
                        pendingMessages.Enqueue(message);
                        return;
                    }
                }
                else
                {
                    Debug.LogError("⚠️ No 'WebView' property - using component directly");
                }

                // PostMessage is on the actual WebView instance
                var postMessageMethod = webViewType.GetMethod("PostMessage", new Type[] { typeof(string) });
                if (postMessageMethod != null)
                {
                    Debug.LogError("✅ Found PostMessage method - invoking...");
                    postMessageMethod.Invoke(actualWebView, new object[] { message });
                    Debug.LogError("✅ PostMessage invoked successfully!");
                    Debug.LogError($"📤 Message sent: {message.Substring(0, Mathf.Min(150, message.Length))}...");
                    LogDebug($"WebViewManager: Message sent to WebView ({message.Length} chars): {message.Substring(0, Mathf.Min(100, message.Length))}...");
                }
                else
                {
                    Debug.LogError($"❌ PostMessage method NOT FOUND on type: {webViewType.Name}");
                    LogDebug("WebViewManager: PostMessage method not found on WebView type: " + webViewType.Name);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ EXCEPTION in SendMessageToWebViewInternal:");
                Debug.LogError($"   {e.Message}");
                Debug.LogError($"   {e.StackTrace}");
                LogDebug($"WebViewManager: Error sending message to WebView: {e.Message}\n{e.StackTrace}");
            }

            Debug.LogError("───────────────────────────────────────────");
        }

        /// <summary>
        /// Retries WebView initialization if it failed the first time
        /// </summary>
        private void RetryWebViewInitialization()
        {
            if (webViewComponent != null)
            {
                LogDebug("WebViewManager: Retrying WebView initialization...");
                InitializeWebView();
            }
        }

        private void OnDestroy()
        {
            if (timelineSlider != null)
            {
                timelineSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
        }
    }
}

