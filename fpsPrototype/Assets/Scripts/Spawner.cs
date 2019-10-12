using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour {

    public GameObject spawn;
    public int amount = 10;
    public float spawnDistance = 5f;

    private void Awake () {
        for (int i = 0; i < amount; i++) {
            Vector3 loc = transform.position - Random.insideUnitSphere * spawnDistance;
            Instantiate(spawn, loc, spawn.transform.rotation);
        }
        
    }




}
