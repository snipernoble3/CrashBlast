using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Player_BlastMechanics))]
public class Player_Movement : MonoBehaviour
{
	// User Preferences
	public bool userPreference_EnableLandingShake = true;
	public bool userPreference_autoAddJumpForceToGroundedRocketJump = true;
	
	// Object References
	private Player_BlastMechanics playerBlastMechanics;
	public GameObject groundPoundParticles; // gets reference to the particles to spawn
	private GameObject gpParticles_GameObject; // stores the instance of the particles
	private GameObject camOffset;
	private GameObject firstPersonCam;
	private Rigidbody playerRB;
	public Animator firstPersonArms_Animator;
	public TextMeshProUGUI hud_Velocity;
	
	// Jumping Variables
	[SerializeField] private float jumpForceMultiplier =  400.0f;
	
	private const float rjBlast_Range = 3.0f;
	private const float rjBlast_Power = 550.0f;
	private Vector3 rjBlast_Epicenter; // The origin of the rocket jump blast radius.
	private const float rjBlast_Radius = 5.0f;
	private const float rjBlast_UpwardForce = 0.0f;
	private const float minRocketJumpCameraAngle = 45.0f;
	
	private float impactVelocity = 0.0f;
	public float minGroundPoundVelocity = 8.0f;
	public float groundPound_Multiplier = 25.0f;
	
	// Rocket Jumping Variables
	private int rjBlast_NumSinceGrounded = 0;
	[SerializeField] private const int rjBlast_NumLimit = 2;
	
	[SerializeField] private float moveSpeedMultiplier = 500.0f;
	private float moveSpeedReduction = 1.0f;
	[SerializeField] private const float moveSpeedReduction_Air = 0.5f;
	[SerializeField] private const float moveSpeedReduction_Water = 0.75f;
	
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
		playerBlastMechanics = GetComponent<Player_BlastMechanics>();
		firstPersonCam = transform.Find("Camera Position Offset/Main Camera").gameObject;
		camOffset = transform.Find("Camera Position Offset").gameObject;
        playerRB = GetComponent<Rigidbody>();
    }

    void Update()
    {		
		LookUpDown();
		
		GetInput_LateralMovement();
		GetInput_Mouse();
		
		
		if (Input.GetButton("Fire1")) firstPersonArms_Animator.SetBool("fire", true);
		else firstPersonArms_Animator.SetBool("fire", false);
		
		if (Input.GetButtonDown("Jump") && IsGrounded()) Jump();
    }
	
	void FixedUpdate()
	{
		LookLeftRight();
		
		LateralMovement(inputMovementVector);
		Vector3 resultMoveVector = new Vector3(playerRB.velocity.x, 0.0f, playerRB.velocity.z);
		hud_Velocity.text = "Lateral Velocity: " + resultMoveVector.magnitude.ToString("F2");		
	}
	
	private void GetInput_LateralMovement()
	{
		// Get input for Movement from project input manager and build a Vector3 to store the two inputs
		inputMovementVector = new Vector3 (Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
		// Limit the magnitude of the vector so that horizontal and vertical input doesn't stack to excede the indended maximum move speed
		inputMovementVector = Vector3.ClampMagnitude(inputMovementVector, 1.0f);
		// Multiply the movement vector with the speed multiplider
		inputMovementVector *= moveSpeedMultiplier * moveSpeedReduction;
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
	
	// Add lateral movement via the physics system (doesn't affect vertical velocity).
	private void LateralMovement(Vector3 requestedMoveVector)
	{
		float moveSpeedMax = 12.0f; // The player is only alowed to go past this lateral movment speed via outside forces like rocket jumping.
		Vector3 forceToAdd = requestedMoveVector;
		Vector3 currentMoveVector = new Vector3(playerRB.velocity.x, 0.0f, playerRB.velocity.z); // Get the current velocity, We are only concerned with the horizontal movement vector so the vertical axis is zeroed out.
		
		// Calculate what the lateral velocity will be if we add the requested force BEFORE adding the force in order to see if it should be applied at all.
		Vector3 testMoveVector = currentMoveVector + requestedMoveVector / playerRB.mass; // * Time.fixedDeltaTime;
		
		// If the requested movement vector is too high, calculate how much force we have to add to maintain top speed without going past it.
		if (testMoveVector.magnitude > moveSpeedMax) forceToAdd = requestedMoveVector.normalized * Mathf.Clamp(moveSpeedMax - currentMoveVector.magnitude, 0.0f, moveSpeedMax);
	
		playerRB.AddRelativeForce(forceToAdd, ForceMode.Impulse); // Apply the calculated force
		
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
				playerBlastMechanics.rjBlast_NumSinceGrounded = 0;
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