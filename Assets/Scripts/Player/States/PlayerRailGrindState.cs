using UnityEngine;
using EchoOfTheVoid.Enemies;

namespace EchoOfTheVoid.Player.States
{
    /// <summary>
    /// Rail Grind (spec 3.1): while a Prism Sentry's beam is a cable (Echo realm), Kael can ride along it. Gravity is off,
    /// he is held on top of the line and carried at least run speed in the direction he was already moving.
    /// Jump kicks off, Dash cancels, and reaching either end (or the cable turning back into a laser) drops him.
    /// </summary>
    public class PlayerRailGrindState : IPlayerState
    {
        private const float MinSpeed = 10f;
        private const float SnapGain = 18f;
        private const float RegrabCooldown = 0.4f;

        private readonly RailCable _rail;
        private Vector2 _direction;
        private float _speed;

        public PlayerRailGrindState(RailCable rail)
        {
            _rail = rail;
        }

        public void Enter(PlayerController player)
        {
            Vector2 velocity = player.LinearVelocity;
            _direction = _rail.Direction;
            float heading = Mathf.Abs(velocity.x) > 0.5f ? velocity.x : player.FacingDirection;
            if (Vector2.Dot(_direction, new Vector2(heading, 0f)) < 0f) _direction = -_direction;

            _speed = Mathf.Max(Mathf.Abs(velocity.x), MinSpeed);
            player.SetVelocity(_direction * _speed);
        }

        public void Update(PlayerController player)
        {
            if (_rail == null || !_rail.IsActive)
            {
                Leave(player, 0.1f);
                return;
            }

            if (player.ConsumeBufferedJump())
            {
                player.ReleaseRail(RegrabCooldown);
                player.ChangeState(new PlayerJumpState());
                return;
            }

            if (player.CheckAndConsumeDash())
            {
                player.ReleaseRail(RegrabCooldown);
                player.ChangeState(new PlayerDashState());
            }
        }

        public void FixedUpdate(PlayerController player)
        {
            if (_rail == null || !_rail.IsActive) return;

            Vector2 start = _rail.StartPoint;
            Vector2 axis = _rail.Direction;
            Vector2 position = player.transform.position;

            float along = Vector2.Dot(position - start, axis);
            bool pastEnd = Vector2.Dot(_direction, axis) > 0f ? along >= _rail.Length : along <= 0f;
            if (pastEnd)
            {
                Leave(player, RegrabCooldown);
                return;
            }

            // Stand on top of the line: the normal that points away from Kael's feet
            Vector2 normal = new Vector2(-axis.y, axis.x);
            if (normal.y * player.UpSign < 0f) normal = -normal;
            Vector2 target = start + axis * along + normal * player.HalfHeight;

            player.SetVelocity(_direction * _speed + (target - position) * SnapGain);
        }

        public void Exit(PlayerController player) { }

        private void Leave(PlayerController player, float regrabCooldown)
        {
            player.ReleaseRail(regrabCooldown);
            player.SetVelocity(_direction * _speed);
            player.ChangeState(new PlayerFallState());
        }
    }
}
