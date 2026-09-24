using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace RightKeyboard {
	/// <summary>
	/// A keyboard RightKeyboard remembers
	/// </summary>
	public class KeyboardEntry {
		/// <summary>
		/// Raw input device name, which stays the same when the keyboard reconnects
		/// </summary>
		public string DeviceName;

		/// <summary>
		/// Language identifier of the input method, or KeyboardSettings.NoSwitch
		/// </summary>
		public ushort Layout;

		/// <summary>
		/// Name chosen by the user, or null
		/// </summary>
		public string Nickname;
	}

	/// <summary>
	/// Stores which input method each keyboard uses, in %LOCALAPPDATA%\RightKeyboard\config.txt
	/// </summary>
	public class KeyboardSettings {
		/// <summary>
		/// Layout value meaning "leave the input method alone when this keyboard is used"
		/// </summary>
		public const ushort NoSwitch = 0;

		private readonly Dictionary<string, KeyboardEntry> entries = new Dictionary<string, KeyboardEntry>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Raised after any keyboard is added, changed or removed
		/// </summary>
		public event EventHandler Changed;

		public KeyboardEntry Get(string deviceName) {
			KeyboardEntry entry;
			entries.TryGetValue(deviceName, out entry);
			return entry;
		}

		public List<KeyboardEntry> GetAll() {
			return new List<KeyboardEntry>(entries.Values);
		}

		public void SetLayout(string deviceName, ushort layout) {
			SetLayout(new string[] { deviceName }, layout);
		}

		/// <summary>
		/// Sets the input method of several devices at once, e.g. all the parts of one keyboard
		/// </summary>
		public void SetLayout(IEnumerable<string> deviceNames, ushort layout) {
			foreach(string deviceName in deviceNames) {
				KeyboardEntry entry = Get(deviceName);
				if(entry == null) {
					entry = new KeyboardEntry();
					entry.DeviceName = deviceName;
					entries.Add(deviceName, entry);
				}
				entry.Layout = layout;
			}
			SaveAndNotify();
		}

		/// <summary>
		/// Names the saved devices among deviceNames; an empty name goes back to the detected one
		/// </summary>
		public void SetNickname(IEnumerable<string> deviceNames, string nickname) {
			nickname = (nickname ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();
			foreach(string deviceName in deviceNames) {
				KeyboardEntry entry = Get(deviceName);
				if(entry != null) {
					entry.Nickname = nickname.Length == 0 ? null : nickname;
				}
			}
			SaveAndNotify();
		}

		public void Remove(IEnumerable<string> deviceNames) {
			bool removed = false;
			foreach(string deviceName in deviceNames) {
				removed |= entries.Remove(deviceName);
			}
			if(removed) {
				SaveAndNotify();
			}
		}

		/// <summary>
		/// Gets the input method already chosen for another part of the same physical keyboard.
		/// Fails when none is saved, or when the saved parts disagree.
		/// </summary>
		public bool TryGetGroupLayout(string deviceName, out ushort layout) {
			layout = NoSwitch;
			string groupKey = KeyboardDevices.Get(deviceName).GroupKey;
			bool found = false;
			foreach(KeyboardEntry entry in entries.Values) {
				if(KeyboardDevices.Get(entry.DeviceName).GroupKey == groupKey) {
					if(found && entry.Layout != layout) {
						return false;
					}
					layout = entry.Layout;
					found = true;
				}
			}
			return found;
		}

		public void Clear() {
			entries.Clear();
			SaveAndNotify();
		}

		private void SaveAndNotify() {
			Save();
			if(Changed != null) {
				Changed(this, EventArgs.Empty);
			}
		}

		public void Save() {
			try {
				using(TextWriter output = new StreamWriter(GetConfigFilePath(), false, Encoding.UTF8)) {
					foreach(KeyboardEntry entry in entries.Values) {
						output.WriteLine("{0}\t{1:X04}\t{2}", entry.DeviceName, entry.Layout, entry.Nickname ?? "");
					}
				}
			}
			catch(Exception err) {
				MessageBox.Show("Could not save the configuration. Reason: " + err.Message, "RightKeyboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		public void Load() {
			try {
				string configFilePath = GetConfigFilePath();
				if(!File.Exists(configFilePath)) {
					return;
				}

				foreach(string line in File.ReadAllLines(configFilePath)) {
					KeyboardEntry entry = ParseLine(line);
					if(entry != null) {
						// Keep the mapping even if the device isn't connected right now
						entries[entry.DeviceName] = entry;
					}
				}
			}
			catch(Exception err) {
				MessageBox.Show("Could not load the configuration. Reason: " + err.Message, "RightKeyboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private static KeyboardEntry ParseLine(string line) {
			string deviceName;
			string layoutText;
			string nickname = null;

			if(line.IndexOf('\t') >= 0) {
				// device <TAB> layout <TAB> nickname
				string[] parts = line.Split('\t');
				deviceName = parts[0];
				layoutText = parts[1];
				if(parts.Length > 2 && parts[2].Trim().Length > 0) {
					nickname = parts[2].Trim();
				}
			}
			else {
				// Older versions wrote device=layout
				int separator = line.LastIndexOf('=');
				if(separator <= 0) {
					return null;
				}
				deviceName = line.Substring(0, separator);
				layoutText = line.Substring(separator + 1);
			}

			ushort layout;
			if(deviceName.Length == 0 || !ushort.TryParse(layoutText.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out layout)) {
				return null;
			}

			KeyboardEntry entry = new KeyboardEntry();
			entry.DeviceName = deviceName;
			entry.Layout = layout;
			entry.Nickname = nickname;
			return entry;
		}

		private static string GetConfigFilePath() {
			string configFileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RightKeyboard");
			if(!Directory.Exists(configFileDir)) {
				Directory.CreateDirectory(configFileDir);
			}

			return Path.Combine(configFileDir, "config.txt");
		}
	}
}
