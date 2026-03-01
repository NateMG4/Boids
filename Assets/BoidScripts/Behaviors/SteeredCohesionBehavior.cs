using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behavior/Cohesion")]
public class SteeredCohesionBehavior : FlockBehavior
{
    Vector2 currentVelocity;
    public float agentSmoothTime = 0.5f;

    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, FlockManager flock)
    {
        // If no neighbors, no adjustment
        if (context.Count == 0)
            return Vector2.zero;

        // Add all points together and average
        Vector2 cohesionMove = Vector2.zero;
        int count = 0;
        
        foreach (Transform item in context)
        {
            // Simple check if it's a boid (in same layer, etc)
            // For now assuming all transforms in context are valid boids
            cohesionMove += (Vector2)item.position;
            count++;
        }
        
        if (count == 0) return Vector2.zero;

        cohesionMove /= count;

        // Create offset from agent position
        cohesionMove -= (Vector2)agent.transform.position;
        
        // Example of smoothing
        // cohesionMove = Vector2.SmoothDamp(agent.transform.up, cohesionMove, ref currentVelocity, agentSmoothTime);
        
        return cohesionMove;
    }
}
