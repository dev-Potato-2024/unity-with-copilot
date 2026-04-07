using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AISphere))]
public class AISphereEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AISphere sphere = (AISphere)target;
        GUILayout.Space(8);

        if (GUILayout.Button("Redraw Chunks"))
        {
            sphere.RedrawChunks();
            EditorUtility.SetDirty(sphere);
        }
    }
}
