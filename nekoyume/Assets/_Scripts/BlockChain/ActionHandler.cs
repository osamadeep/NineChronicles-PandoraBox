using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Lib9c.Renderers;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    public abstract class ActionHandler
    {
        public bool Pending { get; set; }
        public Currency GoldCurrency { get; internal set; }

        public abstract void Start(ActionRenderer renderer);

        protected static bool HasUpdatedAssetsForCurrentAgent<T>(ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            if (States.Instance.AgentState is null)
            {
                return false;
            }

            return evaluation.OutputStates.UpdatedFungibleAssets.ContainsKey(States.Instance.CurrentAvatarState
                .agentAddress);
        }

        protected static bool ValidateEvaluationForCurrentAvatarState<T>(ActionEvaluation<T> evaluation)
            where T : ActionBase =>
            !(States.Instance.CurrentAvatarState is null)
            && evaluation.OutputStates.UpdatedAddresses.Contains(States.Instance.CurrentAvatarState.address);

        protected static bool ValidateEvaluationForCurrentAgent<T>(ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            return !(States.Instance.AgentState is null) &&
                   evaluation.Signer.Equals(States.Instance.CurrentAvatarState.agentAddress);
        }

        protected static AgentState GetAgentState<T>(ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            return evaluation.OutputStates.GetAgentState(agentAddress);
        }

        protected GoldBalanceState GetGoldBalanceState<T>(ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            var agentAddress = States.Instance.CurrentAvatarState.agentAddress;
            if (!evaluation.Signer.Equals(agentAddress) ||
                !evaluation.OutputStates.UpdatedFungibleAssets.TryGetValue(
                    evaluation.Signer,
                    out var currencies) ||
                !currencies.Contains(GoldCurrency))
            {
                return null;
            }

            return evaluation.OutputStates.GetGoldBalanceState(agentAddress, GoldCurrency);
        }

        protected (MonsterCollectionState, int, FungibleAssetValue) GetMonsterCollectionState<T>(
            ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.CurrentAvatarState.agentAddress;
            var monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                agentAddress,
                States.Instance.AgentState.MonsterCollectionRound
            );
            if (!(evaluation.OutputStates.GetState(monsterCollectionAddress) is Bencodex.Types.Dictionary mcDict))
            {
                return (null, 0, new FungibleAssetValue());
            }

            try
            {
                var balance =
                    evaluation.OutputStates.GetBalance(monsterCollectionAddress, GoldCurrency);
                var level =
                    TableSheets.Instance.StakeRegularRewardSheet.FindLevelByStakedAmount(
                        agentAddress, balance);
                return (new MonsterCollectionState(mcDict), level, balance);
            }
            catch (Exception)
            {
                return (null, 0, new FungibleAssetValue());
            }
        }

        protected (StakeState, int, FungibleAssetValue) GetStakeState<T>(
            ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            var agentAddress = States.Instance.CurrentAvatarState.agentAddress;
            var stakeAddress = StakeState.DeriveAddress(agentAddress);
            if (!(evaluation.OutputStates.GetState(stakeAddress) is Bencodex.Types.Dictionary serialized))
            {
                return (null, 0, new FungibleAssetValue());
            }

            try
            {
                var state = new StakeState(serialized);
                var balance = evaluation.OutputStates.GetBalance(
                    state.address,
                    GoldCurrency);
                var level = TableSheets.Instance.StakeRegularRewardSheet.FindLevelByStakedAmount(
                    agentAddress,
                    balance);
                return (state, level, balance);
            }
            catch (Exception)
            {
                return (null, 0, new FungibleAssetValue());
            }
        }

        protected static CrystalRandomSkillState GetCrystalRandomSkillState<T>(
            ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var buffStateAddress = Addresses.GetSkillStateAddressFromAvatarAddress(avatarAddress);
            if (evaluation.OutputStates.GetState(buffStateAddress) is
                Bencodex.Types.List serialized)
            {
                var state = new CrystalRandomSkillState(buffStateAddress, serialized);
                return state;
            }

            return null;
        }

        protected async UniTask UpdateAgentStateAsync<T>(
            ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            Debug.LogFormat(
                "Called UpdateAgentState<{0}>. Updated Addresses : `{1}`",
                evaluation.Action,
                string.Join(",", evaluation.OutputStates.UpdatedAddresses));
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            if (States.Instance.CurrentAvatarKey > 2)
                return;
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            await UpdateAgentStateAsync(GetAgentState(evaluation));
            try
            {
                UpdateGoldBalanceState(GetGoldBalanceState(evaluation));
            }
            catch (BalanceDoesNotExistsException)
            {
                UpdateGoldBalanceState(null);
            }

            UpdateCrystalBalance(evaluation);
        }

        protected static async UniTask UpdateAvatarState<T>(
            ActionEvaluation<T> evaluation,
            int index)
            where T : ActionBase
        {
            Debug.LogFormat(
                "Called UpdateAvatarState<{0}>. Updated Addresses : `{1}`",
                evaluation.Action,
                string.Join(",", evaluation.OutputStates.UpdatedAddresses));
            if (!States.Instance.AgentState.avatarAddresses.ContainsKey(index))
            {
                States.Instance.RemoveAvatarState(index);
                return;
            }

            var agentAddress = States.Instance.CurrentAvatarState.agentAddress;
            var avatarAddress = States.Instance.AgentState.avatarAddresses[index];
            if (evaluation.OutputStates.TryGetAvatarStateV2(
                    agentAddress,
                    avatarAddress,
                    out var avatarState,
                    out _))
            {
                await UpdateAvatarState(avatarState, index);
            }
        }

        protected async UniTask UpdateCurrentAvatarStateAsync<T>(
            ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            var agentAddress = States.Instance.CurrentAvatarState.agentAddress;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            try
            {
                await UpdateCurrentAvatarStateAsync(
                    States.Instance.CurrentAvatarState
                        .UpdateAvatarStateV2(avatarAddress, evaluation.OutputStates));
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Failed to Update AvatarState: {agentAddress}, {avatarAddress}\n{e.Message}");
            }
        }

        protected async UniTask UpdateCurrentAvatarStateAsync()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var avatars =
                await Game.Game.instance.Agent.GetAvatarStates(new[] { avatarAddress });
            if (avatars.TryGetValue(avatarAddress, out var avatarState))
            {
                await UpdateCurrentAvatarStateAsync(avatarState);
            }
            else
            {
                Debug.LogError($"Failed to get AvatarState: {avatarAddress}");
            }
        }

        protected static void UpdateGameConfigState<T>(ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var state = evaluation.OutputStates.GetGameConfigState();
            States.Instance.SetGameConfigState(state);
        }

        protected static void UpdateStakeState(
            StakeState state,
            GoldBalanceState stakedBalanceState,
            int level)
        {
            if (state is { })
            {
                States.Instance.SetStakeState(state, stakedBalanceState, level);
            }
        }

        protected static void UpdateCrystalRandomSkillState<T>(
            ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var state = GetCrystalRandomSkillState(evaluation);

            if (state is { })
            {
                States.Instance.SetCrystalRandomSkillState(state);
            }
        }

        private static UniTask UpdateAgentStateAsync(AgentState state)
        {
            UpdateCache(state);
            return States.Instance.SetAgentStateAsync(state);
        }

        private static void UpdateGoldBalanceState(GoldBalanceState goldBalanceState)
        {
            if (goldBalanceState is { } &&
                Game.Game.instance.Agent.Address.Equals(goldBalanceState.address))
            {
                Game.Game.instance.CachedBalance[goldBalanceState.address] = goldBalanceState.Gold;
            }

            States.Instance.SetGoldBalanceState(goldBalanceState);
        }

        protected static void UpdateCrystalBalance<T>(ActionEvaluation<T> evaluation) where T : ActionBase
        {
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            var addr = States.Instance.CurrentAvatarState.agentAddress; // In case i am the sender
            if (!evaluation.Signer.Equals(States.Instance.CurrentAvatarState.agentAddress)) // In case i am the reciever
                addr = evaluation.Signer;

            if (!evaluation.OutputStates.UpdatedFungibleAssets.TryGetValue(
                    addr,
                    out var currencies) ||
                !currencies.Contains(CrystalCalculator.CRYSTAL))
            {
                return;
            }

            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            try
            {
                var crystal = evaluation.OutputStates.GetBalance(
                    States.Instance.CurrentAvatarState.agentAddress, //evaluation.Signer,
                    CrystalCalculator.CRYSTAL);
                States.Instance.SetCrystalBalance(crystal);
            }
            catch (BalanceDoesNotExistsException)
            {
                var crystal = FungibleAssetValue.FromRawValue(
                    CrystalCalculator.CRYSTAL,
                    0);
                States.Instance.SetCrystalBalance(crystal);
            }
        }

        private static UniTask UpdateAvatarState(AvatarState avatarState, int index) =>
            States.Instance.AddOrReplaceAvatarStateAsync(avatarState, index);

        public async UniTask UpdateCurrentAvatarStateAsync(AvatarState avatarState)
        {
            // When in battle, do not immediately update the AvatarState, but pending it.
            if (Pending)
            {
                Debug.Log($"[{nameof(ActionHandler)}] Pending AvatarState");
                Game.Game.instance.Stage.AvatarState = avatarState;
                return;
            }

            Game.Game.instance.Stage.AvatarState = null;
            var questList = avatarState.questList.Where(i => i.Complete && !i.IsPaidInAction).ToList();
            if (questList.Count >= 1)
            {
                if (questList.Count == 1)
                {
                    var quest = questList.First();
                    var format = L10nManager.Localize("NOTIFICATION_QUEST_COMPLETE");
                    var msg = string.Format(format, quest.GetContent());
                    UI.NotificationSystem.Push(MailType.System, msg, NotificationCell.NotificationType.Information);
                }
                else
                {
                    var format = L10nManager.Localize("NOTIFICATION_MULTIPLE_QUEST_COMPLETE");
                    var msg = string.Format(format, questList.Count);
                    UI.NotificationSystem.Push(MailType.System, msg, NotificationCell.NotificationType.Information);
                }
            }

            UpdateCache(avatarState);
            await UpdateAvatarState(avatarState, States.Instance.CurrentAvatarKey);
        }

        internal static void UpdateCombinationSlotState(
            Address avatarAddress,
            int slotIndex,
            CombinationSlotState state)
        {
            States.Instance.UpdateCombinationSlotState(avatarAddress, slotIndex, state);
            UpdateCache(state);
        }

        private static void UpdateCache(Model.State.State state)
        {
            if (state is null)
            {
                return;
            }

            if (Game.Game.instance.CachedStates.ContainsKey(state.address))
            {
                Game.Game.instance.CachedStates[state.address] = state.Serialize();
            }
        }
    }
}