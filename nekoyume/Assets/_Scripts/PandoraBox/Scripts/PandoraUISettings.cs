using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.PandoraBox
{

    public class PandoraUISettings : MonoBehaviour
    {
        int blockShowType;

        //node connected
        [SerializeField]
        TextMeshProUGUI nodeText;

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

        //Raid Method
        [SerializeField]
        Image farmImage;

        [SerializeField]
        Image progressImage;

        //multiple login
        [SerializeField]
        Image multiLogOnImage;

        [SerializeField]
        Image multiLogOffImage;

        //intro story
        [SerializeField]
        Image introStoryOnImage;

        [SerializeField]
        Image introStoryOffImage;


        void OnEnable()
        {
            if (PandoraBoxMaster.Instance == null)
                return;

            try
            { nodeText.text = "Connected Node: <color=green>" + Game.Game.instance._options.RpcServerHost + "</color>"; }catch { }

            //Load settings
            blockShowType = PandoraBoxMaster.Instance.Settings.BlockShowType;
            LoadTimeScale();
            menuSpeedSlider.value = PandoraBoxMaster.Instance.Settings.MenuSpeed;
            LoadMenuSpeed();
            fightSpeedSlider.value = PandoraBoxMaster.Instance.Settings.FightSpeed;
            LoadFightSpeed();
            arenaUpSlider.value = PandoraBoxMaster.Instance.Settings.ArenaListUpper;
            LoadArenaUp();
            arenaLoSlider.value = PandoraBoxMaster.Instance.Settings.ArenaListLower;
            LoadArenaLo();
            LoadRaidMethod();
            LoadMultipleLogin();
            LoadIntroStory();
        }

        public void ResetDefault()
        {
            PandoraBoxMaster.Instance.Settings = new PandoraSettings();
            PandoraBoxMaster.Instance.Settings.Save();

            //Load settings
            blockShowType = PandoraBoxMaster.Instance.Settings.BlockShowType;
            LoadTimeScale();
            menuSpeedSlider.value = PandoraBoxMaster.Instance.Settings.MenuSpeed;
            LoadMenuSpeed();
            fightSpeedSlider.value = PandoraBoxMaster.Instance.Settings.FightSpeed;
            LoadFightSpeed();
            arenaUpSlider.value = PandoraBoxMaster.Instance.Settings.ArenaListUpper;
            LoadArenaUp();
            arenaLoSlider.value = PandoraBoxMaster.Instance.Settings.ArenaListLower;
            LoadArenaLo();
            LoadRaidMethod();
            LoadMultipleLogin();
            LoadIntroStory();
        }

        public void SaveSettings()
        {
            PandoraBoxMaster.Instance.Settings.BlockShowType = blockShowType;
            PandoraBoxMaster.Instance.Settings.MenuSpeed = (int)menuSpeedSlider.value;
            PandoraBoxMaster.Instance.Settings.FightSpeed = (int)fightSpeedSlider.value;
            PandoraBoxMaster.Instance.Settings.ArenaListUpper = (int)arenaUpSlider.value;
            PandoraBoxMaster.Instance.Settings.ArenaListLower = (int)arenaLoSlider.value;

            PandoraBoxMaster.Instance.Settings.Save();
            gameObject.SetActive(false);
        }

        public void ChangeTimeScale(int value)
        {
            blockShowType = value;
            LoadTimeScale();
        }

        void LoadTimeScale()
        {
            timeImage.color = blockShowType == 0 ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            blockImage.color = blockShowType == 1 ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            bothImage.color = blockShowType == 2 ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeRaidMethod(bool value)
        {
            PandoraBoxMaster.Instance.Settings.RaidMethodIsProgress = value;
            LoadRaidMethod();
        }

        void LoadRaidMethod()
        {
            progressImage.color = PandoraBoxMaster.Instance.Settings.RaidMethodIsProgress ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            farmImage.color = !PandoraBoxMaster.Instance.Settings.RaidMethodIsProgress ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeMultipleLogin(bool value)
        {
            PandoraBoxMaster.Instance.Settings.IsMultipleLogin = value;
            LoadMultipleLogin();
        }

        void LoadMultipleLogin()
        {
            multiLogOnImage.color = PandoraBoxMaster.Instance.Settings.IsMultipleLogin ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            multiLogOffImage.color = !PandoraBoxMaster.Instance.Settings.IsMultipleLogin ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeIntroStory(bool value)
        {
            PandoraBoxMaster.Instance.Settings.IsStory = value;
            LoadIntroStory();
        }

        void LoadIntroStory()
        {
            introStoryOnImage.color = PandoraBoxMaster.Instance.Settings.IsStory ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            introStoryOffImage.color = !PandoraBoxMaster.Instance.Settings.IsStory ? Color.white : new Color(0.5f, 0.5f, 0.5f);
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
            arenaUpText.text = (50 + (PandoraBoxMaster.Instance.Settings.ArenaListStep * (int)arenaUpSlider.value)).ToString();
        }

        public void ChangeArenaLo()
        {
            LoadArenaLo();
        }

        public void LoadArenaLo()
        {
            arenaLoText.text = (20 + (PandoraBoxMaster.Instance.Settings.ArenaListStep * (int)arenaLoSlider.value)).ToString();
        }
    }
}
