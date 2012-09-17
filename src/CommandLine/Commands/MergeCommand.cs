using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Commands
{
    [Command(typeof(NuGetCommand), "merge", "MergeCommandDescription",
        UsageSummaryResourceName = "MergeCommandUsageSummary", UsageDescriptionResourceName = "MergeCommandUsageDescription",
        UsageExampleResourceName = "MergeCommandUsageExamples", MinArgs = 3, MaxArgs = 3)]
    public class MergeCommand : Command
    {
        private bool _conflict = false;

        public override void ExecuteCommand()
        {
            _conflict = false;

            // Verify arguments
            string masterFile = Arguments[0];
            if (!File.Exists(masterFile))
            {
                Console.WriteError(NuGetResources.MergeCommandCannotFindSourceFile, masterFile);
            }
            string secondaryFile = Arguments[1];
            if (!File.Exists(secondaryFile))
            {
                Console.WriteError(NuGetResources.MergeCommandCannotFindSourceFile, secondaryFile);
            }
            string outputFile = Arguments[2];
            if (File.Exists(outputFile))
            {
                Console.WriteWarning(NuGetResources.FileWillBeOverwritten, outputFile);
            }

            // Open source and output files
            ZipPackage masterPackage = new ZipPackage(masterFile);
            ZipPackage secondaryPackage = new ZipPackage(secondaryFile);
            PackageBuilder outputPackage = new PackageBuilder();
            
            // Merge Metadata and files
            MergeMetadata(masterPackage, secondaryPackage, outputPackage);
            MergeFiles(masterPackage, secondaryPackage, outputPackage);

            if (!_conflict)
            {
                // Save output package
                using (Stream strm = File.Open(outputFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    strm.SetLength(0);
                    outputPackage.Save(strm);
                }
            }
        }

        private void MergeFiles(ZipPackage masterPackage, ZipPackage secondaryPackage, PackageBuilder outputPackage)
        {
            // Take all files from master and put them in the package
            outputPackage.Files.AddRange(masterPackage.GetFiles());

            // Take all files from the secondary package and add them, reporting an error if there's a conflict.
            foreach (var file in secondaryPackage.GetFiles())
            {
                if (outputPackage.Files.Any(f => String.Equals(f.Path, file.Path, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteError(NuGetResources.MergeCommandFileConflict, file.Path);
                    _conflict = true;
                }
                else
                {
                    outputPackage.Files.Add(file);
                }
            }
        }

        private void MergeMetadata(ZipPackage masterPackage, ZipPackage secondaryPackage, PackageBuilder outputPackage)
        {
            // Merge Dependency Sets. We don't support both input packages having dependency sets targetting the same framework
            outputPackage.DependencySets.AddRange(masterPackage.DependencySets);
            foreach (var secondarySet in secondaryPackage.DependencySets)
            {
                if (outputPackage.DependencySets.Any(s => s.TargetFramework.Equals(secondarySet.TargetFramework)))
                {
                    Console.WriteError(NuGetResources.MergeCommandDependencySetConflict, secondarySet.TargetFramework);
                    _conflict = true;
                }
                else
                {
                    outputPackage.DependencySets.Add(secondarySet);
                }
            }

            // Merge Framework Assembly References. We don't support assembly references having mismatched supported frameworks
            outputPackage.FrameworkReferences.AddRange(masterPackage.FrameworkAssemblies);
            foreach (var secondaryRef in secondaryPackage.FrameworkAssemblies)
            {
                var matchingRef = outputPackage.FrameworkReferences.FirstOrDefault(
                    s => String.Equals(s.AssemblyName, secondaryRef.AssemblyName, StringComparison.OrdinalIgnoreCase));
                if (matchingRef != null)
                {
                    if (!Enumerable.SequenceEqual(matchingRef.SupportedFrameworks, secondaryRef.SupportedFrameworks))
                    {
                        Console.WriteError(NuGetResources.MergeCommandAssemblyReferenceConflict, secondaryRef.AssemblyName);
                        _conflict = true;
                    }
                }
                else
                {
                    outputPackage.FrameworkReferences.Add(secondaryRef);
                }
            }

            CopyProperty(masterPackage, outputPackage, m => m.Authors);
            CopyProperty(masterPackage, outputPackage, m => m.Copyright);
            CopyProperty(masterPackage, outputPackage, m => m.Description);
            CopyProperty(masterPackage, outputPackage, m => m.IconUrl);
            CopyProperty(masterPackage, outputPackage, m => m.Id);
            CopyProperty(masterPackage, outputPackage, m => m.Language);
            CopyProperty(masterPackage, outputPackage, m => m.LicenseUrl);
            CopyProperty(masterPackage, outputPackage, m => m.Owners);
            CopyProperty(masterPackage, outputPackage, m => m.ProjectUrl);
            CopyProperty(masterPackage, outputPackage, m => m.ReleaseNotes);
            CopyProperty(masterPackage, outputPackage, m => m.RequireLicenseAcceptance);
            CopyProperty(masterPackage, outputPackage, m => m.Summary);
            CopyProperty(masterPackage, outputPackage, m => m.Tags);
            CopyProperty(masterPackage, outputPackage, m => m.Title);
            CopyProperty(masterPackage, outputPackage, m => m.Version);
        }

        private void CopyProperty<T>(ZipPackage masterPackage, PackageBuilder outputPackage, Expression<Func<IPackageMetadata, T>> property)
        {
            // Extract the property from the expression
            MemberExpression expr = property.Body as MemberExpression;
            Debug.Assert(expr != null, String.Format(CultureInfo.CurrentCulture, NuGetResources.ArgumentMustBePropertyExpression, "property"));
            PropertyInfo prop = expr.Member as PropertyInfo;
            Debug.Assert(prop != null, String.Format(CultureInfo.CurrentCulture, NuGetResources.ArgumentMustBePropertyExpression, "property"));

            // Get the master value and put it in the output package
            prop.SetValue(outputPackage, prop.GetValue(masterPackage));
        }
    }
}
