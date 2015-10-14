using UnityEngine;
using System.Collections;

public class PointsText : MonoBehaviour {
	float duration = 1f;
	float upVelocity = 0.5f;
	float spawnTime;

	// Use this for initialization
	void Start () {
		spawnTime = Time.time;
	}
	public void pointsToShow(int newPoints){
		gameObject.GetComponent<TextMesh> ().text = string.Concat ("+", newPoints.ToString ());
	}
	// Update is called once per frame
	void Update () {
		if (Time.time < (spawnTime + duration)) {
			Color newColor = gameObject.GetComponent<TextMesh>().color;
			newColor.a = 1 - (Time.time - spawnTime) / duration;
			//print((Time.time - spawnTime)/duration);
			gameObject.GetComponent<Renderer> ().material.color = newColor;
			gameObject.transform.Translate(Time.deltaTime * upVelocity * Vector3.up);
		} else {
			Destroy (gameObject);
		}
	}
}
