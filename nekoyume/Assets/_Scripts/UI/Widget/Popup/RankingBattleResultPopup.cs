using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using Nekoyume.Model.BattleStatus;
using Nekoyume.PandoraBox;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class RankingBattleResultPopup : PopupWidget
    {
        [SerializeField]
        private GameObject victoryImageContainer = null;

        [SerializeField]
        private GameObject defeatImageContainer = null;

        [SerializeField]
        private TextButton submitButton = null;

        [SerializeField]
        private TextMeshProUGUI scoreText = null;

        [SerializeField]
        private List<SimpleCountableItemView> rewards = null;

        private static readonly Vector3 VfxBattleWinOffset = new Vector3(-0.05f, .25f, 10f);

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = null;
            SubmitWidget = BackToRanking;
            submitButton.OnClick = BackToRanking;
        }

        public void Show(BattleLog log, IReadOnlyList<CountableItem> reward)
        {
            base.Show();

            var win = log.result == BattleLog.Result.Win;
            var code = win ? AudioController.MusicCode.PVPWin : AudioController.MusicCode.PVPLose;
            AudioController.instance.PlayMusic(code);
            victoryImageContainer.SetActive(win);
            defeatImageContainer.SetActive(!win);
            if (win)
            {
                VFXController.instance.CreateAndChase<PVPVictoryVFX>(
                    ActionCamera.instance.transform, VfxBattleWinOffset);
            }

            scoreText.text = $"{log.score}";
            for (var i = 0; i < rewards.Count; i++)
            {
                var view = rewards[i];
                view.gameObject.SetActive(false);
                if (i < reward.Count)
                {
                    view.SetData(reward[i]);
                    view.gameObject.SetActive(true);
                }
            }

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            PandoraBoxMaster.IsRankingSimulate = false;
            PandoraBoxMaster.IsRanking = false;
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        private void BackToRanking()
        {
            Game.Game.instance.Stage.objectPool.ReleaseAll();
            Game.Game.instance.Stage.IsInStage = false;
            ActionCamera.instance.SetPosition(0f, 0f);
            ActionCamera.instance.Idle();
            Close();
            Find<RankingBoard>().Show(States.Instance.WeeklyArenaState);
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        public void BackToMenu()
        {
            //Game.Event.OnRoomEnter.Invoke(false);
            //MainCanvas.instance.InitWidgetInMain();
            //Close();

            //Game.Game.instance.Stage.KillAllCharacters();
            Game.Game.instance.Stage.objectPool.ReleaseAll();
            Game.Game.instance.Stage.IsInStage = false;
            ActionCamera.instance.SetPosition(0f, 0f);
            ActionCamera.instance.Idle();
            Find<Battle>().Close();
            Close();
            //Find<Menu>().Show(false);
            Game.Event.OnRoomEnter.Invoke(true);
        }

        public void BackToArena()
        {           
            Game.Game.instance.Stage.objectPool.ReleaseAll();
            Game.Game.instance.Stage.IsInStage = false;
            ActionCamera.instance.SetPosition(0f, 0f);
            ActionCamera.instance.Idle();
            AudioController.instance.PlayMusic(AudioController.MusicCode.Ranking);
            Find<RankingBoard>().waitingForLaodBlocker.SetActive(false);
            Close();
            Find<RankingBoard>().gameObject.SetActive(true);
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
            Find<HeaderMenuStatic>().Show(true);
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
    }
}
