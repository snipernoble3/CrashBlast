using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Beetle2 : MonoBehaviour {

    private enum State {Attacking, Moving, Idle};
    private State currState;

    private Transform target;
    private float distanceToTarget;

    public float attackRange = 5f;
    public float visionRange = 20f;
    public float lookSpeed = 5f;
    public float moveSpeed = 1f;
    public float chargeSpeed = 5f;
    public int damageOnHit = 2;

    private Quaternion targetLookDirection;

    private bool attacking;
    private bool charging;
    private bool dealtDamage;
    private bool cooldown;
    private Vector3 hitLocation;
    private Vector3 lastLocation;
    public float channelTime = 0.5f;
    public float chargeTime = 1f;
    public float cooldownTime = 0.5f;

    private void Awake () {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        currState = State.Idle;
    }

    private void Update () {

        distanceToTarget = Mathf.Abs(Vector3.Magnitude(target.position - transform.position));

        if (distanceToTarget < visionRange && currState != State.Attacking) {
            UpdateState(State.Moving);
        } else if (currState != State.Attacking) {
            UpdateState(State.Idle);
        }

        switch (currState) {
            case State.Attacking:
                if (!charging && !cooldown) {
                    targetLookDirection = Quaternion.LookRotation(hitLocation - transform.position, transform.position + Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetLookDirection, lookSpeed * Time.deltaTime);
                }
                
                break;
            case State.Moving:
                targetLookDirection = Quaternion.LookRotation(target.position - transform.position,  Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetLookDirection, Random.Range(1f, 4f) * Time.deltaTime);
                
                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.forward, out hit, attackRange)) {
                    if (hit.transform.tag == "Player") {
                        hitLocation = hit.transform.position;
                        UpdateState(State.Attacking);
                    }
                }
                break;
            case State.Idle:
                break;
        }
    }

    void UpdateState (State s) {
        currState = s;
    }

    private void FixedUpdate () {
        switch (currState) {
            case State.Attacking:
                if (!attacking) {
                    StartCoroutine(Attack());
                }

                if (charging) {
                    transform.position += transform.forward * chargeSpeed * Time.fixedDeltaTime;
                }

                break;
            case State.Moving:
                //a* pathfinding?
                //transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.fixedDeltaTime);
                transform.position += transform.forward * moveSpeed * Time.fixedDeltaTime;
                break;
            case State.Idle:
                
                break;
        }
    }

    private void OnCollisionEnter (Collision collision) {
        if (charging && !dealtDamage) {

            if (collision.gameObject == target.gameObject) {
                collision.gameObject.GetComponent<Health>().TakeDamage(damageOnHit);
                dealtDamage = true;

            }

        }
        
    }

    IEnumerator Attack () {
        attacking = true;
        //Debug.Log("Begin Attack");
        //Debug.Log("target: " + target.transform.position.ToString() + " hit: " + hitLocation.ToString());
        //Vector3 attackLocation = hitLocation - transform.position;
        

        yield return new WaitForSeconds(channelTime);
        
        charging = true;

        yield return new WaitForSeconds(chargeTime);

        charging = false;
        dealtDamage = false;
        cooldown = true;

        yield return new WaitForSeconds(cooldownTime);
        cooldown = false;
        attacking = false;
        UpdateState(State.Idle);
    }

}
