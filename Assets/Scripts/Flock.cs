using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flock : MonoBehaviour
{
    public List<BoidV2> boids;
    public int numBoids => boids.Count;
    public Vector2 averageFlockPosition;
    Flock(){

    }
    Flock(List<BoidV2> boidList){
        boids = boidList;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void getAverageFlockPosition(){
        foreach(BoidV2 b in boids){
            averageFlockPosition += b.getPosition();
        }
        averageFlockPosition /= numBoids;
    }
    
}
