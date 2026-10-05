using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections;

[ExecuteInEditMode]
public class FollowSceneView : MonoBehaviour {

	Vector3 initialPosition;
	Transform thisTransform;

	public bool LockY;
	public bool isOn = true;

	void OnEnable()
	{
		thisTransform = transform;
		initialPosition = thisTransform.position;
	}


	void OnDrawGizmos()
	{
		if(enabled && !Application.isPlaying && isOn)
			Follow ();
	}

	void Follow ()
	{
		Vector3 position = SceneView.lastActiveSceneView.pivot; 
		position.z = initialPosition.z;
		if(LockY)position.y = thisTransform.position.y;

		thisTransform.position = position;
	}


	void OnDisable()
	{
		thisTransform.position = initialPosition;
	}
}

#endif
