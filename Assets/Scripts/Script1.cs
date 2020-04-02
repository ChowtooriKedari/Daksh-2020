using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script1 : MonoBehaviour
{
    public TextMesh s;
    // Start is called before the first frame update
    void Start()
    {
        
    }


    // Update is called once per frame
    void Update()
    {
        
    }
    void OnCollisionEnter(Collision C)
    {
        Debug.Log("Collision Occured");
        s.text = "Collided With" + C.collider.name;
    }
}
