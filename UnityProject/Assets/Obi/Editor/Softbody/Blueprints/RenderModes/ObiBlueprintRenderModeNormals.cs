using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Obi
{
    public class ObiBlueprintRenderModeNormals : ObiBlueprintRenderMode
    {
        public override string name
        {
            get { return "Normals"; }
        }

        public ObiBlueprintRenderModeNormals(ObiSoftbodySurfaceBlueprintEditor editor) : base(editor)
        {
        }

        public override void OnSceneRepaint(SceneView sceneView)
        {
            using (new Handles.DrawingScope(Color.blue, Matrix4x4.identity))
            {
                var blueprint = (ObiSoftbodySurfaceBlueprint)editor.blueprint;

                List<Vector3> lines = new List<Vector3>();

                for (int i = 0; i < blueprint.activeParticleCount; ++i)
                {
                    lines.Add(blueprint.positions[i]);
                    lines.Add(blueprint.positions[i] + blueprint.orientations[i] * Vector3.forward * 0.25f);
                }

                Handles.DrawLines(lines.ToArray());
            }
        }

    }
}