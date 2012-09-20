using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NuGet.Test
{
    public class MemoryPackage : LocalPackage
    {
        public ICollection<IPackageFile> PackageFiles { get; private set; }
        public ICollection<IPackageAssemblyReference> PackageAssemblyReferences { get; private set; }

        public MemoryPackage()
        {
            PackageFiles = new List<IPackageFile>();
            PackageAssemblyReferences = new List<IPackageAssemblyReference>();
        }

        public override Stream GetStream()
        {
            return Stream.Null;
        }

        protected override IEnumerable<IPackageFile> GetFilesBase()
        {
            return PackageFiles;
        }

        protected override IEnumerable<IPackageAssemblyReference> GetAssemblyReferencesBase()
        {
            return PackageAssemblyReferences;           
        }
    }
}
