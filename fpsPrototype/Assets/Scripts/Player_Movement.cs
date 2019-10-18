using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MoveSpeed
{
	public float input = 0.0f;
	
	// Speed caps for different situations
	public readonly float groundedMax = 10.0f; // Cap the walking speed.
	public readonly float bHopMax = 25.0f; // Cap the bunny hopping speed.
	public readonly float lateralMax = 50.0f; // NEVER let the player move faster than this lateraly.	
	public readonly float verticalMax = 75.0f; // NEVER let the player move faster than this verticaly.	
	
	public Vector3 inputVector;
	public Vector3 projectedVector;
	
	public ReductionMultiplier reduction = new ReductionMultiplier();
}

public class ReductionMultiplier
{
	public float multiplier; // The current multiplier.
	
	private const float regular = 1.0f; // Set to 1.0f so there is no reduction while grounded.
	private const float air = 0.5f;
	private const float water = 0.75f;
	
	// Setter with specific predefined values, other multipliers can be used by assigning the value directly.
	public void Set(string newMultiplier)
	{
        switch(newMultiplier) 
        {
			case "regular": 
                multiplier = regular;
                break; 
            case "air": 
                multiplier = air;
                break; 
            case "water": 
                multiplier = water;
                break; 
            default:
                Debug.Log("ReductionMultiplier.Set() method was called with an invalid argument, multiplier was not changed.");
				break;
        } 
	}
}

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
	
	private MoveSpeed moveSpeed = new MoveSpeed();
	private Vector3 gravity; // Use this instead of Physics.gravity in case we want to replace gravity with attraction to a gravity sorce (like a tiny planet).
	private bool isGrounded = false; // Initialize as false, since player may spawn in mid-air
	
	// Jump
	[SerializeField] private float jumpForceMultiplier =  3.0f;
	private const float jumpCoolDownTime = 0.2f;
	private float timeSinceLastJump;
	private bool jumpQueue_isQueued = false;
	private const float jumpQueue_gracePeriod = 0.3f; // How long will the jump stay queued.
	private float jumpQueue_timeSinceQueued = 0.0f; // How long has it been since the jump was queued.
	
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
		
		// Set limits for Lateral Movement
		moveSpeed.reduction.Set("regular");
		
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
		// Count how long it's been since the player jumped.
		if (timeSinceLastJump < jumpCoolDownTime) timeSinceLastJump = Mathf.Clamp(timeSinceLastJump += 1.0f * Time.deltaTime, 0.0f, jumpCoolDownTime);
		// Count how long it's been since the player queued the next jump.
		if (jumpQueue_timeSinceQueued < jumpQueue_gracePeriod) jumpQueue_timeSinceQueued = Mathf.Clamp(jumpQueue_timeSinceQueued += 1.0f * Time.deltaTime, 0.0f, jumpQueue_gracePeriod);
		if (jumpQueue_timeSinceQueued == jumpQueue_gracePeriod) jumpQueue_isQueued = false;
		
		// Inputs
		GetInput_Mouse();
		MouseLook();
		GetInput_LateralMovement();
		if (Input.GetButtonDown("Jump"))
		{
			if (isGrounded) Jump();
			else
			{
				jumpQueue_isQueued = true;
				jumpQueue_timeSinceQueued = 0.0f;
			}
		}
    }
	
	void FixedUpdate()
	{		
		gravity = GetGravity();
		LateralMovement(); // Move the player based on the lateral movement input.
		// Slow the player down with "friction" if he is grounded and not trying to move.
		if (moveSpeed.inputVector == Vector3.zero && playerRB.velocity != Vector3.zero && isGrounded) SimulateFriction();
		
		Vector3 localVelocity = transform.InverseTransformDirection(playerRB.velocity);
		Vector3 lateralSpeed = new Vector3(localVelocity.x, 0.0f, localVelocity.z);
		
		if (hud != null) // Update HUD elements.
		{
			hud_LateralVelocity.text = "Lateral Velocity: " + lateralSpeed.magnitude.ToString("F2");		
			hud_VerticalVelocity.text = "Vertical Velocity: " + localVelocity.y.ToString("F2");
			
			radarLines[0].SetVector(new Vector3(moveSpeed.inputVector.x * moveSpeed.input, moveSpeed.inputVector.z * moveSpeed.input, -1.0f));
			radarLines[1].SetVector(new Vector3(lateralSpeed.x, lateralSpeed.z, -2.0f));
		}

		TerminalVelocity();
	}
	
	private void GetInput_LateralMovement()
	{
		// Get input for Movement from project input manager and build a Vector3 to store the two inputs
		moveSpeed.inputVector = new Vector3 (Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
		// Limit the magnitude of the vector so that horizontal and vertical input doesn't stack to excede the indended maximum move speed
		moveSpeed.inputVector = Vector3.ClampMagnitude(moveSpeed.inputVector, 1.0f);
	}
	
	// Add lateral movement via the physics system (doesn't affect vertical velocity).
	private void LateralMovement()
	{
		//float rampUpMultiplier = 45.0f;
		float rampUpMultiplier = 15.0f; // Accelerated velocity in direction of movment
		rampUpMultiplier *= Time.fixedDeltaTime; // Multiply by Time.fixedDeltaTime so that speed is not bound to inconsitencies in the physics time step.
		
		// The distinction between "Current" and "Max" is needed so that partial analogue input doesn't ramp up over time.
		moveSpeed.input = moveSpeed.groundedMax * moveSpeed.inputVector.magnitude * moveSpeed.reduction.multiplier;
		
		// Start with the the user input (for direction of the vector),
		// Multiply by the player's mass (so this script will work consistently regardless of the player's mass)
		// And Multiply by the rampUpMultiplier to add speed at the desired rate.
		Vector3 moveVector_Request = moveSpeed.inputVector * playerRB.mass * rampUpMultiplier;
		
		// Convert the velocity vector from world space to local space (Because the force will be added to the player in local space).
		Vector3 moveVector_Current =  transform.InverseTransformDirection(playerRB.velocity);
		// We are only concerned with the lateral part of the local space velocity vector, so the vertical axis is zeroed out.
		moveVector_Current = new Vector3(moveVector_Current.x, 0.0f, moveVector_Current.z);

		
		
		//Emulate Quake's vector limiting code so as to enable Bunny Hopping.
		
		// Vector projection of Current velocity onto input Movement direction.
		float projVel = Vector3.Dot(moveVector_Current, moveVector_Request.normalized);
		
		// If necessary, truncate the accelerated velocity so the vector projection does not exceed max velocity
		if(projVel + rampUpMultiplier > moveSpeed.groundedMax) rampUpMultiplier = moveSpeed.groundedMax - projVel;
		
		moveSpeed.projectedVector = moveVector_Current + moveVector_Request.normalized * rampUpMultiplier;
		
		moveSpeed.projectedVector = Vector3.ClampMagnitude(moveSpeed.projectedVector, moveSpeed.bHopMax);
		
		Vector3 desiredVelocity = new Vector3(moveSpeed.projectedVector.x, 0.0f, moveSpeed.projectedVector.z);
		playerRB.AddRelativeForce((desiredVelocity - moveVector_Current) * playerRB.mass, ForceMode.Impulse);
		
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
		
		/*
		
		// Calculate what the lateral velocity will be if we add the requested force BEFORE actually adding the force.
		Vector3 moveVector_Test = moveVector_Current + moveVector_Request / playerRB.mass;
		// If the requested movement vector is too high, calculate how much force we have to add to maintain top speed without going past it.
		if (moveVector_Test.magnitude > moveSpeed.input) moveVector_Request = moveVector_Request.normalized * Mathf.Clamp(moveSpeed.input - moveVector_Current.magnitude, 0.0f, moveSpeed.input);
		// Apply the calculated force to the player in local space
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
		
		verticalFrictionMultiplier = 0.0f; // DELETE THIS!!!!

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
	
	public void Jump()
	{
		if (timeSinceLastJump == jumpCoolDownTime)
		{
			timeSinceLastJump = 0.0f;
			playerRB.AddRelativeForce(Vector3.up * (jumpForceMultiplier + GetDownwardVelocity()) * playerRB.mass, ForceMode.Impulse);
			SetIsGrounded(false);
		}
	}
	
	public float GetVerticalCameraAngle()
	{
		return verticalAngle;
	}
	
	public void SetIsGrounded(bool groundedState)
	{
		if (groundedState)
		{
			if (jumpQueue_isQueued)
			{
				Jump();
				jumpQueue_isQueued = false;
			}	
			else
			{
				moveSpeed.reduction.Set("regular");
				isGrounded = true;
			}
		}
		else
		{
			moveSpeed.reduction.Set("air");
			isGrounded = false;
		}
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
		
		if (relativeVelocity.y < 0.0f) currentDownwardSpeed = -relativeVelocity.y;
		else currentDownwardSpeed = 0.0f;
		
		if (currentDownwardSpeed > moveSpeed.verticalMax)
		{
			fallCancelForce = new Vector3(0.0f, ((currentDownwardSpeed - moveSpeed.verticalMax) * playerRB.mass), 0.0f) -gravity;
			
			playerRB.AddRelativeForce(fallCancelForce, ForceMode.Impulse);
		}
	}
}