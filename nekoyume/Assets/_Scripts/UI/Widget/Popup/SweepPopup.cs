using System;
using System.Collections.Generic;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    using Nekoyume.BlockChain;
    using Nekoyume.PandoraBox;
    using UniRx;

    public class SweepPopup : PopupWidget
    {
        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")]
        [SerializeField] private UnityEngine.UI.Toggle extendSlider;
        [SerializeField] private Button multiRepeatBtn;
        [SerializeField] private TMP_InputField stageIDText;
        Nekoyume.Model.Item.Inventory.Item apStonePandora = null;
        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        [SerializeField]
        private SweepSlider apSlider;

        [SerializeField]
        private SweepSlider apStoneSlider;

        [SerializeField]
        private ConditionalButton startButton;

        [SerializeField]
        private Button cancelButton;

        [SerializeField]
        private TextMeshProUGUI expText;

        [SerializeField]
        private TextMeshProUGUI starText;

        [SerializeField]
        private TextMeshProUGUI totalApText;

        [SerializeField]
        private TextMeshProUGUI apStoneText;

        [SerializeField]
        private TextMeshProUGUI haveApText;

        [SerializeField]
        private TextMeshProUGUI haveApStoneText;

        [SerializeField]
        private TextMeshProUGUI enoughCpText;

        [SerializeField]
        private TextMeshProUGUI insufficientCpText;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private GameObject enoughCpContainer;

        [SerializeField]
        private GameObject insufficientCpContainer;

        [SerializeField]
        private GameObject information;

        [SerializeField]
        private GameObject expGlow;

        [SerializeField]
        private Toggle pageToggle;

        [SerializeField]
        private CanvasGroup canvasGroupForRepeat;

        [SerializeField]
        private List<GameObject> objectsForSweep;

        [SerializeField]
        private List<GameObject> objectsForRepeat;

        private readonly ReactiveProperty<int> _apStoneCount = new ReactiveProperty<int>();
        private readonly ReactiveProperty<int> _ap = new ReactiveProperty<int>();
        private readonly ReactiveProperty<int> _cp = new ReactiveProperty<int>();
        private readonly List<Guid> _equipments = new List<Guid>();
        private readonly List<Guid> _costumes = new List<Guid>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private StageSheet.Row _stageRow;
        private int _worldId;
        private int _costAp;
        private bool _useSweep = true;
        private Action<StageType, int, bool> _repeatBattleAction;

        protected override void Awake()
        {
            _apStoneCount.Subscribe(v => UpdateView()).AddTo(gameObject);
            _ap.Subscribe(v => UpdateView()).AddTo(gameObject);
            _cp.Subscribe(v => UpdateCpView()).AddTo(gameObject);
            pageToggle.onValueChanged.AddListener(UpdateByToggle);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            extendSlider.onValueChanged.AddListener(_ =>
            {
                if (extendSlider.isOn)
                {
                    apStoneSlider.Set(Math.Min(haveApStoneCount, 100), 0,100, 1,x => _apStoneCount.Value = x);
                }
                else
                {
                    apStoneSlider.Set(Math.Min(haveApStoneCount, HackAndSlashSweep.UsableApStoneCount),
                    0,HackAndSlashSweep.UsableApStoneCount, 1,x => _apStoneCount.Value = x);
                }
            });

            multiRepeatBtn.onClick.AddListener(() => StartCoroutine(RepeatMultiple()));

            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            pageToggle.onValueChanged.AddListener(UpdateByToggle);

            startButton.OnSubmitSubject
                .Subscribe(_ =>
                {
                    if (_useSweep)
                    {
                        Sweep(_apStoneCount.Value, _ap.Value, _worldId, _stageRow);
                    }
                    else
                    {
                        _repeatBattleAction(
                            StageType.HackAndSlash,
                            _ap.Value / _costAp,
                            false);

                        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                        //save last stage id
                        PlayerPrefs.SetInt("_PandoraBox_PVE_LastRaidStage_" + States.Instance.CurrentAvatarState.address, _stageRow.Id);
                        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                        Close();
                    }
                })
                .AddTo(gameObject);

            cancelButton.onClick.AddListener(() => Close());

            CloseWidget = () => { Close(); };

            base.Awake();
        }

        public void Show(
            int worldId,
            int stageId,
            Action<StageType, int, bool> repeatBattleAction,
            bool ignoreShowAnimation = false)
        {
            if (!Game.Game.instance.TableSheets.StageSheet.TryGetValue(stageId, out var stageRow))
            {
                throw new Exception();
            }

            SubscribeInventory();

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            extendSlider.isOn = false;
            RepeatMultipleIsOn = false;
            stageIDText.text = stageId.ToString();
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            _worldId = worldId;
            _stageRow = stageRow;
            _apStoneCount.SetValueAndForceNotify(0);
            _ap.SetValueAndForceNotify(States.Instance.CurrentAvatarState.actionPoint);
            _cp.SetValueAndForceNotify(States.Instance.CurrentAvatarState.GetCP());
            _repeatBattleAction = repeatBattleAction;
            var disableRepeat = States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(stageId);
            canvasGroupForRepeat.alpha = disableRepeat ? 0 : 1;
            canvasGroupForRepeat.interactable = !disableRepeat;
            pageToggle.isOn = disableRepeat;
            contentText.text =
                $"({L10nManager.Localize("UI_AP")} / {L10nManager.Localize("UI_AP_POTION")})";

            base.Show(ignoreShowAnimation);
        }

        private void UpdateByToggle(bool useSweep)
        {
            objectsForSweep.ForEach(obj => obj.SetActive(useSweep));
            objectsForRepeat.ForEach(obj => obj.SetActive(!useSweep));
            _useSweep = useSweep;
            UpdateView();
        }

        int haveApStoneCount = 0; //|||||||||||||| PANDORA CODE ||||||||||||||||||| just make it public
        private void SubscribeInventory()
        {
            _disposables.DisposeAllAndClear();
            ReactiveAvatarState.Inventory.Subscribe(inventory =>
            {
                if (inventory is null)
                {
                    return;
                }


                _costumes.Clear();
                _equipments.Clear();
                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                haveApStoneCount = 0;
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

                foreach (var item in inventory.Items)
                {
                    if (item.Locked)
                    {
                        continue;
                    }

                    switch (item.item.ItemType)
                    {
                        case ItemType.Costume:
                            var costume = (Costume)item.item;
                            if (costume.equipped)
                            {
                                _costumes.Add(costume.ItemId);
                            }

                            break;

                        case ItemType.Equipment:
                            var equipment = (Equipment)item.item;
                            if (equipment.equipped)
                            {
                                _equipments.Add(equipment.ItemId);
                            }

                            break;

                        case ItemType.Material:
                            if (item.item.ItemSubType != ItemSubType.ApStone)
                            {
                                continue;
                            }

                            if (item.item is ITradableItem tradableItem)
                            {
                                var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
                                if (tradableItem.RequiredBlockIndex > blockIndex)
                                {
                                    continue;
                                }

                                haveApStoneCount += item.count;
                                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                                apStonePandora = item;
                                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                            }
                            else
                            {
                                haveApStoneCount += item.count;
                                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                                apStonePandora = item; 
                                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                            }

                            break;
                    }
                }
                var haveApCount = States.Instance.CurrentAvatarState.actionPoint;

                haveApText.text = haveApCount.ToString();
                haveApStoneText.text = haveApStoneCount.ToString();

                _costAp = States.Instance.StakingLevel > 0
                    ? TableSheets.Instance.StakeActionPointCoefficientSheet.GetActionPointByStaking(
                        _stageRow.CostAP, 1, States.Instance.StakingLevel)
                    : _stageRow.CostAP;
                apSlider.Set(haveApCount / _costAp,
                    haveApCount / _costAp,
                    States.Instance.GameConfigState.ActionPointMax,
                    _costAp,
                    x => _ap.Value = x * _costAp);

                apStoneSlider.Set(Math.Min(haveApStoneCount, HackAndSlashSweep.UsableApStoneCount),
                    0,
                    HackAndSlashSweep.UsableApStoneCount, 1,
                    x => _apStoneCount.Value = x);

                //Debug.LogError(apStonePandora.count);

                _cp.Value = States.Instance.CurrentAvatarState.GetCP();
            }).AddTo(_disposables);
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        void GetApStoneCount()
        {

        }


        bool RepeatMultipleIsOn = false;
        public System.Collections.IEnumerator RepeatMultiple()
        {
            if (!PandoraMaster.CurrentPandoraPlayer.IsPremium())
            {
                OneLineSystem.Push(MailType.System,
                    "<color=green>Pandora Box</color>: This is Premium Feature!",
                    NotificationCell.NotificationType.Alert);
                yield break;
            }

            if (apStonePandora == null)
            {
                OneLineSystem.Push(MailType.System,
                "<color=green>Pandora Box</color>: Failed to find any AP stones!",
                NotificationCell.NotificationType.Alert);
                yield break;
            }

            if (RepeatMultipleIsOn)
            {
                OneLineSystem.Push(MailType.System,
                "<color=green>Pandora Box</color>: Multi Repeat in progress, please wait!",
                NotificationCell.NotificationType.Alert);
                yield break;
            }

            RepeatMultipleIsOn = true;

            //do the ap bar first
            if (_ap.Value >= _stageRow.CostAP)
            {
                _repeatBattleAction(
                StageType.HackAndSlash,
                false,
                _ap.Value / _stageRow.CostAP,
                false);
                OneLineSystem.Push(MailType.System,
                "<color=green>Pandora Box</color>: Sending repeat by AP bar!",
                NotificationCell.NotificationType.Information);
            }


            //repeat the stones
            int iteration = (int)(apStoneSlider.slider.value ); //* HackAndSlashSweep.UsableApStoneCount
            //int apCount = apStonePandora

            for (int i = 0; i < iteration; i++)
            {
                ActionManager.Instance.ChargeActionPoint(apStonePandora.item as Nekoyume.Model.Item.Material)
                .Subscribe();
                yield return new WaitForSeconds(2);
                OneLineSystem.Push(MailType.System,
                $"<color=green>Pandora Box</color>: Sending Repeat using AP Stone {i+1}/{iteration}",
                NotificationCell.NotificationType.Information);
                _repeatBattleAction(
                StageType.HackAndSlash,
                false,
                120 / _stageRow.CostAP,
                false);
                yield return new WaitForSeconds(2);
            }

            Close();
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        private void UpdateCpView()
        {
            if (_stageRow is null)
            {
                return;
            }

            if (!TryGetRequiredCP(_stageRow.Id, out var row))
            {
                return;
            }

            if (_cp.Value < row.RequiredCP)
            {
                enoughCpContainer.SetActive(false);
                insufficientCpContainer.SetActive(true);
                insufficientCpText.text = L10nManager.Localize("UI_SWEEP_CP", row.RequiredCP);
            }
            else
            {
                enoughCpContainer.SetActive(true);
                insufficientCpContainer.SetActive(false);
                enoughCpText.text = L10nManager.Localize("UI_SWEEP_CP", row.RequiredCP);
            }

            UpdateStartButton();
        }


        private void UpdateView()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
            {
                return;
            }

            var (apPlayCount, apStonePlayCount) =
                GetPlayCount(_stageRow, _apStoneCount.Value, _ap.Value, States.Instance.StakingLevel);
            UpdateRewardView(avatarState, _stageRow, apPlayCount, apStonePlayCount);

            var totalPlayCount = apPlayCount + apStonePlayCount;
            if (_apStoneCount.Value == 0 && _ap.Value == 0)
            {
                information.SetActive(true);
                totalApText.text = string.Empty;
                apStoneText.text = string.Empty;
            }
            else
            {
                information.SetActive(false);
                //totalApText.text = (_useSweep ? totalPlayCount : apPlayCount).ToString();
                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                totalApText.text = (totalPlayCount).ToString();
                apStoneText.text = $"(+{apStonePlayCount})";
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

                //apStoneText.text = apStonePlayCount > 0 && _useSweep
                //    ? $"(+{apStonePlayCount})"
                //    : string.Empty;
            }

            UpdateStartButton();
        }

        private void UpdateRewardView(AvatarState avatarState, StageSheet.Row row, int apPlayCount,
            int apStonePlayCount)
        {
            var earnedExp = GetEarnedExp(avatarState,
                row,
                apPlayCount,
                _useSweep ? apStonePlayCount : 0);
            expText.text = $"+{earnedExp}";
            starText.text = $"+{apPlayCount * 2}";
            expGlow.SetActive(earnedExp > 0);
        }

        private static bool TryGetRequiredCP(int stageId, out SweepRequiredCPSheet.Row row)
        {
            var sheet = Game.Game.instance.TableSheets.SweepRequiredCPSheet;
            return sheet.TryGetValue(stageId, out row);
        }

        private static (int, int) GetPlayCount(
            StageSheet.Row row,
            int apStoneCount,
            int ap,
            int stakingLevel)
        {
            if (row is null)
            {
                return (0, 0);
            }

            var actionMaxPoint = States.Instance.GameConfigState.ActionPointMax;
            var costAp = row.CostAP;
            if (stakingLevel > 0)
            {
                costAp = TableSheets.Instance.StakeActionPointCoefficientSheet.GetActionPointByStaking(
                    costAp, 1, stakingLevel);
            }

            var apStonePlayCount = actionMaxPoint / costAp * apStoneCount;
            var apPlayCount = ap / costAp;
            return (apPlayCount, apStonePlayCount);
        }

        private long GetEarnedExp(AvatarState avatarState, StageSheet.Row row, int apPlayCount,
            int apStonePlayCount)
        {
            var levelSheet = Game.Game.instance.TableSheets.CharacterLevelSheet;
            var (_, exp) = avatarState.GetLevelAndExp(levelSheet, row.Id,
                apPlayCount + apStonePlayCount);
            var earnedExp = exp - avatarState.exp;
            return earnedExp;
        }

        private void UpdateStartButton()
        {
            if (!_useSweep)
            {
                startButton.Interactable = _ap.Value > 0;
                return;
            }

            if (_apStoneCount.Value == 0 && _ap.Value == 0)
            {
                startButton.Interactable = false;
                return;
            }

            if (TryGetRequiredCP(_stageRow.Id, out var row))
            {
                if (_cp.Value < row.RequiredCP)
                {
                    startButton.Interactable = false;
                    return;
                }
            }

            startButton.Interactable = true;
        }

        private void Sweep(int apStoneCount, int ap, int worldId, StageSheet.Row stageRow)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var (apPlayCount, apStonePlayCount)
                = GetPlayCount(stageRow, apStoneCount, ap, States.Instance.StakingLevel);
            var totalPlayCount = apPlayCount + apStonePlayCount;
            var actionPoint = apPlayCount * _costAp;
            if (totalPlayCount <= 0)
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("UI_SWEEP_PLAY_COUNT_ZERO"),
                    NotificationCell.NotificationType.Notification);
                return;
            }

            if (!TryGetRequiredCP(stageRow.Id, out var row))
            {
                return;
            }

            if (_cp.Value < row.RequiredCP)
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("ERROR_SWEEP_REQUIRED_CP"),
                    NotificationCell.NotificationType.Notification);
                return;
            }

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            if (apStoneCount > 10)
            {
                if (!PandoraMaster.CurrentPandoraPlayer.IsPremium())
                {
                    OneLineSystem.Push(MailType.System,
                        "<color=green>Pandora Box</color>: This is Premium Feature!",
                        NotificationCell.NotificationType.Alert);
                    return;
                }

                int extraApStoneCount = Mathf.FloorToInt(apStoneCount / 10f);
                apStoneCount -= extraApStoneCount * 10;

                for (int i = 0; i < extraApStoneCount; i++)
                {
                    Game.Game.instance.ActionManager.HackAndSlashSweep(
                    _costumes,
                    _equipments,
                    10,
                    0,
                    worldId,
                    stageRow.Id);

                }
            }

            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            Game.Game.instance.ActionManager.HackAndSlashSweep(
                _costumes,
                _equipments,
                apStoneCount,
                actionPoint,
                worldId,
                stageRow.Id);

            Analyzer.Instance.Track("Unity/HackAndSlashSweep", new Value
            {
                ["stageId"] = stageRow.Id,
                ["apStoneCount"] = apStoneCount,
                ["playCount"] = totalPlayCount,
            });

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            //save last stage id
            PlayerPrefs.SetInt("_PandoraBox_PVE_LastRaidStage_" + States.Instance.CurrentAvatarState.address, stageRow.Id);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||


            Close();

            var earnedExp = GetEarnedExp(avatarState, stageRow, apPlayCount, apStonePlayCount);

            Find<SweepResultPopup>()
                .Show(stageRow, worldId, apPlayCount, apStonePlayCount, earnedExp);
        }
    }
}
