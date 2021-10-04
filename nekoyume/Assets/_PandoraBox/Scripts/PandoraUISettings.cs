using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PandoraUISettings : MonoBehaviour
{
    int blockShowType;

    //Time Scale Elements
    [SerializeField]
    Image timeImage;

    [SerializeField]
    Image blockImage;

    [SerializeField]
    Image bothImage;

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
        blockShowType = PandoraBox.PandoraBoxMaster.Instance.Settings.BlockShowType;
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
        blockShowType = PandoraBox.PandoraBoxMaster.Instance.Settings.BlockShowType;
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
        PandoraBox.PandoraBoxMaster.Instance.Settings.BlockShowType = blockShowType;
        PandoraBox.PandoraBoxMaster.Instance.Settings.MenuSpeed = (int)menuSpeedSlider.value;
        PandoraBox.PandoraBoxMaster.Instance.Settings.FightSpeed = (int)fightSpeedSlider.value;
        PandoraBox.PandoraBoxMaster.Instance.Settings.ArenaListUpper = (int)arenaUpSlider.value;
        PandoraBox.PandoraBoxMaster.Instance.Settings.ArenaListLower = (int)arenaLoSlider.value;

        PandoraBox.PandoraBoxMaster.Instance.Settings.Save();
        gameObject.SetActive(false);
    }

    public void ChangeTimeScale(int value)
    {
        blockShowType = value;    
        LoadTimeScale();
    }

    void LoadTimeScale()
    {
        timeImage.color = blockShowType  == 0 ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        blockImage.color = blockShowType == 1 ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        bothImage.color = blockShowType == 2 ? Color.white : new Color(0.5f, 0.5f, 0.5f);
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
        arenaUpText.text = (90 + (PandoraBox.PandoraBoxMaster.Instance.Settings.ArenaListStep * (int)arenaUpSlider.value)).ToString();
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
