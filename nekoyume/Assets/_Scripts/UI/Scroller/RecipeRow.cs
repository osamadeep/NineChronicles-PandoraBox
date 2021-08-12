using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class RecipeRow : RectCell<
        RecipeRow.Model,
        RecipeScroll.ContextModel>
    {
        public class Model
        {
            public ItemSubType ItemSubType { get; set; }

            public StatType StatType { get; set; }

            public string Name { get; private set; }

            public int Grade { get; private set; }

            public List<SheetRow<int>> Rows { get; private set; }

            public Model(string name, int grade)
            {
                Name = name;
                Grade = grade;
                Rows = new List<SheetRow<int>>();
            }
        }

        [SerializeField]
        private TextMeshProUGUI nameText = null;

        [SerializeField]
        private List<Image> gradeImages = null;

        [SerializeField]
        private List<RecipeCell> recipeCells = null;

        [SerializeField]
        private Animator animator = null;

        [SerializeField]
        private CanvasGroup canvasGroup = null;

        private readonly int _triggerHash = Animator.StringToHash("Show");

        public override void UpdateContent(Model viewModel)
        {
            nameText.text = viewModel.Name;

            for (int i = 0; i < gradeImages.Count; ++i)
            {
                gradeImages[i].enabled = i < viewModel.Grade;
            }

            for (int i = 0; i < recipeCells.Count; ++i)
            {
                if (i >= viewModel.Rows.Count)
                {
                    recipeCells[i].Hide();
                }
                else
                {
                    var cell = recipeCells[i];
                    cell.Show(viewModel.Rows[i]);
                }
            }
        }

        public void HideWithAlpha()
        {
            canvasGroup.alpha = 0f;
        }

        public void ShowAnimation()
        {
            canvasGroup.alpha = 1f;
            animator.SetTrigger(_triggerHash);
        }
    }
}
