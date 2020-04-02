using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class earthToHead : MonoBehaviour {
    public Transform Spawn;
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        this.transform.position = Spawn.transform.position;

    }
}
