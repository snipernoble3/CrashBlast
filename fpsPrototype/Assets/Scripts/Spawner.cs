using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour {

    public GameObject spawnObject;
    public int amountOnAwake = 10;
    public float spawnDistance = 5f;
    public float spawnDelay = 10f;
    public float spawnInterval = 5f;

    private void Awake () {
        for (int i = 0; i < amountOnAwake; i++) {
            Spawn();
        }

        InvokeRepeating("Spawn", spawnDelay, spawnInterval);

    }

    void Spawn () {
        if (!gameObject.activeInHierarchy) {
            CancelInvoke("Spawn");
            return;
        }
        Vector2 r = Random.insideUnitCircle * spawnDistance;
        Vector3 loc = transform.position - new Vector3(r.x, 0f, r.y);
        Instantiate(spawnObject, loc, spawnObject.transform.rotation);
    }


}
