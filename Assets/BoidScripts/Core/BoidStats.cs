using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BoidStats 
{
    [Header("Movement Settings")]
    public float maxSpeed;
    public float maxThrust;     // Force applied forward
    public float maxTorque;     // Torque applied to rotate
    public float mass;          // Rigidbody mass

    [Header("Vision Settings")]
    public float visionRadius;  // For neighbor detection

    [Header("Behavior Weights")]
    // Weights (LLM can tweak these to change "personality")
    public float separationWeight;
    public float cohesionWeight;
    public float alignmentWeight;
    public float targetWeight;

    // Default constructor for easy instantiation
    public static BoidStats Default() 
    {
        return new BoidStats {
            maxSpeed = 10f,
            maxThrust = 10f,
            maxTorque = 50f,
            mass = 1f,
            visionRadius = 5f,
            separationWeight = 1f,
            cohesionWeight = 1f,
            alignmentWeight = 1f,
            targetWeight = 1f
        };
    }
}
