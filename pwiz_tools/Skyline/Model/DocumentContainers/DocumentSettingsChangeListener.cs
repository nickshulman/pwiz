using System;

namespace pwiz.Skyline.Model.DocumentContainers
{
    public interface IDocumentSettingsListener
    {
        void DocumentSettingsChanged();
    }

    public sealed class DocumentSettingsListener : IDocumentSettingsListener
    {
        private object _key;
        private Action _action;

        public DocumentSettingsListener(Action action) : this(action, action)
        {
        }
        public DocumentSettingsListener(object key, Action action)
        {
            _key = key;
            _action = action;
        }

        public void DocumentSettingsChanged()
        {
            _action();
        }

        private bool Equals(DocumentSettingsListener other)
        {
            return _key.Equals(other._key);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is DocumentSettingsListener other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _key.GetHashCode();
        }
    }
}
