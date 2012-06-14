using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Evaluation;
using NuGet.VisualStudio.Resources;
using DTEProject = EnvDTE.Project;
using DTEProjectItem = EnvDTE.ProjectItem;
using MSBuildProject = Microsoft.Build.Evaluation.Project;

namespace NuGet.VisualStudio.Packaging
{
    internal class ManifestDataReader
    {
        private DTEProject _project;
        private MSBuildProject _msbuild;
        private Manifest _manifest;

        public ManifestDataReader(DTEProject project)
        {
            _project = project;
            _msbuild = _project.AsMSBuildProject();
            DTEProjectItem manifest = _project.ProjectItems.OfType<DTEProjectItem>().FirstOrDefault(p => p.FileNames[0].EndsWith(Constants.ManifestExtension));
            if (manifest != null)
            {
                using (Stream strm = File.OpenRead(manifest.FileNames[0]))
                {
                    _manifest = Manifest.ReadFrom(strm);
                }
            }
        }
    }
}
