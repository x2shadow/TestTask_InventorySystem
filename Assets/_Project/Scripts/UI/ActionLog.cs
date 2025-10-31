using System.Collections;
using UnityEngine;
using TMPro;

namespace InventorySystem
{
    public class ActionLog : MonoBehaviour
    {
        public static ActionLog Instance { get; private set; }
        
        [Header("References")]
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Settings")]
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float fadeSpeed = 2f;
        
        private Coroutine fadeCoroutine;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            if (logText == null)
                logText = GetComponent<TextMeshProUGUI>();
            
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            
            // Скрываем по умолчанию
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            
            if (logText != null)
                logText.text = "";
        }
        
        public void LogAction(string message)
        {
            if (logText == null) return;
            
            logText.text = message;
            
            // Останавливаем предыдущую анимацию, если она была
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            fadeCoroutine = StartCoroutine(ShowAndFadeCoroutine());
        }
        
        public void LogItemUsed(string itemName)
        {
            LogAction($"Used {itemName}");
        }
        
        public void LogItemDeleted(string itemName, int quantity)
        {
            string quantityText = quantity > 1 ? $" x{quantity}" : "";
            LogAction($"Deleted {itemName}{quantityText}");
        }
        
        private IEnumerator ShowAndFadeCoroutine()
        {
            if (canvasGroup == null) yield break;
            
            // Плавное появление
            canvasGroup.alpha = 0f;
            while (canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha += Time.deltaTime * fadeSpeed * 2f;
                yield return null;
            }
            canvasGroup.alpha = 1f;
            
            yield return new WaitForSeconds(displayDuration);
            
            // Плавное исчезновение
            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
                yield return null;
            }
            canvasGroup.alpha = 0f;
            
            logText.text = "";
        }
    }
}