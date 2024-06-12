using System;
using Unity.Mathematics;
using UnityEngine;

public abstract class BoidDrive : MonoBehaviour{
    protected Rigidbody2D body;
    public float torqueStrength{
        get;
        set;
    }    
    public float thrustStrength{
        get;
        set;
    }

    public BoidDrive(Rigidbody2D body){
        this.body = body;
    }
    public abstract void setAngle(Vector2 direction);
    public abstract void setVector(Vector2 vector);
    public abstract void setPoint();
}



