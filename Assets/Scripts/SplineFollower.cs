using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SPEngine;

public class SplineFollower : MonoBehaviour
{

    public GameObject splineRoot;
    public float speed;

    private VR vr;
    // Use this for initialization
    void Start()
    {
        vr = new VR();
    }

    // Update is called once per frame
    void Update()
    {
        // TRAVEL_MODE_LOOP, TRAVEL_MODE_ONCE, TRAVEL_MODE_TO_AND_FRO are all static constants of the class VR. Hence they can be accessed using the class name VR and no need an instance of the VR class to access these.
        vr.MoveAlongSpline(splineRoot, gameObject, speed, VR.TRAVEL_MODE_LOOP);
    }
}
