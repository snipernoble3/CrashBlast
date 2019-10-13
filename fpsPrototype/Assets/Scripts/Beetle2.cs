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

    private bool attackStarted;
    public float chargeTime = 0.5f;
    public float cooldownTime = 0.5f;

    private void Awake () {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        currState = State.Idle;
    }

    private void Update () {

        distanceToTarget = Mathf.Abs(Vector3.Magnitude(target.position - transform.position));

        if (distanceToTarget < visionRange) {
            UpdateState(State.Moving);
        } else {
            UpdateState(State.Idle);
        }

        switch (currState) {
            case State.Moving:
                var targetRotation = Quaternion.LookRotation(target.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookSpeed * Time.deltaTime);
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
                if (!attackStarted) {
                    StartCoroutine(Charge());
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

    IEnumerator Charge () {

        yield return new WaitForSeconds(chargeTime);

        yield return new WaitForSeconds(cooldownTime);

        UpdateState(State.Idle);
    }

}
