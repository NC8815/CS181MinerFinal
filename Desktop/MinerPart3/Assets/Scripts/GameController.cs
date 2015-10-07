using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameController : MonoBehaviour {
	public GameObject prefabBronze; //these are the cubes that get spawned. They are defined in the inspector
	public GameObject prefabSilver;
	public GameObject prefabGold;
	public GameObject gameCamera;	//this is the main camera for the game. It's defined in the inspector.
	public GameObject backdrop;

	public float spawnTime = 3f;    //time between cube spawns.

	int score = 0;					//score. duh.

	float cameraChangeTime = 1f;

	List<GameObject> allCubes = new List<GameObject>(); //a list of all cubes currently in the game, used for camera dynamics

	int bronzeCount = 0; //running counts of the number of bronze, silver, and gold cubes, used to determine the next cube.
	int silverCount = 0;
	int goldCount = 0;

	public float maxNumCubes = 100;

	float targetCameraSize;			// these variables deal with dynamically changing the camera to fit the current cube set.
	Vector3 targetCameraLoc;		// the targets are redefined whenever the camera is centered.
	float timeCameraLastCentered;	// the CameraLastCentered variables are initialized in start and updated whenever
	float sizeCameraLastCentered;	// the camera is centered.
	Vector3 locCameraLastCentered;	//

	float timeToAct; //this is a counter to spawn the next cube. it's initialized in start.

	oreType lastSpawned; //redefined whenever a cube is spawned; used to prevent gold spawning indefinitely
	
	void Start () {
		timeToAct = spawnTime;//the first time to act is after spawnFreq seconds have passed from start
		timeCameraLastCentered = centerCamera();//center the camera for the first time
	}

	//the cubes call this function when clicked, so it needs to be public
	public void changeScore(int deltaScore){
		score += deltaScore;
	}

	//for enum switch cases, since there are a finite number of values, if you have them all as cases, you don't need a default case.
	//the cubes call this function when clicked, so it needs to be public
	public void dropCube(GameObject cube){
		allCubes.Remove (cube);
		switch (cube.GetComponent<Cube> ().myType) {
		case oreType.Bronze:
			bronzeCount--;
			break;
		case oreType.Silver:
			silverCount--;
			break;
		case oreType.Gold:
			goldCount--;
			break;
		}
		timeCameraLastCentered = centerCamera ();//recenter the camera whenever you drop a cube
	}

	void addCube (GameObject cubeToAdd, Vector2 spawnLoc){
		GameObject myCube = Instantiate (cubeToAdd, spawnLoc, Random.rotation) as GameObject;
		myCube.GetComponent<Cube> ().Give (gameObject);
		allCubes.Add (myCube);
		switch (myCube.GetComponent<Cube> ().myType) {
		case oreType.Bronze:
			bronzeCount++;
			break;
		case oreType.Silver:
			silverCount++;
			break;
		case oreType.Gold:
			goldCount++;
			break;
		}
		timeCameraLastCentered = centerCamera ();//recenter the camera whenever you add a cube
	}
	
	// the following logic arguments decide what cube to spawn. It was unclear whether I should only allow one gold to spawn ever,
	// or only one at a time, or only one in a row. I interpreted it to mean only spawn a gold cube if there aren't any gold cubes, 
	// and also the last cube spawned wasn't a gold cube, so the player can't just keep clicking the gold cube.

	// regarding the return values, return will set lastSpawned to the relevant type, then pass that type back to whatever called the function.
	oreType oreToSpawn (){
		if (bronzeCount == 2 && silverCount == 2 && lastSpawned != oreType.Gold && goldCount == 0) {
			return lastSpawned = oreType.Gold;
		} else if (bronzeCount < 4) {
			return lastSpawned = oreType.Bronze;
		} else 
			return lastSpawned = oreType.Silver;
	}

	// find a spot, then there's less than the maximum number of cubes, spawn it. The maximum number of cubes will inform the spawn range and max zoom
	void spawnCube(){
		Vector3 spawnXY = getSpawnPoint ();
		if (allCubes.Count < maxNumCubes) {
			switch (oreToSpawn ()) {
			case oreType.Bronze:
				addCube (prefabBronze, spawnXY);
				break;
			case oreType.Silver:
				addCube (prefabSilver, spawnXY);
				break;
			case oreType.Gold:
				addCube (prefabGold, spawnXY);
				break;
			}
		}
	}

	//vector3 can take two arguments, it will default the 3rd one to 0.
	
	// this loop will try up to 100 times to find an empty spawnpoint in the camera field, then increase the size of 
	// subsequent searches every 100 times until it finds a spawn point. This will ALWAYS find a spawn point,
	// so addCube won't actually spawn the cube if there are too many, and the size of the spawn area being dependant 
	// the max number of cubes ensures there will be room for a cube in the spawn area.
	//the - 1 on spawnSize gives a border for the spawn area just inside the camera border.

	Vector3 getSpawnPoint(){
		float spawnSize = targetCameraSize - 1; //simplifying further declarations...
		float aspectRatio = gameCamera.GetComponent<Camera> ().aspect;
		Vector3 camera = targetCameraLoc; //we base the spawn field on where the camera SHOULD be, not where it is, because it may not be finished moving

		float spawnXMin = camera.x - (spawnSize) * aspectRatio;//borders of the spawn area
		float spawnXMax = camera.x + (spawnSize) * aspectRatio;
		float spawnYMin = camera.y - (spawnSize); 
		float spawnYMax = camera.y + (spawnSize); 

		float testX = Random.Range(spawnXMin,spawnXMax); //test point
		float testY = Random.Range(spawnYMin,spawnYMax);
		//search for a point, physics checksphere returns true if anything is colliding with a sphere at that point. That is, 
		// if there's no room there, it returns true, so continue with the search loop.
		do{	for(int spawnAttempts = 0; spawnAttempts < 100 && Physics.CheckSphere(new Vector3(testX,testY),1.5f);spawnAttempts++){
				testX = Random.Range(spawnXMin,spawnXMax);
				testY = Random.Range(spawnYMin,spawnYMax);}
			spawnXMin -= aspectRatio; //expand the next search area
			spawnXMax += aspectRatio;
			spawnYMin --; 
			spawnYMax ++;
		} while (Physics.CheckSphere(new Vector3(testX,testY),1.5f));//did we find a point? if not, keep searching
		return new Vector2 (testX, testY); //once the final checksphere returns false (found an empty space)
	}

	//This function finds the furthest out values of x and y that contain cubes, and changes the zoom and location of the camera. 
	//If there are zero or one cubes in the game, return the current camera instead.
	public float centerCamera(){
		if (allCubes.Count > 1){
			//start all of these at the first cube, then see if each additional cube is further to the left, right, top, or center
			float cubeXMin = allCubes[0].transform.position.x;
			float cubeXMax = allCubes[0].transform.position.x;
			float cubeYMin = allCubes[0].transform.position.y;
			float cubeYMax = allCubes[0].transform.position.y;
			float aspectRatio = gameCamera.GetComponent<Camera> ().aspect;
			foreach (GameObject cube in allCubes) {
				Vector3 cubeLoc = cube.transform.position;
				cubeXMin = Mathf.Min (cubeXMin, cubeLoc.x);
				cubeXMax = Mathf.Max (cubeXMax, cubeLoc.x);
				cubeYMin = Mathf.Min (cubeYMin, cubeLoc.y);
				cubeYMax = Mathf.Max (cubeYMax, cubeLoc.y);
			}
			float avgX = (cubeXMax + cubeXMin)/2;
			float avgY = (cubeYMax + cubeYMin)/2;
			targetCameraLoc = new Vector3(avgX,avgY);	

			float width = (cubeXMax - cubeXMin);
			float height = (cubeYMax - cubeYMin);
			float minCameraSize = Mathf.Sqrt(maxNumCubes)/2;//the maximum and minimum camera sizes are determined by the max number of cubes
			float maxCameraSize = Mathf.Sqrt(maxNumCubes);
			if ((width / height) >= aspectRatio) //we compare the cube rectangle's aspect ratio to the cameras, we don't want any cubes lost offscreen
				targetCameraSize = Mathf.Clamp( width / 2 / aspectRatio + 1, minCameraSize, maxCameraSize);
			else
				targetCameraSize = Mathf.Clamp( height / 2 + 1, minCameraSize, maxCameraSize);
		} else {//edge case for when there are 0 or 1 cubes
			targetCameraLoc = gameCamera.transform.position;
			targetCameraSize = gameCamera.GetComponent<Camera> ().orthographicSize;
		}
		//now that we've centered the camera, set the base point for our camera interpolations.
		sizeCameraLastCentered = gameCamera.GetComponent<Camera> ().orthographicSize;
		locCameraLastCentered = gameCamera.transform.position;
		return Time.time;
	}

	void OnGUI() {
		float hours = Mathf.Floor (Time.time / 3600);
		float minutes = Mathf.Floor (Time.time / 60);
		float seconds = Time.time % 60;
		string time = Mathf.RoundToInt(seconds).ToString();
		if (minutes != 0)
			time = minutes.ToString() + ":" + time;
		if (hours != 0)
			time = hours.ToString() + ":" + time;

		GUI.Box(new Rect(0, 0, 70, 50), "Score\n" + score);
		GUI.Box (new Rect (0, 50, 70, 50), "Time\n" + time);
	}

	//each frame check if the time to act has arrived. If it has, set the next time to act, then spawn a cube
	void Update () {
		if (Time.time > timeToAct) {
			timeToAct += spawnTime;
			spawnCube ();
		}
		//you have to declare this after the time to act is stepped up, because timeCameraLastCentered is changed by SpawnCube()
		float t = (Time.time - timeCameraLastCentered) / cameraChangeTime;
		//camera interpolation
		if (gameCamera.GetComponent<Camera> ().orthographicSize != targetCameraSize) {
			gameCamera.GetComponent<Camera> ().orthographicSize = Mathf.SmoothStep (sizeCameraLastCentered, targetCameraSize, t);
		}
		if (gameCamera.transform.position != targetCameraLoc) {
			gameCamera.transform.position = new Vector3 (Mathf.Lerp (locCameraLastCentered.x, targetCameraLoc.x, t),
			                                            Mathf.Lerp (locCameraLastCentered.y, targetCameraLoc.y, t),
			                                             -10);
		}
		//moving the backdrop and making sure it fills the camera
		backdrop.transform.position = new Vector3 (gameCamera.transform.position.x, gameCamera.transform.position.y, backdrop.transform.position.z);
		if (gameCamera.GetComponent<Camera> ().aspect >= 1) {
			backdrop.transform.localScale = 0.2f * Mathf.Sqrt (maxNumCubes) * gameCamera.GetComponent<Camera> ().aspect  * Vector3.one;
		} else {
			backdrop.transform.localScale = 0.2f * Mathf.Sqrt (maxNumCubes) * Vector3.one;
		}
	}
}
