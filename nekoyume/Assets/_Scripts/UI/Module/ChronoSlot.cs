using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using Bencodex.Types;
    using Nekoyume.PandoraBox;
    using Nekoyume.UI.Scroller;
    using UniRx;

    public class ChronoSlot : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI AvatarNameText;
        [SerializeField] private TextMeshProUGUI AvatarAddressText;
        [SerializeField] private TextMeshProUGUI APText;
        [SerializeField] private TextMeshProUGUI ArenaText;
        [SerializeField] private Image hasNotificationImage;
        private AvatarState currentAvatarState;
        private int _slotIndex;

        private void Awake()
        {
        }

        private void OnEnable()
        {
            //GetState();
        }

        public async void GetState()
        {
            var (exist, avatarState) = await States.TryGetAvatarStateAsync(States.Instance.CurrentAvatarState.address);
            currentAvatarState = avatarState;
        }

        public void SetSlot(long currentBlockIndex, int slotIndex, AvatarState state = null)
        {
            _slotIndex = slotIndex;
            currentAvatarState = state;
            UpdateInformation(currentBlockIndex, state);
        }

        private void UpdateInformation(long currentBlockIndex, AvatarState state)
        {
            AvatarNameText.text = state.NameWithHash;
            AvatarAddressText.text = state.address.ToString();
            APText.text = "Action Points: " + state.actionPoint.ToString() + "/120";


            UpdateArena(state, currentBlockIndex);
            UpdateNotification(state, currentBlockIndex);
        }

        private void UpdateNotification(AvatarState state, long currentBlockIndex)
        {
            hasNotificationImage.enabled = state.actionPoint > 0;
        }

        private async void UpdateArena(AvatarState state, long currentBlockIndex)
        {
            var currentAddress = state?.address;
            ArenaInfo arenaInfo = null;
            if (currentAddress != null)
            {
                var avatarAddress = currentAddress.Value;
                if (Game.Game.instance.Agent.BlockIndex >= RankingBattle.UpdateTargetBlockIndex)
                {
                    var infoAddress = States.Instance.WeeklyArenaState.address.Derive(avatarAddress.ToByteArray());
                    var rawInfo = await Game.Game.instance.Agent.GetStateAsync(infoAddress);
                    if (rawInfo is Dictionary dictionary)
                    {
                        arenaInfo = new ArenaInfo(dictionary);
                    }
                }
                else
                {
                    arenaInfo = States.Instance.WeeklyArenaState.GetArenaInfo(currentAddress.Value);
                }
            }

            if (currentAddress == null || arenaInfo == null)
                return;
            //Debug.LogError(arenaInfo.AvatarName + "  " + arenaInfo.DailyChallengeCount);

            var gameConfigState = States.Instance.GameConfigState;
            float maxTime = States.Instance.GameConfigState.DailyArenaInterval;
            var weeklyArenaState = States.Instance.WeeklyArenaState;
            long _resetIndex = weeklyArenaState.ResetIndex;
            float value;

            value = Game.Game.instance.Agent.BlockIndex - _resetIndex;
            var remainBlock = gameConfigState.DailyArenaInterval - value;
            var time = Util.GetBlockToTime((int)remainBlock);


            if (PandoraBoxMaster.Instance.Settings.BlockShowType == 0)
                ArenaText.text = "Arena : " + time + $" ({arenaInfo.DailyChallengeCount.ToString()}/5)";
            else if (PandoraBoxMaster.Instance.Settings.BlockShowType == 1)
                ArenaText.text = "Arena : " + $"({value}/{gameConfigState.DailyArenaInterval})" +
                                 $" ({arenaInfo.DailyChallengeCount.ToString()}/5)";
            else
                ArenaText.text = "Arena : " + $"{time} ({remainBlock})" +
                                 $" ({arenaInfo.DailyChallengeCount.ToString()}/5)";
        }
    }
}