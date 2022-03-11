using Nekoyume.Game.Controller;
using Nekoyume.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

namespace Nekoyume.PandoraBox
{
    public class GuildInfo : Widget
    {
        [SerializeField]
        private GameObject loadingIMG;

        [SerializeField]
        private Image LogoImage;

        [SerializeField]
        private TextMeshProUGUI NameTxt;

        [SerializeField]
        private TextMeshProUGUI DescTxt;

        [SerializeField]
        private TextMeshProUGUI CountTxt;

        [SerializeField]
        private TextMeshProUGUI OwnerTxt;

        [SerializeField]
        private TextMeshProUGUI CpTxt;

        [SerializeField]
        private TextMeshProUGUI TypeTxt;

        [SerializeField]
        private Blur blur = null;

        [SerializeField]
        private Button closeButton;

        enum GuildType { Open,Closed,Private}

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() => { Close(true); });

            CloseWidget = () => Close(true);

            CloseWidget = () =>
            {
                Close();
            };
        }

        public void Show(string clanShort)
        {
            SetGuildInfo(new Guild());
            base.Show(false);
            blur.Show(2);
            AudioController.PlayPopup();

            Guild myGuild = PandoraBoxMaster.PanDatabase.Guilds.Find(x => x.Short == clanShort);
            SetGuildInfo(myGuild);
        }

        void SetGuildInfo(Guild myGuild)
        {
            loadingIMG.SetActive(true);
            NameTxt.text = myGuild.Name;
            LogoImage.sprite = null;
            if (myGuild.Logo != "")
                StartCoroutine(GetGuildImage(myGuild));
            DescTxt.text = myGuild.Desc;
            CountTxt.text = "0";
            OwnerTxt.text = myGuild.Owner;
            CpTxt.text = myGuild.MinCP;
            TypeTxt.text = ((GuildType)myGuild.Type).ToString();
        }

        IEnumerator GetGuildImage(Guild myGuild)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(myGuild.Logo))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(uwr.error);
                }
                else
                {
                    // Get downloaded asset bundle
                    var texture = DownloadHandlerTexture.GetContent(uwr);
                    LogoImage.sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 150);
                    loadingIMG.SetActive(false);
                }
            }
        }
    }
}

