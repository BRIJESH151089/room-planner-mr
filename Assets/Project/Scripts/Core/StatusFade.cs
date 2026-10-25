using System;

namespace RoomPlanner.Core
{
    // What the room status panel shows: the status line, the first-use hint, both, or nothing.
    public enum StatusPanelContent
    {
        Hidden,
        StatusOnly,
        HintOnly,
        StatusAndHint
    }

    // Timing for a status message that goes away on its own: fully visible for holdSeconds after Show, then fading
    // out linearly over fadeSeconds. ShowSticky keeps the message until the next Show (loading, errors).
    public sealed class StatusFade
    {
        public const float DefaultHoldSeconds = 4f;
        public const float DefaultFadeSeconds = 0.5f;

        private readonly float holdSeconds;
        private readonly float fadeSeconds;
        private float shownAt;
        private bool sticky = true;

        public StatusFade(float holdSeconds = DefaultHoldSeconds, float fadeSeconds = DefaultFadeSeconds)
        {
            if (holdSeconds < 0f || fadeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(holdSeconds), "Hold and fade times cannot be negative.");
            }

            this.holdSeconds = holdSeconds;
            this.fadeSeconds = fadeSeconds;
        }

        public void Show(float now)
        {
            sticky = false;
            shownAt = now;
        }

        public void ShowSticky()
        {
            sticky = true;
        }

        // 1 while held, falling to 0 during the fade, 0 afterwards.
        public float Alpha(float now)
        {
            if (sticky)
            {
                return 1f;
            }

            float intoFade = now - shownAt - holdSeconds;
            if (intoFade <= 0f)
            {
                return 1f;
            }

            return intoFade >= fadeSeconds ? 0f : 1f - intoFade / fadeSeconds;
        }

        public static StatusPanelContent Content(bool statusVisible, bool hintVisible)
        {
            if (statusVisible)
            {
                return hintVisible ? StatusPanelContent.StatusAndHint : StatusPanelContent.StatusOnly;
            }

            return hintVisible ? StatusPanelContent.HintOnly : StatusPanelContent.Hidden;
        }
    }
}
