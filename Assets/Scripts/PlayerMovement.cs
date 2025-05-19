using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // Referencias
    Rigidbody rb;
    public Transform cameraTransform;
    public Transform groundCheck;

    // Variaveis de configuracao
    [SerializeField] float speed = 10f;
    [SerializeField] float groundDistance = 0.3f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float fallMultiplier = 2.5f;
    [SerializeField] float lowJumpMultiplier = 2f;
    [SerializeField] float jumpBufferTime = 0.1f;
    private float jumpBufferTimer;
    [SerializeField] private float coyoteTime = 0.2f;
    private float coyoteTimeCounter;

    // Variaveis de estado
    private Vector2 inputMove;
    Vector3 finalDirection;
    bool isGrounded;
    bool wantsToJump;

    void Awake()
    {
        // Obtem o componente Rigidbody
        rb = GetComponent<Rigidbody>();
    }

    // Captura do movimento do jogador
    public void OnMove(InputAction.CallbackContext context)
    {
        inputMove = context.ReadValue<Vector2>();
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 moveDirection = camForward * inputMove.y + camRight * inputMove.x;
        finalDirection = moveDirection.normalized;
    }

    // Captura do comando de pulo
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            wantsToJump = true;
            jumpBufferTimer = jumpBufferTime;
        }
    }

    void FixedUpdate()
    {
        if (cameraTransform == null) return;

        // Movimentacao do jogador
        rb.linearVelocity = new Vector3(finalDirection.x * speed, rb.linearVelocity.y, finalDirection.z * speed);

        // Aplicação da fisica de queda
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !wantsToJump)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }

        // Verificacao se esta no chao com Raycast
        bool raycastHitGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundDistance);
        isGrounded = raycastHitGrounded && rb.linearVelocity.y <= 0f;

        // Controle do Coyote Time
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.fixedDeltaTime;
        }

        // Logica de pulo
        if ((coyoteTimeCounter > 0f || isGrounded) && wantsToJump)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            wantsToJump = false;
            isGrounded = false;
        }

        // Buffer de pulo para capturar o comando pouco antes de tocar o chao
        if (wantsToJump)
        {
            jumpBufferTimer -= Time.fixedDeltaTime;
            if (jumpBufferTimer <= 0)
            {
                wantsToJump = false;
            }
        }
    }

    // Define a camera do jogador
    public void SetCamera(Transform cam)
    {
        cameraTransform = cam;
    }
}
