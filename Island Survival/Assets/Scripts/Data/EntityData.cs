using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="New Entity Data",menuName = "Terrain Generator/Data/Entity Data", order = 2)]
public class EntityData : ScriptableObject{
    public string entityType;
    public float minHeight;
    public float maxHeight;
    public float minSeperationDistance;
    public int seed;
    // [Range(0,1)] public float density;
    // public GameObject[] gameObjects;
    public terrainObject[] terrainObjects;
}
