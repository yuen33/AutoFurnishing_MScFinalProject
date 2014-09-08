using UnityEngine;
using System.Collections;
using System.Collections.Generic;//for List<T>, Queue<T>
using System.IO;

public class populatingT1 : MonoBehaviour {
	float isFullFactor=0.3f;
	//for testing
	string path;
	string filename;
	System.IO.StreamWriter file;
	public int sumOfIterations=0;
	//Input:
	public static string floorName;
	public static GameObject floorMesh;

	//-------------------Do not support many-to-one----------------------------
	//e.g. "table to chair", "tv to chair" is not valid
	public string[][] pairwiseTAG={
		new string[]{"table","chair"},//on Face 2
		new string[]{"tv","sofa"},//on Face 2, as far as possible
		new string[]{"sofa","teatable"},//on Face 2
		new string[]{"bed","bedside_table"}, //on Face 3 or 4, and (itself) Face 5 along wall
		new string[]{"single_bed","bedside_table"}//not all bed needs a bedsidetable...(as above)
	};
	//-----------------------------------------------


	public bool isInitialised=false;
	public bool isStable=false;
	public static bool isfinished=false;
//	public bool secondaryPairPopulated=false;
	
	
	List<string> nameList;
	List<string> secondaryPairList;//{its primary pair name, its name}
	List<string> primaryPairList;
	public static Vector3[,] globalBestT1;//(center)(rotation)(extents) for global best record
	Vector3[,] lastPosition;
	public static List<GameObject> boxesT1;
	
	Vector3[][]Doors;//(namecode,wallID,0)(center)(extents) Vector3(width,depth,height)
	Vector3[][]Fireplaces;//(namecode,wallID,0)(center)(extents) Vector3(width,depth,height)
	Vector3[][]Windows;//(namecode,wallID,0)(center)(extents) Vector3(width,depth,height)

	int populatingState=0;//max=num Of Populated furniture
	
	public double globalBest=-999999;
	public double currentDistanceScore;
	double lastDistanceScore;
	public double distanceScoreDifference;

	public double currentSumOfScores;
	double lastSumOfScores=-99999;
	public double sumOfScoresDifference;

	double[] lastSingleScores;
	public double[] currentSingleScores;
	public float step=1f;
	public double beta=0.1f;
	public int[] iteration;
	double distanceFactor=1;
	Vector3 floorplanRotation;
	
	// Use this for initialization
	void Start () {
	/** Rooms to be furnished: (tested roomtype example result)
	 * F1:
	 * room_4_646 ->bedroom
	 * room_3_644 ->bedroom
	 * room_2_642 ->bathroom
	 * room_1_640 ->*reading room*
	 * 
	 * G0:
	 * room_8_653 is living room
	 * room_11_659 is bathroom
	 * room_12_661 is *reading room*
	 * 
	 * B1:
	 * room_10_657 is living room
	 * room_5_648 is *kitchen*
	 * 
	 * **:can be single bedroom/kitchen/reading room
	 */
		floorName="room_4_646";
		floorMesh=GameObject.Find(floorName);
		Room.enabled=true;
		boxesT1=new List<GameObject>();
//		Random.seed=Room.roomID;
		floorplanRotation=gameObject.transform.eulerAngles;


		path="Assets/Autofurnishing/scripts/";
		filename=floorName+"_NumericalResults.txt";
		// Write the string to a file.
		file = new System.IO.StreamWriter(path+filename);

	}

	// Update is called once per frame
	void Update () {
		if(!isInitialised && PopulatingGuide.Instance.isfinished
		   && InRoomRetrieval.Instance.isfinished){
			initialization();
//			Time.timeScale=0;//pause the game
//			getScore();
			getOverallScore();
//			Time.timeScale=1;//unpause the game

			//-----End-----
			isInitialised=true;
			lastSumOfScores=currentDistanceScore;

			iteration=new int[nameList.Count];
		}//if(PopulatingGuide.Instance.isfinished

		if(isInitialised && !isStable){
//			foreach(GameObject box in boxesT1){
//				Debug.LogError("furniture "+box.name+" out");
//				if(!isInRoom(box)) moveintotheRoom(boxesT1.IndexOf(box));
//			}

			lastDistanceScore=currentDistanceScore;
			getOverallScore();
			if(Mathf.Abs((float)(currentDistanceScore-lastDistanceScore))<step){
				isStable=true;
			}
		}

		if(isStable && !isfinished){
			
			recordLastPosition();
			simulatedAnnealingControl();

			//move
			for(int i=0;i<populatingState;i++){
				if(Random.value>0.995){
					int k=populatingState-1;
					int idx=Mathf.FloorToInt(Random.value*k %k)+1;//won't be the biggest cube
					jumpOne(idx);
				}

				if(!isInRoom(boxesT1[i])){
					moveintotheRoom(boxesT1.IndexOf(boxesT1[i]));
				}else{
					rotate(boxesT1[i]);

					move(boxesT1[i],i);
				}
			}

			/**
			 * Scores handling
			 */
			lastSingleScores=new double[populatingState];
			if(populatingState>currentSingleScores.Length){
				for(int i=0;i<populatingState;i++){
					if(i<populatingState-1){
						lastSingleScores[i]=currentSingleScores[i];
					}else{
						//i=populatingState-1
						lastSingleScores[i]=0;
					}
				}
			}else{
				System.Array.Copy(currentSingleScores,lastSingleScores,populatingState);
			}

			//version2+
			getOverallScore();

			//compare with globalbest
			if(currentSumOfScores>=globalBest){
//				Debug.Log("----------------------------------------FindGlobalBest="+globalBest
//				          +"! ====iteration["+(populatingState-1)+"]="+iteration[populatingState-1]);
				globalBest=currentSumOfScores;
				recordToGlobalBest();
			}

			MetropolisHastings();

			distanceScoreDifference=currentDistanceScore-lastDistanceScore;
			sumOfScoresDifference=currentSumOfScores-lastSumOfScores;

			lastSumOfScores=currentSumOfScores;
			isStable=false;
		}



		if(Input.GetKeyDown(KeyCode.S)){
			if(Time.timeScale==1){
				Time.timeScale=0;
				isStable=false;
			}else{
				Time.timeScale=1;
			}
		}
	}//Update()
	//==========================================================================================


	//==========================================================================================
	
	void MetropolisHastings(){
		sumOfIterations++;
		writeline(sumOfIterations,currentSumOfScores);

		//Metropolis-Hastings
		double MHDelta=beta*(currentSumOfScores-lastSumOfScores);
		float lnp= Mathf.Log(Random.value);

		if(MHDelta < lnp){
//			sumOfIterations--;
			iteration[populatingState-1]--;
			Debug.Log("lnp="+lnp);
			Debug.Log("beta*(currentSumOfScores-lastSumOfScores)="+ MHDelta);
			for(int i=0;i<populatingState;i++){
				boxesT1[i].transform.position=lastPosition[i,0];
				boxesT1[i].transform.localEulerAngles=lastPosition[i,1];
			}
							
		}

//		else if(MHDelta<=0 && iteration[populatingState-1]<500 && Random.value>0.9){
////			switchAnyTwo();
////			int k=populatingState-1;
////			int idx=Mathf.FloorToInt(Random.value*k %k)+1;//won't be the biggest cube
//			jumpOne(populatingState-1);
//		}

	}

	void recordToGlobalBest(){
//		globalBestT1=new Vector3[nameList.Count,3];
		for(int i=0;i<populatingState;i++){
			globalBestT1[i,0]=boxesT1[i].collider.bounds.center;
			globalBestT1[i,1]=new Vector3(0,0,0);
			globalBestT1[i,1].y=boxesT1[i].transform.localEulerAngles.y;
			globalBestT1[i,2]=boxesT1[i].collider.bounds.extents;
		}
	}
	
	void recordLastPosition(){
		//Record position
		lastPosition=new Vector3[populatingState,3];
		Vector3[] entity=new Vector3[3];
		entity[0]=new Vector3(0,0,0);
		entity[1]=new Vector3(0,0,0);
		entity[2]=new Vector3(0,0,0);
		for(int i=0;i<populatingState;i++){
			entity[0]=boxesT1[i].transform.position;
			entity[1].y=boxesT1[i].transform.localEulerAngles.y;
			entity[2]=boxesT1[i].collider.bounds.extents;
			lastPosition[i,0]=entity[0];
			lastPosition[i,1]=entity[1];
			lastPosition[i,2]=entity[2];
		}
	}

	void simulatedAnnealingControl(){
//		iteration[populatingState-1]++;
		double iterationTimesCritaria=populatingState*300;

		if(sumOfIterations>iterationTimesCritaria){

			if(sumOfIterations>2.5*iterationTimesCritaria) {
				beta=10000;
				step=(float)(step*0.995);
			}else if(sumOfIterations>2*iterationTimesCritaria) {
				beta=1000;step=0.5f;
			}else if(sumOfIterations>iterationTimesCritaria) beta=1;
		}

//		if(iteration[populatingState-1]==iterationTimesCritaria){
//			goToGlobalBest();
////			beta=beta+2;
////			Debug.LogError("idx="+(populatingState-1));
////			Debug.LogError("should import "+nameList[populatingState-1]);
//			if(populatingState<nameList.Count){
////				importInitialFurniture(nameList[populatingState]);
//				if(isFull()){
//					Debug.LogWarning("Room is not Big Enough. End importing new furniture");
//					nameList.RemoveRange(populatingState,nameList.Count-populatingState+1);
//				}else{
//					importInitialFurniture(nameList[populatingState]);
//				}
//			}//if haven't imported all in namelist
//
//		}
//		else if(iteration[populatingState-1]>iterationTimesCritaria){
//
//			if(iteration[populatingState-1]>500) beta=1;
//			if(iteration[populatingState-1]>1000) beta=50;
//			if(iteration[populatingState-1]>1500) {
//				beta=5000;
//				step=(float)(step*0.99);
//			}
////			if(populatingState==nameList.Count)
////				beta=50;
//		}

		if(step<0.1){
			goToGlobalBest();
			isfinished=true;
		}
	}


	

	//==========================goToGlobalBest()===================
	void goToGlobalBest(){
		for(int i=0;i<populatingState;i++){
			boxesT1[i].transform.position=globalBestT1[i,0];
			boxesT1[i].transform.localEulerAngles=globalBestT1[i,1];
		}
	}

	//===========================jumpOne()======================
	void jumpOne(int idx){
		Vector3 newposition=getRandomPosition(idx);
		newposition.y=boxesT1[idx].collider.bounds.extents.y+Room.roomCenter.y;
		boxesT1[idx].transform.position=newposition;
	}

	//============================switch()=======================
	void switchAnyTwo(){
		//switch two objects randomly
		int[] RandomList=getRandomList(populatingState);
		int a=RandomList[0];
		int b=RandomList[1];

		//switch two objects position
		boxesT1[a].transform.position=lastPosition[b,0];
		boxesT1[b].transform.position=lastPosition[a,0];
	}



	//============================rotate()========================
	void rotate(GameObject furniture){

		Vector3 Pi=furniture.collider.bounds.center;
		Vector3 Ei=furniture.collider.bounds.extents;
		int wallID=InRoomRetrieval.FindWall(new Vector2(Pi.x,Pi.z));

		float walldistance=InRoomRetrieval.DistanceToRay2D(
			new Vector2(Pi.x,Pi.z),
			new Vector2(Room.walls[wallID,0].x,Room.walls[wallID,0].z),
			new Vector2(Room.walls[wallID,1].x,Room.walls[wallID,1].z));

		Vector3 from=new Vector3(0,0,1);		
		Vector3 to=Room.walls[wallID,2];
		float rotationY=Vector3.Angle(from,to);
		rotationY=rotationY;//-floorplanRotation.y;

		if(Pi.x<Room.roomCenter.x && Pi.z<Room.roomCenter.z){
			//in III phase
			if(rotationY<10){
				rotationY=0;
			}
			if(rotationY>70){rotationY=90;}
		}else if(Pi.x<Room.roomCenter.x && Pi.z>Room.roomCenter.z){
			//in II phase
			if(rotationY>70){
				rotationY=90;
			}
			if(rotationY<10){
				rotationY=180;
			}
		}else if(Pi.x>Room.roomCenter.x && Pi.z>Room.roomCenter.z){
			//in I phase
			if(rotationY<10){
				rotationY=180;
			}
			if(rotationY>70){
				rotationY=270;
			}
//			Debug.Log("//in I phase, rotationY="+rotationY);
		}else{
			//in IV phase
			if(rotationY<10){
				rotationY=0;
			}
			if(rotationY>70){
				rotationY=270;
			}
//			Debug.Log("//in IV phase, rotationY="+rotationY);
			
		}

		if(Ei.z< Ei.x || walldistance >Ei.z){
			furniture.transform.localEulerAngles=new Vector3(0,rotationY,0);
//			Debug.LogError(furniture.name+" rotates: "+rotationY);
		}
//		else{
//			Debug.LogError(furniture.name+"--------------------------------");
//			Debug.LogError(Pi+" room center"+Room.roomCenter);
//			Debug.LogError(Ei.x+"  x> z?  "+Ei.z);
//			Debug.LogError(walldistance);
//		}

	}


	//-------------------------move()------------------------------
	void move(GameObject furniture,int id){
//		lastRotationY=furniture.transform.localEulerAngles.y;
		Vector3 movingDirection;
		
//		if(isInRoom(furniture)){//make sure it hasn't been out of the room
			movingDirection=new Vector3(Random.value-0.5f,0.0f,Random.value-0.5f);
			movingDirection=movingDirection.normalized;
			furniture.rigidbody.MovePosition(furniture.transform.position+ movingDirection*step);

			
//		}else{//(if has been out of the room) 
//			moveintotheRoom(id);
//		}
		
		//		furniture.transform.position=furniture.transform.position+ movingDirection*step;
	}
	void moveintotheRoom(int id){
		Vector3 movingDirection;
//		Debug.Log("It's OUT!======================================");
		jumpOne(id);

//		jumpOne(id);

		//			//put it back to last position
		//			boxesT1[id].transform.position=lastPosition[id,0];
		//			boxesT1[id].transform.localEulerAngles=lastPosition[id,1];

//		//move towards room center
//		Vector3 D=Room.roomCenter-furniture.transform.position;
//		movingDirection=new Vector3(D.x ,0.0f, D.z);
//		movingDirection=movingDirection.normalized;
//		furniture.rigidbody.MovePosition(furniture.transform.position+ movingDirection*0.2f);

//		//moving along nearest wall normal
//		Vector3 Pi=boxesT1[id].collider.bounds.center;
//		int wallID_Pi=InRoomRetrieval.FindWall(new Vector2(Pi.x,Pi.z));
//		movingDirection=Room.walls[wallID_Pi,2];//wall normal
////		furniture.rigidbody.MovePosition(furniture.transform.position+ movingDirection*0.2f);
//		boxesT1[id].transform.position=boxesT1[id].transform.position+ movingDirection*0.2f;

	}
	//-------------------------move()------------------------------

	bool isInRoom(GameObject box){
		Vector3 center=box.collider.bounds.center;
		center.y=floorMesh.collider.bounds.center.y;

//		Vector3 boundsMIN=box.collider.bounds.min;
//		Vector3 boundsMAX=box.collider.bounds.max;
//		Vector3[] corners=new Vector3[4];
//		corners[0]=new Vector3(boundsMIN.x, y,boundsMIN.z);
//		corners[1]=new Vector3(boundsMIN.x, y,boundsMAX.z);
//		corners[2]=new Vector3(boundsMAX.x, y,boundsMAX.z);
//		corners[3]=new Vector3(boundsMAX.x, y,boundsMIN.z);
//
//		foreach(Vector3 corner in corners){
//			if(!floorMesh.collider.bounds.Contains(corner)){
//				return false;
//			}
//		}

		return floorMesh.collider.bounds.Contains(center);
	}

	int findNearestCornerID(Vector3 Pi){
		int wallID_Pi=InRoomRetrieval.FindWall(new Vector2(Pi.x,Pi.z));
		//find nearest corner
		float[] distances2D=new float[Room.floorCorners.Length];
		for(int j=0;j<Room.floorCorners.Length;j++){
			distances2D[j]=(new Vector2(Pi.x,Pi.z)-
			                new Vector2(Room.floorCorners[j].x,
			            Room.floorCorners[j].z)).magnitude;
		}
		int cornerID_Pi=InRoomRetrieval.findSmallestIDAt(distances2D);
		return cornerID_Pi;
	}
	
	//-------------------------getScore()------------------------------
	void getOverallScore(){
//		distanceFactor=10*nameList.Count/populatingState;
		getAllDistanceScore();
		currentSumOfScores=currentDistanceScore;
//		currentSumOfScores=0;
		currentSingleScores=new double[populatingState];
		for(int i=0;i<populatingState;i++){
			getSingleScore(i);
			currentSumOfScores+=currentSingleScores[i];
		}

//		Debug.Log("    currentSumOfScores="+currentSumOfScores);
	}

	void getAllDistanceScore(){
		double SumsumOfDistance0=0;
		double SumsumOfDistance1=0;// large= :)
		double SumsumOfDistance2=0;// large= :)

		for(int i=0;i<populatingState;i++){
			Vector3 Pi=boxesT1[i].collider.bounds.center;
			for(int j=i+1;j<populatingState;j++){
				Vector3 Pj=boxesT1[j].collider.bounds.center;
				float distance=(new Vector2(Pi.x,Pi.z)-new Vector2(Pj.x,Pj.z)).magnitude;
				if(!isInRoom(boxesT1[i])) distance=0;
				SumsumOfDistance0+=distance;
			}
		}


//		/**
//		 * New Distance term, 3D "defined"
//		 */
//		for(int i=0;i<populatingState;i++){
//			Vector3 Pi=boxesT1[i].collider.bounds.center;
//			Vector3 Ei=boxesT1[i].collider.bounds.extents;
//			double newDistance=((Pi-Room.roomCenter).magnitude+Ei.magnitude)/(2*i+1);
//			if(!isInRoom(boxesT1[i])) newDistance=0;
//			SumsumOfDistance1+=newDistance;
//		}
//
//
//		for(int i=0;i<populatingState;i++){
//			/**
//			 * Distance term
//			 */
//			Vector3 Pi=boxesT1[i].collider.bounds.center;
//			Vector3 Ei=boxesT1[i].collider.bounds.extents;
//			float[] distances=new float[populatingState];
//			for(int j=i+1;j<populatingState;j++){
//				Vector3 Pj=boxesT1[j].collider.bounds.extents;
//				Vector3 Ej=boxesT1[j].collider.bounds.extents;
//				distances[j]=(Pi-Pj).magnitude-Ei.magnitude-Ej.magnitude;
//				distances[j]=Mathf.Max(distances[j],0);
//			}
//			System.Array.Sort(distances);
//			SumsumOfDistance2+=distances[0];//only count the smallest
//		}//for i<populatingState

		currentDistanceScore=distanceFactor*SumsumOfDistance0;

//		Debug.Log("    SumsumOfDistance="+SumsumOfDistance);
	}//getDistanceScore()

	void getSingleScore(int i){
		/**
		 * Door, windows and fireplace term:
		 * Vector3[][]Doors windows Fireplaces: (namecode,wallID,0)(center)(extents) Vector3(width,depth,height)
		 * Vector3[,] walls: (start point)(end point)(normal to inside the room)
		 */
		/**
		 * Nearest wall and corner term
		 */
		Vector3 Pi=boxesT1[i].transform.position;
		Vector3 Ei=boxesT1[i].collider.bounds.extents;

		Vector2 A=new Vector2(Pi.x,Pi.z);
		int wallID=InRoomRetrieval.FindWall(A);
		int cornerID=findNearestCornerID(new Vector3(A.x,0,A.y));

		float walldistance=InRoomRetrieval.DistanceToRay2D(
			A,
			new Vector2(Room.walls[wallID,0].x,Room.walls[wallID,0].z),
			new Vector2(Room.walls[wallID,1].x,Room.walls[wallID,1].z))
			-Ei.z;
		walldistance=Mathf.Max(walldistance,0);
		float cornerdistance=(new Vector2(Room.floorCorners[cornerID].x,
		                                  Room.floorCorners[cornerID].z)
		                      -A).magnitude-new Vector2(Ei.x,Ei.z).magnitude;
		cornerdistance=Mathf.Max(cornerdistance,0);

		double NearestWallDistanceScore=100/(walldistance+1)/(10*i+1)*Ei.y;
		double NearestCornerDistanceScore=100/(cornerdistance+1)/(10*i+1)*Ei.y;

		/**
		 * Rotation check
		 */
		float rotationY=boxesT1[i].transform.localEulerAngles.y;
		float targetedRY=Vector3.Angle(new Vector3(0,0,1),Room.walls[wallID,2]);
		if(Mathf.Abs(rotationY-targetedRY)>45){
			NearestWallDistanceScore=NearestWallDistanceScore/2;
		}
		

		/**
		 * Window shielded score
		 */
		double WindowShieldedScore=0;
		for(int j=0;j<Windows.GetLength(0);j++){
			float cosTheta=0;
			float windowDistance=0;
			int windowWall=(int)Windows[j][0].y;
			//if the box nearest wall is the window wall
			if(wallID==windowWall){
				//the box should not be higher than the window:
				//windowcenter.y-boxcenter.y
				float heightDelta=Windows[j][1].y-Pi.y;
				//>0 and even > sum of two extents in Y
				//alow furniture that a litter higher than window lowest bounds
				if(heightDelta<Windows[j][2].y*0.3f+Ei.y){
					//cos(theta)
					Vector3 pointingtoPi=Pi-Windows[j][1];//pointing to Pi
					pointingtoPi.y=0;
					//----2D distance
					windowDistance=pointingtoPi.magnitude;
					windowDistance-=Mathf.Max(boxesT1[i].collider.bounds.extents.x,
					                          boxesT1[i].collider.bounds.extents.z);
					windowDistance=Mathf.Max(windowDistance,0);

					pointingtoPi=pointingtoPi.normalized;//and normalise it
					//the normalized wall normal * pointingtoPi
					cosTheta=Vector3.Dot(Room.walls[windowWall,2],pointingtoPi);
				}//if box is lower than the window, it doesn't matter
				else{
					WindowShieldedScore=100;
				}
			}//if the box is not near to the window, it's doesn't matter too
			else{
				WindowShieldedScore=100;
			}

			WindowShieldedScore+=windowDistance/(10*cosTheta+1);

			if(cosTheta>0.71){//if cos(theta)>1/sqrt(2)
				WindowShieldedScore=WindowShieldedScore+(windowDistance+1)/(cosTheta+1)
					-10/(windowDistance+1);
			}
		}

		/**
		 * Fireplace shielded score
		 */
		double FireplaceShieldedScore=0;
		for(int j=0;j<Fireplaces.GetLength(0);j++){
			float cosTheta=0;//when theta= 90 degree, very important
			float FireplaceDistance=0;
			int fireplaceWall=(int)Fireplaces[j][0].y;
			//if the box nearest wall is the fireplace wall
			if(wallID==fireplaceWall){
				//cos(theta)
				Vector3 pointingtoPi=Pi-Fireplaces[j][1];//pointing to Pi
				pointingtoPi.y=0;
				//----2D distance
				FireplaceDistance=pointingtoPi.magnitude;
				FireplaceDistance-=Mathf.Max(boxesT1[i].collider.bounds.extents.x,
				                             boxesT1[i].collider.bounds.extents.z);
				FireplaceDistance=Mathf.Max(FireplaceDistance,0);
				//if center 2d distance larger than fireplace depth*1.5
				if(FireplaceDistance<=Fireplaces[j][3].y*1.5f){
					pointingtoPi=pointingtoPi.normalized;//and normalise it
					//the normalized wall normal * pointingtoPi
					cosTheta=Vector3.Dot(Room.walls[fireplaceWall,2],pointingtoPi);
				}//else ignore
				else{
					FireplaceShieldedScore=100;
				}
			}//else ignore
			else{
				FireplaceShieldedScore=100;
			}
			
			FireplaceShieldedScore+=(FireplaceDistance+1)/(cosTheta+1);
			if(cosTheta>0.87){//expected narrow than window
				FireplaceShieldedScore=FireplaceShieldedScore- FireplaceDistance/(cosTheta+1)
					-10/(FireplaceDistance+1);
			}
		}

		/**
		 * Door path score
		 */
//		Debug.Log(boxesT1[i].name+" corner score was "+NearestCornerDistanceScore);
		double DoorPathScore=0;
		for(int j=0;j<Doors.GetLength(0);j++){
			float cosTheta=0;
			float DoorDistance=0;
			int doorCorner=findNearestCornerID(Doors[j][1]);
			if(cornerID==doorCorner){
				NearestCornerDistanceScore=0;
//				Debug.Log(boxesT1[i].name+" near door");
				int doorWall=(int)Doors[j][0].y;
				//cos(theta)
				Vector3 pointingtoPi=Pi-Doors[j][1];//pointing to Pi
				pointingtoPi.y=0;
				//----2D distance
				float doorDistance=pointingtoPi.magnitude;
				doorDistance-=Mathf.Max(boxesT1[i].collider.bounds.extents.x,
				                        boxesT1[i].collider.bounds.extents.z);
				doorDistance=Mathf.Max(doorDistance,0);
				pointingtoPi=pointingtoPi.normalized;//then normalise it
				//the normalized wall normal * pointingtoPi
				cosTheta=Vector3.Dot(Room.walls[doorWall,2],pointingtoPi);
			}//else it should move far away from doors
			else{
				DoorPathScore=100;
			}
//			else{
//				int former;
//				int latter;
//				switch(doorCorner){
//				case 0:
//					former=Room.floorCorners.Length-1;
//					latter=doorCorner+1;
//					break;
//				case Room.floorCorners.Length-1:
//					former=doorCorner-1;
//					latter=0;
//					break;
//				default:
//					former=doorCorner-1;
//					latter=doorCorner+1;
//					break;
//				}
//				if(cornerID==former || cornerID==latter){
//					//TODO?
//				}
//			}
			float theta=Mathf.Acos(cosTheta)*180/Mathf.PI;
			DoorPathScore+=DoorDistance*theta;

//			DoorPathScore+=(DoorDistance+1)/(cosTheta+1);
			if(cosTheta>0.8){//expected wider than window
				DoorPathScore=DoorPathScore- //DoorDistance/(cosTheta+1)
					-10/(DoorDistance+1);
			}
		}
//		Debug.Log(boxesT1[i].name+" DoorPathScore become "+DoorPathScore);


//		Debug.Log("NearestWallDistanceScore "+NearestWallDistanceScore);
//		Debug.Log("NearestCornerDistanceScore"+NearestCornerDistanceScore);
//		Debug.Log("DoorPathScore "+DoorPathScore);
//		Debug.Log("WindowShieldedScore "+WindowShieldedScore);
//		Debug.Log("FireplaceShieldedScore "+FireplaceShieldedScore);

		currentSingleScores[i]=
			NearestWallDistanceScore+NearestCornerDistanceScore
				+DoorPathScore+WindowShieldedScore+FireplaceShieldedScore;

	}//getSingleScore()
	//-------------------------getScore()------------------------------


	//-------------------------initialization()--------------------------
	void initialization(){
		getKnownFloorplanFurniture();
		determineInitialFurniture();
		/**
		 * import the furniture from name
		 */
		int i=0;
		foreach(string name in nameList){
			if(isFull()) break;
			i++;
			importInitialFurniture(name);
		}

	}
	//-------------------------initialization()--------------------------

	void determineInitialFurniture(){//is a big room of the house
		nameList=new List<string>();
		/**
		 * get all initially imported furniture names
		 */
		//add all first line furniture
		string[][] furarray=PopulatingGuide.Instance.furnitureArray;
		int NumOfItems=furarray[0].Length;
		for(int i=0;i<NumOfItems;i++){
			nameList.Add(furarray[0][i]);
			Debug.Log("Furniture tags add: "+furarray[0][i]);
		}

//		foreach(string element in furarray[0]){
//			//Debug.LogError(element);
//			nameList.Add(element);
//		}

		//add other furniture randomly
		int NumOfRows=furarray.GetLength(0);
		double rank=PopulatingGuide.Instance.areaRank;
//		Debug.LogError(rank);
		if(nameList.Count==0){
			//add all furniture from one of rest lines
			if(NumOfRows>1){
				int restrows=NumOfRows-1;
				int rowth=1+ Mathf.FloorToInt(Random.value *restrows %restrows);
				//Mathf.CeilToInt will exceed idx range
				foreach(string element in furarray[rowth]){
					Debug.Log("Furniture tags add: "+element);
					
					nameList.Add(element);
				}
			}//if numofrow>1

		}else if(rank>0.5){//is a large room of the house

			//add any one in 2nd line
			NumOfItems=furarray[1].Length;
			int idx=Mathf.FloorToInt(Random.value*NumOfItems %NumOfItems);
			nameList.Add(furarray[1][idx]);
			Debug.Log("Furniture tags add: "+furarray[1][idx]);
		
			//add all furniture from one of rest lines
			if(NumOfRows>2){
				int restrows=NumOfRows-2;
				int rowth=2+ Mathf.FloorToInt(Random.value *restrows %restrows);
				//Mathf.CeilToInt will exceed idx range
				foreach(string element in furarray[rowth]){
					nameList.Add(element);
					Debug.Log("Furniture tags add: "+element);
					
				}
			}//if numofrow>2

		}else if(rank>0.25){//is a small room of the house
			//any one of the rest
			int restrow=NumOfRows-1;
			int rowth=1+Mathf.FloorToInt(Random.value *restrow %restrow);
			NumOfItems=furarray[rowth].Length;
			int idx=Mathf.FloorToInt(Random.value*NumOfItems %NumOfItems);
			nameList.Add(furarray[rowth][idx]);
			Debug.Log("Furniture tags add: "+furarray[rowth][idx]);

		}//else: rank<0.25 add nothing more

//		/**
//		 * Find out whether there is any pairwise in the nameList
//		 */
////		Debug.Log(nameList.Count);
////		foreach(string name in nameList){
////			Debug.Log(name);
////		}
//
//		secondaryPairList=new List<string>();
//		primaryPairList=new List<string>();
//		for(int i=0;i<pairwiseTAG.GetLength(0);i++){
//			string pair1=pairwiseTAG[i][1];
//			//find any pair1 in namelist
////				Debug.Log("-----------------------------pairwisetag i="+i);
//			for(int j=0;j<nameList.Count;j++){
////				Debug.Log(pair1+" is ?"+nameList[j]);
//				
//				if(pair1.Equals(nameList[j])){
////				Debug.Log("YES!");
//					
//					string pair0=pairwiseTAG[i][0];
//					//find whether pair0 is in too
//					foreach(string name in nameList){
////				Debug.Log(nameList[j]+"'s pair0 is?"+name);
//						
//						if(name.Equals(pair0)){
////				Debug.Log("YES! And remove "+nameList[j]);
//
//							primaryPairList.Add(name);
//							secondaryPairList.Add(nameList[j]);
//							nameList.Remove(nameList[j]);
//							//not allow two pair0 to one pair1
//							break;//foreach
//						}//if find pair0
//					}//foreach in namelist
//				}//if find pair1
//
//				//break;//if break here, pairwise only one to one
//				//if not break here, pairwise can be one to many:
//				//e.g. one bed with two bedside_table
//			}//for loop: try to find pair1 in namelist
//		}//for loop: all pair1 in pairwiseTAG

//		Debug.Log(nameList.Count);
//		foreach(string name in nameList){
//			Debug.Log(name);
//		}

//		//remove pair1 in namelist
//		foreach(string[] pairwise in secondaryPairList){
//			nameList.Remove(pairwise[1]);
//			Debug.Log("========================Removed:"+pairwise[1]);
//		}

		/**
		 * Sort these initial furniture by volumn==> change to by X-Z 2D area
		 */
		Vector3[] extents=new Vector3[nameList.Count];
		double[] areas=new double[nameList.Count];
		int[] indiceT1=new int[nameList.Count];
		for(int i=0;i<nameList.Count;i++){
			//Find objects with the tag in name, and choose any one of them
			Debug.Log("Import furniture with tag="+nameList[i]);
			GameObject[] gos;
			gos = GameObject.FindGameObjectsWithTag(nameList[i]);
			int idx=Mathf.FloorToInt(Random.value* gos.Length %gos.Length);
			nameList[i]=gos[idx].name;
			Vector3[] array=GetVerticesInChildren(gos[idx]);
			extents[i]=GetFurnitureExtents(array);
			areas[i]=4*extents[i].x *extents[i].z;
			indiceT1[i]=i;
		}
		System.Array.Sort(areas,indiceT1);

		string[] names=nameList.ToArray();
		nameList.Clear();

//		foreach(int i in indiceT1) Debug.Log(i);

		//(center)(rotation)(extents)
		globalBestT1=new Vector3[indiceT1.Length,3];
		int counter=0;
		for(int i=indiceT1.Length-1;i>=0;i--){
			//I wish to make the larger furniture has positioning priority
			globalBestT1[counter,0]=new Vector3(0,0,0);
			globalBestT1[counter,1]=new Vector3(0,0,0);
			globalBestT1[counter,2]=extents[indiceT1[i]];
			counter++;
			nameList.Add(names[indiceT1[i]]);
		}


	}//determineFurniture()


	//---------------------------------------------------------------------
	bool isFull(){
		double occupiedArea=0;
		foreach(GameObject cube in boxesT1){
			occupiedArea+=cube.collider.bounds.extents.x *cube.collider.bounds.extents.z;
		}
		Vector3 floorMeshEx=floorMesh.collider.bounds.extents;
//		Debug.LogWarning("floorMesh extents="+floorMeshEx);
		double critariaArea=floorMeshEx.x*floorMeshEx.z*isFullFactor;

		Debug.Log(occupiedArea+" < "+critariaArea+" ?");

		if(occupiedArea>critariaArea){
			Debug.Log("Room is full");
			return true;
		}else{
			Debug.Log("Room is not full");
			return false;
		}
	}

	void importInitialFurniture(string name){
		if(populatingState<10){
			/**
		 	 * Make sure the decided furniture is not too big for the room
		 	 */
			Vector3 position=new Vector3(0,0,0);
			float rotationY=0;
			//1. half diagonal lines comparing
			Vector3 PiEx=globalBestT1[populatingState,2];
			Vector3 RoEx=floorMesh.collider.bounds.extents;
			//first check whether there is a wall is long enough
			float[] wallLengths=new float[Room.walls.GetLength(0)];
			int[] indices=new int[wallLengths.Length];
			for(int i=0;i<wallLengths.Length;i++){
				//wall's starting point - ending point
				wallLengths[i]=(Room.walls[i,0]-Room.walls[i,1]).magnitude;
				indices[i]=i;
			}
			System.Array.Sort(wallLengths,indices);//from shortest to largest
			if(Mathf.Max(PiEx.x,PiEx.z)>Mathf.Max(RoEx.x,RoEx.z) ||
			   Mathf.Min(PiEx.x,PiEx.z)>Mathf.Min(RoEx.x,RoEx.z) ||
			   new Vector2(PiEx.x,PiEx.z).magnitude>Room.roomDiagonalXZ ||
			   wallLengths[wallLengths.Length-1]<PiEx.x*2f){
				//last one: the longest wall is shorter than this furniture
				//it will waste too many space even if it could be imported
				Debug.LogError("Unexpected situation: Furniture "+name+"is too large for this room");
				nameList.Remove(name);
				return;
			}else if(new Vector2(PiEx.x,PiEx.z).magnitude>=Mathf.Max(RoEx.x,RoEx.z)-1){
				Debug.LogWarning(name+" is huge box");
				//extremly big furniture but importable with fixed suitable rotation
				//e.g. a kitchen oven ect. table can fill half kitchen
				//put it in center and with longest wall normal rotation
				position=new Vector3(0,PiEx.y,0)+Room.roomCenter;
				rotationY=Vector3.Angle(new Vector3(0,0,0),
				                        Room.walls[indices[indices.Length-1],2]);
				GameObject huge;
				huge=GameObject.CreatePrimitive(PrimitiveType.Cube);
				huge.transform.localScale=PiEx*2;//extentes
//				Debug.Log(huge.transform.localScale);
				huge.AddComponent<BoxCollider>();
				huge.AddComponent<Rigidbody>();
				//with right rotation
				huge.transform.localEulerAngles=new Vector3(0,rotationY,0);
				huge.transform.position=position;
//				huge.rigidbody.mass=nameList.Count+pairwiseTAG.GetLength(0)-populatingState;
				huge.rigidbody.drag=10*(nameList.Count+pairwiseTAG.GetLength(0)-populatingState);
				huge.rigidbody.angularDrag=0f;
				huge.rigidbody.constraints =RigidbodyConstraints.FreezePositionY;
				huge.rigidbody.freezeRotation=true;
				huge.rigidbody.interpolation=RigidbodyInterpolation.Extrapolate;
				huge.rigidbody.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
				//it can't be rotate by physics engine
				huge.rigidbody.constraints &=~RigidbodyConstraints.FreezeRotationY;
				huge.rigidbody.rotation=Quaternion.identity;
				string boxTag=GameObject.Find(name).tag;
				huge.tag=boxTag;
				huge.name=name;
				boxesT1.Add(huge);
				populatingState++;
				Debug.Log(name);
				
				return;
				
				//but if it's bed.. it's OK, accessible too
			}else{
				commonlyImport(name);
			}
		}//if populating state<2
		else{
			commonlyImport(name);
		}//populating state>=2 imported anyway
	}//importInitialFurniture()

	void commonlyImport(string name){
		GameObject box;
		box=GameObject.CreatePrimitive(PrimitiveType.Cube);
		box.transform.localScale=globalBestT1[populatingState,2]*2;//extents
//		Debug.LogError("populatingState"+populatingState);
//		Debug.Log(box.transform.localScale);
		
		box.AddComponent<BoxCollider>();
		box.AddComponent<Rigidbody>();
		box.transform.position=new Vector3(0,box.collider.bounds.extents.y,0)+
			getRandomPosition(populatingState);
		box.rigidbody.mass=nameList.Count+pairwiseTAG.GetLength(0)-populatingState;
		box.rigidbody.drag=10*(nameList.Count+pairwiseTAG.GetLength(0)-populatingState);;
		box.rigidbody.angularDrag=0f;
		box.rigidbody.constraints =RigidbodyConstraints.FreezePositionY;
		box.rigidbody.freezeRotation=true;
		box.rigidbody.interpolation=RigidbodyInterpolation.Extrapolate;
		box.rigidbody.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
		box.rigidbody.constraints &=~RigidbodyConstraints.FreezeRotationY;
		//		box.rigidbody.constraints &=RigidbodyConstraints.FreezeRotationX;
		//		box.rigidbody.constraints &=RigidbodyConstraints.FreezeRotationZ;
		box.rigidbody.rotation=Quaternion.identity;
		string boxTag=GameObject.Find(name).tag;
		box.tag=boxTag;
		box.name=name;
		boxesT1.Add(box);
		populatingState++;
		Debug.Log(name);
	}

	Vector3 getRandomPosition(int id){
		Vector3 randomPosition=new Vector3(0,0,0);
		Vector3 extentsDifference= Room.roomExtents- globalBestT1[id,2];//extents
//		float radius=Mathf.Sqrt(extentsDifference.x*extentsDifference.x
//		                        + extentsDifference.z *extentsDifference.z);

		bool isFound=false;
		do{
			//make sure the randomposition is in room
			randomPosition.x=Room.roomCenter.x-extentsDifference.x+ Random.value*2*extentsDifference.x;
			randomPosition.z=Room.roomCenter.z-extentsDifference.z+ Random.value*2*extentsDifference.z;
			randomPosition.y=Room.roomCenter.y;

			if(populatingState!=0){
				for(int j=0;j<populatingState;j++){
					Vector3 PiEx=globalBestT1[id,2];
					Vector3 c0=randomPosition-PiEx;
					if(boxesT1[j].collider.bounds.Contains(c0)) break;
					Vector3 c2=randomPosition+PiEx;
					if(boxesT1[j].collider.bounds.Contains(c2)) break;
					Vector3 c1=c0;
					c1.z=c2.z;
					if(boxesT1[j].collider.bounds.Contains(c1)) break;
					Vector3 c3=c2;
					c3.x=c0.x;
					if(boxesT1[j].collider.bounds.Contains(c3)) break;

					if(j==populatingState-1) isFound=true;
				}

			}else{
				//populatingstate==0
				isFound=true;
			}

		}while(!isFound);

		return randomPosition;
	}//getPosition()



	Vector3[] GetVerticesInChildren(GameObject go) {
		MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
		List<Vector3> vList = new List<Vector3>();
		foreach (MeshFilter mf in mfs) {
			vList.AddRange (mf.mesh.vertices);
		}
		return vList.ToArray ();
	}

	Vector3 GetFurnitureExtents(Vector3[] vertices){
		float xmax=-99999;
		float xmin=99999;
		float ymax=-99999;
		float ymin=99999;
		float zmax=-99999;
		float zmin=99999;
		for(int i=0;i<vertices.Length;i++){
			xmax = Mathf.Max (xmax, vertices[i].x);
			xmin = Mathf.Min (xmin, vertices[i].x);
			ymax = Mathf.Max (ymax, vertices[i].y);
			ymin = Mathf.Min (ymin, vertices[i].y);
			zmax = Mathf.Max (zmax, vertices[i].z);
			zmin = Mathf.Min (zmin, vertices[i].z);
		}//for
		Vector3 extents=new Vector3(xmax-xmin,ymax-ymin,zmax-zmin);
		extents=extents*0.5f;
		return extents;
	}

//	/**
//	 * return (xmin,ymin,zmin) and (xmax,ymax,zmax)
//	 */
//	Vector3[] GetBoundingBoxCorners(Vector3[] vertices){
//		float xmax=-99999;
//		float xmin=99999;
//		float ymax=-99999;
//		float ymin=99999;
//		float zmax=-99999;
//		float zmin=99999;
//		for(int i=0;i<vertices.Length;i++){
//			xmax = Mathf.Max (xmax, vertices[i].x);
//			xmin = Mathf.Min (xmin, vertices[i].x);
//			ymax = Mathf.Max (ymax, vertices[i].y);
//			ymin = Mathf.Min (ymin, vertices[i].y);
//			zmax = Mathf.Max (zmax, vertices[i].z);
//			zmin = Mathf.Min (zmin, vertices[i].z);
//		}//for
//		Vector3[] extents=new Vector3[2];
//		extents[0]=new Vector3(xmin,ymin,zmin);
//		extents[1]=new Vector3(xmax,ymax,zmax);
//		return extents;
//	}

	void getKnownFloorplanFurniture(){
		/**
		 * Room preprocess:
		 * 1. Add DoorBlock;
		 * 2. Doors Fireplaces[][](Doors and fireplaces should not be hidden);
		 * 3. Windows[][](Window shouldn't be hidden by "high object" 
		 * 	whose heightest point is higher than the window center);
		 */
		List<Vector3[]> list1=new List<Vector3[]>();
		List<Vector3[]> list2=new List<Vector3[]>();
		List<Vector3[]> list3=new List<Vector3[]>();
		
		//floorplanFurniture[][]:(namecode,wallID,0.0)(center)(extents) Vector3(width,depth,height)
		for(int i=0;i<InRoomRetrieval.Instance.floorplanFurniture.GetLength(0);i++){
			switch((int)InRoomRetrieval.Instance.floorplanFurniture[i][0].x){
			case 1://is door
				Instantiate(GameObject.Find("DoorBlock"),
				            InRoomRetrieval.Instance.floorplanFurniture[i][1],Quaternion.identity);
				list1.Add(InRoomRetrieval.Instance.floorplanFurniture[i]);
				break;
			case 2://is window
				list2.Add(InRoomRetrieval.Instance.floorplanFurniture[i]);
				break;
			case 3://is fireplace
				GameObject fireplaceBlock= GameObject.CreatePrimitive(PrimitiveType.Cube);
				fireplaceBlock.renderer.enabled=false;
				fireplaceBlock.transform.localScale=InRoomRetrieval.Instance.floorplanFurniture[i][2]*2f;				
				fireplaceBlock.AddComponent<BoxCollider>();
				int wallID=(int) InRoomRetrieval.Instance.floorplanFurniture[i][0].y;
				float depth=InRoomRetrieval.Instance.floorplanFurniture[i][3].y*0.5f;//fireplace depth
				//position is the fireplace center move towards wall normal with fireplace depth
				fireplaceBlock.transform.position=InRoomRetrieval.Instance.floorplanFurniture[i][1]+
					Room.walls[wallID,2]*depth;

				fireplaceBlock.AddComponent("CollisionHandler");
				
				list3.Add(InRoomRetrieval.Instance.floorplanFurniture[i]);
				break;
			default://=0 uninitialised; it should be floorstairs
				break;
			}//switch
		}//for floorplanFurniture
		Doors=list1.ToArray();
		Windows=list2.ToArray();
		Fireplaces=list3.ToArray();
		list1.Clear();
		list2.Clear();
		list3.Clear();
	}//getKnownFloorplanFurniture()

	//==========================================================================================
	int[] getRandomList(int k){
		List<int> list=new List<int>();
		for(int i=0;i<k;i++){
			list.Add(i);
		}
		int[] RandomList= new int[k];
		for (int j=0;j<k;j++){
//			Debug.Log("list.Count="+list.Count);
			int idx=(int) (Random.value * list.Count);
//			Debug.Log("Remove: list["+idx+"]="+list[idx]);
			RandomList[j]=list[idx];
//			Debug.Log("RandomList["+j+"]="+idx);
			list.RemoveAt(idx);
		}
		return RandomList;
	}//getRandomList()

	//data output for testing numerical result in Matlab
	void writeline(int x, double y){
		file.WriteLine(x.ToString() + " " + y.ToString());
	}


}//Class
