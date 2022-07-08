using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using Nekoyume.Model.BattleStatus;
using Nekoyume.PandoraBox;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class RankingBattleResultPopup : PopupWidget
    {
        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")]
        [SerializeField] private TextButton MenuButton = null;
        [SerializeField] private TextMeshProUGUI BounsPointsTxt = null;

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        [SerializeField]
        private GameObject victoryImageContainer = null;

        [SerializeField] private GameObject defeatImageContainer = null;

        [SerializeField] private TextButton submitButton = null;

        [SerializeField] private TextMeshProUGUI scoreText = null;

        [SerializeField] private List<SimpleCountableItemView> rewards = null;

        private static readonly Vector3 VfxBattleWinOffset = new Vector3(-0.05f, .25f, 10f);

        private System.Action _onClose;
        private System.Action _onCloseToMenu;

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = null;
            SubmitWidget = BackToRanking;
            submitButton.OnClick = BackToRanking;
            MenuButton.OnClick = BackToMenu;
        }

        public void Show(
            ArenaLog log,
            IReadOnlyList<ItemBase> rewardItems,
            System.Action onClose, System.Action onCloseToMenu)
        {
            base.Show();

            var win = log.Result == ArenaLog.ArenaResult.Win;
            var code = win
                ? AudioController.MusicCode.PVPWin
                : AudioController.MusicCode.PVPLose;
            AudioController.instance.PlayMusic(code);
            victoryImageContainer.SetActive(win);
            defeatImageContainer.SetActive(!win);
            if (win)
            {
                VFXController.instance.CreateAndChase<PVPVictoryVFX>(
                    ActionCamera.instance.transform, VfxBattleWinOffset);
            }

            scoreText.text = $"{log.Score}";
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            int difference = log.Score - Find<ArenaBoard>().OldScore;
            //Debug.LogError(log.score + "  " + Find<RankingBoard>().OldScore + "  " + difference);
            if (difference <= 0)
                BounsPointsTxt.text = $"<color=red>{difference}";
            else
                BounsPointsTxt.text = $"<color=green>+{difference}";
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            var items = rewardItems.ToCountableItems();

            for (var i = 0; i < rewards.Count; i++)
            {
                var view = rewards[i];
                view.gameObject.SetActive(false);
                if (i < items.Count)
                {
                    view.SetData(items[i]);
                    view.gameObject.SetActive(true);
                }
            }

            _onClose = onClose;
            _onCloseToMenu = onCloseToMenu;
        }

        private void BackToRanking()
        {
            Close();
            _onClose?.Invoke();
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        public void BackToMenu()
        {
            Close();
            _onCloseToMenu?.Invoke();

            ////Game.Game.instance.Stage.KillAllCharacters();
            //Game.Game.instance.Stage.objectPool.ReleaseAll();
            //Game.Game.instance.IsInWorld = false;
            //ActionCamera.instance.SetPosition(0f, 0f);
            //ActionCamera.instance.Idle();
            //Find<ArenaJoin>().Close();
            //Find<ArenaBoard>().Close();
            //Find<ArenaBattle>().Close();
            //Close();
            ////Find<Menu>().Show(false);
            //Game.Event.OnRoomEnter.Invoke(true);
        }

        public void BackToArena()
        {
            return;
            Game.Game.instance.Stage.objectPool.ReleaseAll();
            Game.Game.instance.IsInWorld = false;
            ActionCamera.instance.SetPosition(0f, 0f);
            ActionCamera.instance.Idle();
            AudioController.instance.PlayMusic(AudioController.MusicCode.Ranking);
            //Find<RankingBoard>().waitingForLaodBlocker.SetActive(false);
            Close();
            //Find<RankingBoard>().gameObject.SetActive(true);
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
            Find<HeaderMenuStatic>().Show(true);
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
    }
}
