using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Feedback;
using EchoOfTheVoid.Player.States;

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

        [Header("Wall Jump Metrics (GDD)")]
        [SerializeField] private float wallJumpHorizontalSpeed = 11f;
        [SerializeField] private float wallJumpVerticalSpeed = 16f;

        [Header("Dash Metrics (GDD)")]
        [SerializeField] private float dashSpeed = 24f;
        [SerializeField] private float dashDuration = 0.2f;

        [Header("Collision Layers")]
        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private float groundCheckDistance = 0.08f;
        [SerializeField] private float wallCheckDistance = 0.15f;

        private Rigidbody2D _rb;
        private BoxCollider2D _collider;
        private SpriteRenderer _renderer;
        private SquashAndStretch _squash;
        private GhostTrail _ghostTrail;
        private PlayerCombat _combat;
        private PlayerStats _stats;

        // Kinematics parameters
        private float _jumpVelocity;
        private float _baseGravity;
        private float _fallGravity;

        // Runtime State
        private PlayerStateMachine _stateMachine;
        private float _horizontalInput;
        private float _facingDirection = 1f;
        private bool _isGrounded;
        private bool _isTouchingWall;
        private float _wallDirection;
        private float _coyoteTimer;
        private float _jumpBufferTimer;

        // Dash & Freeze timers
        private bool _canDash = true;
        private float _freezeVerticalTimer;

        public float HorizontalInput => _horizontalInput;
        public float FacingDirection => _facingDirection;
        public bool IsGrounded => _isGrounded;
        public bool IsTouchingWall => _isTouchingWall;
        public float WallDirection => _wallDirection;
        public float DashDuration => dashDuration;
        public Vector2 LinearVelocity => _rb.linearVelocity;
        public bool IsAttacking => (_combat != null && _combat.IsAttacking);
        public string CurrentStateName => _stateMachine?.CurrentState?.GetType().Name ?? "None";

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _squash = GetComponent<SquashAndStretch>();
            _ghostTrail = GetComponent<GhostTrail>();
            _combat = GetComponent<PlayerCombat>();
            _stats = GetComponent<PlayerStats>();

            // Calculate kinematic parameters
            _jumpVelocity = (2f * jumpHeight) / timeToApex;
            _baseGravity = (2f * jumpHeight) / (timeToApex * timeToApex);
            _fallGravity = _baseGravity * fallGravityMultiplier;

            _rb.gravityScale = 0f;

            // Initialize FSM
            _stateMachine = new PlayerStateMachine(this);
            _stateMachine.Initialize(new PlayerIdleState());
        }

        private void Update()
        {
            HandleInputs();
            UpdateTimers();
            _stateMachine.Update();
        }

        private void FixedUpdate()
        {
            CheckEnvironment();
            _stateMachine.FixedUpdate();
        }

        private void HandleInputs()
        {
            float moveX = 0f;
            bool jumpPressed = false;
            bool dashPressed = false;
            bool shiftPressed = false;
            bool attackPressed = false;
            bool resonancePressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX += 1f;

                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) jumpPressed = true;
                if (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame) shiftPressed = true;
                if (Keyboard.current.jKey.wasPressedThisFrame || Keyboard.current.zKey.wasPressedThisFrame) attackPressed = true;
                if (Keyboard.current.kKey.wasPressedThisFrame || Keyboard.current.leftCtrlKey.wasPressedThisFrame) dashPressed = true;
                if (Keyboard.current.lKey.wasPressedThisFrame || Keyboard.current.uKey.wasPressedThisFrame) resonancePressed = true;
            }

            if (Gamepad.current != null)
            {
                float stickX = Gamepad.current.leftStick.x.ReadValue();
                if (Mathf.Abs(stickX) > 0.15f) moveX = stickX;

                if (Gamepad.current.buttonSouth.wasPressedThisFrame) jumpPressed = true;
                if (Gamepad.current.rightShoulder.wasPressedThisFrame) shiftPressed = true;
                if (Gamepad.current.buttonWest.wasPressedThisFrame) attackPressed = true;
                if (Gamepad.current.rightTrigger.wasPressedThisFrame) dashPressed = true;
                if (Gamepad.current.buttonNorth.wasPressedThisFrame) resonancePressed = true;
            }
#else
            moveX = Input.GetAxisRaw("Horizontal");
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)) jumpPressed = true;
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.E)) shiftPressed = true;
            if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Z)) attackPressed = true;
            if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.LeftControl)) dashPressed = true;
            if (Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.U)) resonancePressed = true;
#endif

            _horizontalInput = Mathf.Clamp(moveX, -1f, 1f);

            if (_horizontalInput > 0.05f)
            {
                _facingDirection = 1f;
                if (_renderer != null) _renderer.flipX = false;
            }
            else if (_horizontalInput < -0.05f)
            {
                _facingDirection = -1f;
                if (_renderer != null) _renderer.flipX = true;
            }

            if (jumpPressed) _jumpBufferTimer = jumpBufferDuration;
            if (dashPressed && _canDash) _jumpBufferTimer = 0f; // prioritize dash

            // Reality Shift
            if (shiftPressed && RealityManager.Instance != null)
            {
                RealityManager.Instance.ToggleRealm();
            }

            // Attacks
            if (attackPressed && _combat != null)
            {
                _combat.TryNormalAttack();
            }
            if (resonancePressed && _combat != null)
            {
                _combat.TryResonanceStrike();
            }
        }

        private void UpdateTimers()
        {
            if (_coyoteTimer > 0f) _coyoteTimer -= Time.deltaTime;
            if (_jumpBufferTimer > 0f) _jumpBufferTimer -= Time.deltaTime;
            if (_freezeVerticalTimer > 0f) _freezeVerticalTimer -= Time.deltaTime;
        }

        private void CheckEnvironment()
        {
            // Ground Check
            Vector2 origin = (Vector2)transform.position + _collider.offset - new Vector2(0f, _collider.size.y * 0.5f);
            Vector2 size = new Vector2(_collider.size.x * 0.85f, groundCheckDistance);

            RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, groundLayer);
            bool wasGrounded = _isGrounded;
            _isGrounded = hit.collider != null && hit.collider.gameObject != gameObject;

            if (_isGrounded)
            {
                _coyoteTimer = coyoteDuration;
                _canDash = true; // reset dash on landing
            }

            // Wall Check
            _isTouchingWall = false;
            _wallDirection = 0f;

            if (!_isGrounded)
            {
                Vector2 wallOrigin = (Vector2)transform.position + _collider.offset;
                Vector2 wallSize = new Vector2(wallCheckDistance, _collider.size.y * 0.7f);

                RaycastHit2D hitRight = Physics2D.BoxCast(wallOrigin, wallSize, 0f, Vector2.right, wallCheckDistance, groundLayer);
                if (hitRight.collider != null && hitRight.collider.gameObject != gameObject)
                {
                    _isTouchingWall = true;
                    _wallDirection = 1f;
                }
                else
                {
                    RaycastHit2D hitLeft = Physics2D.BoxCast(wallOrigin, wallSize, 0f, Vector2.left, wallCheckDistance, groundLayer);
                    if (hitLeft.collider != null && hitLeft.collider.gameObject != gameObject)
                    {
                        _isTouchingWall = true;
                        _wallDirection = -1f;
                    }
                }
            }
        }

        public void ChangeState(IPlayerState newState)
        {
            _stateMachine.ChangeState(newState);
        }

        public bool CheckAndConsumeJump()
        {
            if (_jumpBufferTimer > 0f && (_coyoteTimer > 0f || _isTouchingWall))
            {
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                return true;
            }
            return false;
        }

        public bool CheckAndConsumeDash()
        {
#if ENABLE_INPUT_SYSTEM
            bool dashInput = (Keyboard.current != null && (Keyboard.current.kKey.wasPressedThisFrame || Keyboard.current.leftCtrlKey.wasPressedThisFrame))
                          || (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame);
#else
            bool dashInput = Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.LeftControl);
#endif
            if (dashInput && _canDash)
            {
                _canDash = false;
                return true;
            }
            return false;
        }

        public bool CheckAndConsumeAttack()
        {
            return _combat != null && _combat.IsAttacking;
        }

        public void ApplyHorizontalMovement()
        {
            float targetSpeed = _horizontalInput * maxSpeed;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? (maxSpeed / timeToMaxSpeed) : (maxSpeed / timeToStop);

            float vx = Mathf.MoveTowards(_rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(vx, _rb.linearVelocity.y);
        }

        public void ApplyDeceleration()
        {
            float vx = Mathf.MoveTowards(_rb.linearVelocity.x, 0f, (maxSpeed / timeToStop) * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(vx, _rb.linearVelocity.y);
        }

        public void ApplyCustomGravity(bool isFalling)
        {
            if (_freezeVerticalTimer > 0f)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
                return;
            }

            Vector2 vel = _rb.linearVelocity;
            float g = (isFalling || vel.y < 0f) ? _fallGravity : _baseGravity;
            vel.y -= g * Time.fixedDeltaTime;
            _rb.linearVelocity = vel;
        }

        public void ExecuteJump()
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpVelocity);
            if (_squash != null) _squash.OnJump();
        }

        public void ExecuteWallJump()
        {
            // Jump away from the wall
            float launchX = -_wallDirection * wallJumpHorizontalSpeed;
            _rb.linearVelocity = new Vector2(launchX, wallJumpVerticalSpeed);
            _facingDirection = -_wallDirection;
            if (_renderer != null) _renderer.flipX = (_facingDirection < 0f);
            if (_squash != null) _squash.OnJump();
        }

        public void StartDash()
        {
            if (_stats != null) _stats.SetInvulnerable(true);
            _rb.linearVelocity = new Vector2(_facingDirection * dashSpeed, 0f);
            if (_squash != null) _squash.OnDash();

            RealmType realm = (RealityManager.Instance != null) ? RealityManager.Instance.CurrentRealm : RealmType.Prime;
            if (_ghostTrail != null) _ghostTrail.StartTrail(realm);
        }

        public void MaintainDashVelocity()
        {
            _rb.linearVelocity = new Vector2(_facingDirection * dashSpeed, 0f);
        }

        public void EndDash()
        {
            if (_stats != null) _stats.SetInvulnerable(false);
            if (_ghostTrail != null) _ghostTrail.StopTrail();
        }

        public void SetVelocity(Vector2 velocity)
        {
            _rb.linearVelocity = velocity;
        }

        public void SetHorizontalVelocity(float vx)
        {
            _rb.linearVelocity = new Vector2(vx, _rb.linearVelocity.y);
        }

        public void ResetVerticalVelocity()
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        }

        public void FreezeVerticalVelocityFor(float seconds)
        {
            _freezeVerticalTimer = seconds;
            ResetVerticalVelocity();
        }

        public void TriggerSquashLand()
        {
            if (_squash != null) _squash.OnLand();
        }
    }
}
