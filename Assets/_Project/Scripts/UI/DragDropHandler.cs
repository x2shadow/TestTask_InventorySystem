using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace InventorySystem
{
    public class DragDropHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private GraphicRaycaster raycaster;
        
        private SlotUI currentSlot;
        private GameObject draggedIcon;
        private RectTransform draggedRectTransform;
        private CanvasGroup draggedCanvasGroup;
        private int draggedSlotIndex = -1;
        
        private void Start()
        {
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
            
            if (raycaster == null)
                raycaster = canvas.GetComponent<GraphicRaycaster>();
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            currentSlot = eventData.pointerCurrentRaycast.gameObject.GetComponent<SlotUI>();
            
            if (currentSlot == null || currentSlot.Slot == null || currentSlot.Slot.IsEmpty)
                return;
            
            draggedSlotIndex = currentSlot.SlotIndex;
            
            // Создаем визуальную копию иконки для перетаскивания
            draggedIcon = new GameObject("DraggedIcon");
            draggedRectTransform = draggedIcon.AddComponent<RectTransform>();
            draggedIcon.transform.SetParent(canvas.transform, false);
            draggedIcon.transform.SetAsLastSibling();
            
            Image iconImage = draggedIcon.AddComponent<Image>();
            iconImage.sprite = currentSlot.Slot.item.icon;
            iconImage.raycastTarget = false;
            
            draggedRectTransform.sizeDelta = new Vector2(100, 100);
            
            draggedCanvasGroup = draggedIcon.AddComponent<CanvasGroup>();
            draggedCanvasGroup.alpha = 0.7f;
            draggedCanvasGroup.blocksRaycasts = false;
            
            TooltipUI.Instance?.Hide();
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (draggedIcon != null)
            {
                draggedRectTransform.position = eventData.position;
            }
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (draggedIcon != null)
            {
                Destroy(draggedIcon);
            }
            
            if (draggedSlotIndex == -1)
                return;
            
            // Находим слот, на который мы перетащили предмет
            SlotUI targetSlot = null;
            
            var results = new System.Collections.Generic.List<RaycastResult>();
            raycaster.Raycast(eventData, results);
            
            foreach (var result in results)
            {
                targetSlot = result.gameObject.GetComponent<SlotUI>();
                if (targetSlot != null)
                    break;
            }
            
            if (targetSlot != null && targetSlot.SlotIndex != draggedSlotIndex)
            {
                // Пробуем объединить слоты
                InventorySystem.Instance.MergeSlots(draggedSlotIndex, targetSlot.SlotIndex);
            }
            
            draggedSlotIndex = -1;
        }
    }
}