//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;//for List<T>, Queue<T>
//
//public class populating : MonoBehaviour {
//	bool isInitialised=false;//for run time furnishing, tracing the player position
//	public bool isTriggered;
//	bool isRunning=false;
//	bool isfinished=false;
//	
//	public string roomType="bedroom";
//	Vector3[][]Doors;//(namecode,wallID,0)(center)(extents) Vector3(width,depth,height)
//	Vector3[][]Fireplaces;//(namecode,wallID,0)(center)(extents) Vector3(width,depth,height)
//	Vector3[][]Windows;//(namecode,wallID,0)(center)(extents) Vector3(width,depth,height)
//	
//	
//	private GameObject[] BigPieces;
//	int populatingState=0;//max=num Of Populated furniture
//	int[] indiceT1;//in ascending order of furniture volumns (smallest to largest)
//	
//	public double globalBest=0;
//	public double currentScore;
//	public double lastScore;
//	public float step=1.2f;
//	public int iteration=0;
//	int period=150;
//	Vector3[,] lastPosition; //(position)(rotation)
//	float distanceFactor=20;
//	double beta=1;
//	float lastRotationY;
//	
//	
//	// Use this for initialization
//	void Start () {
//		Random.seed=Room.roomID;
//	}
//	
//	Vector3[] GetVerticesInChildren(GameObject go) {
//		MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
//		List<Vector3> vList = new List<Vector3>();
//		foreach (MeshFilter mf in mfs) {
//			vList.AddRange (mf.mesh.vertices);
//		}
//		return vList.ToArray ();
//	}
//	
//	Vector3 GetFurnitureExtents(Vector3[] vertices){
//		float xmax=-99999;
//		float xmin=99999;
//		float ymax=-99999;
//		float ymin=99999;
//		float zmax=-99999;
//		float zmin=99999;
//		//[0]xmin,zmin	[1]xmin,zmax
//		//[2]xmax,zmin	[3]xmax,zmax
//		//		Vector3[] boundsVertices=new Vector3[2];
//		for(int i=0;i<vertices.Length;i++){
//			xmax = Mathf.Max (xmax, vertices[i].x);
//			xmin = Mathf.Min (xmin, vertices[i].x);
//			ymax = Mathf.Max (ymax, vertices[i].y);
//			ymin = Mathf.Min (ymin, vertices[i].y);
//			zmax = Mathf.Max (zmax, vertices[i].z);
//			zmin = Mathf.Min (zmin, vertices[i].z);
//		}//for
//		//		boundsVertices[0].x = xmin;
//		//		boundsVertices[1].x = xmax;
//		//		boundsVertices[0].y = ymin;
//		//		boundsVertices[1].y = ymax;
//		//		boundsVertices[0].z = zmin;
//		//		boundsVertices[1].z = zmax;
//		Vector3 extents=new Vector3(xmax-xmin,ymax-ymin,zmax-zmin);
//		extents=extents*0.5f;
//		return extents;
//	}
//	
//	Vector3 getPosition(int id){
//		//check InRoomRetrival
//		Vector3 randomPosition;
//		Vector3 extentsDifference= Room.roomExtents- InRoomRetrieval.Instance.T1Furniture[id,1];
//		float radius=Mathf.Sqrt(extentsDifference.x*extentsDifference.x
//		                        + extentsDifference.z *extentsDifference.z);
//		
//		bool isSearching=true;
//		do{
//			//make sure the randomposition is in room
//			randomPosition.x=Room.roomCenter.x-extentsDifference.x+ Random.value*2*extentsDifference.x;
//			randomPosition.z=Room.roomCenter.z-extentsDifference.z+ Random.value*2*extentsDifference.z;
//			randomPosition.y=Room.roomCenter.y;
//			//I have reposition the gameobject "center" at a little bit lower than its bottom center
//			
//			if(populatingState!=0){
//				//check radius and distance
//				for(int i=0;i<populatingState;i++){
//					Vector3 Pi=BigPieces[i].transform.position;
//					if((Pi-randomPosition).magnitude<radius) continue;
//				}//for
//				break;//won't overlap with imported furniture, so won't push them away
//				
//			}else{//populatingState==0
//				break;
//			}
//		}while(isSearching);
//		
//		return randomPosition;
//	}//getPosition()
//	
//	void importFurniture(string furnitureName){
////		//		BigPieces[populatingState]=(GameObject)Instantiate(
////		//			GameObject.Find(furnitureName),getPosition(populatingState),Quaternion.identity);
////		
////		BigPieces[populatingState]=GameObject.CreatePrimitive(PrimitiveType.Cube);
////		BigPieces[populatingState].transform.localScale=InRoomRetrieval.Instance.T1Furniture[populatingState,2]*2;
////		
////		BigPieces[populatingState].AddComponent<BoxCollider>();
////		float y=BigPieces[populatingState].collider.bounds.extents.y;
////		BigPieces[populatingState].AddComponent<Rigidbody>();
////		
////		BigPieces[populatingState].transform.position=new Vector3(0,y,0)+getPosition(populatingState)
////			
////		//		BigPieces[populatingState].rigidbody.mass=500;
////		BigPieces[populatingState].rigidbody.drag=500;
////		BigPieces[populatingState].rigidbody.angularDrag=0;
////		//		BigPieces[populatingState].rigidbody.freezeRotation=true;//bad effect on actual model collision :(
////		BigPieces[populatingState].rigidbody.constraints=RigidbodyConstraints.FreezePositionY;
////		BigPieces[populatingState].rigidbody.rotation= Quaternion.identity;
////		BigPieces[populatingState].name=furnitureName;
////		BigPieces[populatingState].layer=9;
////		populatingState++;
//	}
//	
//	//-------------------------------------initialise()----------------------------------------------
//	void initialise(){
//		/**
//		 * Room preprocess:
//		 * 1. Add DoorBlock;
//		 * 2. Doors Fireplaces[][](Doors and fireplaces should not be hidden);
//		 * 3. Windows[][](Window shouldn't be hidden by "high object" 
//		 * 	whose heightest point is higher than the window center);
//		 */
//		List<Vector3[]> list1=new List<Vector3[]>();
//		List<Vector3[]> list2=new List<Vector3[]>();
//		List<Vector3[]> list3=new List<Vector3[]>();
//		
//		//floorplanFurniture[][]:(namecode,wallID,0.0)(center)(extents) Vector3(width,depth,height)
//		for(int i=0;i<InRoomRetrieval.Instance.floorplanFurniture.GetLength(0);i++){
//			switch((int)InRoomRetrieval.Instance.floorplanFurniture[i][0].x){
//			case 1://is door
//				Instantiate(GameObject.Find("DoorBlock"),
//				            InRoomRetrieval.Instance.floorplanFurniture[i][1],Quaternion.identity);
//				list1.Add(InRoomRetrieval.Instance.floorplanFurniture[i]);
//				break;
//			case 2://is window
//				list2.Add(InRoomRetrieval.Instance.floorplanFurniture[i]);
//				break;
//			case 3://is fireplace
//				GameObject fireplaceBlock= GameObject.CreatePrimitive(PrimitiveType.Cube);
//				fireplaceBlock.renderer.enabled=false;
//				fireplaceBlock.transform.localScale=InRoomRetrieval.Instance.floorplanFurniture[i][2]*2f;				
//				fireplaceBlock.AddComponent<BoxCollider>();
//				int wallID=(int) InRoomRetrieval.Instance.floorplanFurniture[i][0].y;
//				float depth=InRoomRetrieval.Instance.floorplanFurniture[i][3].y;//fireplace depth
//				//position is the fireplace center move towards wall normal with fireplace depth
//				fireplaceBlock.transform.position=InRoomRetrieval.Instance.floorplanFurniture[i][1]+
//					Room.walls[wallID,2]*depth;
//				
//				list3.Add(InRoomRetrieval.Instance.floorplanFurniture[i]);
//				break;
//			default://=0 uninitialised; it should be floorstairs
//				break;
//			}//switch
//		}//for floorplanFurniture
//		Doors=list1.ToArray();
//		Windows=list2.ToArray();
//		Fireplaces=list3.ToArray();
//		list1.Clear();
//		list2.Clear();
//		list3.Clear();
//		
//		
//		/**
//		 * Load furniture
//		 */
//		if(roomType.Equals("bedroom")){
//			if(Room.roomDiagonalXZ>PopulatingGuide.Instance.smallestXZ){
//				BigPieces=new GameObject[PopulatingGuide.Instance.bedroomT1_old.Length];
//				//(center)(extents)(rotation)
//				InRoomRetrieval.Instance.T1Furniture= new Vector3[BigPieces.Length,3];
//				Vector3[] extents=new Vector3[BigPieces.Length];
//				double[] volumns=new double[BigPieces.Length];
//				indiceT1=new int[BigPieces.Length];
//				for(int i=0;i<BigPieces.Length;i++){
//					Vector3[] array=GetVerticesInChildren(GameObject.Find(PopulatingGuide.Instance.bedroomT1_old[i]));
//					extents[i]=GetFurnitureExtents(array);
//					volumns[i]=8*extents[i].x *extents[i].y *extents[i].z;
//					indiceT1[i]=i;
//				}
//				System.Array.Sort(volumns,indiceT1);
//				//				foreach (int volumn in volumns) Debug.Log(volumn+" ");
//				//				foreach (int index in indice) Debug.Log(index+" ");
//				
//				int counter=0;
//				for(int i=BigPieces.Length-1;i>=0;i--){
//					//I wish to make the larger furniture has positioning priority
//					InRoomRetrieval.Instance.T1Furniture[counter,2]=extents[indiceT1[i]];
//					//					Debug.Log("---------------------------furniture extents:"+extents[indiceT1[i]]);
//					counter++;
//				}
//				
//				//populating first two (in volumn) BigPiece at first
//				for(int i=0;i<2;i++){
//					int GuideID=indiceT1[BigPieces.Length-1-populatingState];
//					importFurniture(PopulatingGuide.Instance.bedroomT1_old[GuideID]);
//				}
//				
//			}//if Room.roomDiagonalXZ
//		}//if roomType
//		
//		lastPosition=new Vector3[BigPieces.Length,2];
//		
//		getScore();
//		
//	}//initialise()
//	
//	//	Vector2 getNearestCorner(Vector3 center){
//	//		Vector2 A=new Vector2(center.x,center.z);
//	//		for(int i=0;i<Room.floorCorners.Length;i++){
//	//			Vector2 B=new
//	//		}
//	//	}
//	
//	//-----------------------------------------Score---------------------------------------------
//	void getScore(){
//		double SumsumOfDistance=0;// :)
//		double RoomFurnitureBlock=0;// :(
//		double distanceToCorner=0;// :(
//		for(int i=0;i<populatingState;i++){
//			/**
//			 * Distance term
//			 */
//			Vector3 Pi=BigPieces[i].transform.position;
//			for(int j=i+1;j<populatingState;j++){
//				Vector3 Pj=BigPieces[j].transform.position;
//				SumsumOfDistance=SumsumOfDistance+(Pi-Pj).magnitude;
//			}
//			/**
//			 * Distance to nearest corner
//			 */
//			
//			/**
//			 * Door, windows and fireplace term:
//			 * Vector3[][]Doors windows Fireplaces: (namecode,wallID,0)(center)(extents) Vector3(width,depth,height)
//			 * Vector3[,] walls: (start point)(end point)(normal to inside the room)
//			 */
//			//Door has been added a block, it shouldn't be a problem
//			
//			//bigger object should be far away from door
//			for(int j=0;j<Doors.GetLength(0);j++){
//				float distance=InRoomRetrieval.Find2DDistance(Pi,Doors[j][1]);
//				RoomFurnitureBlock=RoomFurnitureBlock+ distance* BigPieces[i].collider.bounds.extents.magnitude;
//			}
//			for(int j=0;j<Windows.GetLength(0);j++){
//				//if the object is too high, too close
//				Vector3 distance;
//				float dotproduct=0;
//				int wallID;
//				wallID=(int)Windows[j][0].y;
//				float maxHeight=Windows[j][1].y;//- Windows[j][3].z *0.2f;
//				float furnitureHeight=BigPieces[i].collider.bounds.extents.y+BigPieces[i].collider.bounds.center.y;
//				if(furnitureHeight>maxHeight){
//					distance=Pi-Windows[j][1];//pointing to Pi
//					distance.y=0;
//					dotproduct=Vector3.Dot(Room.walls[wallID,2],distance);
//				}
//				RoomFurnitureBlock=RoomFurnitureBlock+dotproduct;
//			}//for windows
//			
//			/**
//			 * Along walls
//			 */
//			//find nearest wall
//			//			int wallID=InRoomRetrieval.FindWall(new Vector2(Pi.x,Pi.z));
//			
//			
//		}//for i<populatingState
//		
//		currentScore= SumsumOfDistance*distanceFactor -RoomFurnitureBlock;
//		
//		//currentScore=DistanceSum()+ AlongWallsScore()+AccessibleScore()+PairwiseScore();
//	}
//	
//	//--------------------------------------------------------------------------------------
//	
//	bool isInRoom(GameObject furniture, int id){
//		//furniture center and room center in 2D
//		Vector2 A=new Vector2(furniture.transform.position.x,furniture.transform.position.z);
//		Vector2 R=new Vector2(Room.roomCenter.x,Room.roomCenter.z);
//		//room extents in 2D
//		Vector2 E=new Vector2(Room.roomExtents.x,Room.roomExtents.z);
//		Vector2 D=A-R;
//		if(Mathf.Abs(D.x)<E.x || Mathf.Abs(D.y)<E.y){
//			return true;
//		}
//		return false;
//	}
//	
//	
//	//-------------------------------------------Moving-------------------------------------------
//	void move(GameObject furniture,int id){
//		lastRotationY=furniture.transform.eulerAngles.y;
//		Vector3 movingDirection;
//		
//		if(isInRoom(furniture,id)){//make sure it hasn't been out of the room
//			Vector2 A=new Vector2(furniture.transform.position.x,furniture.transform.position.z);
//			//!!!rotation
//			int wallID=InRoomRetrieval.FindWall(A);
//			float distance=InRoomRetrieval.DistanceToRay2D(
//				A,
//				new Vector2(Room.walls[wallID,0].x,Room.walls[wallID,0].z),
//				new Vector2(Room.walls[wallID,1].x,Room.walls[wallID,1].z));
//			float furnitureX=furniture.collider.bounds.extents.x;
//			float furnitureZ=furniture.collider.bounds.extents.z;
//			float furnitureXZ=new Vector2(furnitureX,furnitureZ).magnitude;
//			
//			//distance>furnitureZ condition prevent: e.g. bed is always rotating when towards corners
//			if(distance<=furnitureXZ){
//				Vector3 from=furniture.transform.eulerAngles;
//				Vector3 to=Room.walls[wallID,2];
//				float rotationY=Vector3.Angle(from,to);//+(Random.value-0.5f)*5;
//				
//				if(distance<=furnitureZ && distance>=furnitureX){
//					rotationY=lastRotationY;
//				}
//				furniture.transform.eulerAngles=new Vector3(0,rotationY,0);//from now to the nearest wall normal vector
//			}
//			
//			movingDirection=new Vector3(Random.value-0.5f,0.0f,Random.value-0.5f);
//			movingDirection=movingDirection.normalized;
//			furniture.rigidbody.MovePosition(furniture.transform.position+ movingDirection*step);
//			
//			
//		}else{//(if has been out of the room) moving towards room center
//			//put back to global best position
//			furniture.transform.position=InRoomRetrieval.Instance.T1Furniture[id,0];
//			furniture.transform.eulerAngles=InRoomRetrieval.Instance.T1Furniture[id,1];
//			//			movingDirection=new Vector3(D.x ,0.0f, D.y);
//			//			movingDirection=movingDirection.normalized;
//		}
//		
//		//		furniture.transform.position=furniture.transform.position+ movingDirection*step;
//	}
//	//-------------------------------------------Moving end-------------------------------------------
//	
//	
//	int[] getRandomList(){
//		List<int> list=new List<int>();
//		for(int i=0;i<populatingState;i++){
//			list.Add(i);
//		}
//		int[] RandomList= new int[populatingState];
//		for (int j=0;j<populatingState;j++){
//			//			Debug.Log("list.Count="+list.Count);
//			int idx=(int) (Random.value * list.Count);
//			//			Debug.Log("Remove: list["+idx+"]="+list[idx]);
//			RandomList[j]=list[idx];
//			//			Debug.Log("RandomList["+j+"]="+idx);
//			list.RemoveAt(idx);
//		}
//		return RandomList;
//	}
//	
//	//	void switchAnyTwo(){
//	//		//switch two objects randomly
//	//		int[] RandomList=getRandomList();
//	//		int a=RandomList[0];
//	//		int b=RandomList[1];
//	//		Vector3 aPosition=BigPieces[a].collider.bounds.center;
//	//		//check whether these two objects are still in room
//	//
//	//
//	//		BigPieces[a].transform.position=BigPieces[b].collider.bounds.center;
//	//		BigPieces[b].transform.position=aPosition;
//	//	}
//	
//	// Update is called once per frame
//	void Update () {
//		if(Input.GetKeyDown(KeyCode.S)){
//			isRunning=!isRunning;
//			//			for(int i=0;i<populatingState;i++){
//			//				BigPieces[i].rigidbody.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
//			//				BigPieces[i].rigidbody.interpolation=RigidbodyInterpolation.Extrapolate;
//			//			}//for rigidbody
//		}//if input s
//		
//		if( Input.GetKeyDown(KeyCode.P) || isfinished ){
//			isRunning=false;
//			RollBackToGlobalBest();
//			
//		}
//		
//		if(Input.GetKeyDown(KeyCode.E)){
//			isRunning=false;
//			//			GameObject.Destroy(BigPieces);
//			//			for(int i=0;i<populatingState;i++){
//			//				int GuideID=indiceT1[BigPieces.Length-1-populatingState];
//			//				BigPieces[i]=(GameObject)Instantiate(GameObject.Find(PopulatingGuide.Instance.bedroomT1_old[GuideID]),
//			//				                                     InRoomRetrieval.Instance.T1Furniture[i,0],
//			//				                                     InRoomRetrieval.Instance.T1Furniture[i,1]);
//			//
//			//			}
//		}
//		
//		if(isRunning && isInitialised && populatingState<=BigPieces.Length){
//			iteration++;
//			if(iteration>period && populatingState<BigPieces.Length){//import next object
//				int GuideID=indiceT1[BigPieces.Length-1-populatingState];
//				importFurniture(PopulatingGuide.Instance.bedroomT1_old[GuideID]);
//				iteration=0;
//				step=1f;
//			}
//			
//			if(populatingState>=BigPieces.Length && iteration>populatingState*period){
//				beta++;
//				step=(float)(step*0.9998);
//			}
//			
//			//furnishing BigPieces
//			lastScore=currentScore;
//			
//			//record the lastPosition and move
//			for(int i=0;i<populatingState;i++){
//				lastPosition[i,0]=BigPieces[i].transform.position;
//				lastPosition[i,1]=new Vector3(0,BigPieces[i].transform.eulerAngles.y ,0);
//				
//				move(BigPieces[i],i);
//			}//for lastPosition
//			
//			getScore();
//			
//			//compare to the global best record
//			if(globalBest<currentScore){
//				bool allInRoom=false;
//				for(int i=0;i<populatingState;i++){
//					if(!isInRoom(BigPieces[i],i)){
//						allInRoom=false;
//						break;
//					}//else continue
//				}//for all object is in room
//				
//				if(allInRoom){
//					globalBest=currentScore;
//					for(int i=0;i<populatingState;i++){
//						InRoomRetrieval.Instance.T1Furniture[i,0]=BigPieces[i].transform.position;
//						InRoomRetrieval.Instance.T1Furniture[i,1]=new Vector3(0,BigPieces[i].transform.eulerAngles.y,0);
//						InRoomRetrieval.Instance.T1Furniture[i,2]=BigPieces[i].collider.bounds.extents;
//					}
//				}//if all in room
//				
//			}else{
//				//Metropolis-Hasting
//				float lnp= Mathf.Log(Random.value);
//				Debug.Log("lnp="+lnp);
//				double deltaScore=beta*(currentScore-lastScore);
//				Debug.Log("currentScore-lastScore="+ deltaScore);
//				if(lnp>= deltaScore){
//					//reset to lastPosition or global best
//					if(iteration>200 && Random.value>0.99){
//						//						RollBackToGlobalBest();
//					}else{
//						for(int i=0;i<populatingState;i++){
//							BigPieces[i].transform.position=lastPosition[i,0];
//							BigPieces[i].transform.eulerAngles=lastPosition[i,1];
//						}
//					}
//					iteration--;
//					Debug.Log("*************************************return******************************************");
//					
//				}//if lnp
//			}//if best... else MH
//			
//		}//if furnishing BigPieces
//		
//		if(!isInitialised && InRoomRetrieval.Instance.isfinished && isTriggered){
//			initialise();
//			//-----------------Populating Initialisation End---------------
//			isInitialised=true;
//		}//if InRoomRetrieval.Instance.isfinished
//		
//	}//Update()
//	
//	void RollBackToGlobalBest(){
//		for(int i=0;i<populatingState;i++){
//			BigPieces[i].transform.position=InRoomRetrieval.Instance.T1Furniture[i,0];
//			Vector3 v=InRoomRetrieval.Instance.T1Furniture[i,1];
//			//Unity forces to write a represetative variable like this... >P
//			BigPieces[i].transform.eulerAngles=v;
//		}
//	}
//	
//}//Class
