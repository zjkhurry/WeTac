using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections;

namespace Obi{
	
	[CustomEditor(typeof(ObiSoftbodySkinner)), CanEditMultipleObjects] 
	public class ObiSoftbodySkinnerEditor : Editor
	{
		
		public ObiSoftbodySkinner skinner;
		protected IEnumerator routine;

        private ObiRaycastBrush paintBrush;
        private ObiSoftbodyInfluenceChannel currentProperty = null;
        private Material paintMaterial;

        private bool editInfluences = false;
		
		public void OnEnable()
        {
			skinner = (ObiSoftbodySkinner)target;

            paintBrush = new ObiRaycastBrush(null,
                                                         () =>
                                                         {
                                                             // As RecordObject diffs with the end of the current frame,
                                                             // and this is a multi-frame operation, we need to use RegisterCompleteObjectUndo instead.
                                                             Undo.RegisterCompleteObjectUndo(target, "Paint influences");
                                                         },
                                                         () =>
                                                         {
                                                             SceneView.RepaintAll();
                                                         },
                                                         () =>
                                                         {
                                                             EditorUtility.SetDirty(target);
                                                         });

            currentProperty = new ObiSoftbodyInfluenceChannel(this);

            if (paintMaterial == null)
                paintMaterial = Resources.Load<Material>("PropertyGradientMaterial");
        }
		
		public void OnDisable()
        {
			EditorUtility.ClearProgressBar();
        }

		private void BakeMesh()
        {

			SkinnedMeshRenderer skin = skinner.GetComponent<SkinnedMeshRenderer>();

			if (skin != null && skin.sharedMesh != null)
            {

				Mesh baked = new Mesh();
				skin.BakeMesh(baked);

				ObiEditorUtils.SaveMesh(baked,"Save extruded mesh","rope mesh",false,true);
			}
		}

        protected void NonReadableMeshWarning(Mesh mesh)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Texture2D icon = EditorGUIUtility.Load("icons/console.erroricon.png") as Texture2D;
            EditorGUILayout.LabelField(new GUIContent("The renderer mesh is not readable. Read/Write must be enabled in the mesh import settings.", icon), EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Fix now", GUILayout.MaxWidth(100), GUILayout.MinHeight(32)))
            {
                string assetPath = AssetDatabase.GetAssetPath(mesh);
                ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (modelImporter != null)
                {
                    modelImporter.isReadable = true;
                }
                modelImporter.SaveAndReimport();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        protected bool ValidateRendererMesh()
        {
            SkinnedMeshRenderer skin = skinner.GetComponent<SkinnedMeshRenderer>();

            if (skin != null && skin.sharedMesh != null)
            {
                if (!skin.sharedMesh.isReadable)
                {
                    NonReadableMeshWarning(skin.sharedMesh);
                    return false;
                }
                return true;
            }
            return false;
        }

        public override void OnInspectorGUI() {
			
			serializedObject.Update();		

			GUI.enabled = skinner.Source != null && ValidateRendererMesh();
            if (GUILayout.Button("Bind skin")){

					EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
					CoroutineJob job = new CoroutineJob();
					routine = job.Start(skinner.BindSkin());
					EditorCoroutine.ShowCoroutineProgressBar("Binding to particles...",ref routine);
					EditorGUIUtility.ExitGUI();

			}
			if (GUILayout.Button("Bake Mesh")){
				BakeMesh();
			}

            EditorGUI.BeginChangeCheck();
            editInfluences = GUILayout.Toggle(editInfluences,new GUIContent("Paint Influence"),"Button");
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();

            if (editInfluences && paintBrush != null)
            {
                currentProperty.BrushModes(paintBrush);

                if (paintBrush.brushMode.needsInputValue)
                    currentProperty.PropertyField();

                paintBrush.radius = EditorGUILayout.Slider("Brush size", paintBrush.radius, 0.0001f, 0.5f);
                paintBrush.innerRadius = EditorGUILayout.Slider("Brush inner size", paintBrush.innerRadius, 0, 1);
                paintBrush.opacity = EditorGUILayout.Slider("Brush opacity", paintBrush.opacity, 0, 1);
            }

			GUI.enabled = true;

			if (skinner.Source == null){
				EditorGUILayout.HelpBox("No source softbody present.",MessageType.Info);
			}

			skinner.Source = EditorGUILayout.ObjectField("Source softbody",skinner.Source, typeof(ObiSoftbody), true) as ObiSoftbody;

			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				serializedObject.ApplyModifiedProperties();
			}
		}

        public void OnSceneGUI()
        {
            if (editInfluences)
            {
                skinner.InitializeInfluences();

                SkinnedMeshRenderer skin = skinner.GetComponent<SkinnedMeshRenderer>();

                if (skin != null && skin.sharedMesh != null)
                {
                    var bakedMesh = new Mesh();
                    skin.BakeMesh(bakedMesh);

                    if (Event.current.type == EventType.Repaint)
                        DrawMesh(bakedMesh);

                    if (Camera.current != null)
                    {
                        paintBrush.raycastTarget = bakedMesh; 
                        paintBrush.raycastTransform = skin.transform.localToWorldMatrix;

                        // TODO: do better.
                        var v = bakedMesh.vertices;
                        Vector3[] worldSpace = new Vector3[v.Length];
                        for (int i = 0; i < worldSpace.Length; ++i)
                            worldSpace[i] = paintBrush.raycastTransform.MultiplyPoint3x4(v[i]);

                        paintBrush.DoBrush(worldSpace);
                    }

                    DestroyImmediate(bakedMesh);
                }

            }
        }

        private void DrawMesh(Mesh mesh)
        {
            if (paintMaterial.SetPass(0))
            {
                Color[] colors = new Color[mesh.vertexCount];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = new Color(skinner.m_softbodyInfluences[i], skinner.m_softbodyInfluences[i], skinner.m_softbodyInfluences[i]);
                }

                mesh.colors = colors;
                Graphics.DrawMeshNow(mesh, paintBrush.raycastTransform);

                if (paintMaterial.SetPass(1))
                {
                    Color wireColor = ObiEditorSettings.GetOrCreateSettings().brushWireframeColor;
                    for (int i = 0; i < paintBrush.weights.Length; i++)
                    {
                        colors[i] = wireColor * paintBrush.weights[i];
                    }

                    mesh.colors = colors;
                    GL.wireframe = true;
                    Graphics.DrawMeshNow(mesh, paintBrush.raycastTransform);
                    GL.wireframe = false;
                }
            }
        }
	}
}


