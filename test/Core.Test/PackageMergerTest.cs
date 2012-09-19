using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Test
{
    public class PackageMergerTest
    {
        public class TheConstructor
        {
            [Fact]
            public void IntializesCollections()
            {
                // Arrange/Act
                PackageMerger merger = new PackageMerger();

                // Assert
                Assert.NotNull(merger.Conflicts);
                Assert.NotNull(merger.Files);
                Assert.NotNull(merger.DependencySets);
                Assert.NotNull(merger.FrameworkReferences);
                Assert.NotNull(merger.PackageAssemblyReferences);
                Assert.NotNull(merger.Authors);
                Assert.NotNull(merger.Owners);
                Assert.NotNull(merger.Tags);
            }
        }
    }
}
