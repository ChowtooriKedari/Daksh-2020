using UnityEngine;
using SPEngine;


public class TrackBuilder : MonoBehaviour
{
    public GameObject splineRoot;

    public GameObject leftRailPrefab;
    public GameObject rightRailPrefab;
    public GameObject crossBeamPrefab;

    public float resolution = 0.005f;

    private VR vr = new VR();

    // We won't initialise VR class in Start because this script will be used in Edit Mode only and not during play mode. So the start method doesnt get called at all.
    void Start()
    {

    }
    public void BuildTrack()
    {
        
        vr.BuildRollercoasterTrack(gameObject, splineRoot, leftRailPrefab, rightRailPrefab, crossBeamPrefab, resolution);
    }


}

