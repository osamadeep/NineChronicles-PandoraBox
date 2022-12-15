using Nekoyume.Game.Controller;
using Nekoyume.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System;
using Lib9c.Model.Order;
using Nekoyume.State;
using System.Threading.Tasks;
using Nekoyume.Helper;

namespace Nekoyume.PandoraBox
{
    public class GuildInfo : PopupWidget
    {
        Guild selectedGuild;

        [SerializeField] private ScrollRect MembersScroll;

        [SerializeField] private GameObject loadingIMG;

        [SerializeField] private Image LogoImage;

        [SerializeField] private TextMeshProUGUI NameTxt;

        [SerializeField] private TextMeshProUGUI DescTxt;

        [SerializeField] private TextMeshProUGUI CountTxt;

        [SerializeField] private TextMeshProUGUI LevelTxt;

        [SerializeField] private TextMeshProUGUI LanguageTxt;

        [SerializeField] private TextMeshProUGUI BoostTxt;

        [SerializeField] private TextMeshProUGUI MemberTxt;

        [SerializeField] private TextMeshProUGUI TotalCPTxt;

        [SerializeField] private GameObject LoadingIMGMembers;

        [SerializeField] private UnityEngine.UI.Button JoinButton;

        [SerializeField] private Blur blur = null;

        [SerializeField] private Button closeButton;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() => { Close(true); });

            CloseWidget = () => Close(true);

            CloseWidget = () => { Close(); };
        }

        public void Show(string clanShort)
        {
            base.Show(false);
            //blur.Show(2);
            AudioController.PlayPopup();
            //SetGuildInfo(new Guild());

            selectedGuild = PandoraMaster.PanDatabase.Guilds.Find(x => x.Tag == clanShort);
            SetGuildInfo();
        }

        void SetGuildInfo()
        {
            //Global info
            NameTxt.text = $"[<color=#2E8A18>{selectedGuild.Tag}</color>] {selectedGuild.Name}";
            TotalCPTxt.text = "...";
            MemberTxt.text = "";
            DescTxt.text = selectedGuild.Desc;
            LevelTxt.text = selectedGuild.MinLevel;
            LanguageTxt.text = selectedGuild.Language;
            if (PandoraMaster.CurrentGuildPlayer is null)
            {
                if (selectedGuild.Type == 1)
                {
                    JoinButton.interactable = false;
                    JoinButton.GetComponentInChildren<TextMeshProUGUI>().text = "CLOSED";
                    JoinButton.GetComponent<Image>().color = Color.red;
                }
                else
                {
                    JoinButton.interactable = true;
                    JoinButton.GetComponentInChildren<TextMeshProUGUI>().text = "CONTACT";
                    JoinButton.GetComponent<Image>().color = Color.green;
                }
            }
            else
            {
                if (selectedGuild.Tag == PandoraMaster.CurrentGuildPlayer.Guild)
                {
                    JoinButton.interactable = false;
                    JoinButton.GetComponentInChildren<TextMeshProUGUI>().text = "JOINED";
                    JoinButton.GetComponent<Image>().color = Color.white;
                }
                else
                {
                    if (selectedGuild.Type == 1)
                    {
                        JoinButton.interactable = false;
                        JoinButton.GetComponentInChildren<TextMeshProUGUI>().text = "CLOSED";
                    }
                    else
                    {
                        JoinButton.interactable = true;
                        JoinButton.GetComponentInChildren<TextMeshProUGUI>().text = "CONTACT";
                    }
                }
            }

            StartCoroutine(GetGuildImage(selectedGuild)); //guild image

            //Members related
            List<GuildPlayer> selectedGuildPlayers =
                PandoraMaster.PanDatabase.GuildPlayers.FindAll(x => x.Guild == selectedGuild.Tag);
            string maxCount = selectedGuildPlayers.Count > 20
                ? $"<color=green>{selectedGuildPlayers.Count}</color>"
                : "20";
            CountTxt.text = $"Members {selectedGuildPlayers.Count}/{maxCount}";

            //boosters
            List<PandoraPlayer> selectedGuildPlayersPandoraProfile = new List<PandoraPlayer>();
            foreach (GuildPlayer selectedGuildPlayer in selectedGuildPlayers)
            {
                PandoraPlayer tmp = Premium.Pandoraplayers.Find(y =>
                    y.Address.ToLower() == selectedGuildPlayer.Address.ToLower());
                if (tmp is null)
                    continue;
                else
                    selectedGuildPlayersPandoraProfile.Add(tmp);
            }

            BoostTxt.text =
                $"<color=green>+{selectedGuildPlayersPandoraProfile.FindAll(x => x.IsPremium()).Count}</color>";

            //members details
            GG(selectedGuildPlayers);
        }

        IEnumerator GetGuildImage(Guild myGuild)
        {
            loadingIMG.SetActive(true);
            LogoImage.GetComponent<Image>().enabled = false;

            Uri imageLink = new Uri(PandoraDB.DBPath + "Guilds/" + myGuild.Tag + ".png");
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(imageLink))
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
                    LogoImage.sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 150);
                    LogoImage.GetComponent<Image>().enabled = true;
                    loadingIMG.SetActive(false);
                }
            }
        }

        async void GG(List<GuildPlayer> selectedGuildPlayers)
        {
            LoadingIMGMembers.SetActive(true);
            MemberTxt.text = await GetMemberListState(selectedGuildPlayers);
            MembersScroll.content.GetComponent<RectTransform>().sizeDelta =
                new Vector2(MembersScroll.content.GetComponent<RectTransform>().sizeDelta.x,
                    MemberTxt.preferredHeight + 50);
            LoadingIMGMembers.SetActive(false);
        }

        async Task<string> GetMemberListState(List<GuildPlayer> selectedGuildPlayers)
        {
            string membersText = "";
            List<playerRecord> gList = new List<playerRecord>();

            int totalCP = 0;
            int totalLevel = 0;
            foreach (GuildPlayer member in selectedGuildPlayers)
            {
                if (string.IsNullOrEmpty(member.AvatarAddress))
                {
                    gList.Add(new playerRecord(member.Rank, 0, 0,
                        "(<color=red>!</color>)" + member.Address.Substring(0, 6).ToLower()));
                    continue;
                }

                var (exist, avatarState) =
                    await States.TryGetAvatarStateAsync(
                        new Libplanet.Address(member.AvatarAddress.Substring(2).ToLower()));
                if (!exist)
                    gList.Add(new playerRecord(member.Rank, 0, 0,
                        "(<color=red>!</color>)" + member.Address.Substring(0, 6).ToLower()));
                else
                {
                    gList.Add(new playerRecord(member.Rank, avatarState.level, 100, avatarState.name)); //avatarState.GetCP()
                    totalCP += 100;
                    totalLevel += avatarState.level;
                }
            }

            //((sum level members * 0.8)/num members) + (0.5*sum of CP)
            TotalCPTxt.text = ((int)((totalLevel * 0.8f) / gList.Count + (0.5f * totalCP))).ToString();

            //fill leader
            playerRecord leader = gList.Find(x => x.Rank == 100);
            membersText += "-= <color=green> LEADER </color> =-";
            membersText += $"\n<color=green>{leader.Level}</color> " + leader.Name + "\n";
            gList.Remove(leader);

            //Officers
            List<playerRecord> Officers = gList.FindAll(x => x.Rank == 50);
            if (Officers is null || Officers.Count == 0)
            {
            }
            else
            {
                membersText += "\n-= <color=green> Officers </color> =-";
                Officers.Sort((a, b) => b.Level.CompareTo(a.Level));
                foreach (playerRecord member in Officers)
                {
                    membersText += $"\n<color=green>{member.Level}</color> " + member.Name;
                }

                gList.RemoveAll(x => x.Rank == 50);
            }

            membersText += "\n";
            membersText += "\n- <color=green> Members </color> -";
            gList.Sort((a, b) => b.Level.CompareTo(a.Level));
            foreach (playerRecord member in gList)
            {
                membersText += $"\n<color=green>{member.Level}</color> " + member.Name;
            }

            return membersText;
        }

        public void ContactGuild()
        {
            if (selectedGuild is null || selectedGuild.Type == 1)
                return;
            Application.OpenURL(selectedGuild.Link);
        }

        class playerRecord
        {
            public int Level { set; get; }
            public string Name { set; get; }
            public int Rank { set; get; }
            public int CP { set; get; }

            public playerRecord(int rank, int lv, int cp, string name)
            {
                Level = lv;
                Name = name;
                Rank = rank;
                CP = cp;
            }
        }
    }
}
