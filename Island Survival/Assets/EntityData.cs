using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class EntityData : ScriptableObject{
    public string entityType;
    [Range(0,1)]
    public float density;

    public float minHeight;
    public float maxHeight;
    public float minSeperationDistance;
    public GameObject[] gameObjects;
}
