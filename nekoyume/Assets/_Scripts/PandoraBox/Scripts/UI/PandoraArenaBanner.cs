using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Nekoyume;

namespace Nekoyume.PandoraBox
{
    public class PandoraArenaBanner : MonoBehaviour
    {
        public string ItemName; //cannot use transform.name since it may change when cloning it

        [SerializeField] private TextMeshProUGUI GradeText;
        [SerializeField] private GameObject isNFTObject;

        //Banner data
        NFTItem item; //showing general info of item
        PandoraItem arenaBanner; //set banner settings

        // Start is called before the first frame update
        void OnEnable()
        {
            SetData();
        }

        void SetData()
        {
            item = PandoraMaster.PanDatabase.NFTItems.Find(x => x.ItemID == ItemName);
            arenaBanner = PandoraUtil.GetPandoraItem(ItemName);
            GradeText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(arenaBanner.Color)}>{arenaBanner.Grade}</color>";
            //Debug.LogError(ColorUtility.ToHtmlStringRGB(arenaBanner.Color) + "   " + arenaBanner.Color);
            isNFTObject.gameObject.SetActive(arenaBanner.IsBlockchain);
        }
    }
}
