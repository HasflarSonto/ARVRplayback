using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRInteractionRecording
{
    /// <summary>
    /// Serializable data structure for storing recorded interactions
    /// </summary>
    [Serializable]
    public class RecordingData
    {
        [SerializeField]
        public float recordingDuration;

        [SerializeField]
        public List<ObjectInitialState> initialStates = new List<ObjectInitialState>();

        [SerializeField]
        public List<InteractionEvent> interactionEvents = new List<InteractionEvent>();

        [SerializeField]
        public List<TransformSnapshot> transformSnapshots = new List<TransformSnapshot>();

        public RecordingData()
        {
            recordingDuration = 0f;
            initialStates = new List<ObjectInitialState>();
            interactionEvents = new List<InteractionEvent>();
            transformSnapshots = new List<TransformSnapshot>();
        }
    }

    /// <summary>
    /// Stores the initial state of an object before recording
    /// </summary>
    [Serializable]
    public class ObjectInitialState
    {
        [SerializeField]
        public string objectId;

        [SerializeField]
        public Vector3 position;

        [SerializeField]
        public Quaternion rotation;

        [SerializeField]
        public Vector3 scale;

        public ObjectInitialState(string id, Vector3 pos, Quaternion rot, Vector3 scl)
        {
            objectId = id;
            position = pos;
            rotation = rot;
            scale = scl;
        }
    }

    /// <summary>
    /// Represents an interaction event (grab or release)
    /// </summary>
    [Serializable]
    public class InteractionEvent
    {
        [SerializeField]
        public string objectId;

        [SerializeField]
        public InteractionEventType eventType;

        [SerializeField]
        public float timestamp;

        [SerializeField]
        public Vector3 position;

        [SerializeField]
        public Quaternion rotation;

        public InteractionEvent(string id, InteractionEventType type, float time, Vector3 pos, Quaternion rot)
        {
            objectId = id;
            eventType = type;
            timestamp = time;
            position = pos;
            rotation = rot;
        }
    }

    /// <summary>
    /// Type of interaction event
    /// </summary>
    public enum InteractionEventType
    {
        Grab,
        Release
    }

    /// <summary>
    /// Snapshot of an object's transform at a specific time
    /// </summary>
    [Serializable]
    public class TransformSnapshot
    {
        [SerializeField]
        public string objectId;

        [SerializeField]
        public float timestamp;

        [SerializeField]
        public Vector3 position;

        [SerializeField]
        public Quaternion rotation;

        [SerializeField]
        public Vector3 scale;

        public TransformSnapshot(string id, float time, Vector3 pos, Quaternion rot, Vector3 scl)
        {
            objectId = id;
            timestamp = time;
            position = pos;
            rotation = rot;
            scale = scl;
        }
    }
}

