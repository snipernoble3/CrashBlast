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
    private float shotCount;
    private float timeToReduce = 0f;

    private bool reloading;
	public Animator firstPersonArms_Animator;
    public GameObject firingPosition;
    public GameObject target;
    public GameObject endOfGun;

    private float range = 150f;
    private Vector3[] linePositions = new Vector3[2];

    // Start is called before the first frame update
    void Start() {
        currAmmo = maxAmmo;

        if (target.activeInHierarchy) {
            target.transform.localScale = new Vector3(maxDeviation * 2, target.transform.localScale.y, maxDeviation * 2);
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

        if (Input.GetKey(KeyCode.R) && !reloading && currAmmo != maxAmmo) {
            firstPersonArms_Animator.Play("FirstPersonArms_Reload", 0, 0.0f); // Play the reload animation.
			StartCoroutine(Reload());
        }
        

    }

    private void LateUpdate () {
        if (timeToReduce > 0 && shotCount > 0) {
            timeToReduce -= Time.fixedDeltaTime;
        }

        if (!Input.GetMouseButton(0) && timeToReduce <= 0 && !reloading) {
            shotCount = Mathf.Clamp(shotCount - 0.75f, 0, maxAmmo);
            timeToReduce = 0.1f;
            UpdateSpread();
        }
    }


    void Fire () {

        if (currAmmo == 0) {
            return;
        }

        shotCount++;

        UpdateSpread();

        Vector3 deviation3D = Random.insideUnitCircle * maxDeviation;
        Quaternion rot = Quaternion.LookRotation(Vector3.forward * deviationDistance + deviation3D);
        Vector3 forwardVector = Camera.main.transform.rotation * rot * Vector3.forward;

        RaycastHit hit;

        if (Physics.Raycast(firingPosition.transform.position, forwardVector, out hit, range)) {
            //Debug.Log("Hit something");
            //Debug.DrawRay(firingPosition.transform.position, forwardVector * range, Color.green, 2f);

            if (hit.collider.gameObject.GetComponent<Health>() != null && hit.collider.gameObject.GetComponent<Health>().GetTag() != Health.Tag.Ally) {
                hit.collider.gameObject.GetComponent<Health>().TakeDamage(1);
            }

        }

        StartCoroutine(Laser(hit));

        currAmmo--;

    }

    void UpdateSpread () {
        if (shotCount == 0) {
            maxDeviation = 0.001f;
        } else if (shotCount < 3) {
            maxDeviation = (-.00738f * shotCount * shotCount * shotCount) + (.0999f * shotCount * shotCount) - (.1799f * shotCount) + .0873f;
        } else if (shotCount < 10) {
            maxDeviation = (.0016f * shotCount * shotCount * shotCount) - (.0299f * shotCount * shotCount) + (.2467f * shotCount) - .3092f;
        } else {
            maxDeviation = Mathf.Clamp(1f + (.25f * (shotCount - 10)), 1f, 2f);
        }

        if (target.activeInHierarchy) {
            target.transform.localScale = new Vector3(Mathf.Max( 0.25f, maxDeviation * 2), target.transform.localScale.y, Mathf.Max(0.25f, maxDeviation * 2));
        }
    }

    IEnumerator Laser (RaycastHit hit) {
        linePositions[0] = endOfGun.transform.position;
        linePositions[1] = hit.point;

        GetComponent<LineRenderer>().enabled = true;
        GetComponent<LineRenderer>().SetPositions(linePositions);
        yield return new WaitForSeconds(0.01f);
        GetComponent<LineRenderer>().enabled = false;
    }

    IEnumerator Reload () {
        reloading = true;

        yield return new WaitForSeconds(1f); //reload time
        
        currAmmo = maxAmmo;
        reloading = false;
    }
    
}
