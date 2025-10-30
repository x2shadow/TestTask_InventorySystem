using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<Item> items = new List<Item>();
        
        private Dictionary<string, Item> itemDictionary;
        
        public void Initialize()
        {
            itemDictionary = new Dictionary<string, Item>();
            
            foreach (var item in items)
            {
                if (item != null && !itemDictionary.ContainsKey(item.name))
                {
                    itemDictionary.Add(item.name, item);
                }
            }
            
            Debug.Log($"ItemDatabase initialized with {itemDictionary.Count} items");
        }
        
        public Item GetItemByName(string itemName)
        {
            if (itemDictionary == null || itemDictionary.Count == 0)
                Initialize();
            
            if (itemDictionary.TryGetValue(itemName, out Item item))
            {
                return item;
            }
            
            Debug.LogWarning($"Item '{itemName}' not found in database!");
            return null;
        }
        
        public List<Item> GetAllItems()
        {
            return new List<Item>(items);
        }
        
        public List<Item> GetItemsByType(ItemType type)
        {
            return items.FindAll(item => item != null && item.itemType == type);
        }
        
        public void AddItem(Item item)
        {
            if (item != null && !items.Contains(item))
            {
                items.Add(item);
                if (itemDictionary != null && !itemDictionary.ContainsKey(item.name))
                {
                    itemDictionary.Add(item.name, item);
                }
            }
        }
        
#if UNITY_EDITOR
        [ContextMenu("Auto-Fill Items from Project")]
        private void AutoFillItems()
        {
            items.Clear();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Item");
            
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                Item item = UnityEditor.AssetDatabase.LoadAssetAtPath<Item>(path);
                if (item != null)
                {
                    items.Add(item);
                }
            }
            
            Debug.Log($"Auto-filled {items.Count} items");
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}