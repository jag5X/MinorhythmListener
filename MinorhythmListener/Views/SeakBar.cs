using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MinorhythmListener.Views
{
    public class SeakBar : Slider
    {
        public event DragStartedEventHandler SeakStarted;
        public event DragDeltaEventHandler SeakDelta;
        public event DragCompletedEventHandler SeakCompleted;

        protected override void OnThumbDragStarted(DragStartedEventArgs e)
        {
            base.OnThumbDragStarted(e);
            if (SeakStarted != null) SeakStarted(this, e);
        }

        protected override void OnThumbDragDelta(DragDeltaEventArgs e)
        {
            base.OnThumbDragDelta(e);
            if (SeakDelta != null) SeakDelta(this, e);
        }

        protected override void OnThumbDragCompleted(DragCompletedEventArgs e)
        {
            base.OnThumbDragCompleted(e);
            if (SeakCompleted != null) SeakCompleted(this, e);
        }
    }
}
