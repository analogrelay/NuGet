namespace NuGet.Test {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Xunit;
    using Moq;
    using NuGet.Test.Mocks;

    
    public class PackageManagerTest {
        [Fact]
        public void CtorThrowsIfDependenciesAreNull() {
            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => new PackageManager(null, new DefaultPackagePathResolver("foo"), new MockProjectSystem(), new MockPackageRepository()), "sourceRepository");
            ExceptionAssert.ThrowsArgNull(() => new PackageManager(new MockPackageRepository(), null, new MockProjectSystem(), new MockPackageRepository()), "pathResolver");
            ExceptionAssert.ThrowsArgNull(() => new PackageManager(new MockPackageRepository(), new DefaultPackagePathResolver("foo"), null, new MockPackageRepository()), "fileSystem");
            ExceptionAssert.ThrowsArgNull(() => new PackageManager(new MockPackageRepository(), new DefaultPackagePathResolver("foo"), new MockProjectSystem(), null), "localRepository");
        }

        [Fact]
        public void InstallingPackageWithUnknownDependencyAndIgnoreDepencenciesInstallsPackageWithoutDependencies() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                 new PackageDependency("C")
                                                             });

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0");
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);

            // Act
            packageManager.InstallPackage("A", version: null, ignoreDependencies: true);

            // Assert
            Assert.True(localRepository.Exists(packageA));
            Assert.False(localRepository.Exists(packageC));
        }

        [Fact]
        public void UninstallingUnknownPackageThrows() {
            // Arrange
            PackageManager packageManager = CreatePackageManager();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => packageManager.UninstallPackage("foo"), "Unable to find package 'foo'.");
        }

        [Fact]
        public void UninstallingUnknownNullOrEmptyPackageIdThrows() {
            // Arrange
            PackageManager packageManager = CreatePackageManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.UninstallPackage((string)null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.UninstallPackage(String.Empty), "packageId");
        }

        [Fact]
        public void UninstallingPackageWithNoDependents() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var package = PackageUtility.CreatePackage("foo", "1.2.33");
            localRepository.AddPackage(package);

            // Act
            packageManager.UninstallPackage("foo");

            // Assert
            Assert.False(packageManager.LocalRepository.Exists(package));
        }

        [Fact]
        public void InstallingUnknownPackageThrows() {
            // Arrange
            PackageManager packageManager = CreatePackageManager();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => packageManager.InstallPackage("unknown"),
                                                              "Unable to find package 'unknown'.");
        }

        [Fact]
        public void InstallPackageNullOrEmptyPackageIdThrows() {
            // Arrange
            PackageManager packageManager = CreatePackageManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.InstallPackage((string)null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.InstallPackage(String.Empty), "packageId");
        }

        [Fact]
        public void InstallPackageAddPackageToCache1() {
            // Arrange
            var cacheRepository = new Mock<IPackageRepository>();
            var projectSystem = new MockProjectSystem();
            var sourceRepository = new MockPackageRepository();
            var packageManager = new PackageManager(
                sourceRepository, 
                new DefaultPackagePathResolver(projectSystem), 
                projectSystem, 
                new MockPackageRepository(), 
                cacheRepository: cacheRepository.Object);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                             new[] { "contentFile", @"sub\contentFile" },
                                                             new[] { @"lib\reference.dll" },
                                                             new[] { @"readme.txt" });

            sourceRepository.AddPackage(packageA);

            // Act
            packageManager.InstallPackage("A");

            // Assert
            cacheRepository.Verify(p => p.AddPackage(packageA), Times.Once());
        }

        [TestMethod]
        public void InstallPackageAddPackageToCache2() {
            // Arrange
            var cacheRepository = new Mock<IPackageRepository>();
            var projectSystem = new MockProjectSystem();
            var sourceRepository = new MockPackageRepository();
            var packageManager = new PackageManager(
                sourceRepository,
                new DefaultPackagePathResolver(projectSystem),
                projectSystem,
                new MockPackageRepository(),
                cacheRepository: cacheRepository.Object);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                             new[] { "contentFile", @"sub\contentFile" },
                                                             new[] { @"lib\reference.dll" },
                                                             new[] { @"readme.txt" });

            sourceRepository.AddPackage(packageA);

            // Act
            packageManager.InstallPackage("A", new Version("1.0"));

            // Assert
            cacheRepository.Verify(p => p.AddPackage(packageA), Times.Once());
        }

        [TestMethod]
        public void InstallPackageAddPackageToCache3() {
            // Arrange
            var cacheRepository = new Mock<IPackageRepository>();
            var projectSystem = new MockProjectSystem();
            var sourceRepository = new MockPackageRepository();
            var packageManager = new PackageManager(
                sourceRepository,
                new DefaultPackagePathResolver(projectSystem),
                projectSystem,
                new MockPackageRepository(),
                cacheRepository: cacheRepository.Object);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                             new[] { "contentFile", @"sub\contentFile" },
                                                             new[] { @"lib\reference.dll" },
                                                             new[] { @"readme.txt" });

            sourceRepository.AddPackage(packageA);

            // Act
            packageManager.InstallPackage("A", new Version("1.0"), ignoreDependencies: true);

            // Assert
            cacheRepository.Verify(p => p.AddPackage(packageA), Times.Once());
        }

        [TestMethod]
        public void InstallPackageAddsAllFilesToFileSystem() {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var sourceRepository = new MockPackageRepository();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                             new[] { "contentFile", @"sub\contentFile" },
                                                             new[] { @"lib\reference.dll" },
                                                             new[] { @"readme.txt" });

            sourceRepository.AddPackage(packageA);

            // Act
            packageManager.InstallPackage("A");

            // Assert
            Assert.Equal(0, projectSystem.References.Count);
            Assert.Equal(5, projectSystem.Paths.Count);
            Assert.True(projectSystem.FileExists(@"A.1.0\content\contentFile"));
            Assert.True(projectSystem.FileExists(@"A.1.0\content\sub\contentFile"));
            Assert.True(projectSystem.FileExists(@"A.1.0\lib\reference.dll"));
            Assert.True(projectSystem.FileExists(@"A.1.0\tools\readme.txt"));
            Assert.True(projectSystem.FileExists(@"A.1.0\A.1.0.nupkg"));
        }

        [Fact]
        public void UnInstallingPackageUninstallsPackageButNotDependencies() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                new PackageDependency("B")
                                                            });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);

            // Act
            packageManager.UninstallPackage("A");

            // Assert            
            Assert.False(localRepository.Exists(packageA));
            Assert.True(localRepository.Exists(packageB));
        }

        [Fact]
        public void ReInstallingPackageAfterUninstallingDependencyShouldReinstallAllDependencies() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            PackageManager packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                dependencies: new List<PackageDependency> {
                    new PackageDependency("B")
                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                new PackageDependency("C")
                                                            });

            var packageC = PackageUtility.CreatePackage("C", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);

            // Act
            packageManager.InstallPackage("A");

            // Assert            
            Assert.True(localRepository.Exists(packageA));
            Assert.True(localRepository.Exists(packageB));
            Assert.True(localRepository.Exists(packageC));
        }

        [Fact]
        public void InstallPackageThrowsExceptionPackageIsNotInstalled() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new Mock<IProjectSystem>();
            projectSystem.Setup(m => m.AddFile(@"A.1.0\content\file", It.IsAny<Stream>())).Throws<UnauthorizedAccessException>();
            projectSystem.Setup(m => m.Root).Returns("FakeRoot");
            PackageManager packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem.Object), projectSystem.Object, localRepository);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "file" });
            sourceRepository.AddPackage(packageA);

            // Act
            ExceptionAssert.Throws<UnauthorizedAccessException>(() => packageManager.InstallPackage("A"));


            // Assert
            Assert.False(packageManager.LocalRepository.Exists(packageA));
        }

        [Fact]
        public void UpdatePackageUninstallsPackageAndInstallsNewPackage() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            PackageManager packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage A10 = PackageUtility.CreatePackage("A", "1.0");
            IPackage A20 = PackageUtility.CreatePackage("A", "2.0");
            localRepository.Add(A10);
            sourceRepository.Add(A20);

            // Act
            packageManager.UpdatePackage("A", updateDependencies: true);

            // Assert
            Assert.False(localRepository.Exists("A", new Version("1.0")));
            Assert.True(localRepository.Exists("A", new Version("2.0")));
        }

        [Fact]
        public void UpdatePackageAddNewPackageToCache() {
            // Arrange
            var cacheRepository = new Mock<IPackageRepository>();
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            PackageManager packageManager = new PackageManager(
                sourceRepository, 
                new DefaultPackagePathResolver(projectSystem), 
                projectSystem, 
                localRepository,
                cacheRepository: cacheRepository.Object);

            IPackage A10 = PackageUtility.CreatePackage("A", "1.0");
            IPackage A20 = PackageUtility.CreatePackage("A", "2.0");
            localRepository.Add(A10);
            sourceRepository.Add(A20);

            // Act
            packageManager.UpdatePackage("A", updateDependencies: true);

            // Assert
            cacheRepository.Verify(p => p.AddPackage(A20), Times.Once());
            cacheRepository.Verify(p => p.AddPackage(A10), Times.Never());
        }

        [TestMethod]
        public void UpdatePackageThrowsIfPackageNotInstalled() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            PackageManager packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage A20 = PackageUtility.CreatePackage("A", "2.0");
            sourceRepository.Add(A20);

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => packageManager.UpdatePackage("A", updateDependencies: true), "Unable to find package 'A'.");
        }

        [Fact]
        public void UpdatePackageDoesNothingIfNoUpdatesAvailable() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            PackageManager packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage A10 = PackageUtility.CreatePackage("A", "1.0");
            localRepository.Add(A10);

            // Act
            packageManager.UpdatePackage("A", updateDependencies: true);

            // Assert
            Assert.True(localRepository.Exists("A", new Version("1.0")));
        }

        [TestCleanup]
        public void CleanUp() {
            MachineCache.Default.Clear();
        }
        
        private PackageManager CreatePackageManager() {
            var projectSystem = new MockProjectSystem();
            return new PackageManager(
                new MockPackageRepository(), 
                new DefaultPackagePathResolver(projectSystem), 
                projectSystem);
        }

    }
}
