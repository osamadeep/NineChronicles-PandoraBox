using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Timer;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Nekoyume.Helper;
using Nekoyume.PandoraBox;
using System.Collections;
using ObservableExtensions = UniRx.ObservableExtensions;

namespace Nekoyume.UI
{
    using UniRx;

    public class Status : Widget
    {
        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")] [SerializeField]
        private TextMeshProUGUI PandoraStatus = null;

        [SerializeField] private Button guildButton;

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        [SerializeField]
        private FramedCharacterView characterView = null;

        [SerializeField] private TextMeshProUGUI textLvName = null;

        [SerializeField] private TextMeshProUGUI textHp = null;

        [SerializeField] private TextMeshProUGUI textExp = null;

        [SerializeField] private Image hpBar = null;

        [SerializeField] private Image expBar = null;

        [SerializeField] private BuffLayout buffLayout = null;

        [SerializeField] private BuffTooltip buffTooltip = null;

        [SerializeField] private BattleTimerView battleTimerView = null;

        private Player _player;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            Game.Event.OnRoomEnter.AddListener(b => Show());
            Game.Event.OnUpdatePlayerEquip.Subscribe(characterView.SetByPlayer)
                .AddTo(gameObject);
            Game.Event.OnUpdatePlayerStatus.Subscribe(SubscribeOnUpdatePlayerStatus)
                .AddTo(gameObject);

            CloseWidget = null;
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            guildButton.onClick.AddListener(() => { ShowGuild(); });
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        #endregion

        public override void Show(bool ignoreStartAnimation = false)
        {
            base.Show(ignoreStartAnimation);
            battleTimerView.Close();
            hpBar.transform.parent.gameObject.SetActive(false);
            buffLayout.SetBuff(null);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            PandoraStatus.rectTransform.anchoredPosition = new Vector2(95, -52.5f);
            StopAllCoroutines();
            StartCoroutine(UpdateRemainingPremium());
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        public void ShowBattleStatus()
        {
            hpBar.transform.parent.gameObject.SetActive(true);
            PandoraStatus.rectTransform.anchoredPosition = new Vector2(95, -79f);
        }

        public void ShowBattleTimer(int timeLimit)
        {
            battleTimerView.Show(timeLimit);
        }

        // NOTE: call from Hierarchy
        public void ShowBuffTooltip(GameObject sender)
        {
            var icon = sender.GetComponent<BuffIcon>();
            var iconRectTransform = icon.image.rectTransform;

            buffTooltip.gameObject.SetActive(true);
            buffTooltip.UpdateText(icon.Data);
            buffTooltip.RectTransform.anchoredPosition =
                iconRectTransform.anchoredPosition + Vector2.down * iconRectTransform.sizeDelta.y;
        }

        // NOTE: call from Hierarchy
        public void HideBuffTooltip()
        {
            buffTooltip.gameObject.SetActive(false);
        }

        public void UpdateOnlyPlayer(Player player)
        {
            characterView.SetByPlayer(player);

            if (player)
            {
                _player = player;
            }

            UpdateExp();
        }

        public void UpdatePlayer(Player player)
        {
            characterView.SetByPlayer(player);
            Show();

            if (player)
            {
                _player = player;
            }

            UpdateExp();
        }

        private void SubscribeOnUpdatePlayerStatus(Player player)
        {
            if (player is null ||
                player is EnemyPlayer ||
                player.Model is null)
            {
                return;
            }

            UpdateExp();
            buffLayout.SetBuff(player.Model.Buffs);
        }

        private void UpdateExp()
        {
            if (!_player)
            {
                return;
            }

            var level = _player.Level;
            //var avatarName = States.Instance.CurrentAvatarState.NameWithHash;
            //textLvName.text = $"<color=#B38271>LV. {level}</color> {avatarName}";
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            if (gameObject.activeInHierarchy)
                UpdataPandoraStatus();
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            var displayHp = _player.CurrentHP;
            textHp.text = $"{displayHp} / {_player.HP}";
            textExp.text =
                $"{_player.Model.Exp.Need - _player.EXPMax + _player.EXP} / {_player.Model.Exp.Need}";

            var hpValue = _player.CurrentHP / (float)_player.HP;
            hpBar.gameObject.SetActive(hpValue > 0.0f);
            hpValue = Mathf.Min(Mathf.Max(hpValue, 0.1f), 1.0f);
            hpBar.fillAmount = hpValue;

            var expNeed = _player.Model.Exp.Need;
            var levelExp = _player.EXPMax - expNeed;
            var expValue = (float)(_player.EXP - levelExp) / expNeed;
            expBar.gameObject.SetActive(expValue > 0.0f);
            expValue = Mathf.Min(Mathf.Max(expValue, 0.1f), 1.0f);
            expBar.fillAmount = expValue;
        }

        public void UpdateForLobby(
            AvatarState avatarState,
            List<Equipment> equipments,
            List<Costume> costumes
        )
        {
            // portrait
            var portraitId = Util.GetPortraitId(equipments, costumes);
            characterView.SetByFullCostumeOrArmorId(portraitId);

            // level& name
            textLvName.text = $"<color=#B38271>LV. {avatarState.level}</color> {avatarState.NameWithHash}";
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            if (!(PandoraMaster.CurrentGuildPlayer is null))
                textLvName.text =
                    $"<color=#B38271>LV. {avatarState.level}</color> <color=#8488BC>[</color><color=green>{PandoraMaster.CurrentGuildPlayer.Guild}" +
                    $"</color><color=#8488BC>]</color> {States.Instance.CurrentAvatarState.name}";
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            // exp
            var levelSheet = Game.Game.instance.TableSheets.CharacterLevelSheet;
            if (levelSheet.TryGetValue(avatarState.level, out var levelRow))
            {
                var currentExp = avatarState.exp - levelRow.Exp;
                textExp.text = $"{currentExp} / {levelRow.ExpNeed}";
                expBar.fillAmount = (float)currentExp / levelRow.ExpNeed;
            }
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        public void ShowGuild()
        {
            if (PandoraMaster.CurrentGuildPlayer is null)
                return;
            Find<GuildInfo>().Show(PandoraMaster.CurrentGuildPlayer.Guild);
        }

        void UpdataPandoraStatus()
        {
            try
            {
                PandoraStatus.gameObject.SetActive(Premium.PandoraProfile.IsPremium());
            }
            catch
            {
            }

            var level = _player.Level;
            textLvName.text = $"<color=#B38271>LV. {level}</color> {States.Instance.CurrentAvatarState.name}";

            if (!(PandoraMaster.CurrentGuildPlayer is null))
                textLvName.text =
                    $"<color=#B38271>LV. {level}</color> <color=#8488BC>[</color><color=green>{PandoraMaster.CurrentGuildPlayer.Guild}" +
                    $"</color><color=#8488BC>]</color> {States.Instance.CurrentAvatarState.name}";
        }

        IEnumerator UpdateRemainingPremium()
        {
            while (true)
            {
                try
                {
                    //Debug.LogError("pre? " + Premium.CurrentPandoraPlayer.IsPremium());
                    PandoraStatus.gameObject.SetActive(Premium.PandoraProfile.IsPremium());
                    if (Premium.PandoraProfile.IsPremium())
                    {
                        int blocks = Premium.PandoraProfile.Statistics
                            .FirstOrDefault(s => s.StatisticName == PandoraMaster.PanDatabase.PremiumStats)?.Value ?? 0;
                        var timeR = Util.GetBlockToTime(blocks - (int)Game.Game.instance.Agent.BlockIndex);
                        PandoraStatus.text = $"{timeR} ({blocks - (int)Game.Game.instance.Agent.BlockIndex})";
                    }
                }
                catch
                {
                }

                yield return new WaitForSeconds(10);
            }
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
    }
}