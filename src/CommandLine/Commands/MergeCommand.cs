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
        public override void ExecuteCommand()
        {
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
            if (MergeMetadata(masterPackage, secondaryPackage, outputPackage) && MergeFiles(masterPackage, secondaryPackage, outputPackage))
            {
                // Save output package
                using (Stream strm = File.Open(outputFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    strm.SetLength(0);
                    outputPackage.Save(strm);
                }
            }
        }

        private bool MergeFiles(ZipPackage masterPackage, ZipPackage secondaryPackage, PackageBuilder outputPackage)
        {
            throw new NotImplementedException();
        }

        private bool MergeMetadata(ZipPackage masterPackage, ZipPackage secondaryPackage, PackageBuilder outputPackage)
        {
            // Merge Dependency Sets. We don't support both input packages having dependency sets targetting the same framework
            outputPackage.DependencySets.AddRange(masterPackage.DependencySets);
            foreach (var secondarySet in secondaryPackage.DependencySets)
            {
                if (outputPackage.DependencySets.Any(s => s.TargetFramework.Equals(secondarySet.TargetFramework)))
                {
                    Console.WriteError(NuGetResources.MergeCommandDependencySetConflict, secondarySet.TargetFramework);
                    return false;
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
                        Console.WriteError(NuGetResources.MergeCommandDependencySetConflict, secondaryRef.AssemblyName);
                        return false;
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
            return true;
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
