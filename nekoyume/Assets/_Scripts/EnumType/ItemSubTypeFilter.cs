using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.EnumType
{
    public enum ItemSubTypeFilter
    {
        All,
        Weapon,
        Armor,
        Belt,
        Necklace,
        Ring,

        Food_HP,
        Food_ATK,
        Food_DEF,
        Food_CRI,
        Food_HIT,

        FullCostume,
        HairCostume,
        EarCostume,
        EyeCostume,
        TailCostume,
        Title,
        Materials,

        Equipment,
        Food,
        Costume,
    }

    public static class ItemSubTypeFilterExtension
    {
        public static IEnumerable<ItemSubTypeFilter> Filters
        {
            get
            {
                return new[]
                {
                    ItemSubTypeFilter.All,
                    ItemSubTypeFilter.Weapon,
                    ItemSubTypeFilter.Armor,
                    ItemSubTypeFilter.Belt,
                    ItemSubTypeFilter.Necklace,
                    ItemSubTypeFilter.Ring,
                    ItemSubTypeFilter.Food_HP,
                    ItemSubTypeFilter.Food_ATK,
                    ItemSubTypeFilter.Food_DEF,
                    ItemSubTypeFilter.Food_CRI,
                    ItemSubTypeFilter.Food_HIT,
                    ItemSubTypeFilter.FullCostume,
                    ItemSubTypeFilter.HairCostume,
                    ItemSubTypeFilter.EarCostume,
                    ItemSubTypeFilter.EyeCostume,
                    ItemSubTypeFilter.TailCostume,
                    ItemSubTypeFilter.Title,
                    ItemSubTypeFilter.Materials,
                };
            }
        }

        public static string TypeToString(this ItemSubTypeFilter type, bool useSell = false)
        {
            switch (type)
            {
                case ItemSubTypeFilter.All:
                    return L10nManager.Localize("ALL");
                case ItemSubTypeFilter.Equipment:
                    return L10nManager.Localize("UI_EQUIPMENTS");
                case ItemSubTypeFilter.Costume:
                    return L10nManager.Localize("UI_COSTUME");
                case ItemSubTypeFilter.Food_HP:
                    return useSell
                        ? $"{StatType.HP.ToString()} {ItemSubType.Food.GetLocalizedString()}"
                        : StatType.HP.ToString();
                case ItemSubTypeFilter.Food_ATK:
                    return useSell
                        ? $"{StatType.ATK.ToString()} {ItemSubType.Food.GetLocalizedString()}"
                        : StatType.ATK.ToString();
                case ItemSubTypeFilter.Food_DEF:
                    return useSell
                        ? $"{StatType.DEF.ToString()} {ItemSubType.Food.GetLocalizedString()}"
                        : StatType.DEF.ToString();
                case ItemSubTypeFilter.Food_CRI:
                    return useSell
                        ? $"{StatType.CRI.ToString()} {ItemSubType.Food.GetLocalizedString()}"
                        : StatType.CRI.ToString();
                case ItemSubTypeFilter.Food_HIT:
                    return useSell
                        ? $"{StatType.HIT.ToString()} {ItemSubType.Food.GetLocalizedString()}"
                        : StatType.HIT.ToString();
                case ItemSubTypeFilter.Materials:
                    return L10nManager.Localize("UI_MATERIALS");

                default:
                    return ((ItemSubType)Enum.Parse(typeof(ItemSubType), type.ToString()))
                        .GetLocalizedString();
            }
        }

        public static ItemSubTypeFilter StatTypeToItemSubTypeFilter(StatType statType)
        {
            switch (statType)
            {
                case StatType.HP:
                    return ItemSubTypeFilter.Food_HP;
                case StatType.ATK:
                    return ItemSubTypeFilter.Food_ATK;
                case StatType.DEF:
                    return ItemSubTypeFilter.Food_DEF;
                case StatType.CRI:
                    return ItemSubTypeFilter.Food_CRI;
                case StatType.HIT:
                    return ItemSubTypeFilter.Food_HIT;
                case StatType.SPD:
                case StatType.NONE:
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }

        public static List<ItemSubTypeFilter> GetItemSubTypeFilter(int itemId)
        {
            var result = new List<ItemSubTypeFilter>();
            var row = TableSheets.Instance.ItemSheet[itemId];
            if (row.ItemType == ItemType.Consumable)
            {
                var consumableRow = (ConsumableItemSheet.Row) row;
                foreach (var statMap in consumableRow.Stats)
                {
                    switch (statMap.StatType)
                    {
                        case StatType.HP:
                            result.Add(ItemSubTypeFilter.Food_HP);
                            break;
                        case StatType.ATK:
                            result.Add(ItemSubTypeFilter.Food_ATK);
                            break;
                        case StatType.DEF:
                            result.Add(ItemSubTypeFilter.Food_DEF);
                            break;
                        case StatType.CRI:
                            result.Add(ItemSubTypeFilter.Food_CRI);
                            break;
                        case StatType.HIT:
                            result.Add(ItemSubTypeFilter.Food_HIT);
                            break;
                        case StatType.SPD:
                        case StatType.NONE:
                        default:
                            throw new ArgumentOutOfRangeException(
                                nameof(statMap.StatType),
                                statMap.StatType,
                                null);
                    }
                }

                return result;
            }

            switch (row.ItemSubType)
            {
                case ItemSubType.Weapon:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.Weapon };
                case ItemSubType.Armor:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.Armor };
                case ItemSubType.Belt:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.Belt };
                case ItemSubType.Necklace:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.Necklace };
                case ItemSubType.Ring:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.Ring };
                case ItemSubType.FullCostume:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.FullCostume };
                case ItemSubType.HairCostume:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.HairCostume };
                case ItemSubType.EarCostume:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.EarCostume };
                case ItemSubType.EyeCostume:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.EyeCostume };
                case ItemSubType.TailCostume:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.TailCostume };
                case ItemSubType.Title:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.Title };
                case ItemSubType.Hourglass:
                case ItemSubType.ApStone:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.Materials };
                case ItemSubType.Food:
                case ItemSubType.EquipmentMaterial:
                case ItemSubType.FoodMaterial:
                case ItemSubType.MonsterPart:
                case ItemSubType.NormalMaterial:
                case ItemSubType.Chest:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
