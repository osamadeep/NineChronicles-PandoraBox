using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class NineRunnerPopup : PopupWidget
    {
        protected override void Awake()
        {
            base.Awake();

        }

        public void Show()
        {
            base.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }

        public void ComingSoon()
        {
            OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: coming soon!",
                NotificationCell.NotificationType.Information);
            return;
        }
    }
}
