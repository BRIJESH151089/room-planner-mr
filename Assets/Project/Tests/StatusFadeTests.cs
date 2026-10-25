using System;
using NUnit.Framework;
using RoomPlanner.Core;

namespace RoomPlanner.Tests
{
    public class StatusFadeTests
    {
        [Test]
        public void Sticky_ByDefault_NeverFades()
        {
            var fade = new StatusFade(4f, 0.5f);

            Assert.AreEqual(1f, fade.Alpha(1000f));
        }

        [Test]
        public void Shown_StaysFullyVisibleDuringHold()
        {
            var fade = new StatusFade(4f, 0.5f);
            fade.Show(10f);

            Assert.AreEqual(1f, fade.Alpha(10f));
            Assert.AreEqual(1f, fade.Alpha(14f));
        }

        [Test]
        public void Shown_FadesLinearlyAfterHold_ThenIsGone()
        {
            var fade = new StatusFade(4f, 0.5f);
            fade.Show(10f);

            Assert.AreEqual(0.5f, fade.Alpha(14.25f), 1e-4f);
            Assert.AreEqual(0f, fade.Alpha(14.5f));
            Assert.AreEqual(0f, fade.Alpha(100f));
        }

        [Test]
        public void ShowAgain_RestartsTheHold()
        {
            var fade = new StatusFade(4f, 0.5f);
            fade.Show(0f);
            fade.Show(10f);

            Assert.AreEqual(1f, fade.Alpha(13f));
        }

        [Test]
        public void ShowSticky_AfterFading_BringsMessageBackForGood()
        {
            var fade = new StatusFade(4f, 0.5f);
            fade.Show(0f);
            fade.ShowSticky();

            Assert.AreEqual(1f, fade.Alpha(60f));
        }

        [Test]
        public void ZeroFade_DisappearsRightAfterHold()
        {
            var fade = new StatusFade(4f, 0f);
            fade.Show(0f);

            Assert.AreEqual(1f, fade.Alpha(4f));
            Assert.AreEqual(0f, fade.Alpha(4.01f));
        }

        [Test]
        public void NegativeTimes_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatusFade(-1f, 0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatusFade(4f, -0.5f));
        }

        [Test]
        public void Content_HintOutlivesTheFadedStatus()
        {
            Assert.AreEqual(StatusPanelContent.StatusAndHint, StatusFade.Content(true, true));
            Assert.AreEqual(StatusPanelContent.StatusOnly, StatusFade.Content(true, false));
            Assert.AreEqual(StatusPanelContent.HintOnly, StatusFade.Content(false, true));
            Assert.AreEqual(StatusPanelContent.Hidden, StatusFade.Content(false, false));
        }
    }
}
