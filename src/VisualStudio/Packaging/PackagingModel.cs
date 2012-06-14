using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.VisualStudio.Packaging
{
    /// <summary>
    /// Represents the packaging information for a particular project.
    /// </summary>
    /// <remarks>
    /// This model unifies data stored in the following locations:
    /// * The Project MSBuild file (.csproj, .vbproj, etc.)
    /// * The AssemblyInfo.cs file, if present
    /// * The NuSpec file, if present
    /// </remarks>
    public class PackagingModel : IPackageMetadata
    {
        public string Id { get; set; }
        public SemanticVersion Version { get; set; }
        public string Title { get; set; }
        public ICollection<string> Authors { get; private set; }
        public ICollection<string> Owners { get; private set; }
        public Uri IconUrl { get; set; }
        public Uri LicenseUrl { get; set; }
        public Uri ProjectUrl { get; set; }
        public bool RequireLicenseAcceptance { get; set; }
        public string Description { get; set; }
        public string Summary { get; set; }
        public string ReleaseNotes { get; set; }
        public string Language { get; set; }
        public string Tags { get; set; }
        public string Copyright { get; set; }

        public ICollection<PackagingReference> References { get; private set; }

        public PackagingModel()
        {
            Authors = new List<string>();
            Owners = new List<string>();
        }

        IEnumerable<FrameworkAssemblyReference> IPackageMetadata.FrameworkAssemblies
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<PackageDependencySet> IPackageMetadata.DependencySets
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<string> IPackageMetadata.Authors
        {
            get { return this.Authors; }
        }

        IEnumerable<string> IPackageMetadata.Owners
        {
            get { return this.Owners; }
        }
    }
}
