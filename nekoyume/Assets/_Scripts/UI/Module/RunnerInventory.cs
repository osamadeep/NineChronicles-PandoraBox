using UnityEngine;
using Material = Nekoyume.Model.Item.Material;

namespace Nekoyume.UI.Module
{
    using UniRx;
    using UnityEngine.UI;

    public class RunnerInventory : MonoBehaviour
    {
        public enum RunnerInventoryTabType
        {
            Equipment,
            Consumable,
            Rune,
            Material,
            Costume,
        }

        [SerializeField] private Button equipmentButton = null;

        [SerializeField] private Button consumableButton = null;

        [SerializeField] private Button materialButton = null;

        [SerializeField] private Transform scroll = null;

        //private readonly Dictionary<ItemSubType, List<InventoryItem>> _equipments = new();
        //private readonly List<InventoryItem> _consumables = new();
        //private readonly List<InventoryItem> _materials = new();
        //private readonly List<InventoryItem> _costumes = new();
        //private readonly List<InventoryItem> _runes = new();

        private Transform _selectedModel;
        private RunnerInventoryTabType _activeTabType = RunnerInventoryTabType.Equipment;

        void Awake()
        {
        }
    }
}