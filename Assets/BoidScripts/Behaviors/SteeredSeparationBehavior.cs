using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behavior/Separation")]
public class SteeredSeparationBehavior : FlockBehavior
{
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, FlockManager flock)
    {
        // If no neighbors, no adjustment
        if (context.Count == 0)
            return Vector2.zero;

        Vector2 separationMove = Vector2.zero;
        int nAvoid = 0;

        foreach (Transform item in context)
        {
            float sqrDist = Vector2.SqrMagnitude(item.position - agent.transform.position);

            if (sqrDist < flock.SquareAvoidanceRadius)
            {
                nAvoid++;
                separationMove += (Vector2)(agent.transform.position - item.position);
            }
        }

        if (nAvoid > 0)
            separationMove /= nAvoid;

        return separationMove;
    }
}
