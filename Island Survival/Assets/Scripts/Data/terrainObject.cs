using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="New Terrain Object",menuName = "Terrain Generator/Data/Terrain Object", order = 1)]
public class terrainObject : ScriptableObject{
    public GameObject[] gameObjects;
    public int seed;
    [Range(0,100)] public float objectsPerChunk;
}
