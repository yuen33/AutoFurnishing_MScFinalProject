using UnityEngine;
using System.Collections;
using System.IO;


public class WriteToTXT : MonoBehaviour {
	bool isFinished=false;

	string path;
	string filename;

	// Use this for initialization
	void Start () {
		path="Assets/Autofurnishing/scripts/";
	
	}
	
	// Update is called once per frame
	void Update () {
	
		if(populatingT1.isfinished && !isFinished){
			filename=populatingT1.floorName+".txt";
			// Write the string to a file.
			System.IO.StreamWriter file = new System.IO.StreamWriter(path+filename);

			for(int i=0;i<populatingT1.boxesT1.Count;i++){
				file.WriteLine("name "+ populatingT1.boxesT1[i].name);
				file.WriteLine(populatingT1.globalBestT1[i,0].x.ToString() +" "
				               +populatingT1.globalBestT1[i,0].y.ToString() +" "
				               +populatingT1.globalBestT1[i,0].z.ToString() +" ");
				file.WriteLine(populatingT1.globalBestT1[i,1].x.ToString() +" "
				               +populatingT1.globalBestT1[i,1].y.ToString() +" "
				               +populatingT1.globalBestT1[i,1].z.ToString() +" ");
				file.WriteLine(populatingT1.globalBestT1[i,2].x.ToString() +" "
				               +populatingT1.globalBestT1[i,2].y.ToString() +" "
				               +populatingT1.globalBestT1[i,2].z.ToString() +" ");
				               

			}
			
			file.Close();

			isFinished=true;
		}
	}//Update()
}
