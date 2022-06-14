using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Obi
{

    [CreateAssetMenu(fileName = "softbody surface blueprint", menuName = "Obi/Softbody Surface Blueprint", order = 160)]
    public class ObiSoftbodySurfaceBlueprint : ObiSoftbodyBlueprintBase
    {
        [Flags]
        public enum ParticleType
        {
            None = 0,
            Bone = 1 << 0,
            Volume = 1 << 1,
            Surface = 1 << 2,
            All = Bone | Volume | Surface
        }

        [Flags]
        public enum VoxelConnectivity
        {
            None = 0,
            Faces = 1 << 0,
            Edges = 1 << 1,
            Vertices = 1 << 2,
            All = Faces | Edges | Vertices
        }

        public enum SurfaceSamplingMode
        {
            None,
            Vertices,
            Voxels
        }

        public enum VolumeSamplingMode
        {
            None,
            Voxels
        }

        public enum SamplingMode
        {
            Surface,
            Volume,
            Full
        }

        [Tooltip("Method used to distribute particles on the surface of the mesh.")]
        public SurfaceSamplingMode surfaceSamplingMode = SurfaceSamplingMode.Voxels;

        [Tooltip("Resolution of the surface particle distribution.")]
        [Range(2, 128)]
        public int surfaceResolution = 16;

        [Tooltip("Method used to distribute particles on the volume of the mesh.")]
        public VolumeSamplingMode volumeSamplingMode = VolumeSamplingMode.None;

        [Tooltip("Resolution of the volume particle distribution.")]
        [Range(2, 128)]
        public int volumeResolution = 16;

        [Tooltip("GameObject that contains the skeleton to sample.")]
        public GameObject skeleton;
        [Tooltip("Root bone of the skeleton.")]
        public Transform rootBone; /**< root bone used for skeleton particles.*/
        [Tooltip("Optional rotation applied to the skeleton.")]
        public Quaternion boneRotation;

        [Tooltip("Maximum aspect ratio allowed for particles. High values will allow particles to deform more to better fit their neighborhood.")]
        [Range(1, 5)]
        public float maxAnisotropy = 3;      /**< Maximum particle anisotropy. High values will allow particles to deform to better fit their neighborhood.*/

        [Range(0, 1)]
        [Tooltip("Amount of smoothing applied to particle positions.")]
        public float smoothing = 0.25f;

        [Tooltip("Voxel resolution used to analyze the shape of the mesh.")]
        [Range(2, 128)]
        public int shapeResolution = 48;

        [HideInInspector] public Mesh generatedMesh = null;
        [HideInInspector] public int[] vertexToParticle = null;
        [HideInInspector] public List<ParticleType> particleType = null;

        public struct ParticleToSurface
        {
            public int particleIndex;
            public float distance;

            public ParticleToSurface(int particleIndex, float distance)
            {
                this.particleIndex = particleIndex;
                this.distance = distance;
            }
        }

        public const float DEFAULT_PARTICLE_MASS = 0.1f;
        private Matrix4x4 blueprintTransform;

        private VoxelDistanceField m_DistanceField;
        private VoxelPathFinder m_PathFinder;
        private List<int>[] voxelToParticles;

        private List<int> particleIndices = new List<int>();
        private List<int> constraintIndices = new List<int>();

        public MeshVoxelizer surfaceVoxelizer { get; private set; }
        public MeshVoxelizer volumeVoxelizer  { get; private set; }
        public MeshVoxelizer shapeVoxelizer { get; private set; }

        protected override IEnumerator Initialize()
        {
            if (inputMesh == null || !inputMesh.isReadable)
            {
                Debug.LogError("The input mesh is null, or not readable.");
                yield break;
            }

            ClearParticleGroups();

            // Prepare candidate particle arrays: 
            List<Vector3> particlePositions = new List<Vector3>();
            List<Vector3> particleNormals = new List<Vector3>();

            // Prepare constraint arrays:
            particleIndices = new List<int>();
            constraintIndices = new List<int>();

            // Transform mesh data:
            blueprintTransform = Matrix4x4.TRS(Vector3.zero, rotation, scale);
            Vector3[] vertices = inputMesh.vertices;
            Vector3[] normals = inputMesh.normals;
            int[] tris = inputMesh.triangles;
            Bounds transformedBounds = new Bounds();

            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = blueprintTransform.MultiplyPoint3x4(vertices[i]);
                transformedBounds.Encapsulate(vertices[i]);
            }

            for (int i = 0; i < normals.Length; ++i)
                normals[i] = Vector3.Normalize(blueprintTransform.MultiplyVector(normals[i]));

            // initialize arrays:
            particleType = new List<ParticleType>();

            //voxelize for cluster placement:
            var voxelize = VoxelizeForShapeAnalysis(transformedBounds.size);
            while (voxelize.MoveNext()) yield return voxelize.Current;

            //voxelize for surface: TODO: do cheaper surface voxelization, no fill.
            voxelize = VoxelizeForSurfaceSampling(transformedBounds.size);
            while (voxelize.MoveNext()) yield return voxelize.Current;

            if (surfaceSamplingMode == SurfaceSamplingMode.Voxels)
            { 
                var surface = VoxelSampling(surfaceVoxelizer, particlePositions, particleNormals, MeshVoxelizer.Voxel.Boundary, ParticleType.Surface);
                while (surface.MoveNext()) yield return surface.Current;

                IEnumerator ip = InsertParticlesIntoVoxels(surfaceVoxelizer, particlePositions);
                while (ip.MoveNext()) yield return ip.Current;

                IEnumerator vc = CreateClustersFromVoxels(surfaceVoxelizer, particlePositions, VoxelConnectivity.Faces | VoxelConnectivity.Edges, new List<ParticleType>(){ParticleType.Surface});
                while (vc.MoveNext()) yield return vc.Current;

                for (int i = 0; i < particlePositions.Count; ++i)
                    particlePositions[i] = ProjectOnMesh(particlePositions[i], vertices, tris);

            }
            else if (surfaceSamplingMode == SurfaceSamplingMode.Vertices)
            {
                var sv = VertexSampling(vertices, particlePositions);
                while (sv.MoveNext()) yield return sv.Current;

                var mp = MapVerticesToParticles(vertices, particlePositions);
                while (mp.MoveNext()) yield return mp.Current;

                var ss = SurfaceMeshShapeMatchingConstraints(particlePositions,tris);
                while (ss.MoveNext()) yield return ss.Current;
            }

            if (volumeSamplingMode == VolumeSamplingMode.Voxels)
            {
                // sample the volume of the mesh:
                voxelize = VoxelizeForVolumeSampling(transformedBounds.size);
                while (voxelize.MoveNext()) yield return voxelize.Current;

                var volume = VoxelSampling(volumeVoxelizer, particlePositions, particleNormals, MeshVoxelizer.Voxel.Inside, ParticleType.Volume);
                while (volume.MoveNext()) yield return volume.Current;

                var ip = InsertParticlesIntoVoxels(volumeVoxelizer, particlePositions);
                while (ip.MoveNext()) yield return ip.Current;

                var vc = CreateClustersFromVoxels(volumeVoxelizer, particlePositions, VoxelConnectivity.Faces, new List<ParticleType>() { ParticleType.Volume });
                while (vc.MoveNext()) yield return vc.Current;

                vc = CreateClustersFromVoxels(volumeVoxelizer, particlePositions, VoxelConnectivity.All, new List<ParticleType>() { ParticleType.Volume | ParticleType.Surface });
                while (vc.MoveNext()) yield return vc.Current;
            }

            // sample skeleton:
            var sk = SkeletonSampling(transformedBounds.size, particlePositions, particleNormals);
            while (sk.MoveNext()) yield return sk.Current;

            // create skeleton clusters:
            if (skeleton != null)
            {
                var sc = CreateClustersFromSkeleton(particlePositions);
                while (sc.MoveNext()) yield return sc.Current;
            }

            // generate particles:
            var generate = GenerateParticles(normals, particlePositions, particleNormals);
            while (generate.MoveNext()) yield return generate.Current;

            // generate shape matching constraints:
            IEnumerator bc = CreateShapeMatchingConstraints(particlePositions);
            while (bc.MoveNext()) yield return bc.Current;

            generatedMesh = inputMesh;

        }

        private Vector3 ProjectOnMesh(Vector3 point, Vector3[] vertices, int[] tris)
        {
            Vector3 triProjection;
            Vector3 meshProjection = point;
            float min = float.MaxValue;

            var voxel = surfaceVoxelizer.GetPointVoxel(point) - surfaceVoxelizer.Origin;
            var triangleIndices = surfaceVoxelizer.GetTrianglesOverlappingVoxel(surfaceVoxelizer.GetVoxelIndex(voxel.x,voxel.y,voxel.z));

            if (triangleIndices != null)
            {
                foreach (int i in triangleIndices)
                {
                    ObiUtils.NearestPointOnTri(vertices[tris[i * 3]], vertices[tris[i * 3 + 1]], vertices[tris[i * 3 + 2]], point, out triProjection);
                    float dist = Vector3.SqrMagnitude(triProjection - point);
                    if (dist < min)
                    {
                        min = dist;
                        meshProjection = triProjection;
                    }
                }
            }
            return meshProjection;
        }

        private IEnumerator VoxelizeForShapeAnalysis(Vector3 boundsSize)
        {
            // Calculate voxel size: 
            float longestSide = Mathf.Max(Mathf.Max(boundsSize.x, boundsSize.y), boundsSize.z);
            float size = longestSide / shapeResolution;

            // Voxelize mesh and calculate discrete distance field:
            shapeVoxelizer = new MeshVoxelizer(inputMesh, size);
            var voxelizeCoroutine = shapeVoxelizer.Voxelize(blueprintTransform);
            while (voxelizeCoroutine.MoveNext())
                yield return voxelizeCoroutine.Current;

            shapeVoxelizer.BoundaryThinning();

            // Generate distance field:
            m_DistanceField = new VoxelDistanceField(shapeVoxelizer);
            var dfCoroutine = m_DistanceField.JumpFlood();
            while (dfCoroutine.MoveNext())
                yield return dfCoroutine.Current;

            // Create path finder:
            m_PathFinder = new VoxelPathFinder(shapeVoxelizer);
        }

        private IEnumerator VoxelizeForSurfaceSampling(Vector3 boundsSize)
        {
            float longestSide = Mathf.Max(Mathf.Max(boundsSize.x, boundsSize.y), boundsSize.z);
            float size = longestSide / surfaceResolution;

            surfaceVoxelizer = new MeshVoxelizer(inputMesh, size);
            var voxelizeCoroutine = surfaceVoxelizer.Voxelize(blueprintTransform, true);
            while (voxelizeCoroutine.MoveNext())
                yield return voxelizeCoroutine.Current;

            surfaceVoxelizer.BoundaryThinning();
        }

        private IEnumerator VoxelizeForVolumeSampling(Vector3 boundsSize)
        {
            float longestSide = Mathf.Max(Mathf.Max(boundsSize.x, boundsSize.y), boundsSize.z);
            float size = longestSide / volumeResolution;

            volumeVoxelizer = new MeshVoxelizer(inputMesh, size);
            var voxelizeCoroutine = volumeVoxelizer.Voxelize(blueprintTransform, true);
            while (voxelizeCoroutine.MoveNext())
                yield return voxelizeCoroutine.Current;

            volumeVoxelizer.BoundaryThinning();
        }

        private IEnumerator InsertParticlesIntoVoxels(MeshVoxelizer voxelizer, List<Vector3> particles)
        {
            voxelToParticles = new List<int>[voxelizer.voxelCount];
            for (int i = 0; i < voxelToParticles.Length; ++i)
                voxelToParticles[i] = new List<int>(4);

            for (int i = 0; i < particles.Count ; ++i)
            {
                Vector3Int voxel = voxelizer.GetPointVoxel(particles[i]) - voxelizer.Origin;
                int index = voxelizer.GetVoxelIndex(voxel.x, voxel.y, voxel.z);

                voxelToParticles[index].Add(i);

                if (i % 100 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiSoftbody: inserting particles into voxels...", i / (float)particles.Count);
            }
        }

        private IEnumerator VertexSampling(Vector3[] vertices, List<Vector3> particlePositions)
        {
            float particleRadius = ObiUtils.sqrt3 * 0.5f * surfaceVoxelizer.voxelSize;

            for (int i = 0; i < vertices.Length; ++i)
            {
                bool valid = true;
                for (int j = 0; j < particlePositions.Count; ++j)
                {
                    if (Vector3.Distance(vertices[i], particlePositions[j]) < particleRadius * 1.2f)
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                {
                    particlePositions.Add(vertices[i]);
                    particleType.Add(ParticleType.Surface);
                }

                if (i % 100 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiSoftbody: sampling surface...", i / (float)vertices.Length);
            }
        }

        private IEnumerator VoxelSampling(MeshVoxelizer voxelizer, List<Vector3> particles, List<Vector3> normals, MeshVoxelizer.Voxel voxelType, ParticleType pType)
        {
            int i = 0;

            for (int x = 0; x < voxelizer.resolution.x; ++x)
                for (int y = 0; y < voxelizer.resolution.y; ++y)
                    for (int z = 0; z < voxelizer.resolution.z; ++z)
                    {
                        if ((voxelizer[x, y, z] & voxelType) != 0)
                        {
                            var voxel = new Vector3Int(x, y, z);
                            Vector3 voxelCenter = voxelizer.GetVoxelCenter(voxel); 

                            particles.Add(voxelCenter);
                            //normals.Add(m_DistanceField.SampleFiltered(voxelCenter.x,voxelCenter.y,voxelCenter.z)); TODO: get normals?
                            particleType.Add(pType);
                        }
                        if (++i % 1000 == 0)
                            yield return new CoroutineJob.ProgressInfo("ObiSoftbody: sampling voxels...", i / (float)voxelizer.voxelCount);
                    }
        }

        // Samples a skeleton.
        private IEnumerator SkeletonSampling(Vector3 boundsSize, List<Vector3> particles, List<Vector3> normals)
        {
            float longestSide = Mathf.Max(Mathf.Max(boundsSize.x, boundsSize.y), boundsSize.z);
            float size = longestSide / (volumeSamplingMode != VolumeSamplingMode.None ? volumeResolution : surfaceResolution);

            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(rootBone);

            while (queue.Count > 0)
            {
                var bone = queue.Dequeue();

                if (bone != null)
                {
                    // create a new particle group for each bone:
                    var group = AppendNewParticleGroup(bone.name, false);

                    group.particleIndices.Add(particles.Count);
                    particles.Add(boneRotation * bone.position);
                    normals.Add(Vector3.forward);
                    particleType.Add(ParticleType.Bone);

                    foreach (Transform child in bone)
                    {
                        Vector3 boneDir = child.position - bone.position;
                        float boneLength = boneDir.magnitude;
                        boneDir.Normalize();

                        int particlesInBone = 1 + Mathf.FloorToInt(boneLength / size);
                        float distance = boneLength / particlesInBone;

                        for (int i = 1; i < particlesInBone; ++i)
                        {
                            group.particleIndices.Add(particles.Count);
                            particles.Add(boneRotation * (bone.position + boneDir * distance * i));
                            normals.Add(Vector3.forward);
                            particleType.Add(ParticleType.Bone);
                        }

                        queue.Enqueue(child);
                    }

                    yield return new CoroutineJob.ProgressInfo("ObiSoftbody: sampling skeleton...", 1);
                }
            }
        }

        private IEnumerator MapVerticesToParticles(Vector3[] vertices, List<Vector3> particlePositions)
        {
            vertexToParticle = new int[vertices.Length];

            // Find out the closest particle to each vertex:
            for (int i = 0; i < vertices.Length; ++i)
            {
                float minDistance = float.MaxValue;
                for (int j = 0; j < particlePositions.Count; ++j)
                {
                    float distance = Vector3.SqrMagnitude(vertices[i] - particlePositions[j]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        vertexToParticle[i] = j;
                    }
                }

                if (i % 100 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiSoftbody: mapping vertices to particles...", i / (float)vertices.Length);
            }
        }

        private IEnumerator GenerateParticles(Vector3[] inputNormals, List<Vector3> particlePositions, List<Vector3> particleNormals)
        {
            float particleRadius = ObiUtils.sqrt3 * 0.5f * surfaceVoxelizer.voxelSize;

            positions = new Vector3[particlePositions.Count];
            orientations = new Quaternion[particlePositions.Count];
            restPositions = new Vector4[particlePositions.Count];
            restOrientations = new Quaternion[particlePositions.Count];
            velocities = new Vector3[particlePositions.Count];
            angularVelocities = new Vector3[particlePositions.Count];
            invMasses = new float[particlePositions.Count];
            invRotationalMasses = new float[particlePositions.Count];
            principalRadii = new Vector3[particlePositions.Count];
            filters = new int[particlePositions.Count];
            colors = new Color[particlePositions.Count];

            m_ActiveParticleCount = particlePositions.Count;

            for (int i = 0; i < particlePositions.Count; ++i)
            {
                // Perform ellipsoid fitting:
                List<Vector3> neighbourVertices = new List<Vector3>();

                Vector3 centroid = particlePositions[i];
                Quaternion orientation = Quaternion.identity;
                Vector3 principalValues = Vector3.one * particleRadius;

                // Calculate high-def voxel neighborhood extents:
                var anisotropyNeighborhood = Vector3.one * shapeVoxelizer.voxelSize * ObiUtils.sqrt3 * 2;
                Vector3Int min = shapeVoxelizer.GetPointVoxel(centroid - anisotropyNeighborhood) - shapeVoxelizer.Origin;
                Vector3Int max = shapeVoxelizer.GetPointVoxel(centroid + anisotropyNeighborhood) - shapeVoxelizer.Origin;
                        
                for (int nx = min.x; nx <= max.x; ++nx)
                    for (int ny = min.y; ny <= max.y; ++ny)
                        for (int nz = min.z; nz <= max.z; ++nz)
                        {
                            if (shapeVoxelizer.VoxelExists(nx,ny,nz) &&
                                shapeVoxelizer[nx, ny, nz] != MeshVoxelizer.Voxel.Outside)
                            {
                                Vector3 voxelCenter = shapeVoxelizer.GetVoxelCenter(new Vector3Int(nx, ny, nz));
                                neighbourVertices.Add(voxelCenter);
                            }
                        }

                Vector3 dfnormal = m_DistanceField.SampleFiltered(centroid.x, centroid.y, centroid.z);

                // if the particle has a non-empty neighborhood, perform ellipsoidal fitting:
                if (neighbourVertices.Count > 0)
                    ObiUtils.GetPointCloudAnisotropy(neighbourVertices, maxAnisotropy, particleRadius, dfnormal, ref centroid, ref orientation, ref principalValues);

                invRotationalMasses[i] = invMasses[i] = 1.0f;
                positions[i] = Vector3.Lerp(particlePositions[i], centroid, smoothing);
                restPositions[i] = positions[i];
                restPositions[i][3] = 1; // activate rest position.
                orientations[i] = orientation;
                restOrientations[i] = orientation;
                principalRadii[i] = principalValues;
                filters[i] = ObiUtils.MakeFilter(ObiUtils.CollideWithEverything, 1);
                colors[i] = Color.white;

                if (i % 100 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiSoftbody: generating particles...", i / (float)particlePositions.Count);
            }
        }

        protected override void SwapWithFirstInactiveParticle(int index)
        {
            base.SwapWithFirstInactiveParticle(index);

            particleType.Swap(index, m_ActiveParticleCount);

            // Keep vertexToParticle map in sync:
            for (int i = 0; i < vertexToParticle.Length; ++i)
            {
                if (vertexToParticle[i] == index)
                    vertexToParticle[i] = m_ActiveParticleCount;
                else if (vertexToParticle[i] == m_ActiveParticleCount)
                    vertexToParticle[i] = index;
            }
        }

        protected void ConnectToNeighborParticles(MeshVoxelizer voxelizer, int particle, List<Vector3> particles, List<ParticleType> allowed, int x, int y, int z, Vector3Int[] neighborhood, float clusterSize)
        {
            Vector3Int startVoxel = shapeVoxelizer.GetPointVoxel(particles[particle]) - shapeVoxelizer.Origin;
            startVoxel = m_PathFinder.FindClosestNonEmptyVoxel(startVoxel).coordinates;

            for (int j = 0; j < neighborhood.Length; ++j)
            {
                var voxel = neighborhood[j];
                int index = voxelizer.GetVoxelIndex(x + voxel.x, y + voxel.y, z + voxel.z);

                foreach (var neighbor in voxelToParticles[index])
                {
                    if (!allowed.Contains(particleType[particle] | particleType[neighbor]))
                        continue;

                    Vector3Int endVoxel = shapeVoxelizer.GetPointVoxel(particles[neighbor]) - shapeVoxelizer.Origin;
                    endVoxel = m_PathFinder.FindClosestNonEmptyVoxel(endVoxel).coordinates;

                    var path = m_PathFinder.FindPath(startVoxel, endVoxel);
                    float geodesicDistance = path.distance;

                    if (geodesicDistance <= clusterSize)
                        particleIndices.Add(neighbor);
                }
            }
        }

        protected IEnumerator CreateClustersFromVoxels(MeshVoxelizer voxelizer, List<Vector3> particles, VoxelConnectivity connectivity, List<ParticleType> allowed)
        {
            float clusterSize = ObiUtils.sqrt3 * voxelizer.voxelSize * 1.5f;

            for (int i = 0; i < particles.Count; ++i)
            {
                Vector3Int voxel = voxelizer.GetPointVoxel(particles[i]) - voxelizer.Origin;

                constraintIndices.Add(particleIndices.Count);
                particleIndices.Add(i);

                if ((connectivity & VoxelConnectivity.Faces) != 0)
                    ConnectToNeighborParticles(voxelizer, i, particles, allowed, voxel.x, voxel.y, voxel.z, MeshVoxelizer.faceNeighborhood, clusterSize);

                if ((connectivity & VoxelConnectivity.Edges) != 0)
                    ConnectToNeighborParticles(voxelizer, i, particles, allowed, voxel.x, voxel.y, voxel.z, MeshVoxelizer.edgeNeighborhood, clusterSize);

                if ((connectivity & VoxelConnectivity.Vertices) != 0)
                    ConnectToNeighborParticles(voxelizer, i, particles, allowed, voxel.x, voxel.y, voxel.z, MeshVoxelizer.vertexNeighborhood, clusterSize);

                if (i % 100 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiSoftbody: generating shape matching clusters...", i / (float)voxelizer.voxelCount);
            }
        }

        protected IEnumerator CreateClustersFromSkeleton(List<Vector3> particles)
        {
            if (volumeVoxelizer == null && surfaceVoxelizer == null)
                yield break;

            float voxelSize = volumeVoxelizer != null ? volumeVoxelizer.voxelSize : surfaceVoxelizer.voxelSize;
            float clusterSize = ObiUtils.sqrt3 * voxelSize * 1.5f;

            // skeleton particles
            for (int i = 0; i < particles.Count; ++i)
            {
                if (particleType[i] == ParticleType.Bone)
                {
                    constraintIndices.Add(particleIndices.Count);
                    particleIndices.Add(i);

                    for (int j = 0; j < particles.Count; ++j)
                    {
                        if (particleType[j] != ParticleType.Bone && Vector3.Distance(particles[j], particles[i]) <= clusterSize * 0.5f)
                        {
                            particleIndices.Add(j);
                        }
                    }
                }

                if (i % 100 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiSoftbody: generating shape matching clusters...", i / (float)particles.Count);
            }
        }

        protected IEnumerator SurfaceMeshShapeMatchingConstraints(List<Vector3> particles, int[] triangles)
        {
            HashSet<int>[] connections = new HashSet<int>[particles.Count];
            for (int i = 0; i < connections.Length; ++i)
                connections[i] = new HashSet<int>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int p1 = vertexToParticle[triangles[i]];
                int p2 = vertexToParticle[triangles[i + 1]];
                int p3 = vertexToParticle[triangles[i + 2]];

                if (p1 != p2)
                {
                    connections[p1].Add(p2);
                    connections[p2].Add(p1);
                }

                if (p1 != p3)
                {
                    connections[p1].Add(p3);
                    connections[p3].Add(p1);
                }

                if (p2 != p3)
                {
                    connections[p2].Add(p3);
                    connections[p3].Add(p2);
                }

                if (i % 100 == 0)
                    yield return new CoroutineJob.ProgressInfo("ObiSoftbody: generating shape matching clusters...", i / (float)triangles.Length);
            }

            for (int i = 0; i < connections.Length; ++i)
            {
                constraintIndices.Add(particleIndices.Count);
                particleIndices.Add(i);

                foreach (var n in connections[i])
                    particleIndices.Add(n);
            }
        }

        protected virtual IEnumerator CreateShapeMatchingConstraints(List<Vector3> particles)
        {

            // full particle index count:
            constraintIndices.Add(particleIndices.Count);

            //Create shape matching clusters:
            shapeMatchingConstraintsData = new ObiShapeMatchingConstraintsData();

            // pass a copy of the particleIndices array, as we need to preserve particle order (first particle in each constraint is the center particle)
            int[] constraintColors = GraphColoring.Colorize(particleIndices.ToArray(), constraintIndices.ToArray());

            for (int i = 0; i < constraintColors.Length; ++i)
            {
                int color = constraintColors[i];
                int cIndex = constraintIndices[i];

                // Add a new batch if needed:
                if (color >= shapeMatchingConstraintsData.GetBatchCount())
                    shapeMatchingConstraintsData.AddBatch(new ObiShapeMatchingConstraintsBatch());

                int amount = constraintIndices[i + 1] - cIndex;
                int[] clusterIndices = new int[amount];
                for (int j = 0; j < amount; ++j)
                    clusterIndices[j] = particleIndices[cIndex + j];

                shapeMatchingConstraintsData.batches[color].AddConstraint(clusterIndices,false);
            }

            // Set initial amount of active constraints:
            for (int i = 0; i < shapeMatchingConstraintsData.batches.Count; ++i)
            {
                shapeMatchingConstraintsData.batches[i].activeConstraintCount = shapeMatchingConstraintsData.batches[i].constraintCount;
            }

            yield return new CoroutineJob.ProgressInfo("ObiSoftbody: batching constraints", 1);
        }
    }
}