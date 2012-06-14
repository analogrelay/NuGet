using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace NuGet.Tools
{
    internal static class NativeMethods
    {
        [ComImport]
        [Guid("00000001-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IClassFactory
        {
            [PreserveSig]
            int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);
            [PreserveSig]
            int LockServer(bool fLock);
        }

        [DllImport("ole32.dll")]
        public static extern int CoCreateInstance(ref Guid clsid, IntPtr unkOuter, uint clsContext, ref Guid iid, out IntPtr obj);
        [DllImport("ole32.dll")]
        public static extern int CoRegisterClassObject(ref Guid clsid, [MarshalAs(UnmanagedType.Interface)] IClassFactory classFactory, uint context, uint flags, out uint register);
        [DllImport("ole32.dll")]
        public static extern int CoRevokeClassObject(uint token);
    }
}
