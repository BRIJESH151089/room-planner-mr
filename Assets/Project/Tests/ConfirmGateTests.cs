using NUnit.Framework;
using RoomPlanner.Core;

namespace RoomPlanner.Tests
{
    public class ConfirmGateTests
    {
        [Test]
        public void FirstPress_ArmsButDoesNotConfirm()
        {
            var gate = new ConfirmGate();

            Assert.IsFalse(gate.Press(10f));
            Assert.IsTrue(gate.IsArmed(10f));
        }

        [Test]
        public void SecondPressWithinThreeSeconds_Confirms()
        {
            var gate = new ConfirmGate();
            gate.Press(10f);

            Assert.IsTrue(gate.Press(12.9f));
            Assert.IsFalse(gate.IsArmed(12.9f), "Confirming disarms the gate.");
        }

        [Test]
        public void SecondPressAfterThreeSeconds_OnlyReArms()
        {
            var gate = new ConfirmGate();
            gate.Press(10f);

            Assert.IsFalse(gate.IsArmed(13.5f));
            Assert.IsFalse(gate.Press(13.5f));
            Assert.IsTrue(gate.Press(14f));
        }

        [Test]
        public void PressAfterConfirm_StartsOver()
        {
            var gate = new ConfirmGate();
            gate.Press(1f);
            gate.Press(2f);

            Assert.IsFalse(gate.Press(2.5f));
        }

        [Test]
        public void Reset_Disarms()
        {
            var gate = new ConfirmGate();
            gate.Press(1f);
            gate.Reset();

            Assert.IsFalse(gate.IsArmed(1.5f));
            Assert.IsFalse(gate.Press(1.5f));
        }
    }
}
