using UnityEngine;
using System.Collections;

namespace Obi
{

    public abstract class ObiSoftbodyBlueprintBase : ObiMeshBasedActorBlueprint
    {
        protected override IEnumerator Initialize() { yield return null; }
    }
}