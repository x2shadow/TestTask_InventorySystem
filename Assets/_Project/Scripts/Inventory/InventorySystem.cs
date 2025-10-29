using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InventorySystem
{
    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem Instance { get; private set; }
        
        [SerializeField] private int rows = 4;
        [SerializeField] private int columns = 5;
        
        private InventorySlot[] slots;
        public int TotalSlots => rows * columns;
        
        public event Action OnInventoryChanged;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            InitializeInventory();
        }
        
        private void InitializeInventory()
        {
            slots = new InventorySlot[TotalSlots];
            for (int i = 0; i < TotalSlots; i++)
            {
                slots[i] = new InventorySlot();
            }
        }
        
        public bool AddItem(Item item, int amount = 1)
        {
            if (item == null) { Debug.Log("item == null"); return false; }
            
            // Если предмет стакаемый, попробуем добавить к существующим стакам
            if (item.isStackable)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (!slots[i].IsEmpty && slots[i].item == item && slots[i].quantity < item.maxStackSize)
                    {
                        int spaceLeft = item.maxStackSize - slots[i].quantity;
                        int amountToAdd = Mathf.Min(amount, spaceLeft);
                        slots[i].AddItem(item, amountToAdd);
                        amount -= amountToAdd;
                        
                        if (amount <= 0)
                        {
                            OnInventoryChanged?.Invoke();
                            return true;
                        }
                    }
                }
            }
            
            // Ищем пустой слот
            while (amount > 0)
            {
                int emptySlot = FindEmptySlot();
                if (emptySlot == -1)
                {
                    Debug.Log("Инвентарь полон!");
                    return false;
                }
                
                int amountToAdd = item.isStackable ? Mathf.Min(amount, item.maxStackSize) : 1;
                slots[emptySlot].AddItem(item, amountToAdd);
                amount -= amountToAdd;
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItem(int slotIndex, int amount = 1)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex].IsEmpty)
                return false;

            slots[slotIndex].RemoveItem(amount);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        public void DeleteItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex].IsEmpty)
                return;
            
            slots[slotIndex].RemoveItem(slots[slotIndex].quantity);
            OnInventoryChanged?.Invoke();
        }
        
        public void UseItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex].IsEmpty)
                return;
            
            Item item = slots[slotIndex].item;
            item.Use();
            
            // Если предмет расходуемый, удаляем его
            if (item.itemType == ItemType.Consumable || item.itemType == ItemType.Potion)
            {
                RemoveItem(slotIndex, 1);
            }
        }
        
        public void SwapSlots(int slotA, int slotB)
        {
            if (slotA < 0 || slotA >= slots.Length || slotB < 0 || slotB >= slots.Length)
                return;
            
            InventorySlot temp = slots[slotA].Clone();
            slots[slotA] = slots[slotB].Clone();
            slots[slotB] = temp;
            
            OnInventoryChanged?.Invoke();
        }
        
        public bool MergeSlots(int fromSlot, int toSlot)
        {
            if (fromSlot < 0 || fromSlot >= slots.Length || toSlot < 0 || toSlot >= slots.Length)
                return false;
            
            InventorySlot from = slots[fromSlot];
            InventorySlot to = slots[toSlot];
            
            if (from.IsEmpty) return false;
            
            // Если целевой слот пустой, просто перемещаем
            if (to.IsEmpty)
            {
                SwapSlots(fromSlot, toSlot);
                return true;
            }
            
            // Если это один и тот же предмет и он стекаемый
            if (from.item == to.item && from.item.isStackable)
            {
                int spaceLeft = to.item.maxStackSize - to.quantity;
                int amountToMove = Mathf.Min(from.quantity, spaceLeft);
                
                to.quantity += amountToMove;
                from.quantity -= amountToMove;
                
                if (from.quantity <= 0)
                {
                    from.Clear();
                }
                
                OnInventoryChanged?.Invoke();
                return true;
            }
            
            // Иначе меняем местами
            SwapSlots(fromSlot, toSlot);
            return true;
        }
        
        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= slots.Length)
                return null;
            return slots[index];
        }
        
        public int FindEmptySlot()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty)
                    return i;
            }
            return -1;
        }

        public void SortInventory(bool byType = true)
        {
            List<InventorySlot> nonEmptySlots = slots.Where(s => !s.IsEmpty).ToList();

            if (byType)
            {
                nonEmptySlots = nonEmptySlots.OrderBy(s => s.item.itemType)
                                             .ThenBy(s => s.item.itemName)
                                             .ToList();
            }
            else
            {
                nonEmptySlots = nonEmptySlots.OrderBy(s => s.item.itemName).ToList();
            }

            // Очищаем весь инвентарь
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].Clear();
            }

            // Заполняем отсортированными предметами
            for (int i = 0; i < nonEmptySlots.Count; i++)
            {
                slots[i] = nonEmptySlots[i].Clone();
            }

            OnInventoryChanged?.Invoke();
        }
        
        // Сохранение и загрузка
        
        public void SaveInventory()
        {
            InventorySaveData saveData = new InventorySaveData();
            saveData.slots = new List<SlotSaveData>();
            
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    saveData.slots.Add(new SlotSaveData
                    {
                        slotIndex = i,
                        itemName = slots[i].item.name,
                        quantity = slots[i].quantity
                    });
                }
            }
            
            string json = JsonUtility.ToJson(saveData, true);
            PlayerPrefs.SetString("InventorySave", json);
            PlayerPrefs.Save();
            Debug.Log("Инвентарь сохранен");
        }
        
        public void LoadInventory()
        {
            if (!PlayerPrefs.HasKey("InventorySave"))
            {
                Debug.Log("Сохранение не найдено");
                return;
            }
            
            string json = PlayerPrefs.GetString("InventorySave");
            InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);
            
            // Очищаем инвентарь
            InitializeInventory();
            
            // Загружаем предметы
            foreach (var slotData in saveData.slots)
            {
                Item item = Resources.Load<Item>($"Items/{slotData.itemName}");
                if (item != null)
                {
                    slots[slotData.slotIndex].AddItem(item, slotData.quantity);
                }
            }
            
            OnInventoryChanged?.Invoke();
            Debug.Log("Инвентарь загружен");
        }
        
        public void ClearInventory()
        {
            InitializeInventory();
            OnInventoryChanged?.Invoke();
        }
    }
    
    [Serializable]
    public class InventorySaveData
    {
        public List<SlotSaveData> slots;
    }
    
    [Serializable]
    public class SlotSaveData
    {
        public int slotIndex;
        public string itemName;
        public int quantity;
    }
}