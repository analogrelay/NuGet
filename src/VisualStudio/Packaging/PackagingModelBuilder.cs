using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using EnvDTE;

namespace NuGet.VisualStudio.Packaging
{
    [Export(typeof(IPackagingModelBuilder))]
    public class PackagingModelBuilder : IPackagingModelBuilder
    {
        public PackagingModel GetModelForProject(Project project)
        {
            ManifestDataReader config = new ManifestDataReader(project);
            return new PackagingModel()
            {
            };
        }
    }
}
