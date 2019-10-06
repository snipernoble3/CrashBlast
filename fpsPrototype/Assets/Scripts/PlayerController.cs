using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    private Rigidbody rb;
    //private PlayerStats stats;
    
    private Vector3 spawnLocation;

    private float moveSpeed;
    public float baseMoveSpeed = 5f;
    public float altMoveSpeed = 7.5f;
    public float jumpSpeed;

    private int moveF;
    private int moveR;

    public float lookSensitivity = 3f;
    private Vector2 rotation = Vector2.zero;
    private Vector3 velocity;
    private float gravity = 9f;
    private bool grounded = true;

    void Awake() {
        rb = GetComponent<Rigidbody>();
        //stats = GetComponent<PlayerStats>();
        spawnLocation = transform.position;
    }

	// Use this for initialization
	void Start () {
        /*
        spawnLocation = gameObject.transform.position;
        xPos = spawnLocation.x;
        yPos = spawnLocation.y;
        zPos = spawnLocation.z;
        */
        
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        //if (stats.alive) {
            rotation.y += Input.GetAxis("Mouse X");
            rotation.x += -Input.GetAxis("Mouse Y"); //here is where to add look inversion
            rotation.x = Mathf.Clamp(rotation.x, -20f, 20f);
            transform.eulerAngles = new Vector2(0, rotation.y) * lookSensitivity;
            Camera.main.transform.localRotation = Quaternion.Euler(rotation.x * lookSensitivity, 0, 0);


            moveSpeed = (Input.GetKey(KeyCode.LeftShift)) ? altMoveSpeed : baseMoveSpeed;

            moveF = 0;
            moveR = 0;

            if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.D)) {
                moveR = 0;
            } else if (Input.GetKey(KeyCode.A)) { //Move left
                moveR += -1; //rb.velocity = -transform.right * moveSpeed;
            } else if (Input.GetKey(KeyCode.D)) { //Move right
                moveR += 1; //rb.velocity = transform.right * moveSpeed;
            } else {
                moveR = 0;
            }

            if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.S)) {
                moveF = 0;
            } else if (Input.GetKey(KeyCode.W)) { //Move forward
                moveF += 1; //rb.velocity = transform.forward * moveSpeed;
            } else if (Input.GetKey(KeyCode.S)) { //Move back
                moveF += -1; //rb.velocity = -transform.forward * moveSpeed;
            } else {
                moveF = 0;
            }
            

            velocity = (moveF * (transform.forward * moveSpeed)) + (moveR * (transform.right * moveSpeed));

            rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z);

            /* crouching
            if (Input.GetKeyDown(KeyCode.LeftControl)) {
                yPos -= 0.5f;
            }
            if (Input.GetKeyUp(KeyCode.LeftControl)) {
                yPos += 0.5f;
            }
            */


            /* jumping */
            if (grounded && Input.GetKeyDown(KeyCode.Space)) {
                //grounded = false;
                rb.AddForce(new Vector3(0, jumpSpeed, 0), ForceMode.VelocityChange);
            }

        //}
    }

    public void setGrounded(bool g) {
        grounded = g;
    }

}
