using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnEntityValidation : MonoBehaviour
{
    GameObject validator;
    GameObject parentTerrain;
    static MapGenerator mapGenerator;
    int amountOfEntitiesToSpawn;

    private Vector3 randomPoint;

    void Awake(){
        mapGenerator = FindObjectOfType<MapGenerator>();

        var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        obj.name = "Validator";
        obj.transform.parent = validator.transform;
        obj.transform.localPosition = Vector3.zero;
        obj.GetComponent<Renderer>().material.color = Random.ColorHSV(0,1,0,1);

        StartCoroutine(SpawnEntities(mapGenerator.entityData));
    }

    public void PassDataForEntitySpawning(GameObject validatorObj, GameObject terrainMeshObj){
        parentTerrain = terrainMeshObj;
        validator = validatorObj;
    }

    public IEnumerator SpawnEntities(EntityData entityData){

        for (int i = 0; i < entityData.terrainObjects.Length; i++){
            yield return new WaitForSeconds(0.5f);
            amountOfEntitiesToSpawn = Mathf.RoundToInt((entityData.terrainObjects[i].objectsPerChunk / 100) * MapGenerator.mapChunkSize); // finds amount of objects to spawn
            Vector3[] validPositions = FindValidPosition();

            while (amountOfEntitiesToSpawn > 0){
                GameObject objToSpawn = entityData.terrainObjects[i].gameObjects[Random.Range(0, entityData.terrainObjects[i].gameObjects.Length)]; // get random GO from list to spawn
                print(objToSpawn.name);

                var entity = Instantiate(objToSpawn, Vector3.zero, Quaternion.identity, parentTerrain.transform);
                entity.transform.localPosition = validPositions[amountOfEntitiesToSpawn - 1];
                
                amountOfEntitiesToSpawn--;
            }
        }


        Destroy(this.gameObject);
    }

    Vector3[] FindValidPosition(){
        Vector3[] positions = new Vector3[amountOfEntitiesToSpawn];
        
        float minY = mapGenerator.entityData.minHeight * (mapGenerator.terrainData.meshHeightCurve.Evaluate(mapGenerator.entityData.minHeight) * mapGenerator.terrainData.meshHeightMultiplier) * 10;
        float maxY = mapGenerator.entityData.maxHeight * (mapGenerator.terrainData.meshHeightCurve.Evaluate(mapGenerator.entityData.maxHeight) * mapGenerator.terrainData.meshHeightMultiplier) * 10;
        print($"minY: {minY}, maxY: {maxY}");

        int positionsToFind = positions.Length;
        while (positionsToFind > 0){
            bool withinHeightRange = false;
            // bool withinMinDistanceRange = false;
            var pos = GetRandomPointOnMesh(parentTerrain.GetComponent<MeshFilter>().sharedMesh);

            if(pos.y > minY && pos.y < maxY){ // ensures that the entity is within the height range
                withinHeightRange = true;
            }

            // for (int i = 0; i < positions.Length; i++){   
            //     if(Vector3.Distance(positions[i], pos) > mapGenerator.entityData.minSeperationDistance * mapGenerator.terrainData.uniformScale * 2){
            //         print(Vector3.Distance(positions[i], pos));
            //         withinMinDistanceRange = true;
            //     }
            // }

            if(withinHeightRange /*&& withinMinDistanceRange*/){
                positions[positionsToFind - 1] = pos;
                positionsToFind--;
            }
        }

        return positions;
    }

    Vector3 GetRandomPointOnMesh(Mesh mesh)
    {
        //if you're repeatedly doing this on a single mesh, you'll likely want to cache cumulativeSizes and total
        float[] sizes = GetTriSizes(mesh.triangles, mesh.vertices);
        float[] cumulativeSizes = new float[sizes.Length];
        float total = 0;

        for (int i = 0; i < sizes.Length; i++)
        {
            total += sizes[i];
            cumulativeSizes[i] = total;
        }

        //so everything above this point wants to be factored out

        float randomsample = Random.value* total;

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

        float r = Random.value;
        float s = Random.value;

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
