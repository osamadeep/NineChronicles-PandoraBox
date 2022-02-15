using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class ArenaBattleLoadingScreen : ScreenWidget
    {
        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")]
        [SerializeField] private Transform SkinsHolder;
        [SerializeField] private TextMeshProUGUI rollText = null;
        [SerializeField] private TextMeshProUGUI rollStringText = null;
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

        public void Show(ArenaInfo enemyInfo)
        {
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            int x = Random.Range(1, 100);

            //reset all skins
            foreach (Transform item in SkinsHolder)
                item.gameObject.SetActive(false);

            if (x >= 65)
            {
                SkinsHolder.GetChild(0).gameObject.SetActive(true);
                rollText.text = "<color=#878787>" + x + "</color>%";
                rollStringText.text = "<color=#878787>COMMON</color>";
            }
            else if (x >= 35)
            {
                SkinsHolder.GetChild(1).gameObject.SetActive(true);
                rollText.text = "<color=#1AA6FF>" + x + "</color>%";
                rollStringText.text = "<color=#1AA6FF>RARE</color>";
            }
            else if (x >= 15)
            {
                SkinsHolder.GetChild(2).gameObject.SetActive(true);
                rollText.text = "<color=#FF00F4>" + x + "</color>%";
                rollStringText.text = "<color=#FF00F4>EPIC</color>";
            }
            else
            {
                SkinsHolder.GetChild(3).gameObject.SetActive(true);
                rollText.text = "<color=#FF9000>" + x + "</color>%";
                rollStringText.text = "<color=#FF9000>LEGENDARY</color>";
            }

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
            try
            { player.gameObject.SetActive(true); }
            catch { }
        }
    }
}
