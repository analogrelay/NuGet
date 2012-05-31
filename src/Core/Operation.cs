using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet
{
    /// <summary>
    /// Contains constant values for use with <see cref="NuGet.Extensions.PackageRepositoryExtensions.StartOperation(string)"/>
    /// </summary>
    public static class Operation
    {
        public static readonly string Install = "Install";
        public static readonly string Upgrade = "Upgrade";
        public static readonly string Restore = "Restore";
    }
}
