using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Module;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class HpBar : ProgressBar
    {
        [SerializeField] private BuffLayout buffLayout = null;

        [SerializeField] private TextMeshProUGUI levelText = null;

        [SerializeField] private Slider additionalSlider = null;

        public HpBarVFX HpVFX { get; private set; }

        public void SetBuffs(IReadOnlyDictionary<int, Buff> buffs)
        {
            buffLayout.SetBuff(buffs);

            if (buffLayout.IsBuffAdded(StatType.HP))
            {
                if (HpVFX)
                {
                    HpVFX.Stop();
                }

                var rectTransform = bar.rectTransform;
                HpVFX = VFXController.instance.CreateAndChaseRectTransform<HpBarVFX>(rectTransform);
                HpVFX.Play();
            }
            else if (!buffLayout.HasBuff(StatType.HP))
            {
                if (HpVFX)
                {
                    HpVFX.Stop();
                }
            }
        }

        public void SetLevel(int value)
        {
            levelText.text = value.ToString();
        }

        public void Set(int current, int additional, int max)
        {
            SetText($"{current} / {max}");
            SetValue((float)math.min(current, max - additional) / max);

            bool isHPBoosted = additional > 0;
            additionalSlider.gameObject.SetActive(isHPBoosted);
            if (isHPBoosted)
                additionalSlider.value = (float)current / max;
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        public void SetPandora(Nekoyume.Model.CharacterBase characterBase)//int current, int additional, int max, int ATK, int DEF, int HIT, string SPD)
        {
            SetText($"<size=80%>" +
                    $"<color=#FFFFFF>HP:</color><color=green>{characterBase.CurrentHP}</color>" +
                    $"<color=#FFFFFF>,ATK:</color><color=green>{characterBase.ATK}</color>" +
                    $"<color=#FFFFFF>,DEF:</color><color=green>{characterBase.DEF}</color>\n" +
                    $"<color=#FFFFFF>HIT:</color><color=green>{characterBase.HIT}</color>" +
                    $"<color=#FFFFFF>,SPD:</color><color=green>{ StatType.SPD.ValueToString(characterBase.SPD)}</color>" +
                    $"<color=#FFFFFF>,CRI:</color><color=green>{ StatType.CRI.ValueToString(characterBase.CRI)}%</color>");
            SetValue((float)math.min(characterBase.CurrentHP, characterBase.HP - characterBase.Stats.BuffStats.HP) / characterBase.HP);

            bool isHPBoosted = characterBase.Stats.BuffStats.HP > 0;
            additionalSlider.gameObject.SetActive(isHPBoosted);
            if (isHPBoosted)
                additionalSlider.value = (float)characterBase.CurrentHP / characterBase.HP;
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        protected override void OnDestroy()
        {
            if (HpVFX)
            {
                HpVFX.Stop();
            }

            base.OnDestroy();
        }
    }
}
