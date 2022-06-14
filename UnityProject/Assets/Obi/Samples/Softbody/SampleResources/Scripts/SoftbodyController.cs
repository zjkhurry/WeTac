using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

[RequireComponent(typeof(ObiSoftbody))]
public class SoftbodyController : MonoBehaviour
{
    public Transform referenceFrame;
    public float acceleration = 80;
    public float jumpPower = 1;

    [Range(0,1)]
    public float airControl = 0.3f;

    ObiSoftbody softbody;
    bool onGround = false;

    // Start is called before the first frame update
    void Start()
    {
        softbody = GetComponent<ObiSoftbody>();
        softbody.solver.OnCollision += Solver_OnCollision;
    }

    private void OnDestroy()
    {
        softbody.solver.OnCollision -= Solver_OnCollision;
    }

    // Update is called once per frame
    void Update()
    {
        if (referenceFrame != null)
        {
            Vector3 direction = Vector3.zero;

            // Determine movement direction:
            if (Input.GetKey(KeyCode.W))
            {
                direction += referenceFrame.forward * acceleration;
            }
            if (Input.GetKey(KeyCode.A))
            {
                direction += -referenceFrame.right * acceleration;
            }
            if (Input.GetKey(KeyCode.S))
            {
                direction += -referenceFrame.forward * acceleration;
            }
            if (Input.GetKey(KeyCode.D))
            {
                direction += referenceFrame.right * acceleration;
            }

            // flatten out the direction so that it's parallel to the ground:
            direction.y = 0;

            // apply ground/air movement:
            float effectiveAcceleration = acceleration;

            if (!onGround)
                effectiveAcceleration *= airControl;

            softbody.AddForce(direction.normalized * effectiveAcceleration, ForceMode.Acceleration);

            // jump:
            if (onGround && Input.GetKeyDown(KeyCode.Space))
            {
                onGround = false;
                softbody.AddForce(Vector3.up * jumpPower, ForceMode.VelocityChange);
            }
        }
    }

    private void Solver_OnCollision(ObiSolver solver, ObiSolver.ObiCollisionEventArgs e)
    {
        onGround = false;

        var world = ObiColliderWorld.GetInstance();
        foreach (Oni.Contact contact in e.contacts)
        {
            // look for actual contacts only:
            if (contact.distance > 0.01)
            {
                var col = world.colliderHandles[contact.bodyB].owner;
                if (col != null)
                {
                    onGround = true;
                    return;
                }
            }
        }
    }
}
