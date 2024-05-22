using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Video;

public class BoidV2 : MonoBehaviour
{
    Rigidbody2D body;
    public int rotationStrength;
    public int thrustStrength;
    public float rotationStopTime;
    
    public float OrientationAngle;
    public float estimateStopedAngle;
    public bool TestTurnToAngle;
    public float AngularVelocity;
    public float appliedForce;
    public float vError;
    public float fixedUpdateTime;
    public float testVelocity;
    // Start is called before the first frame update
    void Start()
    {
        rotationStrength = 20;
        thrustStrength = 5;
        gameObject.SetActive(true);
        body = GetComponent<Rigidbody2D>();
        body.rotation = 90;
        body.angularVelocity = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        OrientationAngle = Orientation() % 360;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        body.velocity = ThrustUnitVector() * v * thrustStrength;
        float torque = -h * rotationStrength * Time.deltaTime;
        // body.AddTorque(torque);
        // rotationStopTime = calculateRotationalConvergenceTime();


        screenWrap();
        
        // body.angularDrag = Input.GetKey(KeyCode.Space) ? 1 : 0;
            
        
        if(Input.GetKey(KeyCode.LeftShift)){
            stopRotation();
        }
        TestTurnToAngle = Input.GetKey(KeyCode.Space) ? true : TestTurnToAngle;

        
    }
    void FixedUpdate()
    {
        AngularVelocity = body.angularVelocity;
        fixedUpdateTime = Time.fixedDeltaTime;
        if(TestTurnToAngle){
            body.AddTorque(rotationStrength / Time.fixedDeltaTime, ForceMode2D.Force);
            TestTurnToAngle = false;
            // TurnToAngle(0);
        }
    }
    float OrientationRadians()
    {
        return (body.rotation + 90) * Mathf.Deg2Rad;
    }
    float Orientation()
    {
        return (body.rotation + 90);
    }

    void TurnToAngle(float angle){
        angle = angle %360;
        float bodyAngle = Orientation()%360;
        float deltaAngle = angle - bodyAngle;
        float stopTime = calculateRotationalConvergenceTime();
        float estimatedAngle = calculateRotation(stopTime);
        estimateStopedAngle = estimatedAngle;

        int direction = -Math.Sign(body.angularVelocity);
        float velocityError = rotationStrength * Time.fixedDeltaTime;
        vError = velocityError;
        if(body.angularVelocity > velocityError || body.angularVelocity < -velocityError){
            appliedForce = direction * rotationStrength;
            body.AddTorque(appliedForce, ForceMode2D.Force);
        }
        else{
            body.angularVelocity = 0;
            TestTurnToAngle = false;
        }

    }
    void stopRotation(){
        body.angularVelocity = 0;
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

    float calculateRotationalConvergenceTime(){
        return  Math.Abs(body.angularVelocity) / rotationStrength;
    }
    float calculateRotation(float time){
        return time*((body.angularVelocity) + (.5f)*(rotationStrength)*(time));
    }
}
