using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float rotationSpeed = 15.0f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -19.62f;

    [Header("Referanslar")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleGroundCheck();
        MovePlayer();
    }

    private void HandleGroundCheck()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
    }

    private void MovePlayer()
    {
        // 1. WASD Input Okuması
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed) vertical -= 1f;
            if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed) horizontal += 1f;
        }

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        Vector3 moveDirection = Vector3.zero;

        if (direction.magnitude >= 0.1f)
        {
            // Kameranın baktığı açıya göre hareket yönü hesapla
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;

            // Karakteri hareket yönüne yumuşakça döndür
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Baktığı yöne doğru yatay hareket vektörü
            moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }

        // 2. Zıplama Kontrolü
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 3. Yerçekimini Hesapla
        playerVelocity.y += gravity * Time.deltaTime;

        // 4. TEK ÇAĞRI: Yatay Hız + Dikey Hızı Birleştirip Birlikte Uygula
        Vector3 finalVelocity = (moveDirection.normalized * moveSpeed) + playerVelocity;
        controller.Move(finalVelocity * Time.deltaTime);
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y < 0.5f) // zemin değil, yan çarpma
            Debug.Log($"Çarpılan: {hit.collider.name}", hit.collider);
    }
}