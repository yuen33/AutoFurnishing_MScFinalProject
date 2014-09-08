using UnityEngine;
using System.Collections;
using System.Text;
//using System.StringSplitOptions;
using System.IO;
using System.Collections.Generic;//for List<T>, Queue<T>

public class InRoomRetrieval : MonoBehaviour {
	private static InRoomRetrieval instance = null;
	// to create a singleton which holds the variables
	public static InRoomRetrieval Instance{
		get{
			if(instance==null){
				instance=new GameObject("InRoomRetrieval").AddComponent<InRoomRetrieval>();
			}
			return instance;
		}
	}

	public bool isfinished;

	//known
	//(namecode,wallID,0f) (center) (extents) Vector3(width,depth,height)
	public Vector3[][] floorplanFurniture;

	//unknown
	public Vector3[,] T1Furniture;//(center)(rotation)(extent) 
	public Vector3[,] T2Furniture;

	
	protected FileInfo theSourceFile = null;
	protected StreamReader reader = null;
	protected string text = " "; // assigned to allow first line to be read below

	// Use this for initialization
	void Start () {
		isfinished=false;
	}//Start()

	/**
	 * Find x-z 2D point to point distance from Vector3 points
	 */
	public static float Find2DDistance(Vector3 A, Vector3 B){
		float distance= (new Vector2(A.x,A.z)-new Vector2(B.x,B.z)).magnitude;
//		Debug.Log("distance="+distance);
		return distance;
	}

	/**
	 * To get which walls the floorplan furniture is on
	 * it will return the wall id in Room.cs
	 */
	public static int FindWall(Vector2 p){
		int NumOfWalls=Room.floorCorners.Length;
		float[] distance=new float[NumOfWalls];
		for(int i=0;i<NumOfWalls;i++){
			Vector2 B=new Vector2(Room.walls[i,0].x,Room.walls[i,0].z);
			Vector2 C=new Vector2(Room.walls[i,1].x,Room.walls[i,1].z);
			distance[i]=DistanceToRay2D(p,B,C);
//			Debug.Log("FindWall distance[i]="+distance[i]);
		}
		return findSmallestIDAt(distance);
	}

	/**
	 * get the distance from Point A to the Line of BC
	 */
	public static float DistanceToRay2D(Vector2 A, Vector2 B, Vector2 C){
		Ray ray=new Ray(B,C-B);
		float distance=Vector3.Cross(ray.direction,A-B).magnitude;
//		distance=distance/(B-C).magnitude;
//		Debug.Log("distance="+distance);
		return distance;
	}

	/**
	 * get the smallest number id of an array
	 */
	public static int findSmallestIDAt(float[] array){
		float smallest=9999f;
		int ID=-1;
		for(int i=0;i<array.Length;i++){
			if(array[i]<smallest){
				smallest=array[i];
				ID=i;
			} 
		}
//		Debug.Log("smallest distance============================="+smallest);
		return ID;
	}//int findsmallestIDat
	
	// Update is called once per frame
	void Update () {
		if(Room.isfinished && !isfinished){
			
			float floorElevatedHeight=(Room.roomCenter.y-Room.shiftedVector.y)*10f;
			List<Vector3[]> list=new List<Vector3[]>();
			/**
		 * Read .txt file:
		 * to find the this floor furniture whose
		 * 2D distance to the room center is equal/smaller than related roomextents
		 */
			theSourceFile = new FileInfo ("Assets/Autofurnishing/scripts/floorplanFurniture.txt");
			reader = theSourceFile.OpenText();
			
			Vector3 center;
			Vector3 extents;
			text=reader.ReadLine();
			//		Debug.Log(text);
			do{
				//each floorplanFurniture block:
				//<elevation> "-------------"
				//Furniture: <furniture name>
				//Vector3 <center>
				//Vector3 <width, depth, height>
				string[] elevation_str=text.Split(' ');
				//			Debug.Log(elevation_str[1]);
				if(elevation_str[1].StartsWith("-")){
					
					float elevation=float.Parse(elevation_str[0]);
					//				Debug.Log("elevation="+elevation);
					//				Debug.Log("floorElevatedHeight="+floorElevatedHeight);
					if(elevation<=floorElevatedHeight+4 && elevation>=floorElevatedHeight-4 ){
						//belong to this floor
						text=reader.ReadLine();//name----------------
						//					Debug.Log(text);
						string[] word=text.Split(' ');
						int lastOne=word.Length-1;
						int namecode=0;
//						Debug.Log("-------------------"+word[lastOne]);
						if(word[lastOne].Equals("door")){
							namecode=1;
						}else if(word[lastOne].Equals("window")){
							namecode=2;
						}else if(word[lastOne].Equals("Fireplace")){
							namecode=3;
						}
						
						text=reader.ReadLine();//center-----------------
						string[] center_str=text.Split(' ');
						center.x=float.Parse(center_str[0]);
						center.y=float.Parse(center_str[1]);
						center.z=float.Parse(center_str[2]);
						
						center=center*0.1f+Room.shiftedVector;//to unity coord.
						
						//find whether in the room
						//					Debug.Log("roomDiagonalXZ="+Room.roomDiagonalXZ);
						if(Find2DDistance(Room.roomCenter,center)<=Room.roomDiagonalXZ+2){
							//if it is in this room
							
							//then find which wall it belongs to:
							int wallID=FindWall(new Vector2(center.x,center.z));
//							Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! wallID="+wallID);
							
						/**
						 * After the wallID is determined, 
						 * it needs to check whether the furniture is in room
						 * (e.g. the case TWO DOORS like:
						 * 	 |------|
						 * 	 |		|
						 * _\|room	|		_\: another room's opened door
						 * 	 |		|
						 * 	 |/		|		|/: this room's opened door
						 * 	 -------
						 *  
						 *  ^z
						 *  |-->x
						 * )
						 */
							Vector2 x_axis= new Vector2(1,0);
							Vector3 wallVector= Room.walls[wallID,0]-Room.walls[wallID,1];
							Vector2 onthewall=new Vector2(wallVector.x,wallVector.y);
							
							float angle=Vector2.Angle(x_axis,onthewall);//in degree 0 to 360(=0)
							float max_dist;
							float relatedAxis_dist;
							if(angle<45 || (angle>=180 && angle<225)){
								//the wall is along x_axis
								max_dist=Room.roomExtents.z;
								relatedAxis_dist= Mathf.Abs(Room.roomCenter.z-center.z);
							}else{
								//the wall is along z_axis
								max_dist=Room.roomExtents.x;
								relatedAxis_dist= Mathf.Abs(Room.roomCenter.x-center.x);
							}
							if(relatedAxis_dist>max_dist+0.4){
								continue;
							}
							
							
							text=reader.ReadLine();//width, depth, height
							string[] size=text.Split(' ');
							float width=float.Parse(size[0]);
							float depth=float.Parse(size[1]);
							float height=float.Parse(size[2]);
							Vector3 localSize=new Vector3(width,depth,height)*0.1f;//to Unity coord. unit
							if(angle<45 || (angle>=180 && angle<225)){
								//is on the wall along x_axis
								extents=new Vector3(width,height,depth)/2f;
							}else{
								//is on the wall along z_axis
								extents=new Vector3(depth, height,width)/2f;
							}
							extents=extents*0.1f;//to Unity coord.
							
							Vector3[] listline=new Vector3[4];
							listline[0]=new Vector3((float)namecode,(float)wallID,0f);
							listline[1]=center;
							listline[2]=extents;
							listline[3]=localSize;
							list.Add(listline);
							
						}//if it is in the room
						
						
						
					}//if elevation: if the furniture is on this room floor
				}//if elevation_str: if it is the new block first line
				
				text=reader.ReadLine();
			}while(text != null);
			reader.Close();
			
			floorplanFurniture=list.ToArray();
//			for(int i=0;i<list.Count;i++){
//				Debug.Log(floorplanFurniture[i][0]+" "+floorplanFurniture[i][1]+" "+floorplanFurniture[i][2]);
//			}//for list count
			isfinished=true;
		}//if Room.isFinished
	}//Update()

}//Class

