using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RightKeyboard {
	static class Program {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() {
			// Render sharply on high DPI screens instead of being stretched by Windows
			if(Environment.OSVersion.Version.Major >= 6) {
				RightKeyboard.Win32.API.SetProcessDPIAware();
			}
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
	}
}