using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntitySpawner : MonoBehaviour{
    public static void SpawnValidator(Transform transform){
        var validator = new GameObject("Terrain Entity Validator");
        validator.transform.position = new Vector3(transform.position.x, 50, transform.position.z);
        validator.transform.parent = transform;

        SpawnEntityValidation spawnEntityValidation = validator.AddComponent<SpawnEntityValidation>();
        spawnEntityValidation.PassDataForEntitySpawning(validator, transform.gameObject);
    }
}
