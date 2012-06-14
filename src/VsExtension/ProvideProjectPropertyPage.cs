using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;

namespace NuGet.Tools
{
    public class ProvideProjectPropertyPageAttribute : RegistrationAttribute
    {
        public string PropertyPageGuid { get; set; }
        public string[] ProjectTypeGuids { get; set; }
        public string Name { get; set; }

        public ProvideProjectPropertyPageAttribute(Type propertyPageType, string name, params string[] projectTypeGuids)
        {
            PropertyPageGuid = propertyPageType.GUID.ToString("B");
            Name = name;
            ProjectTypeGuids = projectTypeGuids;
        }

        public override void Register(RegistrationAttribute.RegistrationContext context)
        {
            foreach (string projectType in ProjectTypeGuids)
            {
                Key k = context.CreateKey(@"Projects\" + projectType + @"\ConfigPropertyPages\" + PropertyPageGuid);
                k.SetValue(null, Name);
            }
        }

        public override void Unregister(RegistrationAttribute.RegistrationContext context)
        {
            foreach (string projectType in ProjectTypeGuids)
            {
                context.RemoveKey(@"Projects\" + projectType + @"\ConfigPropertyPages\" + PropertyPageGuid);
            }
        }
    }
}
