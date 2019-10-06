using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RifleTest : MonoBehaviour {

    public KeyCode firingKey = KeyCode.Mouse0;

    public int maxAmmo;
    private int currAmmo;

    //halo assault rifle ref: 10.8 shots per sec, 905 m/s velocity, 300 m "effective range"
    public float firerate;
    private float timeSinceFiring;
    private int sprayCount;
    public float bulletVelocity;
    public float maxRange;

    public LayerMask bulletCollision;

    public GameObject bulletPrefab;
    public GameObject firingPosition;
    public GameObject spreadDisplay;

    //audio stuff and particle effects


    
    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        
    }

    private void Fire () {

    }

    private void Reload () {

    }

    private Vector3 randomSprayCalculation () {
        Vector3 position = new Vector3(0,0,0);

        return position;
    }

}
