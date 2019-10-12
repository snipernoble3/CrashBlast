using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidAI : MonoBehaviour {

    /**
     * scores
     * player score: targeting range / distance from player
     * avoidance score: min distance / distance to nearest unit
     * cohesion score: distance to center / min distance
     * alignment score: angle between avg direction and current direction
     * object avoidance: raycasts in front of boid, steer in direction of least collision, only apply other rules that dont steer into obj if obj is within min distance
     *                   maybe get normal of contact point and use that to turn the boid away from the object?
     * 
     * 
     * unit vectors of each force * score
     * combine vectors
     * unit vector of combination * steering force(speed)
     * 
     * OR
     * 
     * handle each vector from the weakest link out
     * compare two scores, bigger one wins
     * 
     * OR
     * 
     * divide area around boids into boxes (center of rubix cube)
     * only move to boxes with no obstacles
     * move in the best box to get avg direction while avoiding boxes with units
     * 
     */
    
    private Rigidbody rb;
    public float speed = 1f;

    float alignmentWeight;
    float cohesionWeight;
    float separationWeight;
    float obstacleWeight;
    float targetWeight;

    Vector3 center;
    Vector3 avgDirection;



    void Awake () {
        

        rb = GetComponent<Rigidbody>();
        
    }

    void Update () {
        //get neighbors
        //check for object collision
        //check for player
        alignmentWeight = 1f;
        cohesionWeight = 1f;
        separationWeight = 1f;

    }

    void FixedUpdate () {
        //apply force
        transform.position += SteerTowards(transform.forward);

    }

    Vector3 GetDirection () {
        Vector3 dir = Vector3.zero;
        //var alignment = SteerTowards() * alignmentWeight;
        //var cohesion = SteerTowards() * cohesionWeight;
        //var separation = SteerTowards() * separationWeight;
        //var obstacle = SteerTowards() * obstacleWeight;

        return dir;
    }

    Vector3 SteerTowards (Vector3 vector) {
        Vector3 v = vector.normalized * speed;
        return v;
    }

}
