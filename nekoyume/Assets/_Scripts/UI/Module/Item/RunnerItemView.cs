using System;
using System.Linq;
using Coffee.UIEffects;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.PandoraBox;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Nekoyume
{
    public class RunnerItemView : MonoBehaviour
    {
        [SerializeField] private GameObject container;

        [SerializeField] private GameObject emptyObject;

        [SerializeField] private TouchHandler touchHandler;

        [SerializeField] private Image itemImage;

        [SerializeField] private TextMeshProUGUI countText;

        [SerializeField] private GameObject lockObject;

        public GameObject Container => container;
        public GameObject EmptyObject => emptyObject;
        public TouchHandler TouchHandler => touchHandler;
        public Image ItemImage => itemImage;
        public TextMeshProUGUI CountText => countText;
        public GameObject LockObject => lockObject;

        public static void GetItemViewData()
        {
        }
    }
}