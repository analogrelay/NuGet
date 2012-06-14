using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;

namespace NuGet.VisualStudio.Packaging
{
    public interface IPackagingModelBuilder
    {
        PackagingModel GetModelForProject(Project project);
    }
}
