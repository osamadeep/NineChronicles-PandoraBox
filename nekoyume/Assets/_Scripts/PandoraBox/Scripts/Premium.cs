using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Libplanet.Assets;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.PandoraBox;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.PandoraBox
{
    public class Premium
    {
        public static async void ShopRefresh(BuyView view, System.Action<ShopItem,
            RectTransform> ShowItemTooltip, CancellationTokenSource _cancellationTokenSource)
        {
            int cooldown = 50;

            var initWeaponTask = Task.Run(async () =>
            {
                var list = new List<ItemSubType> { ItemSubType.Weapon, };
                await ReactiveShopState.SetBuyDigestsAsync(list);
                return true;
            });

            var initWeaponResult = await initWeaponTask;
            if (initWeaponResult)
            {
                //base.Show(ignoreShowAnimation);
                view.Show(ReactiveShopState.BuyDigest, ShowItemTooltip);
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var initOthersTask = Task.Run(async () =>
            {
                var list = new List<ItemSubType>
                {
                    ItemSubType.Armor,
                    ItemSubType.Belt,
                    ItemSubType.Necklace,
                    ItemSubType.Ring,
                    ItemSubType.Food,
                    ItemSubType.FullCostume,
                    ItemSubType.HairCostume,
                    ItemSubType.EarCostume,
                    ItemSubType.EyeCostume,
                    ItemSubType.TailCostume,
                    ItemSubType.Title,
                    ItemSubType.Hourglass,
                    ItemSubType.ApStone,
                };
                await ReactiveShopState.SetBuyDigestsAsync(list);
                return true;
            }, _cancellationTokenSource.Token);

            if (initOthersTask.IsCanceled)
            {
                return;
            }

            var initOthersResult = await initOthersTask;
            if (!initOthersResult)
            {
                return;
            }

            view.IsDoneLoadItem = true;

            if (PandoraMaster.CurrentPandoraPlayer.IsPremium())
            {
                //Some Premium Code
                cooldown = 0;
            }
            else
            {
                cooldown = 5;
            }

            Button refreshButton = Widget.Find<ShopBuy>().RefreshButton;
            for (int i = cooldown; i > 0; i--)
            {
                refreshButton.GetComponentInChildren<TextMeshProUGUI>().text = i.ToString();
                await Task.Delay(1000);
            }
            refreshButton.GetComponentInChildren<TextMeshProUGUI>().text = "Refresh";
            refreshButton.interactable = true;
        }
    }
}
