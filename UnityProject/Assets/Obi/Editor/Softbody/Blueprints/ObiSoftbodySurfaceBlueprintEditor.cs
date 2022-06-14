using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Obi
{
    [CustomEditor(typeof(ObiSoftbodySurfaceBlueprint), true)]
    public class ObiSoftbodySurfaceBlueprintEditor : ObiMeshBasedActorBlueprintEditor
    {

        public ObiSoftbodySurfaceBlueprint.ParticleType particleTypeFlags = ObiSoftbodySurfaceBlueprint.ParticleType.All;

        SerializedProperty inputMesh;             
        SerializedProperty scale;
        SerializedProperty rotation;
        SerializedProperty shapeResolution;
        SerializedProperty maxAnisotropy;
        SerializedProperty smoothing;
        SerializedProperty surfaceSamplingMode;
        SerializedProperty surfaceResolution;
        SerializedProperty volumeSamplingMode;
        SerializedProperty volumeResolution;
        SerializedProperty skeleton;
        SerializedProperty rootBone;
        SerializedProperty boneRotation;

        BooleanPreference showSurfaceSampling;
        BooleanPreference showVolumeSampling;
        BooleanPreference showSkeletonSampling;
        BooleanPreference showShapeAnalysis;

        public override Mesh sourceMesh
        {
            get { return softbodyBlueprint != null ? softbodyBlueprint.generatedMesh : null; }
        }

        public ObiSoftbodySurfaceBlueprint softbodyBlueprint
        {
            get { return blueprint as ObiSoftbodySurfaceBlueprint; }
        }

        protected override bool ValidateBlueprint()
        {
            if (softbodyBlueprint != null && softbodyBlueprint.inputMesh != null)
            {
                if (!softbodyBlueprint.inputMesh.isReadable)
                {
                    NonReadableMeshWarning(softbodyBlueprint.inputMesh);
                    return false;
                }
                return true;
            }
            return false;
        }

        protected override void DrawBlueprintProperties()
        {
            EditorGUILayout.PropertyField(inputMesh);
            EditorGUILayout.PropertyField(scale);
            EditorGUILayout.PropertyField(rotation);

            showSurfaceSampling.value = EditorGUILayout.BeginFoldoutHeaderGroup(showSurfaceSampling,"Surface sampling");
            if (showSurfaceSampling)
            {
                EditorGUILayout.PropertyField(surfaceSamplingMode, new GUIContent("Mode"));
                EditorGUILayout.PropertyField(surfaceResolution, new GUIContent("Resolution"));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            showVolumeSampling.value = EditorGUILayout.BeginFoldoutHeaderGroup(showVolumeSampling, "Volume sampling");
            if (showVolumeSampling)
            {
                EditorGUILayout.PropertyField(volumeSamplingMode, new GUIContent("Mode"));
                EditorGUILayout.PropertyField(volumeResolution, new GUIContent("Resolution"));
            }
             EditorGUILayout.EndFoldoutHeaderGroup();

            showSkeletonSampling.value = EditorGUILayout.BeginFoldoutHeaderGroup(showSkeletonSampling, "Skeleton sampling");
            if (showSkeletonSampling)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(skeleton);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(softbodyBlueprint, "Set root bone");
                    rootBone.objectReferenceValue = null;
                }

                DisplayRootBoneSelector();
                EditorGUILayout.PropertyField(boneRotation);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            showShapeAnalysis.value = EditorGUILayout.BeginFoldoutHeaderGroup(showShapeAnalysis, "Shape analysis");
            if (showShapeAnalysis)
            {
                EditorGUILayout.PropertyField(shapeResolution, new GUIContent("Resolution"));
                EditorGUILayout.PropertyField(maxAnisotropy);
                EditorGUILayout.PropertyField(smoothing);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        public override void OnEnable()
        {
            base.OnEnable();

            showSurfaceSampling = new BooleanPreference($"{target.GetType()}.showSurfaceSampling", true);
            showVolumeSampling = new BooleanPreference($"{target.GetType()}.showVolumeSampling", false);
            showSkeletonSampling = new BooleanPreference($"{target.GetType()}.showSkeletonSampling", false);
            showShapeAnalysis = new BooleanPreference($"{target.GetType()}.showShapeAnalysis", false);

            inputMesh = serializedObject.FindProperty("inputMesh");
            scale = serializedObject.FindProperty("scale");
            rotation = serializedObject.FindProperty("rotation");

            surfaceSamplingMode = serializedObject.FindProperty("surfaceSamplingMode");
            surfaceResolution = serializedObject.FindProperty("surfaceResolution");

            volumeSamplingMode = serializedObject.FindProperty("volumeSamplingMode");
            volumeResolution = serializedObject.FindProperty("volumeResolution");

            skeleton = serializedObject.FindProperty("skeleton");
            rootBone = serializedObject.FindProperty("rootBone");
            boneRotation = serializedObject.FindProperty("boneRotation");

            shapeResolution = serializedObject.FindProperty("shapeResolution");
            maxAnisotropy = serializedObject.FindProperty("maxAnisotropy");
            smoothing = serializedObject.FindProperty("smoothing");

            properties.Add(new ObiBlueprintMass(this));
            properties.Add(new ObiBlueprintRadius(this));
            properties.Add(new ObiBlueprintFilterCategory(this));
            properties.Add(new ObiBlueprintColor(this));
            properties.Add(new ObiBlueprintDeformationResistance(this));
            properties.Add(new ObiBlueprintPlasticYield(this));
            properties.Add(new ObiBlueprintPlasticCreep(this));
            properties.Add(new ObiBlueprintPlasticRecovery(this));
            properties.Add(new ObiBlueprintMaxDeformation(this));

            renderModes.Add(new ObiBlueprintRenderModeVoxels(this));
            renderModes.Add(new ObiBlueprintRenderModeMesh(this));
            renderModes.Add(new ObiBlueprintRenderModeShapeMatchingConstraints(this));
            renderModes.Add(new ObiBlueprintRenderModeSkeleton(this));
            renderModes.Add(new ObiBlueprintRenderModeNormals(this));

            // render particles by default:
            renderModeFlags = 1;

            tools.Clear();
            tools.Add(new ObiParticleSelectionEditorTool(this));
            tools.Add(new ObiPaintBrushEditorTool(this));
            tools.Add(new ObiPropertyTextureEditorTool(this));
        }

        private void DisplayRootBoneSelector()
        {
            GUI.enabled = softbodyBlueprint.skeleton != null;
            EditorGUI.indentLevel++;

            var rect = EditorGUILayout.GetControlRect();
            var label = EditorGUI.BeginProperty(rect, new GUIContent("Root bone"), rootBone);
            rect = EditorGUI.PrefixLabel(rect, label);

            bool hasRootNode = softbodyBlueprint.skeleton != null && softbodyBlueprint.rootBone != null;
            if (GUI.Button(rect, hasRootNode ? softbodyBlueprint.rootBone.name : "None", EditorStyles.popup))
            {
                // create the menu and add items to it
                GenericMenu menu = new GenericMenu();
                menu.allowDuplicateNames = true;

                Queue<Transform> stack = new Queue<Transform>();
                stack.Enqueue(softbodyBlueprint.skeleton.transform);
                while (stack.Count != 0)
                {
                    var trfm = stack.Dequeue();
                    if (trfm != null)
                    {
                        menu.AddItem(new GUIContent(GetFullTransformName(trfm)),
                                     trfm == softbodyBlueprint.rootBone,
                                     OnParticleGroupSelected,
                                     trfm);

                        foreach (Transform child in trfm)
                            stack.Enqueue(child);
                    }
                }

                // display the menu
                menu.DropDown(rect);
            }

            EditorGUI.EndProperty();
            EditorGUI.indentLevel--;
            GUI.enabled = true;
        }

        private string GetFullTransformName(Transform trfm)
        {
            string fullName = trfm.name;
            while (trfm.parent != null)
            {
                trfm = trfm.parent;
                fullName = trfm.name + "/" + fullName;
            }
            return fullName;
        }

        private void OnParticleGroupSelected(object index)
        {
            Undo.RecordObject(softbodyBlueprint, "Set root bone");
            softbodyBlueprint.rootBone = index as Transform;
        }

        public override int VertexToParticle(int vertexIndex)
        {
            return softbodyBlueprint.vertexToParticle[vertexIndex];
        }

        public override void UpdateParticleVisibility()
        {
            if (sourceMesh != null && Camera.current != null)
            {
                int typeLength = softbodyBlueprint.particleType != null ? softbodyBlueprint.particleType.Count : 0;

                for (int i = 0; i < blueprint.particleCount; i++)
                {
                    Vector3 camToParticle = Camera.current.transform.position - blueprint.positions[i];
                    sqrDistanceToCamera[i] = camToParticle.sqrMagnitude;

                    if (i < typeLength)
                    {
                        if ((softbodyBlueprint.particleType[i] & particleTypeFlags) == 0)
                        {
                            visible[i] = false;
                            continue;
                        }
                        if (softbodyBlueprint.particleType[i] != ObiSoftbodySurfaceBlueprint.ParticleType.Surface)
                        {
                            visible[i] = true;
                            continue;
                        }
                    }

                    switch (particleCulling)
                    {
                        case ParticleCulling.Off:
                            visible[i] = true;
                            break;
                        case ParticleCulling.Back:
                            visible[i] = Vector3.Dot(blueprint.orientations[i] * Vector3.forward, camToParticle) > 0;
                            break;
                        case ParticleCulling.Front:
                            visible[i] = Vector3.Dot(blueprint.orientations[i] * Vector3.forward, camToParticle) <= 0;
                            break;
                    }
                }

                if ((renderModeFlags & 1) != 0)
                    Refresh();
            }

        }

        protected override void UpdateTintColor()
        {
            base.UpdateTintColor();

            int typeLength = softbodyBlueprint.particleType != null ? softbodyBlueprint.particleType.Count : 0;

            Color boneColor = new Color(0.8f, 0.5f, 0.5f, 1);
            Color volumeColor = new Color(0.8f, 0.7f, 0.7f, 1);
            Color surfaceColor = Color.white;

            for (int i = 0; i < blueprint.positions.Length; i++)
            {
                if (i < typeLength)
                {
                    switch(softbodyBlueprint.particleType[i])
                    {
                        case ObiSoftbodySurfaceBlueprint.ParticleType.Bone: tint[i] *= boneColor; break;
                        case ObiSoftbodySurfaceBlueprint.ParticleType.Surface: tint[i] *= surfaceColor; break;
                        case ObiSoftbodySurfaceBlueprint.ParticleType.Volume: tint[i] *= volumeColor; break;
                    }
                }
            }
        }

        public override void RenderModeSelector()
        {
            base.RenderModeSelector();

            if ((renderModeFlags & 1) != 0)
            {
                particleTypeFlags = (ObiSoftbodySurfaceBlueprint.ParticleType)EditorGUILayout.MaskField("Particle type", (int)particleTypeFlags, new string[]{ "Bone", "Volume", "Surface"});
            }
        }
    }


}
