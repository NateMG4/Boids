using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class Flock : MonoBehaviour
{
    public List<Boid> boids;
    public int numBoids => boids.Count;
    public Vector2 averageFlockPosition;
    public Vector2 averageTrajectory;
    public float seperation;
    public float alignment;
    public float cohesion;
    public GameObject target;
    public Vector2 targetVector;
    public float alignmentMagnitude;
    public bool drawRays;
    Flock(){

    }
    Flock(List<Boid> boidList){
        foreach(Boid b in boidList){
            addBoid(b);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        List<Boid> allBoids = new List<Boid>(GameObject.FindObjectsOfType<Boid>());
        foreach(Boid b in allBoids){
            addBoid(b);
        } 

    }

    // Update is called once per frame
    void Update()
    {

        if(Input.GetMouseButtonDown(0)){
            target.SetActive(true);
            Vector2 setPoint = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            setPoint = Camera.main.ScreenToWorldPoint(setPoint);
            target.transform.position = setPoint;
        }

        getAverageFlockPosition();
        getAverageFlockTrajectory();
        float averageTrajectoryMag = averageTrajectory.magnitude;
        Vector2 averageTrajectoryUnit = averageTrajectory.normalized;


        foreach(Boid b in boids){
            Vector2 seperationVector = caculateSeperationVector(b, seperation);

            Vector2 alignmentVector = Vector2.zero;
            if(target.activeSelf){
                alignmentVector = target.transform.position - (Vector3) b.getPosition();
            }
            Vector2 cohesionVector = cohesion*(averageFlockPosition - b.getPosition());

            if(drawRays){
            b.drawRay(seperationVector, Color.red);
            b.drawRay(alignmentVector, Color.green);
            b.drawRay(cohesionVector, Color.blue);
            }
            Vector2 newTarget = seperationVector + alignmentVector + cohesionVector;
            b.setTargetVector(newTarget);
        }
    }
    void addBoid(Boid b){
        b.setMode(BoidMode.Flock);
        boids.Add(b);
    }
    void getAverageFlockPosition(){
        foreach(Boid b in boids){
            averageFlockPosition += b.getPosition();
        }
        averageFlockPosition /= numBoids;
    }
    void getAverageFlockTrajectory(){
        foreach(Boid b in boids){
            averageTrajectory += b.targetVector;
        }
        averageTrajectory /= numBoids;

    }
    Vector2 caculateSeperationVector(Boid boid, float seperationConstant){
        Vector2 adjustmentVector = Vector2.zero;;
        Vector2 unitVector = new Vector2(1,1);
        foreach(Boid b in boids){
            if(b == boid){
                continue;
            }
            adjustmentVector += seperationConstant * unitVector/getDistanceBetweenBoids(boid,b);

        }
        return adjustmentVector;
    }   
    Vector2 getDistanceBetweenBoids(Boid a, Boid b){
        return a.getPosition() - b.getPosition();
    }
    

    
}
