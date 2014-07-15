using UnityEngine;
using System;


namespace Zigfu.Utility
{
    abstract public class NativeObjectWrapper
    {
        abstract protected String ClassName { get; }

        public static bool verbose = false;


        protected IntPtr p;
        public IntPtr NativeObject { get { return p; } }

        protected NativeObjectWrapper(IntPtr p)
        {
            LogStatus("ctor");
            if (p == IntPtr.Zero) { throw new ArgumentNullException(); }

            this.p = p;
        }

        ~NativeObjectWrapper() { LogStatus("~destructor"); }


        protected void LogStatus(String msg)
        {
            if (!verbose) { return; }
            UnityEngine.Debug.Log(ClassName + ":: " + msg);
        }
    }
}
