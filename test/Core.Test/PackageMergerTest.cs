using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using NuGet.Resources;
using Xunit;

namespace NuGet.Test
{
    public class PackageMergerTest
    {
        private const bool OldRequireLicenseAcceptance = false;
        private const bool NewRequireLicenseAcceptance = true;
        private static readonly SemanticVersion OldVersion = new SemanticVersion("1.0.0-old");
        private static readonly SemanticVersion NewVersion = new SemanticVersion("2.0.0-new");
        private static readonly FrameworkName NetFx1 = new FrameworkName(".NETFramework, Version=1.0");
        private static readonly FrameworkName NetFx2 = new FrameworkName(".NETFramework, Version=2.0");

        public class TheConstructor
        {
            [Fact]
            public void IntializesConflictsList()
            {
                // Arrange/Act
                PackageMerger merger = new PackageMerger();

                // Assert
                Assert.NotNull(merger.Conflicts);
                Assert.Empty(merger.Conflicts);
            }
        }

        public class TheMergeInMethod
        {
            [Fact]
            public void RequiresNonNullPackage()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new PackageMerger().MergeIn(null));
                Assert.Equal("package", ex.ParamName);
            }

            [Fact]
            public void OverwritesSingleValueMetadataWithSpecifiedPackageMetadata()
            {
                // Arrange
                PackageMerger merger = CreateBaseMerger();
                MemoryPackage package = CreateNewPackage();

                // Act
                merger.MergeIn(package);

                // Assert
                Assert.Equal("NewCopyright", merger.Copyright);
                Assert.Equal("NewDescription", merger.Description);
                Assert.Equal(new Uri("http://NewIconUrl"), merger.IconUrl);
                Assert.Equal("NewId", merger.Id);
                Assert.Equal("NewLanguage", merger.Language);
                Assert.Equal(new Uri("http://NewLicenseUrl"), merger.LicenseUrl);
                Assert.Equal(new Uri("http://NewProjectUrl"), merger.ProjectUrl);
                Assert.Equal("NewReleaseNotes", merger.ReleaseNotes);
                Assert.Equal(NewRequireLicenseAcceptance, merger.RequireLicenseAcceptance);
                Assert.Equal("NewSummary", merger.Summary);
                Assert.Equal("NewTitle", merger.Title);
                Assert.Equal(NewVersion, merger.Version);
            }

            [Fact]
            public void CombinesMultiValueMetadataWithSpecifiedPackageMetadata()
            {
                // Arrange
                PackageMerger merger = CreateBaseMerger();
                MemoryPackage package = CreateNewPackage();

                // Act
                merger.MergeIn(package);

                // Assert
                Assert.Contains("NewTag", merger.Tags);
                Assert.Contains("OldTag", merger.Tags);
                Assert.Contains("NewAuthor", merger.Authors);
                Assert.Contains("OldAuthor", merger.Authors);
                Assert.Contains("NewOwner", merger.Owners);
                Assert.Contains("OldOwner", merger.Owners);
            }

            [Fact]
            public void RemovesDuplicatesWhenCombiningMultiValueMetadata()
            {
                // Arrange
                PackageMerger merger = CreateBaseMerger();
                MemoryPackage package = CreateNewPackage();
                package.Tags = "OldTag";
                package.Authors = new[] { "OldAuthor" };
                package.Owners = new[] { "OldOwner" };

                // Act
                merger.MergeIn(package);

                // Assert
                Assert.Equal(1, merger.Tags.Where(s => s == "OldTag").Count());
                Assert.Equal(1, merger.Authors.Where(s => s == "OldAuthor").Count());
                Assert.Equal(1, merger.Owners.Where(s => s == "OldOwner").Count());
            }

            [Fact]
            public void CombinesFrameworkReferencesWithSpecifiedPackageReferences()
            {
                // Arrange
                PackageMerger merger = CreateBaseMerger();
                MemoryPackage package = CreateNewPackage();

                // Act
                merger.MergeIn(package);

                // Assert
                Assert.Equal(1, merger.FrameworkReferences.Where(r => r.AssemblyName == "OldReference").Count());
                Assert.Equal(1, merger.FrameworkReferences.Where(r => r.AssemblyName == "NewReference").Count());
            }

            [Fact]
            public void MarksFrameworkReferencesWithDifferentSupportedFrameworksAsConflictAndDoesNotMerge()
            {
                // Arrange
                PackageMerger merger = CreateBaseMerger();
                MemoryPackage package = CreateNewPackage();

                merger.FrameworkReferences.Clear();
                merger.FrameworkReferences.Add(new FrameworkAssemblyReference("Reference", new[] { NetFx1 }));
                package.FrameworkAssemblies = new[] { new FrameworkAssemblyReference("Reference", new[] { NetFx2 }) };

                // Act
                merger.MergeIn(package);

                // Assert
                Assert.Equal(1,
                    merger.FrameworkReferences
                          .Where(f => f.AssemblyName == "Reference")
                          .Count());
                Assert.Equal(NetFx1, 
                    merger.FrameworkReferences
                          .Where(f => f.AssemblyName == "Reference")
                          .Single()
                          .SupportedFrameworks
                          .Single());
                Assert.Equal(String.Format(
                    CultureInfo.CurrentCulture,
                    NuGetResources.MergerAssemblyReferenceConflict,
                    "Reference"), merger.Conflicts.Single());
            }

            [Fact]
            public void CombinesDependencySetsWithSpecifiedPackageReferences()
            {
                // Arrange
                PackageMerger merger = CreateBaseMerger();
                MemoryPackage package = CreateNewPackage();

                // Act
                merger.MergeIn(package);

                // Assert
                Assert.Equal(2, merger.DependencySets.Count);
                Assert.Equal("Foo", merger.DependencySets.Where(d => d.TargetFramework == NetFx1).Single().Dependencies.Single().Id);
                Assert.Equal("Bar", merger.DependencySets.Where(d => d.TargetFramework == NetFx2).Single().Dependencies.Single().Id);
            }

            [Fact]
            public void CombinesIndividualDependencySetsWithSameTargetFramework()
            {
                // Arrange
                PackageMerger merger = CreateBaseMerger();
                MemoryPackage package = CreateNewPackage();
                package.DependencySets = new[] {
                    new PackageDependencySet(NetFx1, new [] {
                        new PackageDependency("Foo"),
                        new PackageDependency("Quuz")
                    })
                };

                // Act
                merger.MergeIn(package);

                // Assert
                Assert.Equal(1, merger.DependencySets.Count);
                Assert.Equal(NetFx1, merger.DependencySets.Single().TargetFramework);
                Assert.Equal(2, merger.DependencySets.Single().Dependencies.Count);
                Assert.Equal(1, merger.DependencySets.Single().Dependencies.Where(d => d.Id == "Foo").Count());
                Assert.Equal(1, merger.DependencySets.Single().Dependencies.Where(d => d.Id == "Quuz").Count());
            }
        }

        private static PackageMerger CreateBaseMerger()
        {
            PackageMerger merger = new PackageMerger()
            {
                Copyright = "OldCopyright",
                Description = "OldDescription",
                IconUrl = new Uri("http://OldIconUrl"),
                Id = "OldId",
                Language = "OldLanguage",
                LicenseUrl = new Uri("http://OldLicenseUrl"),
                ProjectUrl = new Uri("http://OldProjectUrl"),
                ReleaseNotes = "OldReleaseNotes",
                RequireLicenseAcceptance = OldRequireLicenseAcceptance,
                Summary = "OldSummary",
                Title = "OldTitle",
                Version = OldVersion
            };
            merger.Tags.Add("OldTag");
            merger.Authors.Add("OldAuthor");
            merger.Owners.Add("OldOwner");

            merger.FrameworkReferences.Add(new FrameworkAssemblyReference("OldReference"));
            merger.DependencySets.Add(new PackageDependencySet(NetFx1, new[] { 
                new PackageDependency("Foo")
            }));

            return merger;
        }

        private static MemoryPackage CreateNewPackage()
        {
            MemoryPackage package = new MemoryPackage()
            {
                Copyright = "NewCopyright",
                Description = "NewDescription",
                IconUrl = new Uri("http://NewIconUrl"),
                Id = "NewId",
                Language = "NewLanguage",
                LicenseUrl = new Uri("http://NewLicenseUrl"),
                ProjectUrl = new Uri("http://NewProjectUrl"),
                ReleaseNotes = "NewReleaseNotes",
                RequireLicenseAcceptance = NewRequireLicenseAcceptance,
                Summary = "NewSummary",
                Title = "NewTitle",
                Version = NewVersion
            };
            package.Tags = "NewTag";
            package.Authors = new[] { "NewAuthor" };
            package.Owners = new[] { "NewOwner" };

            package.FrameworkAssemblies = new[] { new FrameworkAssemblyReference("NewReference") };
            package.DependencySets = new[] { 
                new PackageDependencySet(NetFx2, new[] { 
                    new PackageDependency("Bar")
                }) 
            };

            return package;
        }
    }
}
