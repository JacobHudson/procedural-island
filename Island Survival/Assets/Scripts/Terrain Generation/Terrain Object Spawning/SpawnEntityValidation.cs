using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnEntityValidation : MonoBehaviour
{
    GameObject validator;
    GameObject parentTerrain;
    static MapGenerator mapGenerator;
    int amountOfEntitiesToSpawn;

    void Awake(){
        mapGenerator = FindObjectOfType<MapGenerator>();

        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = "Validator";
        obj.transform.parent = validator.transform;
        obj.transform.localPosition = Vector3.zero;

        StartCoroutine(SpawnEntities(mapGenerator.entityData));
    }

    public void PassDataForEntitySpawning(GameObject validatorObj, GameObject terrainMeshObj){
        parentTerrain = terrainMeshObj;
        validator = validatorObj;
    }

    public IEnumerator SpawnEntities(EntityData entityData){
        yield return new WaitForSeconds(0.25f); // waits for the map generator to finish generating the map

        for (int i = 0; i < entityData.terrainObjects.Length; i++){
            amountOfEntitiesToSpawn = Mathf.RoundToInt((entityData.terrainObjects[i].objectsPerChunk / 100) * MapGenerator.mapChunkSize); // finds amount of objects to spawn
            int totalEntitiesToSpawn = amountOfEntitiesToSpawn;
            Vector3[] validPositions = FindValidPosition(entityData.seed * (i+2));

            System.Random randForObject = new System.Random(entityData.seed * (i+1));

            while (amountOfEntitiesToSpawn > 0){
                GameObject objToSpawn = entityData.terrainObjects[i].gameObjects[randForObject.Next(0, entityData.terrainObjects[i].gameObjects.Length)]; // get random GO from list to spawn

                var entity = Instantiate(objToSpawn, Vector3.zero, Quaternion.identity, parentTerrain.transform);
                entity.transform.localPosition = new Vector3(validPositions[amountOfEntitiesToSpawn - 1].x, validPositions[amountOfEntitiesToSpawn - 1].y - entityData.terrainObjects[i].yOffset, validPositions[amountOfEntitiesToSpawn - 1].z); // set position of entity

                mapGenerator.spawnedEntityCount = totalEntitiesToSpawn - amountOfEntitiesToSpawn;
                mapGenerator.currentObjectSpawningName = objToSpawn.name;
                amountOfEntitiesToSpawn--;
            }
        }

        mapGenerator.chunksDoneSpawning++;
        Destroy(this.gameObject);
    }

    Vector3[] FindValidPosition(int seed){
        Vector3[] positions = new Vector3[amountOfEntitiesToSpawn];
        
        float minY = mapGenerator.entityData.minHeight * (mapGenerator.terrainData.meshHeightCurve.Evaluate(mapGenerator.entityData.minHeight) * mapGenerator.terrainData.meshHeightMultiplier) * 10;
        float maxY = mapGenerator.entityData.maxHeight * (mapGenerator.terrainData.meshHeightCurve.Evaluate(mapGenerator.entityData.maxHeight) * mapGenerator.terrainData.meshHeightMultiplier) * 10;
        int positionsToFind = positions.Length;
        System.Random randForPosition = new System.Random(seed);
        while (positionsToFind > 0){
            bool withinHeightRange = false;
            // bool withinMinDistanceRange = false;
            var pos = GetRandomPointOnMesh(parentTerrain.GetComponent<MeshFilter>().sharedMesh, randForPosition.NextDouble());

            if(pos.y > minY && pos.y < maxY){ // ensures that the entity is within the height range
                withinHeightRange = true;
            }

            if(withinHeightRange){
                positions[positionsToFind - 1] = pos;
                positionsToFind--;
            }
        }

        return positions;
    }

    Vector3 GetRandomPointOnMesh(Mesh mesh, double random)
    {
        float[] sizes = GetTriSizes(mesh.triangles, mesh.vertices);
        float[] cumulativeSizes = new float[sizes.Length];
        float total = 0;


        for (int i = 0; i < sizes.Length; i++)
        {
            total += sizes[i];
            cumulativeSizes[i] = total;
        }

        float randomsample = (float)random* total;

        int triIndex = -1;
        
        for (int i = 0; i < sizes.Length; i++)
        {
            if (randomsample <= cumulativeSizes[i])
            {
                triIndex = i;
                break;
            }
        }

        if (triIndex == -1) Debug.LogError("triIndex should never be -1");

        Vector3 a = mesh.vertices[mesh.triangles[triIndex * 3]];
        Vector3 b = mesh.vertices[mesh.triangles[triIndex * 3 + 1]];
        Vector3 c = mesh.vertices[mesh.triangles[triIndex * 3 + 2]];

        //generate random barycentric coordinates

        float r = (float)random;
        float s = (float)random;

        if(r + s >=1)
        {
            r = 1 - r;
            s = 1 - s;
        }
        //and then turn them back to a Vector3
        Vector3 pointOnMesh = a + r*(b - a) + s*(c - a);
        return pointOnMesh;
    }

    float[] GetTriSizes(int[] tris, Vector3[] verts)
    {
        int triCount = tris.Length / 3;
        float[] sizes = new float[triCount];
        for (int i = 0; i < triCount; i++){
            sizes[i] = .5f*Vector3.Cross(verts[tris[i*3 + 1]] - verts[tris[i*3]], verts[tris[i*3 + 2]] - verts[tris[i*3]]).magnitude;
        }
        return sizes;
    }
}
