using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.OLE.Interop;

namespace NuGet.VisualStudio.PropertyPage
{
    public abstract class PropertyPageBase : IPropertyPage2
    {
        public abstract string Title { get; }
        public virtual bool IsDirty { get { return false; } }
        public IPropertyPageSite Site { get; private set; }

        public virtual void Activate(IntPtr hWndParent, RECT[] pRect, int bModal)
        {
        }

        public virtual void Apply()
        {
        }

        public virtual void Deactivate()
        {
        }

        public void EditProperty(int DISPID)
        {
            throw new NotImplementedException();
        }

        public void GetPageInfo(PROPPAGEINFO[] pPageInfo)
        {
            PROPPAGEINFO info = new PROPPAGEINFO()
            {
                cb = (uint)Marshal.SizeOf(typeof(PROPPAGEINFO)),
                dwHelpContext = 0,
                pszDocString = null,
                pszHelpFile = null,
                pszTitle = Title
            };
            if (pPageInfo != null && pPageInfo.Length > 0)
            {
                pPageInfo[0] = info;
            }
        }

        public virtual void Help(string pszHelpDir)
        {
        }

        int IPropertyPage.IsPageDirty()
        {
            return IsDirty ? VsConstants.S_OK : VsConstants.S_FALSE;
        }

        public virtual void Move(RECT[] pRect)
        {
        }

        public virtual void SetObjects(uint cObjects, object[] ppunk)
        {
        }

        public virtual void SetPageSite(IPropertyPageSite pPageSite)
        {
            Site = pPageSite;
        }

        public virtual void Show(uint nCmdShow)
        {
        }

        public virtual int TranslateAccelerator(MSG[] pMsg)
        {
            return VsConstants.S_FALSE;
        }


        int IPropertyPage2.IsPageDirty()
        {
            return ((IPropertyPage)this).IsPageDirty();
        }


        int IPropertyPage.Apply()
        {
            try
            {
                ((IPropertyPage2)this).Apply();
            }
            catch (Exception ex)
            {
                return Marshal.GetHRForException(ex);
            }
            return VsConstants.S_OK;
        }
    }
}
