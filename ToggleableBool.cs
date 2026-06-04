namespace Sackrany.GameInput.SackranyInput
{
    public class ToggleableBool
    {
        bool _held;
        bool _toggled;

        public bool IsHold { get; set; } = true;

        public bool Value => IsHold ? _held : _toggled;
        public bool JustPressed { get; private set; }

        public void OnStarted()
        {
            JustPressed = true;

            if (IsHold)
                _held = true;
            else
                _toggled = !_toggled;
        }

        public void OnCanceled()
        {
            if (IsHold)
                _held = false;
        }

        public void ResetOneShot()
        {
            JustPressed = false;
        }
    }
}
