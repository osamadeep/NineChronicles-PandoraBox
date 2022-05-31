using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleShake : MonoBehaviour
{
    public Vector3 axis;
    public int speed = 5;
    float currentScale;
    // Start is called before the first frame update
    void Start()
    {
        currentScale = transform.localScale.x;
        StartCoroutine(Shake());
    }

    IEnumerator Shake()
    {
        while (true)
        {

            float newScale;
            newScale = Random.Range(0.9f * currentScale, currentScale);
            float value = currentScale;
            while (value > newScale)
            {
                value -= Time.deltaTime * speed;
                transform.localScale = new Vector3(currentScale, currentScale, value);
                yield return null;
            }
            value = newScale;
            while (value <= currentScale)
            {
                value += Time.deltaTime * speed;
                transform.localScale = new Vector3(currentScale, currentScale, value);
                yield return null;
            }
        }
    }
}
