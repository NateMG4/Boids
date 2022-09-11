using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Boid : MonoBehaviour
{

    // might try turning on the show refrences code lense to see if some of these are pointless 

    public float vectorScaler;
    [SerializeField] float thrustStrength;
    [SerializeField] float torqueStrength;
    public float tolerance;
    public float angularVelocity;
    public float angleError;

    public Vector2 currentVector;
    public Vector2 targetVector;
    public float targetVectorMag;
    public bool auto;
    public bool isThrust;
    public float currentError;
    public float ifThrustError;

    public float rotation;
    public float torque;

    public GameObject allBoids;
    Rigidbody2D body;
    public float viewRange;
    public bool drawFlock;
    public bool drawVel;
    public bool drawThrust;
    public bool drawTarg;
    // mode 0: follows average neigbor vector
    // mode 1: follows mouse
    // mode 2: follows mouse dosnt affect neighbor vectors
    public float minTargetVector;
    public Vector2 setPoint;
    public bool setPointMode;

    public float kP;
    public float kI;
    public float kD;
    public float resultantTorque;

    public float convergenceTime = 0f;

    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(true);
        thrustStrength = (float) 15;
        torqueStrength = (float) 10;
        viewRange = 5f;
        tolerance = 0.1f;
        vectorScaler = 1;
        auto = false;
        drawFlock = true;
        body = GetComponent<Rigidbody2D>();

        drawVel = true;
        drawThrust = false;
        drawTarg = true;

        setPoint = new Vector2(0f, 0f);
        // setPointMode = false;
        // kP = 1;
        // kI = .01f;
        // kD = .25f;
        // if(Random.Range(0, 100) < 10){
        //     controlMode = 1;
        // }
        targetVector = new Vector2(Random.Range(-10, 10), Random.Range(-10, 10));
        // body.velocity = new Vector2(Random.Range(-1, 1), Random.Range(-1, 1));
    }
    public Vector2 getPosition(){
        return body.position;
    }
    float OrientationRadians()
    {
        return (body.rotation + 90) * Mathf.Deg2Rad;
    }

    // The vector that we would add to velocity, if we thrust
    Vector2 ThrustUnitVector(float addAngle = 0)
    {
        // get real rotation
        float angle = OrientationRadians() + addAngle;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
    public void setTargetVector(Vector2 vector){
        targetVector = vector;
    }
    // Update is called once per frame
    void Update()
    {

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        angularVelocity = body.angularVelocity;
        
        if(setPointMode){
            if(Input.GetMouseButtonDown(0)){
                setPoint = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                setPoint = Camera.main.ScreenToWorldPoint(setPoint);
                convergenceTime = 0;
            }
            setTargetVector(setPoint - body.position);
        }
        
        screenWrap();

        // if(targetVector.magnitude < minTargetVector){
        //     targetVector.x += (Mathf.Sign(targetVector.x) *thrustStrength);
        //     targetVector.y += (Mathf.Sign(targetVector.y) *thrustStrength);

        // }
        adjustToTargetVector();


        // Vector2 thrust = v*thrustStrength*ThrustUnitVector();
        // body.AddForce(thrust);
        // Debug.DrawRay(body.position, thrust*vectorScaler, Color.red);

        torque = -h*torqueStrength;
        body.AddTorque(torque);

        currentVector = body.velocity;
        if(drawVel){
            Debug.DrawRay(body.position, currentVector*vectorScaler, Color.yellow);
        }
        if(drawTarg){
            if(!setPointMode){
                Debug.DrawRay(body.position, targetVector*vectorScaler, Color.magenta);
            }
            else{
                Debug.DrawRay(body.position, targetVector*vectorScaler, Color.cyan);
            }
        }
        


        // body.rotation += 90;
        // body.AddForce
        // body.rotation
        
    }
    public void drawFlockRays(ArrayList flock){
        if(drawFlock){
            foreach(Boid b in flock){
                Debug.DrawLine(body.position, b.getPosition());
            }
        }
    }
    void screenWrap(){
        var viewportPosition = Camera.main.WorldToViewportPoint(transform.position);
        Vector3 extents = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));
        var newPosition = transform.position;
        Vector2 velocity = body.velocity;
        // Debug.DrawLine(new Vector3(0,0,0), transform.position, Color.green);

        if (viewportPosition.x >= 1 || viewportPosition.x <= 0)
        {
            newPosition.x += 2*extents.x * -Mathf.Sign(newPosition.x);
            // velocity.x = -body.velocity.x;
            // targetVector.x = -targetVector.x;
        }

        if (viewportPosition.y >= 1 || viewportPosition.y <= 0)
        {
            newPosition.y += 2*extents.y * -Mathf.Sign(newPosition.y);
            // velocity.y = -body.velocity.y;
            // targetVector.x = -targetVector.x;
        }
        // body.velocity = velocity;
        transform.position = newPosition;    
    }
    float calcError(Vector2 v, Vector2 target){
        // float e = Mathf.Abs(Vector2.Dot(target,target)-Vector2.Dot(v, target));
        float e = Mathf.Abs(Vector2.Angle(target, v));
        return e;
    }

    public float DerivativeTimeScale = .5f;
    public float derivativeTimer = 0f;

    public float prevError = 0f;
    // public float totalError = 0f;
    public float derivative = 0f;
    // public float intergral = 0f;
    public float angleTolerance = .5f;

    void turnToVector(Vector2 desiredVector){
        Debug.Log("Turning!");
        angleError = -Vector2.SignedAngle(desiredVector, ThrustUnitVector());
        angularVelocity = body.angularVelocity;

        if(Mathf.Abs(angleError) > angleTolerance){
            // intergral += angleError * Time.deltaTime;
            if(derivativeTimer > DerivativeTimeScale){
                derivative = (angleError - prevError) / derivativeTimer;
                derivativeTimer = 0;
                prevError = angleError;
            }
            derivativeTimer += Time.deltaTime;

            resultantTorque = (kP * angleError) + /*(kI * intergral) +*/ (kD * derivative);
            convergenceTime += Time.deltaTime;
            body.AddTorque(resultantTorque);
        }

    }
    public Vector2 desiredThrust;
    void adjustToTargetVector(){
        Debug.Log("Adjusting!");
        currentVector = body.velocity;
        desiredThrust = (targetVector - currentVector).normalized;
        Vector2 thrustVector = ThrustUnitVector()*thrustStrength;        
        turnToVector(desiredThrust);

        Debug.DrawRay(body.position + targetVector, desiredThrust * vectorScaler * 20, Color.green);
        
        // if(ifThrustError < currentError){
        if(Vector2.Dot(desiredThrust, thrustVector.normalized) > .5f){
            isThrust = true;
            body.AddForce(thrustVector);
            if(drawThrust){
                Debug.DrawRay(body.position, thrustVector*vectorScaler, Color.red);
            }

        }


        // Vector2 R = ThrustUnitVector(angularVelocity*Time.deltaTime);//*thrustStrength;
        // Vector2 LeftR = ThrustUnitVector((angularVelocity+torqueStrength*Time.deltaTime)*Time.deltaTime);//*thrustStrength;
        // Vector2 RightR = ThrustUnitVector((angularVelocity-torqueStrength*Time.deltaTime)*Time.deltaTime);//*thrustStrength;

        // float dotR = Vector2.Dot(desiredThrust, R);
        // float dotLeftR = Vector2.Dot(desiredThrust, LeftR);
        // float dotRightR = Vector2.Dot(desiredThrust, RightR);

        // if(dotLeftR > dotR && dotLeftR > dotRightR){
        //     body.AddTorque(torqueStrength);
        // }
        // else if(dotRightR > dotR){
        //     body.AddTorque(-torqueStrength);
        // }


    }
    void adjustToTargetVector2(){
        currentVector = body.velocity;
        float xError = targetVector.x - currentVector.x;
        float yError = targetVector.y - currentVector.y;
        
    }
    

}
