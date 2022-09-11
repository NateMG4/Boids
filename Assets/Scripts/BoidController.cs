using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidController : MonoBehaviour
{
    Logger l;

    public float minVectorMag; 
    public ArrayList boids;
    public Boid[] boidArray;
    public Boid orignialBoid;
    // Start is called before the first frame update
    void Start()
    {  
        minVectorMag = 10;
        boids = new ArrayList(GameObject.FindObjectsOfType<Boid>());
        boidArray = GameObject.FindObjectsOfType<Boid>();
        orignialBoid = GameObject.FindObjectOfType<Boid>();
    }

    // Update is called once per frame
    void Update()
    {

        setAverageFlockVectors();
        if(Input.GetMouseButtonDown(0)){
            Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
            mousePos = Camera.main.ScreenToWorldPoint(mousePos);
            mousePos.z = 0;
            Boid b = Instantiate(orignialBoid, mousePos, Quaternion.Euler(0, 0, 0));
            b.name = "Boid";
            boids.Add(b);
            boidArray = GameObject.FindObjectsOfType<Boid>();

        }
    }
    void setAverageFlockVectors(){
        Vector2 averageTargetVector;
        int neighborsInRange;
        ArrayList flock;
        foreach(Boid b in boids){
            averageTargetVector = new Vector2(0, 0);
            neighborsInRange = 0;
            flock = new ArrayList();
            if(b.controlMode == 0){
                foreach(Boid bNeighbor in boids){
                    float dist = Mathf.Abs((b.getPosition() - bNeighbor.getPosition()).magnitude);
                    if(dist < b.viewRange){
                        averageTargetVector += bNeighbor.targetVector;
                        flock.Add(bNeighbor);
                        neighborsInRange += 1;
                    }
                }
                averageTargetVector /= neighborsInRange;
                if(averageTargetVector.magnitude < minVectorMag){
                    averageTargetVector.x += (Mathf.Sign(averageTargetVector.x) *minVectorMag);
                    averageTargetVector.y += (Mathf.Sign(averageTargetVector.y) *minVectorMag);

               }
                b.setTargetVector(averageTargetVector);
                Debug.Log("AverageTargetVector " + averageTargetVector);
                Debug.Log("Neighbors In Range " + neighborsInRange);
                b.drawFlockRays(flock);

            }
        }

    }
}
