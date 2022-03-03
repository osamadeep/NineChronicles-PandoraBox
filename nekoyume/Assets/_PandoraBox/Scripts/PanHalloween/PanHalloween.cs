using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nekoyume.Game;
using TMPro;
using UnityEngine.Audio;
using Nekoyume.UI;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;

public class PanHalloween : MonoBehaviour
{
    public static PanHalloween Instance;
    public Transform[] Eggs;
    public AudioMixer Audiomixer;
    public int RemainingTimer;
    int NextCooldown;
    int ResetBlock = 25;

    public TextMeshProUGUI CountText;
    public TextMeshProUGUI RemainingText;

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!PlayerPrefs.HasKey("_PandoraBox_Halloween_NextCooldown"))
        {
            int NextCooldown = (int)Game.instance.Agent.BlockIndex + ResetBlock; //25 = 5m
            PlayerPrefs.SetInt("_PandoraBox_Halloween_NextCooldown", NextCooldown);
        }

        NextCooldown = PlayerPrefs.GetInt("_PandoraBox_Halloween_NextCooldown");
    }

    public void StartHalloween()
    {
        NextCooldown = (int)Game.instance.Agent.BlockIndex + ResetBlock;
        PlayerPrefs.SetInt("_PandoraBox_Halloween_NextCooldown", NextCooldown);
        StartCoroutine(CountDown());
    }

    IEnumerator CountDown()
    {
        RemainingTimer = 90;
        RemainingText.gameObject.SetActive(true);
        CountText.text = "0/8";
        RemainingText.text = RemainingTimer.ToString();
        float musicVolume;
        Audiomixer.GetFloat("MusicVolume", out musicVolume);
        Audiomixer.SetFloat("MusicVolume", -80f);

        Eggs[0].gameObject.SetActive(true);
        while (RemainingTimer > 0)
        {
            yield return new WaitForSeconds(1);
            RemainingTimer--;
            RemainingText.text = RemainingTimer.ToString();
        }
        NextCooldown = (int)Game.instance.Agent.BlockIndex + ResetBlock;
        PlayerPrefs.SetInt("_PandoraBox_Halloween_NextCooldown", NextCooldown);
        Audiomixer.SetFloat("MusicVolume", musicVolume);
        OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: You <color=red>Failed</color>, try again!"
            , NotificationCell.NotificationType.Information);
        gameObject.SetActive(false);

    }

    public void FoundOne(int newEgg)
    {
        int oldEgges = PlayerPrefs.GetInt("_PandoraBox_Halloween_Unlocked", 0);
        CountText.text = newEgg + "/8";
        if (newEgg > oldEgges)
            PlayerPrefs.SetInt("_PandoraBox_Halloween_Unlocked", newEgg);
        Eggs[newEgg - 1].gameObject.SetActive(false);
        //Show next
        if (newEgg < 8)
            Eggs[newEgg].gameObject.SetActive(true);
        else
        {
            //event Complete
        }
    }
}
