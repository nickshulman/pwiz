using System;

namespace pwiz.Skyline.Model.Databinding
{
    internal class ActionDocumentChangeListener : IDocumentChangeListener
    {
        private readonly Action _action;
        public ActionDocumentChangeListener(Action action)
        {
            _action = action;
        }
        public void DocumentOnChanged(object sender, DocumentChangedEventArgs args)
        {
            _action();
        }

        protected bool Equals(ActionDocumentChangeListener other)
        {
            return _action.Equals(other._action);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ActionDocumentChangeListener) obj);
        }

        public override int GetHashCode()
        {
            return _action.GetHashCode();
        }
    }
}