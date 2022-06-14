using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

public class PostOnRail : MonoBehaviour
{
    public Vector3 startPoint;
    public Vector3 endPoint;
    public float speed = 50;
    public float wait = 1;
    public float stabilization = 1;
    public bool initialDirection = true;

    float direction = 1;

    private void Awake()
    {
        direction = initialDirection ? 1 : -1;
    }

    void FixedUpdate()
    {

        // project the transform to the rail:
        float mu;
        Vector3 projection = ObiUtils.ProjectPointLine(transform.position, startPoint, endPoint, out mu);

        if (direction > 0 && mu >= 1 ||
            direction < 0 && mu <= 0)
            direction *= -1;

        GetComponent<Rigidbody>().velocity = ((endPoint - startPoint).normalized * direction * speed + (projection - transform.position) * stabilization) * Time.fixedDeltaTime;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(startPoint, endPoint);
        Gizmos.DrawWireSphere(startPoint, 0.2f);
        Gizmos.DrawWireSphere(endPoint, 0.2f);
    }
}
