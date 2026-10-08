using System;
using System.Windows.Forms;
using RightLayout.Win32;

namespace RightLayout {
	internal static class Program {
		[STAThread]
		private static void Main() {
			// Also for DLLs loaded on our behalf (e.g. by WinForms): search only System32, not the exe's
			// folder or the current directory. Both calls exist on every supported Windows version.
			try {
				API.SetDllDirectory("");
				API.SetDefaultDllDirectories(API.LOAD_LIBRARY_SEARCH_SYSTEM32);
			}
			catch(EntryPointNotFoundException) {
			}

			// Render sharply on high-DPI screens instead of being stretched by Windows
			API.SetProcessDPIAware();

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new TrayApp());
		}
	}
}
