﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Player_BlastMechanics))]
public class Player_Movement : MonoBehaviour
{
	// Object References
	private Player_BlastMechanics blastMechanics; // Reference to script that controls rocket jumping, ground pounding, and blasting
	private GameObject firstPersonCam;
	private Rigidbody playerRB;
	public Animator firstPersonArms_Animator;
	public TextMeshProUGUI hud_Velocity;
	
	// Movement Variables
	private float moveSpeedReduction = 1.0f; // Set to 1.0f so there is no reduction while grounded.
	private const float moveSpeedReduction_Air = 0.5f;
	private const float moveSpeedReduction_Water = 0.75f;
	
	[SerializeField] private float jumpForceMultiplier =  400.0f;
	
	// Mouse Input
	[SerializeField] private float mouseSensitivity_X = 6.0f;
	[SerializeField] private float mouseSensitivity_Y = 3.0f;
	[SerializeField] private bool matchXYSensitivity = false;
	[SerializeField] private bool useRawMouseInput = true;
	[SerializeField] private bool invertVerticalInput = false;
	[SerializeField] private const float lookUpDownAngle_Max = 90.0f;
	[SerializeField] private const float lookUpDownAngle_Min = -90.0f;
	private float lookUpDownAngle_Current = 0.0f;
	private float rotation_vertical = 0.0f;
	private float rotation_horizontal = 0.0f;
	
	// Lateral Movement
	private Vector3 inputMovementVector;
    
    void Awake()
    {
		// Hide the mouse cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
		
		// Set up references
		blastMechanics = GetComponent<Player_BlastMechanics>();
		firstPersonCam = transform.Find("Camera Position Offset/Main Camera").gameObject;
        playerRB = GetComponent<Rigidbody>();
    }

    void Update()
    {		
		GetInput_Mouse();
		GetInput_LateralMovement();
		LookUpDown();
		
		if (Input.GetButton("Fire1")) firstPersonArms_Animator.SetBool("fire", true);
		else firstPersonArms_Animator.SetBool("fire", false);
		
		if (Input.GetButtonDown("Jump") && IsGrounded()) Jump();
    }
	
	void FixedUpdate()
	{
		LookLeftRight();
		LateralMovement();
		//if (Mathf.Approximately(inputMovementVector.magnitude, 0.0f)
		//	&& !Mathf.Approximately(playerRB.velocity.magnitude, 0.0f)
		//	&& IsGrounded()) SimulateFriction();
		
		if (inputMovementVector == Vector3.zero && playerRB.velocity != Vector3.zero && IsGrounded()) SimulateFriction();
		
		Vector3 resultMoveVector = new Vector3(playerRB.velocity.x, 0.0f, playerRB.velocity.z);
		hud_Velocity.text = "Lateral Velocity: " + resultMoveVector.magnitude.ToString("F2");		
	}
	
	private void GetInput_LateralMovement()
	{
		// Get input for Movement from project input manager and build a Vector3 to store the two inputs
		inputMovementVector = new Vector3 (Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
		// Limit the magnitude of the vector so that horizontal and vertical input doesn't stack to excede the indended maximum move speed
		inputMovementVector = Vector3.ClampMagnitude(inputMovementVector, 1.0f);
	}
	
	// Add lateral movement via the physics system (doesn't affect vertical velocity).
	private void LateralMovement()
	{
		float moveSpeedMultiplier = 25.0f;
		
		// Multiply the movement vector with the speed multiplider
		Vector3 requestedMoveVector = inputMovementVector * moveSpeedMultiplier * moveSpeedReduction;
		
		
		float moveSpeedMax = 12.0f; // The player is only alowed to go past this lateral movment speed via outside forces like rocket jumping.
		Vector3 forceToAdd = requestedMoveVector;
		Vector3 currentMoveVector = new Vector3(playerRB.velocity.x, 0.0f, playerRB.velocity.z); // Get the current velocity, We are only concerned with the horizontal movement vector so the vertical axis is zeroed out.
		
		// Calculate what the lateral velocity will be if we add the requested force BEFORE adding the force in order to see if it should be applied at all.
		Vector3 testMoveVector = currentMoveVector + requestedMoveVector / playerRB.mass; // * Time.fixedDeltaTime; // This would be added if we were using the Force ForceMode, but it's an Impulse 
		
		// If the requested movement vector is too high, calculate how much force we have to add to maintain top speed without going past it.
		if (testMoveVector.magnitude > moveSpeedMax) forceToAdd = requestedMoveVector.normalized * Mathf.Clamp(moveSpeedMax - currentMoveVector.magnitude, 0.0f, moveSpeedMax);
	
		playerRB.AddRelativeForce(forceToAdd, ForceMode.Impulse); // Apply the calculated force
	}		
		/* Important code snippets from quake style movement script
		*************************************

        // Calculate top velocity
        Vector3 udp = playerVelocity;
        udp.y = 0.0f;
        if(udp.magnitude > playerTopVelocity)
        playerTopVelocity = udp.magnitude;
		
	    private void Accelerate(Vector3 wishdir, float wishspeed, float accel)
		{
			float addspeed;
			float accelspeed;
			float currentspeed;

			currentspeed = Vector3.Dot(playerVelocity, wishdir);
			addspeed = wishspeed - currentspeed;
			if(addspeed <= 0)
				return;
			accelspeed = accel * Time.deltaTime * wishspeed;
			if(accelspeed > addspeed)
            accelspeed = addspeed;

			playerVelocity.x += accelspeed * wishdir.x;
			playerVelocity.z += accelspeed * wishdir.z;
		}
		
		*************************************
		*/
	
	private void SimulateFriction()
	{
		Debug.Log("Slowing Down");
		
		float frictionMultiplier = 15.0f;
		Vector3 frictionForceToAdd = new Vector3(-playerRB.velocity.x, 0.0f, -playerRB.velocity.z); // Get the current velocity, We are only concerned with the horizontal movement vector so the vertical axis is zeroed out.
		
		// Calculate what the lateral velocity will be if we add the requested force BEFORE adding the force in order to see if it should be applied at all.
		//Vector3 testMoveVector = currentMoveVector + requestedMoveVector / playerRB.mass; // * Time.fixedDeltaTime; // This would be added if we were using the Force ForceMode, but it's an Impulse 
		
		// If the requested movement vector is too high, calculate how much force we have to add to maintain top speed without going past it.
		//if (testMoveVector.magnitude > moveSpeedMax) forceToAdd = requestedMoveVector.normalized * Mathf.Clamp(moveSpeedMax - currentMoveVector.magnitude, 0.0f, moveSpeedMax);
		
		frictionForceToAdd *= frictionMultiplier;
		
		playerRB.AddForce(frictionForceToAdd, ForceMode.Impulse);
		//playerRB.AddForce(frictionForceToAdd, ForceMode.Force);
	}
		
	private void GetInput_Mouse()
	{
		if (matchXYSensitivity) mouseSensitivity_Y = mouseSensitivity_X;
		
		if (useRawMouseInput) rotation_horizontal = Input.GetAxisRaw("Mouse X") * mouseSensitivity_X;
		else rotation_horizontal = Input.GetAxis("Mouse X") * mouseSensitivity_X;
		
		if (useRawMouseInput) rotation_vertical = -Input.GetAxisRaw("Mouse Y") * mouseSensitivity_Y;
		else rotation_vertical = -Input.GetAxis("Mouse Y") * mouseSensitivity_Y;
		if (invertVerticalInput) rotation_vertical *= -1;
	}
	
	private void LookUpDown()
	{
		lookUpDownAngle_Current += rotation_vertical;
		lookUpDownAngle_Current = Mathf.Clamp(lookUpDownAngle_Current, lookUpDownAngle_Min, lookUpDownAngle_Max);
			
		firstPersonCam.transform.localRotation = Quaternion.AngleAxis(lookUpDownAngle_Current, Vector3.right);
	}
	
	private void LookLeftRight()
	{
		Quaternion deltaRotation = Quaternion.Euler(new Vector3(0.0f, rotation_horizontal, 0.0f));
		playerRB.MoveRotation(playerRB.rotation * deltaRotation);
	}
	
	public bool IsGrounded()
	{
		RaycastHit[] hits = Physics.SphereCastAll(playerRB.position + (Vector3.up * 0.49f), 0.49f, Vector3.down, 0.1f);
		foreach (RaycastHit groundCheckObject in hits)
		{
			if (groundCheckObject.rigidbody == playerRB) continue;
			if (groundCheckObject.collider != null)
			{
				moveSpeedReduction = 1.0f;
				blastMechanics.rjBlast_NumSinceGrounded = 0;
				return true;
			}
		}
		moveSpeedReduction = moveSpeedReduction_Air;
		return false;
	}
	
	public void Jump()
	{
		playerRB.AddRelativeForce(Vector3.up * jumpForceMultiplier, ForceMode.Impulse);
	}
}