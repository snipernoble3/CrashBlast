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
	private Rigidbody rigidbody;
	
	private float rotation_vertical = 0.0f;
	private float rotation_horizontal = 0.0f;
		
	private Vector3 movement_vector;
	public float movement_vectorRequestedMegnitude;
	
	// Jumping Variables
	[SerializeField] private float jumpForceMultiplier =  400.0f;
	private const float groundCheckDistance = 0.15f;
	private bool isGrounded = true;
	private bool wasGroundedLastFrame = true;
	
	// Rocket Jumping Variables
	private int rjBlast_NumSinceGrounded = 0;
	[SerializeField] private const int rjBlast_NumLimit = 2;
	private Vector3 rjBlast_Epicenter;
	[SerializeField] private const float rjBlast_Range = 3.0f;
	[SerializeField] private const float rjBlast_Radius = 5.0f;
	[SerializeField] private const float rjBlast_Power = 550.0f;
	[SerializeField] private const float rjBlast_UpPower = 0.5f;
	
	[SerializeField] private float movement_SpeedMultiplier = 50.0f;
	private float movement_SpeedReduction_Multiplier = 1.0f;
	[SerializeField] private const float movement_SpeedReduction_Air = 0.5f;
	[SerializeField] private const float movement_SpeedReduction_Water = 0.75f;
	
	[SerializeField] private const float movement_MaxSpeed = 20.0f;
	
	// Mouse Input
	[SerializeField] private float mouseSensitivity_X = 6.0f;
	[SerializeField] private float mouseSensitivity_Y = 3.0f;
	[SerializeField] private bool matchXYSensitivity = false;
	[SerializeField] private bool useRawMouseInput = true;
	[SerializeField] private bool invertVerticalInput = false;
	[SerializeField] private const float lookUpDownAngle_Max = 90.0f;
	[SerializeField] private const float lookUpDownAngle_Min = -90.0f;
	private float lookUpDownAngle_Current = 0.0f;
    
	// Start is called before the first frame update
    void Awake()
    {
		// Hide the cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
		
		// Set up references
		firstPersonCam = transform.Find("Camera Position Offset/Main Camera").gameObject;
		camOffset = transform.Find("Camera Position Offset").gameObject;
        rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
		GetMouseInput();
		LookUpDown();
		
		if (isGrounded && Input.GetButtonDown("Jump")) Jump();
		CheckIfGrounded();
		
		if (Input.GetButtonDown("Fire2")) RocketJump();
		
		// Get input for Movement:
		// Get input from project input manager and build a Vector3 to store the two inputs
		movement_vector = new Vector3 (Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
		// Limit the magnitude of the vector so that horizontal and vertical movement doesn't stack to excede the indended maximum move speed
		movement_vector = Vector3.ClampMagnitude(movement_vector, 1.0f);
		// Multiply with delta time to ensure consistency between computers and framerates.
		//movement_vector *= Time.deltaTime;
		//movement_vector *= Time.fixedDeltaTime; // Movement is physics based, so fixedDeltaTime is used instead of regular deltaTime
		// Multiply the movement vector with the speed multiplider
		movement_vector *= movement_SpeedMultiplier;
    }
	
	void Jump()
	{
		rigidbody.AddRelativeForce(Vector3.up * jumpForceMultiplier, ForceMode.Impulse);
	}
	
	void RocketJump()
	{
		if (rjBlast_NumSinceGrounded < rjBlast_NumLimit)
		{
			RaycastHit hit;
			if(Physics.Raycast(firstPersonCam.transform.position, firstPersonCam.transform.forward, out hit, rjBlast_Range))
			{
				rjBlast_Epicenter = hit.point;
			}
			else rjBlast_Epicenter = firstPersonCam.transform.position + (firstPersonCam.transform.forward * rjBlast_Range);
			
			
			Collider[] colliders = Physics.OverlapSphere(rjBlast_Epicenter, rjBlast_Radius);
			foreach (Collider objectToBlast in colliders)
			{
				Rigidbody rb = objectToBlast.GetComponent<Rigidbody>();
				if (rb != null) // If the object has a rigidbody component
				{
					// If this is the player, check if the player is grounded, if so, don't add the rocket jump force to the player.
					if (rb == rigidbody && isGrounded) continue;
					rb.AddExplosionForce(rjBlast_Power, rjBlast_Epicenter, rjBlast_Radius, rjBlast_UpPower, ForceMode.Impulse);
				}
			}
			
			if (!isGrounded) rjBlast_NumSinceGrounded += 1;
		}
	}
	
	void CheckIfGrounded()
	{
		if (Physics.SphereCast(transform.position + Vector3.up, 0.95f, Vector3.down, out RaycastHit hit, groundCheckDistance))
		{
			isGrounded = true;
			if (rjBlast_NumSinceGrounded > 0) StartCoroutine(UpDownCameraShake(0.6f, 5.0f, 25.0f));
			rjBlast_NumSinceGrounded = 0;
		}
		else
		{
			isGrounded = false;
		}
	}
	
	void GetMouseInput()
	{
		if (matchXYSensitivity) mouseSensitivity_Y = mouseSensitivity_X;
		
		if (useRawMouseInput) rotation_horizontal = Input.GetAxisRaw("Mouse X") * mouseSensitivity_X;
		else rotation_horizontal = Input.GetAxis("Mouse X") * mouseSensitivity_X;
		
		if (useRawMouseInput) rotation_vertical = -Input.GetAxisRaw("Mouse Y") * mouseSensitivity_Y;
		else rotation_vertical = -Input.GetAxis("Mouse Y") * mouseSensitivity_Y;
		if (invertVerticalInput) rotation_vertical *= -1;
	}
	
	void LookUpDown()
	{
		lookUpDownAngle_Current += rotation_vertical;
		lookUpDownAngle_Current = Mathf.Clamp(lookUpDownAngle_Current, lookUpDownAngle_Min, lookUpDownAngle_Max);
			
		firstPersonCam.transform.localRotation = Quaternion.AngleAxis(lookUpDownAngle_Current, Vector3.right);
	}
	
	private IEnumerator UpDownCameraShake(float shake_Duration, float shake_amplitude, float shake_frequency)
	{
		if (userPreference_EnableLandingShake) // If the player has chosen to disable camera shake, then do nothing.
		{
			float fade_Rate_In = 5.0f;
			float fade_Rate_Out = 3.0f;
			float fade_Duration = (1.0f / fade_Rate_In) + (1.0f / fade_Rate_Out);
		
			if (fade_Duration < shake_Duration) // If there won't be enough time to complete the fade in and the fade out, then don't shake at all.
			{
				float fade_Multiplier = 0.0f; // Start with the shake faded out completely.
				float fade_Progress = 0.0f; // Fade progress in the Lerp is 0.0f
				float fade_Rate = fade_Rate_In; // Set the fade rate so that the shake will fade in over time.
				
				bool fade_isFadingIn = true;
				bool fade_isFadingOut = false;
				
				float shake_timeElapsed = 0.0f;
				float shake_UpDownAngle = 0.0f;
				
				while (shake_timeElapsed < shake_Duration)
				{
					shake_UpDownAngle = Mathf.Sin(Time.time * shake_frequency) * shake_amplitude * fade_Multiplier;
					
					if (fade_isFadingIn || fade_isFadingOut)
					{
						fade_Multiplier = Mathf.Lerp(0.0f, 1.0f, fade_Progress);
						fade_Progress = Mathf.Clamp(fade_Progress + (fade_Rate * Time.deltaTime), 0.0f, 1.0f);
						if (fade_Progress == 1.0f) fade_isFadingIn = false;
					}
					
					if (!fade_isFadingIn && !fade_isFadingOut && shake_timeElapsed < shake_Duration + (fade_Rate_Out * Time.deltaTime))
					{
						fade_Rate = -fade_Rate_Out;
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
			else Debug.Log("Requested shake duration was " + shake_Duration + " seconds, but the time needed to fade in and out is " + fade_Duration + " seconds, so the shake was not performed.");
		}
	}
	
	void FixedUpdate()
	{
		LookLeftRight();
		
		//if (rigidbody.velocity.magnitude <= movement_MaxSpeed)
		
		// Check if adding the requested input movement vector to the current player velocity will result in the player moving too fast.
		// If the resulting velocity is fater than the max move speed, then don't add any new force.
		
		//movement_vectorRequestedMegnitude = rigidbody.velocity.magnitude + movement_vector.magnitude;
		
		//if (rigidbody.velocity.magnitude + movement_vector.magnitude < movement_MaxSpeed)
		//{		
			if (isGrounded) movement_SpeedReduction_Multiplier = 1.0f;
			else movement_SpeedReduction_Multiplier = movement_SpeedReduction_Air;
			
			rigidbody.AddRelativeForce(movement_vector * movement_SpeedReduction_Multiplier, ForceMode.Impulse);
			//rigidbody.AddRelativeForce (rigidbody.velocity +  movement_vector * movement_SpeedReduction_Multiplier, ForceMode.VelocityChange);
		//}
	}
	
	void LookLeftRight()
	{
		Quaternion deltaRotation = Quaternion.Euler(new Vector3(0.0f, rotation_horizontal, 0.0f));
		rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);
	}
}