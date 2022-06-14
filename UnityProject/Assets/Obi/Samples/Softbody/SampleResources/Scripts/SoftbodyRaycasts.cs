using System.Collections.Generic;
using UnityEngine;
using Obi;

public class SoftbodyRaycasts : MonoBehaviour
{
    public ObiSolver solver;
    public LineRenderer[] lasers;

    List<Ray> rays = new List<Ray>();

    void Update()
    {
        rays.Clear();

        for (int i = 0; i < lasers.Length; ++i)
        {
            lasers[i].useWorldSpace = true;
            lasers[i].positionCount = 2;
            lasers[i].SetPosition(0, lasers[i].transform.position);
            rays.Add(new Ray(lasers[i].transform.position, lasers[i].transform.up));
        }

        int filter = ObiUtils.MakeFilter(ObiUtils.CollideWithEverything, 0);

        var results = solver.Raycast(rays, filter,100,0.05f);

        if (results != null)
        {
            for (int i = 0; i < results.Length; ++i)
            {
                lasers[i].SetPosition(1, rays[i].GetPoint(results[i].distance));

                if (results[i].simplexIndex >= 0)
                {
                    lasers[i].startColor = Color.red;
                    lasers[i].endColor = Color.red;
                }
                else
                {
                    lasers[i].startColor = Color.blue;
                    lasers[i].endColor = Color.blue;
                }
            }
        }
    }
}
