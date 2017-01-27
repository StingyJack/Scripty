using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;


namespace Scripty
{
    using Core;

    /// <summary>
    // Extends the standard dialog functionality for implementing ToolsOptions pages,
    // with support for the Visual Studio automation model, Windows Forms, and state
    // persistence through the Visual Studio settings mechanism.
    /// </summary>
    [Guid(ScriptyOptions.ScriptyOptionsGuidString)]
    public sealed class ScriptyOptions : DialogPage
    {
        /// <summary>
        /// ScriptyOptions GUID string.
        /// </summary>
        public const string ScriptyOptionsGuidString = "1fd5d182-c25d-47a2-b4f3-e1471556b246";

        #region Constructors

        public ScriptyOptions()
        {
            OnScriptGenerateOutputBehavior = OnScriptGenerateOutputBehavior.AlwaysOverwriteOutput;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Determine scripty output handling
        /// </summary>
        /// <remarks>This value is shown in the options page.</remarks>
        [Category("Behaviors")]
        [DisplayName("Script Output Handling")]
        [Description("What should scripty do in regards to output handling? The default is AlwaysOverwriteOutput.")]
        public OnScriptGenerateOutputBehavior OnScriptGenerateOutputBehavior { get; set; }
        
        #endregion Properties

        #region Event Handlers

        /// <summary>
        /// Handles "activate" messages from the Visual Studio environment.
        /// </summary>
        /// <devdoc>
        /// This method is called when Visual Studio wants to activate this page.
        /// </devdoc>
        /// <remarks>If this handler sets e.Cancel to true, the activation will not occur.</remarks>
        protected override void OnActivate(CancelEventArgs e)
        {
            //int result = VsShellUtilities.ShowMessageBox(Site, "Press Cancel to cancel this activiation. Ok to continue.", null /*title*/, OLEMSGICON.OLEMSGICON_QUERY, OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            //if (result == (int)VSConstants.MessageBoxResult.IDCANCEL)
            //{
            //    e.Cancel = true;
            //}

            base.OnActivate(e);
        }

        /// <summary>
        /// Handles "close" messages from the Visual Studio environment.
        /// </summary>
        /// <devdoc>
        /// This event is raised when the page is closed.
        /// </devdoc>
        protected override void OnClosed(EventArgs e)
        {
            //VsShellUtilities.ShowMessageBox(Site, "In OnClosed", null /*title*/, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        /// <summary>
        /// Handles "deactivate" messages from the Visual Studio environment.
        /// </summary>
        /// <devdoc>
        /// This method is called when VS wants to deactivate this
        /// page.  If this handler sets e.Cancel, the deactivation will not occur.
        /// </devdoc>
        /// <remarks>
        /// A "deactivate" message is sent when focus changes to a different page in
        /// the dialog.
        /// </remarks>
        protected override void OnDeactivate(CancelEventArgs e)
        {
            //int result = VsShellUtilities.ShowMessageBox(Site, "Press Cancel to cancel this deactivation. OK to coninue.", null /*title*/, OLEMSGICON.OLEMSGICON_QUERY, OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            //if (result == (int)VSConstants.MessageBoxResult.IDCANCEL)
            //{
            //    e.Cancel = true;
            //}
        }

        /// <summary>
        /// Handles "apply" messages from the Visual Studio environment.
        /// </summary>
        /// <devdoc>
        /// This method is called when VS wants to save the user's
        /// changes (for example, when the user clicks OK in the dialog).
        /// </devdoc>
        protected override void OnApply(PageApplyEventArgs e)
        {
            //int result = VsShellUtilities.ShowMessageBox(Site, "Press Cancel to cancel this OnApply.  OK to continue", null /*title*/, OLEMSGICON.OLEMSGICON_QUERY, OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            //if (result == (int)VSConstants.MessageBoxResult.IDCANCEL)
            //{
            //    e.ApplyBehavior = ApplyKind.Cancel;
            //}
            //else
            //{
            //    base.OnApply(e);
            //}

            //VsShellUtilities.ShowMessageBox(Site, "In OnApply", null /*title*/, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        #endregion Event Handlers
    }

}
