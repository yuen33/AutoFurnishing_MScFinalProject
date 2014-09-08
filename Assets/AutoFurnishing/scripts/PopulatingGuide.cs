using UnityEngine;
using System.Collections;
//using System.StringSplitOptions;
using System.IO;
using System.Collections.Generic;//for List<T>, Queue<T>


public class PopulatingGuide : MonoBehaviour {
	private static PopulatingGuide instance = null;
	// to create a singleton which holds the variables
	public static PopulatingGuide Instance{
		get{
			if(instance==null){
				instance=new GameObject("PopulatingGuide").AddComponent<PopulatingGuide>();
			}
			return instance;
		}
	}

//	public int smallestXZ=20;
	//in descending order of importance
//	public string[] bedroomT1_old={"bed","wardrobe","chair","table","bedside_table"};

	//-------------------------------new-----------------------------------
	public bool isfinished=false;
	public string roomType;
	public string[][] furnitureArray;
	public double areaRank;

	/**
	 * The second line: choose any one
	 * 3rd and so on: together with being populated or not being populated
	 */
	string[][] bedroomT1={
		new string[]{"bed","bedside_table","wardrobe","table","chair"},
		new string[]{"shelf","armchair","bookcase"},
		new string[]{"sofa","teatable"},
		new string[]{"soundbox"}
	};
	string[][] singlebedroomT1={
		new string[]{"bed","table","chair"},
		new string[]{"wardrobe","bedside_table"},
		new string[]{"bookcase","armchair"},
		new string[]{"shelf","soundbox"}
	};
	string[][] livingroomT1={
		new string[]{"tv","sofa","teatable","bookcase","dining_table"},
		new string[]{"table","armchair"},
		new string[]{"shelf","soundbox"},
		new string[]{"kitchen_table"}
	};
	string[][] bathroomT1={
		new string[]{},//empty
		new string[]{"basins","toilet","washer"},//bathtub or shower
		new string[]{"shower"},
	};
	string[][] kitchenT1={
		new string[]{"kitchen_table","fridge","cupboard"},
		new string[]{"washer","cupboard"},
		new string[]{"dining_table"}//they are suits with chairs
	};
	string[][] readingroomT1={
		new string[]{"table","chair","bookcase"},
		new string[]{"sofa","shelf","wardrobe"},
		new string[]{"doublechairs"}
	};

	protected FileInfo theSourceFile = null;
	protected StreamReader reader = null;
	protected string text = " "; // assigned to allow first line to be read below
	List<int> list1;
	List<int> list2;

	// Use this for initialization
	void Start () {
		/**
		 * Read .txt file:
		 * to find the nearest room center
		 */
		list1=new List<int>();
		list2=new List<int>();
		theSourceFile = new FileInfo ("Assets/Autofurnishing/scripts/rooms.txt");
		reader = theSourceFile.OpenText();
		
		//read centers coordinates
		text=reader.ReadLine();
		do{
			if(text.StartsWith("RoomID")){
				string[] word=text.Split(' ');
				//word[0]="RoomID"
				//word[1]=<RoomID>
				//word[2]=<RoomArea>
				list1.Add(int.Parse(word[1]));
				list2.Add((int) (double.Parse(word[2])/100));

			}//if text startwith
			
			text=reader.ReadLine();
		}while(text != null);
		reader.Close();

	}//Start()

	void getFurniture(int RoomID, int[] AreaSortedRoomIDs){
		areaRank=0;
		for(int i=0;i<AreaSortedRoomIDs.Length;i++){
			if(RoomID==AreaSortedRoomIDs[i]){
				areaRank=i+1;
				break;
			}//if roomID is found
		}//for

		double k=areaRank/AreaSortedRoomIDs.Length;
		areaRank=k;
		//2.5/12=0.208333
		//7.5/12=0.625
		//10.5/12=0.875
		if(k<0.208){//bathroom
			roomType="bathroom";
			furnitureArray=bathroomT1;
			//Debug.Log("is bathroom");

		}else if(k>0.875){//livingroom (+)
			roomType="livingroom";
			furnitureArray=livingroomT1;
			//Debug.Log("is living room");

		}else if(k>0.625){//bedroom
			if(Random.value>0.8){
				roomType="singlebedroom";
				furnitureArray=singlebedroomT1;
				//Debug.Log("is single bedroom");
			}else{
				roomType="bedroom";
				furnitureArray=bedroomT1;
				//Debug.Log("is bedroom");
			}//if random else

		}else{//single bedroom/reading room/kitchen
			switch((int)(Random.value*3)%3){
			case 0: //can be 0...0 and 1...0 (I hope singlebedroom can be more than the others)
				roomType="singlebedroom";
				furnitureArray=singlebedroomT1;
				//Debug.Log("is single bedroom");
				break;

			case 1: //only can be 0...1
				roomType="kitchen";
				furnitureArray=kitchenT1;
				//Debug.Log("is kitchen");
				break;

			default://only can be 0...2
				roomType="readingroom";
				furnitureArray=readingroomT1;
				//Debug.Log("is reading room");
				break;
			}//switch

		}//if else if...

	}//void getRoomType()
	
	// Update is called once per frame
	void Update () {
		if(Room.isfinished && !isfinished){
			//save list as a new array
			int[] AreaSortedRoomIDs=list1.ToArray();
			int[] RoomAreas=list2.ToArray();
			System.Array.Sort(RoomAreas,AreaSortedRoomIDs);
			list1.Clear();
			list2.Clear();

			getFurniture(Room.roomID,AreaSortedRoomIDs);

			isfinished=true;
		}//if Room.isfinished
	}//Update()
}
