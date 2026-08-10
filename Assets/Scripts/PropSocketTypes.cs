using System;
using System.Collections.Generic;
using UnityEngine;

public enum PropSocketRole
{
    Start,
    Continue,
    End,
    Single
}

public enum PropSocketDirection
{
    North,
    East,
    South,
    West
}

public enum PropSocketPlatformPolicy
{
    Automatic,
    ManualOnly
}

[Serializable]
public class BakedPropSocket
{
    public string structureId;
    public string laneId = "Default";
    public List<string> compatibleLaneIds = new();
    public string bundleId = "Default";
    [Min(0f)] public float selectionWeight = 1f;
    public PropSocketRole role;
    public PropSocketDirection direction;
    public bool allowsTraversalExit;
    public PropSocketPlatformPolicy platformPolicy;
    public Vector3 localPosition;
    public Quaternion localRotation = Quaternion.identity;
}

[Serializable]
public class GeneratedStructurePiece
{
    public Vector2Int cell;
    public string tileProfileId;
    public PropSocketRole role;
    public string laneId;
    public string bundleId;
    public bool hasTraversalExit;
    public Vector3 worldPosition;
    public BakedPropSocket socket;
}

[Serializable]
public class GeneratedTraversalEndpoint
{
    public Vector2Int cell;
    public Vector3 worldPosition;
    public PropSocketRole sourceRole;
    public bool isIntermediate;
}

[Serializable]
public class GeneratedStructureRun
{
    public string structureId;
    public int generationVersion;
    public List<GeneratedStructurePiece> pieces = new();
    public List<GeneratedTraversalEndpoint> traversalEndpoints = new();
}
