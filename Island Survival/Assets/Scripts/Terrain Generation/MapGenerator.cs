using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour{

    public enum DrawMode{noiseMap, colourMap, Mesh, FalloffMap};
    public DrawMode drawMode;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public EntityData entityData;

    [Range(0,6)]
    public int EditorPreviewLOD;

    public bool autoUpdate;

    public bool spawnEntities;

    public TerrainType[] regions;
    public static MapGenerator instance;

    float[,] falloffMap;
    bool falloffMapGenerated;

    [Header("Chunk Spawning Stats")]
    public int chunksDoneSpawning;
    public int initialChunksSpawned = 109;
    public int initialChunkSpawnProgress;
    public bool initialEntitiesSpawned = false;
    public string currentObjectSpawningName;
    public int spawnedEntityCount;
    
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    void Awake(){
        GenerateFalloffMap();
        instance = this;
    }

    void OnValuesUpdated(){
        if(!Application.isPlaying){
            DrawMapInEditor();
        }
    }

    public static int mapChunkSize{
        get{
            if(!instance){
                instance = FindObjectOfType<MapGenerator>();
            }

            if(instance.terrainData.useFlatShading){
                return 95;
            } else{
                return 239;
            }
        }
    }

    void GenerateFalloffMap(){
        if(terrainData.useFalloff && !falloffMapGenerated){ 
            falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
            falloffMapGenerated = true;
        }
    }

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
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, EditorPreviewLOD, terrainData.useFlatShading), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
                break;
            case DrawMode.FalloffMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
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
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
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
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++){
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if(!initialEntitiesSpawned){
            initialChunkSpawnProgress = Mathf.RoundToInt((chunksDoneSpawning / initialChunksSpawned) * 100f);
            if(chunksDoneSpawning >= initialChunksSpawned){
                initialEntitiesSpawned = true;
            }
            //print($"Chunks Spawned Percent: {chunksDoneSpawning / initialChunksSpawned * 100}%");
        }

    }

    MapData GenerateMapData(Vector2 center){
        GenerateFalloffMap();

        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, center + noiseData.offset, noiseData.normalizeMode);

        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize + 2; y++){
            for (int x = 0; x < mapChunkSize + 2; x++){
                if(terrainData.useFalloff){
                    noiseMap[x,y] = Mathf.Clamp01(noiseMap[x,y] - falloffMap[x,y]);
                }

                if(x < mapChunkSize && y < mapChunkSize){
                    float currentHeight = noiseMap[x,y];
                    for (int i = 0; i < regions.Length; i++){
                        if(currentHeight >= regions[i].height){
                            colourMap[y * mapChunkSize + x] = regions[i].colour;
                        }else{
                            break;
                        }
                    }
                }
                
            }
        }

        return new MapData(noiseMap, colourMap);
    }

    void OnValidate()
    {   
        if(terrainData != null){
            terrainData.OnValuesUpdated -= OnValuesUpdated; // prevents us form subbscribing to same event multiple times
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }
        if(noiseData != null){
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }

        falloffMapGenerated = false;
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

