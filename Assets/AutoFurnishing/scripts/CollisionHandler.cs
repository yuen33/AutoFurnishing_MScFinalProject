using UnityEngine;
using System.Collections;

public class CollisionHandler : MonoBehaviour {

	void OnCollisionEnter(Collision col){
		Vector3 movingDirection;
//		Debug.Log("It starts to enter the floorplan!======================================");
		
		//moving along nearest wall normal
		Vector3 Pi=col.gameObject.collider.bounds.center;
		int wallID_Pi=InRoomRetrieval.FindWall(new Vector2(Pi.x,Pi.z));
		movingDirection=Room.walls[wallID_Pi,2];//wall normal
		col.gameObject.rigidbody.detectCollisions=false;
		col.gameObject.transform.position=col.gameObject.transform.position+movingDirection*0.01f;
		col.gameObject.rigidbody.detectCollisions=true;
	}

	void OnCollisionStay(Collision col) {
		Vector3 movingDirection;
//		Debug.Log("It Collides!======================================");

		//moving along nearest wall normal
		Vector3 Pi=col.gameObject.collider.bounds.center;
		int wallID_Pi=InRoomRetrieval.FindWall(new Vector2(Pi.x,Pi.z));
		movingDirection=Room.walls[wallID_Pi,2];//wall normal
		col.gameObject.rigidbody.detectCollisions=false;
		col.gameObject.transform.position=col.gameObject.transform.position+movingDirection*0.02f;
		col.gameObject.rigidbody.detectCollisions=true;
		
	}
		

//		foreach (ContactPoint contact in collisionInfo.contacts) {
//
//			Debug.DrawRay(contact.point, contact.normal * 10, Color.white);
//		}

//	// Use this for initialization
//	void Start () {
//	
//	}
//	
//	// Update is called once per frame
//	void Update () {
//	
//	}
}
