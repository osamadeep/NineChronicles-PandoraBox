using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Nekoyume;

namespace Nekoyume.PandoraBox
{
    public class PandoraArenaBanner : MonoBehaviour
    {
        public enum Grade
        {
            COMMON = 0,
            UNCOMMON = 1,
            RARE = 2,
            EPIC = 3,
            LEGENDARY = 4,
            MYTHIC = 5
        }


        public string ItemName; //cannot use transform.name since it may change when cloning it

        [SerializeField] private TextMeshProUGUI GradeText;
        [SerializeField] private GameObject isNFTObject;

        //Banner data
        NFTItem item;
        PandoraItem arenaBanner = new PandoraItem();

        // Start is called before the first frame update
        void OnEnable()
        {
            SetData();
        }

        void SetData()
        {
            item = PandoraBoxMaster.PanDatabase.NFTItems.Find(x => x.ItemID == ItemName);
            Debug.LogError(item.ItemID);
            arenaBanner.IsBlockchain = System.Convert.ToBoolean(int.Parse(item.ItemID.Substring(0, 1)));
            arenaBanner.Type =
                int.Parse(item.ItemID.Substring(1,
                    2)); //decide what kind of items its, for arena banner it should be 01
            arenaBanner.Grade = int.Parse(item.ItemID.Substring(4, 1)); // banner grade and color
            arenaBanner.ID = item.ItemID.Substring(4, 4); // item ID is different on the NFT ItemID
            //Debug.LogError(arenaBanner.IsBlockchain + " " + arenaBanner.Type + " " + arenaBanner.Grade + " " + arenaBanner.ID);
            Color[] gradeColors = new Color[6]
            {
                Color.white,
                new Color(0, 193, 18),
                new Color(80, 106, 253),
                new Color(243, 68, 201),
                new Color(246, 153, 36),
                Color.red
            };
            arenaBanner.Color = gradeColors[arenaBanner.Grade];
            GradeText.text =
                $"<color=#{ColorUtility.ToHtmlStringRGBA(arenaBanner.Color)}>{(Grade)arenaBanner.Grade}</color>";
            isNFTObject.gameObject.SetActive(arenaBanner.IsBlockchain);
        }
    }


    public class PandoraItem
    {
        public bool IsBlockchain;
        public int Type;
        public int Grade;
        public string ID;
        public Color Color;
    }
}