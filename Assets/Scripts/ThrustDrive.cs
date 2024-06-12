using System;
using Unity.Mathematics;
using UnityEngine;
public class ThrustDrive : BoidDrive 
{
    public ThrustDrive(Rigidbody2D body) : base(body)
    {

    }

    public override void setAngle(Vector2 targetAngle)
    {
        float deltaAngle = -Vector2.SignedAngle(targetAngle, ThrustUnitVector());

        float stopTime = estimatedAngularBreakTime();
        float estimatedDeltaAngle = estimatedAngularBreakRotation(stopTime);
        int direction = System.Math.Sign(deltaAngle);
        float angularVelocityError = torqueStrength * Time.fixedDeltaTime;
        float appliedForce;
        float deltaDeltaAngle = Mathf.DeltaAngle(deltaAngle, estimatedDeltaAngle);

        float maxAngularVelocity = 400;
        float maxAngleError = 5;

        if(math.sign(deltaAngle) != math.sign(estimatedDeltaAngle)){
            Debug.Log(string.Format("deltaAngle: {0}\n estimatedDeltaAngle: {1}",deltaAngle, estimatedDeltaAngle));
        }

        if(math.abs(body.angularVelocity) > maxAngularVelocity){
            stopRotationManual();
        }
        else if( math.sign(deltaDeltaAngle - direction*maxAngleError) != direction){
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

    private void stopRotationManual()
    {
        throw new NotImplementedException();
    }

    public override void setVector(Vector2 vector)
    {
        throw new System.NotImplementedException();
    }
        public override void setPoint()
    {
        throw new System.NotImplementedException();
    }

    float estimatedAngularBreakTime(){
        return  math.abs(body.angularVelocity) / torqueStrength;
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
        float flipTime = estimatedTurnTime(180);
        float scalar = body.velocity.magnitude*flipTime + time*(body.velocity.magnitude - (.5f)*(thrustStrength)*(time));
        return body.velocity.normalized*scalar;
    }
    Vector2 ThrustUnitVector(float addAngle = 0)
    {
        // get real rotation
        float angle = OrientationRadians() + addAngle;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
    float OrientationRadians()
    {
        return (body.rotation + 90) * Mathf.Deg2Rad;
    }
}