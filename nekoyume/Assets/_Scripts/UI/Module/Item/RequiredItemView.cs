﻿using System.Globalization;
using Nekoyume.UI.Model;
using TMPro;

namespace Nekoyume.UI.Module
{
    public class RequiredItemView : SimpleCountableItemView
    {
        public TextMeshProUGUI requiredText;

        protected const string CountTextFormatEnough = "{0}/{1}";
        protected const string CountTextFormatNotEnough = "<color=red>{0}</color>/{1}";

        public int RequiredCount { get; set; } = 1;

        public void SetData(CountableItem model, int requiredCount)
        {
            RequiredCount = requiredCount;
            base.SetData(model);
        }

        protected override void SetCount(int count)
        {
            bool isEnough = count >= RequiredCount;

            countText.text = string.Format(isEnough ?
                CountTextFormatEnough :
                CountTextFormatNotEnough,
                Model.Count.Value, RequiredCount);

            countText.gameObject.SetActive(true);
            requiredText.gameObject.SetActive(false);
        }

        public void SetRequiredText()
        {
            requiredText.text = RequiredCount.ToString(CultureInfo.InvariantCulture);
            requiredText.gameObject.SetActive(true);
            countText.gameObject.SetActive(false);
        }
    }
}
