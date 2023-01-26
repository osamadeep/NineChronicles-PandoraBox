using Cysharp.Threading.Tasks;
using Nekoyume.Extensions;
using System;
using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module.WorldBoss;
using Nekoyume.UI.Scroller;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class WorldBossRankItemView : MonoBehaviour
    {
        [SerializeField]
        private Image portrait;

        [SerializeField]
        private Image rankImage;

        [SerializeField]
        private TextMeshProUGUI rankText;

        [SerializeField]
        private TextMeshProUGUI avatarName;

        [SerializeField]
        private TextMeshProUGUI address;

        [SerializeField]
        private TextMeshProUGUI level;

        [SerializeField]
        private TextMeshProUGUI cp;

        [SerializeField]
        private TextMeshProUGUI highScore;

        [SerializeField]
        private TextMeshProUGUI totalScore;

        [SerializeField]
        private Transform gradeContainer;

        [SerializeField]
        private GameObject rankImageContainer;

        [SerializeField]
        private GameObject rankTextContainer;

        [SerializeField]
        public TouchHandler touchHandler;

        private GameObject _gradeObject;
        private GameObject _rankObject;
        private readonly List<IDisposable> _disposables = new();

        public void Set(WorldBossRankItem model, WorldBossRankScroll.ContextModel context)
        {
            if (model is null)
            {
                return;
            }

            if (_gradeObject != null)
            {
                Destroy(_gradeObject);
            }

            if (_rankObject != null)
            {
                Destroy(_rankObject);
            }

            var grade = (WorldBossGrade)WorldBossHelper.CalculateRank(model.BossRow, model.HighScore);
            if (WorldBossFrontHelper.TryGetGrade(grade, true, out var prefab))
            {
                _gradeObject = Instantiate(prefab, gradeContainer);
            }

            portrait.sprite = SpriteHelper.GetItemIcon(model.Portrait);

            avatarName.text = model.AvatarName;
            address.text = $"#{model.Address[..4]}" + $", (?/3)";
            level.text = $"{model.Level}";
            cp.text = $"{model.Cp:#,0}";
            highScore.text = $"{model.HighScore:#,0}";
            totalScore.text = $"{model.TotalScore:#,0}";

            rankImageContainer.SetActive(false);
            rankTextContainer.SetActive(false);
            if (model.Ranking > 3)
            {
                rankTextContainer.SetActive(true);
                rankText.text = $"{model.Ranking}";
            }
            else
            {
                rankImageContainer.SetActive(true);
                var rankPrefab = WorldBossFrontHelper.GetRankPrefab(model.Ranking);
                _rankObject = Instantiate(rankPrefab, rankImageContainer.transform);
            }
            if (touchHandler != null)
            {
                _disposables.DisposeAllAndClear();
                touchHandler.OnClick.Select(_ => model)
                    .Subscribe(context.OnClick.OnNext).AddTo(_disposables);

            }

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            //guild name
            var enemyGuildPlayer = PandoraBox.PandoraMaster.PanDatabase.GuildPlayers.Find(x => x.AvatarAddress.ToLower() == "0x" + model.Address.ToString().ToLower());
            if (!(enemyGuildPlayer is null))
                avatarName.text = $"<color=#8488BC>[</color><color=#228232>{enemyGuildPlayer.Guild}</color><color=#8488BC>]</color> {model.AvatarName}";

            GetRemainingRickets(model).Forget();
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        private async UniTask GetRemainingRickets(WorldBossRankItem model)
        {
            var avatarAddress = new Libplanet.Address(model.Address);
            var bossSheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;

            WorldBossListSheet.Row raidRow = new WorldBossListSheet.Row();
            var isOnSeason = false;

            try
            {
                raidRow = bossSheet.FindRowByBlockIndex(blockIndex);
                isOnSeason = true;
            }
            catch
            {
                address.text = $"#{model.Address[..4]}" + $", (?/3)";
            }

            var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, raidRow.Id);
            var raiderState = await Game.Game.instance.Agent.GetStateAsync(raiderAddress);
            var raider = raiderState is Bencodex.Types.List raiderList ? new RaiderState(raiderList) : null;

            try
            {
                address.text = $"#{model.Address[..4]}" + $", ({raider.RemainChallengeCount}/3), T:{raider.TotalChallengeCount}";
            }
            catch
            {
                address.text = $"#{model.Address[..4]}" + $", (?/3), T:?";
            }
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
    }
}
