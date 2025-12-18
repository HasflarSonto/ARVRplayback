using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace VRInteractionRecording
{
    /// <summary>
    /// Simple on-screen debug console for VR that captures Debug.LogError messages
    /// </summary>
    public class VRDebugConsole : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Text component to display logs")]
        private TextMeshProUGUI consoleText;

        [SerializeField]
        [Tooltip("Maximum number of log lines to keep")]
        private int maxLines = 50;

        [SerializeField]
        [Tooltip("Show only Debug.LogError messages (recommended for VR)")]
        private bool errorsOnly = true;

        private Queue<string> logQueue = new Queue<string>();
        private bool updateNeeded = false;

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            // Only show errors if errorsOnly is true
            if (errorsOnly && type != LogType.Error)
            {
                return;
            }

            // Format the log with type prefix
            string prefix = type switch
            {
                LogType.Error => "[ERROR]",
                LogType.Warning => "[WARN]",
                LogType.Log => "[LOG]",
                LogType.Exception => "[EXCEPTION]",
                LogType.Assert => "[ASSERT]",
                _ => ""
            };

            string formattedLog = $"{prefix} {logString}";

            // Add to queue
            logQueue.Enqueue(formattedLog);

            // Remove oldest if exceeds max
            while (logQueue.Count > maxLines)
            {
                logQueue.Dequeue();
            }

            updateNeeded = true;
        }

        private void Update()
        {
            if (updateNeeded && consoleText != null)
            {
                // Update text display
                consoleText.text = string.Join("\n", logQueue);
                updateNeeded = false;
            }
        }

        /// <summary>
        /// Clears all logs from the console
        /// </summary>
        public void ClearConsole()
        {
            logQueue.Clear();
            if (consoleText != null)
            {
                consoleText.text = "";
            }
        }
    }
}
