namespace Obi
{
    public class ObiBlueprintMaxDeformation : ObiBlueprintFloatProperty
    {

        public ObiBlueprintMaxDeformation(ObiActorBlueprintEditor editor) : base(editor, 0)
        {
            brushModes.Add(new ObiFloatPaintBrushMode(this));
            brushModes.Add(new ObiFloatAddBrushMode(this));
            brushModes.Add(new ObiFloatSmoothBrushMode(this));
        }

        public override string name
        {
            get { return "Max deformation"; }
        }

        public override float Get(int index)
        {
            var constraints = editor.blueprint.GetConstraintsByType(Oni.ConstraintType.ShapeMatching) as ObiConstraints<ObiShapeMatchingConstraintsBatch>;
            foreach (var batch in constraints.batches)
            {
                for (int i = batch.constraintCount - 1; i >= 0; --i)
                {
                    int firstParticle = batch.particleIndices[batch.firstIndex[i]];
                    if (firstParticle == index)
                    {
                        return batch.materialParameters[i * 5 + 4];
                    }
                }
            }
            return 0;
        }
        public override void Set(int index, float value)
        {
            var constraints = editor.blueprint.GetConstraintsByType(Oni.ConstraintType.ShapeMatching) as ObiConstraints<ObiShapeMatchingConstraintsBatch>;
            foreach (var batch in constraints.batches)
            {
                for (int i = batch.constraintCount - 1; i >= 0; --i)
                {
                    int firstParticle = batch.particleIndices[batch.firstIndex[i]];
                    if (firstParticle == index)
                    {
                        batch.materialParameters[i * 5 + 4] = value;
                        return;
                    }
                }
            }
        }
        public override bool Masked(int index)
        {
            return !editor.Editable(index);
        }
    }
}
