using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnEntityValidation : MonoBehaviour
{
    RaycastHit hit;
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
        yield return new WaitForSeconds(0.5f);
        amountOfEntitiesToSpawn = Mathf.RoundToInt(entityData.density * MapGenerator.mapChunkSize); // finds amount of objects to spawn
        Vector3[] validPositions = FindValidPosition();
        print($"Entities Per Chunk: {amountOfEntitiesToSpawn}");

        while (amountOfEntitiesToSpawn > 0){
            GameObject objToSpawn = entityData.gameObjects[Random.Range(0, entityData.gameObjects.Length)]; // get random GO from list to spawn
            print(objToSpawn.name);

            // var groundPoint = new GameObject();
            // groundPoint.transform.SetParent(parentTerrain.transform);
            // groundPoint.transform.localPosition = validPositions[amountOfEntitiesToSpawn - 1];

            var entity = Instantiate(objToSpawn, validPositions[amountOfEntitiesToSpawn - 1], Quaternion.identity, parentTerrain.transform);
            entity.transform.localPosition = validPositions[amountOfEntitiesToSpawn - 1];
            //entity.transform.localPosition = new Vector3(entity.transform.localPosition.x, entity.transform.localPosition.y, entity.transform.localPosition.z);


            //print(validPositions[amountOfEntitiesToSpawn - 1]);
            //Destroy(groundPoint, 1f);
            
            amountOfEntitiesToSpawn--;
        }
    }

    Vector3[] FindValidPosition(){ // gets local postitions meeting criteria
        Vector3[] positionsOnMesh = new Vector3[amountOfEntitiesToSpawn];
        Vector3[] positionsOnMeshGrounded = new Vector3[positionsOnMesh.Length];
        for (int i = 0; i < amountOfEntitiesToSpawn; i++){
            var pos = GetRandomPointOnMesh(parentTerrain.GetComponent<MeshFilter>().sharedMesh);
            positionsOnMesh[i] = pos;
        }
        
        for (int i = 0; i < positionsOnMesh.Length; i++){
            var groundCheckObj = new GameObject();
            groundCheckObj.transform.parent = parentTerrain.transform;
            groundCheckObj.transform.position = new Vector3(positionsOnMesh[i].x, 50, positionsOnMesh[i].z);
            
            RaycastHit hit;
            if(Physics.Raycast(groundCheckObj.transform.position, Vector3.down, out hit, Mathf.Infinity)){
                positionsOnMeshGrounded[i] = hit.point;
                //Debug.DrawLine(groundCheckObj.transform.position, hit.point, Color.green, 100f);
            }

            Destroy(groundCheckObj);
        }

        
        print($"Chunk, {parentTerrain.name + parentTerrain.GetInstanceID().ToString()}, has the calculated the positions: {positionsOnMesh}");
        return positionsOnMeshGrounded;
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
        for (int i = 0; i < triCount; i++)
        {
            sizes[i] = .5f*Vector3.Cross(verts[tris[i*3 + 1]] - verts[tris[i*3]], verts[tris[i*3 + 2]] - verts[tris[i*3]]).magnitude;
        }
        print(sizes);
        return sizes;
    }
}
