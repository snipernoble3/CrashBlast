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
	public TextMeshProUGUI hud_Velocity;
	
	// Movement Variables
	private bool isGrounded = false; // Initialize as false, since player may spawn in mid-air
	[SerializeField] private float jumpForceMultiplier =  400.0f;
	private float moveSpeedReduction = 1.0f; // Set to 1.0f so there is no reduction while grounded.
	private const float moveSpeedReduction_Air = 0.5f;
	private const float moveSpeedReduction_Water = 0.75f;
	
	// Mouse Input
	[SerializeField] private float mouseSensitivity_X = 6.0f;
	[SerializeField] private float mouseSensitivity_Y = 3.0f;
	[SerializeField] private bool matchXYSensitivity = false;
	[SerializeField] private bool useRawMouseInput = true;
	[SerializeField] private bool invertVerticalInput = false;
	[SerializeField] private const float lookUpDownAngle_Max = 90.0f;
	[SerializeField] private const float lookUpDownAngle_Min = -90.0f;
	private float lookUpDownAngle = 0.0f;
	private float lookLeftRightAngle = 0.0f;
	private float rotation_vertical = 0.0f;
	private float rotation_horizontal = 0.0f;
	
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
		// if (!playerRB.useGravity && )
		
		GetInput_Mouse();
		GetInput_LateralMovement();
		LookUpDown();
		
		if (Input.GetButton("Fire1")) firstPersonArms_Animator.SetBool("fire", true);
		else firstPersonArms_Animator.SetBool("fire", false);
		
		if (Input.GetButtonDown("Jump") && isGrounded) Jump();
    }
	
	void FixedUpdate()
	{		
		gravity = GetGravity();
		
		CheckIfGrounded(); // update the isGrounded bool for use elsewhere
		LookLeftRight(); // rotate the player's rigidbody
		LateralMovement(); // move the player
		
		if (inputMovementVector == Vector3.zero && playerRB.velocity != Vector3.zero && isGrounded) SimulateFriction();
		
		Vector3 resultMoveVector = new Vector3(playerRB.velocity.x, 0.0f, playerRB.velocity.z);
		if (hud_Velocity != null) hud_Velocity.text = "Lateral Velocity: " + resultMoveVector.magnitude.ToString("F2");		
		//if (hud_Velocity != null) hud_Velocity.text = "Vertical Velocity: " + playerRB.velocity.y.ToString("F2");	

		//TerminalVelocity();
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
		// Move the player
		
		float speedRampUpMultiplier = 50.0f;
		
		// Multiply the movement vector with the speed multiplider
		Vector3 requestedMoveVector = inputMovementVector * speedRampUpMultiplier;
		
		float moveSpeedMax = 12.0f; // The player is only alowed to go past this lateral movment speed via outside forces like rocket jumping.
		moveSpeedMax *= inputMovementVector.magnitude * moveSpeedReduction; // Needed so that analogue input doesn't ramp up over time
		
		
		Vector3 forceToAdd = requestedMoveVector;
		Vector3 currentMoveVector = new Vector3(playerRB.velocity.x, 0.0f, playerRB.velocity.z); // Get the current velocity, We are only concerned with the horizontal movement vector so the vertical axis is zeroed out.
		currentMoveVector = transform.InverseTransformDirection(currentMoveVector); // Convert from world space to Local Space since we are adding the force in local space.
		
		// Calculate what the lateral velocity will be if we add the requested force BEFORE adding the force in order to see if it should be applied at all.
		Vector3 testMoveVector = currentMoveVector + requestedMoveVector / playerRB.mass; // * Time.fixedDeltaTime; // This would be added if we were using the Force ForceMode, but it's an Impulse 
		
		// If the requested movement vector is too high, calculate how much force we have to add to maintain top speed without going past it.
		if (testMoveVector.magnitude > moveSpeedMax)
		{
			forceToAdd = requestedMoveVector.normalized * Mathf.Clamp(moveSpeedMax - currentMoveVector.magnitude, 0.0f, moveSpeedMax);
		}
		// Apply the calculated force
		playerRB.AddRelativeForce(forceToAdd, ForceMode.Impulse);
		
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
		float frictionMultiplier = 15.0f;
		//Vector3 frictionForceToAdd = new Vector3(-playerRB.velocity.x, 0.0f, -playerRB.velocity.z); // Get the current velocity, We are only concerned with the horizontal movement vector so the vertical axis is zeroed out.
		Vector3 frictionForceToAdd = -playerRB.velocity; // Get the opposite of the player's world space velocity
		frictionForceToAdd = transform.InverseTransformDirection(frictionForceToAdd); // Convert to local space
		//frictionForceToAdd = new Vector3(frictionForceToAdd.x, 0.0f, frictionForceToAdd.z); // Don't manipulate the local vertical axis.
		
		//frictionForceToAdd -= gravity;
		
		frictionForceToAdd *= frictionMultiplier;
		
		//playerRB.AddForce(frictionForceToAdd, ForceMode.Impulse);
		
		playerRB.AddRelativeForce(frictionForceToAdd, ForceMode.Impulse);
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
		lookUpDownAngle += rotation_vertical;
		lookUpDownAngle = Mathf.Clamp(lookUpDownAngle, lookUpDownAngle_Min, lookUpDownAngle_Max);
		firstPersonCam.transform.localRotation = Quaternion.AngleAxis(lookUpDownAngle, Vector3.right);
	}
	
	private void LookLeftRight()
	{
		transform.Rotate(new Vector3(0.0f, rotation_horizontal, 0.0f), Space.Self);
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
	
	public bool GetIsGrounded()
	{
		return isGrounded;
	}
	
	public Vector3 GetGravity()
	{
		if (gravitySource != null) return gravitySource.GetGravityVector(transform);
		else return Physics.gravity;
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