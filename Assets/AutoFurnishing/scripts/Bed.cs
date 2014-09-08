using UnityEngine;
using System.Collections;
using System.Collections.Generic;//for List<T>, Queue<T>

public class Bed : MonoBehaviour {
//	private static Bed instance = null;
//	// to create a singleton which holds the variables
//	public static Bed Instance{
//		get{
//			if(instance==null){
//				instance=new GameObject("Bed").AddComponent<Bed>();
//			}
//			return instance;
//		}
//	}
	
	/** Oriented Bounding Box face code:
	 * -like a die:
	 * 
	 *     ^ y 				1: top area
	 *     |				6: bottom area
	 *    _|__
	 * 	/__1_ /|
	 * |     |2|			2: facing area
	 * |  4  | |-------->z	5: back area
	 * |_____|/
	 *    /
	 *   / x				3: left area
	 *  v					4: right area
	 * Four boolean preference considerations:
	 * [0]			[1]				[2]				[3]
	 * alongWalls	accessibleArea	childrenArea	echoingArea
	 * 
	 * Bed:
	 * 			 __2__
	 * ^z		 |   |
	 * |		3|   |4
	 * +-->x	 =====		(1:top;6:bottom)
	 * 			   5
	 */
	public static bool[,] OBBPreference;
	string alongWalls="3 4 5";
	string accessibleArea="2 3 4";
	string childrenArea="1 3 4";
	string echoingArea="";  
	Vector3 center;
	
	Vector3[] GetVerticesInChildren(GameObject go) {
		MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
		List<Vector3> vList = new List<Vector3>();
		foreach (MeshFilter mf in mfs) {
			vList.AddRange (mf.mesh.vertices);
		}
		return vList.ToArray ();
	}
	
	Vector3[] GetBoundsVertices(Vector3[] vertices){
		float xmax=-99999;
		float xmin=99999;
		float ymax=-99999;
		float ymin=99999;
		float zmax=-99999;
		float zmin=99999;
		//[0]xmin,zmin	[1]xmin,zmax
		//[2]xmax,zmin	[3]xmax,zmax
		Vector3[] boundsVertices=new Vector3[2];
		for(int i=0;i<vertices.Length;i++){
			xmax = Mathf.Max (xmax, vertices[i].x);
			xmin = Mathf.Min (xmin, vertices[i].x);
			ymax = Mathf.Max (ymax, vertices[i].y);
			ymin = Mathf.Min (ymin, vertices[i].y);
			zmax = Mathf.Max (zmax, vertices[i].z);
			zmin = Mathf.Min (zmin, vertices[i].z);
		}//for
		boundsVertices[0].x = xmin;
		boundsVertices[1].x = xmax;
		boundsVertices[0].y = ymin;
		boundsVertices[1].y = ymax;
		boundsVertices[0].z = zmin;
		boundsVertices[1].z = zmax;
		return boundsVertices;
	}
	
	// Use this for initialization
	void Start () {	
		center=gameObject.collider.bounds.center;
		Vector3[] array=GetVerticesInChildren(gameObject);
		Debug.Log("--------------------"+center);
		Debug.Log("--------------------"+array.Length);
		Vector3[] boundsVertices=GetBoundsVertices(array);
		for(int i=0;i<boundsVertices.Length;i++){
			Debug.Log(boundsVertices[i]);
		}
		
		
		int NumOfBool=4;
		OBBPreference=new bool[7,NumOfBool];//in order to use ID as 1~6, the first element[0] is useless
		for(int i=0;i<NumOfBool;i++) inputOBBPreference(i);
		
	}
	
	void inputOBBPreference(int boolID){
		//		print(boolID);
		string[] idx;
		switch(boolID){
		case 0:
			idx=alongWalls.Split(' ');
			break;
		case 1:
			idx=accessibleArea.Split(' ');
			break;
		case 2:
			idx=childrenArea.Split(' ');
			break;
		default: //case 3
			idx=echoingArea.Split(' ');
			break;
		}
		
		if(!idx[0].Equals("")){//for non empty inputs
			for(int i=0;i<idx.Length;i++){
				int faceCode=int.Parse(idx[i]);
				//				print(faceCode);
				if(faceCode>0 && faceCode<7) OBBPreference[faceCode,boolID]=true;
			}//for int
		}//if idx[0]
	}//inputOBB...()
	
	//	// Update is called once per frame
	//	void Update () {
	//	
	//	}
}


