using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Order;
using Lib9c.Renderer;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.Model.State;
using Nekoyume.Model.Quest;
using Nekoyume.State.Modifiers;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module;
using UnityEngine;
using Cysharp.Threading.Tasks;
using mixpanel;
using Nekoyume.Arena;
using Nekoyume.Game;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.EnumType;
using Unity.Mathematics;

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
using Lib9c.DevExtensions.Action;
#endif

namespace Nekoyume.BlockChain
{
    using Nekoyume.PandoraBox;
    using UI.Scroller;
    using UniRx;

    /// <summary>
    /// 현상태 : 각 액션의 랜더 단계에서 즉시 게임 정보에 반영시킴. 아바타를 선택하지 않은 상태에서 이전에 성공시키지 못한 액션을 재수행하고
    ///       이를 핸들링하면, 즉시 게임 정보에 반영시길 수 없기 때문에 에러가 발생함.
    /// 참고 : 이후 언랜더 처리를 고려한 해법이 필요함.
    /// 해법 1: 랜더 단계에서 얻는 `eval` 자체 혹은 변경점을 queue에 넣고, 게임의 상태에 따라 꺼내 쓰도록.
    /// </summary>
    public class ActionRenderHandler : ActionHandler
    {
        private static class Singleton
        {
            internal static readonly ActionRenderHandler Value = new ActionRenderHandler();
        }

        public static ActionRenderHandler Instance => Singleton.Value;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private IDisposable _disposableForBattleEnd;

        private ActionRenderer _actionRenderer;

        private ActionRenderHandler()
        {
        }

        public override void Start(ActionRenderer renderer)
        {
            _actionRenderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

            Stop();
            _actionRenderer.BlockEndSubject.ObserveOnMainThread().Subscribe(_ =>
            {
                Debug.Log($"[{nameof(BlockRenderHandler)}] Render actions end");
            }).AddTo(_disposables);
            _actionRenderer.ActionRenderSubject.ObserveOnMainThread().Subscribe(eval =>
            {
                if (!(eval.Action is GameAction gameAction))
                {
                    return;
                }

                if (ActionManager.Instance.TryPopActionEnqueuedDateTime(gameAction.Id, out var enqueuedDateTime))
                {
                    var actionType = gameAction.GetActionTypeAttribute();
                    var elapsed = (DateTime.Now - enqueuedDateTime).TotalSeconds;
                    Analyzer.Instance.Track("Unity/ActionRender", new Value
                    {
                        ["ActionType"] = actionType.TypeIdentifier,
                        ["Elapsed"] = elapsed,
                    });
                }
            }).AddTo(_disposables);

            RewardGold();
            GameConfig();
            CreateAvatar();
            TransferAsset();
            MonsterCollect();
            Stake();

            // Battle
            HackAndSlash();
            MimisbrunnrBattle();
            HackAndSlashSweep();
            HackAndSlashRandomBuff();

            // Craft
            CombinationConsumable();
            CombinationEquipment();
            ItemEnhancement();
            RapidCombination();
            Grinding();

            // Market
            Sell();
            SellCancellation();
            UpdateSell();
            Buy();

            // Consume
            DailyReward();
            RedeemCode();
            ChargeActionPoint();
            ClaimMonsterCollectionReward();
            ClaimStakeReward();

            // Crystal Unlocks
            UnlockEquipmentRecipe();
            UnlockWorld();
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            Testbed();
#endif

            // Arena
            InitializeArenaActions();
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private void RewardGold()
        {
            // FIXME RewardGold의 결과(ActionEvaluation)에서 다른 갱신 주소가 같이 나오고 있는데 더 조사해봐야 합니다.
            // 우선은 HasUpdatedAssetsForCurrentAgent로 다르게 검사해서 우회합니다.
            _actionRenderer.EveryRender<RewardGold>()
                .Where(HasUpdatedAssetsForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(async eval => await UpdateAgentStateAsync(eval))
                .AddTo(_disposables);
        }

        private void CreateAvatar()
        {
            _actionRenderer.EveryRender<CreateAvatar>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseCreateAvatar)
                .AddTo(_disposables);
        }

        private void HackAndSlash()
        {
            _actionRenderer.EveryRender<HackAndSlash>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseHackAndSlash)
                .AddTo(_disposables);
        }

        private void MimisbrunnrBattle()
        {
            _actionRenderer.EveryRender<MimisbrunnrBattle>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseMimisbrunnr)
                .AddTo(_disposables);
        }

        private void CombinationConsumable()
        {
            _actionRenderer.EveryRender<CombinationConsumable>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombinationConsumable)
                .AddTo(_disposables);
        }

        private void Sell()
        {
            _actionRenderer.EveryRender<Sell>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseSell)
                .AddTo(_disposables);
        }

        private void SellCancellation()
        {
            _actionRenderer.EveryRender<SellCancellation>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseSellCancellation)
                .AddTo(_disposables);
        }

        private void UpdateSell()
        {
            _actionRenderer.EveryRender<UpdateSell>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseUpdateSell)
                .AddTo(_disposables);
        }

        private void Buy()
        {
            _actionRenderer.EveryRender<Buy>()
                .ObserveOnMainThread()
                .Subscribe(ResponseBuy)
                .AddTo(_disposables);
        }

        private void ItemEnhancement()
        {
            _actionRenderer.EveryRender<ItemEnhancement>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseItemEnhancement)
                .AddTo(_disposables);
        }

        private void DailyReward()
        {
            _actionRenderer.EveryRender<DailyReward>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseDailyReward)
                .AddTo(_disposables);
        }

        private void CombinationEquipment()
        {
            _actionRenderer.EveryRender<CombinationEquipment>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombinationEquipment)
                .AddTo(_disposables);
        }

        private void Grinding()
        {
            _actionRenderer.EveryRender<Grinding>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseGrinding)
                .AddTo(_disposables);
        }

        private void UnlockEquipmentRecipe()
        {
            _actionRenderer.EveryRender<UnlockEquipmentRecipe>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseUnlockEquipmentRecipeAsync)
                .AddTo(_disposables);
        }

        private void UnlockWorld()
        {
            _actionRenderer.EveryRender<UnlockWorld>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseUnlockWorld)
                .AddTo(_disposables);
        }

        private void HackAndSlashRandomBuff()
        {
            _actionRenderer.EveryRender<HackAndSlashRandomBuff>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseHackAndSlashRandomBuff)
                .AddTo(_disposables);
        }

        private void RapidCombination()
        {
            _actionRenderer.EveryRender<RapidCombination>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseRapidCombination)
                .AddTo(_disposables);
        }

        private void GameConfig()
        {
            _actionRenderer.EveryRender(GameConfigState.Address)
                .ObserveOnMainThread()
                .Subscribe(UpdateGameConfigState)
                .AddTo(_disposables);
        }

        private void RedeemCode()
        {
            _actionRenderer.EveryRender<Action.RedeemCode>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseRedeemCode)
                .AddTo(_disposables);
        }

        private void ChargeActionPoint()
        {
            _actionRenderer.EveryRender<ChargeActionPoint>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseChargeActionPoint)
                .AddTo(_disposables);
        }

        private void MonsterCollect()
        {
            _actionRenderer.EveryRender<MonsterCollect>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseMonsterCollect)
                .AddTo(_disposables);
        }

        private void ClaimMonsterCollectionReward()
        {
            _actionRenderer.EveryRender<ClaimMonsterCollectionReward>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseClaimMonsterCollectionReward)
                .AddTo(_disposables);
        }

        private void TransferAsset()
        {
            _actionRenderer.EveryRender<TransferAsset>()
                .Where(HasUpdatedAssetsForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseTransferAsset)
                .AddTo(_disposables);
        }

        private void HackAndSlashSweep()
        {
            _actionRenderer.EveryRender<HackAndSlashSweep>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseHackAndSlashSweep)
                .AddTo(_disposables);
        }

        private void Stake()
        {
            _actionRenderer.EveryRender<Stake>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseStake)
                .AddTo(_disposables);
        }

        private void ClaimStakeReward()
        {
            _actionRenderer.EveryRender<ClaimStakeReward>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseClaimStakeReward)
                .AddTo(_disposables);
        }

        private void InitializeArenaActions()
        {
            _actionRenderer.EveryRender<JoinArena>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseJoinArenaAsync)
                .AddTo(_disposables);

            _actionRenderer.EveryRender<BattleArena>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseBattleArena)
                .AddTo(_disposables);
        }

        private async UniTaskVoid ResponseCreateAvatar(ActionBase.ActionEvaluation<CreateAvatar> eval)
        {
            if (eval.Exception != null)
            {
                return;
            }

            await UpdateAgentStateAsync(eval);
            await UpdateAvatarState(eval, eval.Action.index);
            var avatarState
                = await States.Instance.SelectAvatarAsync(eval.Action.index);
            RenderQuest(
                avatarState.address,
                avatarState.questList.completedQuestIds);

            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = agentAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar2.DeriveFormat,
                    eval.Action.index
                )
            );
            DialogPopup.DeleteDialogPlayerPrefs(avatarAddress);

            var loginDetail = Widget.Find<LoginDetail>();
            if (loginDetail && loginDetail.IsActive())
            {
                loginDetail.OnRenderCreateAvatar(eval);
            }
        }

        private void ResponseRapidCombination(ActionBase.ActionEvaluation<RapidCombination> eval)
        {
            if (eval.Exception is null)
            {
                var avatarAddress = eval.Action.avatarAddress;
                var slotIndex = eval.Action.slotIndex;
                var slotState = eval.OutputStates.GetCombinationSlotState(avatarAddress, slotIndex);
                var result = (RapidCombination5.ResultModel)slotState.Result;
                foreach (var pair in result.cost)
                {
                    LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value);
                }

                var formatKey = string.Empty;
                var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
                var combinationSlotState = States.Instance.GetCombinationSlotState(currentBlockIndex);
                var stateResult = combinationSlotState[slotIndex]?.Result;
                switch (stateResult)
                {
                    case CombinationConsumable5.ResultModel combineResultModel:
                    {
                        LocalLayerModifier.AddNewResultAttachmentMail(avatarAddress, combineResultModel.id,
                            currentBlockIndex);
                        if (combineResultModel.itemUsable is Equipment equipment)
                        {
                            if (combineResultModel.subRecipeId.HasValue &&
                                Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheetV2.TryGetValue(
                                    combineResultModel.subRecipeId.Value,
                                    out var subRecipeRow))
                            {
                                formatKey = equipment.optionCountFromCombination == subRecipeRow.Options.Count
                                    ? "NOTIFICATION_COMBINATION_COMPLETE_GREATER"
                                    : "NOTIFICATION_COMBINATION_COMPLETE";
                            }
                            else
                            {
                                formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                            }
                        }
                        else
                        {
                            formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                        }

                        break;
                    }
                    case ItemEnhancement.ResultModel enhancementResultModel:
                    {
                        LocalLayerModifier.AddNewResultAttachmentMail(avatarAddress, enhancementResultModel.id,
                            currentBlockIndex);

                        switch (enhancementResultModel.enhancementResult)
                        {
                            case Action.ItemEnhancement.EnhancementResult.GreatSuccess:
                                formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_GREATER";
                                break;
                            case Action.ItemEnhancement.EnhancementResult.Success:
                                formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                                break;
                            case Action.ItemEnhancement.EnhancementResult.Fail:
                                Analyzer.Instance.Track("Unity/ItemEnhancement Failed", new Value
                                {
                                    ["GainedCrystal"] = (long)enhancementResultModel.CRYSTAL.MajorUnit,
                                    ["BurntNCG"] = (long)enhancementResultModel.gold,
                                });
                                formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_FAIL";
                                break;
                            default:
                                Debug.LogError(
                                    $"Unexpected result.enhancementResult: {enhancementResultModel.enhancementResult}");
                                formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                                break;
                        }

                        break;
                    }
                    default:
                        Debug.LogError(
                            $"Unexpected state.Result: {stateResult}");
                        formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                        break;
                }

                var format = L10nManager.Localize(formatKey);
                NotificationSystem.CancelReserve(result.itemUsable.TradableId);
                NotificationSystem.Push(
                    MailType.Workshop,
                    string.Format(format, result.itemUsable.GetLocalizedName()),
                    NotificationCell.NotificationType.Notification);

                UpdateCombinationSlotState(slotIndex, slotState);
                UpdateAgentStateAsync(eval).Forget();
                UpdateCurrentAvatarStateAsync(eval).Forget();
            }

            Widget.Find<CombinationSlotsPopup>().SetCaching(eval.Action.slotIndex, false);
        }

        private void ResponseCombinationEquipment(ActionBase.ActionEvaluation<CombinationEquipment> eval)
        {
            if (eval.Action.payByCrystal)
            {
                Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(false);
            }

            if (eval.Exception is null)
            {
                var agentAddress = eval.Signer;
                var avatarAddress = eval.Action.avatarAddress;
                var slotIndex = eval.Action.slotIndex;
                var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, slotIndex);
                var result = (CombinationConsumable5.ResultModel)slot.Result;

                if (!eval.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress, out var avatarState, out _))
                {
                    return;
                }

                LocalLayerModifier.ModifyAgentGold(agentAddress, result.gold);
                LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, result.actionPoint);
                foreach (var pair in result.materials)
                {
                    LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value);
                }

                LocalLayerModifier.RemoveItem(avatarAddress, result.itemUsable.ItemId,
                    result.itemUsable.RequiredBlockIndex, 1);
                LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

                var gameInstance = Game.Game.instance;
                var nextQuest = avatarState.questList?
                    .OfType<CombinationEquipmentQuest>()
                    .Where(x => !x.Complete)
                    .OrderBy(x => x.StageId)
                    .FirstOrDefault(x =>
                        gameInstance.TableSheets.EquipmentItemRecipeSheet.TryGetValue(x.RecipeId, out _));

                UpdateCombinationSlotState(slotIndex, slot);
                UpdateAgentStateAsync(eval).Forget();
                UpdateCurrentAvatarStateAsync(eval).Forget();
                RenderQuest(avatarAddress, avatarState.questList?.completedQuestIds);

                if (!(nextQuest is null))
                {
                    var isRecipeMatch = nextQuest.RecipeId == eval.Action.recipeId;

                    if (isRecipeMatch)
                    {
                        var celebratesPopup = Widget.Find<CelebratesPopup>();
                        celebratesPopup.Show(nextQuest);
                        celebratesPopup.OnDisableObservable
                            .First()
                            .Subscribe(_ =>
                            {
                                var menu = Widget.Find<Menu>();
                                if (menu.isActiveAndEnabled)
                                {
                                    menu.UpdateGuideQuest(avatarState);
                                }
                            });
                    }
                }

                // Notify
                string formatKey;
                if (result.itemUsable is Equipment equipment)
                {
                    if (eval.Action.subRecipeId.HasValue &&
                        Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheetV2.TryGetValue(
                            eval.Action.subRecipeId.Value,
                            out var row))
                    {
                        formatKey = equipment.optionCountFromCombination == row.Options.Count
                            ? "NOTIFICATION_COMBINATION_COMPLETE_GREATER"
                            : "NOTIFICATION_COMBINATION_COMPLETE";
                    }
                    else
                    {
                        formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                    }
                }
                else
                {
                    Debug.LogError($"[{nameof(ResponseCombinationEquipment)}] result.itemUsable is not Equipment");
                    formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                }

                var format = L10nManager.Localize(formatKey);
                UI.NotificationSystem.Reserve(
                    MailType.Workshop,
                    string.Format(format, result.itemUsable.GetLocalizedName()),
                    slot.UnlockBlockIndex,
                    result.itemUsable.TradableId);
                // ~Notify
            }

            Widget.Find<CombinationSlotsPopup>().SetCaching(eval.Action.slotIndex, false);
        }

        private void ResponseCombinationConsumable(ActionBase.ActionEvaluation<CombinationConsumable> eval)
        {
            if (eval.Exception is null)
            {
                var agentAddress = eval.Signer;
                var avatarAddress = eval.Action.avatarAddress;
                var slotIndex = eval.Action.slotIndex;
                var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, slotIndex);
                var result = (CombinationConsumable5.ResultModel)slot.Result;
                var itemUsable = result.itemUsable;
                if (!eval.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress, out var avatarState, out _))
                {
                    return;
                }

                LocalLayerModifier.ModifyAgentGold(agentAddress, result.gold);
                LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, result.actionPoint);
                foreach (var pair in result.materials)
                {
                    LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value);
                }

                LocalLayerModifier.RemoveItem(avatarAddress, itemUsable.ItemId, itemUsable.RequiredBlockIndex, 1);
                LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

                UpdateCombinationSlotState(slotIndex, slot);
                UpdateAgentStateAsync(eval).Forget();
                UpdateCurrentAvatarStateAsync(eval).Forget();
                RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);

                // Notify
                var format = L10nManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
                NotificationSystem.Reserve(
                    MailType.Workshop,
                    string.Format(format, result.itemUsable.GetLocalizedName()),
                    slot.UnlockBlockIndex,
                    result.itemUsable.TradableId);
                // ~Notify
            }

            Widget.Find<CombinationSlotsPopup>().SetCaching(eval.Action.slotIndex, false);
        }

        private void ResponseItemEnhancement(ActionBase.ActionEvaluation<ItemEnhancement> eval)
        {
            Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(false);
            if (eval.Exception is null)
            {
                var agentAddress = eval.Signer;
                var avatarAddress = eval.Action.avatarAddress;
                var slotIndex = eval.Action.slotIndex;
                var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, slotIndex);
                var result = (ItemEnhancement.ResultModel)slot.Result;
                var itemUsable = result.itemUsable;
                if (!eval.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress, out var avatarState, out _))
                {
                    return;
                }

                LocalLayerModifier.ModifyAgentGold(agentAddress, result.gold);
                LocalLayerModifier.ModifyAgentCrystalAsync(agentAddress, -result.CRYSTAL.MajorUnit).Forget();
                LocalLayerModifier.AddItem(avatarAddress, itemUsable.TradableId, itemUsable.RequiredBlockIndex, 1);
                foreach (var tradableId in result.materialItemIdList)
                {
                    if (avatarState.inventory.TryGetNonFungibleItem(tradableId,
                            out ItemUsable materialItem))
                    {
                        LocalLayerModifier.AddItem(avatarAddress, tradableId, materialItem.RequiredBlockIndex, 1);
                    }
                }

                LocalLayerModifier.RemoveItem(avatarAddress, itemUsable.TradableId, itemUsable.RequiredBlockIndex, 1);
                LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

                UpdateCombinationSlotState(slotIndex, slot);
                UpdateAgentStateAsync(eval).Forget();
                UpdateCurrentAvatarStateAsync(eval).Forget();
                RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);

                // Notify
                string formatKey;
                switch (result.enhancementResult)
                {
                    case Action.ItemEnhancement.EnhancementResult.GreatSuccess:
                        formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_GREATER";
                        break;
                    case Action.ItemEnhancement.EnhancementResult.Success:
                        formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                        break;
                    case Action.ItemEnhancement.EnhancementResult.Fail:
                        Analyzer.Instance.Track("Unity/ItemEnhancement Failed", new Value
                        {
                            ["GainedCrystal"] = (long)result.CRYSTAL.MajorUnit,
                            ["BurntNCG"] = (long)result.gold,
                        });
                        formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_FAIL";
                        break;
                    default:
                        Debug.LogError($"Unexpected result.enhancementResult: {result.enhancementResult}");
                        formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                        break;
                }

                var format = L10nManager.Localize(formatKey);
                NotificationSystem.Reserve(
                    MailType.Workshop,
                    string.Format(format, result.itemUsable.GetLocalizedName()),
                    slot.UnlockBlockIndex,
                    result.itemUsable.TradableId);
                // ~Notify
            }

            Widget.Find<CombinationSlotsPopup>().SetCaching(eval.Action.slotIndex, false);
        }

        private void ResponseSell(ActionBase.ActionEvaluation<Sell> eval)
        {
            if (eval.Exception is null)
            {
                var count = eval.Action.count;
                var outputStates = eval.OutputStates;
                var item = GetItem(outputStates, eval.Action.tradableId);
                if (item is null)
                {
                    return;
                }

                string message = string.Empty;
                if (count > 1)
                {
                    message = string.Format(L10nManager.Localize("NOTIFICATION_MULTIPLE_SELL_COMPLETE"),
                        item.GetLocalizedName(),
                        count);
                }
                else
                {
                    message = string.Format(L10nManager.Localize("NOTIFICATION_SELL_COMPLETE"),
                        item.GetLocalizedName());
                }

                OneLineSystem.Push(
                    MailType.Auction,
                    message,
                    NotificationCell.NotificationType.Information);

                UpdateCurrentAvatarStateAsync(eval).Forget();
                ReactiveShopState.UpdateSellDigestsAsync().Forget();
            }
        }

        private async void ResponseSellCancellation(ActionBase.ActionEvaluation<SellCancellation> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var avatarAddress = eval.Action.sellerAvatarAddress;
            var order = await Util.GetOrder(eval.Action.orderId);
            var itemName = await Util.GetItemNameByOrderId(order.OrderId);
            var count = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;
            LocalLayerModifier.RemoveItem(avatarAddress, order.TradableId, order.ExpiredBlockIndex, count);
            LocalLayerModifier.AddNewMail(avatarAddress, eval.Action.orderId);

            string message;
            if (count > 1)
            {
                message = string.Format(L10nManager.Localize("NOTIFICATION_MULTIPLE_SELL_CANCEL_COMPLETE"),
                    itemName, count);
            }
            else
            {
                message = string.Format(L10nManager.Localize("NOTIFICATION_SELL_CANCEL_COMPLETE"), itemName);
            }

            OneLineSystem.Push(MailType.Auction, message, NotificationCell.NotificationType.Information);

            UpdateCurrentAvatarStateAsync(eval).Forget();
            ReactiveShopState.UpdateSellDigestsAsync().Forget();
        }

        private async void ResponseUpdateSell(ActionBase.ActionEvaluation<UpdateSell> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var itemName = await Util.GetItemNameByOrderId(eval.Action.orderId);
            var order = await Util.GetOrder(eval.Action.orderId);
            var count = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;

            string message;
            if (count > 1)
            {
                message = string.Format(L10nManager.Localize("NOTIFICATION_MULTIPLE_REREGISTER_COMPLETE"),
                    itemName, count);
            }
            else
            {
                message = string.Format(L10nManager.Localize("NOTIFICATION_REREGISTER_COMPLETE"), itemName);
            }

            OneLineSystem.Push(MailType.Auction, message, NotificationCell.NotificationType.Information);
            UpdateCurrentAvatarStateAsync(eval).Forget();
            ReactiveShopState.UpdateSellDigestsAsync().Forget();
        }

        private async void ResponseBuy(ActionBase.ActionEvaluation<Buy> eval)
        {
            if (!(eval.Exception is null))
            {
                Debug.Log(eval.Exception);
                return;
            }

            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            if (!eval.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress,
                    out var avatarState, out _))
            {
                return;
            }

            var errorList = eval.Action.errors;
            List<(Guid orderId, int errorCode)> errors = errorList
                .Cast<List>()
                .Select(t => (t[0].ToGuid(), t[1].ToInteger()))
                .ToList();
            var purchaseInfos = eval.Action.purchaseInfos;
            if (eval.Action.buyerAvatarAddress == avatarAddress) // buyer
            {
                foreach (var purchaseInfo in purchaseInfos)
                {
                    var order = await Util.GetOrder(purchaseInfo.OrderId);
                    var itemName = await Util.GetItemNameByOrderId(order.OrderId);
                    var count = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;
                    var price = purchaseInfo.Price;

                    if (errors.Exists(tuple => tuple.orderId.Equals(purchaseInfo.OrderId)))
                    {
                        var (orderId, errorCode) =
                            errors.FirstOrDefault(tuple => tuple.orderId == purchaseInfo.OrderId);

                        var errorType = ((ShopErrorType)errorCode).ToString();
                        LocalLayerModifier.ModifyAgentGoldAsync(agentAddress, price).Forget();

                        string message;
                        if (count > 1)
                        {
                            message = string.Format(L10nManager.Localize("NOTIFICATION_MULTIPLE_BUY_FAIL"),
                                itemName, L10nManager.Localize(errorType), price, count);
                        }
                        else
                        {
                            message = string.Format(L10nManager.Localize("NOTIFICATION_BUY_FAIL"),
                                itemName, L10nManager.Localize(errorType), price);
                        }

                        OneLineSystem.Push(MailType.Auction, message, NotificationCell.NotificationType.Alert);
                    }
                    else
                    {
                        LocalLayerModifier.ModifyAgentGoldAsync(agentAddress, price).Forget();
                        LocalLayerModifier.RemoveItem(avatarAddress, order.TradableId, order.ExpiredBlockIndex, count);
                        LocalLayerModifier.AddNewMail(avatarAddress, purchaseInfo.OrderId);

                        string message;
                        if (count > 1)
                        {
                            message = string.Format(L10nManager.Localize("NOTIFICATION_MULTIPLE_BUY_BUYER_COMPLETE"),
                                itemName, price, count);
                        }
                        else
                        {
                            message = string.Format(L10nManager.Localize("NOTIFICATION_BUY_BUYER_COMPLETE"),
                                itemName, price);
                        }

                        OneLineSystem.Push(MailType.Auction, message, NotificationCell.NotificationType.Notification);
                    }
                }
            }
            else // seller
            {
                foreach (var purchaseInfo in purchaseInfos)
                {
                    var buyerAvatarStateValue = eval.OutputStates.GetState(eval.Action.buyerAvatarAddress);
                    if (buyerAvatarStateValue is null)
                    {
                        Debug.LogError("buyerAvatarStateValue is null.");
                        return;
                    }

                    const string nameWithHashFormat = "{0} <size=80%><color=#A68F7E>#{1}</color></size>";
                    var buyerNameWithHash = string.Format(
                        nameWithHashFormat,
                        ((Text)((Dictionary)buyerAvatarStateValue)["name"]).Value,
                        eval.Action.buyerAvatarAddress.ToHex().Substring(0, 4)
                    );

                    var order = await Util.GetOrder(purchaseInfo.OrderId);
                    var itemName = await Util.GetItemNameByOrderId(order.OrderId);
                    var count = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;
                    var taxedPrice = order.Price - order.GetTax();

                    LocalLayerModifier.ModifyAgentGoldAsync(agentAddress, -taxedPrice).Forget();
                    LocalLayerModifier.AddNewMail(avatarAddress, purchaseInfo.OrderId);

                    string message;
                    if (count > 1)
                    {
                        message = string.Format(L10nManager.Localize("NOTIFICATION_MULTIPLE_BUY_SELLER_COMPLETE"),
                            buyerNameWithHash, itemName, count);
                    }
                    else
                    {
                        message = string.Format(L10nManager.Localize("NOTIFICATION_BUY_SELLER_COMPLETE"),
                            buyerNameWithHash, itemName);
                    }

                    OneLineSystem.Push(MailType.Auction, message, NotificationCell.NotificationType.Notification);
                }
            }

            UpdateAgentStateAsync(eval).Forget();
            UpdateCurrentAvatarStateAsync(eval).Forget();
            RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
        }

        private void ResponseDailyReward(ActionBase.ActionEvaluation<DailyReward> eval)
        {
            if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.avatarAddress))
            {
                GameConfigStateSubject.ActionPointState.Remove(eval.Action.avatarAddress);
            }

            if (eval.Exception is null &&
                eval.Action.avatarAddress == States.Instance.CurrentAvatarState.address)
            {
                LocalLayer.Instance.ClearAvatarModifiers<AvatarDailyRewardReceivedIndexModifier>(
                    eval.Action.avatarAddress);
                UpdateCurrentAvatarStateAsync(eval).Forget();
                UI.NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_RECEIVED_DAILY_REWARD"),
                    NotificationCell.NotificationType.Notification);
            }
        }

        private void ResponseHackAndSlash(ActionBase.ActionEvaluation<HackAndSlash> eval)
        {
            if (eval.Exception is null)
            {
                if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
                {
                    return;
                }

                _disposableForBattleEnd?.Dispose();
                _disposableForBattleEnd =
                    Game.Game.instance.Stage.onEnterToStageEnd
                        .First()
                        .Subscribe(_ =>
                        {
                            var task = UniTask.Run(() =>
                            {
                                UpdateCurrentAvatarStateAsync(eval).Forget();
                                UpdateCrystalRandomSkillState(eval);
                                var avatarState = States.Instance.CurrentAvatarState;
                                RenderQuest(eval.Action.avatarAddress,
                                    avatarState.questList.completedQuestIds);
                                _disposableForBattleEnd = null;
                                Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
                            });
                            task.ToObservable()
                                .First()
                                // ReSharper disable once ConvertClosureToMethodGroup
                                .DoOnError(e => Debug.LogException(e));
                        });

                var tableSheets = Game.Game.instance.TableSheets;

                var skillsOnWaveStart = new List<Model.Skill.Skill>();
                if (eval.Action.stageBuffId.HasValue)
                {
                    var skill = CrystalRandomSkillState.GetSkill(
                        eval.Action.stageBuffId.Value,
                        tableSheets.CrystalRandomBuffSheet,
                        tableSheets.SkillSheet);
                    skillsOnWaveStart.Add(skill);
                }

                var simulator = new StageSimulator(
                    new LocalRandom(eval.RandomSeed),
                    States.Instance.CurrentAvatarState,
                    eval.Action.foods,
                    skillsOnWaveStart,
                    eval.Action.worldId,
                    eval.Action.stageId,
                    Game.Game.instance.TableSheets.GetStageSimulatorSheets(),
                    Game.Game.instance.TableSheets.CostumeStatSheet,
                    StageSimulator.ConstructorVersionV100080);
                simulator.Simulate(1);
                var log = simulator.Log;
                Game.Game.instance.Stage.PlayCount = 1;

                if (eval.Action.stageBuffId.HasValue)
                {
                    Analyzer.Instance.Track("Unity/Use Crystal Bonus Skill", new Value
                    {
                        ["RandomSkillId"] = eval.Action.stageBuffId,
                        ["IsCleared"] = simulator.Log.IsClear,
                    });
                }

                if (Widget.Find<LoadingScreen>().IsActive())
                {
                    if (Widget.Find<BattlePreparation>().IsActive())
                    {
                        Widget.Find<BattlePreparation>().GoToStage(log);
                    }
                    else if (Widget.Find<Menu>().IsActive())
                    {
                        Widget.Find<Menu>().GoToStage(log);
                    }
                }
                else if (Widget.Find<StageLoadingEffect>().IsActive() &&
                         Widget.Find<BattleResultPopup>().IsActive())
                {
                    Widget.Find<BattleResultPopup>().NextStage(log);
                }

                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                else if (PandoraBoxMaster.CurrentAction == PandoraUtil.ActionType.HackAndSlash)
                {
                    BattleResultPopup.Model _battleResultModel = new BattleResultPopup.Model();
                    Widget.Find<BattleResultPopup>().StageProgressBar.Initialize(false);

                    _battleResultModel.ClearedWaveNumber = log.clearedWaveNumber;

                    var avatarState = States.Instance.CurrentAvatarState;
                    var isClear = log.IsClear;

                    _battleResultModel.ActionPoint = avatarState.actionPoint;
                    _battleResultModel.State = log.result;
                    Game.Game.instance.TableSheets.WorldSheet.TryGetValue(log.worldId, out var world);
                    _battleResultModel.WorldName = world?.GetLocalizedName();
                    _battleResultModel.WorldID = log.worldId;
                    _battleResultModel.StageID = log.stageId;
                    avatarState.worldInformation.TryGetLastClearedStageId(out var lasStageId);
                    _battleResultModel.LastClearedStageId = lasStageId;
                    _battleResultModel.IsClear = log.IsClear;
                    var succeedToGetWorldRow =
                        Game.Game.instance.TableSheets.WorldSheet.TryGetValue(log.worldId, out var worldRow);
                    if (succeedToGetWorldRow)
                    {
                        _battleResultModel.IsEndStage = log.stageId == worldRow.StageEnd;
                    }

                    if (log.FirstOrDefault(e => e is GetReward) is GetReward getReward)
                    {
                        var rewards = getReward.Rewards;
                        foreach (var item in rewards)
                        {
                            var countableItem = new UI.Model.CountableItem(item, 1);
                            _battleResultModel.AddReward(countableItem);
                        }
                    }

                    _battleResultModel.NextState = BattleResultPopup.NextState.Raid;
                    Widget.Find<BattleResultPopup>().Show(_battleResultModel, false); //eval.Action.playCount ???

                    ActionRenderHandler.Instance.Pending = false;
                    //UpdateCurrentAvatarStateAsync(eval);
                    //UpdateWeeklyArenaState(eval);
                    UpdateAgentStateAsync(eval).Forget();
                    UpdateCurrentAvatarStateAsync(eval).Forget();


                    //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                }
            }
            else
            {
                var showLoadingScreen = false;
                if (Widget.Find<StageLoadingEffect>().IsActive())
                {
                    Widget.Find<StageLoadingEffect>().Close();
                }

                if (Widget.Find<BattleResultPopup>().IsActive())
                {
                    showLoadingScreen = true;
                    Widget.Find<BattleResultPopup>().Close();
                }

                Game.Game.BackToMainAsync(eval.Exception.InnerException, showLoadingScreen).Forget();
            }

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            PandoraBoxMaster.CurrentAction = PandoraUtil.ActionType.Idle;
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        private void ResponseHackAndSlashSweep(ActionBase.ActionEvaluation<HackAndSlashSweep> eval)
        {
            if (eval.Exception is null)
            {
                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                if (PandoraBoxMaster.CurrentAction == PandoraUtil.ActionType.HackAndSlash)
                {
                    Widget.Find<SweepResultPopup>().ShowPandora(eval.Action.worldId, eval.Action.actionPoint, eval.Action.apStoneCount,0);
                    //Widget.Find<SweepResultPopup>().OnBattleFinish();
                    ActionRenderHandler.Instance.Pending = false;
                }
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||


                Widget.Find<SweepResultPopup>().OnActionRender(new LocalRandom(eval.RandomSeed));

                if (eval.Action.apStoneCount > 0)
                {
                    var avatarAddress = eval.Action.avatarAddress;
                    LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, eval.Action.actionPoint);
                    var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values.First(r =>
                        r.ItemSubType == ItemSubType.ApStone);
                    LocalLayerModifier.AddItem(avatarAddress, row.ItemId, eval.Action.apStoneCount);
                }

                UpdateCurrentAvatarStateAsync().Forget();


            }
            else
            {
                Widget.Find<SweepResultPopup>().Close();
                Game.Game.BackToMainAsync(eval.Exception.InnerException, false).Forget();
            }

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            PandoraBoxMaster.CurrentAction = PandoraUtil.ActionType.Idle;
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        private void ResponseMimisbrunnr(ActionBase.ActionEvaluation<MimisbrunnrBattle> eval)
        {
            if (eval.Exception is null)
            {
                if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
                {
                    return;
                }

                _disposableForBattleEnd?.Dispose();
                _disposableForBattleEnd =
                    Game.Game.instance.Stage.onEnterToStageEnd
                        .First()
                        .Subscribe(_ =>
                        {
                            var task = UniTask.Run(() =>
                            {
                                UpdateCurrentAvatarStateAsync(eval).Forget();
                                var avatarState = States.Instance.CurrentAvatarState;
                                RenderQuest(eval.Action.avatarAddress,
                                    avatarState.questList.completedQuestIds);
                                _disposableForBattleEnd = null;
                                Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
                            });
                            task.ToObservable()
                                .First()
                                // ReSharper disable once ConvertClosureToMethodGroup
                                .DoOnError(e => Debug.LogException(e));
                        });

                var simulator = new StageSimulator(
                    new LocalRandom(eval.RandomSeed),
                    States.Instance.CurrentAvatarState,
                    eval.Action.foods,
                    eval.Action.worldId,
                    eval.Action.stageId,
                    Game.Game.instance.TableSheets.GetStageSimulatorSheets(),
                    Game.Game.instance.TableSheets.CostumeStatSheet,
                    StageSimulator.ConstructorVersionV100080,
                    eval.Action.playCount
                );
                simulator.Simulate(eval.Action.playCount);
                BattleLog log = simulator.Log;
                Game.Game.instance.Stage.PlayCount = eval.Action.playCount;

                if (Widget.Find<LoadingScreen>().IsActive())
                {
                    if (Widget.Find<BattlePreparation>().IsActive())
                    {
                        Widget.Find<BattlePreparation>().GoToStage(log);
                    }
                    else if (Widget.Find<Menu>().IsActive())
                    {
                        Widget.Find<Menu>().GoToStage(log);
                    }
                }
                else if (Widget.Find<StageLoadingEffect>().IsActive() &&
                         Widget.Find<BattleResultPopup>().IsActive())
                {
                    Widget.Find<BattleResultPopup>().NextMimisbrunnrStage(log);
                }
            }
            else
            {
                var showLoadingScreen = false;
                if (Widget.Find<StageLoadingEffect>().IsActive())
                {
                    Widget.Find<StageLoadingEffect>().Close();
                }

                if (Widget.Find<BattleResultPopup>().IsActive())
                {
                    showLoadingScreen = true;
                    Widget.Find<BattleResultPopup>().Close();
                }

        ////        Game.Game.BackToMain(showLoadingScreen, eval.Exception.InnerException).Forget();
        ////    }
        ////}

        ////private void ResponseRankingBattle(ActionBase.ActionEvaluation<RankingBattle> eval)
        ////{
        ////    if (eval.Exception is null)
        ////    {
        ////        if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
        ////        {
        ////            return;
        ////        }

        ////        _disposableForBattleEnd?.Dispose();
        ////        _disposableForBattleEnd =
        ////            Game.Game.instance.Stage.onEnterToStageEnd
        ////                .First()
        ////                .Subscribe(_ =>
        ////                {
        ////                    var task = UniTask.Run(() =>
        ////                    {
        ////                        UpdateAgentStateAsync(eval).Forget();
        ////                        UpdateCurrentAvatarStateAsync().Forget();
        ////                        UpdateWeeklyArenaState(eval);
        ////                        _disposableForBattleEnd = null;
        ////                        Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
        ////                    });
        ////                    task.ToObservable()
        ////                        .First()
        ////                        // ReSharper disable once ConvertClosureToMethodGroup
        ////                        .DoOnError(e => Debug.LogException(e));
        ////                });

        ////        var tableSheets = Game.Game.instance.TableSheets;
        ////        ArenaInfo previousArenaInfo;
        ////        ArenaInfo previousEnemyArenaInfo;
        ////        EnemyPlayerDigest previousEnemyPlayerDigest;
        ////        if (eval.Extra is { })
        ////        {
        ////            var aid = (Dictionary)eval.Extra[nameof(Action.RankingBattle.PreviousArenaInfo)];
        ////            previousArenaInfo = new ArenaInfo(aid);
        ////            var eid = (Dictionary)eval.Extra[nameof(Action.RankingBattle.PreviousEnemyArenaInfo)];
        ////            previousEnemyArenaInfo = new ArenaInfo(eid);
        ////            var epd = (List)eval.Extra[nameof(Action.RankingBattle.PreviousEnemyPlayerDigest)];
        ////            previousEnemyPlayerDigest = new EnemyPlayerDigest(epd);
        ////        }
        ////        else
        ////        {
        ////            var previousAvatarState = eval.PreviousStates.GetAvatarStateV2(eval.Action.avatarAddress);
        ////            var tuple = eval.PreviousStates.GetArenaInfo(
        ////                eval.Action.weeklyArenaAddress,
        ////                previousAvatarState,
        ////                tableSheets.CharacterSheet,
        ////                tableSheets.CostumeStatSheet);
        ////            previousArenaInfo = tuple.arenaInfo;
        ////            var previousEnemyAvatarState = eval.PreviousStates.GetAvatarStateV2(eval.Action.enemyAddress);
        ////            var enemyTuple = eval.PreviousStates.GetArenaInfo(
        ////                eval.Action.weeklyArenaAddress,
        ////                previousEnemyAvatarState,
        ////                tableSheets.CharacterSheet,
        ////                tableSheets.CostumeStatSheet);
        ////            previousEnemyArenaInfo = enemyTuple.arenaInfo;
        ////            previousEnemyPlayerDigest = new EnemyPlayerDigest(previousEnemyAvatarState);
        ////        }

        ////        var rankingSimulatorSheets = tableSheets.GetRankingSimulatorSheets();
        ////        var player = new Player(States.Instance.CurrentAvatarState, rankingSimulatorSheets);
        ////        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        ////        PandoraBoxMaster.CurrentArenaEnemyAddress = previousEnemyArenaInfo.AvatarAddress.ToString().ToLower();
        ////        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        ////        var simulator = new RankingSimulator(
        ////            new LocalRandom(eval.RandomSeed),
        ////            player,
        ////            previousEnemyPlayerDigest,
        ////            new List<Guid>(),
        ////            rankingSimulatorSheets,
        ////            Action.RankingBattle.StageId,
        ////            tableSheets.CostumeStatSheet
        ////        );
        ////        simulator.Simulate();
        ////        var challengerScoreDelta = previousArenaInfo.Update(
        ////            previousEnemyArenaInfo,
        ////            simulator.Result,
        ////            ArenaScoreHelper.GetScore);
        ////        var rewards = RewardSelector.Select(
        ////            simulator.Random,
        ////            tableSheets.WeeklyArenaRewardSheet,
        ////            tableSheets.MaterialItemSheet,
        ////            player.Level,
        ////            previousArenaInfo.GetRewardCount());
        ////        simulator.PostSimulate(rewards, challengerScoreDelta, previousArenaInfo.Score);

        ////        //give option to know that battle is done
        ////        OneLineSystem.Push(MailType.System,
        ////            "<color=green>Pandora Box</color>: Arena Random Fight " +
        ////            "<color=green><b>Successfully</b></color> committed on the blockchain!"
        ////            , NotificationCell.NotificationType.Information);


        ////        if (Widget.Find<ArenaBattleLoadingScreen>().IsActive())
        ////        {
        ////            Widget.Find<RankingBoard>().GoToStage(simulator.Log);
        ////        }

        ////        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        ////        else if (PandoraBoxMaster.CurrentAction == PandoraUtil.ActionType.Ranking)
        ////        {
        ////            ActionRenderHandler.Instance.Pending = false;
        ////            UpdateAgentStateAsync(eval);
        ////            UpdateCurrentAvatarStateAsync(eval);
        ////            UpdateWeeklyArenaState(eval);
        ////        }

        ////        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        ////        Widget.Find<Menu>().ClearRemainingTickets();
        ////    }
        ////    else
        ////    {
        ////        var showLoadingScreen = false;
        ////        if (Widget.Find<ArenaBattleLoadingScreen>().IsActive())
        ////        {
        ////            Widget.Find<ArenaBattleLoadingScreen>().Close();
        ////        }

        ////        if (Widget.Find<RankingBattleResultPopup>().IsActive())
        ////        {
        ////            showLoadingScreen = true;
        ////            Widget.Find<RankingBattleResultPopup>().Close();
        ////        }

        ////        Game.Game.BackToMain(showLoadingScreen, eval.Exception.InnerException).Forget();
                Game.Game.BackToMainAsync(eval.Exception.InnerException, showLoadingScreen).Forget();
            }

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            PandoraBoxMaster.CurrentAction = PandoraUtil.ActionType.Idle;
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        private void ResponseRedeemCode(ActionBase.ActionEvaluation<Action.RedeemCode> eval)
        {
            var key = "UI_REDEEM_CODE_INVALID_CODE";
            if (eval.Exception is null)
            {
                Widget.Find<CodeRewardPopup>().Show(eval.Action.Code, eval.OutputStates.GetRedeemCodeState());
                key = "UI_REDEEM_CODE_SUCCESS";
                UpdateCurrentAvatarStateAsync(eval).Forget();
                var msg = L10nManager.Localize(key);
                UI.NotificationSystem.Push(MailType.System, msg, NotificationCell.NotificationType.Information);
            }
            else
            {
                if (eval.Exception.InnerException is DuplicateRedeemException)
                {
                    key = "UI_REDEEM_CODE_ALREADY_USE";
                }

                var msg = L10nManager.Localize(key);
                UI.NotificationSystem.Push(MailType.System, msg, NotificationCell.NotificationType.Alert);
            }
        }

        private void ResponseChargeActionPoint(ActionBase.ActionEvaluation<ChargeActionPoint> eval)
        {
            if (eval.Exception is null)
            {
                var avatarAddress = eval.Action.avatarAddress;
                LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress,
                    -States.Instance.GameConfigState.ActionPointMax);
                var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values.First(r =>
                    r.ItemSubType == ItemSubType.ApStone);
                LocalLayerModifier.AddItem(avatarAddress, row.ItemId, 1);

                if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.avatarAddress))
                {
                    GameConfigStateSubject.ActionPointState.Remove(eval.Action.avatarAddress);
                }

                UpdateCurrentAvatarStateAsync(eval).Forget();
            }
        }

        private void ResponseMonsterCollect(ActionBase.ActionEvaluation<MonsterCollect> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            NotificationSystem.Push(
                MailType.System,
                L10nManager.Localize("UI_MONSTERCOLLECTION_UPDATED"),
                NotificationCell.NotificationType.Information);

            UpdateAgentStateAsync(eval).Forget();
            UpdateCurrentAvatarStateAsync(eval).Forget();
            var (mcState, level, balance) = GetMonsterCollectionState(eval);
            if (mcState != null)
            {
                UpdateMonsterCollectionState(mcState,
                    new GoldBalanceState(mcState.address, balance),
                    level);
            }
        }

        private void ResponseClaimMonsterCollectionReward(
            ActionBase.ActionEvaluation<ClaimMonsterCollectionReward> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var agentAddress = eval.Signer;
            var avatarAddress = eval.Action.avatarAddress;
            if (!eval.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress, out var avatarState, out _))
            {
                return;
            }

            var mail = avatarState.mailBox.FirstOrDefault(e => e is MonsterCollectionMail);
            if (!(mail is MonsterCollectionMail { attachment: MonsterCollectionResult monsterCollectionResult }))
            {
                return;
            }

            // LocalLayer
            var rewardInfos = monsterCollectionResult.rewards;
            for (var i = 0; i < rewardInfos.Count; i++)
            {
                var rewardInfo = rewardInfos[i];
                if (!rewardInfo.ItemId.TryParseAsTradableId(
                        Game.Game.instance.TableSheets.ItemSheet,
                        out var tradableId))
                {
                    continue;
                }

                if (!rewardInfo.ItemId.TryGetFungibleId(
                        Game.Game.instance.TableSheets.ItemSheet,
                        out var fungibleId))
                {
                    continue;
                }

                avatarState.inventory.TryGetFungibleItems(fungibleId, out var items);
                var item = items.FirstOrDefault(x => x.item is ITradableItem);
                if (item != null && item is ITradableItem tradableItem)
                {
                    LocalLayerModifier.RemoveItem(avatarAddress,
                        tradableId,
                        tradableItem.RequiredBlockIndex,
                        rewardInfo.Quantity);
                }
            }

            LocalLayerModifier.AddNewAttachmentMail(avatarAddress, mail.id);
            // ~LocalLayer

            // Notification
            NotificationSystem.Push(
                MailType.System,
                L10nManager.Localize("NOTIFICATION_CLAIM_MONSTER_COLLECTION_REWARD_COMPLETE"),
                NotificationCell.NotificationType.Information);

            UpdateAgentStateAsync(eval).Forget();
            UpdateCurrentAvatarStateAsync(eval).Forget();
            RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
        }

        private void ResponseTransferAsset(ActionBase.ActionEvaluation<TransferAsset> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var senderAddress = eval.Action.Sender;
            var recipientAddress = eval.Action.Recipient;
            var currentAgentAddress = States.Instance.AgentState.address;
            var playToEarnRewardAddress = new Address("d595f7e85e1757d6558e9e448fa9af77ab28be4c");

            if (senderAddress == currentAgentAddress)
            {
                var amount = eval.Action.Amount;

                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("UI_TRANSFERASSET_NOTIFICATION_SENDER", amount, recipientAddress),
                    NotificationCell.NotificationType.Notification);
            }
            else if (recipientAddress == currentAgentAddress)
            {
                var amount = eval.Action.Amount;
                if (senderAddress == playToEarnRewardAddress)
                {
                    OneLineSystem.Push(MailType.System,
                        L10nManager.Localize("UI_PLAYTOEARN_NOTIFICATION_FORMAT", amount),
                        NotificationCell.NotificationType.Notification);
                }
                else
                {
                    OneLineSystem.Push(MailType.System,
                        L10nManager.Localize("UI_TRANSFERASSET_NOTIFICATION_RECIPIENT", amount, senderAddress),
                        NotificationCell.NotificationType.Notification);
                }
            }

            UpdateAgentStateAsync(eval).Forget();
        }

        private void ResponseGrinding(ActionBase.ActionEvaluation<Grinding> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var avatarAddress = eval.Action.AvatarAddress;
            var avatarState = eval.OutputStates.GetAvatarState(avatarAddress);
            var mail = avatarState.mailBox.OfType<GrindingMail>().FirstOrDefault(m => m.id.Equals(eval.Action.Id));
            if (mail is null)
            {
                return;
            }

            if (eval.Action.ChargeAp)
            {
                var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values.First(r =>
                    r.ItemSubType == ItemSubType.ApStone);
                LocalLayerModifier.AddItem(avatarAddress, row.ItemId);

                if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.AvatarAddress))
                {
                    GameConfigStateSubject.ActionPointState.Remove(eval.Action.AvatarAddress);
                }
            }

            OneLineSystem.Push(MailType.Grinding,
                L10nManager.Localize("UI_GRINDING_NOTIFY"),
                NotificationCell.NotificationType.Information);
            UpdateCurrentAvatarStateAsync(eval).Forget();
            UpdateAgentStateAsync(eval).Forget();
        }

        private async UniTaskVoid ResponseUnlockEquipmentRecipeAsync(
            ActionBase.ActionEvaluation<UnlockEquipmentRecipe> eval)
        {
            Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(false);
            var sharedModel = Craft.SharedModel;
            var recipeIds = eval.Action.RecipeIds;
            if (!(eval.Exception is null))
            {
                foreach (var id in eval.Action.RecipeIds)
                {
                    sharedModel.UnlockingRecipes.Remove(id);
                }

                sharedModel.SetUnlockedRecipes(sharedModel.UnlockedRecipes.Value);
                sharedModel.UpdateUnlockableRecipes();
                return;
            }

            var sheet = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet;
            var cost = CrystalCalculator.CalculateRecipeUnlockCost(recipeIds, sheet);
            await UniTask.WhenAll(
                LocalLayerModifier.ModifyAgentCrystalAsync(
                    States.Instance.AgentState.address,
                    cost.MajorUnit),
                UpdateCurrentAvatarStateAsync(eval),
                UpdateAgentStateAsync(eval));

            foreach (var id in recipeIds)
            {
                sharedModel.UnlockingRecipes.Remove(id);
            }

            recipeIds.AddRange(sharedModel.UnlockedRecipes.Value);
            sharedModel.SetUnlockedRecipes(recipeIds);
            sharedModel.UpdateUnlockableRecipes();
        }

        private void ResponseUnlockWorld(ActionBase.ActionEvaluation<UnlockWorld> eval)
        {
            Widget.Find<UnlockWorldLoadingScreen>().Close();

            if (!(eval.Exception is null))
            {
                Debug.LogError($"unlock world exc : {eval.Exception.InnerException}");
                return;
                // Exception handling...
            }

            var worldMap = Widget.Find<WorldMap>();
            worldMap.SharedViewModel.UnlockedWorldIds.AddRange(eval.Action.WorldIds);
            worldMap.SetWorldInformation(States.Instance.CurrentAvatarState.worldInformation);

            UpdateCurrentAvatarStateAsync(eval).Forget();
            UpdateAgentStateAsync(eval).Forget();
        }

        private void ResponseHackAndSlashRandomBuff(ActionBase.ActionEvaluation<HackAndSlashRandomBuff> eval)
        {
            if (!(eval.Exception is null))
            {
                Debug.LogError($"HackAndSlashRandomBuff exc : {eval.Exception.InnerException}");
                return;
            }

            UpdateCurrentAvatarStateAsync(eval).Forget();
            UpdateAgentStateAsync(eval).Forget();
            UpdateCrystalRandomSkillState(eval);

            Widget.Find<BuffBonusLoadingScreen>().Close();
            Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(false);
            var skillState = States.Instance.CrystalRandomSkillState;
            Widget.Find<BuffBonusResultPopup>().Show(skillState.StageId, skillState);
        }

        private void ResponseStake(ActionBase.ActionEvaluation<Stake> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            NotificationSystem.Push(
                MailType.System,
                L10nManager.Localize("UI_MONSTERCOLLECTION_UPDATED"),
                NotificationCell.NotificationType.Information);

            var (state, level, balance) = GetStakeState(eval);
            if (state != null)
            {
                UpdateStakeState(state, new GoldBalanceState(state.address, balance), level);
            }

            UpdateAgentStateAsync(eval).Forget();
        }

        private void ResponseClaimStakeReward(ActionBase.ActionEvaluation<ClaimStakeReward> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            // Notification
            NotificationSystem.Push(
                MailType.System,
                L10nManager.Localize("NOTIFICATION_CLAIM_MONSTER_COLLECTION_REWARD_COMPLETE"),
                NotificationCell.NotificationType.Information);

            UpdateCurrentAvatarStateAsync(eval).Forget();
        }

        public static void RenderQuest(Address avatarAddress, IEnumerable<int> ids)
        {
            if (avatarAddress != States.Instance.CurrentAvatarState.address)
            {
                return;
            }

            var questList = States.Instance.CurrentAvatarState.questList;
            foreach (var id in ids)
            {
                var quest = questList.First(q => q.Id == id);
                var rewardMap = quest.Reward.ItemMap;

                foreach (var reward in rewardMap)
                {
                    var materialRow = Game.Game.instance.TableSheets
                        .MaterialItemSheet
                        .First(pair => pair.Key == reward.Key);

                    LocalLayerModifier.RemoveItem(
                        avatarAddress,
                        materialRow.Value.ItemId,
                        reward.Value);
                }

                LocalLayerModifier.AddReceivableQuest(avatarAddress, id);
            }
        }

        private static ItemBase GetItem(IAccountStateDelta state, Guid tradableId)
        {
            var address = Addresses.GetItemAddress(tradableId);
            if (state.GetState(address) is Dictionary dictionary)
            {
                return ItemFactory.Deserialize(dictionary);
            }

            return null;
        }

        private class LocalRandom : System.Random, IRandom
        {
            public LocalRandom(int Seed)
                : base(Seed)
            {
            }

            public int Seed => throw new NotImplementedException();
        }


#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
        private void Testbed()
        {
            _actionRenderer.EveryRender<CreateTestbed>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseTestbed)
                .AddTo(_disposables);

            _actionRenderer.EveryRender<CreateArenaDummy>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseCreateArenaDummy)
                .AddTo(_disposables);
        }

        private void ResponseTestbed(ActionBase.ActionEvaluation<CreateTestbed> eval)
        {
        }

        private void ResponseCreateArenaDummy(ActionBase.ActionEvaluation<CreateArenaDummy> eval)
        {
        }
#endif

        private static async UniTaskVoid ResponseJoinArenaAsync(ActionBase.ActionEvaluation<JoinArena> eval)
        {
            if (eval.Action.avatarAddress != States.Instance.CurrentAvatarState.address)
            {
                return;
            }

            var arenaJoin = Widget.Find<ArenaJoin>();
            if (eval.Exception != null)
            {
                if (arenaJoin && arenaJoin.IsActive())
                {
                    arenaJoin.OnRenderJoinArena(eval);
                }
            }

            UpdateCrystalBalance(eval);

            var currentRound = TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(
                Game.Game.instance.Agent.BlockIndex);
            if (eval.Action.championshipId == currentRound.ChampionshipId &&
                eval.Action.round == currentRound.Round)
            {
                await UniTask.WhenAll(
                    RxProps.ArenaInfoTuple.UpdateAsync(),
                    RxProps.ArenaParticipantsOrderedWithScore.UpdateAsync());
            }
            else
            {
                await RxProps.ArenaInfoTuple.UpdateAsync();
            }

            if (arenaJoin && arenaJoin.IsActive())
            {
                arenaJoin.OnRenderJoinArena(eval);
            }
        }

        private void ResponseBattleArena(ActionBase.ActionEvaluation<BattleArena> eval)
        {
            if (!ActionManager.IsLastBattleActionId(eval.Action.Id) ||
                eval.Action.myAvatarAddress != States.Instance.CurrentAvatarState.address)
            {
                return;
            }

            var arenaBattlePreparation = Widget.Find<ArenaBattlePreparation>();
            if (eval.Exception != null)
            {
                if (arenaBattlePreparation && arenaBattlePreparation.IsActive())
                {
                    arenaBattlePreparation.OnRenderBattleArena(eval);
                }

                Game.Game.BackToMainAsync(eval.Exception.InnerException, false).Forget();

                return;
            }

            // NOTE: Start cache some arena info which will be used after battle ends.
            RxProps.ArenaInfoTuple.UpdateAsync().Forget();
            RxProps.ArenaParticipantsOrderedWithScore.UpdateAsync().Forget();

            _disposableForBattleEnd?.Dispose();
            _disposableForBattleEnd = Game.Game.instance.Arena.OnArenaEnd
                .First()
                .Subscribe(_ =>
                {
                    UniTask.Run(() =>
                        {
                            UpdateAgentStateAsync(eval).Forget();
                            UpdateCurrentAvatarStateAsync().Forget();
                            // TODO!!!! [`PlayersArenaParticipant`]를 개별로 업데이트 한다.
                            // RxProps.PlayersArenaParticipant.UpdateAsync().Forget();
                            _disposableForBattleEnd = null;
                            Game.Game.instance.Arena.IsAvatarStateUpdatedAfterBattle = true;
                        }).ToObservable()
                        .First()
                        // ReSharper disable once ConvertClosureToMethodGroup
                        .DoOnError(e => Debug.LogException(e));
                });

            var tableSheets = Game.Game.instance.TableSheets;
            ArenaPlayerDigest? myDigest = null;
            ArenaPlayerDigest? enemyDigest = null;
            int? previousMyScore = null;
            int? outputMyScore = null;
            if (eval.Extra is { })
            {
                myDigest = eval.Extra.TryGetValue(
                    nameof(BattleArena.ExtraMyArenaPlayerDigest),
                    out var myDigestValue)
                    ? myDigestValue is List myDigestList
                        ? new ArenaPlayerDigest(myDigestList)
                        : (ArenaPlayerDigest?)null
                    : null;

                enemyDigest = eval.Extra.TryGetValue(
                    nameof(BattleArena.ExtraEnemyArenaPlayerDigest),
                    out var enemyDigestValue)
                    ? enemyDigestValue is List enemyDigestList
                        ? new ArenaPlayerDigest(enemyDigestList)
                        : (ArenaPlayerDigest?)null
                    : null;

                previousMyScore = eval.Extra.TryGetValue(
                    nameof(BattleArena.ExtraPreviousMyScore),
                    out var previousMyScoreValue)
                    ? previousMyScoreValue is Text previousMyScoreText
                        ? previousMyScoreText.ToInteger()
                        : ArenaScore.ArenaScoreDefault
                    : ArenaScore.ArenaScoreDefault;
                
                // TODO: Add `ExtraOutputMyScore` to `BattleArena`
                outputMyScore = null;
            }

            if (!myDigest.HasValue)
            {
                var myAvatarState
                    = eval.OutputStates.GetAvatarStateV2(eval.Action.myAvatarAddress);
                if (!eval.OutputStates.TryGetArenaAvatarState(
                        ArenaAvatarState.DeriveAddress(eval.Action.myAvatarAddress),
                        out var myArenaAvatarState))
                {
                    Debug.LogError("Failed to get ArenaAvatarState of mine");
                }

                myDigest
                    = new ArenaPlayerDigest(myAvatarState, myArenaAvatarState);
            }

            if (!enemyDigest.HasValue)
            {
                var enemyAvatarState
                    = eval.OutputStates.GetAvatarStateV2(eval.Action.enemyAvatarAddress);
                if (!eval.OutputStates.TryGetArenaAvatarState(
                        ArenaAvatarState.DeriveAddress(eval.Action.enemyAvatarAddress),
                        out var enemyArenaAvatarState))
                {
                    Debug.LogError("Failed to get ArenaAvatarState of enemy");
                }

                enemyDigest
                    = new ArenaPlayerDigest(enemyAvatarState, enemyArenaAvatarState);
            }

            previousMyScore ??= RxProps.PlayersArenaParticipant.HasValue
                ? RxProps.PlayersArenaParticipant.Value.Score
                : ArenaScore.ArenaScoreDefault;

            outputMyScore ??= eval.OutputStates.TryGetState(
                ArenaScore.DeriveAddress(
                    eval.Action.myAvatarAddress,
                    eval.Action.championshipId,
                    eval.Action.round),
                out List outputMyScoreList)
                ? (int)(Integer)outputMyScoreList[1]
                : ArenaScore.ArenaScoreDefault;

            var random = new LocalRandom(eval.RandomSeed);
            // TODO!!!! ticket 수 만큼 돌려서 마지막 전투 결과를 띄운다.
            // eval.Action.ticket
            var simulator = new ArenaSimulator(random);
            var log = simulator.Simulate(
                myDigest.Value,
                enemyDigest.Value,
                tableSheets.GetArenaSimulatorSheets());
            log.Score = outputMyScore.Value;

            var rewards = RewardSelector.Select(
                random,
                tableSheets.WeeklyArenaRewardSheet,
                tableSheets.MaterialItemSheet,
                myDigest.Value.Level,
                maxCount: ArenaHelper.GetRewardCount(previousMyScore.Value));
            if (log.Result == ArenaLog.ArenaResult.Win)
            {
                var championshipId = eval.Action.championshipId;
                var round = eval.Action.round;
                var hasMedalReward =
                    tableSheets.ArenaSheet.TryGetValue(
                        championshipId,
                        out var row) &&
                    row.Round.Any(e =>
                        e.Round == round &&
                        e.ArenaType != ArenaType.OffSeason);
                if (hasMedalReward)
                {
                    var medalItemId = ArenaHelper.GetMedalItemId(
                        eval.Action.championshipId,
                        eval.Action.round);
                    var medalItem = ItemFactory.CreateMaterial(
                        tableSheets.MaterialItemSheet,
                        medalItemId);
                    if (medalItem is { })
                    {
                        rewards.Add(medalItem);
                    }
                }
            }

            if (arenaBattlePreparation && arenaBattlePreparation.IsActive())
            {
                arenaBattlePreparation.OnRenderBattleArena(eval);
                Game.Game.instance.Arena.Enter(
                    log,
                    rewards,
                    myDigest.Value,
                    enemyDigest.Value);
            }
        }
    }
}
