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
            if (PandoraMaster.Instance == null)
                return;

            try
            { nodeText.text = "Connected Node: <color=green>" + Game.Game.instance._options.RpcServerHost + "</color>"; }catch { }

            //Load settings
            blockShowType = PandoraMaster.Instance.Settings.BlockShowType;
            LoadTimeScale();
            menuSpeedSlider.value = PandoraMaster.Instance.Settings.MenuSpeed;
            LoadMenuSpeed();
            fightSpeedSlider.value = PandoraMaster.Instance.Settings.FightSpeed;
            LoadFightSpeed();
            arenaUpSlider.value = PandoraMaster.Instance.Settings.ArenaListUpper;
            LoadArenaUp();
            arenaLoSlider.value = PandoraMaster.Instance.Settings.ArenaListLower;
            LoadArenaLo();
            LoadRaidMethod();
            LoadMultipleLogin();
            LoadIntroStory();
        }

        public void ResetDefault()
        {
            PandoraMaster.Instance.Settings = new PandoraSettings();
            PandoraMaster.Instance.Settings.Save();

            //Load settings
            blockShowType = PandoraMaster.Instance.Settings.BlockShowType;
            LoadTimeScale();
            menuSpeedSlider.value = PandoraMaster.Instance.Settings.MenuSpeed;
            LoadMenuSpeed();
            fightSpeedSlider.value = PandoraMaster.Instance.Settings.FightSpeed;
            LoadFightSpeed();
            arenaUpSlider.value = PandoraMaster.Instance.Settings.ArenaListUpper;
            LoadArenaUp();
            arenaLoSlider.value = PandoraMaster.Instance.Settings.ArenaListLower;
            LoadArenaLo();
            LoadRaidMethod();
            LoadMultipleLogin();
            LoadIntroStory();
        }

        public void SaveSettings()
        {
            PandoraMaster.Instance.Settings.BlockShowType = blockShowType;
            PandoraMaster.Instance.Settings.MenuSpeed = (int)menuSpeedSlider.value;
            PandoraMaster.Instance.Settings.FightSpeed = (int)fightSpeedSlider.value;
            PandoraMaster.Instance.Settings.ArenaListUpper = (int)arenaUpSlider.value;
            PandoraMaster.Instance.Settings.ArenaListLower = (int)arenaLoSlider.value;

            PandoraMaster.Instance.Settings.Save();
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
            PandoraMaster.Instance.Settings.RaidMethodIsProgress = value;
            LoadRaidMethod();
        }

        void LoadRaidMethod()
        {
            progressImage.color = PandoraMaster.Instance.Settings.RaidMethodIsProgress ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            farmImage.color = !PandoraMaster.Instance.Settings.RaidMethodIsProgress ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeMultipleLogin(bool value)
        {
            PandoraMaster.Instance.Settings.IsMultipleLogin = value;
            LoadMultipleLogin();
        }

        void LoadMultipleLogin()
        {
            multiLogOnImage.color = PandoraMaster.Instance.Settings.IsMultipleLogin ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            multiLogOffImage.color = !PandoraMaster.Instance.Settings.IsMultipleLogin ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeIntroStory(bool value)
        {
            PandoraMaster.Instance.Settings.IsStory = value;
            LoadIntroStory();
        }

        void LoadIntroStory()
        {
            introStoryOnImage.color = PandoraMaster.Instance.Settings.IsStory ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            introStoryOffImage.color = !PandoraMaster.Instance.Settings.IsStory ? Color.white : new Color(0.5f, 0.5f, 0.5f);
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
            arenaUpText.text = (10 + (PandoraMaster.Instance.Settings.ArenaListStep * (int)arenaUpSlider.value)).ToString();
        }

        public void ChangeArenaLo()
        {
            LoadArenaLo();
        }

        public void LoadArenaLo()
        {
            arenaLoText.text = (10 + (PandoraMaster.Instance.Settings.ArenaListStep * (int)arenaLoSlider.value)).ToString();
        }
    }
}
