using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behavior/Composite")]
public class CompositeBehavior : FlockBehavior
{
    [System.Serializable]
    public struct BehaviorGroup 
    {
        public FlockBehavior behavior;
        public float weight; // Base weight, can be modulated by agent stats
    }

    public BehaviorGroup[] behaviors;

    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, FlockManager flock)
    {
        Vector2 move = Vector2.zero;

        // Iterate through behaviors
        for (int i = 0; i < behaviors.Length; i++)
        {
            Vector2 partialMove = behaviors[i].behavior.CalculateMove(agent, context, flock) * behaviors[i].weight;

            if (partialMove != Vector2.zero)
            {
                if (extractWeight(behaviors[i].behavior, agent.stats, out float statWeight)) {
                    partialMove *= statWeight;
                }
                
                if (partialMove.sqrMagnitude > behaviors[i].weight * behaviors[i].weight)
                {
                    partialMove.Normalize();
                    partialMove *= behaviors[i].weight;
                }

                move += partialMove;
            }
        }

        return move;
    }

    // Helper to map specific behavior types to BoidStats weights
    // This allows the LLM to control "Cohesion" abstractly without knowing about the ScriptableObject
    private bool extractWeight(FlockBehavior behavior, BoidStats stats, out float weight) 
    {
        string name = behavior.name.ToLower(); // simplistic matching for now, or use type checks
        // Ideally, Behaviors would have an enum property: BehaviorType { Alignment, Cohesion, etc }
        // For now, let's assume standard ones exist.
        
        // Better approach: Let's assume the passed behaviors are the "Logic" and the stat weights are multipliers.
        // It's hard to map generically without a Type.
        // Let's rely on the Behavior implementation to multiply by 1, and here we multiply by the relevant stat if we can detect it.
        // Actually, simpler: Let's pass the BoidStats INTO the behaviors? No, CalculateMove signature is fixed.
        // Let's check type.
        
        weight = 1f;
        if (behavior is SteeredAlignmentBehavior) weight = stats.alignmentWeight;
        else if (behavior is SteeredCohesionBehavior) weight = stats.cohesionWeight;
        else if (behavior is SteeredSeparationBehavior) weight = stats.separationWeight;
        
        return true;
    }
}
