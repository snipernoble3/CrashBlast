using System.Collections;
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
	public TextMeshProUGUI hud_LateralVelocity;
	public TextMeshProUGUI hud_VerticalVelocity;
	
	// Movement Variables
	private bool isGrounded = false; // Initialize as false, since player may spawn in mid-air
	[SerializeField] private float jumpForceMultiplier =  400.0f;
	private float moveSpeedReduction = 1.0f; // Set to 1.0f so there is no reduction while grounded.
	private const float moveSpeedReduction_Air = 0.5f;
	private const float moveSpeedReduction_Water = 0.75f;
	
	// Mouse Input
	[SerializeField] private float mouseSensitivity_X = 6.0f;
	[SerializeField] private float mouseSensitivity_Y = 3.0f;
	[SerializeField] private bool matchXYSensitivity = true;
	[SerializeField] private bool useRawMouseInput = true;
	[SerializeField] private bool invertVerticalInput = false;
	private float rotation_vertical = 0.0f;
	private float rotation_horizontal = 0.0f;
	private float verticalAngle = 0.0f;
	private float horizontalAngle = 0.0f;
	
	// Lateral Movement
	private Vector3 inputMovementVector;
	private Vector3 gravity; // Use this instead of Physics.gravity in case we want to replace gravity with attraction to a gravity sorce (like a tiny planet).
    
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
    }

    void Update()
    {		
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
		if (inputMovementVector == Vector3.zero && playerRB.velocity != Vector3.zero && isGrounded) SimulateFriction();
		
		Vector3 localVelocity = transform.InverseTransformDirection(playerRB.velocity);
		Vector3 lateralSpeed = new Vector3(localVelocity.x, 0.0f, localVelocity.z);
		if (hud_LateralVelocity != null) hud_LateralVelocity.text = "Lateral Velocity: " + lateralSpeed.magnitude.ToString("F2");		
		if (hud_VerticalVelocity != null) hud_VerticalVelocity.text = "Vertical Velocity: " + localVelocity.y.ToString("F2");	

		TerminalVelocity();
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
		float speedRampUpMultiplier = 60.0f;
		speedRampUpMultiplier *= Time.fixedDeltaTime; // Multiply by Time.fixedDeltaTime so that speed is not bound to inconsitencies in the physics time step.
		
		float targetMoveSpeed = 12.0f; // The player is only alowed to go past this lateral movment speed via outside forces like rocket jumping.
		targetMoveSpeed *= inputMovementVector.magnitude * moveSpeedReduction; // Needed so that analogue input doesn't ramp up over time.
		
		// Start with the the user input (for direction of the vector),
		// Multiply by the player's mass (so this script will work consistently regardless of the player's mass)
		// And Multiply by the speedRampUpMultiplier to add speed at the desired rate.
		Vector3 requestedMoveVector = inputMovementVector * playerRB.mass * speedRampUpMultiplier;
		
			/* This was a completely over engineered way of doing it:
			// Convert the player's velocity to align with the current direction of gravity.
			Vector3 currentMoveVector = Vector3.Project(playerRB.velocity, GetGravity().normalized);
			// We are only concerned with the horizontal part of the local space velocity vector so the vertical axis is zeroed out.
			currentMoveVector = new Vector3(currentMoveVector.x, 0.0f, currentMoveVector.z);
			// Convert the vector from world space to local space since the force will be added to the player in local space.
			currentMoveVector = transform.InverseTransformDirection(currentMoveVector);
			*/
		
		// Convert the velocity vector from world space to local space (Because the force will be added to the player in local space).
		Vector3 currentMoveVector =  transform.InverseTransformDirection(playerRB.velocity);
		// We are only concerned with the lateral part of the local space velocity vector, so the vertical axis is zeroed out.
		currentMoveVector = new Vector3(currentMoveVector.x, 0.0f, currentMoveVector.z);
		// Calculate what the lateral velocity will be if we add the requested force.
		// Do this BEFORE adding the force in order to see if it should be added at all.
		Vector3 testMoveVector = currentMoveVector + requestedMoveVector / playerRB.mass;
		
		// If the requested movement vector is too high, calculate how much force we have to add to maintain top speed without going past it.
		if (testMoveVector.magnitude > targetMoveSpeed)
		{
			requestedMoveVector = requestedMoveVector.normalized * Mathf.Clamp(targetMoveSpeed - currentMoveVector.magnitude, 0.0f, targetMoveSpeed);
		}
		// Apply the calculated force to the player in local space
		playerRB.AddRelativeForce(requestedMoveVector, ForceMode.Impulse);
		
		// Change Direction	
		if (Vector3.Angle(currentMoveVector, requestedMoveVector) != 0.0f) ChangeDirection(requestedMoveVector, currentMoveVector);
	}
	
	private void ChangeDirection(Vector3 requestedMoveVector, Vector3 currentMoveVector)
	{
		float redirectInfluence = 1.0f; // Change 100%
		Vector3 directionChangeVector = requestedMoveVector.normalized * currentMoveVector.magnitude * redirectInfluence;
		directionChangeVector -= currentMoveVector;
		playerRB.AddRelativeForce(directionChangeVector, ForceMode.Impulse);
		//playerRB.velocity = directionChangeVector;
	}
	
	private void SimulateFriction()
	{
		float frictionMultiplier = 5.0f;
		
		//Vector3 frictionForceToAdd = new Vector3(-playerRB.velocity.x, 0.0f, -playerRB.velocity.z); // Get the current velocity, We are only concerned with the horizontal movement vector so the vertical axis is zeroed out.
		Vector3 frictionForceToAdd = -playerRB.velocity; // Get the opposite of the player's world space velocity
		//frictionForceToAdd = transform.InverseTransformDirection(frictionForceToAdd); // Convert to local space
		//frictionForceToAdd = new Vector3(frictionForceToAdd.x, 0.0f, frictionForceToAdd.z); // Don't manipulate the local vertical axis.
		
		//frictionForceToAdd -= gravity;
		
		//frictionForceToAdd *= frictionMultiplier;
		
		//playerRB.AddForce(frictionForceToAdd, ForceMode.Impulse);
		
		//playerRB.AddRelativeForce(frictionForceToAdd, ForceMode.Impulse);
		
		frictionForceToAdd *= playerRB.mass * frictionMultiplier * Time.fixedDeltaTime;
		
		playerRB.AddForce(frictionForceToAdd, ForceMode.Impulse);
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
	
	public void Jump()
	{
		playerRB.AddRelativeForce(Vector3.up * jumpForceMultiplier, ForceMode.Impulse);
	}
		
	public float GetDownwardVelocity()
	{
		if (GetIsGrounded()) return 0.0f; // If the player is grounded, then there is no downward velocity.
		
		// Calculate how fast the player is moving along his local vertical axis.
		Vector3 downwardVelocity = Vector3.Project(playerRB.velocity, GetGravity().normalized);
		
		if (downwardVelocity.y > 0.0f || Mathf.Approximately(downwardVelocity.y, 0.0f)) return 0.0f; // If the player isn't moving vertically or the player is going up, then there is no downward velocity.
		else return Mathf.Abs(downwardVelocity.y); // If the player is falling, update the downward velocity to match.
	}
	
	public void TerminalVelocity()
	{
		Vector3 fallCancelForce;
		float currentDownwardSpeed = 0.0f;
		float maxDownwardSpeed = 75.0f;
		
		if (playerRB.velocity.y < 0.0f) currentDownwardSpeed = -playerRB.velocity.y;
		else currentDownwardSpeed = 0.0f;
		
		if (currentDownwardSpeed > maxDownwardSpeed)
		{
			fallCancelForce = new Vector3(0.0f, ((currentDownwardSpeed - maxDownwardSpeed) * playerRB.mass) - gravity.y, 0.0f);
			
			playerRB.AddForce(fallCancelForce, ForceMode.Impulse);
		}
	}
}