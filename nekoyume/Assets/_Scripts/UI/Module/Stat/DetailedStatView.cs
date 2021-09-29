using System;
using System.Collections.Generic;
using System.Globalization;
using Nekoyume.Model.Stat;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class DetailedStatView : StatView
    {
        public TextMeshProUGUI additionalText;

        public void Show(StatType statType, int statValue, int additionalStatValue)
        {
            statTypeText.text = statType.ToString();
            valueText.text = GetStatString(statType, statValue);
            SetAdditional(statType, additionalStatValue);
        }

        public void Show(StatType statType, (int valueMin, int valueMax) valueRange)
        {
            statTypeText.text = statType.ToString();
            valueText.text = statType == StatType.SPD
                ? $"{(valueRange.valueMin / 100f).ToString(CultureInfo.InvariantCulture)} - {(valueRange.valueMax / 100f).ToString(CultureInfo.InvariantCulture)}"
                : $"{valueRange.valueMin} - {valueRange.valueMax}";
            additionalText.text = string.Empty;
            gameObject.SetActive(true);
        }

        public void Show(string keyText, int statValue, int additionalStatValue)
        {
            if (!Enum.TryParse<StatType>(keyText, out var statType))
            {
                Debug.LogError("Failed to parse StatType.");
            }

            Show(statType, statValue, additionalStatValue);
        }

        public void SetAdditional(StatType statType, int additionalStatValue)
        {
            if (additionalStatValue == 0)
            {
                additionalText.text = string.Empty;
            }
            else
            {
                additionalText.text = additionalStatValue > 0
                    ? $"({GetStatString(statType, additionalStatValue, true)})"
                    : $"<color=red>({GetStatString(statType, additionalStatValue, true)})</color>";
            }

            gameObject.SetActive(true);
        }

        protected string GetStatString(StatType statType, float value, bool isSigned = false)
        {
            switch (statType)
            {
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.HIT:
                    return isSigned
                        ? value.ToString("+0.##;-0.##")
                        : value.ToString();
                case StatType.CRI:
                    return isSigned
                        ? value.ToString("+0.##\\%;-0.##\\%")
                        : $"{value:0.#\\%}";
                case StatType.SPD:
                    return isSigned
                        ? (value / 100f).ToString("+0.##;-0.##", CultureInfo.InvariantCulture)
                        : (value / 100f).ToString(CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }
        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        public void Show(StatType statType, float statValue, int additionalStatValue, Nekoyume.Model.Item.Equipment equipment)
        {
            statTypeText.text = statType.ToString();
            if (additionalStatValue != 0)
                valueText.text = GetStatString(statType, statValue);
            else
                valueText.text = GetStatString(statType, statValue, equipment);
            SetAdditional(statType, additionalStatValue, equipment);
        }
        public void SetAdditional(StatType statType, int additionalStatValue, Nekoyume.Model.Item.Equipment equipment)
        {
            if (additionalStatValue == 0)
            {
                additionalText.text = string.Empty;
            }
            else
            {
                additionalText.text = additionalStatValue > 0
                    ? $"({GetStatString(statType, additionalStatValue, true)}){GetMinMaxValues(equipment, additionalStatValue, statType)}"
                    : $"<color=red>({GetStatString(statType, additionalStatValue, true)})</color>";
            }

            gameObject.SetActive(true);
        }

        protected string GetStatString(StatType statType, float value, Nekoyume.Model.Item.Equipment equipment, bool isSigned = false)
        {
            switch (statType)
            {
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.HIT:
                    return isSigned
                        ? value.ToString("+0.##;-0.##") + GetMinMaxValues(equipment, value, statType)
                        : value.ToString();
                case StatType.CRI:
                    return isSigned
                        ? value.ToString("+0.##\\%;-0.##\\%")
                        : $"{value:0.#\\%}";
                case StatType.SPD:
                    return isSigned
                        ? (value / 100f).ToString("+0.##;-0.##", CultureInfo.InvariantCulture) + GetMinMaxValues(equipment, value / 100f, statType)
                        : (value / 100f).ToString(CultureInfo.InvariantCulture) + GetMinMaxValues(equipment, value / 100f, statType);
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }

        string GetMinMaxValues(Nekoyume.Model.Item.Equipment equipment, float value, StatType statType)
        {
            CustomItem temp = GetItemData(equipment.Id);
            float minValue = 0, maxValue = 0;

            //return "--[" + equipment.Id.ToString() + "]";
            if (temp == null)
                return "";

            switch (statType)
            {
                case StatType.ATK:
                    minValue = temp.AtkMin;
                    maxValue = temp.AtkMax;
                    break;
                case StatType.SPD:
                    minValue = temp.SpdMin;
                    maxValue = temp.SpdMax;
                    break;
                case StatType.HIT:
                    minValue = temp.HitMin;
                    maxValue = temp.HitMax;
                    break;
                case StatType.HP:
                    minValue = temp.HpMin;
                    maxValue = temp.HpMax;
                    break;
                case StatType.DEF:
                    minValue = temp.DefMin;
                    maxValue = temp.DefMax;
                    break;
            }


            if (equipment.level == 10)
            {
                minValue *= 2.197f;
                maxValue *= 2.197f;
            }
            else if (equipment.level >= 7)
            {
                minValue *= 1.69f;
                maxValue *= 1.69f;
            }
            else if (equipment.level >= 4)
            {
                minValue *= 1.3f;
                maxValue *= 1.3f;
            }
            if (statType!=StatType.SPD)
                return $"<color=yellow><size=12>[{(int)(minValue)}-{(int)maxValue}]</size></color> {GetPercentage(minValue, maxValue, value)}";
            else
                return $"<color=yellow><size=12>[{String.Format("{0:0.00}", minValue)}-{String.Format("{0:0.0}", maxValue)}]</size></color> {GetPercentage(minValue, maxValue, value)}";
        }

        string GetPercentage(float min, float max, float value)
        {
            float per = (value) / (max);
            if (per < 0.4f)
            {
                return $"<color=red><size=16>{String.Format("{0:0.0}%", per * 100)}</size></color>";
            }
            else if (per >= 0.4f && per < 0.7f)
            {
                return $"<color=#FFA200><size=16>{String.Format("{0:0.0}%", per * 100)}</size></color>";
            }
            else
            {
                return $"<color=green><size=16>{String.Format("{0:0.0}%", per * 100)}</size></color>";
            }

            //return ((value - min) / (max - min)).ToString();


        }

        CustomItem GetItemData(int id)
        {
            List<CustomItem> ItemDataBase = new List<CustomItem>();

            //Black Crow Wind Sword , 10134000
            ItemDataBase.Add(new CustomItem() { ItemID = 10134000, AtkMin = 219, AtkMax = 767, SpdMin = 8.37f, SpdMax = 43.97f });
            //Heavy Sword Earth 220 , 10133001
            ItemDataBase.Add(new CustomItem() { ItemID = 10133001, AtkMin = 304, AtkMax = 849, SpdMin = 9.6f, SpdMax = 40.14f });
            //Black Crow Armor Earth , 10233000
            ItemDataBase.Add(new CustomItem() { ItemID = 10233000, HpMin = 3374, HpMax = 14102, DefMin = 168, DefMax = 470 });
            //Black Crow Armor Wind , 10234000
            ItemDataBase.Add(new CustomItem() { ItemID = 10234000, HpMin = 3653, HpMax = 19179, DefMin = 223, DefMax = 781 });
            //Leather Belt Wind , 10324000
            ItemDataBase.Add(new CustomItem() { ItemID = 10324000, AtkMin = 118, AtkMax = 416, HitMin = 535, HitMax = 2809, SpdMin = 6.8f, SpdMax = 15.88f });
            //Solid Belt Earth , 10333000
            ItemDataBase.Add(new CustomItem() { ItemID = 10333000, AtkMin = 163, AtkMax = 454, HitMin = 661, HitMax = 2763, SpdMin = 7.71f, SpdMax = 14.32f });
            //Guardian Nick Wind , 10424000
            ItemDataBase.Add(new CustomItem() { ItemID = 10424000, HpMin = 2905, HpMax = 6779, HitMin = 532, HitMax = 1864, DefMin = 75, DefMax = 263 });
            //Mana Nick Earth , 10433000
            ItemDataBase.Add(new CustomItem() { ItemID = 10433000, HpMin = 6503, HpMax = 12077, HitMin = 650, HitMax = 1811, DefMin = 144, DefMax = 402 });
            //Guardian Ring Wind , 10524000
            ItemDataBase.Add(new CustomItem() { ItemID = 10524000, SpdMin = 2.28f, SpdMax = 8.01f, AtkMin = 59, AtkMax = 257 });


            return ItemDataBase.Find(x => x.ItemID == id);
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
    }

    //|||||||||||||| PANDORA START CODE |||||||||||||||||||
    class CustomItem
    {
        public int ItemID { set; get; }
        public float AtkMin { set; get; }
        public float AtkMax { set; get; }
        public float SpdMin { set; get; }
        public float SpdMax { set; get; }
        public float HpMin { set; get; }
        public float HpMax { set; get; }
        public float DefMin { set; get; }
        public float DefMax { set; get; }
        public float HitMin { set; get; }
        public float HitMax { set; get; }

    }
    //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
}
