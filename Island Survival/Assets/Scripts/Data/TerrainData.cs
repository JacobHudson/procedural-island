using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="New Terrain Data",menuName = "Terrain Generator/Data/Terrain Data", order = 3)]
public class TerrainData : UpdatableData
{
    public float uniformScale = 1f;

    public bool useFlatShading;
    public bool useFalloff;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    

}
