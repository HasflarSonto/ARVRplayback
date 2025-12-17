using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRInteractionRecording
{
    /// <summary>
    /// Manages edit mode playback with timeline controls (like a video player)
    /// Shows recorded interactions with player model (headset/controllers) and allows scrubbing
    /// Includes visual annotations: red highlights for upcoming grabs, path lines, and green end positions
    /// </summary>
    public class RecordingPlaybackEditor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        [Tooltip("Reference to InteractionRecordingManager")]
        private InteractionRecordingManager recordingManager;

        [SerializeField]
        [Tooltip("Reference to ObjectStateManager")]
        private ObjectStateManager objectStateManager;

        [SerializeField]
        [Tooltip("Reference to VisualCueManager for highlighting")]
        private VisualCueManager visualCueManager;

        [Header("UI Elements")]
        [SerializeField]
        [Tooltip("Timeline slider for scrubbing through recording")]
        private Slider timelineSlider;

        [SerializeField]
        [Tooltip("Play/Pause button")]
        private Button playPauseButton;

        [SerializeField]
        [Tooltip("Text to display current time / total time")]
        private TextMeshProUGUI timeDisplayText;

        [SerializeField]
        [Tooltip("Text on play/pause button")]
        private TextMeshProUGUI playPauseButtonText;

        [Header("Player Model")]
        [SerializeField]
        [Tooltip("Container GameObject for player model (headset and controllers)")]
        private GameObject playerModelContainer;

        [SerializeField]
        [Tooltip("GameObject representing headset (will be created if null)")]
        private GameObject headsetModel;

        [SerializeField]
        [Tooltip("GameObject representing left controller (will be created if null)")]
        private GameObject leftControllerModel;

        [SerializeField]
        [Tooltip("GameObject representing right controller (will be created if null)")]
        private GameObject rightControllerModel;

        [Header("Settings")]
        [SerializeField]
        [Tooltip("Playback speed multiplier")]
        private float playbackSpeed = 1f;

        [SerializeField]
        [Tooltip("Time before grab event to start highlighting object (in seconds)")]
        private float highlightPreviewTime = 1f;

        [SerializeField]
        [Tooltip("Material for path lines (dotted line effect)")]
        private Material pathLineMaterial;

        [SerializeField]
        [Tooltip("Container for timeline markers (will be created if null)")]
        private RectTransform timelineMarkersContainer;

        [SerializeField]
        [Tooltip("Prefab or template for grab event markers (optional - will create simple image if null)")]
        private GameObject grabMarkerPrefab;

        [SerializeField]
        [Tooltip("Prefab or template for release event markers (optional - will create simple image if null)")]
        private GameObject releaseMarkerPrefab;

        [SerializeField]
        [Tooltip("Color for grab event markers")]
        private Color grabMarkerColor = new Color(1f, 0f, 0f, 0.8f); // Red

        [SerializeField]
        [Tooltip("Color for release event markers")]
        private Color releaseMarkerColor = new Color(0f, 1f, 0f, 0.8f); // Green

        [SerializeField]
        [Tooltip("Size of timeline markers")]
        private Vector2 markerSize = new Vector2(4f, 20f);

        private RecordingData currentRecording;
        private bool isPlaying = false;
        private float currentPlaybackTime = 0f;
        private bool isScrubbing = false;

        // Cached data for efficient lookup
        private Dictionary<string, List<TransformSnapshot>> objectSnapshotsByTime = new Dictionary<string, List<TransformSnapshot>>();
        private List<PlayerPoseSnapshot> sortedPlayerSnapshots = new List<PlayerPoseSnapshot>();
        private List<InteractionSequence> interactionSequences = new List<InteractionSequence>(); // Grab-release pairs

        // Visual annotations
        private Dictionary<string, LineRenderer> pathLines = new Dictionary<string, LineRenderer>(); // Path lines for objects
        private Dictionary<string, GameObject> endPositionHighlights = new Dictionary<string, GameObject>(); // Green highlights at end positions
        
        // Physics state management (to freeze objects)
        private Dictionary<GameObject, Rigidbody> objectRigidbodies = new Dictionary<GameObject, Rigidbody>();
        private Dictionary<GameObject, bool> originalKinematicStates = new Dictionary<GameObject, bool>();

        // Timeline markers
        private List<GameObject> timelineMarkers = new List<GameObject>(); // All marker GameObjects

        private void Start()
        {
            if (recordingManager == null)
            {
                recordingManager = FindFirstObjectByType<InteractionRecordingManager>();
            }

            if (objectStateManager == null)
            {
                objectStateManager = FindFirstObjectByType<ObjectStateManager>();
            }

            if (visualCueManager == null)
            {
                visualCueManager = FindFirstObjectByType<VisualCueManager>();
            }

            // Setup UI
            if (timelineSlider != null)
            {
                timelineSlider.onValueChanged.AddListener(OnTimelineValueChanged);
                timelineSlider.minValue = 0f;
                timelineSlider.maxValue = 1f;
            }

            if (playPauseButton != null)
            {
                playPauseButton.onClick.AddListener(TogglePlayPause);
            }

            // Create player models if they don't exist
            CreatePlayerModelsIfNeeded();
        }

        private void Update()
        {
            if (isPlaying && currentRecording != null && !isScrubbing)
            {
                currentPlaybackTime += Time.deltaTime * playbackSpeed;

                if (currentPlaybackTime >= currentRecording.recordingDuration)
                {
                    currentPlaybackTime = currentRecording.recordingDuration;
                    Pause();
                }

                UpdatePlayback(currentPlaybackTime);
                UpdateUI();
            }
        }

        /// <summary>
        /// Starts edit mode playback with the given recording
        /// </summary>
        public void StartEditPlayback(RecordingData recording)
        {
            if (recording == null)
            {
                Debug.LogError("RecordingPlaybackEditor: Cannot start playback - recording is null!");
                return;
            }

            // Clean up any previous state first
            StopEditPlayback();
            
            currentRecording = recording;
            currentPlaybackTime = 0f;
            isPlaying = false;
            
            // Clear any old cached data
            objectSnapshotsByTime.Clear();
            sortedPlayerSnapshots.Clear();
            interactionSequences.Clear();

            // Organize snapshots for efficient lookup
            OrganizeSnapshots();

            // Build interaction sequences
            BuildInteractionSequences();

            // Reset all objects to initial states
            ResetObjectsToInitialStates();

            // Start with objects frozen (paused state)
            FreezeAllObjects();

            // Clear all visual annotations initially
            ClearVisualAnnotations();

            // Create timeline markers
            CreateTimelineMarkers();

            // Update UI
            UpdateUI();

            // Show player models
            if (playerModelContainer != null)
            {
                playerModelContainer.SetActive(true);
            }

            Debug.Log($"RecordingPlaybackEditor: Edit playback ready. Duration: {recording.recordingDuration:F2}s");
        }

        /// <summary>
        /// Stops edit mode playback
        /// </summary>
        public void StopEditPlayback()
        {
            Debug.LogError("═══════════════════════════════════════════");
            Debug.LogError("🛑 StopEditPlayback() CALLED");
            Debug.LogError("═══════════════════════════════════════════");

            isPlaying = false;
            currentPlaybackTime = 0f;
            currentRecording = null;

            // Hide player models
            if (playerModelContainer != null)
            {
                playerModelContainer.SetActive(false);
                Debug.LogError("✅ Player models hidden");
            }

            // Clear visual annotations
            ClearVisualAnnotations();
            Debug.LogError("✅ Visual annotations cleared");

            // Clear timeline markers
            ClearTimelineMarkers();
            Debug.LogError("✅ Timeline markers cleared");

            // Unfreeze objects
            Debug.LogError("🔓 Calling UnfreezeAllObjects()...");
            UnfreezeAllObjects();
            Debug.LogError("✅ UnfreezeAllObjects() complete");

            // Reset objects
            if (objectStateManager != null)
            {
                Debug.LogError("🔄 Calling objectStateManager.ResetAllObjects()...");
                objectStateManager.ResetAllObjects();
                Debug.LogError("✅ Objects reset");
            }

            Debug.LogError("═══════════════════════════════════════════");
        }

        /// <summary>
        /// Toggles play/pause
        /// </summary>
        public void TogglePlayPause()
        {
            if (currentRecording == null) return;

            if (isPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        /// <summary>
        /// Starts playback
        /// </summary>
        public void Play()
        {
            if (currentRecording == null) return;

            isPlaying = true;
            
            // Unfreeze objects when playing (gravity back on)
            UnfreezeAllObjects();
            
            if (playPauseButtonText != null)
            {
                playPauseButtonText.text = "Pause";
            }
        }

        /// <summary>
        /// Pauses playback
        /// </summary>
        public void Pause()
        {
            isPlaying = false;
            
            // Freeze objects when paused (gravity off)
            FreezeAllObjects();
            
            if (playPauseButtonText != null)
            {
                playPauseButtonText.text = "Play";
            }
        }

        /// <summary>
        /// Called when timeline slider value changes
        /// </summary>
        private void OnTimelineValueChanged(float value)
        {
            if (currentRecording == null) return;

            isScrubbing = true;
            currentPlaybackTime = value * currentRecording.recordingDuration;
            UpdatePlayback(currentPlaybackTime);
            UpdateUI();
            isScrubbing = false;
            
            // When scrubbing, pause playback and freeze objects
            if (isPlaying)
            {
                Pause();
            }
            else
            {
                // Already paused, ensure objects are frozen
                FreezeAllObjects();
            }
        }

        /// <summary>
        /// Updates playback to the specified time
        /// </summary>
        private void UpdatePlayback(float time)
        {
            if (currentRecording == null) return;

            // Update object positions
            UpdateObjectPositions(time);

            // Update player model positions
            UpdatePlayerModelPositions(time);

            // Update visual annotations (highlights, etc.)
            UpdateVisualAnnotations(time);
        }

        /// <summary>
        /// Updates object positions based on recorded snapshots
        /// </summary>
        private void UpdateObjectPositions(float time)
        {
            foreach (var kvp in objectSnapshotsByTime)
            {
                string objectId = kvp.Key;
                List<TransformSnapshot> snapshots = kvp.Value;

                // Find the two snapshots to interpolate between
                TransformSnapshot before = null;
                TransformSnapshot after = null;

                for (int i = 0; i < snapshots.Count; i++)
                {
                    if (snapshots[i].timestamp <= time)
                    {
                        before = snapshots[i];
                    }
                    if (snapshots[i].timestamp >= time && after == null)
                    {
                        after = snapshots[i];
                        break;
                    }
                }

                GameObject obj = objectStateManager.GetObjectFromId(objectId);
                if (obj == null) continue;

                if (before != null && after != null && before.timestamp != after.timestamp)
                {
                    // Interpolate between snapshots
                    float t = (time - before.timestamp) / (after.timestamp - before.timestamp);
                    obj.transform.position = Vector3.Lerp(before.position, after.position, t);
                    obj.transform.rotation = Quaternion.Lerp(before.rotation, after.rotation, t);
                    obj.transform.localScale = Vector3.Lerp(before.scale, after.scale, t);
                }
                else if (before != null)
                {
                    // Use exact snapshot
                    obj.transform.position = before.position;
                    obj.transform.rotation = before.rotation;
                    obj.transform.localScale = before.scale;
                }

                // Ensure object stays frozen (kinematic) during playback
                if (objectRigidbodies.ContainsKey(obj))
                {
                    Rigidbody rb = objectRigidbodies[obj];
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }
        }

        /// <summary>
        /// Updates player model positions (headset and controllers)
        /// </summary>
        private void UpdatePlayerModelPositions(float time)
        {
            if (sortedPlayerSnapshots.Count == 0) return;

            // Find the two snapshots to interpolate between
            PlayerPoseSnapshot before = null;
            PlayerPoseSnapshot after = null;

            for (int i = 0; i < sortedPlayerSnapshots.Count; i++)
            {
                if (sortedPlayerSnapshots[i].timestamp <= time)
                {
                    before = sortedPlayerSnapshots[i];
                }
                if (sortedPlayerSnapshots[i].timestamp >= time && after == null)
                {
                    after = sortedPlayerSnapshots[i];
                    break;
                }
            }

            if (before != null && after != null && before.timestamp != after.timestamp)
            {
                // Interpolate between snapshots
                float t = (time - before.timestamp) / (after.timestamp - before.timestamp);

                // Update headset
                if (headsetModel != null)
                {
                    headsetModel.transform.position = Vector3.Lerp(before.headsetPosition, after.headsetPosition, t);
                    headsetModel.transform.rotation = Quaternion.Lerp(before.headsetRotation, after.headsetRotation, t);
                }

                // Update left controller
                if (leftControllerModel != null)
                {
                    leftControllerModel.transform.position = Vector3.Lerp(before.leftControllerPosition, after.leftControllerPosition, t);
                    leftControllerModel.transform.rotation = Quaternion.Lerp(before.leftControllerRotation, after.leftControllerRotation, t);
                }

                // Update right controller
                if (rightControllerModel != null)
                {
                    rightControllerModel.transform.position = Vector3.Lerp(before.rightControllerPosition, after.rightControllerPosition, t);
                    rightControllerModel.transform.rotation = Quaternion.Lerp(before.rightControllerRotation, after.rightControllerRotation, t);
                }
            }
            else if (before != null)
            {
                // Use exact snapshot
                if (headsetModel != null)
                {
                    headsetModel.transform.position = before.headsetPosition;
                    headsetModel.transform.rotation = before.headsetRotation;
                }
                if (leftControllerModel != null)
                {
                    leftControllerModel.transform.position = before.leftControllerPosition;
                    leftControllerModel.transform.rotation = before.leftControllerRotation;
                }
                if (rightControllerModel != null)
                {
                    rightControllerModel.transform.position = before.rightControllerPosition;
                    rightControllerModel.transform.rotation = before.rightControllerRotation;
                }
            }
        }

        /// <summary>
        /// Organizes snapshots for efficient time-based lookup
        /// </summary>
        private void OrganizeSnapshots()
        {
            objectSnapshotsByTime.Clear();
            sortedPlayerSnapshots.Clear();

            if (currentRecording == null) return;

            // Organize object snapshots by object ID
            foreach (TransformSnapshot snapshot in currentRecording.transformSnapshots)
            {
                if (!objectSnapshotsByTime.ContainsKey(snapshot.objectId))
                {
                    objectSnapshotsByTime[snapshot.objectId] = new List<TransformSnapshot>();
                }
                objectSnapshotsByTime[snapshot.objectId].Add(snapshot);
            }

            // Sort snapshots by timestamp for each object
            foreach (var kvp in objectSnapshotsByTime)
            {
                kvp.Value.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
            }

            // Sort player pose snapshots
            sortedPlayerSnapshots = new List<PlayerPoseSnapshot>(currentRecording.playerPoseSnapshots);
            sortedPlayerSnapshots.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
        }

        /// <summary>
        /// Resets all objects to their initial states
        /// </summary>
        private void ResetObjectsToInitialStates()
        {
            if (currentRecording == null || objectStateManager == null) return;

            foreach (ObjectInitialState initialState in currentRecording.initialStates)
            {
                GameObject obj = objectStateManager.GetObjectFromId(initialState.objectId);
                if (obj != null)
                {
                    obj.transform.position = initialState.position;
                    obj.transform.rotation = initialState.rotation;
                    obj.transform.localScale = initialState.scale;
                }
            }
        }

        /// <summary>
        /// Updates UI elements
        /// </summary>
        private void UpdateUI()
        {
            if (currentRecording == null) return;

            // Update timeline slider
            if (timelineSlider != null)
            {
                float normalizedTime = currentRecording.recordingDuration > 0
                    ? currentPlaybackTime / currentRecording.recordingDuration
                    : 0f;
                timelineSlider.value = normalizedTime;
            }

            // Update time display
            if (timeDisplayText != null)
            {
                string currentTimeStr = FormatTime(currentPlaybackTime);
                string totalTimeStr = FormatTime(currentRecording.recordingDuration);
                timeDisplayText.text = $"{currentTimeStr} / {totalTimeStr}";
            }

            // Note: WebView timeline editor has its own playback timer
            // Unity controls WebView, but they don't need to stay perfectly synced
        }

        /// <summary>
        /// Formats time in MM:SS format
        /// </summary>
        private string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            return $"{minutes:D2}:{seconds:D2}";
        }

        /// <summary>
        /// Creates simple player models if they don't exist
        /// </summary>
        private void CreatePlayerModelsIfNeeded()
        {
            if (playerModelContainer == null)
            {
                GameObject container = new GameObject("RecordingPlayerModel");
                playerModelContainer = container;
            }

            // Create headset model
            if (headsetModel == null)
            {
                headsetModel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                headsetModel.name = "HeadsetModel";
                headsetModel.transform.SetParent(playerModelContainer.transform);
                headsetModel.transform.localScale = Vector3.one * 0.1f;
                headsetModel.GetComponent<Renderer>().material.color = Color.blue;
                // Remove collider (not needed for visual)
                Destroy(headsetModel.GetComponent<Collider>());
            }

            // Create left controller model
            if (leftControllerModel == null)
            {
                leftControllerModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leftControllerModel.name = "LeftControllerModel";
                leftControllerModel.transform.SetParent(playerModelContainer.transform);
                leftControllerModel.transform.localScale = new Vector3(0.05f, 0.1f, 0.15f);
                leftControllerModel.GetComponent<Renderer>().material.color = Color.green;
                Destroy(leftControllerModel.GetComponent<Collider>());
            }

            // Create right controller model
            if (rightControllerModel == null)
            {
                rightControllerModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rightControllerModel.name = "RightControllerModel";
                rightControllerModel.transform.SetParent(playerModelContainer.transform);
                rightControllerModel.transform.localScale = new Vector3(0.05f, 0.1f, 0.15f);
                rightControllerModel.GetComponent<Renderer>().material.color = Color.red;
                Destroy(rightControllerModel.GetComponent<Collider>());
            }

            // Hide by default
            playerModelContainer.SetActive(false);
        }

        /// <summary>
        /// Gets current playback time
        /// </summary>
        public float CurrentTime => currentPlaybackTime;

        /// <summary>
        /// Gets total duration
        /// </summary>
        public float TotalDuration => currentRecording != null ? currentRecording.recordingDuration : 0f;

        /// <summary>
        /// Checks if currently playing
        /// </summary>
        public bool IsPlaying => isPlaying;

        /// <summary>
        /// Builds interaction sequences (grab-release pairs)
        /// </summary>
        private void BuildInteractionSequences()
        {
            interactionSequences.Clear();

            if (currentRecording == null || currentRecording.interactionEvents.Count == 0)
            {
                return;
            }

            Dictionary<string, InteractionEvent> pendingGrabs = new Dictionary<string, InteractionEvent>();

            foreach (InteractionEvent interactionEvent in currentRecording.interactionEvents)
            {
                if (interactionEvent.eventType == InteractionEventType.Grab)
                {
                    pendingGrabs[interactionEvent.objectId] = interactionEvent;
                }
                else if (interactionEvent.eventType == InteractionEventType.Release)
                {
                    if (pendingGrabs.ContainsKey(interactionEvent.objectId))
                    {
                        InteractionSequence sequence = new InteractionSequence
                        {
                            objectId = interactionEvent.objectId,
                            grabEvent = pendingGrabs[interactionEvent.objectId],
                            releaseEvent = interactionEvent
                        };
                        interactionSequences.Add(sequence);
                        pendingGrabs.Remove(interactionEvent.objectId);
                    }
                }
            }
        }

        /// <summary>
        /// Creates path visualization for a specific object (called when object is grabbed)
        /// </summary>
        private void CreatePathForObject(string objectId)
        {
            if (!objectSnapshotsByTime.ContainsKey(objectId)) return;
            if (pathLines.ContainsKey(objectId)) return; // Already created

            List<TransformSnapshot> snapshots = objectSnapshotsByTime[objectId];

            if (snapshots.Count < 2) return; // Need at least 2 points for a line

            // Create LineRenderer for this object's path
            GameObject pathObj = new GameObject($"PathLine_{objectId}");
            pathObj.transform.SetParent(transform);
            LineRenderer lineRenderer = pathObj.AddComponent<LineRenderer>();

            // Configure line renderer for dotted line effect
            lineRenderer.positionCount = snapshots.Count;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.material = GetPathLineMaterial();
            lineRenderer.startColor = new Color(1f, 1f, 1f, 0.6f); // White with transparency
            lineRenderer.endColor = new Color(1f, 1f, 1f, 0.6f);
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            
            // Set positions from snapshots
            for (int i = 0; i < snapshots.Count; i++)
            {
                lineRenderer.SetPosition(i, snapshots[i].position);
            }

            pathLines[objectId] = lineRenderer;
        }

        /// <summary>
        /// Creates green highlight at end position for a specific object (called when object is grabbed)
        /// </summary>
        private void CreateEndPositionHighlight(string objectId)
        {
            if (visualCueManager == null || currentRecording == null) return;
            if (endPositionHighlights.ContainsKey(objectId)) return; // Already created

            // Find the sequence for this object
            InteractionSequence sequence = interactionSequences.Find(s => s.objectId == objectId);
            if (sequence == null) return;

            GameObject obj = objectStateManager.GetObjectFromId(objectId);
            if (obj != null)
            {
                // Show ghost at release position (green highlight)
                visualCueManager.ShowGhostObject(obj, sequence.releaseEvent.position, sequence.releaseEvent.rotation);
                endPositionHighlights[objectId] = obj;
            }
        }

        /// <summary>
        /// Updates visual annotations based on current playback time
        /// Progressive system: red highlight before grab, path+green after grab
        /// </summary>
        private void UpdateVisualAnnotations(float time)
        {
            // Remove all red highlights first
            List<GameObject> objectsToUnhighlight = new List<GameObject>(originalMaterialsForRedHighlight.Keys);
            foreach (GameObject obj in objectsToUnhighlight)
            {
                RemoveRedHighlight(obj);
            }

            // Hide all path lines and green highlights initially
            HideAllPathLines();
            HideAllEndPositionHighlights();

            // Process each interaction sequence based on timeline
            foreach (InteractionSequence sequence in interactionSequences)
            {
                float grabTime = sequence.grabEvent.timestamp;
                float releaseTime = sequence.releaseEvent.timestamp;
                string objectId = sequence.objectId;
                GameObject obj = objectStateManager.GetObjectFromId(objectId);

                if (obj == null) continue;

                if (time < grabTime)
                {
                    // Before grab: Show red highlight if within preview time
                    if (time >= (grabTime - highlightPreviewTime))
                    {
                        HighlightObjectRed(obj);
                    }
                }
                else if (time >= grabTime && time < releaseTime)
                {
                    // During interaction: Show path line and green end position
                    CreatePathForObject(objectId);
                    ShowPathLine(objectId);
                    CreateEndPositionHighlight(objectId);
                    ShowEndPositionHighlight(objectId);
                }
                else if (time >= releaseTime)
                {
                    // After release: Keep path and green visible
                    CreatePathForObject(objectId);
                    ShowPathLine(objectId);
                    CreateEndPositionHighlight(objectId);
                    ShowEndPositionHighlight(objectId);
                }
            }
        }

        // Store original materials for red highlights
        private Dictionary<GameObject, List<Material>> originalMaterialsForRedHighlight = new Dictionary<GameObject, List<Material>>();

        /// <summary>
        /// Highlights an object in red
        /// </summary>
        private void HighlightObjectRed(GameObject obj)
        {
            if (obj == null) return;

            // Skip if already highlighted
            if (originalMaterialsForRedHighlight.ContainsKey(obj)) return;

            // Store original materials
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            List<Material> originalMats = new List<Material>();
            
            foreach (Renderer renderer in renderers)
            {
                if (renderer.material != null)
                {
                    originalMats.Add(renderer.material);
                    
                    // Create red highlight material - use unlit shader for bright red
                    Material redMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    redMat.color = new Color(1f, 0f, 0f, 1f); // Bright red, fully opaque
                    renderer.material = redMat;
                }
                else
                {
                    originalMats.Add(null);
                }
            }
            
            originalMaterialsForRedHighlight[obj] = originalMats;
        }

        /// <summary>
        /// Removes red highlight from an object
        /// </summary>
        private void RemoveRedHighlight(GameObject obj)
        {
            if (obj == null || !originalMaterialsForRedHighlight.ContainsKey(obj)) return;

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            List<Material> originalMats = originalMaterialsForRedHighlight[obj];
            
            int matIndex = 0;
            foreach (Renderer renderer in renderers)
            {
                if (matIndex < originalMats.Count)
                {
                    renderer.material = originalMats[matIndex];
                    matIndex++;
                }
            }
            
            originalMaterialsForRedHighlight.Remove(obj);
        }

        /// <summary>
        /// Clears all visual annotations
        /// </summary>
        private void ClearVisualAnnotations()
        {
            ClearPathLines();
            ClearEndPositionHighlights();
            
            // Remove all red highlights
            List<GameObject> objectsToUnhighlight = new List<GameObject>(originalMaterialsForRedHighlight.Keys);
            foreach (GameObject obj in objectsToUnhighlight)
            {
                RemoveRedHighlight(obj);
            }
            
            if (visualCueManager != null)
            {
                visualCueManager.ClearAllHighlights();
                visualCueManager.HideAllGhosts();
            }
        }

        /// <summary>
        /// Clears path lines
        /// </summary>
        private void ClearPathLines()
        {
            foreach (var kvp in pathLines)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            pathLines.Clear();
        }

        /// <summary>
        /// Clears end position highlights
        /// </summary>
        private void ClearEndPositionHighlights()
        {
            if (visualCueManager != null)
            {
                visualCueManager.HideAllGhosts();
            }
            endPositionHighlights.Clear();
        }

        /// <summary>
        /// Shows path line for an object
        /// </summary>
        private void ShowPathLine(string objectId)
        {
            if (pathLines.ContainsKey(objectId) && pathLines[objectId] != null)
            {
                pathLines[objectId].gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Hides path line for an object
        /// </summary>
        private void HidePathLine(string objectId)
        {
            if (pathLines.ContainsKey(objectId) && pathLines[objectId] != null)
            {
                pathLines[objectId].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Hides all path lines
        /// </summary>
        private void HideAllPathLines()
        {
            foreach (var kvp in pathLines)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Shows end position highlight for an object
        /// </summary>
        private void ShowEndPositionHighlight(string objectId)
        {
            if (visualCueManager == null) return;

            // The ghost is managed by VisualCueManager, but we need to ensure it's visible
            GameObject obj = objectStateManager.GetObjectFromId(objectId);
            if (obj != null && endPositionHighlights.ContainsKey(objectId))
            {
                InteractionSequence sequence = interactionSequences.Find(s => s.objectId == objectId);
                if (sequence != null)
                {
                    visualCueManager.ShowGhostObject(obj, sequence.releaseEvent.position, sequence.releaseEvent.rotation);
                }
            }
        }

        /// <summary>
        /// Hides end position highlight for an object
        /// </summary>
        private void HideEndPositionHighlight(string objectId)
        {
            if (visualCueManager == null) return;
            GameObject obj = objectStateManager.GetObjectFromId(objectId);
            if (obj != null)
            {
                visualCueManager.HideGhostObject(obj);
            }
        }

        /// <summary>
        /// Hides all end position highlights
        /// </summary>
        private void HideAllEndPositionHighlights()
        {
            if (visualCueManager != null)
            {
                visualCueManager.HideAllGhosts();
            }
        }

        /// <summary>
        /// Freezes all objects by making them kinematic
        /// </summary>
        private void FreezeAllObjects()
        {
            if (objectStateManager == null) return;

            objectRigidbodies.Clear();
            originalKinematicStates.Clear();

            foreach (var kvp in objectStateManager.InteractableObjects)
            {
                GameObject obj = kvp.Value.gameObject;
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                
                if (rb != null)
                {
                    objectRigidbodies[obj] = rb;
                    originalKinematicStates[obj] = rb.isKinematic;
                    rb.isKinematic = true; // Freeze the object
                }
            }
        }

        /// <summary>
        /// Unfreezes all objects by restoring their original kinematic state
        /// IMPORTANT: This finds ALL objects from ObjectStateManager and FORCES them to be unfrozen
        /// </summary>
        private void UnfreezeAllObjects()
        {
            Debug.LogError("───────────────────────────────────────────");
            Debug.LogError("🔓 UnfreezeAllObjects() STARTED");
            Debug.LogError($"   Tracked objects: {objectRigidbodies.Count}");
            Debug.LogError($"   Original states: {originalKinematicStates.Count}");

            int unfrozenCount = 0;

            // First, restore any tracked objects
            foreach (var kvp in objectRigidbodies)
            {
                GameObject obj = kvp.Key;
                Rigidbody rb = kvp.Value;

                if (rb != null && originalKinematicStates.ContainsKey(obj))
                {
                    bool wasKinematic = rb.isKinematic;
                    rb.isKinematic = originalKinematicStates[obj];
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    Debug.LogError($"   ✅ {obj.name}: kinematic {wasKinematic} → {rb.isKinematic}");
                    unfrozenCount++;
                }
            }

            Debug.LogError($"   Restored {unfrozenCount} tracked objects");

            // FORCE unfreeze ALL objects from ObjectStateManager (comprehensive check)
            if (objectStateManager != null)
            {
                Debug.LogError($"   ObjectStateManager has {objectStateManager.InteractableObjects.Count} objects");
                int forcedUnfreezeCount = 0;

                foreach (var kvp in objectStateManager.InteractableObjects)
                {
                    GameObject obj = kvp.Value.gameObject;
                    if (obj == null) continue;

                    Rigidbody rb = obj.GetComponent<Rigidbody>();

                    if (rb != null)
                    {
                        bool wasKinematic = rb.isKinematic;

                        // If we tracked it, restore original state
                        if (originalKinematicStates.ContainsKey(obj))
                        {
                            rb.isKinematic = originalKinematicStates[obj];
                            Debug.LogError($"   📝 {obj.name}: restored to original ({rb.isKinematic})");
                        }
                        // Otherwise, FORCE it to be non-kinematic (unfrozen)
                        // This ensures objects are never left frozen after edit mode
                        else if (rb.isKinematic)
                        {
                            rb.isKinematic = false;
                            Debug.LogError($"   🔓 {obj.name}: FORCED to non-kinematic (was: {wasKinematic})");
                            forcedUnfreezeCount++;
                        }

                        // Reset velocities to prevent objects from moving unexpectedly
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }

                Debug.LogError($"   Forced {forcedUnfreezeCount} objects to non-kinematic");
            }
            else
            {
                Debug.LogError("   ❌ ObjectStateManager is NULL!");
            }

            // Clear tracking dictionaries
            objectRigidbodies.Clear();
            originalKinematicStates.Clear();

            Debug.LogError("✅ Tracking dictionaries cleared");
            Debug.LogError("✅ ALL OBJECTS UNFROZEN (gravity restored)");
            Debug.LogError("───────────────────────────────────────────");
            Debug.Log("RecordingPlaybackEditor: All objects unfrozen (gravity restored)");
        }

        /// <summary>
        /// Gets or creates path line material (dotted line effect)
        /// </summary>
        private Material GetPathLineMaterial()
        {
            if (pathLineMaterial != null)
            {
                return pathLineMaterial;
            }

            // Create a simple material for dotted lines
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = Color.white;
            return mat;
        }

        /// <summary>
        /// Creates timeline markers for grab and release events
        /// </summary>
        private void CreateTimelineMarkers()
        {
            ClearTimelineMarkers();

            if (currentRecording == null || timelineSlider == null) return;

            // Get or create markers container
            RectTransform sliderRect = timelineSlider.GetComponent<RectTransform>();
            if (sliderRect == null) return;

            if (timelineMarkersContainer == null)
            {
                // Create container as child of slider
                GameObject containerObj = new GameObject("TimelineMarkersContainer");
                containerObj.transform.SetParent(timelineSlider.transform, false);
                timelineMarkersContainer = containerObj.AddComponent<RectTransform>();
                
                // Position container to match slider's fill area
                timelineMarkersContainer.anchorMin = new Vector2(0f, 0f);
                timelineMarkersContainer.anchorMax = new Vector2(1f, 1f);
                timelineMarkersContainer.sizeDelta = Vector2.zero;
                timelineMarkersContainer.anchoredPosition = Vector2.zero;
            }

            // Create markers for each interaction event
            foreach (InteractionEvent interactionEvent in currentRecording.interactionEvents)
            {
                float normalizedTime = currentRecording.recordingDuration > 0 
                    ? interactionEvent.timestamp / currentRecording.recordingDuration 
                    : 0f;

                // Clamp to valid range
                normalizedTime = Mathf.Clamp01(normalizedTime);

                // Create marker
                GameObject marker = CreateMarker(
                    interactionEvent.eventType == InteractionEventType.Grab ? grabMarkerColor : releaseMarkerColor,
                    normalizedTime
                );

                if (marker != null)
                {
                    timelineMarkers.Add(marker);
                }
            }
        }

        /// <summary>
        /// Creates a single timeline marker
        /// </summary>
        private GameObject CreateMarker(Color color, float normalizedPosition)
        {
            if (timelineMarkersContainer == null) return null;

            GameObject markerObj;

            // Use prefab if available, otherwise create simple image
            if (color == grabMarkerColor && grabMarkerPrefab != null)
            {
                markerObj = Instantiate(grabMarkerPrefab, timelineMarkersContainer);
            }
            else if (color == releaseMarkerColor && releaseMarkerPrefab != null)
            {
                markerObj = Instantiate(releaseMarkerPrefab, timelineMarkersContainer);
            }
            else
            {
                // Create simple image marker
                markerObj = new GameObject($"Marker_{normalizedPosition:F2}");
                markerObj.transform.SetParent(timelineMarkersContainer, false);
                
                Image markerImage = markerObj.AddComponent<Image>();
                markerImage.color = color;
            }

            // Setup RectTransform
            RectTransform markerRect = markerObj.GetComponent<RectTransform>();
            if (markerRect == null)
            {
                markerRect = markerObj.AddComponent<RectTransform>();
            }

            // Position marker based on normalized position (0-1)
            markerRect.anchorMin = new Vector2(normalizedPosition, 0f);
            markerRect.anchorMax = new Vector2(normalizedPosition, 1f);
            markerRect.sizeDelta = new Vector2(markerSize.x, markerSize.y);
            markerRect.anchoredPosition = Vector2.zero;
            markerRect.pivot = new Vector2(0.5f, 0.5f);

            return markerObj;
        }

        /// <summary>
        /// Clears all timeline markers
        /// </summary>
        private void ClearTimelineMarkers()
        {
            foreach (GameObject marker in timelineMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker);
                }
            }
            timelineMarkers.Clear();
        }



        /// <summary>
        /// Data structure for interaction sequence
        /// </summary>
        private class InteractionSequence
        {
            public string objectId;
            public InteractionEvent grabEvent;
            public InteractionEvent releaseEvent;
        }
    }
}

