﻿using System;
using NuGet;

namespace PackageExplorerViewModel.Types {
    public interface IPackageDownloader {
        void Download(Uri downloadUri, string packageId, Version packageVersion, IProxyService proxyService, Action<IPackage> callback);
    }
}