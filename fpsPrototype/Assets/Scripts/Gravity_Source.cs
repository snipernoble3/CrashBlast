using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity_Source : MonoBehaviour
{
	public float gravityStrength = -9.81f;
	private float angularSpeed = 360.0f;
	
	public Vector3 GetGravityVector(Transform attractedObject)
	{
		return (attractedObject.position - transform.position).normalized * gravityStrength;
	}
	
	public void AttractObject(Transform attractedObject, float blend)
	{
		// Rotate the attracted object so that its downward direction faces the gravity source
		Vector3 gravityVector = GetGravityVector(attractedObject);
		
		// Set the target rotation (aim at gravity source)
		Quaternion targetRotation = Quaternion.FromToRotation(-attractedObject.up, gravityVector.normalized) * attractedObject.rotation;
		
		// Blend the object's rotation toward the gravity source.
		//attractedObject.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angularSpeed);
		
		//attractedObject.rotation = targetRotation;
		
		attractedObject.rotation = Quaternion.Slerp(attractedObject.rotation, targetRotation, blend);
		
		// Add force to the attracted object to simulate gravity toward the gravity source.
		attractedObject.gameObject.GetComponent<Rigidbody>().AddForce(gravityVector, ForceMode.Acceleration);
	}
}
