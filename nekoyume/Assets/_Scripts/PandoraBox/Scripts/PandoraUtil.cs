using Nekoyume.Model.Mail;
using Nekoyume.UI;
using Nekoyume.UI.Scroller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.PandoraBox
{
    public class PandoraUtil : MonoBehaviour
    {
        public enum ActionType { Idle,HackAndSlash,Ranking}

        public static bool IsBusy()
        {
            switch (PandoraBoxMaster.CurrentAction)
            {
                case ActionType.Idle:
                    return false;
                case ActionType.Ranking:
                    OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Arena fight in-progress! Please wait ...", NotificationCell.NotificationType.Alert);
                    return true;
                case ActionType.HackAndSlash:
                    OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Stage fight in-progress! Please wait ...", NotificationCell.NotificationType.Alert);
                    return true;
                default:
                    return false;
            }
        }
    }
}
