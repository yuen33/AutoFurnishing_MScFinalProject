using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;//for List<T>, Queue<T>


public class ReadFurnishedRoom : MonoBehaviour {
	protected FileInfo theSourceFile = null;
	protected StreamReader reader = null;
	protected string text = " "; // assigned to allow first line to be read below

	string path="Assets/Autofurnishing/scripts/";
	bool isfinished=false;
	
	string[] fileName;//room name

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

	// Use this for initialization
	void Start () {
		fileName=new string[9];
		fileName[0]="room_4_646";
		fileName[1]="room_3_644";
		fileName[2]="room_2_642";
		fileName[3]="room_1_640";
		fileName[4]="room_8_653";
		fileName[5]="room_11_659";
		fileName[6]="room_12_661";
		fileName[7]="room_10_657";
		fileName[8]="room_5_648";
	
	}
	
	// Update is called once per frame
	void Update () {
//		if(Input.GetKeyDown(KeyCode.S)){
		if(!isfinished){
//			readfile(fileName[0]);
//			readfile(fileName[1]);
//			readfile(fileName[2]);
//			readfile(fileName[3]);
//			readfile(fileName[4]);
//			readfile(fileName[5]);
//			readfile(fileName[6]);
//			readfile(fileName[7]);

			for(int i=0;i<9;i++){
				readfile(fileName[i]);
			}

			isfinished=true;
		}//if press s
	}

	void readfile(string roomname){
		/**
		 * Read .txt file:
		 * to find the nearest room center
		 */
		List<string> name=new List<string>();
		List<Vector3> position=new List<Vector3>();
		List<Vector3> rotation=new List<Vector3>();
		List<Vector3> extents=new List<Vector3>();
		
		theSourceFile = new FileInfo (path+roomname+".txt");
		reader = theSourceFile.OpenText();
		
		//read centers coordinates
		text=reader.ReadLine();
		do{
			if(text.StartsWith("n")){
				string[] word=text.Split(' ');
				//word[0]="name"
				//word[1,2,3]=<name>
				
				name.Add(word[1]);
				//-------------------------
				text=reader.ReadLine();
				string[] pos=text.Split(' ');
				
				position.Add(new Vector3(float.Parse(pos[0]),
				                         float.Parse(pos[1]),
				                         float.Parse(pos[2])));
				//-------------------------
				text=reader.ReadLine();
				string[] rot=text.Split(' ');
				
				rotation.Add(new Vector3(float.Parse(rot[0]),
				                         float.Parse(rot[1]),
				                         float.Parse(rot[2])));
				
				//-------------------------
				text=reader.ReadLine();
				string[] ext=text.Split(' ');
				
				extents.Add(new Vector3(float.Parse(ext[0]),
				                        float.Parse(ext[1]),
				                        float.Parse(ext[2])));
				
				
			}//if text startwith
			
			text=reader.ReadLine();
		}while(text != null);
		reader.Close();
		
		GameObject[] furniture=new GameObject[name.Count];
		
		for(int i=0;i<name.Count;i++){
			furniture[i]=(GameObject)Instantiate(GameObject.Find(name[i]),
			                                     position[i],
			                                     Quaternion.identity);
			furniture[i].transform.eulerAngles=rotation[i];
			furniture[i].name=name[i];
		}
	}//readfile()


}
