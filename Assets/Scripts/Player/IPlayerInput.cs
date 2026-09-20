namespace EchoOfTheVoid.Player
{
    /// <summary>One frame of player intent, independent of the device it came from.</summary>
    public struct PlayerInputFrame
    {
        public float Move;            // -1..1
        public bool JumpPressed;
        public bool JumpReleased;
        public bool DashPressed;
        public bool ShiftPressed;     // Reality Shift
        public bool AttackPressed;
        public bool ResonancePressed;
    }

    /// <summary>
    /// Source of player input. The real game uses <see cref="DevicePlayerInput"/>;
    /// tests and cutscenes can supply their own implementation.
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>Called once per frame. "Pressed"/"Released" flags are edge events for that frame.</summary>
        PlayerInputFrame Poll();
    }
}
