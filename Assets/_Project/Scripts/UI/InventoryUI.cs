using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

namespace InventorySystem
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform slotsParent;
        [SerializeField] private GameObject slotPrefab;
        
        [Header("Buttons")]
        [SerializeField] private Button sortByTypeButton;
        [SerializeField] private Button sortByNameButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button clearButton;
        
        private List<SlotUI> slotUIList = new List<SlotUI>();
        private bool isOpen = false;

        // События для уведомления слотов о состоянии инвентаря
        public Action<bool> OnInventoryToggle;
        
        private void Start()
        {
            CreateSlots();
            SetupButtons();
            
            InventorySystem.Instance.OnInventoryChanged += RefreshUI;
            
            inventoryPanel.SetActive(false);
        }
        
        private void OnDestroy()
        {
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.OnInventoryChanged -= RefreshUI;
            }
        }
        
        private void CreateSlots()
        {
            int totalSlots = InventorySystem.Instance.TotalSlots;
            
            for (int i = 0; i < totalSlots; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotsParent);
                SlotUI slotUI = slotObj.GetComponent<SlotUI>();
                
                if (slotUI != null)
                {
                    slotUI.Initialize(i, this);
                    slotUIList.Add(slotUI);
                }
            }
        }
        
        private void SetupButtons()
        {
            if (sortByTypeButton != null)
                sortByTypeButton.onClick.AddListener(() => InventorySystem.Instance.SortInventory(true));
            
            if (sortByNameButton != null)
                sortByNameButton.onClick.AddListener(() => InventorySystem.Instance.SortInventory(false));
            
            if (saveButton != null)
                saveButton.onClick.AddListener(() => InventorySystem.Instance.SaveInventory());
            
            if (loadButton != null)
                loadButton.onClick.AddListener(() => InventorySystem.Instance.LoadInventory());
            
            if (clearButton != null)
                clearButton.onClick.AddListener(() => InventorySystem.Instance.ClearInventory());
        }
        
        public void ToggleInventory()
        {
            isOpen = !isOpen;
            inventoryPanel.SetActive(isOpen);

            if (isOpen)
            {
                RefreshUI();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // Уведомляем все слоты о закрытии инвентаря
                OnInventoryToggle?.Invoke(false);
                TooltipUI.Instance?.Hide();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            
            // Уведомляем все слоты об изменении состояния инвентаря
            OnInventoryToggle?.Invoke(isOpen);
        }

        private void RefreshUI()
        {
            foreach (var slotUI in slotUIList)
            {
                slotUI.UpdateSlot();
            }
        }
        
        public bool IsOpen => isOpen;
    }
}