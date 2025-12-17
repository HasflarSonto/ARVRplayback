using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRInteractionRecording
{
    /// <summary>
    /// Data structure for task instructions (JSON format)
    /// Represents the playback sequence as PickUp/PutDown steps
    /// </summary>
    [Serializable]
    public class TaskInstruction
    {
        public string taskName;
        public string version = "1.0";
        public string createdAt;
        public string lastModified;
        public float totalDuration;
        public List<InstructionStep> steps = new List<InstructionStep>();

        public TaskInstruction()
        {
            taskName = "Untitled Task";
            version = "1.0";
            createdAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            lastModified = createdAt;
            totalDuration = 0f;
            steps = new List<InstructionStep>();
        }
    }

    /// <summary>
    /// Represents a single instruction step (PickUp, PutDown, or Move)
    /// </summary>
    [Serializable]
    public class InstructionStep
    {
        public int stepNumber;
        public string action; // "PickUp", "PutDown", or "Move"
        public string objectId;
        public string objectName;
        public float timestamp;

        // For PutDown actions (using classes instead of nullable for JsonUtility compatibility)
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
        public SerializableTolerance tolerance;

        // For Move actions (duration-based events)
        public float startTime;
        public float endTime;
        public List<SerializableVector3> pathPoints; // Waypoints for movement validation

        // Helper to check if this is a PutDown with position data
        public bool HasPositionData()
        {
            return position != null && rotation != null;
        }

        // Helper to check if this is a Move action
        public bool IsMove()
        {
            return action == "Move";
        }

        // Helper to check if Move has path data
        public bool HasPathData()
        {
            return pathPoints != null && pathPoints.Count > 0;
        }

        public InstructionStep()
        {
            stepNumber = 0;
            action = "";
            objectId = "";
            objectName = "";
            timestamp = 0f;
            position = null;
            rotation = null;
            tolerance = null;
            startTime = 0f;
            endTime = 0f;
            pathPoints = null;
        }

        // Helper method to clear position data (for PickUp steps)
        public void ClearPositionData()
        {
            position = null;
            rotation = null;
            tolerance = null;
        }

        // Helper method to clear Move data
        public void ClearMoveData()
        {
            startTime = 0f;
            endTime = 0f;
            pathPoints = null;
        }
    }

    /// <summary>
    /// Serializable Vector3 for JSON
    /// </summary>
    [Serializable]
    public class SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3() { }

        public SerializableVector3(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// Serializable Quaternion for JSON
    /// </summary>
    [Serializable]
    public class SerializableQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SerializableQuaternion() { }

        public SerializableQuaternion(Quaternion q)
        {
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }
    }

    /// <summary>
    /// Placement tolerance settings
    /// </summary>
    [Serializable]
    public class SerializableTolerance
    {
        public float distance;
        public float rotation;

        public SerializableTolerance() { }

        public SerializableTolerance(float dist, float rot)
        {
            distance = dist;
            rotation = rot;
        }
    }
}

