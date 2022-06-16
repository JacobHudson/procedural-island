using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour{

    public enum DrawMode{noiseMap, colourMap, Mesh};
    public DrawMode drawMode;
    public Noise.NormalizeMode normalizeMode;

    public const int mapChunkSize = 241;
    [Range(0,6)]
    public int EditorPreviewLOD;
    public float noiseScale;
    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;
    public int seed;
    public Vector2 offset;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;

    public TerrainType[] regions;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor(){
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        switch (drawMode)
        {
            case DrawMode.noiseMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
                break;
            case DrawMode.colourMap:
                display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
                break;
            case DrawMode.Mesh:
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, EditorPreviewLOD), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
                break;
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback){
        ThreadStart threadStart = delegate{
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start(); // starts a new thread
    }

    void MapDataThread(Vector2 center, Action<MapData> callback){
        MapData mapData = GenerateMapData(center); // runs GenerateMapData() on the new thread
        lock (mapDataThreadInfoQueue){  // can only be executed by one thread at a time
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData)); // adds mapdata and callback to queue
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback){
        ThreadStart threadStart = delegate{
            MeshDataThread(mapData, lod, callback);
        };

        new Thread (threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback){
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock(meshDataThreadInfoQueue){
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update(){
        if(mapDataThreadInfoQueue.Count > 0){
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++){
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue(); // sets threadInfo equal to the next mapData in queue
                threadInfo.callback(threadInfo.parameter); // calls the threads callback with "parameter" as the parameter
            }
        }

        if(meshDataThreadInfoQueue.Count > 0){
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData(Vector2 center){
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, center + offset, normalizeMode);

        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x,y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if(currentHeight >= regions[i].height){
                        colourMap[y * mapChunkSize + x] = regions[i].colour;
                    }else{
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colourMap);

    }

    void OnValidate()
    {
        if(lacunarity < 1)
            lacunarity = 1;
        if(octaves < 0)
            octaves = 0;
    }

    struct MapThreadInfo<T>{  // <T> making struct "generic"
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color colour;
}

public struct MapData{
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;

    public MapData(float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}

