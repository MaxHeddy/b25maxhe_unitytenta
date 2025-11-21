using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(CapsuleCollider2D))]
public class PlatformerMovement : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator maxAnimator;

    public bool controlEnabled { get; set; } = true;

    private Vector2 moveInput;
    private Rigidbody2D rb;
    private CircleCollider2D groundCheckCollider;

    // Ground & jump state
    private LayerMask groundLayer = ~0;
    private Vector2 velocity;
    private bool jumpInput;
    private bool jumpReleased;
    private bool wasGrounded;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        groundCheckCollider = GetComponent<CircleCollider2D>();
        groundCheckCollider.isTrigger = true;

        // Disable built-in gravity
        rb.gravityScale = 0;

        if (maxAnimator == null)
            maxAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        velocity = TranslateInputToVelocity(moveInput);

        // Jump
        if (jumpInput && wasGrounded)
        {
            velocity.y = jumpForce;
            jumpInput = false;
        }

        // Detect ground contact changes
        if (wasGrounded && !isGrounded)
        {
            if (velocity.y > 0)
            {
                // Jump started
                maxAnimator.SetBool("Jump", true);
            }
        }
        else if (!wasGrounded && isGrounded)
        {
            // Landed
            jumpReleased = false;
            maxAnimator.SetBool("Jump", false);
        }

        wasGrounded = isGrounded;

        // Flip sprite
        if (spriteRenderer)
        {
            if (moveInput.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (moveInput.x < -0.01f)
                spriteRenderer.flipX = true;
        }
    }

    private void FixedUpdate()
    {
        isGrounded = IsGrounded();
        ApplyGravity();
        rb.linearVelocity = velocity;

        UpdateAnimations();
    }

    private bool IsGrounded()
    {
        return groundCheckCollider.IsTouchingLayers(groundLayer);
    }

    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -1f;
        }
        else
        {
            if (velocity.y > 0)
            {
                float deceleration = jumpReleased ? 5f : 1f;
                velocity.y += Physics2D.gravity.y * deceleration * Time.deltaTime;
            }
            else
            {
                velocity.y += Physics2D.gravity.y * Time.deltaTime;
            }
        }
    }

    Vector2 TranslateInputToVelocity(Vector2 input)
    {
        return new Vector2(input.x * maxSpeed, velocity.y);
    }

    private void UpdateAnimations()
    {
        if (maxAnimator == null) return;

        maxAnimator.SetFloat("velocityX", Mathf.Abs(velocity.x));
        maxAnimator.SetFloat("velocityY", velocity.y);
        maxAnimator.SetBool("isGrounded", isGrounded);

        // Running = horizontal movement on ground
        bool running = Mathf.Abs(moveInput.x) > 0.1f && isGrounded;
        maxAnimator.SetBool("isRunning", running);
    }

    // Movement input
    public void OnMove(InputAction.CallbackContext context)
    {
        if (controlEnabled)
            moveInput = context.ReadValue<Vector2>().normalized;
        else
            moveInput = Vector2.zero;
    }

    // Jump input
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && controlEnabled)
        {
            jumpInput = true;
            jumpReleased = false;
        }

        if (context.canceled && controlEnabled)
        {
            jumpReleased = true;
            jumpInput = false;
        }
    }
}