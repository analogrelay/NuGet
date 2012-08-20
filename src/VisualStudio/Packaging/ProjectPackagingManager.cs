using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace NuGet.VisualStudio.Packaging
{
    [Export(typeof(IProjectPackagingManager))]
    public class ProjectPackagingManager : IProjectPackagingManager
    {
        public bool IsCurrentSolutionEnabledForPackaging
        {
            get { throw new NotImplementedException(); }
        }

        public void EnableCurrentSolutionForPackaging(bool fromActivation)
        {
            throw new NotImplementedException();
        }
    }
}
