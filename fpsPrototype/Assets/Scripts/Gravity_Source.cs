using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity_Source : MonoBehaviour
{
	[SerializeField] private bool isRadial = true;
	[SerializeField] private float gravityStrength = -9.81f;
	public Vector3 nonRadialDirection;
	
	private void Awake()
	{
		nonRadialDirection = transform.up;
	}
	
	public Vector3 GetGravityVector(Transform attractedObject)
	{
		if (isRadial) return (attractedObject.position - transform.position).normalized * gravityStrength;
		else return nonRadialDirection * gravityStrength;
	}
	
	public void AttractObject(Transform attractedObject, float blend)
	{
		// Rotate the attracted object so that its downward direction faces the gravity source
		Vector3 gravityVector = GetGravityVector(attractedObject);
		
		// Set the target rotation (aim at gravity source)
		Quaternion targetRotation = Quaternion.FromToRotation(-attractedObject.up, gravityVector.normalized) * attractedObject.rotation;
				
		attractedObject.rotation = Quaternion.Slerp(attractedObject.rotation, targetRotation, blend);
		
		// Add force to the attracted object to simulate gravity toward the gravity source.
		attractedObject.gameObject.GetComponent<Rigidbody>().AddForce(gravityVector, ForceMode.Acceleration);
	}
}
