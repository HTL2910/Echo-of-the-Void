using UnityEngine;

namespace EchoOfTheVoid.Player.States
{
    public class PlayerIdleState : IPlayerState
    {
        public void Enter(PlayerController player)
        {
            player.SetHorizontalVelocity(0f);
        }

        public void Update(PlayerController player)
        {
            if (player.CheckAndConsumeJump())
            {
                player.ChangeState(new PlayerJumpState());
                return;
            }

            if (player.CheckAndConsumeDash())
            {
                player.ChangeState(new PlayerDashState());
                return;
            }

            if (player.CheckAndConsumeAttack())
            {
                player.ChangeState(new PlayerAttackState());
                return;
            }

            if (Mathf.Abs(player.HorizontalInput) > 0.05f)
            {
                player.ChangeState(new PlayerRunState());
                return;
            }

            // Idle/Run apply no gravity, so leaving the ground must hand over to Fall at once
            // (waiting for vy < 0 would leave Kael hovering forever)
            if (!player.IsGrounded)
            {
                player.ChangeState(new PlayerFallState());
            }
        }

        public void FixedUpdate(PlayerController player)
        {
            player.ApplyDeceleration();
        }

        public void Exit(PlayerController player) { }
    }

    public class PlayerRunState : IPlayerState
    {
        public void Enter(PlayerController player) { }

        public void Update(PlayerController player)
        {
            if (player.CheckAndConsumeJump())
            {
                player.ChangeState(new PlayerJumpState());
                return;
            }

            if (player.CheckAndConsumeDash())
            {
                player.ChangeState(new PlayerDashState());
                return;
            }

            if (player.CheckAndConsumeAttack())
            {
                player.ChangeState(new PlayerAttackState());
                return;
            }

            if (Mathf.Abs(player.HorizontalInput) < 0.05f)
            {
                player.ChangeState(new PlayerIdleState());
                return;
            }

            if (!player.IsGrounded)
            {
                player.ChangeState(new PlayerFallState());
            }
        }

        public void FixedUpdate(PlayerController player)
        {
            player.ApplyHorizontalMovement();
        }

        public void Exit(PlayerController player) { }
    }

    public class PlayerJumpState : IPlayerState
    {
        private readonly bool _fromWallJump;

        public PlayerJumpState(bool fromWallJump = false)
        {
            _fromWallJump = fromWallJump;
        }

        public void Enter(PlayerController player)
        {
            if (!_fromWallJump)
            {
                player.ExecuteJump();
            }
        }

        public void Update(PlayerController player)
        {
            if (player.CheckAndConsumeDash())
            {
                player.ChangeState(new PlayerDashState());
                return;
            }

            if (player.CheckAndConsumeAttack())
            {
                player.ChangeState(new PlayerAttackState());
                return;
            }

            // Direct wall jump if pressing jump while touching a wall
            if (player.CheckAndConsumeJump(out bool isWallJump))
            {
                if (isWallJump)
                {
                    player.ExecuteWallJump();
                    player.ChangeState(new PlayerJumpState(fromWallJump: true));
                    return;
                }
            }

            // Only transition to wall slide if falling/descending, NOT while rising from a jump!
            if (player.IsTouchingWall && !player.IsGrounded && player.VerticalSpeedUp <= 0.1f)
            {
                bool holdingAway = player.HorizontalInput != 0f && Mathf.Sign(player.HorizontalInput) == -player.WallDirection;
                if (!holdingAway)
                {
                    player.ChangeState(new PlayerWallSlideState());
                    return;
                }
            }

            if (player.TryStartRailGrind()) return;

            if (player.VerticalSpeedUp <= 0f)
            {
                player.ChangeState(new PlayerFallState());
            }
        }

        public void FixedUpdate(PlayerController player)
        {
            player.ApplyHorizontalMovement();
            player.ApplyCustomGravity(isFalling: false);
        }

        public void Exit(PlayerController player) { }
    }

    public class PlayerFallState : IPlayerState
    {
        public void Enter(PlayerController player) { }

        public void Update(PlayerController player)
        {
            if (player.CheckAndConsumeJump(out bool isWallJump))
            {
                if (isWallJump)
                {
                    player.ExecuteWallJump();
                    player.ChangeState(new PlayerJumpState(fromWallJump: true));
                }
                else
                {
                    player.ChangeState(new PlayerJumpState(fromWallJump: false));
                }
                return;
            }

            if (player.CheckAndConsumeDash())
            {
                player.ChangeState(new PlayerDashState());
                return;
            }

            if (player.CheckAndConsumeAttack())
            {
                player.ChangeState(new PlayerAttackState());
                return;
            }

            if (player.TryStartRailGrind()) return;

            // Only enter wall slide if falling and NOT holding away from the wall
            if (player.IsTouchingWall && player.VerticalSpeedUp <= 0.1f)
            {
                bool holdingAway = player.HorizontalInput != 0f && Mathf.Sign(player.HorizontalInput) == -player.WallDirection;
                if (!holdingAway)
                {
                    player.ChangeState(new PlayerWallSlideState());
                    return;
                }
            }

            if (player.IsGrounded)
            {
                player.TriggerSquashLand();
                if (Mathf.Abs(player.HorizontalInput) > 0.05f)
                {
                    player.ChangeState(new PlayerRunState());
                }
                else
                {
                    player.ChangeState(new PlayerIdleState());
                }
            }
        }

        public void FixedUpdate(PlayerController player)
        {
            player.ApplyHorizontalMovement();
            player.ApplyCustomGravity(isFalling: true);
        }

        public void Exit(PlayerController player) { }
    }

    public class PlayerWallSlideState : IPlayerState
    {
        private const float WALL_SLIDE_SPEED = -2.5f;
        private const float DETACH_HOLD_TIME = 0.08f;
        private float _detachTimer;

        public void Enter(PlayerController player)
        {
            player.ResetVerticalVelocity();
            _detachTimer = 0f;
        }

        public void Update(PlayerController player)
        {
            if (player.CheckAndConsumeJump(out _))
            {
                player.ExecuteWallJump();
                player.ChangeState(new PlayerJumpState(fromWallJump: true));
                return;
            }

            if (player.CheckAndConsumeDash())
            {
                player.ChangeState(new PlayerDashState());
                return;
            }

            if (player.IsGrounded)
            {
                player.ChangeState(new PlayerIdleState());
                return;
            }

            if (!player.IsTouchingWall)
            {
                player.ChangeState(new PlayerFallState());
                return;
            }

            // Smooth detachment when holding away from the wall
            if (player.HorizontalInput != 0f && Mathf.Sign(player.HorizontalInput) == -player.WallDirection)
            {
                _detachTimer += Time.deltaTime;
                if (_detachTimer >= DETACH_HOLD_TIME)
                {
                    // Gently push away so the collider doesn't immediately re-stick
                    player.SetHorizontalVelocity(player.HorizontalInput * 2f);
                    player.ChangeState(new PlayerFallState());
                    return;
                }
            }
            else
            {
                _detachTimer = 0f;
            }
        }

        public void FixedUpdate(PlayerController player)
        {
            // Slide down with wall friction
            Vector2 vel = player.LinearVelocity;
            vel.y = Mathf.Max(vel.y * player.UpSign, WALL_SLIDE_SPEED) * player.UpSign;
            vel.x = 0f;
            player.SetVelocity(vel);
        }

        public void Exit(PlayerController player) { }
    }

    public class PlayerDashState : IPlayerState
    {
        private float _timer;

        public void Enter(PlayerController player)
        {
            _timer = 0f;
            player.StartDash();
        }

        public void Update(PlayerController player)
        {
            _timer += Time.deltaTime;
            if (_timer >= player.DashDuration)
            {
                player.EndDash();
                if (player.IsGrounded)
                {
                    player.ChangeState(new PlayerIdleState());
                }
                else
                {
                    player.ChangeState(new PlayerFallState());
                }
            }
        }

        public void FixedUpdate(PlayerController player)
        {
            player.MaintainDashVelocity();
        }

        public void Exit(PlayerController player)
        {
            player.EndDash();
        }
    }

    public class PlayerAttackState : IPlayerState
    {
        public void Enter(PlayerController player) { }

        public void Update(PlayerController player)
        {
            if (!player.IsAttacking)
            {
                if (player.IsGrounded)
                {
                    player.ChangeState((Mathf.Abs(player.HorizontalInput) > 0.05f) 
                        ? new PlayerRunState() 
                        : new PlayerIdleState());
                }
                else
                {
                    player.ChangeState(new PlayerFallState());
                }
            }
        }

        public void FixedUpdate(PlayerController player)
        {
            if (player.IsGrounded)
            {
                player.ApplyDeceleration();
            }
            else
            {
                player.ApplyCustomGravity(isFalling: player.VerticalSpeedUp < 0f);
            }
        }

        public void Exit(PlayerController player) { }
    }
}
