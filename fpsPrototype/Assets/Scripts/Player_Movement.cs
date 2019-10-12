using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Player_Movement : MonoBehaviour
{
	// User Preferences
	public bool userPreference_EnableLandingShake = true;
	public bool userPreference_autoAddJumpForceToGroundedRocketJump = true;
	
	// Object References
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
	
	public float cameraAngle = 0.0f;
	
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
		firstPersonCam = transform.Find("Camera Position Offset/Main Camera").gameObject;
		camOffset = transform.Find("Camera Position Offset").gameObject;
        playerRB = GetComponent<Rigidbody>();
    }

    void Update()
    {		
		cameraAngle = firstPersonCam.transform.eulerAngles.x;
		
		GetInput_LateralMovement();
		GetInput_Mouse();
		LookUpDown();
		
		if (Input.GetButton("Fire1")) firstPersonArms_Animator.SetBool("fire", true);
		else firstPersonArms_Animator.SetBool("fire", false);
		
		
		if (Input.GetButtonDown("Fire2")) RocketJumpCheck();
		if (Input.GetButtonDown("Jump") && IsGrounded()) Jump();
		if (Input.GetButton("Crouch") &&  !IsGrounded()) AccelerateDown();
    }
	
	void FixedUpdate()
	{
		LateralMovement(inputMovementVector);
		Vector3 resultMoveVector = new Vector3(playerRB.velocity.x, 0.0f, playerRB.velocity.z);
		hud_Velocity.text = "Lateral Velocity: " + resultMoveVector.magnitude.ToString("F2");
		
		LookLeftRight();
		GroundPoundCheck();
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
				rjBlast_NumSinceGrounded = 0;
				return true;
			}
		}
		moveSpeedReduction = moveSpeedReduction_Air;
		return false;
	}
	
	public float GetDownwardVelocity()
	{
		if (IsGrounded()) return 0.0f; // If the player is grounded , then there is no downward velocity.
		if (playerRB.velocity.y > 0.0f || Mathf.Approximately(playerRB.velocity.y, 0.0f)) return 0.0f; // If the player isn't moving vertically or the player is going up, then there is no downward velocity.
		else return Mathf.Abs(playerRB.velocity.y); // If the player is falling, update the downward velocity to match.
	}
	
	void Jump()
	{
		playerRB.AddRelativeForce(Vector3.up * jumpForceMultiplier, ForceMode.Impulse);
	}
	
	// Called via player input, Checks if the conditions to be able to rocket jump are met,
	// if so, calls the rocket jump method that performs the rocket jump.
	private void RocketJumpCheck()
	{	
		if (rjBlast_NumSinceGrounded < rjBlast_NumLimit) // Don't bother with the rest of the check if the player has used up all of his rocket jumps.
		{
			// Determine where the rocket jump blast's center point should be.
			if (Physics.Raycast(firstPersonCam.transform.position, firstPersonCam.transform.forward, out RaycastHit hit, rjBlast_Range, LayerMask.NameToLayer("Player"))) rjBlast_Epicenter = hit.point;
			else rjBlast_Epicenter = firstPersonCam.transform.position + (firstPersonCam.transform.forward * rjBlast_Range);
		
			firstPersonArms_Animator.Play("FirstPersonArms_Blast", 1, 0.25f); // Play the blast animation.
			BlastForce(rjBlast_Power, rjBlast_Epicenter, rjBlast_Radius, rjBlast_UpwardForce); // Add the blast force to affect other objects.
			
			bool blastOffAngleOK = false;
			if (firstPersonCam.transform.localEulerAngles.x <= 90.0f && firstPersonCam.transform.localEulerAngles.x >= 45.0f) blastOffAngleOK = true;
			
			if (IsGrounded())
			{
				if (blastOffAngleOK)
				{
					// Force the player into the air (as if he jumped) before applying the rocket jump to get a compounding force.
					if (userPreference_autoAddJumpForceToGroundedRocketJump) StartCoroutine(JumpThenRocketJump());
					else RocketJump(); // Rocket jump wihtout jumping first.
				}
			}
			else RocketJump();
		}
	}

	// Called via the Rocket Jump Check method, this actually performs the rocket jump.
	private void RocketJump()
	{
		playerRB.AddExplosionForce(rjBlast_Power, rjBlast_Epicenter, rjBlast_Radius, rjBlast_UpwardForce, ForceMode.Impulse);
		rjBlast_NumSinceGrounded += 1;
	}
	
	private void AccelerateDown()
	{
		// If the player is currently accelerating upward, instantly canncel upward velocity, then apply downward force.
		if (playerRB.velocity.y > 0.0) playerRB.velocity = new Vector3 (playerRB.velocity.x, 0.0f, playerRB.velocity.z);
		playerRB.AddRelativeForce(Vector3.down * groundPound_Multiplier, ForceMode.Acceleration);
	}
	
	private void GroundPoundCheck()
	{
		if(GetDownwardVelocity() != 0.0f) impactVelocity = GetDownwardVelocity(); // Set the "previous velocity" at this physics step so it can be compared during the next physics step.
		if (impactVelocity != 0.0f && GetDownwardVelocity() == 0.0f && impactVelocity >= minGroundPoundVelocity)
		{
			float gpBlast_Power = 65.0f * impactVelocity;
			float gpBlast_Radius = impactVelocity * 0.3f; // 5.0f;
			float gpBlast_UpwardForce = 0.5f;
			
			float camShake_Amplitude = Mathf.Clamp(Mathf.Pow(impactVelocity * 0.04f, 3.0f), 0.5f, 20.0f);
			float camShake_Frequency = Mathf.Clamp(impactVelocity * 2.0f, 15.0f, 25.0f);
			float camShake_Duration = Mathf.Clamp((impactVelocity * 0.02f), 0.0f, 0.7f);
			
			// Camera Shake
			StartCoroutine(CameraShake(camShake_Amplitude, camShake_Frequency, camShake_Duration, camShake_Duration * 0.2f, camShake_Duration * 0.5f));
			
			// Blast Force
			BlastForce(gpBlast_Power, playerRB.position, gpBlast_Radius, gpBlast_UpwardForce); // Apply a blast around the landing
			
			// Particle System
			
			if (gpParticles_GameObject != null) Destroy(gpParticles_GameObject); // to prevent multiple particle systems from spawning when clipping the corner of rounded objects.
	
			float gpParticle_Duration = camShake_Duration * 3.0f;			
			gpParticles_GameObject = Instantiate(groundPoundParticles, playerRB.position, Quaternion.identity) as GameObject;
			ParticleSystem.MainModule gpParticles_MainModule = gpParticles_GameObject.GetComponent<ParticleSystem>().main;
			
			gpParticles_MainModule.startSpeed = Mathf.Clamp(impactVelocity, 5.0f, 50.0f);
			gpParticles_MainModule.startLifetime = gpParticle_Duration;
			gpParticles_MainModule.duration = gpParticle_Duration;
			gpParticles_GameObject.GetComponent<ParticleSystem>().Play();
			StartCoroutine(ParticleTimer(gpParticle_Duration, gpParticles_GameObject));
			
			// Reset downward velocity.
			impactVelocity = 0.0f;
		}
	}
	
	private void BlastForce(float blast_Power, Vector3 blast_Epicenter, float blast_Radius, float blast_UpwardForce)
	{
		// Check all objects within the blast radius.
		Collider[] colliders = Physics.OverlapSphere(blast_Epicenter, blast_Radius);
		foreach (Collider objectToBlast in colliders)
		{
			Rigidbody rb = objectToBlast.GetComponent<Rigidbody>();
			// If the object has a rigidbody component and it is not the player, add the blast force!
			if (rb != null && rb != playerRB) rb.AddExplosionForce(blast_Power, blast_Epicenter, blast_Radius, blast_UpwardForce, ForceMode.Impulse);
		}
	}
	
	// This method includes a tiny delay to ensure that the counter for mid-air rocket jumps isn't thrown off
	// AND so that the two forces aren't applied inhumanly fast resulting in a higher jump than is otherwise humanly reflexivly possible.
	public IEnumerator JumpThenRocketJump()
	{
		Jump();
		yield return new WaitForSeconds(0.1f);
		RocketJump();
	}
	
	public IEnumerator ParticleTimer (float seconds, GameObject particleObjectToDestroy)
	{
		yield return new WaitForSeconds(seconds);
		
		Destroy(particleObjectToDestroy);
	}
	
	public IEnumerator CameraShake(float shake_amplitude, float shake_frequency, float shake_Duration, float fade_Duration_In, float fade_Duration_Out)
	{
		if (userPreference_EnableLandingShake) // If the player has chosen to disable camera shake, then do nothing.
		{
			if (fade_Duration_In + fade_Duration_Out < shake_Duration) // If there won't be enough time to complete the fade in and the fade out, then don't shake at all.
			{
				float fade_Min = 0.0f;
				float fade_Max = 1.0f;

				float fade_Multiplier = fade_Min; // Start with the shake faded out completely.
				float fade_Rate = fade_Max / fade_Duration_In; // Set the fade rate so that the shake will fade in over time.
				
				bool fade_isFadingIn = true;
				bool fade_isFadingOut = false;
				
				float shake_timeElapsed = 0.0f;
				float shake_Angle_Pitch = 0.0f;
				float shake_Angle_Roll = 0.0f;
				
				while (shake_timeElapsed < shake_Duration)
				{
					shake_Angle_Pitch = Mathf.Sin(Time.time * shake_frequency) * shake_amplitude * fade_Multiplier;
					shake_Angle_Roll = Mathf.Cos(Time.time * shake_frequency) * shake_amplitude * fade_Multiplier * 0.5f;
					
					// Increase or decrease the fade multiplier
					if (fade_isFadingIn || fade_isFadingOut)
					{
						fade_Multiplier = Mathf.Clamp(fade_Multiplier + (fade_Rate * Time.deltaTime), fade_Min, fade_Max);
						if (fade_Multiplier == fade_Max) fade_isFadingIn = false;
					}
					
					// Check if it's time to start fading out
					if (!fade_isFadingIn && !fade_isFadingOut && shake_timeElapsed >= shake_Duration - fade_Duration_Out)
					{
						fade_Rate = fade_Max / -fade_Duration_Out;
						fade_isFadingOut = true;
					}
					
					// Apply the shake to the camera
					camOffset.transform.localEulerAngles = new Vector3(shake_Angle_Pitch, 0.0f, shake_Angle_Roll);	
					
					shake_timeElapsed += Time.deltaTime;

					yield return null;
				}
				
				// Ensure that the camera is back to zero when the shake is done.
				camOffset.transform.localRotation = Quaternion.identity;
			}
			else Debug.Log("Fade duration ("+ (fade_Duration_In + fade_Duration_Out) +" seconds) can not be longer than the shake duration (" + shake_Duration + " seconds). The camera shake was not performed.");
		}
	}
}