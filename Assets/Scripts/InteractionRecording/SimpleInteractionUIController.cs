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

        private bool isRecording = false;
        private bool isPlaybackActive = false;
        private RecordingData currentRecording;

        // Button components (will be found automatically)
        private Button recordButtonComponent;
        private Button playbackButtonComponent;
        private Button resetButtonComponent;

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

            // Find button components
            SetupButtons();

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
        }

        /// <summary>
        /// Called when record button is clicked
        /// </summary>
        private void OnRecordButtonClicked()
        {
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
            // Update button interactability
            if (recordButtonComponent != null)
            {
                recordButtonComponent.interactable = !isPlaybackActive;
            }

            if (playbackButtonComponent != null)
            {
                playbackButtonComponent.interactable = !isRecording && currentRecording != null;
            }

            if (resetButtonComponent != null)
            {
                resetButtonComponent.interactable = true; // Always available
            }

            // Update button text (if assigned)
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

            // Update status text
            if (statusText != null)
            {
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

            // Update instruction text
            if (instructionText != null)
            {
                if (isRecording)
                {
                    instructionText.text = "Recording... Grab an object, move it, and release it.";
                }
                else if (isPlaybackActive)
                {
                    instructionText.text = "Playback active. Pick up the highlighted object and place it at the green ghost location.";
                }
                else
                {
                    instructionText.text = "Ready. Press Record to capture a single interaction (grab, move, release).";
                }
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
            if (recordingManager != null)
            {
                currentRecording = recordingManager.GetCurrentRecording();
            }
            UpdateUIState();
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
                instructionText.text = "Interaction completed! Press Reset to try again.";
            }
        }
    }
}

