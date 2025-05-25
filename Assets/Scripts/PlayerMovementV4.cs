using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementV4 : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -20f;

    [Header("Coyote Time & Jump Buffer")]
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    private CharacterController controller;
    private Vector2 input;
    private Vector3 velocity;
    private bool isGrounded;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();

        jumpAction.action.performed += OnJump;
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();

        jumpAction.action.performed -= OnJump;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleInput();
        CheckGrounded();
        ApplyGravity();
        HandleJump();
        Move();
    }

    private void HandleInput()
    {
        input = moveAction.action.ReadValue<Vector2>();
    }

    private void CheckGrounded()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Forçar o player a ficar "colado" no chão
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        jumpBufferCounter = jumpBufferTime;
    }

    private void HandleJump()
    {
        jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            velocity.y = jumpForce;
            jumpBufferCounter = 0f;
        }
    }

    private void Move()
    {
        Vector3 move = new Vector3(input.x, 0f, input.y);

        // Movimentação em relação à câmera
        if (move.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
        }

        // Aplicar movimento vertical (gravidade e pulo)
        controller.Move(velocity * Time.deltaTime);
    }
}
