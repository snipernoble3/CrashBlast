using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity_AreaOfEffect : MonoBehaviour
{
    private Gravity_Source gravitySource;
	
	// Start is called before the first frame update
    void Awake()
    {
        gravitySource = transform.parent.GetComponent<Gravity_Source>();	
    }
	
	private void OnTriggerEnter(Collider triggeredObject)
    {
        Gravity_AttractedObject attractedObject = triggeredObject.transform.GetComponent<Gravity_AttractedObject>();
		
		if (attractedObject != null)
		{
			attractedObject.blendToNewSource = 0.0f;
			attractedObject.gravitySource = this.gravitySource;
			
			triggeredObject.transform.SetParent(transform.parent);
		}
		
		Player_Movement player = triggeredObject.transform.GetComponent<Player_Movement>();
		
		if (player != null)
		{
			player.gravitySource = this.gravitySource;
		}
    }
}
