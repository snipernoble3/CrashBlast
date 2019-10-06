using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Movement : MonoBehaviour
{
	// User Preference Head Bob
	public bool enableLandingShake = true;
	
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
		firstPersonCam = transform.Find("Camera Position Offset/First Person Camera").gameObject;
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
			if (enableLandingShake && rjBlast_NumSinceGrounded > 0) StartCoroutine(UpDownCameraShake(0.6f, 5.0f, 25.0f));
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
	
	private IEnumerator UpDownCameraShake(float duration, float amplitude, float frequency)
	{
		float fadeInOut = 0.0f;
		
		bool isFadingIn = true;
		bool isFadingOut = false;
		
		float fadeIn_Rate = 5.0f;
		float fadeOut_Rate = -3.0f;
		
		float fadeInOut_Rate = fadeIn_Rate;
		float fadeInOut_Progress = 0.0f;
		
		float timeElapsed = 0.0f;
		float shakeUpDown = 0.0f;
		
		while (timeElapsed < duration)
		{
			shakeUpDown = Mathf.Sin(Time.time * frequency) * amplitude * fadeInOut;
			
			if (isFadingIn || isFadingOut)
			{
				fadeInOut = Mathf.Lerp(0.0f, 1.0f, fadeInOut_Progress);
				fadeInOut_Progress = Mathf.Clamp(fadeInOut_Progress + (fadeInOut_Rate * Time.deltaTime), 0.0f, 1.0f);
				if (fadeInOut_Progress == 1.0f) isFadingIn = false;
			}
			
			if (!isFadingIn && !isFadingOut && timeElapsed < duration + (fadeOut_Rate * Time.deltaTime))
			{
				fadeInOut_Rate = fadeOut_Rate;
				isFadingOut = true;
			}
			
			camOffset.transform.localRotation = Quaternion.AngleAxis(shakeUpDown, Vector3.right);	
			
			timeElapsed += Time.deltaTime;
			
			yield return null;
		}
		
		camOffset.transform.localRotation = Quaternion.AngleAxis(0.0f, Vector3.right);
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