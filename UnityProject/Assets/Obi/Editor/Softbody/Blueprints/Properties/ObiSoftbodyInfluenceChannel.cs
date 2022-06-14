namespace Obi
{
    public class ObiSoftbodyInfluenceChannel : ObiBlueprintFloatProperty
    {
        public ObiSoftbodySkinnerEditor skinEditor;

        public ObiSoftbodyInfluenceChannel(ObiSoftbodySkinnerEditor skinEditor) : base(null, 0, 1)
        {
            this.skinEditor = skinEditor;
            brushModes.Add(new ObiFloatPaintBrushMode(this));
            brushModes.Add(new ObiFloatAddBrushMode(this));
            brushModes.Add(new ObiFloatSmoothBrushMode(this));
        }

        public override string name
        {
            get { return "Softbody influence"; }
        }

        public override float Get(int index)
        {
            return skinEditor.skinner.m_softbodyInfluences[index];
        }
        public override void Set(int index, float value)
        {
            skinEditor.skinner.m_softbodyInfluences[index] = value;
        }
        public override bool Masked(int index)
        {
            return false;//!skinEditor.facingCamera[index];
        }
    }
}
