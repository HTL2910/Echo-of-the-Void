using System;
using UnityEngine;
using EchoOfTheVoid.Settings;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Enemies;
using EchoOfTheVoid.UI;
using EchoOfTheVoid.Feedback;
using EchoOfTheVoid.Player.States;

namespace EchoOfTheVoid.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Metrics (GDD & Master Spec)")]
        [SerializeField] private float maxSpeed = 12f;
        [SerializeField] private float timeToMaxSpeed = 0.1f;
        [SerializeField] private float timeToStop = 0.08f;

        [Header("Jump Metrics (GDD & Master Spec)")]
        [SerializeField] private float jumpHeight = 3.5f;
        [SerializeField] private float timeToApex = 0.35f;
        [SerializeField] private float fallGravityMultiplier = 1.6f;
        [SerializeField] private float coyoteDuration = 0.1f;
        [SerializeField] private float jumpBufferDuration = 0.12f;

        [Header("Wall Jump Metrics (GDD & Master Spec)")]
        [SerializeField] private float wallJumpHorizontalSpeed = 11f;
        [SerializeField] private float wallJumpVerticalSpeed = 16f;
        [SerializeField] private float wallJumpInputLock = 0.12f; // spec 3.2: keeps the kick from being cancelled at once
        [SerializeField] private float wallCoyoteDuration = 0.15f;

        [Header("Dash Metrics (GDD & Master Spec)")]
        [SerializeField] private float dashSpeed = 20f; // Spec: 4 tiles / 0.2s = 20 units/s
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 0.8f; // Spec: 0.8s cooldown

        [Header("Collision Layers")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 0.08f;
        [SerializeField] private float wallCheckDistance = 0.15f;

        private Rigidbody2D _rb;
        private BoxCollider2D _collider;
        private SpriteRenderer _renderer;
        private SquashAndStretch _squash;
        private GhostTrail _ghostTrail;
        private PlayerCombat _combat;
        private PlayerStats _stats;
        private AbilitySet _abilities;

        // Kinematics parameters
        private float _jumpVelocity;
        private float _baseGravity;
        private float _fallGravity;

        // Runtime State
        private PlayerStateMachine _stateMachine;
        private IPlayerInput _input = new DevicePlayerInput();
        private PlayerInputFrame _frame;
        private float _horizontalInput;
        private float _facingDirection = 1f;
        private bool _isGrounded;
        private bool _isTouchingWall;
        private float _wallDirection;
        private float _lastWallDirection;
        private float _coyoteTimer;
        private float _wallCoyoteTimer;
        private float _jumpBufferTimer;

        // Dash & Cooldown timers
        private float _dashCooldownTimer;
        private bool _canDash = true;
        private float _freezeVerticalTimer;
        private float _wallJumpLockTimer;
        private float _inputLockTimer;
        private float _lastFallSpeed;

        // Gravity Inversion: +1 = normal, -1 = walking on the ceiling (only possible inside a GravitonField)
        private float _gravitySign = 1f;
        private int _gravitonFields;
        private float _gravityRevertTimer = -1f;
        private const float GravityRevertDelay = 1f; // spec 4: gravity returns 1 s after leaving the field

        // Rail Grind: an active energy cable Kael is touching
        private RailCable _railInReach;
        private float _railCooldown;
        private EchoAnchor _anchor;
        private float _footstepTimer;

        public float HorizontalInput => _horizontalInput;
        public float FacingDirection => _facingDirection;
        public bool IsGrounded => _isGrounded;
        public bool IsTouchingWall => _isTouchingWall;
        public float WallDirection => _wallDirection;
        public float LastWallDirection => _lastWallDirection;
        public float CoyoteTimer => _coyoteTimer;
        public float WallCoyoteTimer => _wallCoyoteTimer;
        public bool CanWallJump => (_isTouchingWall || _wallCoyoteTimer > 0f) && HasAbility(AbilityFlags.WallJump);
        public float DashDuration => dashDuration;
        public Vector2 LinearVelocity => _rb.linearVelocity;
        public bool IsAttacking => (_combat != null && _combat.IsAttacking);
        public string CurrentStateName => _stateMachine?.CurrentState?.GetType().Name ?? "None";
        public float DashCooldownTimer => _dashCooldownTimer;
        public bool InteractPressed => _frame.InteractPressed;

        public event Action Jumped;
        public event Action Dashed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _squash = GetComponent<SquashAndStretch>();
            _ghostTrail = GetComponent<GhostTrail>();
            _combat = GetComponent<PlayerCombat>();
            _stats = GetComponent<PlayerStats>();
            _abilities = GetComponent<AbilitySet>();
            _anchor = GetComponent<EchoAnchor>();

            // Setup Ground layer mask: Neutral (6), PrimeSolid (7), EchoSolid (8)
            if (groundLayer.value == 0 || groundLayer.value == ~0)
            {
                int neutralLayer = LayerMask.NameToLayer("Neutral");
                int primeLayer = LayerMask.NameToLayer("PrimeSolid");
                int echoLayer = LayerMask.NameToLayer("EchoSolid");

                int mask = 0;
                if (neutralLayer != -1) mask |= (1 << neutralLayer);
                if (primeLayer != -1) mask |= (1 << primeLayer);
                if (echoLayer != -1) mask |= (1 << echoLayer);
                if (mask == 0) mask = 1; // Default layer fallback if custom layers not yet loaded
                groundLayer = mask;
            }

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
            UpdateFootsteps();
            UpdateGravityAndRail();
            _stateMachine.Update();
        }

        private void FixedUpdate()
        {
            CheckEnvironment();
            if (!_isGrounded && VerticalSpeedUp < 0f) _lastFallSpeed = Mathf.Max(_lastFallSpeed, -VerticalSpeedUp);
            _stateMachine.FixedUpdate();
        }

        private void HandleInputs()
        {
            _frame = _input.Poll();
            if (_inputLockTimer > 0f || GameFlow.IsPaused) _frame = default; // room transition / pause menu: ignore the player

            float moveX = _frame.Move;
            bool jumpPressed = _frame.JumpPressed;
            bool jumpReleased = _frame.JumpReleased;
            bool shiftPressed = _frame.ShiftPressed;
            bool attackPressed = _frame.AttackPressed;
            bool resonancePressed = _frame.ResonancePressed;

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

            // Variable Jump: cut vertical speed when jump button is released early
            if (jumpReleased && VerticalSpeedUp > 0f)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * 0.5f);
            }

            // Reality Shift
            if (shiftPressed && RealityManager.Instance != null && HasAbility(AbilityFlags.RealityShift))
            {
                RealityManager.Instance.TryToggleRealm(_collider.bounds);
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

            if (_frame.AnchorPressed && _anchor != null) _anchor.Activate();
            if (_frame.GravityPressed) TryToggleGravity();
        }

        private void UpdateTimers()
        {
            if (_coyoteTimer > 0f) _coyoteTimer -= Time.deltaTime;
            if (_wallCoyoteTimer > 0f) _wallCoyoteTimer -= Time.deltaTime;
            if (_jumpBufferTimer > 0f) _jumpBufferTimer -= Time.deltaTime;
            if (_freezeVerticalTimer > 0f) _freezeVerticalTimer -= Time.deltaTime;
            if (_wallJumpLockTimer > 0f) _wallJumpLockTimer -= Time.deltaTime;
            if (_inputLockTimer > 0f) _inputLockTimer -= Time.unscaledDeltaTime;
            if (_dashCooldownTimer > 0f)
            {
                _dashCooldownTimer -= Time.deltaTime;
                if (_dashCooldownTimer <= 0f) _canDash = true;
            }
        }

        private void CheckEnvironment()
        {
            // Ground Check
            // Use world-space bounds so the check is correct whatever the transform scale is
            Bounds bounds = _collider.bounds;
            Vector2 size = new Vector2(bounds.size.x * 0.85f, groundCheckDistance);
            // "Feet" are at the bottom normally and at the top while gravity is inverted
            float feetY = _gravitySign > 0f ? bounds.min.y + groundCheckDistance * 0.5f : bounds.max.y - groundCheckDistance * 0.5f;
            Vector2 origin = new Vector2(bounds.center.x, feetY);

            RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down * _gravitySign, groundCheckDistance, groundLayer);
            _isGrounded = hit.collider != null && hit.collider.gameObject != gameObject;

            if (_isGrounded)
            {
                _coyoteTimer = SettingsService.Current.extendedCoyoteTime ? coyoteDuration + 0.1f : coyoteDuration;
                // Spec: Reset dash immediately upon landing
                ResetDashCooldown();
                _wallCoyoteTimer = 0f;
            }

            // Wall Check
            _isTouchingWall = false;
            _wallDirection = 0f;

            if (!_isGrounded)
            {
                Bounds wallBounds = _collider.bounds;
                Vector2 wallSize = new Vector2(0.05f, wallBounds.size.y * 0.7f);
                float edgeInset = wallBounds.extents.x - wallSize.x * 0.5f;
                Vector2 wallOriginRight = new Vector2(wallBounds.center.x + edgeInset, wallBounds.center.y);
                Vector2 wallOriginLeft = new Vector2(wallBounds.center.x - edgeInset, wallBounds.center.y);

                RaycastHit2D hitRight = Physics2D.BoxCast(wallOriginRight, wallSize, 0f, Vector2.right, wallCheckDistance, groundLayer);
                if (hitRight.collider != null && hitRight.collider.gameObject != gameObject)
                {
                    _isTouchingWall = true;
                    _wallDirection = 1f;
                    _lastWallDirection = 1f;
                    _wallCoyoteTimer = SettingsService.Current.extendedCoyoteTime ? wallCoyoteDuration + 0.1f : wallCoyoteDuration;
                }
                else
                {
                    RaycastHit2D hitLeft = Physics2D.BoxCast(wallOriginLeft, wallSize, 0f, Vector2.left, wallCheckDistance, groundLayer);
                    if (hitLeft.collider != null && hitLeft.collider.gameObject != gameObject)
                    {
                        _isTouchingWall = true;
                        _wallDirection = -1f;
                        _lastWallDirection = -1f;
                        _wallCoyoteTimer = SettingsService.Current.extendedCoyoteTime ? wallCoyoteDuration + 0.1f : wallCoyoteDuration;
                    }
                }
            }
        }

        public void ChangeState(IPlayerState newState)
        {
            _stateMachine.ChangeState(newState);
        }

        /// <summary>Without an AbilitySet component everything is allowed (keeps old prefabs and tests working).</summary>
        public bool HasAbility(AbilityFlags ability) => _abilities == null || _abilities.Has(ability);

        public bool CheckAndConsumeJump()
        {
            return CheckAndConsumeJump(out _);
        }

        public bool CheckAndConsumeJump(out bool isWallJump)
        {
            isWallJump = false;
            if (_jumpBufferTimer <= 0f) return false;

            // 1. Ground Jump (including ground coyote time)
            if (_coyoteTimer > 0f)
            {
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                return true;
            }

            // 2. Wall Jump (including wall coyote time)
            if (CanWallJump)
            {
                isWallJump = true;
                _jumpBufferTimer = 0f;
                _wallCoyoteTimer = 0f;
                return true;
            }

            return false;
        }

        public bool CheckAndConsumeDash()
        {
            bool dashInput = _frame.DashPressed;
            if (dashInput && _canDash && _dashCooldownTimer <= 0f && HasAbility(AbilityFlags.PhaseDash))
            {
                _canDash = false;
                _dashCooldownTimer = dashCooldown;
                return true;
            }
            return false;
        }

        public void ResetDashCooldown()
        {
            _dashCooldownTimer = 0f;
            _canDash = true;
        }

        public bool CheckAndConsumeAttack()
        {
            return _combat != null && _combat.IsAttacking;
        }

        public void ApplyHorizontalMovement()
        {
            if (_wallJumpLockTimer > 0f) return; // keep the wall-jump kick

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
            float g = (isFalling || vel.y * _gravitySign < 0f) ? _fallGravity : _baseGravity;
            vel.y -= _gravitySign * g * Time.fixedDeltaTime;
            _rb.linearVelocity = vel;
        }

        public void ExecuteJump()
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpVelocity * _gravitySign);
            if (_squash != null) _squash.OnJump();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
            Jumped?.Invoke();
        }

        public void ExecuteWallJump()
        {
            float wallDir = _wallDirection != 0f ? _wallDirection : _lastWallDirection;
            if (wallDir == 0f) wallDir = -_facingDirection;

            float launchX = -wallDir * wallJumpHorizontalSpeed;
            _rb.linearVelocity = new Vector2(launchX, wallJumpVerticalSpeed * _gravitySign);
            _facingDirection = -wallDir;
            _wallJumpLockTimer = wallJumpInputLock;
            _wallCoyoteTimer = 0f;
            if (_renderer != null) _renderer.flipX = (_facingDirection < 0f);
            if (_squash != null) _squash.OnJump();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
            Jumped?.Invoke();
        }

        public void StartDash()
        {
            if (_stats != null) _stats.SetInvulnerable(true);
            _rb.linearVelocity = new Vector2(_facingDirection * dashSpeed, 0f);
            if (_squash != null) _squash.OnDash();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
            VfxLibrary.Play(VfxId.DashDust, FeetPosition, flipX: _facingDirection < 0f);
            Dashed?.Invoke();

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

            // Dash speed (20) exceeds run speed (12): drop back to run speed so the dash covers exactly its 4 tiles
            Vector2 vel = _rb.linearVelocity;
            _rb.linearVelocity = new Vector2(Mathf.Clamp(vel.x, -maxSpeed, maxSpeed), vel.y);
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

        private void UpdateFootsteps()
        {
            float speed = Mathf.Abs(_rb.linearVelocity.x);
            if (!_isGrounded || speed < 1.5f || GameFlow.IsPaused)
            {
                _footstepTimer = 0.05f; // first step lands soon after starting to run
                return;
            }

            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer > 0f) return;

            _footstepTimer = Mathf.Lerp(0.42f, 0.26f, Mathf.Clamp01(speed / maxSpeed));
            bool echo = RealityManager.Instance != null && RealityManager.Instance.CurrentRealm == RealmType.Echo;
            AudioManager.Play(echo ? SfxGroup.FootstepEcho : SfxGroup.FootstepPrime, 0.5f);
        }

        public void TriggerSquashLand()
        {
            if (_squash != null) _squash.OnLand();
            AudioManager.Play(_lastFallSpeed > 24f ? SfxGroup.LandHard : SfxGroup.LandSoft, 0.8f);
            _lastFallSpeed = 0f;
            VfxLibrary.Play(VfxId.LandDust, FeetPosition);
        }

        private Vector3 FeetPosition
        {
            get
            {
                Bounds b = _collider.bounds;
                return new Vector3(b.center.x, b.min.y, 0f);
            }
        }

        /// <summary>Ignore the player's input for <paramref name="seconds"/> (room transitions, cutscenes).</summary>
        public void LockInput(float seconds)
        {
            _inputLockTimer = Mathf.Max(_inputLockTimer, seconds);
        }

        public bool IsInputLocked => _inputLockTimer > 0f;

        public bool IsGravityInverted => _gravitySign < 0f;
        /// <summary>+1 normally, -1 while gravity is inverted. "Up" for Kael is world up times this.</summary>
        public float UpSign => _gravitySign;
        /// <summary>Vertical speed measured along Kael's own "up": positive = away from his feet.</summary>
        public float VerticalSpeedUp => _rb.linearVelocity.y * _gravitySign;
        public float HalfHeight => _collider.bounds.extents.y;
        public bool InGravitonField => _gravitonFields > 0;
        public RailCable RailInReach => (_railInReach != null && _railInReach.IsActive && _railCooldown <= 0f) ? _railInReach : null;


        /// <summary>Replace the input source (tests, cutscenes, replays).</summary>
        public void SetInput(IPlayerInput input)
        {
            _input = input ?? new DevicePlayerInput();
        }

        public void SetGroundLayer(LayerMask mask)
        {
            groundLayer = mask;
        }

        public void ResetForRespawn()
        {
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _freezeVerticalTimer = 0f;
            SetGravity(1f);
            _gravityRevertTimer = -1f;
            _railInReach = null;
            _railCooldown = 0f;
            _anchor?.Clear();
            _rb.linearVelocity = Vector2.zero;
            ResetDashCooldown();
            _stateMachine.ChangeState(new PlayerIdleState());
        }

        // ------------------------------------------------------------------ Gravity Inversion (spec 4)
        public void EnterGravitonField()
        {
            _gravitonFields++;
            _gravityRevertTimer = -1f;
        }

        public void LeaveGravitonField()
        {
            _gravitonFields = Mathf.Max(0, _gravitonFields - 1);
            if (_gravitonFields == 0 && IsGravityInverted)
            {
                _gravityRevertTimer = GravityRevertDelay;
                if (PlayerHUD.Instance != null) PlayerHUD.Instance.ShowAnnouncement("GRAVITY RESTORING", GravityRevertDelay);
            }
        }

        /// <returns>true if gravity was flipped.</returns>
        public bool TryToggleGravity()
        {
            if (_gravitonFields <= 0 || !HasAbility(AbilityFlags.GravityInversion)) return false;

            SetGravity(-_gravitySign);
            _gravityRevertTimer = -1f;
            return true;
        }

        private void SetGravity(float sign)
        {
            if (Mathf.Approximately(_gravitySign, sign)) return;

            _gravitySign = sign;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
            _coyoteTimer = 0f;
            _isGrounded = false;
            // Flip the picture, not the physics object: rotation leaves scale (and squash & stretch) alone
            if (_renderer != null) _renderer.transform.localRotation = sign < 0f ? Quaternion.Euler(180f, 0f, 0f) : Quaternion.identity;
        }

        private void UpdateGravityAndRail()
        {
            if (_gravityRevertTimer >= 0f)
            {
                _gravityRevertTimer -= Time.deltaTime;
                if (_gravityRevertTimer < 0f) { SetGravity(1f); _gravityRevertTimer = -1f; }
            }
            if (_railCooldown > 0f) _railCooldown -= Time.deltaTime;
        }

        // ------------------------------------------------------------------ Rail Grind (spec 3.1)
        private void OnTriggerEnter2D(Collider2D other) => TrackRail(other, true);
        private void OnTriggerStay2D(Collider2D other) { if (_railInReach == null) TrackRail(other, true); }
        private void OnTriggerExit2D(Collider2D other) => TrackRail(other, false);

        private void TrackRail(Collider2D other, bool entering)
        {
            var rail = other.GetComponent<RailCable>();
            if (rail == null) return;
            if (entering) _railInReach = rail;
            else if (_railInReach == rail) _railInReach = null;
        }

        /// <summary>Called by the airborne states: latch onto a cable Kael is touching (not while rising fast).</summary>
        public bool TryStartRailGrind()
        {
            var rail = RailInReach;
            if (rail == null || VerticalSpeedUp > 3f) return false;

            ChangeState(new PlayerRailGrindState(rail));
            return true;
        }

        public void ReleaseRail(float cooldownSeconds)
        {
            _railCooldown = cooldownSeconds;
        }

        /// <summary>A jump is buffered (no ground needed): used to jump off a cable.</summary>
        public bool ConsumeBufferedJump()
        {
            if (_jumpBufferTimer <= 0f) return false;
            _jumpBufferTimer = 0f;
            return true;
        }
    }
}
