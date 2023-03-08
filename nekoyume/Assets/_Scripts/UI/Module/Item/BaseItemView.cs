using System;
using System.Linq;
using Coffee.UIEffects;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.PandoraBox;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Nekoyume
{
    public class BaseItemView : MonoBehaviour
    {
        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")] public GameObject FeatureObj = null;
        public GameObject FavoriteObj = null;
        public GameObject GuildObj = null;
        public GameObject myItem = null;
        public TextMeshProUGUI ChangeText = null;

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        [SerializeField]
        private GameObject container;

        [SerializeField] private GameObject emptyObject;

        [SerializeField] private ItemViewDataScriptableObject itemViewData;

        [SerializeField] private TouchHandler touchHandler;

        [SerializeField] private TouchHandler minusTouchHandler;

        [SerializeField] private Image gradeImage;

        [SerializeField] private UIHsvModifier gradeHsv;

        [SerializeField] private GameObject enoughObject;

        [SerializeField] private Image itemImage;

        [SerializeField] private Image spineItemImage;

        [SerializeField] private Image enhancementImage;

        [SerializeField] private TextMeshProUGUI enhancementText;

        [SerializeField] private TextMeshProUGUI countText;

        [SerializeField] private TextMeshProUGUI priceText;

        [SerializeField] private ItemOptionTag optionTag;

        [SerializeField] private GameObject notificationObject;

        [SerializeField] private GameObject equippedObject;

        [SerializeField] private GameObject minusObject;

        [SerializeField] private GameObject focusObject;

        [SerializeField] private GameObject expiredObject;

        [SerializeField] private GameObject tradableObject;

        [SerializeField] private GameObject dimObject;

        [SerializeField] private GameObject levelLimitObject;

        [SerializeField] private GameObject selectObject;

        [SerializeField] private GameObject selectBaseItemObject;

        [SerializeField] private GameObject selectMaterialItemObject;

        [SerializeField] private GameObject lockObject;

        [SerializeField] private GameObject shadowObject;

        [SerializeField] private GameObject loadingObject;

        [SerializeField] private ParticleSystem itemGradeParticle;

        [SerializeField] private GameObject grindingCountObject;

        [SerializeField] private TMP_Text grindingCountText;

        public GameObject Container => container;
        public GameObject EmptyObject => emptyObject;
        public TouchHandler TouchHandler => touchHandler;
        public TouchHandler MinusTouchHandler => minusTouchHandler;
        public Image GradeImage => gradeImage;
        public UIHsvModifier GradeHsv => gradeHsv;
        public GameObject EnoughObject => enoughObject;
        public Image ItemImage => itemImage;
        public Image SpineItemImage => spineItemImage;
        public Image EnhancementImage => enhancementImage;
        public TextMeshProUGUI EnhancementText => enhancementText;
        public TextMeshProUGUI CountText => countText;
        public TextMeshProUGUI PriceText => priceText;
        public ItemOptionTag OptionTag => optionTag;
        public GameObject NotificationObject => notificationObject;
        public GameObject EquippedObject => equippedObject;
        public GameObject MinusObject => minusObject;
        public GameObject FocusObject => focusObject;
        public GameObject ExpiredObject => expiredObject;
        public GameObject TradableObject => tradableObject;
        public GameObject DimObject => dimObject;
        public GameObject LevelLimitObject => levelLimitObject;
        public GameObject SelectObject => selectObject;
        public GameObject SelectBaseItemObject => selectBaseItemObject;
        public GameObject SelectMaterialItemObject => selectMaterialItemObject;
        public GameObject LockObject => lockObject;
        public GameObject ShadowObject => shadowObject;
        public GameObject LoadingObject => loadingObject;
        public ParticleSystem ItemGradeParticle => itemGradeParticle;
        public GameObject GrindingCountObject => grindingCountObject;
        public TMP_Text GrindingCountText => grindingCountText;

        public static Sprite GetItemIcon(ItemBase itemBase)
        {
            var icon = itemBase.GetIconSprite();
            if (icon is null)
            {
                throw new FailedToLoadResourceException<Sprite>(itemBase.Id.ToString());
            }

            return icon;
        }

        public ItemViewData GetItemViewData(ItemBase itemBase)
        {
            var add = itemBase is TradableMaterial ? 1 : 0;
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            FeatureObj.SetActive(false);
            FavoriteObj.SetActive(false);
            GuildObj.SetActive(false);
            myItem.SetActive(false);
            ChangeText.gameObject.SetActive(itemBase.ItemType == ItemType.Equipment);

            if (itemBase is INonFungibleItem nonFungibleItem)
            {
                var nonFungibleId = nonFungibleItem.NonFungibleId;
                //Debug.LogError(nonFungibleId);
                FeatureObj.SetActive(false);
                FeatureObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
                FeatureItem currentFeatureItem =
                    PandoraMaster.PanDatabase.FeatureItems.Find(x => x.IsEqual(nonFungibleId.ToString()));

                //change text
                try
                {
                    if (itemBase is Equipment equipment)
                    {
                        var slotIndex = States.Instance.AvatarStates
                            .FirstOrDefault(x => x.Value.address == States.Instance.CurrentAvatarState.address).Key;
                        var itemSlotStates = States.Instance.ItemSlotStates[slotIndex];
                        var equippedItems =
                            States.Instance.CurrentAvatarState.inventory.Equipments.Where(x => x.equipped);
                        var matchedItem = equippedItems.First(x => x.ItemSubType == itemBase.ItemSubType);
                        var ratio = ((Battle.CPHelper.GetCP(equipment) * 1f) /
                            (Battle.CPHelper.GetCP(matchedItem) * 1f) * 100f) - 100;
                        ChangeText.text = (int)Mathf.Abs(ratio) + "%";
                        ChangeText.color = ratio > 0 ? Color.green : Color.red;
                        ChangeText.transform.GetChild(0).gameObject.SetActive((int)ratio > 0);
                        ChangeText.transform.GetChild(1).gameObject.SetActive((int)ratio < 0);
                        ChangeText.gameObject.SetActive(matchedItem != equipment);
                    }
                }
                catch
                {
                    ChangeText.gameObject.SetActive(false);
                }


                if (!(currentFeatureItem is null) && currentFeatureItem.IsValid())
                {
                    FeatureObj.SetActive(true);
                    FeatureObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text =
                        $"PROMO>{currentFeatureItem.EndBlock}";
                }

                //if (nonFungibleId.ToString() == "8208c642-6848-4fba-81b3-494f36178e19" || nonFungibleId.ToString() == "4e8f40e9-00a2-4e0e-89fe-1d686da22702")
                FavoriteObj.SetActive(PandoraMaster.FavItems.Contains(nonFungibleId.ToString()));
            }

            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            return itemViewData.GetItemViewData(itemBase.Grade + add);
        }

        public ItemViewData GetItemViewData(int grade)
        {
            return itemViewData.GetItemViewData(grade);
        }
    }
}