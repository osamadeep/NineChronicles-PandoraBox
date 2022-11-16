using Cysharp.Threading.Tasks;
using Nekoyume.Extensions;
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

        private GameObject _gradeObject;
        private GameObject _rankObject;

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

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            var raiderState = WorldBossStates.GetRaiderState(new Libplanet.Address(model.Address));
            var RemainTicket = WorldBossFrontHelper.GetRemainTicket(raiderState, Game.Game.instance.Agent.BlockIndex);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
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
            GetRemainingRickets(model).Forget();
        }

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
            var raider = raiderState is Bencodex.Types.List raiderList
                ? new RaiderState(raiderList)
                : null;

            var killRewardAddress = Addresses.GetWorldBossKillRewardRecordAddress(avatarAddress, raidRow.Id);
            var killRewardState = await Game.Game.instance.Agent.GetStateAsync(killRewardAddress);
            var killReward = killRewardState is Bencodex.Types.List killRewardList
                ? new WorldBossKillRewardRecord(killRewardList)
                : null;

            
            address.text = $"#{model.Address[..4]}" + $", ({raider.RemainChallengeCount}/3), T:{raider.TotalChallengeCount}";
        }
    }
}
