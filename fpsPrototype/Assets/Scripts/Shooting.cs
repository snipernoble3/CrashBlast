using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : MonoBehaviour
{

    private float maxDeviation = 0.001f;
    public float deviationDistance = 25f;
    public int maxAmmo = 50;
    private int currAmmo;

    private float fireRate = 0.1f;
    private float timeToFire = 0f;

    public GameObject firingPosition;
    public GameObject target;
    public GameObject endOfGun;

    private float range = 150f;
    private Vector3[] linePositions = new Vector3[2];

    // Start is called before the first frame update
    void Start()
    {
        currAmmo = maxAmmo;

        if (target.activeInHierarchy) {
            target.transform.localScale = new Vector3(maxDeviation, target.transform.localScale.y, maxDeviation);
        }

    }

    // Update is called once per frame
    void FixedUpdate()
    {

        //Debug.DrawRay(firingPosition.transform.position, firingPosition.transform.forward * range, Color.red, 2f);

        if (timeToFire > 0) {
            timeToFire -= Time.fixedDeltaTime;
        }

        if (Input.GetMouseButton(0) && timeToFire <= 0) {
            Fire();
            timeToFire = fireRate;
        }
    }

    void Fire () {

        if (currAmmo == 0) {
            return;
        }

        int shotsFired = maxAmmo - currAmmo;

        if (shotsFired == 0) {
            maxDeviation = 0.001f;
        } else if (shotsFired < 3) {
            maxDeviation = (-.00738f * shotsFired * shotsFired * shotsFired) + (.0999f * shotsFired * shotsFired) - (.1799f * shotsFired) + .0873f;
        } else if (shotsFired < 10) {
            maxDeviation = (.0016f * shotsFired * shotsFired * shotsFired) - (.0299f * shotsFired * shotsFired) + (.2467f * shotsFired) - .3092f;
        } else {
            maxDeviation = Mathf.Clamp(1f + (.25f * (shotsFired - 10)), 1f, 2f);
        }

        if (target.activeInHierarchy) {
            target.transform.localScale = new Vector3(maxDeviation * 2, target.transform.localScale.y, maxDeviation * 2);
        }

        Vector3 deviation3D = Random.insideUnitCircle * maxDeviation;
        Quaternion rot = Quaternion.LookRotation(Vector3.forward * deviationDistance + deviation3D);
        Vector3 forwardVector = Camera.main.transform.rotation * rot * Vector3.forward;

        RaycastHit hit;

        if (Physics.Raycast(firingPosition.transform.position, forwardVector, out hit, range)) {
            //Debug.Log("Hit something");
            Debug.DrawRay(firingPosition.transform.position, forwardVector * range, Color.green, 2f);
            
        }

        StartCoroutine(laser(hit));

        currAmmo--;

    }

    IEnumerator laser (RaycastHit hit) {
        linePositions[0] = endOfGun.transform.position;
        linePositions[1] = hit.point;

        GetComponent<LineRenderer>().enabled = true;
        GetComponent<LineRenderer>().SetPositions(linePositions);
        yield return new WaitForSeconds(0.05f);
        GetComponent<LineRenderer>().enabled = false;
    }


}
