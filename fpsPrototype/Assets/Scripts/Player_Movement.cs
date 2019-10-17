﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class Player_Movement : MonoBehaviour
{
	// Object References
	public Gravity_Source gravitySource;
	private GameObject firstPersonCam;
	private Rigidbody playerRB;
	public Animator firstPersonArms_Animator;
	
	// HUD
	private GameObject hud;
	private List<VectorVisualizer> radarLines = new List<VectorVisualizer>();
	private TextMeshProUGUI hud_LateralVelocity;
	private TextMeshProUGUI hud_VerticalVelocity;
	
	// Lateral Movement
	
	private const float moveSpeed_Input_Max = 12.0f; // The player is only alowed to go past this lateral movment speed via outside forces like rocket jumping, and bunny hopping.
	private float moveSpeed_Input_Current = moveSpeed_Input_Max;
	
	private float moveSpeedReduction = 1.0f; // Set to 1.0f so there is no reduction while grounded.
	private const float moveSpeedReduction_Air = 0.5f;
	private const float moveSpeedReduction_Water = 0.75f;
	private Vector3 moveVector_Input;
	
	private Vector3 gravity; // Use this instead of Physics.gravity in case we want to replace gravity with attraction to a gravity sorce (like a tiny planet).
	
	[SerializeField] private float jumpForceMultiplier =  5.0f;
	private const float jumpCoolDownTime = 0.2f;
	private float timeSinceLastJump;
	private bool isGrounded = false; // Initialize as false, since player may spawn in mid-air
	
	// Mouse Input
	[SerializeField] private float mouseSensitivity_X = 3.0f;
	[SerializeField] private float mouseSensitivity_Y = 1.0f;
	[SerializeField] private bool matchXYSensitivity = true;
	[SerializeField] private bool useRawMouseInput = true;
	[SerializeField] private bool invertVerticalInput = false;
	private float rotation_vertical = 0.0f;
	private float rotation_horizontal = 0.0f;
	private float verticalAngle = 0.0f;
	private float horizontalAngle = 0.0f;
    
    void Awake()
    {
		// Hide the mouse cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
		
		// Set up references
		firstPersonCam = transform.Find("Camera Position Offset/Main Camera").gameObject;
        playerRB = GetComponent<Rigidbody>();
		playerRB.constraints = RigidbodyConstraints.FreezeRotation;
		gravity = GetGravity();
		
		// Make connections to HUD
		hud = GameObject.Find("Canvas_HUD");
		if (hud != null)
		{
			hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
			hud.GetComponent<Canvas>().worldCamera = firstPersonCam.transform.Find("First Person Camera").GetComponent<Camera>();
			//hud.GetComponent<Canvas>().worldCamera = firstPersonCam.GetComponent<Camera>();
			
			hud.transform.GetComponentsInChildren<VectorVisualizer>(false, radarLines);	
			hud_LateralVelocity = hud.transform.Find("Current Lateral Velocity").gameObject.GetComponent<TextMeshProUGUI>();
			hud_VerticalVelocity = hud.transform.Find("Current Vertical Velocity").gameObject.GetComponent<TextMeshProUGUI>();
		}

		timeSinceLastJump = jumpCoolDownTime;
    }

    void Update()
    {		
		if (timeSinceLastJump < jumpCoolDownTime) timeSinceLastJump = Mathf.Clamp(timeSinceLastJump += 1.0f * Time.deltaTime, 0.0f, jumpCoolDownTime);
		
		// Inputs
		GetInput_Mouse();
		MouseLook();
		GetInput_LateralMovement();
		if (isGrounded && Input.GetButtonDown("Jump")) Jump();
    }
	
	void FixedUpdate()
	{		
		gravity = GetGravity();
		CheckIfGrounded(); // Update the isGrounded bool for use elsewhere
		LateralMovement(); // Move the player based on the lateral movement input.
		// Slow the player down with "friction" if he is grounded and not trying to move.
		if (moveVector_Input == Vector3.zero && playerRB.velocity != Vector3.zero && isGrounded) SimulateFriction();
		
		Vector3 localVelocity = transform.InverseTransformDirection(playerRB.velocity);
		Vector3 lateralSpeed = new Vector3(localVelocity.x, 0.0f, localVelocity.z);
		
		if (hud != null) // Update HUD elements.
		{
			hud_LateralVelocity.text = "Lateral Velocity: " + lateralSpeed.magnitude.ToString("F2");		
			hud_VerticalVelocity.text = "Vertical Velocity: " + localVelocity.y.ToString("F2");

			//radarLines[0].SetVector(new Vector3(moveVector_Input.x * moveSpeed_Input_Current, moveVector_Input.z * moveSpeed_Input_Current, -2.0f));
			//radarLines[1].SetVector(new Vector3(lateralSpeed.x, lateralSpeed.z, -1.0f));
			
			radarLines[0].SetVector(new Vector3(moveVector_Input.x * moveSpeed_Input_Current, moveVector_Input.z * moveSpeed_Input_Current, -0.5f));
			radarLines[1].SetVector(new Vector3(lateralSpeed.x, lateralSpeed.z, -0.3f));
		}

		TerminalVelocity();
	}
	
	private void GetInput_LateralMovement()
	{
		// Get input for Movement from project input manager and build a Vector3 to store the two inputs
		moveVector_Input = new Vector3 (Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
		// Limit the magnitude of the vector so that horizontal and vertical input doesn't stack to excede the indended maximum move speed
		moveVector_Input = Vector3.ClampMagnitude(moveVector_Input, 1.0f);
	}
	
	// Add lateral movement via the physics system (doesn't affect vertical velocity).
	private void LateralMovement()
	{
		float rampUpMultiplier = 45.0f;
		rampUpMultiplier *= Time.fixedDeltaTime; // Multiply by Time.fixedDeltaTime so that speed is not bound to inconsitencies in the physics time step.
		
		// The distinction between "Current" and "Max" is needed so that partial analogue input doesn't ramp up over time.
		moveSpeed_Input_Current = moveSpeed_Input_Max * moveVector_Input.magnitude * moveSpeedReduction;
		
		// Start with the the user input (for direction of the vector),
		// Multiply by the player's mass (so this script will work consistently regardless of the player's mass)
		// And Multiply by the rampUpMultiplier to add speed at the desired rate.
		Vector3 moveVector_Request = moveVector_Input * playerRB.mass * rampUpMultiplier;
		
		// Convert the velocity vector from world space to local space (Because the force will be added to the player in local space).
		Vector3 moveVector_Current =  transform.InverseTransformDirection(playerRB.velocity);
		// We are only concerned with the lateral part of the local space velocity vector, so the vertical axis is zeroed out.
		moveVector_Current = new Vector3(moveVector_Current.x, 0.0f, moveVector_Current.z);

			
		Vector3 moveVector_Projected = Vector3.Project(moveVector_Request, moveVector_Current);
		
		/*
		// Check if the player is changing direction
		if (Vector3.Angle(moveVector_Current, moveVector_Request) != 0.0f) 
		{
			float redirectInfluence = 1.0f; // Change 100%
		
			//Vector3 directionChangeVector = moveVector_Request.normalized * moveVector_Current.magnitude * redirectInfluence;
			//directionChangeVector -= moveVector_Current;
			//playerRB.velocity = directionChangeVector;
		
			Vector3 directionChangeVector = Vector3.Project(moveVector_Request, moveVector_Current);
			//directionChangeVector -= moveVector_Current;
			//directionChangeVector *= playerRB.mass;
			playerRB.AddRelativeForce(directionChangeVector, ForceMode.Impulse);
		}	
		*/
		
		
		
		// Calculate what the lateral velocity will be if we add the requested force BEFORE actually adding the force.
		Vector3 moveVector_Test = moveVector_Current + moveVector_Request / playerRB.mass;
		// If the requested movement vector is too high, calculate how much force we have to add to maintain top speed without going past it.
		if (moveVector_Test.magnitude > moveSpeed_Input_Current) moveVector_Request = moveVector_Request.normalized * Mathf.Clamp(moveSpeed_Input_Current - moveVector_Current.magnitude, 0.0f, moveSpeed_Input_Current);
		// Apply the calculated force to the player in local space
		playerRB.AddRelativeForce(moveVector_Request, ForceMode.Impulse);
		
	

		
		/*
		////////Emulate Quake Code
		// Implement this instead to get bhopping working.
		
		float projVel = Vector3.Dot(moveVector_Current, moveVector_Input.normalized); // Vector projection of Current velocity onto input Movement direction.
		float accelVel = moveVector_Request.magnitude * Time.fixedDeltaTime; // Accelerated velocity in direction of movment

		// If necessary, truncate the accelerated velocity so the vector projection does not exceed max velocity
		if(projVel + accelVel > moveSpeed_Input_Max)
        accelVel = moveSpeed_Input_Max - projVel;

		Vector3 directionChangeVector = moveVector_Current + moveVector_Input.normalized * accelVel; // This is the new movement vector original code returned this
		
		
		playerRB.AddRelativeForce(moveVector_Request, ForceMode.Impulse);
		*/
	}
	
	private void SimulateFriction()
	{
		Vector3 frictionForceToAdd = transform.InverseTransformDirection(playerRB.velocity); // Start by getting the the player's velocity in local space.
		
		float rampDownMultiplier = 10.0f;
		rampDownMultiplier *= Time.fixedDeltaTime; // Multiply by Time.fixedDeltaTime so that friction speed is not bound to inconsitencies in the physics time step.
		
		float verticalFrictionMultiplier = 1.0f; // We need some vertical counter force so the player doesn't slip off of edges.
		if (frictionForceToAdd.y >= -0.02f) verticalFrictionMultiplier = 0.0f; // Don't slow down the player's force if he is trying to jump.

		frictionForceToAdd *= -playerRB.mass; // Point the force in the opposite direction with enough strength to counter the mass.
		frictionForceToAdd -= transform.InverseTransformDirection(gravity);  // Counter gravity (in local space) so that the player won't slip off of edges
		// Multiply the rampDownMultiplier with the x and z components so that speed will ramp down for lateral movment, but the vertical friction will be instantanious if enabled.
		frictionForceToAdd = Vector3.Scale(frictionForceToAdd, new Vector3(rampDownMultiplier, verticalFrictionMultiplier, rampDownMultiplier));
		
		playerRB.AddRelativeForce(frictionForceToAdd, ForceMode.Impulse); // Apply the friction to the player in local space
	}
		
	private void GetInput_Mouse()
	{
		if (matchXYSensitivity) mouseSensitivity_Y = mouseSensitivity_X;
		
		if (useRawMouseInput) rotation_horizontal = Input.GetAxisRaw("Mouse X") * mouseSensitivity_X;
		else rotation_horizontal = Input.GetAxis("Mouse X") * mouseSensitivity_X;
		
		if (useRawMouseInput) rotation_vertical = Input.GetAxisRaw("Mouse Y") * mouseSensitivity_Y;
		else rotation_vertical = Input.GetAxis("Mouse Y") * mouseSensitivity_Y;
		if (invertVerticalInput) rotation_vertical *= -1;
	}
	
	private void MouseLook()
	{
		float deltaTimeCompensation = 100.0f;
		float verticalAngle_Min = -90.0f;
		float verticalAngle_Max = 90.0f;
		
		// Up Down
		verticalAngle += rotation_vertical * Time.deltaTime * deltaTimeCompensation;
		verticalAngle = Mathf.Clamp(verticalAngle, verticalAngle_Min, verticalAngle_Max);
		firstPersonCam.transform.localRotation = Quaternion.Euler(Vector3.left * verticalAngle);
		
		// Left Right
		horizontalAngle = rotation_horizontal * Time.deltaTime * deltaTimeCompensation;
		transform.rotation *= Quaternion.Euler(new Vector3(0.0f, horizontalAngle, 0.0f));
	}
	
	public void CheckIfGrounded()
	{
		moveSpeedReduction = moveSpeedReduction_Air; // Start out as if we are in the air, then prove otherwise.
		isGrounded = false; // Start out false, then prove otherwise.
		
		RaycastHit[] hits = Physics.SphereCastAll(playerRB.position + (transform.up * 0.49f), 0.49f, -transform.up, 0.1f);
		foreach (RaycastHit groundCheckObject in hits)
		{
			if (groundCheckObject.rigidbody == playerRB) continue;
			if (groundCheckObject.collider != null)
			{
				moveSpeedReduction = 1.0f;
				isGrounded = true;
			}
		}
	}
	
	public void Jump()
	{
		if (timeSinceLastJump == jumpCoolDownTime)
		{
			timeSinceLastJump = 0.0f;
			playerRB.AddRelativeForce(Vector3.up * jumpForceMultiplier * playerRB.mass, ForceMode.Impulse);
		}
	}
	
	public float GetVerticalCameraAngle()
	{
		return verticalAngle;
	}
	
	public bool GetIsGrounded()
	{
		return isGrounded;
	}
	
	public Vector3 GetGravity()
	{
		if (gravitySource == null)
		{
			playerRB.useGravity = true;
			return Physics.gravity;
		}			
		else return gravitySource.GetGravityVector(transform);
	}
		
	public float GetDownwardVelocity()
	{
		if (isGrounded) return 0.0f; // If the player is grounded, then there is no downward velocity.
		
		// Calculate how fast the player is moving along his local vertical axis.
		Vector3 downwardVelocity = transform.InverseTransformDirection(playerRB.velocity); // Convert the vector to local space
		
		if (downwardVelocity.y > 0.0f || Mathf.Approximately(downwardVelocity.y, 0.0f)) return 0.0f; // If the player isn't moving vertically or the player is going up, then there is no downward velocity.
		else return Mathf.Abs(downwardVelocity.y); // If the player is falling, update the downward velocity to match.
	}
	
	public void TerminalVelocity()
	{
		Vector3 fallCancelForce;
		Vector3 relativeVelocity = transform.InverseTransformDirection(playerRB.velocity);
		float currentDownwardSpeed = 0.0f;
		float maxDownwardSpeed = 75.0f;
		
		if (relativeVelocity.y < 0.0f) currentDownwardSpeed = -relativeVelocity.y;
		else currentDownwardSpeed = 0.0f;
		
		if (currentDownwardSpeed > maxDownwardSpeed)
		{
			fallCancelForce = new Vector3(0.0f, ((currentDownwardSpeed - maxDownwardSpeed) * playerRB.mass), 0.0f) -gravity;
			
			playerRB.AddRelativeForce(fallCancelForce, ForceMode.Impulse);
		}
	}
}