using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using InventorySystem;

public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 1.0f;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private CharacterController characterController;

    [Header("Камера игрока")]
    public Camera playerCamera;
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 90.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -90.0f;
    [Tooltip("Invert Y look (true = typical 'flight' invert)")]
    public bool invertY = false;

    // cinemachine
    private float cinemachineTargetPitch;
    private float rotationVelocity;
    private const float threshold = 0.01f;

    [HideInInspector]
    public InputActions inputActions;
    
    [Header("Инвентарь")]
    [SerializeField] InventoryUI inventoryUI;

    [Header("Пауза")]
    [SerializeField] private GameObject pauseCanvas;
    private bool isPaused = false;

    private bool isInputBlocked = false;

    [Header("Physics / Ground check")]
    public Transform groundCheck;
    public float groundDistance = 0.18f;
    [SerializeField] LayerMask groundMask;
    public float gravity = -9.81f;
    public bool isGrounded = false;

    private Vector3 velocity;

    [Header("Взаимодействие")]
    public float interactDistance = 3f; // дальность луча
    public LayerMask interactMask; // слои для проверки
    public LayerMask obstacleMask; // слои препятствий
    public GameObject interactPromptUI; // UI-элемент "E" в Canvas

    private PickableItem currentPickableItem;

    [Header("Frame Rate Settings")]
    public int targetFrameRate = 60;

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return inputActions.Player.Look.activeControl?.device is UnityEngine.InputSystem.Mouse;
#else
            return false;
#endif
        }
    }

    private void Awake()
    {
        inputActions = new InputActions();
        characterController = GetComponent<CharacterController>();
        playerCamera = Camera.main;

        //originalControllerHeight = characterController.height;
        //originalControllerCenter = characterController.center;
        //currentHeight = characterController.height;

        //cameraInitialLocalPos = CinemachineCameraTarget.transform.localPosition;
    }

    void Start()
    {
        float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
        mouseSensitivity = savedSensitivity;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

#if !UNITY_EDITOR
            ApplyFrameRateSettings();
#endif
    }

    private void OnEnable()
    {
        inputActions.Enable();
        // Подписка на события для экшенов
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Look.performed += OnLook;
        inputActions.Player.Look.canceled += OnLook;
        inputActions.Player.Interact.performed += OnInteract;
        inputActions.Player.Pause.performed += OnPause;
        inputActions.Player.Inventory.performed += OnInventory;
    }

    private void OnDisable()
    {
        inputActions.Disable();
        // Отписка от событий
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Look.performed -= OnLook;
        inputActions.Player.Look.canceled -= OnLook;
        inputActions.Player.Interact.performed -= OnInteract;
        inputActions.Player.Pause.performed -= OnPause;
        inputActions.Player.Inventory.performed -= OnInventory;
    }

    private void Update()
    {
        if (isInputBlocked) return;

        HandleMovement();
        HandleInteractionRay();
    }

    void LateUpdate()
    {
        HandleLook();
    }

    void HandleMovement()
    {
        // Движение персонажа
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move = move.normalized; // нормализация

        // вычисляем текущую скорость с учётом Sprint/Crouch
        float speedMultiplier = 1f;
        float effectiveSpeed = moveSpeed * speedMultiplier;

        // Проверка земли
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        else
            isGrounded = characterController.isGrounded;

        // если на земле и идёт небольшая вниз. скорость — удерживаем на небольшом значении, чтобы персонаж "прислонялся" к земле
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        // применяем гравитацию
        velocity.y += gravity * Time.deltaTime;

        // объединяем движение
        Vector3 finalMove = move * effectiveSpeed + velocity;

        // двигаем CharacterController
        characterController.Move(finalMove * Time.deltaTime);
    }

    void HandleLook()
    {
        // if there is an input
        if (lookInput.sqrMagnitude >= threshold)
        {
            //Don't multiply mouse input by Time.deltaTime
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            // обработка инверсии по Y
            float yInput = invertY ? lookInput.y : -lookInput.y;

            cinemachineTargetPitch += yInput * mouseSensitivity * deltaTimeMultiplier;
            rotationVelocity = lookInput.x * mouseSensitivity * deltaTimeMultiplier;

            // clamp our pitch rotation
            cinemachineTargetPitch = ClampAngle(cinemachineTargetPitch, BottomClamp, TopClamp);

            // Update Cinemachine camera target pitch
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(cinemachineTargetPitch, 0.0f, 0.0f);

            // rotate the player left and right
            transform.Rotate(Vector3.up * rotationVelocity);
        }
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        angle %= 360f; // нормализуем в диапазон -360...360
        if (angle < -180f) angle += 360f; // теперь диапазон -180...180
        return Mathf.Clamp(angle, min, max);
    }

    void OnDrawGizmos()
    {
        if (isGrounded)
        {
            Gizmos.color = Color.green;
        }
        else Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
    }

    private void HandleInteractionRay()
    {
        currentPickableItem = null;
        interactPromptUI.SetActive(false);

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask | obstacleMask))
        {
            currentPickableItem = hit.collider.GetComponent<PickableItem>();

            if (currentPickableItem != null)
            {
                interactPromptUI.SetActive(true);
            }
        }
    }

    private void OnInteract(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;

        if (context.performed && currentPickableItem != null)
        {
            currentPickableItem.PickUp();
        }
    }

    public void ApplyFrameRateSettings()
    {
        // Установка целевого FPS
        Application.targetFrameRate = targetFrameRate;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TogglePause();
        }
    }

    private void OnInventory(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            inventoryUI.ToggleInventory();
            SetInputBlocked(inventoryUI.IsOpen);
        } 
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        pauseCanvas.SetActive(isPaused);

        if (isPaused)
        {
            Time.timeScale = 0f;  // Останавливаем время
            SetInputBlocked(true); // Блокируем управление
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;  // Возвращаем время
            SetInputBlocked(false); // Возвращаем управление
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void SetInputBlocked(bool blocked)
    {
        isInputBlocked = blocked;
        if (blocked)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
        }
    }
}
