using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class CombinationSlotPopup : PopupWidget
    {
        public enum CraftType
        {
            CombineEquipment,
            CombineConsumable,
            Enhancement,
        }

        [Serializable]
        public class Information
        {
            public CraftType Type;
            public GameObject Icon;
            public GameObject OptionContainer;
            public TextMeshProUGUI ItemLevel;
            public ItemOptionView MainStatView;
            public List<ItemOptionWithCountView> StatOptions;
            public List<ItemOptionView> SkillOptions;
        }

        [SerializeField] private SimpleItemView itemView;

        [SerializeField] private Slider progressBar;

        [SerializeField] private TextMeshProUGUI itemNameText;

        [SerializeField] private TextMeshProUGUI requiredBlockIndexText;

        [SerializeField] private TextMeshProUGUI timeText;

        [SerializeField] private ConditionalCostButton rapidCombinationButton;

        [SerializeField] private Button bgButton;

        [SerializeField] private List<Information> _informations;

        private CraftType _craftType;
        private CombinationSlotState _slotState;
        private int _slotIndex;
        private readonly List<IDisposable> _disposablesOfOnEnable = new();

        protected override void Awake()
        {
            base.Awake();

            rapidCombinationButton.OnSubmitSubject
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Game.Game.instance.ActionManager.RapidCombination(_slotState, _slotIndex).Subscribe();
                    Find<CombinationSlotsPopup>().SetCaching(
                        _slotIndex,
                        true,
                        slotType: CombinationSlot.SlotType.WaitingReceive);
                    Close();
                })
                .AddTo(gameObject);

            bgButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
            });
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeOnBlockIndex)
                .AddTo(_disposablesOfOnEnable);
        }

        protected override void OnDisable()
        {
            _disposablesOfOnEnable.DisposeAllAndClear();
            base.OnDisable();
        }

        private void SubscribeOnBlockIndex(long currentBlockIndex)
        {
            UpdateInformation(_craftType, _slotState, currentBlockIndex);
        }

        public void Show(CombinationSlotState state, int slotIndex, long currentBlockIndex)
        {
            _slotState = state;
            _slotIndex = slotIndex;
            _craftType = GetCombinationType(state);
            UpdateInformation(_craftType, state, currentBlockIndex);
            base.Show();
        }

        private void UpdateInformation(CraftType type, CombinationSlotState state, long currentBlockIndex)
        {
            if (state?.Result.itemUsable is null)
            {
                return;
            }

            UpdateOption(type, state);
            UpdateItemInformation(state.Result.itemUsable);
            UpdateButtonInformation(state, currentBlockIndex);
            UpdateRequiredBlockInformation(state, currentBlockIndex);
        }

        private void UpdateOption(CraftType type, CombinationSlotState slotState)
        {
            
            foreach (var information in _informations)
            {
                information.Icon.SetActive(information.Type.Equals(type));
                information.OptionContainer.SetActive(information.Type.Equals(type));
            }

            switch (slotState.Result)
            {
                case CombinationConsumable5.ResultModel cc5:
                    SetCombinationOption(GetInformation(type), cc5, slotState.RequiredBlockIndex);
                    break;
                case ItemEnhancement7.ResultModel _:
                case ItemEnhancement.ResultModel _:
                    SetEnhancementOption(GetInformation(type), slotState.Result);
                    break;
                default:
                    Debug.LogError(
                        $"[{nameof(CombinationSlotPopup)}] Not supported type. {slotState.Result.GetType().FullName}");
                    break;
            }
        }

        void tt()
        {

        }

        private static void SetCombinationOption(
            Information information,
            CombinationConsumable5.ResultModel resultModel,
            long requiredBlockIndex)
        {
            if (!resultModel.itemUsable.TryGetOptionInfo(out var itemOptionInfo))
            {
                Debug.LogError("Failed to create ItemOptionInfo");
                return;
            }
            // Consumable case
            if (!resultModel.subRecipeId.HasValue)
            {
                var (type, value, _) = itemOptionInfo.StatOptions[0];
                information.MainStatView.UpdateView(
                    $"{type} {type.ValueToString(value)}",
                    string.Empty);

                return;
            }

            var statType = itemOptionInfo.MainStat.type;
            var statValueString = statType.ValueToString(itemOptionInfo.MainStat.baseValue);

            information.MainStatView.UpdateView(
                $"{statType} {statValueString}",
                string.Empty);

            var statOptionRows = ItemOptionHelper.GetStatOptionRows(
                resultModel.subRecipeId.Value,
                resultModel.itemUsable,
                Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheetV2,
                Game.Game.instance.TableSheets.EquipmentItemOptionSheet);

            for (var i = 0; i < information.StatOptions.Count; i++)
            {
                var optionView = information.StatOptions[i];
                if (i >= statOptionRows.Count)
                {
                    optionView.Hide();
                    continue;
                }

                var optionRow = statOptionRows[i];
                if (optionRow is null)
                {
                    optionView.Hide();
                    continue;
                }


                var statMin = optionRow.StatType.ValueToString(optionRow.StatMin);
                var statMax = optionRow.StatType.ValueToString(optionRow.StatMax);
                var text = $"{optionRow.StatType} ({statMin} - {statMax})";
                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                try
                {
                    text = $"{optionRow.StatType} ({optionRow.StatType.ValueToString(optionRow.StatMin)} - {optionRow.StatType.ValueToString(optionRow.StatMax)}) = ";
                    if (statOptionRows.Count == 3)
                    {
                        if (i == 0)
                            text += $"<color=green><b>" + $"{optionRow.StatType.ValueToString(itemOptionInfo.StatOptions[0].value)}</b></color>";
                        else if (i == 1)
                            text += "<color=red><b>Calculated!</b></color>";
                        else if (i == 2)
                            text += $"<color=green><b>" + $"{optionRow.StatType.ValueToString(itemOptionInfo.StatOptions[1].value)}</b></color>";
                    }
                    else
                        text += $"<color=green><b>" + $"{optionRow.StatType.ValueToString(itemOptionInfo.StatOptions[i].value)}</b></color>";
                }
                catch { }
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                optionView.UpdateView(text, string.Empty, 1);
                optionView.Show();
            }

            for (var i = 0; i < information.SkillOptions.Count; i++)
            {
                var optionView = information.SkillOptions[i];
                if (i >= itemOptionInfo.SkillOptions.Count)
                {
                    optionView.Hide();
                    continue;
                }

                var optionRow = statOptionRows[i];
                if (optionRow is null)
                {
                    optionView.Hide();
                    continue;
                }

                //var (skillRow, _, _) = itemOptionInfo.SkillOptions[i];
                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                string tmp = itemOptionInfo.SkillOptions[i].skillRow.GetLocalizedName();
                tmp +=
                    $" = <color=green><b>{itemOptionInfo.SkillOptions[i].power}</b></color> , [<color=green><b>{itemOptionInfo.SkillOptions[i].chance}%</b></color>]";
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                optionView.UpdateView(tmp, string.Empty);
                optionView.Show();
            }
        }

        private static void SetEnhancementOption(Information information, AttachmentActionResult resultModel)
        {
            if (!(resultModel.itemUsable is Equipment equipment))
            {
                Debug.LogError("resultModel.itemUsable is not Equipment");
                return;
            }

            information.ItemLevel.text = $"+{equipment.level}";
            var sheet = Game.Game.instance.TableSheets.EnhancementCostSheetV2;
            var grade = equipment.Grade;
            var level = equipment.level;
            var row = sheet.OrderedList.FirstOrDefault(x => x.Grade == grade && x.Level == level);
            if (row is null)
            {
                Debug.LogError($"Not found row: {nameof(EnhancementCostSheetV2)} Grade({grade}) Level({level})");
                return;
            }

            if (!resultModel.itemUsable.TryGetOptionInfo(out var itemOptionInfo))
            {
                Debug.LogError("Failed to create ItemOptionInfo");
                return;
            }

            var format = "{0} +({1:N0}% - {2:N0}%)";
            if (row.BaseStatGrowthMin == 0 && row.BaseStatGrowthMax == 0)
            {
                information.MainStatView.Hide();
            }
            else
            {
                //information.MainStatView.UpdateView(
                //    string.Format(
                //        format,
                //        itemOptionInfo.MainStat.type,
                //        row.BaseStatGrowthMin.NormalizeFromTenThousandths() * 100,
                //        row.BaseStatGrowthMax.NormalizeFromTenThousandths() * 100),
                //    string.Empty);

                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                string text = string.Format(
                    format,
                    itemOptionInfo.MainStat.type,
                    row.BaseStatGrowthMin.NormalizeFromTenThousandths() * 100,
                    row.BaseStatGrowthMax.NormalizeFromTenThousandths() * 100);
                text += $"= <color=green>{itemOptionInfo.MainStat.totalValue}</color>";
                information.MainStatView.UpdateView(text, string.Empty);
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                information.MainStatView.Show();
            }

            for (var i = 0; i < information.StatOptions.Count; i++)
            {
                var optionView = information.StatOptions[i];
                if (row.ExtraStatGrowthMin == 0 && row.ExtraStatGrowthMax == 0 ||
                    i >= itemOptionInfo.StatOptions.Count)
                {
                    optionView.Hide();
                    continue;
                }

                var (type, _, count) = itemOptionInfo.StatOptions[i];
                var text = string.Format(
                    format,
                    type,
                    row.ExtraStatGrowthMin.NormalizeFromTenThousandths() * 100,
                    row.ExtraStatGrowthMax.NormalizeFromTenThousandths() * 100);
                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                text += $"= <color=green><b>{itemOptionInfo.StatOptions[i].value}</b></color>";
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                optionView.UpdateView(text, string.Empty, count);
                optionView.Show();
            }

            format = "{0} +({1:N0}% - {2:N0}%) / ({3:N0}% - {4:N0}%)";
            for (var i = 0; i < information.SkillOptions.Count; i++)
            {
                var optionView = information.SkillOptions[i];
                if (row.ExtraSkillDamageGrowthMin == 0 && row.ExtraSkillDamageGrowthMax == 0 &&
                    row.ExtraSkillChanceGrowthMin == 0 && row.ExtraSkillChanceGrowthMax == 0 ||
                    i >= itemOptionInfo.SkillOptions.Count)
                {
                    optionView.Hide();
                    continue;
                }

                var (skillRow, _, _) = itemOptionInfo.SkillOptions[i];
                var text = string.Format(
                    format,
                    skillRow.GetLocalizedName(),
                    row.ExtraSkillDamageGrowthMin.NormalizeFromTenThousandths() * 100,
                    row.ExtraSkillDamageGrowthMax.NormalizeFromTenThousandths() * 100,
                    row.ExtraSkillChanceGrowthMin.NormalizeFromTenThousandths() * 100,
                    row.ExtraSkillChanceGrowthMax.NormalizeFromTenThousandths() * 100);
                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                text +=
                    $"=<color=green><b>{itemOptionInfo.SkillOptions[i].power}</b></color> , [<color=green><b>{itemOptionInfo.SkillOptions[i].chance}%</b></color>]";
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                optionView.UpdateView(text, string.Empty);
                optionView.Show();
            }
        }

        private void UpdateRequiredBlockInformation(CombinationSlotState state, long currentBlockIndex)
        {
            progressBar.maxValue = Math.Max(state.RequiredBlockIndex, 1);
            var diff = Math.Max(state.UnlockBlockIndex - currentBlockIndex, 1);
            progressBar.value = diff;
            //requiredBlockIndexText.text = $"{diff}.";
            //timeText.text = string.Format(L10nManager.Localize("UI_REMAINING_TIME"), Util.GetBlockToTime((int)diff));
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            if (PandoraBox.PandoraBoxMaster.Instance.Settings.BlockShowType == 0)
            {
                var timeR = Util.GetBlockToTime((int)diff);
                requiredBlockIndexText.text = timeR;
            }
            else if (PandoraBox.PandoraBoxMaster.Instance.Settings.BlockShowType == 1)
            {
                requiredBlockIndexText.text = $"{diff}";
            }
            else
            {
                var timeR = Util.GetBlockToTime((int)diff);
                requiredBlockIndexText.text = $"{timeR} ({diff})";
            }
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        private void UpdateButtonInformation(CombinationSlotState state, long currentBlockIndex)
        {
            var diff = state.UnlockBlockIndex - currentBlockIndex;
            var cost =
                RapidCombination0.CalculateHourglassCount(States.Instance.GameConfigState, diff);
            rapidCombinationButton.SetCost(CostType.Hourglass, cost);
        }

        private void UpdateItemInformation(ItemUsable item)
        {
            itemView.SetData(new Item(item));
            itemNameText.text = TextHelper.GetItemNameInCombinationSlot(item);
        }

        private static CraftType GetCombinationType(CombinationSlotState state)
        {
            switch (state.Result)
            {
                case CombinationConsumable5.ResultModel craft:
                    return craft.itemUsable is Equipment
                        ? CraftType.CombineEquipment
                        : CraftType.CombineConsumable;

                case ItemEnhancement7.ResultModel _:
                case ItemEnhancement9.ResultModel _:
                case ItemEnhancement10.ResultModel _:
                case ItemEnhancement.ResultModel _:
                    return CraftType.Enhancement;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state.Result), state.Result, null);
            }
        }

        private Information GetInformation(CraftType type)
        {
            return _informations.FirstOrDefault(x => x.Type.Equals(type));
        }
    }
}
