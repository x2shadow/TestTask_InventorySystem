using System;
using UnityEngine;

namespace InventorySystem
{
    [Serializable]
    public class InventorySlot
    {
        public Item item;
        public int quantity;
        
        public InventorySlot()
        {
            item = null;
            quantity = 0;
        }
        
        public bool IsEmpty => item == null || quantity <= 0;
        
        public bool CanAddItem(Item newItem)
        {
            if (IsEmpty) return true;
            if (item == newItem && item.isStackable && quantity < item.maxStackSize)
                return true;
            return false;
        }
        
        public void AddItem(Item newItem, int amount = 1)
        {
            if (IsEmpty)
            {
                item = newItem;
                quantity = amount;
            }
            else if (item == newItem && item.isStackable)
            {
                quantity = Mathf.Min(quantity + amount, item.maxStackSize);
            }
        }
        
        public void RemoveItem(int amount = 1)
        {
            quantity -= amount;
            if (quantity <= 0)
            {
                Clear();
            }
        }
        
        public void Clear()
        {
            item = null;
            quantity = 0;
        }
        
        public InventorySlot Clone()
        {
            return new InventorySlot
            {
                item = this.item,
                quantity = this.quantity
            };
        }
    }
}