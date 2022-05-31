using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleRotate1 : MonoBehaviour
{
    public Vector3 axis;
    public float speed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(transform.rotation.y);
        if ( Mathf.Abs(transform.rotation.y) >90f/360f && Mathf.Abs(transform.rotation.y) > 250f / 360f )
            transform.Rotate(axis * speed*5 * Time.deltaTime);
        else
            transform.Rotate(axis * speed * Time.deltaTime);
    }
}
