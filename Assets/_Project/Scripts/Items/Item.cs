using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item")]
    public class Item : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        
        [Header("Properties")]
        public ItemType itemType;
        public bool isStackable;
        public int maxStackSize = 99;
        
        //[Header("Optional")]
        //public GameObject worldPrefab;
        
        public virtual void Use()
        {
            Debug.Log($"Used {itemName}");
        }
        
        public virtual string GetTooltip()
        {
            return $"<b>{itemName}</b>\n<i>{itemType}</i>\n\n{description}";
        }
    }
}