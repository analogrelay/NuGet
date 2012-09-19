using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Resources;

namespace NuGet
{
    public class PackageMerger : PackageBuilder
    {
        private List<string> _conflicts = new List<string>();
        
        public IEnumerable<string> Conflicts { get { return _conflicts; } }
        
        public PackageMerger() : base()
        {
        }

        public void MergeIn(IPackage package)
        {
            // Merge in the basic metadata
            MergeMetadata(package);

            // Merge in Framework References
            MergeFrameworkReferences(package);

            // Merge in Dependency Sets
            MergeDependencySets(package);

            // Merge files
            MergeFiles(package);
        }

        public void Save(string fileName)
        {
            using (Stream strm = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                Save(strm);
            }
        }

        private void MergeFiles(IPackage package)
        {
            // Take all files from the secondary package and add them, reporting an error if there's a conflict.
            foreach (var file in package.GetFiles())
            {
                if (Files.Any(f => String.Equals(f.Path, file.Path, StringComparison.OrdinalIgnoreCase)))
                {
                    _conflicts.Add(String.Format(NuGetResources.MergerFileConflict, file.Path));
                }
                else
                {
                    Files.Add(file);
                }
            }
        }

        private void MergeDependencySets(IPackage package)
        {
            foreach (var newSet in package.DependencySets)
            {
                var existingSet = DependencySets.Where(s => s.TargetFramework.Equals(newSet.TargetFramework)).FirstOrDefault();
                if (existingSet != null)
                {
                    existingSet.Dependencies.AddRange(newSet.Dependencies);
                }
                else
                {
                    DependencySets.Add(newSet);
                }
            }
        }

        private void MergeFrameworkReferences(IPackage package)
        {
            // Merge Framework Assembly References. We don't support assembly references having mismatched supported frameworks
            foreach (var secondaryRef in package.FrameworkAssemblies)
            {
                var matchingRef = FrameworkReferences.FirstOrDefault(
                    s => String.Equals(s.AssemblyName, secondaryRef.AssemblyName, StringComparison.OrdinalIgnoreCase));
                if (matchingRef != null)
                {
                    if (!Enumerable.SequenceEqual(matchingRef.SupportedFrameworks, secondaryRef.SupportedFrameworks))
                    {
                        _conflicts.Add(String.Format(
                            CultureInfo.CurrentCulture,
                            NuGetResources.MergerAssemblyReferenceConflict,
                            secondaryRef.AssemblyName));
                    }
                }
                else
                {
                    FrameworkReferences.Add(secondaryRef);
                }
            }
        }

        private void MergeMetadata(IPackage package)
        {
            Authors.AddRange(package.Authors);
            Copyright = package.Copyright;
            Description = package.Description;
            IconUrl = package.IconUrl;
            Id = package.Id;
            Language = package.Language;
            LicenseUrl = package.LicenseUrl;
            Owners.AddRange(package.Owners);
            ProjectUrl = package.ProjectUrl;
            ReleaseNotes = package.ReleaseNotes;
            RequireLicenseAcceptance = package.RequireLicenseAcceptance;
            Summary = package.Summary;
            if (package.Tags != null)
            {
                Tags.AddRange(package.Tags.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
            Title = package.Title;
            Version = package.Version;
        }
    }
}
