using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Metrics (GDD)")]
        [SerializeField] private float maxSpeed = 12f;
        [SerializeField] private float timeToMaxSpeed = 0.1f;
        [SerializeField] private float timeToStop = 0.08f;

        [Header("Jump Metrics (GDD)")]
        [SerializeField] private float jumpHeight = 3.5f;
        [SerializeField] private float timeToApex = 0.35f;
        [SerializeField] private float fallGravityMultiplier = 1.6f;
        [SerializeField] private float coyoteDuration = 0.1f;
        [SerializeField] private float jumpBufferDuration = 0.12f;

        [Header("Dash Metrics (GDD)")]
        [SerializeField] private float dashSpeed = 24f;
        [SerializeField] private float dashDuration = 0.2f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private float groundCheckDistance = 0.08f;

        private Rigidbody2D _rb;
        private BoxCollider2D _collider;
        private SpriteRenderer _renderer;

        // Computed physics parameters
        private float _jumpVelocity;
        private float _baseGravity;
        private float _fallGravity;

        // Runtime state
        private float _horizontalInput;
        private float _currentVelocityX;
        private bool _isGrounded;
        private float _coyoteTimer;
        private float _jumpBufferTimer;

        // Dash state
        private bool _isDashing;
        private float _dashTimer;
        private float _dashDirection = 1f;
        private bool _canDash = true;

        public bool IsGrounded => _isGrounded;
        public bool IsDashing => _isDashing;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();
            _renderer = GetComponent<SpriteRenderer>();

            // Calculate exact kinematics from GDD formula:
            // V0 = 2h / t_apex
            // g = 2h / (t_apex)^2
            _jumpVelocity = (2f * jumpHeight) / timeToApex;
            _baseGravity = (2f * jumpHeight) / (timeToApex * timeToApex);
            _fallGravity = _baseGravity * fallGravityMultiplier;

            // Turn off default Unity gravity scale because we handle custom kinematic curves
            _rb.gravityScale = 0f;
        }

        private void Update()
        {
            HandleInput();
            UpdateTimers();
        }

        private void FixedUpdate()
        {
            CheckGrounded();

            if (_isDashing)
            {
                ExecuteDash();
                return;
            }

            ApplyHorizontalMovement();
            ApplyCustomGravityAndJump();
        }

        private void HandleInput()
        {
            float moveX = 0f;
            bool jumpPressed = false;
            bool dashPressed = false;
            bool shiftPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX += 1f;

                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
                {
                    jumpPressed = true;
                }

                if (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
                {
                    shiftPressed = true;
                }

                if (Keyboard.current.jKey.wasPressedThisFrame || Keyboard.current.kKey.wasPressedThisFrame || Keyboard.current.leftCtrlKey.wasPressedThisFrame)
                {
                    dashPressed = true;
                }
            }

            if (Gamepad.current != null)
            {
                float stickX = Gamepad.current.leftStick.x.ReadValue();
                if (Mathf.Abs(stickX) > 0.15f) moveX = stickX;

                if (Gamepad.current.buttonSouth.wasPressedThisFrame) jumpPressed = true;
                if (Gamepad.current.rightShoulder.wasPressedThisFrame || Gamepad.current.buttonNorth.wasPressedThisFrame) shiftPressed = true;
                if (Gamepad.current.buttonWest.wasPressedThisFrame || Gamepad.current.rightTrigger.wasPressedThisFrame) dashPressed = true;
            }
#else
            moveX = Input.GetAxisRaw("Horizontal");
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)) jumpPressed = true;
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.E)) shiftPressed = true;
            if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K)) dashPressed = true;
#endif

            _horizontalInput = Mathf.Clamp(moveX, -1f, 1f);

            if (_horizontalInput > 0.05f) _dashDirection = 1f;
            else if (_horizontalInput < -0.05f) _dashDirection = -1f;

            if (jumpPressed)
            {
                _jumpBufferTimer = jumpBufferDuration;
            }

            if (dashPressed && _canDash && !_isDashing)
            {
                StartDash();
            }

            if (shiftPressed && RealityManager.Instance != null)
            {
                RealityManager.Instance.ToggleRealm();
            }
        }

        private void UpdateTimers()
        {
            if (_coyoteTimer > 0f) _coyoteTimer -= Time.deltaTime;
            if (_jumpBufferTimer > 0f) _jumpBufferTimer -= Time.deltaTime;

            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0f)
                {
                    _isDashing = false;
                }
            }
        }

        private void CheckGrounded()
        {
            Vector2 origin = (Vector2)transform.position + _collider.offset - new Vector2(0f, _collider.size.y * 0.5f);
            Vector2 size = new Vector2(_collider.size.x * 0.9f, groundCheckDistance);

            RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, groundLayer);

            // Filter out self
            bool wasGrounded = _isGrounded;
            _isGrounded = hit.collider != null && hit.collider.gameObject != gameObject;

            if (_isGrounded)
            {
                _coyoteTimer = coyoteDuration;
                _canDash = true; // Refresh dash on landing
            }
        }

        private void ApplyHorizontalMovement()
        {
            float targetSpeed = _horizontalInput * maxSpeed;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? (maxSpeed / timeToMaxSpeed) : (maxSpeed / timeToStop);

            _currentVelocityX = Mathf.MoveTowards(_rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(_currentVelocityX, _rb.linearVelocity.y);
        }

        private void ApplyCustomGravityAndJump()
        {
            Vector2 vel = _rb.linearVelocity;

            // Jump Execution via Jump Buffer and Coyote Time
            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
            {
                vel.y = _jumpVelocity;
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
            }
            else
            {
                // Custom gravity curve
                float gravityToApply = (vel.y < 0f) ? _fallGravity : _baseGravity;
                vel.y -= gravityToApply * Time.fixedDeltaTime;
            }

            _rb.linearVelocity = vel;
        }

        private void StartDash()
        {
            _isDashing = true;
            _dashTimer = dashDuration;
            _canDash = false;
            _rb.linearVelocity = new Vector2(_dashDirection * dashSpeed, 0f);
        }

        private void ExecuteDash()
        {
            _rb.linearVelocity = new Vector2(_dashDirection * dashSpeed, 0f);
        }

        private void OnDrawGizmosSelected()
        {
            if (_collider == null) _collider = GetComponent<BoxCollider2D>();
            if (_collider == null) return;

            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Vector2 origin = (Vector2)transform.position + _collider.offset - new Vector2(0f, _collider.size.y * 0.5f);
            Vector2 size = new Vector2(_collider.size.x * 0.9f, groundCheckDistance);
            Gizmos.DrawWireCube(origin + Vector2.down * (groundCheckDistance * 0.5f), size);
        }
    }
}
