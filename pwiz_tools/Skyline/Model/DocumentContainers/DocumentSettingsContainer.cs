using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.DataBinding;

namespace pwiz.Skyline.Model.DocumentContainers
{
    public class DocumentSettingsContainer
    {
        public DocumentSettingsContainer(QueryLock queryLock, DataSchemaLocalizer dataSchemaLocalizer)
        {
            QueryLock = queryLock;
            DataSchemaLocalizer = dataSchemaLocalizer;
        }

        public QueryLock QueryLock { get; private set; }
        public DataSchemaLocalizer DataSchemaLocalizer { get; private set; }
        private HashSet<Action> _documentSettingsChangedHandlers = new HashSet<Action>();
        public DocumentSettings DocumentSettings { get; protected set; }
        public void Listen(Action action)
        {
            lock (_documentSettingsChangedHandlers)
            {
                if (_documentSettingsChangedHandlers.Count == 0)
                {
                    BeforeFirstListenerAdded();
                }

                if (!_documentSettingsChangedHandlers.Add(action))
                {
                    throw new ArgumentException();
                }
            }
        }
        public void Unlisten(Action action)
        {
            lock (_documentSettingsChangedHandlers)
            {
                if (!_documentSettingsChangedHandlers.Remove(action))
                {
                    throw new ArgumentException();
                }

                if (_documentSettingsChangedHandlers.Count == 0)
                {
                    throw new ArgumentException();
                }
            }
        }

        protected void FireOnChanged()
        {
            Action[] listeners;
            lock (_documentSettingsChangedHandlers)
            {
                listeners = _documentSettingsChangedHandlers.ToArray();
            }

            foreach (var listener in listeners)
            {
                listener();
            }
        }

        protected void BeforeFirstListenerAdded()
        {

        }

        protected void AfterLastListenerRemoved()
        {

        }
    }
}
