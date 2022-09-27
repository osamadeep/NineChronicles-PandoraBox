﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libplanet.Assets;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossRuneStoneInventory : WorldBossDetailItem
    {
        [SerializeField]
        private RuneStoneInventoryScroll scroll;

        public async void ShowAsync()
        {
            var items = new List<RuneStoneInventoryItem>();
            var worldBossSheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            var bossIds = worldBossSheet.Values.Select(x => x.BossId).Distinct();
            await foreach (var bossId in bossIds)
            {
                if (!WorldBossFrontHelper.TryGetRunes(bossId, out var runeRows))
                {
                    continue;
                }

                var runes = await GetRunes(runeRows);
                var item = new RuneStoneInventoryItem(runes, bossId);
                items.Add(item);
            }

            scroll.UpdateData(items);
        }

        private async Task<List<FungibleAssetValue>> GetRunes(List<RuneSheet.Row> rows)
        {
            var address = States.Instance.CurrentAvatarState.address;
            var task = Task.Run(async () =>
            {
                var runes = new List<FungibleAssetValue>();
                await foreach (var row in rows)
                {
                    var rune = RuneHelper.ToCurrency(row, 0, null);
                    var fungibleAsset = await Game.Game.instance.Agent.GetBalanceAsync(address, rune);
                    runes.Add(fungibleAsset);
                }

                return runes;
            });

            await task;
            return task.Result;
        }
    }
}
