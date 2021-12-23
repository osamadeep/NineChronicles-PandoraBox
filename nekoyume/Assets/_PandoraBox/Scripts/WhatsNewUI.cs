using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhatsNewUI : MonoBehaviour
{
    public void NewShown()
    {
        PandoraBox.PandoraBoxMaster.Instance.Settings.WhatsNewShown = true;
        PlayerPrefs.SetInt("_PandoraBox_General_WhatsNewShown", 1);
        gameObject.SetActive(false);
    }    
}
