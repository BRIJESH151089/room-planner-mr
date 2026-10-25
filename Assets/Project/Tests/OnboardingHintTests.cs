using NUnit.Framework;
using RoomPlanner.Core;

namespace RoomPlanner.Tests
{
    public class OnboardingHintTests
    {
        [Test]
        public void Hidden_BeforeTheRoomLoads()
        {
            var hint = new OnboardingHint();
            hint.OnItemCountChanged(0);

            Assert.IsFalse(hint.IsVisible);
        }

        [Test]
        public void Shown_WhenRoomLoadsEmpty()
        {
            var hint = new OnboardingHint();
            hint.OnRoomReady(0);

            Assert.IsTrue(hint.IsVisible);
        }

        [Test]
        public void Hidden_AfterTheFirstPlacement_AndStaysHidden()
        {
            var hint = new OnboardingHint();
            hint.OnRoomReady(0);
            hint.OnItemCountChanged(1);
            hint.OnItemCountChanged(0);

            Assert.IsFalse(hint.IsVisible);
        }

        [Test]
        public void Hidden_WhenASavedLayoutIsRestored()
        {
            var hint = new OnboardingHint();
            hint.OnRoomReady(0);
            hint.OnItemCountChanged(3); // auto-load placed the saved layout

            Assert.IsFalse(hint.IsVisible);
        }

        [Test]
        public void Text_MentionsPanelTriggerAndFloor()
        {
            StringAssert.Contains("trigger", OnboardingHint.Text);
            StringAssert.Contains("floor", OnboardingHint.Text);
        }
    }
}
