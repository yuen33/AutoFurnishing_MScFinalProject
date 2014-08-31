using UnityEngine;
using System.Collections;
using System.Text;
//using System.StringSplitOptions;
using System.IO;
using System.Collections.Generic;//for List<T>, Queue<T>

public class Room : MonoBehaviour {
//	private static Room instance = null;
//	// to create a singleton which holds the variables
//	public static Room Instance{
//		get{
//			if(instance==null){
//				instance=new GameObject("Room").AddComponent<Room>();
//			}
//			return instance;
//		}
//	}
	public static bool enabled=false;
	public GameObject furnishingRoom;

	public static bool isfinished=false;
	//input
	string floorplanName="opaqueFloorPlan";
	//outputs
	public static int roomID;
	public static Vector3 roomCenter;
	public static float roomArea;
	public static Vector3 shiftedVector;
	public static Vector3 roomExtents;
	public static float roomDiagonalXZ;
	public static Vector3[] floorCorners;
	public static Vector3[,] walls;//(start point) (end point) (normal direction)

	protected FileInfo theSourceFile = null;
	protected StreamReader reader = null;
	protected string text = " "; // assigned to allow first line to be read below



//	// Use this for initialization
//	void Start () {
//
//	}//Start()

	float[] FindNearestPoint(Vector3 A, Vector3[]points){
		float[] nearest=new float[2];
		nearest[0]=-1;
		nearest[1]=99999;
		float[] distances=new float[points.Length];
		for(int i=0; i<points.Length;i++){
			distances[i]=(points[i]-A).magnitude;
			if(distances[i]<nearest[1]){
				nearest[0]=i;
				nearest[1]=distances[i];
			}
		}
		return nearest;
	}

	// Update is called once per frame
	void Update () {
		if(enabled){
			furnishingRoom=populatingT1.floorMesh;
			
			roomCenter=furnishingRoom.collider.bounds.center;
			shiftedVector=furnishingRoom.transform.position;
			roomExtents=furnishingRoom.collider.bounds.extents;
			roomDiagonalXZ= new Vector2(Room.roomExtents.x,Room.roomExtents.z).magnitude;
			/**
		 * Read .txt file:
		 * to find the nearest room center
		 */
			List<Vector3> list=new List<Vector3>();
			theSourceFile = new FileInfo ("Assets/Autofurnishing/scripts/rooms.txt");
			reader = theSourceFile.OpenText();
			
			//read centers coordinates
			text=reader.ReadLine();
			do{
				if(text.StartsWith("c")){
					string[] word=text.Split(' ');
					//word[0]="center"
					//word[1,2,3]=<x,y,z>
					list.Add(new Vector3(float.Parse(word[1]),
					                     float.Parse(word[2]),
					                     float.Parse(word[3])));
				}//if text startwith
				
				text=reader.ReadLine();
			}while(text != null);
			reader.Close();
			
			//save list as a new array
			Vector3[] centers=list.ToArray();
			list.Clear();
			
			//TxtCentersCoord.*0.1+UnityShifted=UnityWorldCoord
			//so in order to make calculation less,
			//SearchPoint=(UnityWorldCoord-shifted)*10
			roomID=(int)(FindNearestPoint(
				(roomCenter-GameObject.Find(floorplanName).transform.position)*10,
				centers)[0]+1);
			roomCenter=centers[roomID-1]*0.1f+furnishingRoom.transform.position;
			//		Debug.Log("nearest point is "+centers[roomID-1]);
			
			/**
		 * Read txt file find Room floor all corners coordinates
		 */
			//read centers coordinates
			reader = theSourceFile.OpenText();
			text=reader.ReadLine();
			do{
				if(text.StartsWith("R")){
					string[] rID=text.Split(' ');
					
					if(int.Parse(rID[1])==roomID){
						roomArea=float.Parse(rID[2]);//RoomID <roomID> <roomArea>
						text=reader.ReadLine();//center
						
						text=reader.ReadLine();//corners starts
						do{
							string[] word=text.Split(' ');
							Vector3 corner=new Vector3(float.Parse(word[0]),
							                           float.Parse(word[1]),
							                           float.Parse(word[2]));
							corner=corner*0.1f+GameObject.Find(floorplanName).transform.position;
							list.Add(corner);
							
							text=reader.ReadLine();
						}while(text!=null && !text.StartsWith("R"));
						break;
					}//if int (found this room beginning line)
					
				}//if text startwith	
				text=reader.ReadLine();
			}while(text != null);
			reader.Close();
			
			floorCorners=list.ToArray();
			list.Clear();
			
			/**
		 * Get walls infomation
		 */
			int NumOfCorners=floorCorners.Length;
			walls=new Vector3[NumOfCorners,3];
			for(int i=0;i<NumOfCorners;i++){
				//[][0]wall start point
				walls[i,0]=floorCorners[i];
				
				//[][1]wall end point
				if(i+1<NumOfCorners){
					walls[i,1]=floorCorners[i+1];
				}else{
					walls[i,1]=floorCorners[0];
				}
				
				//calculate the wall normal
				//Unity is left hand for cross product: http://docs.unity3d.com/ScriptReference/Vector3.Cross.html
				//and the output wall order is clockwise
				//in order to point inside the room:
				//it should be Y-axis cross the wallline pointing to the end point
				Vector3 A=walls[i,1]-walls[i,0];//pointing to the wall end point
				Vector3 normal= Vector3.Cross(new Vector3(0,1,0),A);
				normal=normal.normalized;
				
				walls[i,2]=normal;
				
			}//get all walls lines
			
			isfinished=true;
		}

	}//Update()
}//class
