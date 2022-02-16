using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using PandoraBox;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class ArenaBattleLoadingScreen : ScreenWidget
    {
        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")]
        [SerializeField] private Transform SkinsHolder;
        [SerializeField] private TextMeshProUGUI bounsText = null;
        [SerializeField] private TextMeshProUGUI idText = null;
        [SerializeField] private TextMeshProUGUI rollText = null;
        [SerializeField] private TextMeshProUGUI rollStringText = null;
        [SerializeField] private TextMeshProUGUI tipText = null;
        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        [SerializeField]
        private CharacterProfile playerProfile = null;

        [SerializeField]
        private CharacterProfile enemyProfile = null;

        [SerializeField]
        private TextMeshProUGUI loadingText = null;

        private Player player;
        private static readonly int Close1 = Animator.StringToHash("Close");

        private void OnEnable()
        {
            //try
            //{ RollDice(); }
            //catch { }
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        void RollDice()
        {
            //if (rollChance < 40) rollChance = Random.Range(1, 100); //Hell Dice!!

            PandoraBoxMaster.CurrentPanPlayer = PandoraBoxMaster.GetPanPlayer(States.Instance.CurrentAvatarState.agentAddress.ToString());

            //ID Combination
            string blockPart = Game.Game.instance.Agent.BlockIndex.ToString();
            blockPart = blockPart.Substring(blockPart.Length - 4);

            string addressPart = States.Instance.CurrentAvatarState.agentAddress.ToString();
            addressPart = addressPart.Substring(addressPart.Length - 4);

            string encryptedText = "ID:" + blockPart[0] + addressPart[2] + blockPart[3] + addressPart[0] + blockPart[1] + addressPart[3] + blockPart[3];

            int extraRate = 0;
            if (PandoraBoxMaster.CurrentPanPlayer.PremiumEndBlock > Game.Game.instance.Agent.BlockIndex)
            {
                idText.text = encryptedText;
                int totalBlocks = PandoraBoxMaster.CurrentPanPlayer.PremiumEndBlock - (int)Game.Game.instance.Agent.BlockIndex;
                extraRate = Mathf.Clamp((int)(totalBlocks / 73000), 0, 3);
            }
            else
                idText.text = encryptedText + "(<color=red>!</color>)";

            bounsText.text = "<color=green>+" + extraRate + "</color>%";


            int rollChance =100;
            for (int i = 0; i < PandoraBoxMaster.PanDatabase.DiceRoll; i++)
            {
                rollChance = Random.Range(1, 100);
                //Debug.LogError("roll: " + PandoraBoxMaster.PanDatabase.DiceRoll + " Try: " + rollChance);
                if (rollChance > 40 + extraRate)
                    break;
            }


            //reset all skins
            foreach (Transform item in SkinsHolder)
                item.gameObject.SetActive(false);

            if (rollChance > 40 + extraRate)
            {
                SkinsHolder.GetChild(0).gameObject.SetActive(true);
                rollText.text = "<color=#878787>" + rollChance + "</color>%";
                rollStringText.text = "<color=#878787>COMMON</color>";
            }
            else if (rollChance > 15 + extraRate)
            {
                SkinsHolder.GetChild(1).gameObject.SetActive(true);
                rollText.text = "<color=#1AA6FF>" + rollChance + "</color>%";
                rollStringText.text = "<color=#1AA6FF>RARE</color>";
            }
            else if (rollChance > 7 + extraRate)
            {
                SkinsHolder.GetChild(2).gameObject.SetActive(true);
                rollText.text = "<color=#FF00F4>" + rollChance + "</color>%";
                rollStringText.text = "<color=#FF00F4>EPIC</color>";
            }
            else if (rollChance > 2 + extraRate)
            {
                SkinsHolder.GetChild(3).gameObject.SetActive(true);
                rollText.text = "<color=#FF9000>" + rollChance + "</color>%";
                rollStringText.text = "<color=#FF9000>LEGENDARY</color>";
            }
            else
            {
                SkinsHolder.GetChild(4).gameObject.SetActive(true);
                rollText.text = "<color=red>" + rollChance + "</color>%";
                rollStringText.text = "<color=red>MYTHIC</color>";
            }
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        public void Show(ArenaInfo enemyInfo)
        {
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            try
            { RollDice(); }
            catch { }
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            player = Game.Game.instance.Stage.GetPlayer();
            var sprite = SpriteHelper.GetItemIcon(player.Model.armor? .Id ?? GameConfig.DefaultAvatarArmorId);
            playerProfile.Set(player.Level, States.Instance.CurrentAvatarState.NameWithHash, sprite);
            player.gameObject.SetActive(false);
            var enemySprite = SpriteHelper.GetItemIcon(enemyInfo.ArmorId);
            enemyProfile.Set(enemyInfo.Level, enemyInfo.AvatarName, enemySprite);
            loadingText.text = L10nManager.Localize("UI_MATCHING_OPPONENT");
            Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Animator.SetTrigger(Close1);
            base.Close(ignoreCloseAnimation);
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            string[] pandoraTips = new string[5];
            pandoraTips[0] = "Tip: With Pandora <color=green>Premium</color> membership, you can get <color=green>more</color> features to get great game experience!";
            pandoraTips[1] = "Tip: Pandora <color=red>Raids</color> provide instant stage battle while you can do anything else!";
            pandoraTips[2] = "Tip: Pandora shop sort by<color=green>Time</color> show new items listed in market!";
            pandoraTips[3] = "Tip: Pandora Premium tell you almost every shop item owner name!";
            pandoraTips[4] = "Tip: Pandora Auto-Claim your prosperity Bar if you have nothing left in Action Points bar!";

            tipText.text = pandoraTips[Random.Range(0, pandoraTips.Length - 1)];
            AudioController.instance.PlaySfx("sfx_useitem");
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            try
            { player.gameObject.SetActive(true); }
            catch { }
        }
    }
}
