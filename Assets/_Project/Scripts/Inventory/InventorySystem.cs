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
        [SerializeField] private ItemDatabase itemDatabase;
        
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
            
            if (itemDatabase == null)
            {
                Debug.LogError("ItemDatabase is not assigned in InventorySystem!");
            }
            else
            {
                itemDatabase.Initialize();
            }
            
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
            if (item == null) return false;
            
            // Если предмет стекаемый, попробуем добавить к существующим стакам
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
        
        public bool DeleteItem(int slotIndex, int amount = 1)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex].IsEmpty)
                return false;

            // Логируем удаление
            ActionLog.Instance?.LogItemDeleted(slots[slotIndex].item.itemName, slots[slotIndex].quantity);

            slots[slotIndex].RemoveItem(slots[slotIndex].quantity);


            OnInventoryChanged?.Invoke();
            return true;
        }
        
        public void UseItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex].IsEmpty)
                return;
            
            Item item = slots[slotIndex].item;
            item.Use();

            // Логируем использование
            ActionLog.Instance?.LogItemUsed(item.itemName);
            
            // Если предмет расходуемый, удаляем его
            if (item.isStackable)
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
            // Собираем только непустые слоты
            List<InventorySlot> nonEmptySlots = new List<InventorySlot>();
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    nonEmptySlots.Add(slots[i].Clone());
                }
            }
            
            // Сортируем
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
            
            // Создаем новый массив слотов
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new InventorySlot();
            }
            
            // Заполняем отсортированными предметами
            for (int i = 0; i < nonEmptySlots.Count; i++)
            {
                slots[i].item = nonEmptySlots[i].item;
                slots[i].quantity = nonEmptySlots[i].quantity;
            }
            
            OnInventoryChanged?.Invoke();
        }
        
        // Сохранение и загрузка
        public void SaveInventory()
        {
            if (itemDatabase == null)
            {
                Debug.LogError("Cannot save: ItemDatabase is not assigned!");
                return;
            }
            
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
            if (itemDatabase == null)
            {
                Debug.LogError("Cannot load: ItemDatabase is not assigned!");
                return;
            }
            
            if (!PlayerPrefs.HasKey("InventorySave"))
            {
                Debug.Log("Сохранение не найдено");
                return;
            }
            
            string json = PlayerPrefs.GetString("InventorySave");
            InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);
            
            // Очищаем инвентарь
            InitializeInventory();
            
            // Загружаем предметы через ItemDatabase
            foreach (var slotData in saveData.slots)
            {
                Item item = itemDatabase.GetItemByName(slotData.itemName);
                if (item != null)
                {
                    slots[slotData.slotIndex].AddItem(item, slotData.quantity);
                }
                else
                {
                    Debug.LogWarning($"Item '{slotData.itemName}' not found in database during load!");
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
        
        public ItemDatabase GetItemDatabase()
        {
            return itemDatabase;
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