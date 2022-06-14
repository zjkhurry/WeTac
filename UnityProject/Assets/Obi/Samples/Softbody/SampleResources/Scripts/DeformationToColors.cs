using UnityEngine;
using Obi;

[RequireComponent(typeof(ObiSoftbody))]
public class DeformationToColors : MonoBehaviour
{
	ObiSoftbody softbody;

    public SkinnedMeshRenderer skin;
    public Gradient gradient;
	public float deformationScaling = 10;

	float[] norms;
	int[] counts;
    Color[] colors;

	void Start()
	{
		softbody = GetComponent<ObiSoftbody>();
		softbody.OnEndStep += Softbody_OnEndStep;

		norms = new float[softbody.particleCount];
		counts = new int[softbody.particleCount];

        if (skin != null && skin.sharedMesh != null)
        {
            colors = new Color[skin.sharedMesh.vertexCount];
        }

    }

    void OnDestroy()
	{
		softbody.OnEndStep -= Softbody_OnEndStep;
	}

	private void Softbody_OnEndStep(ObiActor actor, float substepTime)
	{
		if (Mathf.Approximately(deformationScaling, 0))
			return;

		var dc = softbody.GetConstraintsByType(Oni.ConstraintType.ShapeMatching) as ObiConstraints<ObiShapeMatchingConstraintsBatch>;
		var sc = softbody.solver.GetConstraintsByType(Oni.ConstraintType.ShapeMatching) as ObiConstraints<ObiShapeMatchingConstraintsBatch>;

		if (dc != null && sc != null)
		{

			for (int j = 0; j < dc.batches.Count; ++j)
			{
				var batch = dc.batches[j] as ObiShapeMatchingConstraintsBatch;
				var solverBatch = sc.batches[j] as ObiShapeMatchingConstraintsBatch;

				for (int i = 0; i < batch.activeConstraintCount; i++)
				{
					// use rotation-invariant Frobeniums norm to get amount of deformation.
					int offset = softbody.solverBatchOffsets[(int)Oni.ConstraintType.ShapeMatching][j];

                    // use frobenius norm to estimate deformation.
                    float deformation = solverBatch.linearTransforms[offset + i].FrobeniusNorm() - 2;

                    for (int k = 0; k < batch.numIndices[i]; ++k)
                    {
                        int p = batch.particleIndices[batch.firstIndex[i] + k];
                        int or = softbody.solverIndices[p];
                        if (softbody.solver.invMasses[or] > 0)
                        {
                            norms[p] += deformation;
                            counts[p]++;
                        }
                    }
				}
			}

			// average force over each particle, map to color, and reset forces:
			for (int i = 0; i < softbody.solverIndices.Length; ++i)
			{
                if (counts[i] > 0)
                {
                    int solverIndex = softbody.solverIndices[i];
                    softbody.solver.colors[solverIndex] = gradient.Evaluate(norms[i] / counts[i] * deformationScaling + 0.5f);
                    norms[i] = 0;
                    counts[i] = 0;
                }
			}

            var surfaceBlueprint = softbody.softbodyBlueprint as ObiSoftbodySurfaceBlueprint;
            if (surfaceBlueprint != null && skin != null && skin.sharedMesh != null)
            {
                for (int i = 0; i < colors.Length; ++i)
                {
                    int particleIndex = surfaceBlueprint.vertexToParticle[i];
                    colors[i] = softbody.solver.colors[particleIndex];
                }
                skin.sharedMesh.colors = colors;
            }
        }
	}

}
