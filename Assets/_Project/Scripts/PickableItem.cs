using UnityEngine;
using TMPro;

namespace InventorySystem
{
    public class PickableItem : MonoBehaviour
    {
        [Header("Item Data")]
        [SerializeField] private Item itemData;
        [SerializeField] private int quantity = 1;
        
        [Header("Effects")]
        [SerializeField] private bool rotateItem = true;
        [SerializeField] private float rotationSpeed = 50f;
        [SerializeField] private bool bobUpDown = true;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.2f;
        
        private Vector3 startPosition;
        
        private void Start()
        {
            if (itemData == null)
            {
                Debug.LogError("PickableItem: Item data is not assigned!");
                enabled = false;
                return;
            }
            
            startPosition = transform.position;
        }
        
        private void Update()
        {
            // Визуальные эффекты
            if (rotateItem)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
            
            if (bobUpDown)
            {
                float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }

        }
        
        public void PickUp()
        {
            if (InventorySystem.Instance.AddItem(itemData, quantity))
            {
                Debug.Log($"Подобран предмет: {itemData.itemName} x{quantity}");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("Инвентарь полон!");
            }
        }
    }
}