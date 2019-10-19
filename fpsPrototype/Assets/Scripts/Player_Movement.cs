using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MoveSpeed
{
	public float acceleration = 0.0f;
	
	// Speed caps for different situations
	public float max;
	
	public readonly float groundedMax = 10.0f; // Cap the walking speed.
	public readonly float bHopMax = 25.0f; // Cap the bunny hopping speed.
	
	public readonly float lateralMax = 50.0f; // NEVER let the player move faster than this lateraly.	
	public readonly float verticalMax = 75.0f; // NEVER let the player move faster than this verticaly.	
	
	public Vector3 inputVector; // Raw input from the player.
	public Vector3 requestVector; // Input from the player multiplied by the rampUp and speedReduction values.
	public Vector3 projectedVector;
	public Vector3 outputVector; // The final vector that will be used to apply force to the player.
	
	
	public Vector3 localVelocity;
	public Vector3 localVelocity_Lateral;
	
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
	public bool holdSpaceToKeepJumping = true;
	[SerializeField] private float jumpForceMultiplier =  3.0f;
	private const float jumpCoolDownTime = 0.2f;
	private float timeSinceLastJump;
	private bool jumpQueue_isQueued = false;
	private const float jumpQueue_Expiration = 0.3f; // How long will the jump stay queued.
	private float jumpQueue_TimeSinceQueued = 0.0f; // How long has it been since the jump was queued.
	private const float jumpQueue_bHopGracePeriod = 0.3f; // How long before friction starts being applied to the player.
	private float jumpQueue_timeSinceGrounded = 0.0f; // How long has it been since the player became grounded.
	
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
		if (jumpQueue_TimeSinceQueued < jumpQueue_Expiration) jumpQueue_TimeSinceQueued = Mathf.Clamp(jumpQueue_TimeSinceQueued += 1.0f * Time.deltaTime, 0.0f, jumpQueue_Expiration);
		if (jumpQueue_TimeSinceQueued == jumpQueue_Expiration) jumpQueue_isQueued = false;
		// Count how long it's been since the player became grounded.
		if (jumpQueue_timeSinceGrounded < jumpQueue_bHopGracePeriod) jumpQueue_timeSinceGrounded = Mathf.Clamp(jumpQueue_timeSinceGrounded += 1.0f * Time.deltaTime, 0.0f, jumpQueue_bHopGracePeriod);

		
		
		
		
		// Inputs
		GetInput_Mouse();
		MouseLook();
		GetInput_LateralMovement();
		
		
		if (holdSpaceToKeepJumping && Input.GetButton("Jump"))
		{
			jumpQueue_isQueued = true;
			jumpQueue_TimeSinceQueued = 0.0f;
		}
			
		if (Input.GetButtonDown("Jump"))
		{
			if (isGrounded) Jump();
			else
			{
				jumpQueue_isQueued = true;
				jumpQueue_TimeSinceQueued = 0.0f;
			}
		}
    }
	
	void FixedUpdate()
	{		
		// Convert the velocity vector from world space to local space.
		moveSpeed.localVelocity = transform.InverseTransformDirection(playerRB.velocity);
		// In many cases we are only concerned with the lateral part of the local space velocity vector, so the vertical axis is zeroed out.
		moveSpeed.localVelocity_Lateral = new Vector3(moveSpeed.localVelocity.x, 0.0f, moveSpeed.localVelocity.z);
		
		gravity = GetGravity();

		LateralMovement(); // Move the player based on the lateral movement input.
		//GroundMove();
		
		
		// Slow the player down with "friction" if he is grounded and not trying to move.
		//if (moveSpeed.inputVector == Vector3.zero && playerRB.velocity != Vector3.zero && isGrounded) SimulateFriction();
		if (isGrounded && jumpQueue_timeSinceGrounded == jumpQueue_bHopGracePeriod) SimulateFriction();
		
		TerminalVelocity();
		
		if (hud != null) UpdateHUD(); // Update HUD elements.
	}
	
	void UpdateHUD()
	{
		//hud_LateralVelocity.text = "Lateral Velocity: " + moveSpeed.localVelocity_Lateral.magnitude.ToString("F2");
		//hud_VerticalVelocity.text = "Vertical Velocity: " + moveSpeed.localVelocity.y.ToString("F2");
			
		radarLines[0].SetVector(new Vector3(moveSpeed.requestVector.x, moveSpeed.requestVector.z, -2.0f));
		radarLines[1].SetVector(new Vector3(moveSpeed.localVelocity_Lateral.x, moveSpeed.localVelocity_Lateral.z, -1.0f));
	}
	
	private void GetInput_LateralMovement()
	{
		// Get input for Movement from input manager and build a Vector3 to store the two inputs
		moveSpeed.inputVector = new Vector3 (Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
		// Limit the magnitude of the vector so that horizontal and vertical input doesn't stack to excede the indended maximum move speed
		moveSpeed.inputVector = Vector3.ClampMagnitude(moveSpeed.inputVector, 1.0f);
	}
	
	private void GroundMove()
    {
        // Calculate how fast the player is asking to go.
		moveSpeed.acceleration = 640.0f; // The base speed of the acceleration.
		// The input vector's megnitute will equal 1.0f if the movement keys are held down long enough (determined by input manager sensitivity settings) OR if the analogue input is maxed out.
		moveSpeed.acceleration *= moveSpeed.inputVector.magnitude;
		// The reduction multiplier is 1.0f by default, but will be reduced in the air or in water.
		moveSpeed.acceleration *= moveSpeed.reduction.multiplier;
		// Multiply by Time.fixedDeltaTime so that speed is not bound to inconsitencies in the physics time step. 
		moveSpeed.acceleration *= Time.fixedDeltaTime;
		
		
        
		
		float currentspeed = Vector3.Dot(moveSpeed.localVelocity_Lateral, moveSpeed.inputVector.normalized);
		
		float addspeed = moveSpeed.acceleration - currentspeed;
        
		if(addspeed <= 0) return;
        if(moveSpeed.acceleration > addspeed) moveSpeed.acceleration = addspeed;

        //playerVelocity.x += moveSpeed.acceleration * moveSpeed.inputVector.x;
        //playerVelocity.z += moveSpeed.acceleration * moveSpeed.inputVector.z;
		
/*
		
		//Build the requested vector via the input direction and the desired acceleration.
		moveSpeed.requestVector = moveSpeed.inputVector.normalized * moveSpeed.acceleration;

		// Subtract the player's current lateral velocity in order to get the force need to send the player in the desired direction.
		moveSpeed.requestVector -= moveSpeed.localVelocity_Lateral;

		// Apply the calculated force to the player in local space
		playerRB.AddRelativeForce(moveSpeed.requestVector, ForceMode.VelocityChange);
		
*/
	}
	
	// Emulate Quake's "vector limiting" air strafe code to enable Bunny Hopping.
	private void Accelerate_Lateral()
	{
		// Start by Projecting the player's current velocity vector onto the input acceleration direction (modified to have a length of 1.0f).
		// The Quake approach uses a dot product calculation as a roundabout way of getting the number we care about instead of doing a proper (expensive) vector projection.
		// This is the same as getting Vector3.Project(moveSpeed.localVelocity_Lateral, moveSpeed.inputVector.normalized).magnitude which is the only part of the projection we care about.
		float projectedSpeed = Vector3.Dot(moveSpeed.localVelocity_Lateral, moveSpeed.inputVector.normalized); // Vector projection of current velocity onto a unit vector input direction.
		
		
		radarLines[2].SetVector(new Vector3(moveSpeed.projectedVector.x, moveSpeed.projectedVector.z, -0.5f));
		
		
		// Although the dot product result isn't representitive of the actual velocity, it can still be used for the speed comparison:
		// Calculate the difference between the speed player is asking to go, and the result of the "projected" vector (the dot product calculation).
		float speedToAdd = moveSpeed.acceleration - projectedSpeed;
		
		// Do nothing if this value goes negative. NEVER let this method decelerate the player.
		if (speedToAdd <= 0.0f) return;
		

		if (moveSpeed.acceleration > speedToAdd) moveSpeed.acceleration = speedToAdd;
		
		// Normalizing the inputVector doesn't matter because we've already gotten the good out of it's length eariler while calculating the moveSpeed.acceleration value;
		moveSpeed.outputVector = moveSpeed.inputVector.normalized * moveSpeed.acceleration;
		
		// If necessary, truncate the accelerated velocity so the vector projection does not exceed max_velocity
		//if (projectedSpeed + moveSpeed.acceleration > moveSpeed.max) moveSpeed.acceleration = moveSpeed.max - projectedSpeed;
		
		
		// Apply the calculated force to the player in local space
		playerRB.AddRelativeForce(moveSpeed.outputVector, ForceMode.VelocityChange);
	}
	
	
	
	
	
	
	
	// Add lateral movement via the physics system (doesn't affect vertical velocity).
	private void LateralMovement()
	{
	// GROUND MOVE v

		// Calculate how fast the player is asking to go.
		moveSpeed.acceleration = 640.0f; // The base speed of the acceleration.
		// The input vector's megnitute will equal 1.0f if the movement keys are held down long enough (determined by input manager sensitivity settings) OR if the analogue input is maxed out.
		moveSpeed.acceleration *= moveSpeed.inputVector.magnitude;
		// The reduction multiplier is 1.0f by default, but will be reduced in the air or in water.
		moveSpeed.acceleration *= moveSpeed.reduction.multiplier;
		// Multiply by Time.fixedDeltaTime so that speed is not bound to inconsitencies in the physics time step. 
		moveSpeed.acceleration *= Time.fixedDeltaTime;
		
		//Build the requested vector via the input direction and the desired acceleration.
		moveSpeed.requestVector = moveSpeed.inputVector.normalized * moveSpeed.acceleration;
		
		
		// Calculate how fast the player is alowed to go.
		moveSpeed.max = moveSpeed.groundedMax;
		//if (!isGrounded) moveSpeed.max = moveSpeed.bHopMax;
		
		// reduce how fast the player maxes out at if the input is not maxed out (primarily for analogue input).
		moveSpeed.max *= moveSpeed.inputVector.magnitude;
		// The reduction multiplier is 1.0f by default, but will be reduced in the air or in water.
		moveSpeed.max *= moveSpeed.reduction.multiplier;
		// Multiply by Time.fixedDeltaTime so that speed is not bound to inconsitencies in the physics time step. 
		moveSpeed.max *= Time.fixedDeltaTime;

		Accelerate_Lateral();
		
	// GROUND MOVE ^
	// AIR MOVE v
	
		
		
		
		
		
		
		
		
		
		
	
		
		
		
		//moveSpeed.acceleration -= projectedSpeed;
		//moveSpeed.acceleration = Mathf.Clamp(moveSpeed.acceleration, 0.0f, moveSpeed.max);
		
		
		//moveSpeed.projectedVector = moveSpeed.localVelocity_Lateral + moveSpeed.inputVector.normalized * moveSpeed.acceleration;


		/*
		//Emulate Quake's vector limiting code so as to enable Bunny Hopping.
		float projectedSpeed = Vector3.Dot(moveSpeed.localVelocity_Lateral, moveSpeed.inputVector.normalized);
		float addspeed = moveSpeed.acceleration - projectedSpeed;
		addspeed = Mathf.Clamp(addspeed, 0.0f, moveSpeed.max);
		
		
		moveSpeed.projectedVector = moveSpeed.localVelocity_Lateral + moveSpeed.inputVector.normalized * addspeed;
		*/
	
		/*

		// Calculate what the lateral velocity will be if we add the requested force BEFORE actually adding the force.
		Vector3 moveVector_Test = moveSpeed.localVelocity_Lateral + moveSpeed.requestVector;
		
		
		// If the requested movement vector is too high, calculate how much force we have to add to maintain top speed without going past it.
		if (moveVector_Test.magnitude > moveSpeed.input) moveSpeed.requestVector = moveSpeed.requestVector.normalized * Mathf.Clamp(moveSpeed.input - moveSpeed.localVelocity_Lateral.magnitude, 0.0f, moveSpeed.input);
		// Apply the calculated force to the player in local space
		playerRB.AddRelativeForce(moveSpeed.requestVector, ForceMode.Impulse);
	
	
	
		/*

		moveSpeed.outputVector = moveSpeed.localVelocity_Lateral + (moveSpeed.inputVector.normalized * moveSpeed.acceleration);
		
		// Subtract the player's current lateral velocity in order to get the force need to send the player in the desired direction.
		moveSpeed.outputVector -= moveSpeed.localVelocity_Lateral;
		
		*/
		
		
		
		/*
		
		// If the requested movement vector is too high, calculate how much force we have to add to maintain top speed without going past it.
		if (moveVector_Test.magnitude > moveSpeed.input) moveSpeed.requestVector = moveSpeed.requestVector.normalized * Mathf.Clamp(moveSpeed.input - moveSpeed.localVelocity_Lateral.magnitude, 0.0f, moveSpeed.input);
		// Apply the calculated force to the player in local space
		playerRB.AddRelativeForce(moveSpeed.requestVector, ForceMode.Impulse);
		*/
		
		//moveSpeed.acceleration
		

		
		





		
		//Emulate Quake's vector limiting code so as to enable Bunny Hopping.
		
			// Vector projection of Current velocity onto input Movement direction.
			//float projVel = Vector3.Dot(moveSpeed.localVelocity_Lateral, moveSpeed.inputVector.normalized);
		
			// If necessary, truncate the accelerated velocity so the vector projection does not exceed max velocity
			//if(projVel + rampUpMultiplier > moveSpeed.groundedMax) rampUpMultiplier = moveSpeed.groundedMax - projVel;
		
		//	moveSpeed.projectedVector = moveSpeed.localVelocity_Lateral + moveSpeed.requestVector;
		
			//moveSpeed.projectedVector = Vector3.ClampMagnitude(moveSpeed.projectedVector, moveSpeed.bHopMax);
		
			//moveSpeed.projectedVector = Vector3.ClampMagnitude(moveSpeed.projectedVector, moveSpeed.acceleration);
			
		//	moveSpeed.projectedVector = Vector3.ClampMagnitude(moveSpeed.projectedVector, moveSpeed.requestVector.magnitude);
		
		//	Vector3 desiredVelocity = new Vector3(moveSpeed.projectedVector.x, 0.0f, moveSpeed.projectedVector.z);
	
		
		/*
		// Check if the player is changing direction
		if (Vector3.Angle(moveSpeed.localVelocity_Lateral, moveSpeed.requestVector) != 0.0f) 
		{
			float redirectInfluence = 1.0f; // Change 100%
		
			//Vector3 directionChangeVector = moveSpeed.requestVector.normalized * moveSpeed.localVelocity_Lateral.magnitude * redirectInfluence;
			//directionChangeVector -= moveSpeed.localVelocity_Lateral;
			//playerRB.velocity = directionChangeVector;
		
			Vector3 directionChangeVector = Vector3.Project(moveSpeed.requestVector, moveSpeed.localVelocity_Lateral);
			//directionChangeVector -= moveSpeed.localVelocity_Lateral;
			//directionChangeVector *= playerRB.mass;
			playerRB.AddRelativeForce(directionChangeVector, ForceMode.Impulse);
		}	
		*/
	}
	
	private void SimulateFriction()
	{
		float rampDownMultiplier = 10.0f;
		rampDownMultiplier *= Time.fixedDeltaTime; // Multiply by Time.fixedDeltaTime so that friction speed is not bound to inconsitencies in the physics time step.
		
		Vector3 frictionForceToAdd = -moveSpeed.localVelocity; // Start with a force in the opposite direction of the player's velocity.
		frictionForceToAdd -= transform.InverseTransformDirection(gravity);  // Counter gravity (in local space) so that the player won't slip off of edges.
		
		// Decide if vertical friction should be applied:
		//float verticalFrictionMultiplier = 1.0f; // We need some vertical counter force so the player doesn't slip off of edges.
		//if (frictionForceToAdd.y <= 0.02f) verticalFrictionMultiplier = 0.0f; // Don't slow down the player's force if he is trying to jump.
		float verticalFrictionMultiplier = 0.0f;
		
		// Multiply the rampDownMultiplier with the x and z components so that speed will ramp down for lateral movment, but the vertical friction will be instantanious if enabled.
		frictionForceToAdd = Vector3.Scale(frictionForceToAdd, new Vector3(rampDownMultiplier, verticalFrictionMultiplier, rampDownMultiplier));
		
		playerRB.AddRelativeForce(frictionForceToAdd, ForceMode.VelocityChange); // Apply the friction to the player in local space.
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
			//SetIsGrounded(false);
		}
	}
	
	public float GetVerticalCameraAngle()
	{
		return verticalAngle;
	}
	
	public void SetIsGrounded(bool groundedState)
	{
		if (groundedState) // Check if the player just became grounded.
		{
			if (jumpQueue_isQueued) // If the player has a jump queued, jump without switching to ground movement.
			{
				Jump();
				jumpQueue_isQueued = false;
			}	
			else // If the player didn't have a jump queued, switch to ground movement and become grounded.
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
		float currentVertical = Mathf.Abs(moveSpeed.localVelocity.y);
		float currentLateral = moveSpeed.localVelocity_Lateral.magnitude;
		
		float cancelForce_Vertical = 0.0f;
		float cancelForce_Lateral = 0.0f;
		
		if (currentVertical > moveSpeed.verticalMax) cancelForce_Vertical = currentVertical - moveSpeed.verticalMax;
		if (currentLateral > moveSpeed.lateralMax) cancelForce_Lateral = currentLateral - moveSpeed.lateralMax;
		
		Vector3 cancelVector = -moveSpeed.localVelocity_Lateral.normalized * cancelForce_Lateral;
		cancelVector += (Vector3.up * cancelForce_Vertical);
		
		if (cancelVector != Vector3.zero) playerRB.AddRelativeForce(cancelVector, ForceMode.VelocityChange);
	
	
		/*
		Vector3 fallCancelForce;
		float currentDownwardSpeed = 0.0f;
		
		if (moveSpeed.localVelocity.y < 0.0f) currentDownwardSpeed = -moveSpeed.localVelocity.y;
		else currentDownwardSpeed = 0.0f;
		
		if (currentDownwardSpeed > moveSpeed.verticalMax)
		{
			fallCancelForce = new Vector3(0.0f, ((currentDownwardSpeed - moveSpeed.verticalMax) * playerRB.mass), 0.0f) -gravity;
			
			playerRB.AddRelativeForce(fallCancelForce, ForceMode.Impulse);
		}
		*/
	}
}