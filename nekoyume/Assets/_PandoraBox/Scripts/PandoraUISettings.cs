using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PandoraUISettings : MonoBehaviour
{
    bool isTimeOverBlock;

    //Time Scale Elements
    [SerializeField]
    Image timeImage;

    [SerializeField]
    Image blockImage;

    //Menu Speed Elements
    [SerializeField]
    TextMeshProUGUI menuSpeedText;

    [SerializeField]
    Slider menuSpeedSlider;

    //Fight Speed Elements
    [SerializeField]
    TextMeshProUGUI fightSpeedText;

    [SerializeField]
    Slider fightSpeedSlider;

    //Arena Speed Elements
    [SerializeField]
    TextMeshProUGUI arenaUpText;
    [SerializeField]
    TextMeshProUGUI arenaLoText;

    [SerializeField]
    Slider arenaUpSlider;
    [SerializeField]
    Slider arenaLoSlider;


    void OnEnable()
    {
        if (PandoraBox.PandoraBoxMaster.Instance == null)
            return;

        //Load settings
        isTimeOverBlock = PandoraBox.PandoraBoxMaster.Instance.Settings.IsTimeOverBlock;
        LoadTimeScale();
        menuSpeedSlider.value = PandoraBox.PandoraBoxMaster.Instance.Settings.MenuSpeed;
        LoadMenuSpeed();
        fightSpeedSlider.value = PandoraBox.PandoraBoxMaster.Instance.Settings.FightSpeed;
        LoadFightSpeed();
        arenaUpSlider.value = PandoraBox.PandoraBoxMaster.Instance.Settings.ArenaListUpper;
        LoadArenaUp();
        arenaLoSlider.value = PandoraBox.PandoraBoxMaster.Instance.Settings.ArenaListLower;
        LoadArenaLo();
    }

    public void ResetDefault()
    {
        PandoraBox.PandoraBoxMaster.Instance.Settings = new PandoraBox.PandoraSettings();
        PandoraBox.PandoraBoxMaster.Instance.Settings.Save();

        //Load settings
        isTimeOverBlock = PandoraBox.PandoraBoxMaster.Instance.Settings.IsTimeOverBlock;
        LoadTimeScale();
        menuSpeedSlider.value = PandoraBox.PandoraBoxMaster.Instance.Settings.MenuSpeed;
        LoadMenuSpeed();
        fightSpeedSlider.value = PandoraBox.PandoraBoxMaster.Instance.Settings.FightSpeed;
        LoadFightSpeed();
        arenaUpSlider.value = PandoraBox.PandoraBoxMaster.Instance.Settings.ArenaListUpper;
        LoadArenaUp();
        arenaLoSlider.value = PandoraBox.PandoraBoxMaster.Instance.Settings.ArenaListLower;
        LoadArenaLo();
    }

    private void Start()
    {

    }

    public void SaveSettings()
    {
        PandoraBox.PandoraBoxMaster.Instance.Settings.IsTimeOverBlock = isTimeOverBlock ? true : false;
        PandoraBox.PandoraBoxMaster.Instance.Settings.MenuSpeed = (int)menuSpeedSlider.value;
        PandoraBox.PandoraBoxMaster.Instance.Settings.FightSpeed = (int)fightSpeedSlider.value;
        PandoraBox.PandoraBoxMaster.Instance.Settings.ArenaListUpper = (int)arenaUpSlider.value;
        PandoraBox.PandoraBoxMaster.Instance.Settings.ArenaListLower = (int)arenaLoSlider.value;

        PandoraBox.PandoraBoxMaster.Instance.Settings.Save();
        gameObject.SetActive(false);
    }

    public void ChangeTimeScale(bool isTime)
    {
        isTimeOverBlock = isTime;    
        LoadTimeScale();
    }

    void LoadTimeScale()
    {
        timeImage.color = isTimeOverBlock ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        blockImage.color = isTimeOverBlock ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
    }

    public void ChangeMenuSpeed()
    {      
        LoadMenuSpeed();
    }

    public void LoadMenuSpeed()
    {
        menuSpeedText.text = "Menu Speed : " + (int)(menuSpeedSlider.value * 100) + "%";     
    }

    public void ChangeFightSpeed()
    {
        LoadFightSpeed();
    }

    public void LoadFightSpeed()
    {
        fightSpeedText.text = "Fight Speed : X" + (int)fightSpeedSlider.value;
    }

    public void ChangeArenaUp()
    {
        LoadArenaUp();
    }
    public void LoadArenaUp()
    {
        arenaUpText.text = (20 + (PandoraBox.PandoraBoxMaster.Instance.Settings.ArenaListStep * (int)arenaUpSlider.value)).ToString();
    }

    public void ChangeArenaLo()
    {
        LoadArenaLo();
    }

    public void LoadArenaLo()
    {
        arenaLoText.text = (20 + (PandoraBox.PandoraBoxMaster.Instance.Settings.ArenaListStep * (int)arenaLoSlider.value)).ToString();
    }
}
