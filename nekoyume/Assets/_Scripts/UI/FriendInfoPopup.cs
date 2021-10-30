using System.Linq;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using PandoraBox;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class FriendInfoPopup : PopupWidget
    {
        public override WidgetType WidgetType => WidgetType.Tooltip;

        private const string NicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";

        private static readonly Vector3 NPCPosition = new Vector3(2000f, 1999.2f, 2.15f);

        [SerializeField]
        private Button blurButton = null;

        [SerializeField]
        private RectTransform modal = null;

        [SerializeField]
        private TextMeshProUGUI nicknameText = null;

        [SerializeField]
        private Transform titleSocket = null;

        [SerializeField]
        private TextMeshProUGUI cpText = null;

        [SerializeField]
        private EquipmentSlots costumeSlots = null;

        [SerializeField]
        private EquipmentSlots equipmentSlots = null;

        [SerializeField]
        private AvatarStats avatarStats = null;

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [SerializeField]
        private GameObject paidMember = null;

        [SerializeField]
        private Button copyButton = null;

        [SerializeField]
        private TextMeshProUGUI blockText = null;

        [SerializeField]
        private TextMeshProUGUI dateText = null;

        [SerializeField]
        private TextMeshProUGUI versionText = null;

        [SerializeField]
        private Button NemesisButton = null;

        [SerializeField]
        private Button ResetNemesisButton = null;
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        private CharacterStats _tempStats;
        private GameObject _cachedCharacterTitle;
        private Player _player;

        #region Override

        protected override void Awake()
        {
            base.Awake();

            costumeSlots.gameObject.SetActive(false);
            equipmentSlots.gameObject.SetActive(true);

            blurButton.OnClickAsObservable()
                .Subscribe(_ => Close())
                .AddTo(gameObject);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            copyButton.OnClickAsObservable().Subscribe(_ => CopyPlayerInfo()).AddTo(gameObject);
            NemesisButton.OnClickAsObservable().Subscribe(_ => SetNemesis()).AddTo(gameObject);
            ResetNemesisButton.OnClickAsObservable().Subscribe(_ => ResetAllNemesis()).AddTo(gameObject);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            Show(currentAvatarState, ignoreShowAnimation);
        }

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            TerminatePlayer();
        }
        #endregion

        public void Show(AvatarState avatarState, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            InitializePlayer(avatarState);
            UpdateSlotView(avatarState);
            UpdateStatViews();
        }

        private void InitializePlayer(AvatarState avatarState)
        {
            _player = PlayerFactory.Create(avatarState).GetComponent<Player>();
            var t = _player.transform;
            t.localScale = Vector3.one;
            t.position = NPCPosition;
        }

        private void TerminatePlayer()
        {
            var t = _player.transform;
            t.SetParent(Game.Game.instance.Stage.transform);
            t.localScale = Vector3.one;
            _player.gameObject.SetActive(false);
            _player = null;
        }

        private void UpdateSlotView(AvatarState avatarState)
        {
            tempAvatarState = avatarState;

            var game = Game.Game.instance;
            var playerModel = _player.Model;

            nicknameText.text = string.Format(
                NicknameTextFormat,
                avatarState.level,
                avatarState.NameWithHash);

            var title = avatarState.inventory.Costumes.FirstOrDefault(costume =>
                costume.ItemSubType == ItemSubType.Title &&
                costume.equipped);

            if (!(title is null))
            {
                Destroy(_cachedCharacterTitle);
                var clone = ResourcesHelper.GetCharacterTitle(title.Grade, title.GetLocalizedNonColoredName(false));
                _cachedCharacterTitle = Instantiate(clone, titleSocket);
            }

            cpText.text = CPHelper
                .GetCPV2(avatarState, game.TableSheets.CharacterSheet, game.TableSheets.CostumeStatSheet)
                .ToString();

            costumeSlots.SetPlayerCostumes(playerModel, ShowTooltip, null);
            equipmentSlots.SetPlayerEquipments(playerModel, ShowTooltip, null);


            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            TextMeshProUGUI text = NemesisButton.GetComponentInChildren<TextMeshProUGUI>();
            text.text = PandoraBoxMaster.ArenaFavTargets.Contains(tempAvatarState.address.ToString()) ? "Remove Nemesis" : "Set Nemesis";

            blockText.text = "Block #" + Game.Game.instance.Agent.BlockIndex.ToString();
            dateText.text = System.DateTime.Now.ToUniversalTime().ToString() + " (UTC)";
            versionText.text = "APV: " + PandoraBoxMaster.OriginalVersionId;
            if (nicknameText.text.Contains("Lambo") || nicknameText.text.Contains("AndrewLW") || nicknameText.text.Contains("bmcdee") || nicknameText.text.Contains("Wabbs"))
                paidMember.SetActive(true);
            else
                paidMember.SetActive(false);

            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        public void ResetAllNemesis()
        {
            PandoraBoxMaster.ArenaFavTargets.Clear();
            for (int i = 0; i < 3; i++)
            {
                string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                PlayerPrefs.DeleteKey(key);
            }
            OneLinePopup.Push(MailType.System, "<color=green>Pandora Box</color>: <color=red>Nemesis</color> list is clear Successfully!");
        }


        AvatarState tempAvatarState;
        void CopyPlayerInfo()
        {
            string playerInfo =
                "Avatar Name   : " + tempAvatarState.NameWithHash + "\n" +
                "Player Address: " + tempAvatarState.address + "\n" +
                "Date & Time   : " + System.DateTime.Now.ToUniversalTime().ToString() + " (UTC)" + "\n" +
                "Block         : #" + Game.Game.instance.Agent.BlockIndex.ToString();
            ClipboardHelper.CopyToClipboard(playerInfo);
            OneLinePopup.Push(MailType.System, "<color=green>Pandora Box</color>: Player (<color=green>" + tempAvatarState.NameWithHash + "</color>) Info copy to Clipboard Successfully!");
        }

        
        public void SetNemesis()
        {
            TextMeshProUGUI text = NemesisButton.GetComponentInChildren<TextMeshProUGUI>();
            if (PandoraBoxMaster.ArenaFavTargets.Contains(tempAvatarState.address.ToString()))
            {
                for (int i = 0; i < PandoraBoxMaster.ArenaFavTargets.Count; i++)
                {
                    string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                    PlayerPrefs.DeleteKey(key);
                    //PlayerPrefs.SetString(key, PandoraBoxMaster.ArenaFavTargets[i]);
                }
                PandoraBoxMaster.ArenaFavTargets.Remove(tempAvatarState.address.ToString());
                for (int i = 0; i < PandoraBoxMaster.ArenaFavTargets.Count; i++)
                {
                    string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                    PlayerPrefs.SetString(key, PandoraBoxMaster.ArenaFavTargets[i]);
                }

                OneLinePopup.Push(MailType.System, "<color=green>Pandora Box</color>: " + tempAvatarState.NameWithHash + " removed from your nemesis list!");
            }
            else
            {
                if (PandoraBoxMaster.ArenaFavTargets.Count > 2)
                    OneLinePopup.Push(MailType.System, "<color=green>Pandora Box</color>: You reach <color=red>Maximum</color> number of nemesis, please remove some!");
                else
                {
                    PandoraBoxMaster.ArenaFavTargets.Add(tempAvatarState.address.ToString());
                    OneLinePopup.Push(MailType.System, "<color=green>Pandora Box</color>: " + tempAvatarState.NameWithHash + " added to your nemesis list!");
                    for (int i = 0; i < PandoraBoxMaster.ArenaFavTargets.Count; i++)
                    {
                        string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                        PlayerPrefs.SetString(key, PandoraBoxMaster.ArenaFavTargets[i]);
                    }
                }
            }
            text.text = PandoraBoxMaster.ArenaFavTargets.Contains(tempAvatarState.address.ToString()) ? "Remove Nemesis" : "Set Nemesis";
        }

        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        private void UpdateStatViews()
        {
            _tempStats = _player.Model.Stats.Clone() as CharacterStats;
            var equipments = equipmentSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => slot.Item as Equipment)
                .Where(item => !(item is null))
                .ToList();

            var stats = _tempStats.SetAll(
                _tempStats.Level,
                equipments,
                null,
                Game.Game.instance.TableSheets.EquipmentItemSetEffectSheet
            );


            //Debug.LogError("UPDATE: " +  tempAvatarState.agentAddress.ToString());
            avatarStats.SetData(stats);

            var sprite = Resources.Load<Sprite>("Character/PlayerSpineTexture/Weapon/10151001");
            if (PandoraBoxMaster.Instance.IsRBG(tempAvatarState.agentAddress.ToString()))
                _player.SpineController.UpdateWeapon(10151001, sprite, PandoraBoxMaster.Instance.CosmicSword);
        }

        private static void ShowTooltip(EquipmentSlot slot)
        {
            var tooltip = Find<ItemInformationTooltip>();
            if (slot is null ||
                slot.RectTransform == tooltip.Target)
            {
                tooltip.Close();

                return;
            }

            tooltip.Show(slot.RectTransform, new InventoryItem(slot.Item, 1));
        }
    }
}
