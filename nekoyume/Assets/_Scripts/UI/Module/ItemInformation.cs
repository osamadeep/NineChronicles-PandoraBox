using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ItemInformation : MonoBehaviour
    {
        [Serializable]
        public struct IconArea
        {
            public SimpleCountableItemView itemView;
            public List<Image> elementalTypeImages;
        }

        [Serializable]
        public struct DescriptionArea
        {
            public GameObject itemDescriptionGameObject;
            public TextMeshProUGUI itemDescriptionText;
            public GameObject dividerImageGameObject;
            public GameObject commonGameObject;
            public TextMeshProUGUI commonText;
            public GameObject levelLimitGameObject;
            public TextMeshProUGUI levelLimitText;
            public GameObject combatPowerObject;
            public TextMeshProUGUI combatPowerText;
        }

        [Serializable]
        public struct StatsArea
        {
            public RectTransform root;
            public List<BulletedStatView> stats;
        }

        [Serializable]
        public struct SkillsArea
        {
            public RectTransform root;
            public List<SkillView> skills;
        }

        public IconArea iconArea;
        public DescriptionArea descriptionArea;
        public StatsArea statsArea;
        public SkillsArea skillsArea;

        public Model.ItemInformation Model { get; private set; }

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void SetData(Model.ItemInformation data)
        {
            if (data == null)
            {
                Clear();

                return;
            }

            _disposables.DisposeAllAndClear();
            Model = data;

            UpdateView();
        }

        public void Clear()
        {
            _disposables.DisposeAllAndClear();
            Model?.Dispose();
            Model = null;

            UpdateView();
        }

        private void UpdateView()
        {
            UpdateViewIconArea();
            UpdateDescriptionArea();
            UpdateStatsArea();
            UpdateSkillsArea();
        }

        private void UpdateViewIconArea()
        {
            if (Model?.item.Value is null)
            {
                // 아이콘.
                iconArea.itemView.Clear();

                // 속성.
                foreach (var image in iconArea.elementalTypeImages)
                {
                    image.enabled = false;
                }

                return;
            }

            var item = Model.item.Value.ItemBase.Value;

            // 아이콘.
            iconArea.itemView.SetData(new CountableItem(
                Model.item.Value.ItemBase.Value,
                Model.item.Value.Count.Value));

            // 속성.
            var sprite = item.ElementalType.GetSprite();
            var elementalCount = item.Grade;
            for (var i = 0; i < iconArea.elementalTypeImages.Count; i++)
            {
                var image = iconArea.elementalTypeImages[i];
                if (sprite is null ||
                    i >= elementalCount)
                {
                    image.enabled = false;
                    continue;
                }

                image.enabled = true;
                image.overrideSprite = sprite;
                image.SetNativeSize();
            }
        }

        private void UpdateDescriptionArea()
        {
            if (Model?.item.Value is null)
            {
                descriptionArea.itemDescriptionGameObject.SetActive(false);

                return;
            }

            descriptionArea.itemDescriptionGameObject.SetActive(true);
            descriptionArea.itemDescriptionText.text = Model.item.Value.ItemBase.Value.GetLocalizedDescription();
        }

        private void UpdateStatsArea()
        {
            if (Model?.item.Value is null)
            {
                statsArea.root.gameObject.SetActive(false);

                return;
            }

            foreach (var stat in statsArea.stats)
            {
                stat.Hide();
            }

            var statCount = 0;
            if (Model.item.Value.ItemBase.Value is Equipment equipment)
            {
                descriptionArea.commonGameObject.SetActive(false);
                descriptionArea.dividerImageGameObject.SetActive(false);
                descriptionArea.levelLimitGameObject.SetActive(false);
                descriptionArea.combatPowerObject.SetActive(true);
                descriptionArea.combatPowerText.text = CPHelper.GetCP(equipment).ToString();

                var uniqueStatType = equipment.UniqueStatType;
                foreach (var statMapEx in equipment.StatsMap.GetStats())
                {
                    if (!statMapEx.StatType.Equals(uniqueStatType))
                        continue;

                    AddStat(statMapEx, equipment, true);//|||| PANDORA CODE |||||
                    statCount++;
                }

                foreach (var statMapEx in equipment.StatsMap.GetStats())
                {
                    if (statMapEx.StatType.Equals(uniqueStatType))
                        continue;

                    AddStat(statMapEx, equipment);//|||| PANDORA CODE |||||
                    statCount++;
                }
            }
            else if (Model.item.Value.ItemBase.Value is ItemUsable itemUsable)
            {
                descriptionArea.commonGameObject.SetActive(false);
                descriptionArea.dividerImageGameObject.SetActive(false);
                descriptionArea.levelLimitGameObject.SetActive(false);
                descriptionArea.combatPowerObject.SetActive(false);

                foreach (var statMapEx in itemUsable.StatsMap.GetStats())
                {
                    AddStat(statMapEx);
                    statCount++;
                }
            }
            else if (Model.item.Value.ItemBase.Value is Costume costume)
            {
                var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
                descriptionArea.commonGameObject.SetActive(false);
                descriptionArea.dividerImageGameObject.SetActive(false);
                descriptionArea.levelLimitGameObject.SetActive(false);
                var statsMap = new StatsMap();
                foreach (var row in costumeSheet.OrderedList.Where(r => r.CostumeId == costume.Id))
                {
                    statsMap.AddStatValue(row.StatType, row.Stat);
                }

                foreach (var statMapEx in statsMap.GetStats())
                {
                    AddStat(statMapEx);
                    statCount++;
                }

                var cpEnable = statCount > 0;
                descriptionArea.combatPowerObject.SetActive(cpEnable);
                if (cpEnable)
                {
                    descriptionArea.combatPowerText.text = CPHelper.GetCP(costume, costumeSheet).ToString();
                }
            }
            else
            {
                descriptionArea.levelLimitGameObject.SetActive(false);
                descriptionArea.combatPowerObject.SetActive(false);
                descriptionArea.commonGameObject.SetActive(false);
                descriptionArea.dividerImageGameObject.SetActive(false);
            }

            if (statCount <= 0)
            {
                statsArea.root.gameObject.SetActive(false);

                return;
            }

            statsArea.root.gameObject.SetActive(true);
        }

        private void UpdateSkillsArea()
        {
            if (Model?.item.Value is null)
            {
                skillsArea.root.gameObject.SetActive(false);

                return;
            }

            foreach (var skill in skillsArea.skills)
            {
                skill.Hide();
            }

            var skillCount = 0;
            if (Model.item.Value.ItemBase.Value is ItemUsable itemUsable)
            {
                foreach (var skill in itemUsable.Skills)
                {
                    AddSkill(new Model.SkillView(skill));
                    skillCount++;
                }

                foreach (var skill in itemUsable.BuffSkills)
                {
                    AddSkill(new Model.SkillView(skill));
                    skillCount++;
                }
            }

            if (skillCount <= 0)
            {
                skillsArea.root.gameObject.SetActive(false);

                return;
            }

            skillsArea.root.gameObject.SetActive(true);
        }

        private void AddStat(StatMapEx model, bool isMainStat = false)
        {
            var statView = GetDisabledStatView();
            if (statView is null)
                throw new NotFoundComponentException<BulletedStatView>();
            statView.Show(model, isMainStat);
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        private void AddStat(StatMapEx model, Equipment equipment, bool isMainStat = false)
        {
            var statView = GetDisabledStatView();
            if (statView is null)
                throw new NotFoundComponentException<BulletedStatView>();
            statView.Show(model, isMainStat, equipment);
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        private BulletedStatView GetDisabledStatView()
        {
            foreach (var stat in statsArea.stats)
            {
                if (stat.IsShow)
                {
                    continue;
                }

                return stat;
            }

            return null;
        }


        private void AddSkill(Model.SkillView model)
        {
            foreach (var skill in skillsArea.skills.Where(skill => !skill.IsShown))
            {
                skill.SetData(model);
                skill.Show();

                return;
            }
        }
    }
}
