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
        [Option(typeof(NuGetCommand), "MergeCommandNewPackageIdDescription")]
        public string NewPackageId { get; set; }

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

            // Open source files
            ZipPackage masterPackage = new ZipPackage(masterFile);
            ZipPackage package = new ZipPackage(secondaryFile);
            
            // Merge!
            PackageMerger merger = new PackageMerger();

            // Start with the "secondary" since MergeIn overwrites existing properties 
            // with whatever we pass in here and we want "master" to win
            merger.MergeIn(package);
            merger.MergeIn(masterPackage);

            // Check for conflicts
            if (merger.Conflicts.Any())
            {
                foreach (var conflict in merger.Conflicts)
                {
                    Console.WriteError(conflict);
                }
            }
            else
            {
                // Set the override package id if specified
                if (!String.IsNullOrEmpty(NewPackageId))
                {
                    merger.Id = NewPackageId;
                }

                // Save the output package
                merger.Save(outputFile);
            }
        }
    }
}
