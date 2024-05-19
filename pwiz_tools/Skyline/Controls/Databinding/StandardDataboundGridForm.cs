using pwiz.Skyline.Model;
using System;
using pwiz.Skyline.Model.Databinding;

namespace pwiz.Skyline.Controls.Databinding
{
    public class StandardDataboundGridForm : DataboundGridForm
    {
        public StandardDataboundGridForm(SkylineWindow skylineWindow)
        {
            SkylineWindow = skylineWindow;
            DataSchema = SkylineWindowDataSchema.FromDocumentContainer(skylineWindow);
        }

        private StandardDataboundGridForm()
        {

        }

        public SkylineWindow SkylineWindow { get; }
        protected SkylineDataSchema DataSchema { get; }

        

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (SkylineWindow != null)
            {
                SkylineWindow.DocumentUIChangedEvent += SkylineWindow_OnDocumentUIChangedEvent;
            }
            OnDocumentChanged();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (SkylineWindow != null)
            {
                SkylineWindow.DocumentUIChangedEvent -= SkylineWindow_OnDocumentUIChangedEvent;
            }
            base.OnHandleDestroyed(e);
        }

        private void SkylineWindow_OnDocumentUIChangedEvent(object sender, DocumentChangedEventArgs e)
        {
            OnDocumentChanged();
        }

        protected bool Updating { get; private set; }

        protected void IfNotUpdating(Action action)
        {
            if (Updating)
            {
                return;
            }

            try
            {
                Updating = true;
                action();
            }
            finally
            {
                Updating = false;
            }
        }

        protected virtual void OnDocumentChanged()
        {
            IfNotUpdating(UpdateUi);
        }

        protected virtual void UpdateUi()
        {
        }

    }
}
