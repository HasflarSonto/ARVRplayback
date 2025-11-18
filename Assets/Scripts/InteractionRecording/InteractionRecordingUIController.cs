using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRInteractionRecording
{
    /// <summary>
    /// UI Controller for the Interaction Recording and Playback system
    /// Manages UI panels, buttons, and status displays
    /// </summary>
    public class InteractionRecordingUIController : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField]
        [Tooltip("Reference to InteractionRecordingManager")]
        private InteractionRecordingManager recordingManager;

        [SerializeField]
        [Tooltip("Reference to InteractionPlaybackManager")]
        private InteractionPlaybackManager playbackManager;

        [Header("UI Buttons")]
        [SerializeField]
        [Tooltip("Button to start/stop recording")]
        private Button recordButton;

        [SerializeField]
        [Tooltip("Button to start/stop playback")]
        private Button playbackButton;

        [SerializeField]
        [Tooltip("Button to reset all objects")]
        private Button resetButton;

        [Header("UI Text Elements")]
        [SerializeField]
        [Tooltip("Text to display current status (Recording/Playback/Idle)")]
        private TextMeshProUGUI statusText;

        [SerializeField]
        [Tooltip("Text to display recording duration or playback progress")]
        private TextMeshProUGUI progressText;

        [SerializeField]
        [Tooltip("Text to display instructions to the user")]
        private TextMeshProUGUI instructionText;

        [SerializeField]
        [Tooltip("Text on the record button")]
        private TextMeshProUGUI recordButtonText;

        [SerializeField]
        [Tooltip("Text on the playback button")]
        private TextMeshProUGUI playbackButtonText;

        [Header("UI Panels")]
        [SerializeField]
        [Tooltip("Panel shown during recording mode")]
        private GameObject recordingPanel;

        [SerializeField]
        [Tooltip("Panel shown during playback mode")]
        private GameObject playbackPanel;

        [SerializeField]
        [Tooltip("Panel shown in idle mode")]
        private GameObject idlePanel;

        private bool isRecording = false;
        private bool isPlaybackActive = false;
        private RecordingData currentRecording;

        private void Start()
        {
            // Find managers if not assigned
            if (recordingManager == null)
            {
                recordingManager = FindObjectOfType<InteractionRecordingManager>();
            }

            if (playbackManager == null)
            {
                playbackManager = FindObjectOfType<InteractionPlaybackManager>();
            }

            // Setup button listeners
            if (recordButton != null)
            {
                recordButton.onClick.AddListener(OnRecordButtonClicked);
            }

            if (playbackButton != null)
            {
                playbackButton.onClick.AddListener(OnPlaybackButtonClicked);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(OnResetButtonClicked);
            }

            // Subscribe to manager events
            if (recordingManager != null)
            {
                recordingManager.OnRecordingStarted += OnRecordingStarted;
                recordingManager.OnRecordingStopped += OnRecordingStopped;
                recordingManager.OnRecordingProgress += OnRecordingProgress;
            }

            if (playbackManager != null)
            {
                playbackManager.OnPlaybackStarted += OnPlaybackStarted;
                playbackManager.OnPlaybackStopped += OnPlaybackStopped;
                playbackManager.OnObjectHighlighted += OnObjectHighlighted;
                playbackManager.OnObjectInteractionCompleted += OnObjectInteractionCompleted;
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
                recordingManager.OnRecordingProgress -= OnRecordingProgress;
            }

            if (playbackManager != null)
            {
                playbackManager.OnPlaybackStarted -= OnPlaybackStarted;
                playbackManager.OnPlaybackStopped -= OnPlaybackStopped;
                playbackManager.OnObjectHighlighted -= OnObjectHighlighted;
                playbackManager.OnObjectInteractionCompleted -= OnObjectInteractionCompleted;
            }
        }

        private void Update()
        {
            // Update UI text elements
            UpdateStatusText();
            UpdateProgressText();
        }

        /// <summary>
        /// Called when record button is clicked
        /// </summary>
        private void OnRecordButtonClicked()
        {
            if (recordingManager == null) return;

            if (isRecording)
            {
                // Stop recording
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
                    playbackManager.StartPlayback(currentRecording);
                    isPlaybackActive = true;
                }
                else
                {
                    Debug.LogWarning("InteractionRecordingUIController: No recording available to play back!");
                    if (instructionText != null)
                    {
                        instructionText.text = "No recording available. Please record an interaction first.";
                    }
                }
            }

            UpdateUIState();
        }

        /// <summary>
        /// Called when reset button is clicked
        /// </summary>
        private void OnResetButtonClicked()
        {
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

            // Reset objects
            ObjectStateManager objectStateManager = FindObjectOfType<ObjectStateManager>();
            if (objectStateManager != null)
            {
                objectStateManager.ResetAllObjects();
            }

            // Clear visual cues
            VisualCueManager visualCueManager = FindObjectOfType<VisualCueManager>();
            if (visualCueManager != null)
            {
                visualCueManager.ClearAllHighlights();
                visualCueManager.HideAllGhosts();
            }

            currentRecording = null;
            UpdateUIState();
        }

        /// <summary>
        /// Updates the UI state based on current mode
        /// </summary>
        private void UpdateUIState()
        {
            // Update button states
            if (recordButton != null)
            {
                recordButton.interactable = !isPlaybackActive;
            }

            if (playbackButton != null)
            {
                playbackButton.interactable = !isRecording && currentRecording != null;
            }

            if (resetButton != null)
            {
                resetButton.interactable = true; // Always available
            }

            // Update button text
            if (recordButtonText != null)
            {
                recordButtonText.text = isRecording ? "Stop Recording" : "Start Recording";
            }

            if (playbackButtonText != null)
            {
                playbackButtonText.text = isPlaybackActive ? "Stop Playback" : "Start Playback";
            }

            // Update panel visibility
            if (recordingPanel != null)
            {
                recordingPanel.SetActive(isRecording);
            }

            if (playbackPanel != null)
            {
                playbackPanel.SetActive(isPlaybackActive);
            }

            if (idlePanel != null)
            {
                idlePanel.SetActive(!isRecording && !isPlaybackActive);
            }

            // Update instruction text
            if (instructionText != null)
            {
                if (isRecording)
                {
                    instructionText.text = "Recording in progress... Interact with objects to record their movements.";
                }
                else if (isPlaybackActive)
                {
                    instructionText.text = "Playback active. Pick up the highlighted object and place it at the green ghost location.";
                }
                else
                {
                    instructionText.text = "Ready. Start recording to capture an interaction, or play back a recorded interaction.";
                }
            }
        }

        /// <summary>
        /// Updates the status text
        /// </summary>
        private void UpdateStatusText()
        {
            if (statusText == null) return;

            if (isRecording)
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

        /// <summary>
        /// Updates the progress text
        /// </summary>
        private void UpdateProgressText()
        {
            if (progressText == null) return;

            if (isRecording && recordingManager != null)
            {
                float duration = recordingManager.CurrentRecordingDuration;
                progressText.text = $"Recording: {duration:F1}s";
            }
            else if (isPlaybackActive && playbackManager != null)
            {
                progressText.text = "Playback: Active";
            }
            else
            {
                progressText.text = "";
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
            isRecording = false;
            UpdateUIState();
        }

        private void OnRecordingProgress(float duration)
        {
            // Progress is updated in Update()
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
            if (instructionText != null)
            {
                instructionText.text = "Object placed! Waiting for next interaction...";
            }
        }
    }
}

