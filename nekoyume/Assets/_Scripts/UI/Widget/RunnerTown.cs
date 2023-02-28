using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.Model.BattleStatus;
using UnityEngine;
using Random = UnityEngine.Random;
using mixpanel;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Lobby;
using Nekoyume.UI.Module.WorldBoss;
using TMPro;
using UnityEngine.UI;
using StateExtensions = Nekoyume.Model.State.StateExtensions;

namespace Nekoyume.UI
{
    using Cysharp.Threading.Tasks;
    using Libplanet;
    using Libplanet.Blocks;
    using Nekoyume.Helper;
    using Nekoyume.UI.Model;
    using Nekoyume.UI.Scroller;
    using PandoraBox;
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using TMPro;
    using Scroller;
    using UniRx;
    using PlayFab;
    using PlayFab.ClientModels;

    public class RunnerTown : Widget
    {
        [SerializeField] RunnerInventory inventory;
        [SerializeField] TextMeshProUGUI nameTxt;
        [SerializeField] TextMeshProUGUI pgTxt;
        [SerializeField] TextMeshProUGUI pcTxt;
        [SerializeField] TextMeshProUGUI prTxt;

        protected override void Awake()
        {
            base.Awake();

            //Game.Event.OnRoomEnter.AddListener(b => Show());

            CloseWidget = null;

            //fastSwitchButton.onClick.AddListener(() => { FastCharacterSwitch(); });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Find<HeaderMenuStatic>().Close(true);
            Find<EventBanner>().Close(true);
            Find<Status>().Close(true);
            Find<Menu>().Close(true);

            Game.Game.instance.Runner.player.transform.position = new Vector2(-5000, -5000);
            Game.Game.instance.Runner.player.gameObject.SetActive(true);

            AudioController.instance.PlayMusic(AudioController.MusicCode.RunnerTown);

            nameTxt.text = PandoraMaster.PlayFabPlayerProfile.DisplayName;
            pgTxt.text = PandoraUtil.ToLongNumberNotation(Premium.PandoraProfile.Currencies["PG"]).ToString();
            pcTxt.text = PandoraUtil.ToLongNumberNotation(Premium.PandoraProfile.Currencies["PC"]).ToString();
            prTxt.text = PandoraUtil.ToLongNumberNotation(Premium.PandoraProfile.Currencies["PR"]).ToString();

            base.Show(ignoreShowAnimation);
        }

        public void StartPlay()
        {
            Game.Game.instance.Runner.OnRunnerStart();
            Close(true);
        }

        public void Ranking()
        {
            Find<RunnerRankPopup>().Show();
        }

        public void BackToMain()
        {
            Game.Event.OnRoomEnter.Invoke(true);
            Close(true);
        }

        public void ShowInventory()
        {
            AudioController.PlayClick();
            inventory.gameObject.SetActive(!inventory.gameObject.activeInHierarchy);
        }

        public void ComingSoon()
        {
            OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Coming Soon!",
                NotificationCell.NotificationType.Information);
            return;
        }
    }
}