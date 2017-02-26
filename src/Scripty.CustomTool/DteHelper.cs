namespace Scripty
{
    using System.Runtime.InteropServices;
    using EnvDTE80;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    ///     Gets u some DTE stuff
    /// </summary>
    /// <remarks>
    ///     
    /// </remarks>
    public class DteHelper
    {
        //// https://msdn.microsoft.com/en-us/library/ee834473.aspx

        //[Import]
        //internal SVsServiceProvider _ServiceProvider;
        ////doesnt work
        //private DTE2 GetDteVs14Up()
        //{
        //    var dte2 = (DTE2)_ServiceProvider.GetService(typeof(DTE2));
        //    return dte2;
        //}

        //https://msdn.microsoft.com/en-us/library/68shb4dw.aspx?f=255&MSPPError=-2147217396
        public DTE2 GetDteVs14()
        {
            //this is dirty but works
            var dte2 = (DTE2)Marshal.GetActiveObject("VisualStudio.DTE.14.0");
            return dte2;

        }
    }
}