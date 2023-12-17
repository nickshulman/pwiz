using System.Reflection;
using pwiz.Skyline.Model;

namespace pwiz.Skyline.Controls.Alignment
{
    public class UserInterfaceObject
    {
        private IDocumentContainer _documentContainer;

        public UserInterfaceObject(IDocumentContainer documentContainer)
        {
            _documentContainer = documentContainer;

        }

        public SrmDocument GetDocument()
        {
            return _documentContainer?.Document;
        }

        public UserInterfaceObject Detach()
        {
            if (_documentContainer == null)
            {
                return this;
            }
            var result = MemberwiseClone();
            foreach (var field in result.GetType().GetFields(BindingFlags.FlattenHierarchy | BindingFlags.NonPublic |
                                                             BindingFlags.Public | BindingFlags.Instance))
            {
                var value = field.GetValue(result) as UserInterfaceObject;
                if (value != null)
                {
                    field.SetValue(result, value.Detach());
                }
            }
            return (UserInterfaceObject) result;
        }
    }
}
