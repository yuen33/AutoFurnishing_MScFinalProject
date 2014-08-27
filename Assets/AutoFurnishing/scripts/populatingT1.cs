using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;//for List<T>, Queue<T>

public class populatingT1 : MonoBehaviour {
	//Input:
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
	public static string floorName;

	//-----------------------------------------------
	public string[][] pairwiseTAG={
		new string[]{"table","chair"},
		new string[]{"tv","sofa"},
		new string[]{"sofa","teatable"},
		new string[]{"bed","bedside_table"}, 
		new string[]{"single_bed","bedside_table"}//not all bed needs a bedsidetable...
	};
	//-----------------------------------------------

	bool isInitialised=false;
	bool isRunning=false;
	bool isfinished=false;

	Vector3[][]Doors;//(namecode,wallID,0)(center)(extents) Vector3(width,depth,height)
	Vector3[][]Fireplaces;//(namecode,wallID,0)(center)(extents) Vector3(width,depth,height)
	Vector3[][]Windows;//(namecode,wallID,0)(center)(extents) Vector3(width,depth,height)

	int populatingState=0;//max=num Of Populated furniture
	int[] indiceT1;//in ascending order of furniture volumns (smallest to largest)
	
	public double globalBest=0;
	public double currentScore;
	public double lastScore;
	public float step=1.2f;
	public int iteration=0;
	int period=150;
	Vector3[,] lastPosition; //(position)(rotation)
	float distanceFactor=20;
	double beta=1;
	float lastRotationY;
	
	// Use this for initialization
	void Start () {
		floorName="room_4_646";
		Room.enabled=true;
	}
	
	// Update is called once per frame
	void Update () {
		if(!isInitialised && PopulatingGuide.Instance.isfinished
		   && InRoomRetrieval.Instance.isfinished){
			initialization();
			//...

			//-----End-----
			isInitialised=true;
		}//if(PopulatingGuide.Instance.isfinished
	}//Update()

	//-------------------------initialization()--------------------------
	void initialization(){
		getKnownFloorplanFurniture();
		determineFurniture();



	}
	//-------------------------initialization()--------------------------

	void determineFurniture(){
		int NumOfRows=PopulatingGuide.Instance.furnitureArray.GetLength(0);
		double rank=PopulatingGuide.Instance.areaRank;
		if(rank>0.5){//is a large room of the house
//			int k=PopulatingGuide.Instance.furnitureArray.GetLength(1);
			int i=0;
			int j=0;
			while(PopulatingGuide.Instance.furnitureArray[i][j]!=null){
				Debug.Log(i+j);
			}

		}else{//is a small room of the house

		}

	}//determineFurniture()

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
				float depth=InRoomRetrieval.Instance.floorplanFurniture[i][3].y;//fireplace depth
				//position is the fireplace center move towards wall normal with fireplace depth
				fireplaceBlock.transform.position=InRoomRetrieval.Instance.floorplanFurniture[i][1]+
					Room.walls[wallID,2]*depth;
				
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



}//Class
