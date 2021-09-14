using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeScaleReset : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetTime()
    {
        Time.timeScale = 1;
        Debug.Log("Time Scale is 1 Now!");
    }
}
