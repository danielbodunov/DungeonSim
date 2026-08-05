using System;
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

[Serializable]
public class BakedPropSocket
{
    public string structureId;
    public string laneId = "Default";
    [Min(0f)] public float selectionWeight = 1f;
    public PropSocketRole role;
    public PropSocketDirection direction;
    public Vector3 localPosition;
    public Quaternion localRotation = Quaternion.identity;
}
