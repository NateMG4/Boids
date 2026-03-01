using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhysicsBoidDrive))]
[RequireComponent(typeof(CircleCollider2D))]
public class FlockAgent : MonoBehaviour
{
    public BoidStats stats = BoidStats.Default();
    public FlockAgent AgentCollider { get; private set; }
    
    private PhysicsBoidDrive drive;
    
    public string AgentName { get { return name; } }

    private void Start()
    {
        drive = GetComponent<PhysicsBoidDrive>();
        AgentCollider = this;
    }

    public void Initialize(BoidStats startingStats)
    {
        stats = startingStats;
    }

    public void Move(Vector2 velocity)
    {
        if (drive != null)
        {
            drive.Move(velocity);
        }
    }
}
