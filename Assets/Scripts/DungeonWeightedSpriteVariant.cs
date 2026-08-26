using System;
using UnityEngine;

[Serializable]
public sealed class DungeonWeightedSpriteVariant
{
    [SerializeField] Sprite sprite;
    [Min(0f), SerializeField] float weight = 1f;

    public Sprite Sprite => sprite;
    public float Weight => weight;
}
