using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRInteractionRecording
{
    /// <summary>
    /// Helper script to automatically find and assign UI elements
    /// Right-click the component and use context menu options
    /// </summary>
    public class UIHelper : MonoBehaviour
    {
        [ContextMenu("Auto-Find and Assign UI Elements")]
        public void AutoFindAndAssignUIElements()
        {
            InteractionRecordingUIController controller = FindObjectOfType<InteractionRecordingUIController>();
            if (controller == null)
            {
                Debug.LogError("UIHelper: InteractionRecordingUIController not found! Create it first.");
                return;
            }

            // Find buttons
            Button[] allButtons = FindObjectsOfType<Button>(true);
            Button recordButton = null;
            Button playbackButton = null;
            Button resetButton = null;

            // Try to find by name
            foreach (Button btn in allButtons)
            {
                string name = btn.name.ToLower();
                if (name.Contains("record") && recordButton == null)
                {
                    recordButton = btn;
                }
                else if (name.Contains("playback") || name.Contains("play") && playbackButton == null)
                {
                    playbackButton = btn;
                }
                else if (name.Contains("reset") && resetButton == null)
                {
                    resetButton = btn;
                }
            }

            // If not found by name, use first available buttons
            if (recordButton == null && allButtons.Length > 0)
            {
                recordButton = allButtons[0];
                Debug.LogWarning($"UIHelper: Using '{recordButton.name}' as Record Button");
            }
            if (playbackButton == null && allButtons.Length > 1)
            {
                playbackButton = allButtons[1];
                Debug.LogWarning($"UIHelper: Using '{playbackButton.name}' as Playback Button");
            }
            if (resetButton == null && allButtons.Length > 2)
            {
                resetButton = allButtons[2];
                Debug.LogWarning($"UIHelper: Using '{resetButton.name}' as Reset Button");
            }

            // Find text elements
            TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
            TextMeshProUGUI statusText = null;
            TextMeshProUGUI progressText = null;
            TextMeshProUGUI instructionText = null;

            foreach (TextMeshProUGUI text in allTexts)
            {
                string name = text.name.ToLower();
                if (name.Contains("status") && statusText == null)
                {
                    statusText = text;
                }
                else if (name.Contains("progress") || name.Contains("time") && progressText == null)
                {
                    progressText = text;
                }
                else if (name.Contains("instruction") || name.Contains("modal") && instructionText == null)
                {
                    instructionText = text;
                }
            }

            // Assign using reflection (since fields are private)
            var recordField = typeof(InteractionRecordingUIController).GetField("recordButton",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var playbackField = typeof(InteractionRecordingUIController).GetField("playbackButton",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var resetField = typeof(InteractionRecordingUIController).GetField("resetButton",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var statusTextField = typeof(InteractionRecordingUIController).GetField("statusText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var progressTextField = typeof(InteractionRecordingUIController).GetField("progressText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var instructionTextField = typeof(InteractionRecordingUIController).GetField("instructionText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (recordField != null && recordButton != null)
            {
                recordField.SetValue(controller, recordButton);
                Debug.Log($"UIHelper: Assigned '{recordButton.name}' as Record Button");
            }

            if (playbackField != null && playbackButton != null)
            {
                playbackField.SetValue(controller, playbackButton);
                Debug.Log($"UIHelper: Assigned '{playbackButton.name}' as Playback Button");
            }

            if (resetField != null && resetButton != null)
            {
                resetField.SetValue(controller, resetButton);
                Debug.Log($"UIHelper: Assigned '{resetButton.name}' as Reset Button");
            }

            if (statusTextField != null && statusText != null)
            {
                statusTextField.SetValue(controller, statusText);
                Debug.Log($"UIHelper: Assigned '{statusText.name}' as Status Text");
            }

            if (progressTextField != null && progressText != null)
            {
                progressTextField.SetValue(controller, progressText);
                Debug.Log($"UIHelper: Assigned '{progressText.name}' as Progress Text");
            }

            if (instructionTextField != null && instructionText != null)
            {
                instructionTextField.SetValue(controller, instructionText);
                Debug.Log($"UIHelper: Assigned '{instructionText.name}' as Instruction Text");
            }

            Debug.Log("UIHelper: Auto-assignment complete! Check the InteractionRecordingUIController component.");
        }

        [ContextMenu("Create Basic UI Setup")]
        public void CreateBasicUISetup()
        {
            // Find or create Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create buttons
            CreateButton(canvas.transform, "RecordButton", "Start Recording", new Vector3(-200, 100, 0));
            CreateButton(canvas.transform, "PlaybackButton", "Start Playback", new Vector3(0, 100, 0));
            CreateButton(canvas.transform, "ResetButton", "Reset", new Vector3(200, 100, 0));

            // Create text elements
            CreateText(canvas.transform, "StatusText", "Status: IDLE", new Vector3(0, 50, 0));
            CreateText(canvas.transform, "ProgressText", "", new Vector3(0, 0, 0));
            CreateText(canvas.transform, "InstructionText", "Ready to record or play back interactions.", new Vector3(0, -50, 0));

            Debug.Log("UIHelper: Created basic UI setup on Canvas. You may need to adjust positions and add VR interaction components.");
        }

        private void CreateButton(Transform parent, string name, string text, Vector3 position)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent);
            buttonObj.transform.localPosition = position;
            buttonObj.transform.localScale = Vector3.one;

            // Add RectTransform
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);

            // Add Image (button background)
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Add Button
            Button button = buttonObj.AddComponent<Button>();

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 24;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = Color.white;

            button.targetGraphic = image;
        }

        private void CreateText(Transform parent, string name, string text, Vector3 position)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent);
            textObj.transform.localPosition = position;
            textObj.transform.localScale = Vector3.one;

            RectTransform rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 50);

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 20;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = Color.white;
        }
    }
}

