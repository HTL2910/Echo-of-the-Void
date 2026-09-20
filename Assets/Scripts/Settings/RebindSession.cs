using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EchoOfTheVoid.Settings
{
    /// <summary>
    /// One "press a key" rebind interaction (spec 9.5). The UI starts it, feeds it the next pressed key/button and it
    /// writes the result into the live bindings and the settings file. Escape cancels.
    /// </summary>
    public class RebindSession
    {
        public bool IsListening { get; private set; }
        public bool ForGamepad { get; private set; }
        public PlayerAction Action { get; private set; }
        public int Slot { get; private set; }

        /// <summary>Raised when a rebind was applied or cancelled, so the UI can refresh its labels.</summary>
        public event Action Finished;

        public void BeginKey(PlayerAction action, int slot)
        {
            Action = action;
            Slot = slot;
            ForGamepad = false;
            IsListening = true;
        }

        public void BeginPad(PlayerAction action)
        {
            Action = action;
            Slot = 0;
            ForGamepad = true;
            IsListening = true;
        }

        public void Cancel()
        {
            if (!IsListening) return;
            IsListening = false;
            Finished?.Invoke();
        }

        /// <returns>true if the key was applied.</returns>
        public bool CommitKey(Key key)
        {
            if (!IsListening || ForGamepad) return false;
            if (key == Key.Escape) { Cancel(); return false; }
            if (key == Key.None) return false;

            InputBindings.Current.SetKey(Action, Slot, key);
            Persist();
            return true;
        }

        public bool CommitPad(GamepadButton button)
        {
            if (!IsListening || !ForGamepad) return false;

            InputBindings.Current.SetPadButton(Action, button);
            Persist();
            return true;
        }

        private void Persist()
        {
            InputBindings.Current.WriteTo(SettingsService.Current);
            SettingsService.Apply();
            IsListening = false;
            Finished?.Invoke();
        }
    }
}
