using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PID controller for motorized hinge joints.
/// </summary>
public class PlatformBalancer : MonoBehaviour
{

    public float targetRotation = 0;
    public float bias = 0;
    public float Kp = 1;
    public float Ki = 0.2f;
    public float Kd = 0.1f;

    float lastIntegral;
    float lastError;

    HingeJoint joint;

    private void Awake()
    {
        joint = GetComponent<HingeJoint>();
    }

    void FixedUpdate()
    {
        float error = targetRotation - joint.angle;

        float integral = lastIntegral + error * Time.fixedDeltaTime;
        float derivative = (error - lastError) / Time.fixedDeltaTime;

        var motor = joint.motor;
        motor.targetVelocity = Kp * error + Ki * integral + Kd * derivative + bias;
        joint.motor = motor;

        lastError = error;
        lastIntegral = integral;
    }
}
