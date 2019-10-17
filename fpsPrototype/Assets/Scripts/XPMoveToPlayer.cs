using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XPMoveToPlayer : MonoBehaviour {

    public GameObject target;

    private void Update () {
        transform.position = Vector3.Lerp(transform.position, target.transform.position, 0.1f);
    }


}
