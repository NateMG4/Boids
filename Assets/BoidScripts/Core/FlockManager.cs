using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockManager : MonoBehaviour
{
    public FlockAgent agentPrefab;
    public List<FlockAgent> agents = new List<FlockAgent>();
    public FlockBehavior behavior;

    [Header("Spawn Settings")]
    [Range(10, 500)]
    public int startingCount = 25;
    public float driveFactor = 10f;
    public float maxSpeed = 5f;
    public float neighborRadius = 1.5f;
    public float avoidanceRadius_multiplier = 0.5f;

    [Header("Stats Template")]
    public BoidStats globalStats = BoidStats.Default();

    float squareMaxSpeed;
    float squareNeighborRadius;
    float squareAvoidanceRadius;

    public float SquareAvoidanceRadius { get { return squareAvoidanceRadius; } }

    void Start()
    {
        squareMaxSpeed = maxSpeed * maxSpeed;
        squareNeighborRadius = neighborRadius * neighborRadius;
        squareAvoidanceRadius = squareNeighborRadius * avoidanceRadius_multiplier * avoidanceRadius_multiplier;

        for (int i = 0; i < startingCount; i++)
        {
            FlockAgent newAgent = Instantiate(
                agentPrefab,
                Random.insideUnitCircle * startingCount * 0.2f,
                Quaternion.Euler(Vector3.forward * Random.Range(0f, 360f)),
                transform
            );
            newAgent.name = "Agent " + i;
            newAgent.Initialize(globalStats);
            agents.Add(newAgent);
        }
    }

    void Update()
    {
        foreach (FlockAgent agent in agents)
        {
            List<Transform> context = GetNearbyObjects(agent);
            
            // Pass the stats to the agent dynamically if we want real-time tweaking from Manager
            // agent.stats = globalStats; 

            Vector2 move = behavior.CalculateMove(agent, context, this);
            move *= driveFactor;
            if (move.sqrMagnitude > squareMaxSpeed)
            {
                move = move.normalized * maxSpeed;
            }
            agent.Move(move);
        }
    }

    List<Transform> GetNearbyObjects(FlockAgent agent)
    {
        List<Transform> context = new List<Transform>();
        Collider2D[] contextColliders = Physics2D.OverlapCircleAll(agent.transform.position, neighborRadius);
        foreach (Collider2D c in contextColliders)
        {
            if (c != agent.AgentColliderAsComponent()) // Extension method or cast needed?
            {
                // check if it is a flock agent
                // In a real game, might want to filter by layer
                context.Add(c.transform); 
            }
        }
        return context;
    }
}

// Helper extension to compare safely
public static class FlockExtensions {
    public static Component AgentColliderAsComponent(this FlockAgent agent) {
        return agent.GetComponent<Collider2D>();
    }
}
