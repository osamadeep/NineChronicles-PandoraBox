using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SliderChangeText : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI sweepNumber;

    Slider sldr;

    // Start is called before the first frame update
    void Start()
    {
        sldr = GetComponent<Slider>();
    }

    public void ChangeText()
    {
        sweepNumber.text = sldr.value.ToString();
    }
}
