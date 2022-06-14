using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.Linq;

namespace Obi
{
    [AddComponentMenu("Physics/Obi/Obi Softbody Skinner", 931)]
    [ExecuteInEditMode]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class ObiSoftbodySkinner : MonoBehaviour
    {

        public struct BoneWeightComparer : IComparer<BoneWeight1>
        {
            public int Compare(BoneWeight1 x, BoneWeight1 y)
            {
                return y.weight.CompareTo(x.weight);
            }
        }

        [Tooltip("Influence of the softbody in the resulting skin.")]
        [Range(0, 1)]
        public float softbodyInfluence = 1;
        [Tooltip("Maximum amount of bone influences for each vertex.")]
        [Range(byte.MinValue, byte.MaxValue)]
        public byte maxBonesPerVertex = 4;
        [Tooltip("The ratio at which the cluster's influence on a vertex falls off with distance.")]
        public float m_SkinningFalloff = 1.0f;
        [Tooltip("The maximum distance a cluster can be from a vertex before it will not influence it any more.")]
        public float m_SkinningMaxDistance = 0.5f;

        [HideInInspector] [SerializeField] private Mesh m_SoftMesh;
        [HideInInspector] [SerializeField] List<Matrix4x4> m_Bindposes = new List<Matrix4x4>();
        [HideInInspector] [SerializeField] ObiNativeBoneWeightList m_BoneWeights;
        [HideInInspector] [SerializeField] ObiNativeByteList m_BonesPerVertex;
        [HideInInspector] [SerializeField] public float[] m_softbodyInfluences;

        private SkinnedMeshRenderer m_Target;
        private Mesh m_OriginalMesh;
        private Mesh m_BakedMesh;
        private List<Transform> m_SoftBones = new List<Transform>();
        private List<ObiParticleAttachment> m_BoneAttachments = new List<ObiParticleAttachment>();

        [SerializeField] [HideInInspector] private ObiSoftbody m_Source;

        public ObiSoftbody Source
        {
            set
            {
                if (m_Source != value)
                {
                    if (m_Source != null)
                        m_Source.OnInterpolate -= UpdateSoftBones;

                    m_Source = value;

                    if (m_Source != null)
                        m_Source.OnInterpolate += UpdateSoftBones;
                }

            }
            get { return m_Source; }
        }

        public void Awake()
        {
            // autoinitialize "target" with the first skinned mesh renderer we find up our hierarchy.
            m_Target = GetComponent<SkinnedMeshRenderer>();
            InitializeInfluences();

            // autoinitialize "source" with the first softbody we find up our hierarchy.
            if (Source == null)
                Source = GetComponentInParent<ObiSoftbody>();
        }

        public void InitializeInfluences()
        {
            if (m_Target != null && m_Target.sharedMesh != null)
            {
                if (m_softbodyInfluences == null || m_softbodyInfluences.Length != m_Target.sharedMesh.vertexCount)
                {   
                    m_softbodyInfluences = new float[m_Target.sharedMesh.vertexCount];
                    for (int i = 0; i < m_softbodyInfluences.Length; ++i)
                        m_softbodyInfluences[i] = 1;
                }
            }
        }

        public void OnDestroy()
        {
            DestroyImmediate(m_BakedMesh);
            m_BoneWeights.Dispose();
            m_BonesPerVertex.Dispose();
        }

        public void OnEnable()
        {
            if (Source != null)
                Source.OnInterpolate += UpdateSoftBones;

            if (Application.isPlaying)
                AttachParticlesToSkeleton();
        }

        public void OnDisable()
        {
            if (m_Source != null)
                m_Source.OnInterpolate -= UpdateSoftBones;

            if (m_OriginalMesh != null)
                m_Target.sharedMesh = m_OriginalMesh;

            if (m_SoftMesh)
                DestroyImmediate(m_SoftMesh);

            RemoveSoftBones();

            if (Application.isPlaying)
                DetachParticlesFromSkeleton();
        }

        private void Setup()
        {
            if (Application.isPlaying)
            {
                // Setup the mesh from scratch, in case it is not a clone of an already setup mesh.
                if (m_SoftMesh == null)
                {
                    // Create a copy of the original mesh:
                    m_SoftMesh = Instantiate(m_Target.sharedMesh);

                    SetBoneWeights();
                    AppendBindposes();
                    AppendSoftBones();
                }
                // Reuse the same mesh, just copy bone references as we need to update bones every frame.
                else
                {
                    CopySoftBones();
                }

                // Set the new mesh:
                m_SoftMesh.RecalculateBounds();
                m_OriginalMesh = m_Target.sharedMesh;
                m_Target.sharedMesh = m_SoftMesh;

                // Recalculate bounds:
                m_Target.localBounds = m_SoftMesh.bounds;
                m_Target.rootBone = m_Source.transform;
            }
        }

        private void AttachParticlesToSkeleton()
        {
            if (m_Source != null)
            {
                Queue<Transform> queue = new Queue<Transform>();
                queue.Enqueue(m_Target.rootBone);

                while (queue.Count > 0)
                {
                    var bone = queue.Dequeue();

                    if (bone != null)
                    {
                        foreach (var group in m_Source.blueprint.groups)
                        {
                            if (group.name == bone.name)
                            {
                                var attach = m_Source.gameObject.AddComponent<ObiParticleAttachment>();
                                attach.particleGroup = group;
                                attach.target = bone;
                                attach.constrainOrientation = true;
                                attach.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
                                m_BoneAttachments.Add(attach);
                            }
                        }

                        foreach (Transform child in bone)
                            queue.Enqueue(child);
                    }
                }
            }
        }

        private void DetachParticlesFromSkeleton()
        {
            for(int i = 0; i < m_BoneAttachments.Count; ++i)
                Destroy(m_BoneAttachments[i]);
            m_BoneAttachments.Clear();
        }

        private void UpdateSoftBones(ObiActor actor)
        {
            if (m_SoftBones.Count > 0)
	        {
                if (m_Source.solver != null)
                {
                    int boneIndex = 0;
                    var constraints = m_Source.softbodyBlueprint.GetConstraintsByType(Oni.ConstraintType.ShapeMatching) as ObiConstraints<ObiShapeMatchingConstraintsBatch>;
                    for (int j = 0; j < constraints.GetBatchCount(); ++j)
                    {
                        var batch = constraints.GetBatch(j) as ObiShapeMatchingConstraintsBatch;
                        for (int i = 0; i < batch.activeConstraintCount; ++i, ++boneIndex)
                        {
                            // the first particle index in each shape is the "center" particle.
                            int shapeIndex = batch.particleIndices[batch.firstIndex[i]];
                            m_SoftBones[boneIndex].position = m_Source.GetParticlePosition(m_Source.solverIndices[shapeIndex]);
                            m_SoftBones[boneIndex].rotation = m_Source.GetParticleOrientation(m_Source.solverIndices[shapeIndex]);
                        }
                    }
                }
	        }
            else
                Setup();
        }

        public IEnumerator BindSkin()
        {
            if (m_Source == null || m_Source.softbodyBlueprint == null || m_Target.sharedMesh == null){
				yield break;
			}

            InitializeInfluences();

            if (m_BoneWeights == null)
                m_BoneWeights = new ObiNativeBoneWeightList();
            if (m_BonesPerVertex == null)
                m_BonesPerVertex = new ObiNativeByteList();

            // get the amount of active shape matching clusters, and prepare a list to store their positions:
            var constraints = m_Source.softbodyBlueprint.GetConstraintsByType(Oni.ConstraintType.ShapeMatching) as ObiConstraints<ObiShapeMatchingConstraintsBatch>;
            var clusterCount = constraints.GetActiveConstraintCount();
            var clusterCenters = new List<Vector3>(clusterCount);

            // bake skinned mesh:
            if (m_BakedMesh == null)
                m_BakedMesh = new Mesh();
            m_Target.BakeMesh(m_BakedMesh);

            // get the amount of vertices and bones per vertex.
            Vector3[] vertices = m_BakedMesh.vertices;
            var bindPoses = m_Target.sharedMesh.bindposes;
            var bonesPerVertex = m_Target.sharedMesh.GetBonesPerVertex();
            var boneWeights = m_Target.sharedMesh.GetAllBoneWeights();
            var bonesOffset = bindPoses.Length;

            // prepare lists to store new bindposes and weights:
            m_Bindposes.Clear();
            m_Bindposes.Capacity = clusterCount;
            m_BonesPerVertex.Clear();
            m_BoneWeights.Clear();

            // Calculate softbody local to world matrix, and target to world matrix.
            Matrix4x4 source2w = m_Source.transform.localToWorldMatrix;
            Quaternion source2wRot = source2w.rotation;
            Matrix4x4 target2w = transform.localToWorldMatrix;

            for (int j = 0; j < constraints.GetBatchCount(); ++j)
            {
                var batch = constraints.GetBatch(j) as ObiShapeMatchingConstraintsBatch;

                // Create bind pose matrices, one per shape matching cluster:
                for (int i = 0; i < batch.activeConstraintCount; ++i)
                {
                    int shapeIndex = batch.particleIndices[batch.firstIndex[i]];

                    // world space bone center/orientation:
                    Vector3 clusterCenter = source2w.MultiplyPoint3x4(m_Source.softbodyBlueprint.restPositions[shapeIndex]);
                    Quaternion clusterOrientation = source2wRot * m_Source.softbodyBlueprint.restOrientations[shapeIndex];

                    clusterCenters.Add(clusterCenter);

                    // world space bone transform * object local to world.
                    m_Bindposes.Add(Matrix4x4.TRS(clusterCenter, clusterOrientation, Vector3.one).inverse * target2w);

                    yield return new CoroutineJob.ProgressInfo("ObiSoftbody: calculating bind poses...", i / (float)batch.activeConstraintCount);
                }
            }

            BoneWeightComparer comparer = new BoneWeightComparer();

            // Calculate skin weights and bone indices:
            int originalBoneWeightOffset = 0;
            int newBoneWeightOffset = 0;
            for (var j = 0; j < vertices.Length; j++)
            {
                var originalBoneCount = j < bonesPerVertex.Length ? bonesPerVertex[j] : byte.MinValue;
                var vertexPosition = target2w.MultiplyPoint3x4(vertices[j]);
                var softInfluence = softbodyInfluence * m_softbodyInfluences[j];

                // calculate and append new weights:
                byte newBoneCount = 0;
                for (int i = 0; i < clusterCenters.Count; ++i)
                {
                    float distance = Vector3.Distance(vertexPosition, clusterCenters[i]);

                    if (distance <= m_SkinningMaxDistance)
                    {
                        var boneWeight = new BoneWeight1();

                        boneWeight.boneIndex = bonesOffset + i;
                        boneWeight.weight = distance > 0 ? m_SkinningMaxDistance / distance : 100;
                        boneWeight.weight = Mathf.Pow(boneWeight.weight, m_SkinningFalloff);
                        m_BoneWeights.Add(boneWeight);
                        newBoneCount++;
                    }
                }

                // normalize new weights only:
                NormalizeWeights(newBoneWeightOffset, newBoneCount);

                // scale new weights:
                for (int i = 0; i < newBoneCount; ++i)
                {
                    var boneWeight = m_BoneWeights[newBoneWeightOffset + i];
                    boneWeight.weight *= softInfluence;
                    m_BoneWeights[newBoneWeightOffset + i] = boneWeight;
                }

                // scale existing weights:
                for (int i = 0; i < originalBoneCount; ++i)
                {
                    var boneWeight = boneWeights[originalBoneWeightOffset + i];
                    if (newBoneCount > 0)
                        boneWeight.weight *= 1 - softInfluence;
                    m_BoneWeights.Add(boneWeight);
                }

                originalBoneWeightOffset += originalBoneCount;

                // calculate total bone count for this vertex (original bones + new bones):
                byte totalBoneCount = (byte)(originalBoneCount + newBoneCount);

                // renormalize all weights:
                NormalizeWeights(newBoneWeightOffset, totalBoneCount);

                // Sort bones by decreasing weight:
                var slice = m_BoneWeights.AsNativeArray<BoneWeight1>().Slice(newBoneWeightOffset, totalBoneCount);
                #if OBI_COLLECTIONS
                    slice.Sort(comparer);
                #else
                var sorted = slice.OrderByDescending(x => x.weight).ToList();
                for (int i = 0; i < totalBoneCount; ++i)
                    m_BoneWeights[newBoneWeightOffset + i] = sorted[i];
                #endif

                // Limit the amount of bone  influences:
                totalBoneCount = (byte)Mathf.Min(totalBoneCount, maxBonesPerVertex);
                m_BoneWeights.RemoveRange(newBoneWeightOffset + totalBoneCount, m_BoneWeights.count - (newBoneWeightOffset + totalBoneCount));

                // Renormalize all weights:
                NormalizeWeights(newBoneWeightOffset, totalBoneCount);

                // Append total bone count
                m_BonesPerVertex.Add(totalBoneCount);
                newBoneWeightOffset += totalBoneCount;

                yield return new CoroutineJob.ProgressInfo("ObiSoftbody: calculating bone weights...", j / (float)vertices.Length);
            }
        }

        private void NormalizeWeights(int offset, int count)
        {
            float weightSum = 0;
            for (int i = 0; i < count; ++i)
                weightSum += m_BoneWeights[offset + i].weight;

            if (weightSum > 0)
            {
                for (int i = 0; i < count; ++i)
                {
                    var boneWeight = m_BoneWeights[offset + i];
                    boneWeight.weight /= weightSum;
                    m_BoneWeights[offset + i] = boneWeight;
                }
            }
        }

        private void SetBoneWeights()
        {
            if (m_BoneWeights != null && m_BoneWeights.count > 0)
                m_SoftMesh.SetBoneWeights(m_BonesPerVertex.AsNativeArray<byte>(), m_BoneWeights.AsNativeArray<BoneWeight1>());
        }

        private void AppendBindposes()
        {
            List<Matrix4x4> bindposes = new List<Matrix4x4>(m_SoftMesh.bindposes);
            bindposes.AddRange(m_Bindposes);
            m_SoftMesh.bindposes = bindposes.ToArray();
        }

        private void AppendSoftBones()
        {
            // Calculate softbody local to world matrix, and target to world matrix.
            Matrix4x4 source2w = m_Source.transform.localToWorldMatrix;
            Quaternion source2wRot = source2w.rotation;

            m_SoftBones.Clear();

            var constraints = m_Source.softbodyBlueprint.GetConstraintsByType(Oni.ConstraintType.ShapeMatching) as ObiConstraints<ObiShapeMatchingConstraintsBatch>;
            for (int j = 0; j < constraints.GetBatchCount(); ++j)
            {
                var batch = constraints.GetBatch(j) as ObiShapeMatchingConstraintsBatch;
                for (int i = 0; i < batch.activeConstraintCount; ++i)
                {
                    int shapeIndex = batch.particleIndices[batch.firstIndex[i]];

                    GameObject bone = new GameObject("Cluster" + i);
                    bone.transform.parent = transform;
                    bone.transform.position = source2w.MultiplyPoint3x4(m_Source.softbodyBlueprint.restPositions[shapeIndex]);
                    bone.transform.rotation = source2wRot * m_Source.softbodyBlueprint.restOrientations[shapeIndex];
                    bone.hideFlags = HideFlags.HideAndDontSave;
                    m_SoftBones.Add(bone.transform);
                }
            }

            // append our bone list to the original one:
            var bones = new List<Transform>(m_Target.bones);
            bones.AddRange(m_SoftBones);
            m_Target.bones = bones.ToArray();
        }

        // Copies existing softbones from the skinned mesh renderer. Useful when reusing an existing mesh instead of creating an instance.
        private void CopySoftBones()
        {
            var constraints = m_Source.softbodyBlueprint.GetConstraintsByType(Oni.ConstraintType.ShapeMatching) as ObiConstraints<ObiShapeMatchingConstraintsBatch>;
            int boneCount = constraints.GetActiveConstraintCount();

            m_SoftBones.Clear();
            m_SoftBones.Capacity = boneCount;

            for (int i = m_Target.bones.Length - boneCount; i < m_Target.bones.Length; ++i)
                m_SoftBones.Add(m_Target.bones[i]);
        }

        private void RemoveSoftBones()
        {
            if (m_Target != null)
            {
                var bones = m_Target.bones;
                Array.Resize(ref bones, bones.Length - m_SoftBones.Count);
                m_Target.bones = bones;
            }

            foreach (Transform t in m_SoftBones)
                if (t) Destroy(t.gameObject);

            m_SoftBones.Clear();
        }

    }
}
