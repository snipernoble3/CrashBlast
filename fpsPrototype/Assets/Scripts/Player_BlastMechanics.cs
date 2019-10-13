﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script handles Rocket Jumping, Ground Pounding, and Object Blasting.
[RequireComponent(typeof(Player_Movement))]
public class Player_BlastMechanics : MonoBehaviour
{
	// User Preferences
	public bool enableCameraShake_gpLanding = true;
	public bool autoJumpBeforeGroundedRocketJump = true;
	
	// Object References
	private Player_Movement playerMovement;
	public GameObject groundPoundParticles; // gets reference to the particles to spawn
	private GameObject gpParticles_GameObject; // stores the instance of the particles
	private GameObject firstPersonCam;
	private GameObject camOffset;
	
	private Rigidbody playerRB;
	public Animator firstPersonArms_Animator;
	
	// Rocket Jumping Variables
	public int rjBlast_NumSinceGrounded = 0;
	[SerializeField] private const int rjBlast_NumLimit = 100; // set this back to 2 after testing
	
	private const float rjBlast_Range = 3.0f;
	private const float rjBlast_Power = 550.0f;
	private Vector3 rjBlast_Epicenter; // The origin of the rocket jump blast radius.
	private const float rjBlast_Radius = 5.0f;
	private const float rjBlast_UpwardForce = 0.0f;
	private const float minRocketJumpCameraAngle = 45.0f;
	
	private float impactVelocity = 0.0f;
	public float minGroundPoundVelocity = 8.0f;
	public float groundPound_Multiplier = 25.0f;
    
    void Awake()
    {		
		// Set up references
		playerMovement = GetComponent<Player_Movement>();
		firstPersonCam = transform.Find("Camera Position Offset/Main Camera").gameObject;
		camOffset = transform.Find("Camera Position Offset").gameObject;
        playerRB = GetComponent<Rigidbody>();
    }

    void Update()
    {		
		if (Input.GetButtonDown("Fire2")) RocketJumpCheck();
		if (Input.GetButton("Crouch") &&  !playerMovement.GetIsGrounded()) AccelerateDown();
    }
	
	void FixedUpdate()
	{	
		GroundPoundCheck();
		if (playerMovement.GetIsGrounded()) rjBlast_NumSinceGrounded = 0;
	}
	
	public float GetDownwardVelocity()
	{
		if (playerMovement.GetIsGrounded()) return 0.0f; // If the player is grounded , then there is no downward velocity.
		if (playerRB.velocity.y > 0.0f || Mathf.Approximately(playerRB.velocity.y, 0.0f)) return 0.0f; // If the player isn't moving vertically or the player is going up, then there is no downward velocity.
		else return Mathf.Abs(playerRB.velocity.y); // If the player is falling, update the downward velocity to match.
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
			
			if (playerMovement.GetIsGrounded())
			{
				if (blastOffAngleOK)
				{
					// Force the player into the air (as if he jumped) before applying the rocket jump to get a compounding force.
					if (autoJumpBeforeGroundedRocketJump) StartCoroutine(JumpThenRocketJump());
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
		//if (playerRB.velocity.y > 0.0) playerRB.velocity = new Vector3 (playerRB.velocity.x, 0.0f, playerRB.velocity.z);
		playerRB.AddRelativeForce(playerMovement.GetGravity().normalized * groundPound_Multiplier, ForceMode.Acceleration);
		//playerMovement.TerminalVelocity();
		
		Debug.Log("We're going down");
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
		playerMovement.Jump(); // Comunicate with the Player_Movement script to force the player to jump.
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
		if (enableCameraShake_gpLanding) // If the player has chosen to disable camera shake, then do nothing.
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
