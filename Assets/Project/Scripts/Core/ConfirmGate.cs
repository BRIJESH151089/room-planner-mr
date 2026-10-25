namespace RoomPlanner.Core
{
    // Two-press confirmation for destructive actions such as Clear All: the first press arms the gate, a second press
    // within WindowSeconds confirms. Times are passed in (seconds), so the logic is testable without a clock.
    public sealed class ConfirmGate
    {
        public const float WindowSeconds = 3f;

        private bool armed;
        private float armedUntil;

        // True when this press confirms; false when it only arms (or re-arms after the window ran out).
        public bool Press(float now)
        {
            if (IsArmed(now))
            {
                armed = false;
                return true;
            }

            armed = true;
            armedUntil = now + WindowSeconds;
            return false;
        }

        public bool IsArmed(float now) => armed && now <= armedUntil;

        public void Reset()
        {
            armed = false;
        }
    }
}
