using UnityEngine;

namespace Obi
{
    [AddComponentMenu("Physics/Obi/Obi Softbody", 930)]
    public class ObiSoftbody : ObiActor, IShapeMatchingConstraintsUser
    {
        [SerializeField] protected ObiSoftbodyBlueprintBase m_SoftbodyBlueprint;

        [SerializeField] protected bool m_SelfCollisions = false;

        [SerializeField] [HideInInspector] private int centerBatch = -1;
        [SerializeField] [HideInInspector] private int centerShape = -1;

        // shape matching constraints:
        [SerializeField] protected bool _shapeMatchingConstraintsEnabled = true;
        [SerializeField] [Range(0, 1)] protected float _deformationResistance = 1;
        [SerializeField] [Range(0, 1)] protected float _maxDeformation = 0;
        [SerializeField] protected float _plasticYield = 0;
        [SerializeField] protected float _plasticCreep = 0;
        [SerializeField] protected float _plasticRecovery = 0;

        /// <summary>
        /// Whether to use simplices (triangles, edges) for contact generation.
        /// </summary>
        public override bool surfaceCollisions
        {
            get
            {
                return false;
            }
            set
            {
                m_SurfaceCollisions = false;
            }
        }

        /// <summary>  
        /// Whether this actor's shape matching constraints are enabled.
        /// </summary>
        public bool shapeMatchingConstraintsEnabled
        {
            get { return _shapeMatchingConstraintsEnabled; }
            set
            {
                if (value != _shapeMatchingConstraintsEnabled)
                {
                    _shapeMatchingConstraintsEnabled = value; SetConstraintsDirty(Oni.ConstraintType.ShapeMatching);
                }
            }
        }

        /// <summary>  
        /// Deformation resistance for shape matching constraints.
        /// </summary>
        /// A value of 1 will make constraints to try and resist deformation as much as possible, given the current solver settings.
        /// Lower values will progressively make the softbody softer.
        public float deformationResistance
        {
            get { return _deformationResistance; }
            set
            {
                _deformationResistance = value; SetConstraintsDirty(Oni.ConstraintType.ShapeMatching);
            }
        }

        /// <summary>  
        /// Maximum amount of plastic deformation.
        /// </summary>
        /// This determines how much deformation can be permanently absorbed via plasticity by shape matching constraints.
        public float maxDeformation
        {
            get { return _maxDeformation; }
            set
            {
                _maxDeformation = value; SetConstraintsDirty(Oni.ConstraintType.ShapeMatching);
            }
        }

        /// <summary>  
        /// Threshold for plastic behavior. 
        /// </summary>
        /// Once softbody deformation goes above this value, a percentage of the deformation (determined by <see cref="plasticCreep"/>) will be permanently absorbed into the softbody's rest shape.
        public float plasticYield
        {
            get { return _plasticYield; }
            set
            {
                _plasticYield = value; SetConstraintsDirty(Oni.ConstraintType.ShapeMatching);
            }
        }

        /// <summary>  
        /// Percentage of deformation that gets absorbed into the rest shape, once deformation goes above the <see cref="plasticYield"/> threshold.
        /// </summary>
        public float plasticCreep
        {
            get { return _plasticCreep; }
            set
            {
                _plasticCreep = value; SetConstraintsDirty(Oni.ConstraintType.ShapeMatching);
            }
        }

        /// <summary>  
        /// Rate of recovery from plastic deformation.
        /// </summary>
        /// A value of 0 will make sure plastic deformation is permament, and the softbody never recovers from it. Any higher values will make the softbody return to
        /// its original shape gradually over time.
        public float plasticRecovery
        {
            get { return _plasticRecovery; }
            set
            {
                _plasticRecovery = value; SetConstraintsDirty(Oni.ConstraintType.ShapeMatching);
            }
        }

        public override ObiActorBlueprint sourceBlueprint
        {
            get { return m_SoftbodyBlueprint; }
        }

        public ObiSoftbodyBlueprintBase softbodyBlueprint
        {
            get { return m_SoftbodyBlueprint; }
            set
            {
                if (m_SoftbodyBlueprint != value)
                {
                    RemoveFromSolver();
                    ClearState();
                    m_SoftbodyBlueprint = value;
                    AddToSolver();
                }
            }
        }

        /// <summary>  
        /// Whether particles in this actor colide with particles using the same phase value.
        /// </summary>
        public bool selfCollisions
        {
            get { return m_SelfCollisions; }
            set { if (value != m_SelfCollisions) { m_SelfCollisions = value; SetSelfCollisions(selfCollisions); } }
        }

        /// <summary>
        /// If true, it means particles may not be completely spherical, but ellipsoidal.
        /// </summary>
        /// In the case of softbodies, this is true, as particles can be deformed to adapt to the body surface.
        public override bool usesAnisotropicParticles
        {
            get { return true; }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            SetupRuntimeConstraints();
        }

        private void SetupRuntimeConstraints()
        {
            SetConstraintsDirty(Oni.ConstraintType.ShapeMatching);
            SetSelfCollisions(m_SelfCollisions);
        }

        public override void LoadBlueprint(ObiSolver solver)
        {
            base.LoadBlueprint(solver);
            RecalculateCenterShape();
            SetSelfCollisions(m_SelfCollisions);
        }

        public override void Teleport(Vector3 position, Quaternion rotation)
        {
            base.Teleport(position, rotation);

            if (!isLoaded)
                return;

            Matrix4x4 offset = solver.transform.worldToLocalMatrix *
                               Matrix4x4.TRS(position, Quaternion.identity, Vector3.one) *
                               Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one) *
                               Matrix4x4.TRS(Vector3.zero, Quaternion.Inverse(transform.rotation), Vector3.one) *
                               Matrix4x4.TRS(-transform.position, Quaternion.identity, Vector3.one) *
                               solver.transform.localToWorldMatrix;

            Quaternion rotOffset = offset.rotation;

            var ac = GetConstraintsByType(Oni.ConstraintType.ShapeMatching) as ObiConstraints<ObiShapeMatchingConstraintsBatch>;
            var sc = solver.GetConstraintsByType(Oni.ConstraintType.ShapeMatching) as ObiConstraints<ObiShapeMatchingConstraintsBatch>;

            // rotate clusters accordingly:
            for (int i = 0; i < ac.GetBatchCount(); ++i)
            {
                int batchOffset = solverBatchOffsets[(int)Oni.ConstraintType.ShapeMatching][i];

                for (int j = 0; j < ac.batches[i].activeConstraintCount; ++j)
                {
                    sc.batches[j].orientations[batchOffset + i] = rotOffset * sc.batches[i].orientations[batchOffset + j];
                }
            }

        }

        public void RecalculateRestShapeMatching()
        {
            if (Application.isPlaying && isLoaded)
            {
                var sc = solver.GetConstraintsByType(Oni.ConstraintType.ShapeMatching) as ObiConstraints<ObiShapeMatchingConstraintsBatch>;

                foreach (var batch in sc.batches)
                    batch.RecalculateRestShapeMatching();
            }
        }

        private void RecalculateCenterShape()
        {

            centerShape = -1;
            centerBatch = -1;

            if (Application.isPlaying && isLoaded)
            {

                for (int i = 0; i < solverIndices.Length; ++i)
                {
                    if (solver.invMasses[solverIndices[i]] <= 0)
                        return;
                }

                var sc = m_SoftbodyBlueprint.GetConstraintsByType(Oni.ConstraintType.ShapeMatching) as ObiConstraints<ObiShapeMatchingConstraintsBatch>;

                // Get the particle whose center is closest to the actor's center (in blueprint space)
                float minDistance = float.MaxValue;
                for (int j = 0; j < sc.GetBatchCount(); ++j)
                {
                    var batch = sc.GetBatch(j) as ObiShapeMatchingConstraintsBatch;

                    for (int i = 0; i < batch.activeConstraintCount; ++i)
                    {
                        float dist = m_SoftbodyBlueprint.positions[batch.particleIndices[batch.firstIndex[i]]].sqrMagnitude;

                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            centerShape = i;
                            centerBatch = j;
                        }
                    }
                }
            }
        }

        /// <summary>  
        /// Recalculates shape matching rest state.
        /// </summary>
        /// Recalculates the shape used as reference for transform position/orientation when there are no fixed particles, as well as the rest shape matching state.
        /// Should be called manually when changing the amount of fixed particles and/ or active particles.
        public override void UpdateParticleProperties()
        {
            RecalculateRestShapeMatching();
            RecalculateCenterShape();
        }

        public override void Interpolate()
        {
            var sc = solver.GetConstraintsByType(Oni.ConstraintType.ShapeMatching) as ObiConstraints<ObiShapeMatchingConstraintsBatch>;

            if (Application.isPlaying && isActiveAndEnabled && centerBatch > -1 && centerBatch < sc.batches.Count)
            {
                var batch = sc.batches[centerBatch] as ObiShapeMatchingConstraintsBatch;
                var offsets = solverBatchOffsets[(int)Oni.ConstraintType.ShapeMatching];

                if (centerShape > -1 && centerShape < batch.activeConstraintCount && centerBatch < offsets.Count)
                {
                    int offset = offsets[centerBatch] + centerShape;

                    transform.position = solver.transform.TransformPoint((Vector3)batch.coms[offset] - batch.orientations[offset] * batch.restComs[offset]);
                    transform.rotation = solver.transform.rotation * batch.orientations[offset];
                }
            }

            SetSelfCollisions(selfCollisions);

            base.Interpolate();
        }

    }

}