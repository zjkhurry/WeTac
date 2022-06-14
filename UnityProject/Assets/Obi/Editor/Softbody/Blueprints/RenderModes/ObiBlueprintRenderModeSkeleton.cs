using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Obi
{
    public class ObiBlueprintRenderModeSkeleton : ObiBlueprintRenderMode
    {
        public override string name
        {
            get { return "Skeleton"; }
        }

        public ObiBlueprintRenderModeSkeleton(ObiSoftbodySurfaceBlueprintEditor editor) : base(editor)
        {
        }

        public override void OnSceneRepaint(SceneView sceneView)
        {
            using (new Handles.DrawingScope(Color.magenta, Matrix4x4.identity))
            {
                Quaternion boneRotation = ((ObiSoftbodySurfaceBlueprint)editor.blueprint).boneRotation;

                Queue<Transform> queue = new Queue<Transform>();
                queue.Enqueue(((ObiSoftbodySurfaceBlueprint)editor.blueprint).rootBone);

                List<Vector3> lines = new List<Vector3>();

                while (queue.Count > 0)
                {
                    var bone = queue.Dequeue();

                    if (bone != null)
                    {
                        foreach (Transform child in bone)
                        {
                            lines.Add(boneRotation * bone.position);
                            lines.Add(boneRotation * child.position);
                            queue.Enqueue(child);
                        }

                    }
                }

                Handles.DrawLines(lines.ToArray());
            }
        }

    }
}