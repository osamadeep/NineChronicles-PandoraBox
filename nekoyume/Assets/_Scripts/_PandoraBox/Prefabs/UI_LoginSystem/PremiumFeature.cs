using Nekoyume.PandoraBox;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekoyume
{
    public class PremiumFeature : MonoBehaviour
    {
        [SerializeField] string PremiumMessage;

        public void ShowPremiumMessage()
        {
            PandoraUtil.ShowSystemNotification(PremiumMessage, UI.Scroller.NotificationCell.NotificationType.Alert);
        }
    }
}