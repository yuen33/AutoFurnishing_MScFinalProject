using UnityEngine;
using System.Collections;
using System.Collections.Generic;//get mesh vertices

public class RoomAttributes : MonoBehaviour {
	private static RoomAttributes instance = null;
	// to create a singleton which holds the variables
	public static RoomAttributes Instance{
		get{
			if(instance==null){
				instance=new GameObject("RoomAttributes").AddComponent<RoomAttributes>();
			}
			return instance;
		}
	}

//	bool getRoomAttributes=false;
	//inputs
	public GameObject door1;
	public int door1wall;//0 90 180 270
	public GameObject door2;
	public int door2wall;
	public GameObject window1;
	public int window1wall;
	public GameObject window2;
	public int window2wall;
	public GameObject window3;
	public int window3wall;
	public GameObject window4;
	public int window4wall;
	
	public Vector3 roomCenter;
	public Vector3 roomExtents;
	public float room_xz_diagonalLength;
	public float roomArea;//size.x*size.z

	public float wall0_z;
	public float wall90_x;
	public float wall180_z;
	public float wall270_x;

	public int NumOfCorners;
	public Vector3 cornerA;
	public Vector3 cornerB;
	public Vector3 cornerC;
	public Vector3 cornerD;

//	//This method is wrong... no idea why it's wrong for getting corners..
//	/**
//			 * Vector3[] vertices: (Note the index and position)
//			 * ^z
//			 * |-->x
//			 * 
//			 * vertices[1]---vertices[2]
//			 * 		|			|
//			 * 		|	room	|
//			 * 		|			|
//			 * vertices[3]---vertices[0]
//			 * 
//			 * However, I defined:
//			 * cornerB---cornerC
//			 * 	|			|
//			 * 	|	room	|
//			 * 	|			|
//			 * cornerA---cornerD
//			 */
//	Vector3[] GetVerticesInChildren(GameObject go) {
//		MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
//		List<Vector3> vList = new List<Vector3>();
//		foreach (MeshFilter mf in mfs) {
//			vList.AddRange (mf.mesh.vertices);
//		}
//		return vList.ToArray ();
//	}

	// Use this for initialization
	void Start () {
		gameObject.AddComponent<BoxCollider>();
//		if(door1!=null){
//			switch(door1wall){
//			case 0:
//				break;
//			case 90:
//				break;
//			case 180:
//				break;
//			case 270:
//				break;
//			default:
//				Debug.Log("Error on door");
//				break;
//			}
//
//			GameObject doorShelter=(GameObject)Instantiate(GameObject.Find("DoorShelter"),
//			                        transform.position+getPosition(BigPiece,id)
//			                        ,transform.rotation);
//
//		}

		roomCenter=gameObject.collider.bounds.center;
		roomExtents=gameObject.collider.bounds.extents;
		room_xz_diagonalLength=2*Mathf.Sqrt(roomExtents.x*roomExtents.x+roomExtents.z*roomExtents.z);
		roomArea=4*roomExtents.x*roomExtents.z;

		

		/**
		 * I defined:
		 * 				wall180
		 * 		cornerB-------cornerC
		 * 			|			|
		 *   wall90	|	room	| wall270
		 * 			|			|
		 * 		cornerA---------cornerD
		 * 				wall0
		 */
		wall0_z=roomCenter.z-roomExtents.z;
		wall90_x=roomCenter.x-roomExtents.x;
		wall180_z=roomCenter.z+roomExtents.z;
		wall270_x=roomCenter.x+roomExtents.x;
		cornerA=roomCenter-roomExtents;
		cornerA.y=roomCenter.y;
		cornerB=new Vector3(roomCenter.x-roomExtents.x,roomCenter.y,roomCenter.z+roomExtents.z);
		cornerC=roomCenter+roomExtents;
		cornerC.y=roomCenter.y;
		cornerD=new Vector3(roomCenter.x+roomExtents.x,roomCenter.y,roomCenter.z-roomExtents.z);

		/**
		 * 		cornerB-------cornerC
		 * 			|			|
		 *   		|	room	| 
		 * 			|			|
		 * 		cornerA---------cornerD
		 */

//		getRoomAttributes=true;
	}
	
	// Update is called once per frame
	void Update () {
	
	}


}


