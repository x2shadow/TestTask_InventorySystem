using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace InventorySystem
{
    public class SlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Image backgroundImage;
        
        [Header("Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.8f);
        
        private int slotIndex;
        private InventorySlot slot;
        private float lastClickTime;
        private const float doubleClickThreshold = 0.3f;
        private bool isPointerOver = false;
        private bool isInventoryOpen = false;
        
        public int SlotIndex => slotIndex;
        public InventorySlot Slot => slot;

        public void Initialize(int index, InventoryUI inventoryUI)
        {
            slotIndex = index;

            inventoryUI.OnInventoryToggle += OnInventoryToggled;

            UpdateSlot();
        }
        
        private void OnDestroy()
        {
            // Отписываемся от события при уничтожении объекта
            InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
            if (inventoryUI != null)
            {
                inventoryUI.OnInventoryToggle -= OnInventoryToggled;
            }
        }

        public void UpdateSlot()
        {
            slot = InventorySystem.Instance.GetSlot(slotIndex);

            if (slot == null || slot.IsEmpty)
            {
                iconImage.enabled = false;
                quantityText.enabled = false;
            }
            else
            {
                iconImage.enabled = true;
                iconImage.sprite = slot.item.icon;

                if (slot.item.isStackable && slot.quantity > 1)
                {
                    quantityText.enabled = true;
                    quantityText.text = slot.quantity.ToString();
                }
                else
                {
                    quantityText.enabled = false;
                }

                // Обновляем тултип если курсор над слотом
                if (isPointerOver && isInventoryOpen)
                {
                    TooltipUI.Instance?.Show(slot.item.GetTooltip(), transform.position);
                }
            }
        }
        
        private void OnInventoryToggled(bool isOpen)
        {
            isInventoryOpen = isOpen;
            
            // Если инвентарь закрывается, сбрасываем состояние слота
            if (!isOpen)
            {
                ResetState();
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;

            if (backgroundImage != null)
                backgroundImage.color = highlightColor;
            
            if (slot != null && !slot.IsEmpty && !eventData.dragging)
            {
                TooltipUI.Instance?.Show(slot.item.GetTooltip(), transform.position);
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;

            if (backgroundImage != null)
                backgroundImage.color = normalColor;
            
            TooltipUI.Instance?.Hide();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInventoryOpen) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // Проверка на двойной клик
                if (Time.time - lastClickTime < doubleClickThreshold)
                {
                    OnDoubleClick();
                }
                lastClickTime = Time.time;
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (slot != null && !slot.IsEmpty)
                {
                    InventorySystem.Instance.DeleteItem(slotIndex);
                }
            }
        }
        
        private void OnDoubleClick()
        {
            if (slot != null && !slot.IsEmpty)
            {
                InventorySystem.Instance.UseItem(slotIndex);
            }
        }

        public void SetHighlight(bool highlight)
        {
            if (backgroundImage != null)
                backgroundImage.color = highlight ? highlightColor : normalColor;
        }
        
        // Новый метод для сброса состояния слота
        public void ResetState()
        {
            isPointerOver = false;
            SetHighlight(false);
        }
    }
}