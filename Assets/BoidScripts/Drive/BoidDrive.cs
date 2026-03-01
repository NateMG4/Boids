using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PhysicsBoidDrive : MonoBehaviour
{
    private Rigidbody2D rb;
    private FlockAgent agent;

    private float currentThrust;
    private float currentTorque;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        agent = GetComponent<FlockAgent>();
        if (agent == null)
        {
            Debug.LogError("PhysicsBoidDrive requires a FlockAgent component.");
        }
    }

    private void FixedUpdate()
    {
        if (agent == null) return;
        rb.mass = agent.stats.mass; // sync mass (optional, can be cached)
    }

    public void Move(Vector2 desiredVelocity)
    {
        if (desiredVelocity == Vector2.zero) return;

        // 1. Rotate towards desired velocity
        float targetAngle = Mathf.Atan2(desiredVelocity.y, desiredVelocity.x) * Mathf.Rad2Deg - 90f; // -90 because 0 deg is usually "up" in Unity 2D sprites, if usage is different adjust here.
        // Assuming sprite faces UP (positive Y) as forward.
        
        float currentAngle = rb.rotation;
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);

        ApplyTorque(angleDifference);

        // 2. Apply Thrust
        // Only thrust if we are roughly facing the right way (e.g. within 90 degrees)
        // This prevents "drifting" backwards strongly
        if (Mathf.Abs(angleDifference) < 90f)
        {
             ApplyThrust(desiredVelocity);
        }
    }

    private void ApplyTorque(float angleError)
    {
        // Simple PD controller for rotation
        // Torque = (Kp * error) - (Kd * angularVelocity)
        // Kp: spring strength, Kd: damping
        
        // We can approximate "optimal" turn with just maxTorque logic for now or a simple damping.
        // Let's use a logic that tries to reach 0 error.
        
        // If we want to be physically accurate to the "Summer Civilization" prompt:
        // "must apply torque to rotate"
        
        float turnSpeed = agent.stats.maxTorque;
        float desiredAngularVel = angleError * 2.0f; // Multiplier defines how "snappy"
        desiredAngularVel = Mathf.Clamp(desiredAngularVel, -turnSpeed, turnSpeed);

        float torque = (desiredAngularVel - rb.angularVelocity) * rb.mass; // F = ma sort of approximation for torque
        
        // Clamp to max torque capability
        torque = Mathf.Clamp(torque, -agent.stats.maxTorque, agent.stats.maxTorque);

        rb.AddTorque(torque);
    }

    private void ApplyThrust(Vector2 desiredVelocity)
    {
        // Simple thrust logic: vector projection onto forward
        // We want to reach desiredVelocity.
        
        Vector2 currentVelocity = rb.velocity;
        Vector2 neededDeltaV = desiredVelocity - currentVelocity;

        // We can only apply force in the forward direction.
        Vector2 forward = transform.up; 
        
        // Project neededDeltaV onto forward vector
        float forwardNeeded = Vector2.Dot(neededDeltaV, forward);

        if (forwardNeeded > 0)
        {
            float force = forwardNeeded * rb.mass; // F = ma, to get accel
            force = Mathf.Clamp(force, 0, agent.stats.maxThrust);
            rb.AddForce(forward * force);
        }
        else
        {
             // Optional: retro-thrusters? 
             // If not, we just rely on drag or turning around.
             // For now, no retro thrusters.
        }
        
        // Cap speed
        if (rb.velocity.magnitude > agent.stats.maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * agent.stats.maxSpeed;
        }
    }
}
