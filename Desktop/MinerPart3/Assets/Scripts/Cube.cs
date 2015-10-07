using UnityEngine;
using System.Collections;

public class Cube : MonoBehaviour {
	public oreType myType;
	public int myPoints;
	public GameObject textPrefab;

	GameObject myController;

	bool isRotating = false;

	float growthRate = 2f; //absolute scale increase per second
	float rotationRate = 90f;

	Vector3 randRotAxis;

	// Use this for initialization
	void Start () {
		gameObject.transform.localScale = new Vector3(0,0,0);
		randRotAxis = Random.onUnitSphere;
		StartCoroutine ("changeScale", 1);
		//StartCoroutine ("changeRotation", Quaternion.AngleAxis (90, rotationAxis));
		StartCoroutine ("changeRotation", 0.5f);
		//lastRotationAxis = Quaternion.AngleAxis (10, Random.onUnitSphere) * lastRotationAxis;
	}
	//this function gets called by the game controller, giving the cube the controller's reference,
	// so this cube can send information back to the controller, which eliminates the need to use 
	// static score and cube counts, which would be a BAD THING (tm)
	public void Give (GameObject controller) {
		myController = controller;
	}

	void OnMouseDown(){
		myController.GetComponent<GameController> ().dropCube (gameObject);
		myController.GetComponent<GameController> ().changeScore (myPoints);
		GameObject myFadePoints = Instantiate (textPrefab, gameObject.transform.position, Quaternion.identity) as GameObject;
		myFadePoints.GetComponent<PointsText> ().pointsToShow (myPoints);
		Destroy (gameObject);
	}

	IEnumerator changeScale(float targetScale){
		Vector3 startScale = gameObject.transform.localScale;
		Vector3 endScale = new Vector3(targetScale,targetScale,targetScale);
		float growthDuration = Mathf.Abs((targetScale - startScale.x) / growthRate);//how long should it take for the change x/x' = t.
		for (float t = 0; t < growthDuration; t = Mathf.Clamp(t + Time.deltaTime,0,growthDuration)) {
			gameObject.transform.localScale = Vector3.Lerp(startScale,endScale,t/growthDuration);
			yield return null;
		}
	}
	/*Ok, so this function needs some explanation. randRotAxis is initialzed on creation as a random unit vector. This function shifts that axis
	 * 15 degrees in a random direction, then rotates the cube around that axis for rotDuration seconds. The isRotating bools at the beginning 
	 * and end allow OnMouseOver to check if this rotation is currently running so it doesn't start it on top of itself. An explanation of 
	 * Quaternions is beyond the scope of a comment, but there are plenty of resources at Unity3D.com
	 * (check docs.unity3d.com/ScriptReference/Quaternion.html   
	 */
	 
	IEnumerator changeRotation(float rotDuration){
		isRotating = true;
		Quaternion startRotation = gameObject.transform.rotation;
		randRotAxis = Quaternion.AngleAxis (15, Random.onUnitSphere) * randRotAxis;
		Quaternion targetRotation = Quaternion.AngleAxis (rotDuration * rotationRate, randRotAxis) * startRotation;
		for (float t = 0; t < rotDuration; t += Time.deltaTime) {
			gameObject.transform.rotation = Quaternion.Slerp (startRotation, targetRotation, t / rotDuration);
			yield return null;
		}
		isRotating = false;
	}

	void OnMouseEnter(){
		StopAllCoroutines();
		gameObject.transform.localScale = new Vector3 (1.2f, 1.2f, 1.2f);
		StartCoroutine ("changeRotation", 0.5f);
	}
	void OnMouseExit(){
		StopAllCoroutines();
		StartCoroutine ("changeScale", 1);
	}
	void OnMouseOver(){
		if (isRotating == false) {
			StopCoroutine ("changeRotation");
			StartCoroutine ("changeRotation", 0.5f);
		}
	}

	// Update is called once per frame
	void Update () {
	
	}
}
/*
	IEnumerator changeRotation(Quaternion targetRotation){
		isRotating = true;
		Quaternion startRotation = gameObject.transform.rotation;
		float rotDuration = Quaternion.Angle (targetRotation, startRotation) / rotationRate;
		for (float t = 0; t < rotDuration; t += Time.deltaTime) {
			gameObject.transform.rotation = Quaternion.Slerp (startRotation, targetRotation, t / rotDuration);
			yield return null;
		}
		isRotating = false;
		rotationAxis = Quaternion.AngleAxis (45, Random.onUnitSphere) * rotationAxis;
	}/**/