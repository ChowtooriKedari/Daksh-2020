using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TrackBuilder))]
public class TrackBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TrackBuilder trackBuilderScript = (TrackBuilder)target;
        if (GUILayout.Button("Generate Track"))
        {
            trackBuilderScript.BuildTrack();
        }
    }
}
