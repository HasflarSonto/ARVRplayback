using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace VRInteractionRecording
{
    /// <summary>
    /// Quick diagnostic tool to check WebView interaction setup
    /// Attach to any GameObject and run "Run Diagnostics" from context menu
    /// </summary>
    public class WebViewDiagnostic : MonoBehaviour
    {
        [ContextMenu("Run Diagnostics")]
        public void RunDiagnostics()
        {
            Debug.Log("╔═══════════════════════════════════════════════╗");
            Debug.Log("║   WEBVIEW INTERACTION DIAGNOSTIC              ║");
            Debug.Log("╚═══════════════════════════════════════════════╝");
            Debug.Log("");

            CheckEventSystem();
            CheckCanvases();
            CheckWebViews();
            CheckRayInteractors();

            Debug.Log("");
            Debug.Log("╔═══════════════════════════════════════════════╗");
            Debug.Log("║   END DIAGNOSTIC                              ║");
            Debug.Log("╚═══════════════════════════════════════════════╝");
        }

        void CheckEventSystem()
        {
            Debug.Log("┌─ 1. EVENT SYSTEM CHECK ─────────────────────┐");

            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogError("  ❌ NO EventSystem found in scene!");
                Debug.LogError("     → Add an EventSystem GameObject");
            }
            else
            {
                Debug.Log($"  ✅ EventSystem found: {eventSystem.name}");

                // Check for XRUIInputModule
                var xrInputModuleType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
                if (xrInputModuleType != null)
                {
                    var xrModule = eventSystem.GetComponent(xrInputModuleType);
                    if (xrModule != null)
                    {
                        Debug.Log("  ✅ XRUIInputModule present");
                    }
                    else
                    {
                        Debug.LogError("  ❌ XRUIInputModule MISSING!");
                        Debug.LogError("     → Add XRUIInputModule component to EventSystem");
                    }
                }

                // Check for conflicting StandaloneInputModule
                var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (standaloneModule != null)
                {
                    Debug.LogWarning("  ⚠️ StandaloneInputModule found (conflicts with XR)");
                    Debug.LogWarning("     → Remove StandaloneInputModule");
                }
            }

            Debug.Log("└──────────────────────────────────────────────┘");
            Debug.Log("");
        }

        void CheckCanvases()
        {
            Debug.Log("┌─ 2. CANVAS CHECK ───────────────────────────┐");

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"  Found {canvases.Length} Canvas(es)");
            Debug.Log("");

            foreach (Canvas canvas in canvases)
            {
                Debug.Log($"  📊 Canvas: {canvas.name}");
                Debug.Log($"     Render Mode: {canvas.renderMode}");
                Debug.Log($"     Sorting Order: {canvas.sortingOrder}");

                // Check Event Camera
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
                {
                    if (canvas.worldCamera != null)
                    {
                        Debug.Log($"     ✅ Event Camera: {canvas.worldCamera.name}");
                    }
                    else
                    {
                        Debug.LogError($"     ❌ Event Camera: NOT SET!");
                        Debug.LogError($"        → Set to Main Camera");
                    }
                }

                // Check for TrackedDeviceGraphicRaycaster
                var trackedRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
                if (trackedRaycasterType != null)
                {
                    var trackedRaycaster = canvas.GetComponent(trackedRaycasterType);
                    if (trackedRaycaster != null)
                    {
                        Debug.Log("     ✅ TrackedDeviceGraphicRaycaster present");
                    }
                    else
                    {
                        Debug.LogError("     ❌ TrackedDeviceGraphicRaycaster MISSING!");
                        Debug.LogError("        → Add TrackedDeviceGraphicRaycaster component");

                        // Check if regular raycaster exists instead
                        var regularRaycaster = canvas.GetComponent<GraphicRaycaster>();
                        if (regularRaycaster != null)
                        {
                            Debug.LogWarning("     ⚠️ Has GraphicRaycaster (not VR-compatible)");
                            Debug.LogWarning("        → Replace with TrackedDeviceGraphicRaycaster");
                        }
                    }
                }

                Debug.Log("");
            }

            Debug.Log("└──────────────────────────────────────────────┘");
            Debug.Log("");
        }

        void CheckWebViews()
        {
            Debug.Log("┌─ 3. WEBVIEW CHECK ──────────────────────────┐");

            // Check for CanvasWebViewPrefab
            var vuplexType = System.Type.GetType("Vuplex.WebView.CanvasWebViewPrefab, Vuplex.WebView");
            if (vuplexType != null)
            {
                var webViews = FindObjectsByType(vuplexType, FindObjectsInactive.Include, FindObjectsSortMode.None);
                Debug.Log($"  Found {webViews.Length} CanvasWebViewPrefab(s)");
                Debug.Log("");

                foreach (var webView in webViews)
                {
                    var mb = webView as MonoBehaviour;
                    if (mb != null)
                    {
                        Debug.Log($"  🌐 WebView: {mb.gameObject.name}");
                        Debug.Log($"     Active: {mb.gameObject.activeInHierarchy}");

                        // Check if it's in a Canvas
                        Canvas parentCanvas = mb.GetComponentInParent<Canvas>();
                        if (parentCanvas != null)
                        {
                            Debug.Log($"     ✅ Parent Canvas: {parentCanvas.name}");
                        }
                        else
                        {
                            Debug.LogError($"     ❌ NO parent Canvas!");
                            Debug.LogError($"        → WebView must be child of Canvas");
                        }

                        // Check own Canvas
                        Canvas ownCanvas = mb.GetComponent<Canvas>();
                        if (ownCanvas == null)
                        {
                            ownCanvas = mb.GetComponentInChildren<Canvas>();
                        }
                        if (ownCanvas != null)
                        {
                            Debug.Log($"     Own Canvas sorting order: {ownCanvas.sortingOrder}");

                            if (parentCanvas != null && ownCanvas.sortingOrder <= parentCanvas.sortingOrder)
                            {
                                Debug.LogWarning($"     ⚠️ WebView sorting order ({ownCanvas.sortingOrder}) <= Parent ({parentCanvas.sortingOrder})");
                                Debug.LogWarning($"        → WebView won't receive clicks! Increase to {parentCanvas.sortingOrder + 1}");
                            }
                        }

                        Debug.Log("");
                    }
                }
            }
            else
            {
                Debug.LogWarning("  ⚠️ Vuplex WebView package not found");
            }

            Debug.Log("└──────────────────────────────────────────────┘");
            Debug.Log("");
        }

        void CheckRayInteractors()
        {
            Debug.Log("┌─ 4. RAY INTERACTOR CHECK ───────────────────┐");

            var rayInteractorType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRRayInteractor, Unity.XR.Interaction.Toolkit");
            if (rayInteractorType != null)
            {
                var rayInteractors = FindObjectsByType(rayInteractorType, FindObjectsInactive.Include, FindObjectsSortMode.None);
                Debug.Log($"  Found {rayInteractors.Length} XR Ray Interactor(s)");
                Debug.Log("");

                foreach (var interactor in rayInteractors)
                {
                    var mb = interactor as MonoBehaviour;
                    if (mb != null)
                    {
                        Debug.Log($"  👉 Ray Interactor: {mb.name}");
                        Debug.Log($"     Active: {mb.gameObject.activeInHierarchy}");
                        Debug.Log($"     Enabled: {mb.enabled}");

                        var maxDistanceProp = rayInteractorType.GetProperty("maxRaycastDistance");
                        if (maxDistanceProp != null)
                        {
                            float maxDist = (float)maxDistanceProp.GetValue(interactor);
                            Debug.Log($"     Max Raycast Distance: {maxDist}");

                            if (maxDist < 5f)
                            {
                                Debug.LogWarning($"     ⚠️ Distance very short - may not reach UI");
                            }
                        }

                        Debug.Log("");
                    }
                }
            }
            else
            {
                Debug.LogWarning("  ⚠️ XR Interaction Toolkit not found");
            }

            Debug.Log("└──────────────────────────────────────────────┘");
        }
    }
}
