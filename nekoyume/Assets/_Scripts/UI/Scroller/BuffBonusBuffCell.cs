using Nekoyume.PandoraBox;
using Nekoyume.TableData.Crystal;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class BuffBonusBuffCell : MonoBehaviour
    {
        [SerializeField] private BonusBuffViewDataScriptableObject bonusBuffViewData;

        [SerializeField] private Image gradeIconImage;

        [SerializeField] private Image buffIconImage;

        [SerializeField] private TextMeshProUGUI buffNameText;

        public void Set(CrystalRandomBuffSheet.Row itemData)
        {
            var skillSheet = Game.Game.instance.TableSheets.SkillSheet;
            if (!skillSheet.TryGetValue(itemData.SkillId, out var skillRow))
            {
                return;
            }

            var gradeData = bonusBuffViewData.GetBonusBuffGradeData(itemData.Rank);
            gradeIconImage.sprite = gradeData.BgSprite;
            buffIconImage.sprite = bonusBuffViewData.GetBonusBuffIcon(skillRow.SkillCategory);
            buffNameText.text = skillRow.GetLocalizedName();
            buffNameText.color = itemData.Rank.GetBuffGradeColor();

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            itemDataSimulation = itemData;
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }


        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        CrystalRandomBuffSheet.Row itemDataSimulation;

        public void SetSimulateBuff()
        {
            if (!Premium.PANDORA_CheckPremium())
                return;
            PandoraUtil.ShowSystemNotification("Custom Crystal Buff <color=green>Selected</color>!",
                NotificationCell.NotificationType.Information);
            PlayerPrefs.SetInt("_PandoraBox_PVE_SelectedCrystalBuff", itemDataSimulation.Id);
            PlayerPrefs.SetInt("_PandoraBox_PVE_SelectedCrystalBuffSkillId", itemDataSimulation.SkillId);
            PlayerPrefs.SetInt("_PandoraBox_PVE_SelectedCrystalBuffRank", (int)itemDataSimulation.Rank);
            Widget.Find<BattlePreparation>().UpdateSimulateBuff();
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
    }
}