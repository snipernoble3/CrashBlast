using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity_AreaOfEffect : MonoBehaviour
{
    private Gravity_Source gravitySource;
	private Transform defaultGravitySource;
	
	// Start is called before the first frame update
    void Awake()
    {
        gravitySource = transform.parent.GetComponent<Gravity_Source>();
		
		defaultGravitySource = transform.parent.parent; // The parent of the AOE is the source, get the parent of THAT object.
		
		if (defaultGravitySource != null)
		{
			if (defaultGravitySource.GetComponent<Gravity_Source>() == null) defaultGravitySource = transform.root; 	
		}
    }
	
	private void OnTriggerEnter(Collider triggeredObject)
    {
        Gravity_AttractedObject attractedObject = triggeredObject.transform.GetComponent<Gravity_AttractedObject>();
		
		if (attractedObject != null)
		{
			//attractedObject.timeSinceSourceChange = 0.0f; // Reset the timer on the attractedObject.
			//attractedObject.blendToNewSource = 0.0f;
			//attractedObject.gravitySource = this.gravitySource;
			
			attractedObject.SetGravitySource(this.gravitySource);
			triggeredObject.transform.SetParent(transform.parent);
		}
		
		Player_Movement player = triggeredObject.transform.GetComponent<Player_Movement>();
		
		if (player != null)
		{
			player.gravitySource = this.gravitySource;
		}
    }
	
	private void OnTriggerExit(Collider triggeredObject)
    {
        Gravity_AttractedObject exitingObject = triggeredObject.transform.GetComponent<Gravity_AttractedObject>();
		
		if (exitingObject != null)
		{
			
			
			//exitingObject.timeSinceSourceChange = 0.0f; // Reset the timer on the attractedObject.
			//exitingObject.blendToNewSource = 0.0f;
			//exitingObject.gravitySource = defaultGravitySource.GetComponent<Gravity_Source>();
			
			exitingObject.SetGravitySource(defaultGravitySource.GetComponent<Gravity_Source>());
			triggeredObject.transform.SetParent(defaultGravitySource);
		}
		
		Player_Movement player = triggeredObject.transform.GetComponent<Player_Movement>();
		
		if (player != null)
		{
			player.gravitySource = defaultGravitySource.GetComponent<Gravity_Source>();
		}
    }
}
