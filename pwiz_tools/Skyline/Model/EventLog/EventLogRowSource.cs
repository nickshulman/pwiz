using System.Collections;
using System.Linq;
using pwiz.Common.DataBinding;

namespace pwiz.Skyline.Model.EventLog
{
    public class EventLogRowSource : AbstractRowSource
    {
        protected override void AfterLastListenerRemoved()
        {
            base.AfterLastListenerRemoved();
            SkylineEventLog.NewEventsAvailable -= EventLog_OnNewEventsAvailable;
        }

        protected override void BeforeFirstListenerAdded()
        {
            base.BeforeFirstListenerAdded();
            SkylineEventLog.NewEventsAvailable += EventLog_OnNewEventsAvailable;
        }

        private void EventLog_OnNewEventsAvailable()
        {
            FireListChanged();
        }

        public override IEnumerable GetItems()
        {
            return SkylineEventLog.GetLogEvents().Select(logEvent => new EventLogRow(logEvent));
        }
    }
}
