using DarkCanvas.Assets.Scripts.ProceduralTerrain;
using UnityEditor;
using UnityEngine;

namespace DarkCanvas.Assets.Scripts.Editor
{
    [CustomEditor(typeof(MapGenerator))]
    public class MapGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var mapGenerator = (MapGenerator)target;

            if ((DrawDefaultInspector() && mapGenerator.AutoUpdate) ||
                GUILayout.Button("Generate"))
            {
                mapGenerator.DrawMapInEditor();
            }
        }
    }
}
