using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;

namespace NuGet.Tools
{
    [ComVisible(true)]
    [Guid("43C45E72-52C3-4D18-BE06-B132506E1AC1")]
    internal class PropertyPageFactory : NativeMethods.IClassFactory
    {
        private Func<object> _factory;

        public PropertyPageFactory(Func<object> factory)
        {
            _factory = factory;
        }

        public static IDisposable Register<T>() where T : class, new()
        {
            // Initialize the property page
            Guid pageId = typeof(T).GUID;
            uint token;
            Marshal.ThrowExceptionForHR(
                NativeMethods.CoRegisterClassObject(
                    ref pageId, PropertyPageFactory.ForType<T>(), 1u, 1u, out token));

            return new DisposableAction(() => 
                Marshal.ThrowExceptionForHR(NativeMethods.CoRevokeClassObject(token)));
        }

        public static PropertyPageFactory ForType<T>() where T : class, new()
        {
            return new PropertyPageFactory(() => new T());
        }

        public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
        {
            ppvObject = IntPtr.Zero;
            if (pUnkOuter != IntPtr.Zero)
            {
                // Caller is trying to aggregate
                // HR -2147221232 is CLASS_E_NOAGGREGATION
                Marshal.ThrowExceptionForHR(-2147221232);
            }
            else
            {
                if (riid == GuidList.iidIUnknown || riid == GuidList.iidIPropertyPage)
                {
                    object page = _factory();
                    ppvObject = Marshal.GetComInterfaceForObject(page, typeof(IPropertyPage));
                }
                else
                {
                    // E_NOINTERFACE
                    Marshal.ThrowExceptionForHR(-2147467262);
                }
            }
            return 0; // S_OK
        }

        public int LockServer(bool fLock)
        {
            return 0;
        }
    }
}
