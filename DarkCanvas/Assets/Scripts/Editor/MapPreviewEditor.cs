using DarkCanvas.ProceduralTerrain;
using UnityEditor;
using UnityEngine;

namespace DarkCanvas.Editor
{
    [CustomEditor(typeof(MapPreview))]
    public class MapPreviewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var mapPreview = (MapPreview)target;

            if ((DrawDefaultInspector() && mapPreview.AutoUpdate) ||
                GUILayout.Button("Generate"))
            {
                mapPreview.DrawMapInEditor();
            }
        }
    }
}
