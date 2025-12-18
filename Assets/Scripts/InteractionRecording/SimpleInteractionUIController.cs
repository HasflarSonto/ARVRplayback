using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRInteractionRecording
{
    /// <summary>
    /// Simplified UI Controller for single interaction recording/playback
    /// Works with Text Poke Button Special structure (Button Front/Back are parts of one button)
    /// </summary>
    public class SimpleInteractionUIController : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField]
        [Tooltip("Reference to InteractionRecordingManager (auto-finds if null)")]
        private InteractionRecordingManager recordingManager;

        [SerializeField]
        [Tooltip("Reference to InteractionPlaybackManager (auto-finds if null)")]
        private InteractionPlaybackManager playbackManager;

        [Header("Text Poke Buttons")]
        [SerializeField]
        [Tooltip("Text Poke Button for Record (the main button GameObject)")]
        private GameObject recordButton;

        [SerializeField]
        [Tooltip("Text Poke Button for Playback (the main button GameObject)")]
        private GameObject playbackButton;

        [SerializeField]
        [Tooltip("Text Poke Button for Reset (the main button GameObject)")]
        private GameObject resetButton;

        [SerializeField]
        [Tooltip("Text Poke Button for Edit (the main button GameObject)")]
        private GameObject editButton;

        [Header("Button Text (Optional - for Button Front/Back)")]
        [SerializeField]
        [Tooltip("Text on Button Front of Record button (optional)")]
        private TextMeshProUGUI recordButtonFrontText;

        [SerializeField]
        [Tooltip("Text on Button Back of Record button (optional)")]
        private TextMeshProUGUI recordButtonBackText;

        [SerializeField]
        [Tooltip("Text on Button Front of Playback button (optional)")]
        private TextMeshProUGUI playbackButtonFrontText;

        [SerializeField]
        [Tooltip("Text on Button Back of Playback button (optional)")]
        private TextMeshProUGUI playbackButtonBackText;

        [Header("Status Display")]
        [SerializeField]
        [Tooltip("Text to display current status (Recording/Playback/Idle)")]
        private TextMeshProUGUI statusText;

        [SerializeField]
        [Tooltip("Text to display instructions to the user")]
        private TextMeshProUGUI instructionText;

        [Header("Edit Mode")]
        [SerializeField]
        [Tooltip("Panel for edit mode UI (timeline, controls, etc.)")]
        private GameObject editModePanel;

        [SerializeField]
        [Tooltip("Reference to RecordingPlaybackEditor")]
        private RecordingPlaybackEditor playbackEditor;

        [SerializeField]
        [Tooltip("Reference to WebViewManager for displaying JSON")]
        private WebViewManager webViewManager;

        private bool isRecording = false;
        private bool isPlaybackActive = false;
        private bool isEditModeActive = false;
        private RecordingData currentRecording;

        // Button components (will be found automatically)
        private Button recordButtonComponent;
        private Button playbackButtonComponent;
        private Button resetButtonComponent;
        private Button editButtonComponent;

        private void Awake()
        {
            // Clean up broken Affordance components to reduce console errors
            // Run in Awake to catch components before they start
            GlobalAffordanceCleanup cleanup = GetComponent<GlobalAffordanceCleanup>();
            if (cleanup == null)
            {
                cleanup = gameObject.AddComponent<GlobalAffordanceCleanup>();
            }
            cleanup.CleanupAllAffordances();
        }

        private void Start()
        {

            // Find managers if not assigned
            if (recordingManager == null)
            {
                recordingManager = FindFirstObjectByType<InteractionRecordingManager>();
            }

            if (playbackManager == null)
            {
                playbackManager = FindFirstObjectByType<InteractionPlaybackManager>();
            }

            if (playbackEditor == null)
            {
                playbackEditor = FindFirstObjectByType<RecordingPlaybackEditor>();
            }

            if (webViewManager == null)
            {
                webViewManager = FindFirstObjectByType<WebViewManager>();
            }

            // Find button components
            SetupButtons();

            // Hide edit mode panel by default
            if (editModePanel != null)
            {
                editModePanel.SetActive(false);
            }

            // Subscribe to manager events
            if (recordingManager != null)
            {
                recordingManager.OnRecordingStarted += OnRecordingStarted;
                recordingManager.OnRecordingStopped += OnRecordingStopped;
            }

            if (playbackManager != null)
            {
                playbackManager.OnPlaybackStarted += OnPlaybackStarted;
                playbackManager.OnPlaybackStopped += OnPlaybackStopped;
                playbackManager.OnObjectHighlighted += OnObjectHighlighted;
                playbackManager.OnObjectInteractionCompleted += OnObjectInteractionCompleted;
                playbackManager.OnObjectIncorrectlyPlaced += OnObjectIncorrectlyPlaced;
                playbackManager.OnInteractionSequenceProgress += OnInteractionSequenceProgress;
                playbackManager.OnAllInteractionsCompleted += OnAllInteractionsCompleted;
                playbackManager.OnMovementGoalsLoaded += OnMovementGoalsLoaded;
            }

            // Initialize UI state
            UpdateUIState();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (recordingManager != null)
            {
                recordingManager.OnRecordingStarted -= OnRecordingStarted;
                recordingManager.OnRecordingStopped -= OnRecordingStopped;
            }

            if (playbackManager != null)
            {
                playbackManager.OnPlaybackStarted -= OnPlaybackStarted;
                playbackManager.OnPlaybackStopped -= OnPlaybackStopped;
                playbackManager.OnObjectHighlighted -= OnObjectHighlighted;
                playbackManager.OnObjectInteractionCompleted -= OnObjectInteractionCompleted;
                playbackManager.OnObjectIncorrectlyPlaced -= OnObjectIncorrectlyPlaced;
                playbackManager.OnInteractionSequenceProgress -= OnInteractionSequenceProgress;
                playbackManager.OnAllInteractionsCompleted -= OnAllInteractionsCompleted;
                playbackManager.OnMovementGoalsLoaded -= OnMovementGoalsLoaded;
            }
        }

        /// <summary>
        /// Sets up button components and listeners
        /// </summary>
        private void SetupButtons()
        {
            // Record button
            if (recordButton != null)
            {
                recordButtonComponent = recordButton.GetComponent<Button>();
                if (recordButtonComponent == null)
                {
                    // Try to find button in children (Button Front or Button Back)
                    recordButtonComponent = recordButton.GetComponentInChildren<Button>();
                }

                if (recordButtonComponent != null)
                {
                    recordButtonComponent.onClick.AddListener(OnRecordButtonClicked);
                }
                else
                {
                    Debug.LogWarning("SimpleInteractionUIController: Record button has no Button component!");
                }
            }

            // Playback button
            if (playbackButton != null)
            {
                playbackButtonComponent = playbackButton.GetComponent<Button>();
                if (playbackButtonComponent == null)
                {
                    playbackButtonComponent = playbackButton.GetComponentInChildren<Button>();
                }

                if (playbackButtonComponent != null)
                {
                    playbackButtonComponent.onClick.AddListener(OnPlaybackButtonClicked);
                }
                else
                {
                    Debug.LogWarning("SimpleInteractionUIController: Playback button has no Button component!");
                }
            }

            // Reset button
            if (resetButton != null)
            {
                resetButtonComponent = resetButton.GetComponent<Button>();
                if (resetButtonComponent == null)
                {
                    resetButtonComponent = resetButton.GetComponentInChildren<Button>();
                }

                if (resetButtonComponent != null)
                {
                    resetButtonComponent.onClick.AddListener(OnResetButtonClicked);
                }
                else
                {
                    Debug.LogWarning("SimpleInteractionUIController: Reset button has no Button component!");
                }
            }

            // Edit button
            if (editButton != null)
            {
                editButtonComponent = editButton.GetComponent<Button>();
                if (editButtonComponent == null)
                {
                    editButtonComponent = editButton.GetComponentInChildren<Button>();
                }

                if (editButtonComponent != null)
                {
                    editButtonComponent.onClick.AddListener(OnEditButtonClicked);
                }
                else
                {
                    Debug.LogWarning("SimpleInteractionUIController: Edit button has no Button component!");
                }
            }
        }

        /// <summary>
        /// Called when record button is clicked
        /// </summary>
        private void OnRecordButtonClicked()
        {
            // Don't allow record button to work while in edit mode
            if (isEditModeActive) return;
            
            if (recordingManager == null) return;

            if (isRecording)
            {
                // Stop recording manually (though it should auto-stop after release)
                currentRecording = recordingManager.StopRecording();
                isRecording = false;
            }
            else
            {
                // Start recording
                if (isPlaybackActive)
                {
                    playbackManager.StopPlayback();
                    isPlaybackActive = false;
                }

                recordingManager.StartRecording();
                isRecording = true;
            }

            UpdateUIState();
        }

        /// <summary>
        /// Called when playback button is clicked
        /// </summary>
        private void OnPlaybackButtonClicked()
        {
            // Don't allow playback button to work while in edit mode
            if (isEditModeActive) return;
            
            if (playbackManager == null) return;

            if (isPlaybackActive)
            {
                // Stop playback
                playbackManager.StopPlayback();
                isPlaybackActive = false;
            }
            else
            {
                // Start playback
                if (isRecording)
                {
                    currentRecording = recordingManager.StopRecording();
                    isRecording = false;
                }

                if (currentRecording != null)
                {
                    // Check if there's a baked TaskInstruction with Move blocks
                    TaskInstruction bakedTask = null;

                    Debug.LogError("═══════════════════════════════════════════");
                    Debug.LogError("[SimpleInteractionUIController] PLAYBACK BUTTON CLICKED");
                    Debug.LogError($"   webViewManager null? {webViewManager == null}");

                    if (webViewManager != null)
                    {
                        bakedTask = webViewManager.GetBakedTaskInstruction();
                        Debug.LogError($"   bakedTask null? {bakedTask == null}");
                        if (bakedTask != null)
                        {
                            Debug.LogError($"   bakedTask.steps null? {bakedTask.steps == null}");
                            if (bakedTask.steps != null)
                            {
                                Debug.LogError($"   bakedTask.steps.Count = {bakedTask.steps.Count}");
                                int moveCount = 0;
                                foreach (var step in bakedTask.steps)
                                {
                                    if (step.IsMove()) moveCount++;
                                }
                                Debug.LogError($"   Move blocks in bakedTask = {moveCount}");
                            }
                        }
                    }

                    // Start playback with or without baked task instruction
                    if (bakedTask != null)
                    {
                        Debug.LogError($"✅ Starting playback WITH baked TaskInstruction ({bakedTask.steps.Count} steps)");
                        playbackManager.StartPlayback(currentRecording, bakedTask);
                    }
                    else
                    {
                        Debug.LogError("❌ Starting playback WITHOUT TaskInstruction (bakedTask is NULL)");
                        playbackManager.StartPlayback(currentRecording);
                    }
                    Debug.LogError("═══════════════════════════════════════════");

                    isPlaybackActive = true;
                }
                else
                {
                    Debug.LogWarning("SimpleInteractionUIController: No recording available to play back!");
                    if (instructionText != null)
                    {
                        instructionText.text = "No recording available. Please record an interaction first.";
                    }
                }
            }

            UpdateUIState();
        }

        /// <summary>
        /// Called when edit button is clicked
        /// </summary>
        private void OnEditButtonClicked()
        {
            // Prevent accidental toggles - only allow if not currently recording or playing back
            if (isRecording || isPlaybackActive)
            {
                Debug.LogWarning("SimpleInteractionUIController: Cannot enter edit mode while recording or playing back!");
                return;
            }
            
            if (currentRecording == null)
            {
                Debug.LogWarning("SimpleInteractionUIController: No recording available to edit!");
                if (instructionText != null)
                {
                    instructionText.text = "No recording available. Please record an interaction first.";
                }
                return;
            }

            // Toggle edit mode
            isEditModeActive = !isEditModeActive;

            if (isEditModeActive)
            {
                // Enter edit mode
                // Stop any active playback
                if (isPlaybackActive && playbackManager != null)
                {
                    playbackManager.StopPlayback();
                    isPlaybackActive = false;
                }

                // Stop any active recording
                if (isRecording && recordingManager != null)
                {
                    currentRecording = recordingManager.StopRecording();
                    isRecording = false;
                }

                // Show edit mode panel
                if (editModePanel != null && !editModePanel.activeSelf)
                {
                    editModePanel.SetActive(true);
                }

                // Start edit playback
                if (playbackEditor != null)
                {
                    playbackEditor.StartEditPlayback(currentRecording);
                }

                // Generate and display JSON in timeline editor when entering edit mode
                if (currentRecording != null && webViewManager != null)
                {
                    Debug.LogError("═══════════════════════════════════════════");
                    Debug.LogError("📋 EDIT MODE ACTIVATED - Scheduling JSON Generation");
                    Debug.LogError($"   Recording has {currentRecording.interactionEvents.Count} events");
                    Debug.LogError($"   WebViewManager exists: {webViewManager != null}");
                    Debug.LogError("   Calling GenerateAndDisplayJSON in 3 seconds...");
                    Debug.LogError("   (Longer delay to ensure Vuplex APIs are injected)");
                    Debug.LogError("═══════════════════════════════════════════");
                    // INCREASED delay to ensure WebView AND Vuplex APIs are fully ready
                    // The WebView HTML loads first, but Vuplex needs time to inject its JavaScript APIs
                    Invoke(nameof(GenerateAndDisplayJSON), 3f);
                }
                else
                {
                    Debug.LogError("═══════════════════════════════════════════");
                    Debug.LogError("❌ EDIT MODE - Cannot generate JSON!");
                    Debug.LogError($"   currentRecording is null: {currentRecording == null}");
                    Debug.LogError($"   webViewManager is null: {webViewManager == null}");
                    Debug.LogError("═══════════════════════════════════════════");
                }

                if (instructionText != null)
                {
                    instructionText.text = "Edit Mode: Use timeline to scrub through recording. Click Edit again to exit.";
                }
            }
            else
            {
                // Exit edit mode
                if (playbackEditor != null)
                {
                    playbackEditor.StopEditPlayback();
                }
                
                // Hide panel AFTER stopping playback
                if (editModePanel != null && editModePanel.activeSelf)
                {
                    editModePanel.SetActive(false);
                }

                if (instructionText != null)
                {
                    instructionText.text = "Edit mode closed.";
                }
            }

            UpdateUIState();
        }

        /// <summary>
        /// Called when reset button is clicked
        /// </summary>
        private void OnResetButtonClicked()
        {
            // Don't allow reset button to work while in edit mode (user should exit edit mode first)
            if (isEditModeActive) return;
            
            // Stop recording if active
            if (isRecording && recordingManager != null)
            {
                recordingManager.StopRecording();
                isRecording = false;
            }

            // Stop playback if active
            if (isPlaybackActive && playbackManager != null)
            {
                playbackManager.StopPlayback();
                isPlaybackActive = false;
            }

            // Exit edit mode if active
            if (isEditModeActive)
            {
                isEditModeActive = false;
                if (editModePanel != null && editModePanel.activeSelf)
                {
                    editModePanel.SetActive(false);
                }
                if (playbackEditor != null)
                {
                    playbackEditor.StopEditPlayback();
                }
            }

            // Reset objects
            ObjectStateManager objectStateManager = FindFirstObjectByType<ObjectStateManager>();
            if (objectStateManager != null)
            {
                objectStateManager.ResetAllObjects();
            }

            // Clear visual cues
            VisualCueManager visualCueManager = FindFirstObjectByType<VisualCueManager>();
            if (visualCueManager != null)
            {
                visualCueManager.ClearAllHighlights();
                visualCueManager.HideAllGhosts();
            }

            currentRecording = null;
            UpdateUIState();
        }

        private bool isUpdatingUI = false; // Prevent recursive calls
        private Coroutine uiUpdateCoroutine = null; // Track coroutine to prevent multiple
        
        /// <summary>
        /// Updates the UI state based on current mode
        /// Uses a single batched update to prevent layout jumps
        /// </summary>
        private void UpdateUIState()
        {
            // Prevent recursive calls that could cause layout jumps
            if (isUpdatingUI) return;
            
            // Cancel any existing update coroutine
            if (uiUpdateCoroutine != null)
            {
                StopCoroutine(uiUpdateCoroutine);
            }
            
            // Start a new batched update
            uiUpdateCoroutine = StartCoroutine(UpdateUIStateDelayed());
        }
        
        /// <summary>
        /// Batches all UI updates together to prevent layout jumps
        /// </summary>
        private System.Collections.IEnumerator UpdateUIStateDelayed()
        {
            isUpdatingUI = true;
            
            // Wait one frame to batch all updates
            yield return null;
            
            try
            {
                // CRITICAL: Ensure edit mode panel stays visible when in edit mode
                // Do this FIRST before any other updates
                if (isEditModeActive && editModePanel != null)
                {
                    if (!editModePanel.activeSelf)
                    {
                        editModePanel.SetActive(true);
                    }
                }
                
                // Update button interactability
                if (recordButtonComponent != null)
                {
                    recordButtonComponent.interactable = !isPlaybackActive && !isEditModeActive;
                }

                if (playbackButtonComponent != null)
                {
                    playbackButtonComponent.interactable = !isRecording && currentRecording != null && !isEditModeActive;
                }

                if (resetButtonComponent != null)
                {
                    resetButtonComponent.interactable = !isEditModeActive;
                }

                if (editButtonComponent != null)
                {
                    editButtonComponent.interactable = !isRecording && currentRecording != null && !isPlaybackActive;
                }

                // Update all text elements (batched together to minimize layout recalculations)
                if (recordButtonFrontText != null)
                {
                    recordButtonFrontText.text = isRecording ? "Stop Recording" : "Start Recording";
                }
                if (recordButtonBackText != null)
                {
                    recordButtonBackText.text = isRecording ? "Stop Recording" : "Start Recording";
                }

                if (playbackButtonFrontText != null)
                {
                    playbackButtonFrontText.text = isPlaybackActive ? "Stop Playback" : "Start Playback";
                }
                if (playbackButtonBackText != null)
                {
                    playbackButtonBackText.text = isPlaybackActive ? "Stop Playback" : "Start Playback";
                }

                if (statusText != null)
                {
                    if (isEditModeActive)
                    {
                        statusText.text = "Status: EDIT MODE";
                        statusText.color = Color.cyan;
                    }
                    else if (isRecording)
                    {
                        statusText.text = "Status: RECORDING";
                        statusText.color = Color.red;
                    }
                    else if (isPlaybackActive)
                    {
                        statusText.text = "Status: PLAYBACK";
                        statusText.color = Color.green;
                    }
                    else
                    {
                        statusText.text = "Status: IDLE";
                        statusText.color = Color.white;
                    }
                }

                if (instructionText != null)
                {
                    if (isRecording)
                    {
                        instructionText.text = "Recording... Interact with objects. Click Record again to stop.";
                    }
                    else if (isPlaybackActive)
                    {
                        int currentStep = playbackManager != null ? playbackManager.CurrentStep : 0;
                        int totalSteps = playbackManager != null ? playbackManager.TotalSteps : 0;
                        if (totalSteps > 0)
                        {
                            instructionText.text = $"Step {currentStep} of {totalSteps}: Pick up the highlighted object and place it at the green ghost location.";
                        }
                        else
                        {
                            instructionText.text = "Playback active. Pick up the highlighted object and place it at the green ghost location.";
                        }
                    }
                    else
                    {
                        instructionText.text = "Ready. Press Record to capture multiple interactions. Click Record again to stop.";
                    }
                }
                
                // Double-check panel visibility after all updates
                if (isEditModeActive && editModePanel != null && !editModePanel.activeSelf)
                {
                    editModePanel.SetActive(true);
                }
            }
            finally
            {
                isUpdatingUI = false;
                uiUpdateCoroutine = null;
            }
        }

        // Event handlers
        private void OnRecordingStarted()
        {
            isRecording = true;
            UpdateUIState();
        }

        private void OnRecordingStopped()
        {
            Debug.LogError("═══════════════════════════════════════════");
            Debug.LogError("⏹️ OnRecordingStopped CALLED");
            Debug.LogError("═══════════════════════════════════════════");

            isRecording = false;
            if (recordingManager != null)
            {
                currentRecording = recordingManager.GetCurrentRecording();

                Debug.LogError($"   Recording retrieved: {currentRecording != null}");
                if (currentRecording != null)
                {
                    Debug.LogError($"   Recording has {currentRecording.interactionEvents.Count} events");
                }

                // Generate and display JSON with delay to ensure WebView is ready
                // Same delay as Edit mode (WebView needs time to initialize and for Vuplex APIs to inject)
                if (currentRecording != null && webViewManager != null)
                {
                    Debug.LogError($"   WebViewManager exists: {webViewManager != null}");
                    Debug.LogError("   Scheduling JSON generation in 5 seconds...");
                    Debug.LogError("   (Waiting for WebView page to load)");
                    Invoke(nameof(GenerateAndDisplayJSON), 5f);
                }
                else
                {
                    Debug.LogError("❌ Cannot generate JSON:");
                    Debug.LogError($"   currentRecording is null: {currentRecording == null}");
                    Debug.LogError($"   webViewManager is null: {webViewManager == null}");
                }
            }
            else
            {
                Debug.LogError("❌ recordingManager is NULL!");
            }

            Debug.LogError("═══════════════════════════════════════════");
            UpdateUIState();
        }

        /// <summary>
        /// Generates JSON from recording and displays it
        /// </summary>
        private void GenerateAndDisplayJSON()
        {
            Debug.LogError("═══════════════════════════════════════════");
            Debug.LogError("🔄 GenerateAndDisplayJSON() CALLED");
            Debug.LogError("═══════════════════════════════════════════");

            if (currentRecording == null)
            {
                Debug.LogError("❌ No recording data!");
                return;
            }

            Debug.LogError($"✅ Recording exists: {currentRecording.interactionEvents.Count} events");

            // Get ObjectStateManager
            ObjectStateManager objectStateManager = FindFirstObjectByType<ObjectStateManager>();
            if (objectStateManager == null)
            {
                Debug.LogError("❌ ObjectStateManager not found!");
                Debug.LogWarning("SimpleInteractionUIController: ObjectStateManager not found. Cannot generate JSON.");
                return;
            }

            Debug.LogError("✅ ObjectStateManager found");

            // Generate task instruction
            TaskInstruction task = TaskInstructionGenerator.GenerateFromRecording(
                currentRecording,
                objectStateManager,
                "Recorded Task"
            );

            if (task == null)
            {
                Debug.LogError("❌ Task generation failed!");
                Debug.LogWarning("SimpleInteractionUIController: Failed to generate task instruction.");
                return;
            }

            Debug.LogError($"✅ Task generated: {task.steps.Count} steps");

            // Convert to JSON
            string json = TaskInstructionGenerator.ToFormattedJSON(task);
            Debug.LogError($"✅ JSON created: {json.Length} characters");
            Debug.LogError("------FULL JSON START------");
            Debug.LogError(json);
            Debug.LogError("------FULL JSON END------");
            Debug.LogError($"📄 JSON Preview: {json.Substring(0, Mathf.Min(200, json.Length))}...");

            // Display in WebView
            if (webViewManager != null)
            {
                Debug.LogError("✅ WebViewManager exists - calling DisplayJSON()");
                // Get total duration from the task
                float duration = task.totalDuration;
                Debug.LogError($"   Duration: {duration}s");
                Debug.LogError($"   JSON length: {json.Length}");
                Debug.LogError($"   Recording events: {currentRecording.interactionEvents.Count}");

                // Pass both task JSON and recording data to timeline editor
                webViewManager.DisplayJSON(json, duration, currentRecording);
                Debug.LogError("✅ DisplayJSON() called!");
                Debug.Log("SimpleInteractionUIController: JSON generated and displayed");
            }
            else
            {
                Debug.LogError("❌ WebViewManager is NULL!");
                // Fallback: log to console
                Debug.Log($"SimpleInteractionUIController: Generated JSON:\n{json}");
            }

            Debug.LogError("═══════════════════════════════════════════");
        }

        private void OnPlaybackStarted()
        {
            isPlaybackActive = true;
            UpdateUIState();
        }

        private void OnPlaybackStopped()
        {
            isPlaybackActive = false;
            UpdateUIState();
        }

        private void OnObjectHighlighted(string objectId)
        {
            if (instructionText != null)
            {
                instructionText.text = "Pick up the highlighted object.";
            }
        }

        private void OnObjectInteractionCompleted(string objectId)
        {
            if (instructionText != null && playbackManager != null)
            {
                int currentStep = playbackManager.CurrentStep;
                int totalSteps = playbackManager.TotalSteps;
                
                if (currentStep > totalSteps)
                {
                    instructionText.text = "Perfect! All steps completed. Press Reset to try again.";
                }
                else
                {
                    instructionText.text = $"Perfect! Step {currentStep - 1} completed. Continue to step {currentStep} of {totalSteps}.";
                }
            }
        }

        private void OnInteractionSequenceProgress(int currentStep, int totalSteps)
        {
            if (instructionText != null)
            {
                instructionText.text = $"Step {currentStep} of {totalSteps}: Pick up the highlighted object.";
            }
        }

        private void OnObjectIncorrectlyPlaced(string objectId, float distance, float rotationAngle)
        {
            if (instructionText != null)
            {
                instructionText.text = $"Not quite right. Get closer to the green ghost. (Distance: {distance:F2}m)";
            }
        }

        private void OnAllInteractionsCompleted()
        {
            if (instructionText != null)
            {
                instructionText.text = "🎉 All tasks completed! Great job!";
            }

            if (statusText != null)
            {
                statusText.text = "Status: COMPLETE";
                statusText.color = Color.green;
            }

            // Playback will automatically stop, but we update UI here
            isPlaybackActive = false;
            UpdateUIState();
        }

        private void OnMovementGoalsLoaded(int totalMoveBlocks, int createdPaths)
        {
            Debug.LogError($"[SimpleInteractionUIController] OnMovementGoalsLoaded called: {totalMoveBlocks} Move blocks, {createdPaths} paths created");

            if (statusText != null)
            {
                if (createdPaths > 0)
                {
                    // Show successful path loading
                    statusText.text = $"{totalMoveBlocks} object(s) moved | {createdPaths} movement path(s)";
                    statusText.color = Color.cyan;
                }
                else if (totalMoveBlocks > 0)
                {
                    // Show that Move blocks were found but no paths created
                    statusText.text = $"{totalMoveBlocks} object(s) moved | 0 movement paths";
                    statusText.color = Color.red;
                }
                else
                {
                    // No Move blocks found
                    statusText.text = "0 object(s) moved | 0 movement paths";
                    statusText.color = Color.gray;
                }
            }
        }
    }
}

