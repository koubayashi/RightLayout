using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace RightKeyboard {
	/// <summary>
	/// Opens the app's folders in File Explorer, so users can find the exe or back up their settings
	/// </summary>
	public static class AppLocations {
		/// <summary>
		/// Opens the folder the app is running from, with the exe selected
		/// </summary>
		public static void ShowApp() {
			ShowInExplorer(Application.ExecutablePath);
		}

		/// <summary>
		/// Opens the folder holding the settings, with config.txt selected
		/// </summary>
		public static void ShowSettings() {
			ShowInExplorer(KeyboardSettings.ConfigFilePath);
		}

		private static void ShowInExplorer(string path) {
			try {
				// Use Explorer from the Windows folder by full path, not whatever "explorer.exe" is found first
				string explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
				// Select the file when it exists; otherwise just open its folder
				string arguments = File.Exists(path) ? "/select,\"" + path + "\"" : "\"" + Path.GetDirectoryName(path) + "\"";
				Process.Start(explorer, arguments);
			}
			catch(Exception err) {
				MessageBox.Show("Could not open File Explorer. Reason: " + err.Message + "\n\n" + path, "RightLayout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}
	}
}
