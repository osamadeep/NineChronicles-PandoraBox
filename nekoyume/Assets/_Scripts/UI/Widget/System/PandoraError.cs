using System.Collections;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class PandoraError : TitleOneButtonSystem
    {
        [SerializeField] private TextMeshProUGUI titleTxt;
        [SerializeField] private TextMeshProUGUI messageTxt;

        protected override void Awake()
        {
            base.Awake();
        }

        public void Show(string title, string msg)
        {
            titleTxt.text = title;
            messageTxt.text = msg;
            base.Show();
        }

        public void OpenDiscord()
        {
            //Application.OpenURL("https://discord.gg/rnCrYnGvdb");
            Close();
        }
    }
}