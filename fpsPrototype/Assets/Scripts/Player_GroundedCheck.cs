using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_GroundedCheck : MonoBehaviour
{
    public LayerMask groundingLayers;
	private Player_Movement playerMovement;
	
	void Awake()
	{
		playerMovement = transform.parent.GetComponent<Player_Movement>();
		playerMovement.SetIsGrounded(true);
	}
	
	void OnTriggerEnter(Collider collision)
	{
		// Check if the collision detected is on one of the groundingLayers
		// Bool check provided by https://answers.unity.com/questions/50279/check-if-layer-is-in-layermask.html
		if (groundingLayers == (groundingLayers | (1 << collision.gameObject.layer)))
		{
			playerMovement.SetIsGrounded(true);
		}
	}
	
	void OnTriggerExit(Collider collision)
    {
        playerMovement.SetIsGrounded(false);
    }
}
