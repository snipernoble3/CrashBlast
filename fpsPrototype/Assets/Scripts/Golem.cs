using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Golem : MonoBehaviour {

    public GameObject target;
    public GameObject projectile;
    private Rigidbody rb;

    [SerializeField] private float moveForce;
    [SerializeField] private float attackRange = 50f;
    [SerializeField] private float projectileVelocity;
    [SerializeField] private float projectileThrowRate;

    private float grabDiameter = 5f;
    private float timeSinceAttacking;

    private bool attacking;
    private bool moving;
    private float timeMoving;
    private float timeToNextMove;

    private void Awake () {
        target = GameObject.FindGameObjectWithTag("Player");

        rb = GetComponent<Rigidbody>();
    }

    private void Update () {
        if (timeSinceAttacking > 0) {
            timeSinceAttacking -= Time.deltaTime;
        }

        if (Vector3.Magnitude(target.transform.position - transform.position) < 50f && timeSinceAttacking <= 0 && !attacking) { //if player in range and able to attack and not already attacking
            //start attack
            attacking = true;
            StartCoroutine(Attack());

        } else if (!attacking) { //if not attacking
            //idle
            if (moving) {
                //pick a new direction to move in
                //move and rotate to the move direction

            } else {
                //wait for a bit

            }

        }
    }




    private IEnumerator Attack () {

        yield return new WaitForSeconds(1f);
        attacking = false;
    }

}
