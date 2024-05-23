using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LineController : MonoBehaviour
{
    LineRenderer lr;
    private Vector3[] points;
    // Start is called before the first frame update
    void Start()
    {
        lr = GetComponent<LineRenderer>();
        points = new Vector3[2];
    }
    

    // Update is called once per frame
    void Update()
    {
        for(int i=0; i < points.Length; i++){
            lr.SetPosition(i, points[i]);
        }
    }
    public void setActive(bool b = true){
        gameObject.SetActive(b);

    }
    public void setLine(Vector3 pos1, Vector3 pos2){
        lr.positionCount = 2;
        points[0] = pos1;
        points[1] = pos2;

    }
}
