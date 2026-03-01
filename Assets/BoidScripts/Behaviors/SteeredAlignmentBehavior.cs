using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behavior/Alignment")]
public class SteeredAlignmentBehavior : FlockBehavior
{
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, FlockManager flock)
    {
        // If no neighbors, maintain current heading
        if (context.Count == 0)
            return agent.transform.up; // or Vector2.zero if we want them to stop? Usually maintain heading.

        Vector2 alignmentMove = Vector2.zero;
        int count = 0;
        
        foreach (Transform item in context)
        {
            // For alignment we need the `transform.up` or `velocity` of the neighbor
            // context is Transform, so we can use transform.up as heading
            alignmentMove += (Vector2)item.transform.up;
            count++;
        }
        
        if (count == 0) return agent.transform.up;

        alignmentMove /= count;

        return alignmentMove;
    }
}
