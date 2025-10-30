using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace InventorySystem
{
    public class TooltipUI : MonoBehaviour
    {
        public static TooltipUI Instance { get; private set; }
        
        [SerializeField] private RectTransform tooltipRect;
        [SerializeField] private TextMeshProUGUI tooltipText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float padding = 10f;
        [SerializeField] private float offsetX = 0f;
        [SerializeField] private float offsetY = 0f;
        
        private Canvas canvas;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            canvas = GetComponentInParent<Canvas>();
            
            Hide();
        }
        
        public void Show(string text, Vector2 mousePosition)
        {
            if (string.IsNullOrEmpty(text))
            {
                Hide();
                return;
            }
            
            gameObject.SetActive(true);
            tooltipText.text = text;
            
            // Обновляем размер тултипа под текст
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
            
            // Позиционируем тултип
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                mousePosition,
                canvas.worldCamera,
                out localPoint
            );
            
            tooltipRect.localPosition = localPoint + new Vector2(padding, -padding);
            
            // Проверяем, чтобы тултип не выходил за края экрана
            Vector3[] corners = new Vector3[4];
            tooltipRect.GetWorldCorners(corners);
            
            float overflowX = 0;
            float overflowY = 0;
            
            RectTransform canvasRect = canvas.transform as RectTransform;
            
            if (corners[2].x > Screen.width)
                overflowX = corners[2].x - Screen.width;
            
            if (corners[0].y < 0)
                overflowY = -corners[0].y;

            tooltipRect.localPosition -= new Vector3(overflowX / canvas.scaleFactor, -overflowY / canvas.scaleFactor, 0);

            tooltipRect.localPosition += new Vector3(offsetX, offsetY, 0);
            
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }
        
        public void Hide()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}