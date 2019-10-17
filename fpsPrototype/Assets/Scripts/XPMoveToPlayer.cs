using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XPMoveToPlayer : MonoBehaviour {

    private GameObject target;

    private void Awake () {
        target = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update () {
        transform.position = Vector3.Lerp(transform.position, target.transform.position + Vector3.up, 0.05f);
    }


}
