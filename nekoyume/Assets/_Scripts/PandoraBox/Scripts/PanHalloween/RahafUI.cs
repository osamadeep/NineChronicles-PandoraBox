using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nekoyume.Game;
using UnityEngine.UI;
using Nekoyume.Helper;
using TMPro;

public class RahafUI : MonoBehaviour
{
    public Transform EggHolder;
    public Button StartButton;
    public TextMeshProUGUI startText;

    void OnEnable()
    {
        checkUnlocked();
        StartCoroutine(CooldownTimer());
    }

    IEnumerator CooldownTimer()
    {
        StartButton.interactable = false;
        while (true)
        {
            int HalloweenCooldown = PlayerPrefs.GetInt("_PandoraBox_Halloween_NextCooldown");
            float value = HalloweenCooldown - Game.instance.Agent.BlockIndex;
            var time = Util.GetBlockToTime((int)value);
            if (value > 0)
            {
                StartButton.interactable = false;
                startText.text = "Wait (" + time + ")";
            }
            else
            {
                StartButton.interactable = true;
                startText.text = "Start";
            }
            yield return new WaitForSeconds(5);
        }
    }

    void checkUnlocked()
    {
        foreach (Transform item in EggHolder)
        {
            item.GetComponent<Image>().color = Color.black;
        }

        int Unlocked = PlayerPrefs.GetInt("_PandoraBox_Halloween_Unlocked",0);

        for (int i = 0; i < Unlocked; i++)
        {
            EggHolder.GetChild(i).GetComponent<Image>().color = Color.white;
        }
    }
}
