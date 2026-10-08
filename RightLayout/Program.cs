using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RightLayout {
	static class Program {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() {
			// Also for DLLs loaded on our behalf (e.g. by WinForms): search only System32, not the exe's
			// folder or the current directory. Both calls exist on every supported Windows version.
			try {
				RightLayout.Win32.API.SetDllDirectory("");
				RightLayout.Win32.API.SetDefaultDllDirectories(RightLayout.Win32.API.LOAD_LIBRARY_SEARCH_SYSTEM32);
			}
			catch(EntryPointNotFoundException) {
			}

			// Render sharply on high DPI screens instead of being stretched by Windows
			if(Environment.OSVersion.Version.Major >= 6) {
				RightLayout.Win32.API.SetProcessDPIAware();
			}
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
	}
}