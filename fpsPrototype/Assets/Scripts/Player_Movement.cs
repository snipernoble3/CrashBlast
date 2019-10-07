using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Movement : MonoBehaviour
{
	// User Preference Head Bob
	public bool userPreference_EnableLandingShake = true;
	
	// Object References
	private GameObject camOffset;
	private GameObject firstPersonCam;
	private Rigidbody playerRB;
		
	private Vector3 movement_vector;
	public float movement_vectorRequestedMegnitude;
	
	// Jumping Variables
	[SerializeField] private float jumpForceMultiplier =  400.0f;
	
	private float impactVelocity = 0.0f;
	public float minGroundPoundVelocity = 5.5f;
	public float groundPound_Multiplier = 25.0f;
	
	// Rocket Jumping Variables
	private int rjBlast_NumSinceGrounded = 0;
	[SerializeField] private const int rjBlast_NumLimit = 2;
	
	[SerializeField] private const float rjBlast_Range = 3.0f;
	[SerializeField] private const float rjBlast_Power = 550.0f;
	private Vector3 rjBlast_Epicenter;
	[SerializeField] private const float rjBlast_Radius = 5.0f;
	[SerializeField] private const float rjBlast_UpwardForce = 0.5f;
	
	[SerializeField] private float movement_SpeedMultiplier = 50.0f;
	[SerializeField] private const float movement_MaxSpeed = 20.0f;
	private float movement_SpeedReduction_Multiplier = 1.0f;
	[SerializeField] private const float movement_SpeedReduction_Air = 0.5f;
	[SerializeField] private const float movement_SpeedReduction_Water = 0.75f;
	
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
    
    void Awake()
    {
		// Hide the cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
		
		// Set up references
		firstPersonCam = transform.Find("Camera Position Offset/Main Camera").gameObject;
		camOffset = transform.Find("Camera Position Offset").gameObject;
        playerRB = GetComponent<Rigidbody>();
    }

    void Update()
    {		
		GetInput_LateralMovement();
		GetInput_Mouse();
		LookUpDown();
		
		if (Input.GetButtonDown("Fire2")) RocketJump();
		if (Input.GetButtonDown("Jump") && IsGrounded()) Jump();
		if (Input.GetButton("Crouch") &&  !IsGrounded()) AccelerateDown();
    }
	
	void FixedUpdate()
	{
		MoveLateral();
		LookLeftRight();
		GroundPoundCheck();
	}
	
	private void GetInput_LateralMovement()
	{
		// Get input for Movement from project input manager and build a Vector3 to store the two inputs
		movement_vector = new Vector3 (Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
		// Limit the magnitude of the vector so that horizontal and vertical movement doesn't stack to excede the indended maximum move speed
		movement_vector = Vector3.ClampMagnitude(movement_vector, 1.0f);
		// Multiply the movement vector with the speed multiplider
		movement_vector *= movement_SpeedMultiplier;
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
	
	private void MoveLateral()
	{
		//if (playerRB.velocity.magnitude <= movement_MaxSpeed)
		
		// Check if adding the requested input movement vector to the current player velocity will result in the player moving too fast.
		// If the resulting velocity is fater than the max move speed, then don't add any new force.
		
		//movement_vectorRequestedMegnitude = playerRB.velocity.magnitude + movement_vector.magnitude;
		
		//if (playerRB.velocity.magnitude + movement_vector.magnitude < movement_MaxSpeed)
		if (true)
		{		
			playerRB.AddRelativeForce(movement_vector * movement_SpeedReduction_Multiplier, ForceMode.Impulse);
			//playerRB.AddRelativeForce (playerRB.velocity +  movement_vector * movement_SpeedReduction_Multiplier, ForceMode.VelocityChange);
		}
	}
	
	public bool IsGrounded()
	{
		RaycastHit[] hits = Physics.SphereCastAll(playerRB.position + (Vector3.up * 0.49f), 0.49f, Vector3.down, 0.1f);
		foreach (RaycastHit groundCheckObject in hits)
		{
			if (groundCheckObject.rigidbody == playerRB) continue;
			if (groundCheckObject.collider != null)
			{
				movement_SpeedReduction_Multiplier = 1.0f;
				rjBlast_NumSinceGrounded = 0;
				return true;
			}
		}
		movement_SpeedReduction_Multiplier = movement_SpeedReduction_Air;
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
	
	private void RocketJump()
	{
		if (rjBlast_NumSinceGrounded < rjBlast_NumLimit)
		{
			if(Physics.Raycast(firstPersonCam.transform.position, firstPersonCam.transform.forward, out RaycastHit hit, rjBlast_Range, LayerMask.NameToLayer("Player"))) rjBlast_Epicenter = hit.point;
			else rjBlast_Epicenter = firstPersonCam.transform.position + (firstPersonCam.transform.forward * rjBlast_Range);
			
			BlastForce(rjBlast_Power, rjBlast_Epicenter, rjBlast_Radius, rjBlast_UpwardForce);
			
			if (!IsGrounded())
			{
				playerRB.AddExplosionForce(rjBlast_Power, rjBlast_Epicenter, rjBlast_Radius, 0.0f, ForceMode.Impulse);
				rjBlast_NumSinceGrounded += 1;
			}
		}
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
			float gpBlast_Power = 80.0f * impactVelocity;
			float gpBlast_Radius = 5.0f;
			float gpBlast_UpwardForce = 0.5f;
			
			StartCoroutine(UpDownCameraShake(impactVelocity * 0.1f, 25.0f, 0.5f, 0.1f, 0.25f));
			BlastForce(gpBlast_Power, playerRB.position, gpBlast_Radius, gpBlast_UpwardForce); // Apply a blast around the landing
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
	
	public IEnumerator UpDownCameraShake(float shake_amplitude, float shake_frequency, float shake_Duration, float fade_Duration_In, float fade_Duration_Out)
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
				float shake_UpDownAngle = 0.0f;
				
				while (shake_timeElapsed < shake_Duration)
				{
					shake_UpDownAngle = Mathf.Sin(Time.time * shake_frequency) * shake_amplitude * fade_Multiplier;
					
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
					
					// Shake the camera
					camOffset.transform.localRotation = Quaternion.AngleAxis(shake_UpDownAngle, Vector3.right);	
					
					shake_timeElapsed += Time.deltaTime;

					yield return null;
				}
				
				// Ensure that the camera is back to zero when the shake is done.
				camOffset.transform.localRotation = Quaternion.AngleAxis(0.0f, Vector3.right);
			}
			else Debug.Log("Fade duration ("+ (fade_Duration_In + fade_Duration_Out) +" seconds) can not be longer than the shake duration (" + shake_Duration + " seconds). The camera shake was not performed.");
		}
	}
}