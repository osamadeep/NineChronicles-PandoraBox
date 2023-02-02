using System.Linq;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.PandoraBox;
using Nekoyume.State;
using UnityEngine;
using UnityEngine.UI;
using Player = Nekoyume.Game.Character.Player;

namespace Nekoyume.UI.Module
{
    public class VanillaCharacterView : MonoBehaviour
    {
        [SerializeField]
        private Image iconImage = null;

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        public string AvatarAddress;
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void SetByAvatarState(AvatarState avatarState)
        {
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            AvatarAddress = avatarState.address.ToString().ToLower();
            NFTOwner currentNFTOwner = new NFTOwner();
            currentNFTOwner = PandoraMaster.PanDatabase.NFTOwners.Find(x => x.AvatarAddress.ToLower() == AvatarAddress);
            if (!(currentNFTOwner is null) && currentNFTOwner.OwnedItems.Count > 0)
            {
                if (!string.IsNullOrEmpty(currentNFTOwner.CurrentPortrait))
                {
                    NFTItem portrait = PandoraMaster.PanDatabase.NFTItems.Find(x => x.ItemID == currentNFTOwner.CurrentPortrait);
                    var image = Resources.Load<Sprite>(portrait.PrefabLocation);
                    SetIcon(image);
                    return;
                }
            }
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            var id = avatarState.GetArmorIdForPortrait();
            SetByFullCostumeOrArmorId(id);
        }

        public virtual void SetByPlayer(Player player)
        {
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            AvatarAddress = player.avatarAddress.ToLower();
            NFTOwner currentNFTOwner = new NFTOwner();
            currentNFTOwner = PandoraMaster.PanDatabase.NFTOwners.Find(x => x.AvatarAddress.ToLower() == AvatarAddress);
            if (!(currentNFTOwner is null) && currentNFTOwner.OwnedItems.Count > 0)
            {
                if (!string.IsNullOrEmpty(currentNFTOwner.CurrentPortrait))
                {
                    NFTItem portrait = PandoraMaster.PanDatabase.NFTItems.Find(x => x.ItemID == currentNFTOwner.CurrentPortrait);
                    var image = Resources.Load<Sprite>(portrait.PrefabLocation);
                    SetIcon(image);
                    return;
                }
            }
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            var id = Util.GetPortraitId(BattleType.Adventure);
            SetByFullCostumeOrArmorId(id);
            SetByCharacterId(player.Model.RowData.Id);
        }

        public void SetByCharacterId(int characterId)
        {
            var image = SpriteHelper.GetCharacterIcon(characterId);
            if (image is null)
            {
                throw new FailedToLoadResourceException<Sprite>(characterId.ToString());
            }

            SetIcon(image);
        }

        public void SetByFullCostumeOrArmorId(int armorOrFullCostumeId)
        {
            var image = SpriteHelper.GetItemIcon(armorOrFullCostumeId);
            if (image is null)
            {
                throw new FailedToLoadResourceException<Sprite>(armorOrFullCostumeId.ToString());
            }

            SetIcon(image);
        }

        protected virtual void SetDim(bool isDim)
        {
            var alpha = isDim ? .3f : 1f;
            iconImage.color = GetColor(iconImage.color, alpha);
        }

        protected static Color GetColor(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        private void SetIcon(Sprite image)
        {
            iconImage.sprite = image;
            iconImage.enabled = true;
        }
    }
}
