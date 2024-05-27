using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Video;

public class Boid : MonoBehaviour
{
    Rigidbody2D body;
    public int torqueStrength;
    public int thrustStrength;
    public float OrientationAngle;
    public bool test;
    public float AngularVelocity;
    public float setAngle;
    public GameObject target;
    public LineController line;
    public Vector2 targetVector;
    public int vectorScaler;
    public float angleError;
    public float thrustError;
    public float maxDistanceError;
    public float flipTime;
    public Vector2 esitmatedTransformation;
    private Vector2 lastVector;
    public float angularVelocityError;
    public BoidMode mode = BoidMode.Setpoint;
    public bool DrawRays;
    public bool DrawTarget;
    public float maxAngularVelocity;

    // Start is called before the first frame update
    void Start()
    {
        // rotationStrength = 20;
        // thrustStrength = 5;
        gameObject.SetActive(true);
        body = GetComponent<Rigidbody2D>();
        // body.rotation = 90;
        // body.angularVelocity = 1f;
        // body.angularVelocity = 1000;
    }

    // Update is called once per frame
    void Update()
    {
        OrientationAngle = Orientation() % 360;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
       


        screenWrap();
        
        if(DrawTarget){
            target.SetActive(true);
            target.transform.position = body.position + targetVector;
            line.setLine(body.position, target.transform.position);
        }

        if(mode == BoidMode.Setpoint){
            line.setLine(body.position, target.transform.position);
            targetVector = target.transform.position - body.transform.position;
        }
        
        if(mode == BoidMode.Setpoint && Input.GetMouseButtonDown(0)){
            test = true;
            target.SetActive(true);
            line.setActive();
            Vector2 setPoint = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            setPoint = Camera.main.ScreenToWorldPoint(setPoint);
            target.transform.position = setPoint;
        }


        if(Input.GetKey(KeyCode.LeftShift)){
            stopRotation();
        }

    }
    void FixedUpdate()
    {
        AngularVelocity = body.angularVelocity;
        // if(test){

            adjustToTargetVector();
        // }
        // body.AddTorque(totalForce, ForceMode2D.Force);
    }

    float OrientationRadians()
    {
        return (body.rotation + 90) * Mathf.Deg2Rad;
    }
    float Orientation()
    {
        return (body.rotation + 90);
    }
    public Vector2 getPosition(){
        return body.position;
    }
    void TurnToVector(Vector2 desiredVector){

        float deltaAngle = -Vector2.SignedAngle(desiredVector, ThrustUnitVector());

        float stopTime = estimatedAngularBreakTime();
        float estimatedDeltaAngle = estimatedAngularBreakRotation(stopTime);
        int direction = Math.Sign(deltaAngle);
        angularVelocityError = torqueStrength * Time.fixedDeltaTime;
        float appliedForce;
        float deltaDeltaAngle = Mathf.DeltaAngle(deltaAngle, estimatedDeltaAngle);

        if(math.sign(deltaAngle) != math.sign(estimatedDeltaAngle)){
            Debug.Log(string.Format("deltaAngle: {0}\n estimatedDeltaAngle: {1}",deltaAngle, estimatedDeltaAngle));
        }

        if(math.abs(body.angularVelocity) > maxAngularVelocity){
            stopRotationManual();
        }
    else if( math.sign(deltaDeltaAngle - direction*angleError) != direction){
            appliedForce = direction * torqueStrength * Mathf.Deg2Rad;
            body.AddTorque(appliedForce, ForceMode2D.Force);
        }
        else if(body.angularVelocity > angularVelocityError || body.angularVelocity < -angularVelocityError){
            appliedForce = -direction * torqueStrength * Mathf.Deg2Rad;
            body.AddTorque(appliedForce, ForceMode2D.Force);
        }
        else{
            body.angularVelocity = 0;
            // TestTurnToAngle = false;
        }

    }
    
    void stopRotation(){
        body.angularVelocity = 0;
    }
    void stopRotationManual(){
        if(body.angularVelocity > angularVelocityError || body.angularVelocity < -angularVelocityError){
            float appliedForce = -Math.Sign(body.angularVelocity) * torqueStrength * Mathf.Deg2Rad;
            body.AddTorque(appliedForce, ForceMode2D.Force);
        }
    }
    Vector2 ThrustUnitVector(float addAngle = 0)
    {
        // get real rotation
        float angle = OrientationRadians() + addAngle;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
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

    float estimatedAngularBreakTime(){
        return  Math.Abs(body.angularVelocity) / torqueStrength;
    }

    float estimatedAngularBreakRotation(float time){
        return time*((body.angularVelocity) + (.5f)*(torqueStrength)*(time));
    }

    // Assumes 0 starting angular velocity
    float estimatedTurnTime(float delta){
        float av = math.sqrt(2*torqueStrength*(math.abs(delta)/2));
        return 2*(av/torqueStrength);
    }
    float estimatedLinearBreakTime(){
        return Math.Abs(body.velocity.magnitude) / thrustStrength;
    }
    // Includes time to rotatate to thrusting vector
    float estimatedLinearBreakTime(Vector2 vector){
        float turnTime = estimatedTurnTime(-Vector2.SignedAngle(vector, ThrustUnitVector()));
        return Math.Abs(body.velocity.magnitude) / thrustStrength;
    }
    Vector2 estimatedLinerBreakDistance(float time){
        flipTime = estimatedTurnTime(180);
        float scalar = body.velocity.magnitude*flipTime + time*(body.velocity.magnitude - (.5f)*(thrustStrength)*(time));
        return body.velocity.normalized*scalar;
    }


    void adjustToTargetVector(){
        // Debug.Log("Adjusting!");
        Vector2 currentVector = body.velocity;
        // Vector2 desiredThrust = (targetVector - currentVector).normalized;
        Vector2 thrustVector = ThrustUnitVector()*thrustStrength;        
        Vector2 adjustedTargetVector = targetVector;
        // - targetVector.normalized*maxDistanceError;
        //calculate time to stop
        float time = estimatedLinearBreakTime();
        esitmatedTransformation = estimatedLinerBreakDistance(time);
        float estimatedError = (targetVector - esitmatedTransformation).magnitude;
        Vector2 desiredThrust = -(esitmatedTransformation - adjustedTargetVector);
        Vector2 desiredThrustDirection = desiredThrust.normalized;

        if(DrawRays){
        Debug.DrawRay(body.position, body.velocity, Color.yellow);
        Debug.DrawRay(body.position, esitmatedTransformation, Color.blue);
        Debug.DrawRay(body.position + targetVector, desiredThrust * vectorScaler, Color.green);
        }
        

        if(targetVector.magnitude > maxDistanceError){
            TurnToVector(desiredThrust);
            lastVector = thrustVector;
            if(Vector2.Dot(desiredThrustDirection, thrustVector.normalized) > thrustError){
                body.AddForce(thrustVector);
                if(DrawRays){
                    Debug.DrawRay(body.position, thrustVector*vectorScaler, Color.red);
                }
            }
        }
        else if(targetVector.magnitude < maxDistanceError && body.velocity.magnitude > 0){
            TurnToVector(-body.velocity);
            desiredThrustDirection = -body.velocity.normalized;
            if(Vector2.Dot(desiredThrustDirection, thrustVector.normalized) > thrustError){
                thrustVector = math.min(thrustStrength, body.velocity.magnitude) * ThrustUnitVector();
                body.AddForce(thrustVector);
                if(DrawRays){
                    Debug.DrawRay(body.position, thrustVector*vectorScaler, new Color(255, 127, 0, 1));
                }
            }
        }
        else{
            stopRotationManual();
        }
    }
    // Converts angle to bounds -180 to 180
    float boundAngle(float angle){
        angle = angle % 360;
        angle = (angle + 360) % 360;
        return angle > 180 ? angle -= 360 : angle;
    }

    public void setMode(BoidMode m){
        mode = m;
    }
    public void setTargetVector(Vector2 newTarget){
        targetVector = newTarget;
    }
    public void drawRay(Vector2 direction, Color c){
        Debug.DrawRay(body.position, direction, c);
    }

}
public enum BoidMode{
        Setpoint,
        Flock
    }
