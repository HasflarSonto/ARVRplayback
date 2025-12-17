using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VRInteractionRecording
{
    /// <summary>
    /// Generates TaskInstruction JSON from RecordingData
    /// Converts grab/release events into PickUp/PutDown steps
    /// </summary>
    public static class TaskInstructionGenerator
    {
        /// <summary>
        /// Generates TaskInstruction from RecordingData
        /// </summary>
        public static TaskInstruction GenerateFromRecording(RecordingData recording, ObjectStateManager objectStateManager, string taskName = null)
        {
            if (recording == null || objectStateManager == null)
            {
                Debug.LogError("TaskInstructionGenerator: Recording or ObjectStateManager is null!");
                return null;
            }

            TaskInstruction task = new TaskInstruction();
            task.taskName = string.IsNullOrEmpty(taskName) ? "Untitled Task" : taskName;
            task.totalDuration = recording.recordingDuration;
            task.createdAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            task.lastModified = task.createdAt;

            // Build interaction sequences (grab-release pairs)
            Dictionary<string, InteractionEvent> pendingGrabs = new Dictionary<string, InteractionEvent>();
            List<InteractionSequence> sequences = new List<InteractionSequence>();

            foreach (InteractionEvent interactionEvent in recording.interactionEvents)
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
                        sequences.Add(sequence);
                        pendingGrabs.Remove(interactionEvent.objectId);
                    }
                }
            }

            // Convert sequences to instruction steps
            int stepNumber = 1;
            foreach (InteractionSequence sequence in sequences)
            {
                // Get object name
                GameObject obj = objectStateManager.GetObjectFromId(sequence.objectId);
                string objectName = obj != null ? obj.name : $"Object_{sequence.objectId}";

                // PickUp step
                InstructionStep pickUpStep = new InstructionStep
                {
                    stepNumber = stepNumber++,
                    action = "PickUp",
                    objectId = sequence.objectId,
                    objectName = objectName,
                    timestamp = sequence.grabEvent.timestamp
                };
                task.steps.Add(pickUpStep);

                // PutDown step
                InstructionStep putDownStep = new InstructionStep
                {
                    stepNumber = stepNumber++,
                    action = "PutDown",
                    objectId = sequence.objectId,
                    objectName = objectName,
                    timestamp = sequence.releaseEvent.timestamp
                };
                // Only set position/rotation for PutDown (not PickUp)
                putDownStep.position = new SerializableVector3(sequence.releaseEvent.position);
                putDownStep.rotation = new SerializableQuaternion(sequence.releaseEvent.rotation);
                putDownStep.tolerance = new SerializableTolerance(0.1f, 15.0f); // Default tolerance
                task.steps.Add(putDownStep);
            }

            return task;
        }

        /// <summary>
        /// Converts TaskInstruction to formatted JSON string
        /// </summary>
        public static string ToFormattedJSON(TaskInstruction task)
        {
            if (task == null) return "{}";

            // Build JSON manually to handle nullables properly
            StringBuilder json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($"  \"taskName\": \"{EscapeJSON(task.taskName)}\",");
            json.AppendLine($"  \"version\": \"{task.version}\",");
            json.AppendLine($"  \"createdAt\": \"{task.createdAt}\",");
            json.AppendLine($"  \"lastModified\": \"{task.lastModified}\",");
            json.AppendLine($"  \"totalDuration\": {task.totalDuration},");
            json.AppendLine("  \"steps\": [");

            for (int i = 0; i < task.steps.Count; i++)
            {
                InstructionStep step = task.steps[i];
                json.AppendLine("    {");
                json.AppendLine($"      \"stepNumber\": {step.stepNumber},");
                json.AppendLine($"      \"action\": \"{step.action}\",");
                json.AppendLine($"      \"objectId\": \"{step.objectId}\",");
                json.AppendLine($"      \"objectName\": \"{EscapeJSON(step.objectName)}\",");
                json.AppendLine($"      \"timestamp\": {step.timestamp}");

                // Add position/rotation for PutDown actions
                if (step.action == "PutDown" && step.position != null)
                {
                    json.AppendLine(",");
                    json.AppendLine("      \"position\": {");
                    json.AppendLine($"        \"x\": {step.position.x},");
                    json.AppendLine($"        \"y\": {step.position.y},");
                    json.AppendLine($"        \"z\": {step.position.z}");
                    json.AppendLine("      },");
                    json.AppendLine("      \"rotation\": {");
                    json.AppendLine($"        \"x\": {step.rotation.x},");
                    json.AppendLine($"        \"y\": {step.rotation.y},");
                    json.AppendLine($"        \"z\": {step.rotation.z},");
                    json.AppendLine($"        \"w\": {step.rotation.w}");
                    json.AppendLine("      }");

                    if (step.tolerance != null)
                    {
                        json.AppendLine(",");
                        json.AppendLine("      \"tolerance\": {");
                        json.AppendLine($"        \"distance\": {step.tolerance.distance},");
                        json.AppendLine($"        \"rotation\": {step.tolerance.rotation}");
                        json.AppendLine("      }");
                    }
                }

                json.AppendLine("    }");
                if (i < task.steps.Count - 1)
                {
                    json.Append(",");
                }
            }

            json.AppendLine("  ]");
            json.AppendLine("}");

            return json.ToString();
        }

        /// <summary>
        /// Escapes special characters in JSON strings
        /// </summary>
        private static string EscapeJSON(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        /// <summary>
        /// Helper class for building sequences
        /// </summary>
        private class InteractionSequence
        {
            public string objectId;
            public InteractionEvent grabEvent;
            public InteractionEvent releaseEvent;
        }
    }
}

