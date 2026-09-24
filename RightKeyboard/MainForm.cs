using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using RightKeyboard.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;

namespace RightKeyboard {
	public partial class MainForm : Form {
		private bool selectingLayout = false;

		// Keyed by raw input device name, which (unlike the device handle) stays the same across reconnects
		private readonly KeyboardSettings settings = new KeyboardSettings();

		// Keyboards whose prompt was dismissed; asked again after a restart
		private readonly Dictionary<string, bool> skippedThisSession = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

		private KeyboardsForm keyboardsForm;

		public MainForm() {
			InitializeComponent();

			// DEVNOTIFY sends WM_INPUT_DEVICE_CHANGE when keyboards connect or disconnect
			RAWINPUTDEVICE rawInputDevice = new RAWINPUTDEVICE(1, 6, API.RIDEV_INPUTSINK | API.RIDEV_DEVNOTIFY, this);
			bool ok = API.RegisterRawInputDevices(rawInputDevice);
			if(!ok) {
				throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
			}
			Debug.Assert(ok);

			WindowState = FormWindowState.Minimized;

			Theme.StyleMenu(contextMenuStrip);
			settings.Load();
		}

		protected override void OnClosed(EventArgs e) {
			if(keyboardsForm != null) {
				keyboardsForm.Close();
			}
			notifyIcon.Visible = false;
			base.OnClosed(e);
		}

		private void ShowKeyboards() {
			if(keyboardsForm == null) {
				keyboardsForm = new KeyboardsForm(settings);
				keyboardsForm.FormClosed += delegate { keyboardsForm = null; };
				keyboardsForm.Show();
			}
			if(keyboardsForm.WindowState == FormWindowState.Minimized) {
				keyboardsForm.WindowState = FormWindowState.Normal;
			}
			keyboardsForm.Activate();
		}

		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
			Hide();
		}

		protected override void WndProc(ref Message message) {
			switch(message.Msg) {
				case API.WM_INPUT:
					if(!selectingLayout) {
						ProcessInputMessage(message);
					}
					break;

				case API.WM_POWERBROADCAST:
					ProcessPowerMessage(message);
					break;

				case API.WM_INPUT_DEVICE_CHANGE:
					if(keyboardsForm != null) {
						keyboardsForm.NotifyDevicesChanged();
					}
					base.WndProc(ref message);
					break;

				default:
					base.WndProc(ref message);
					break;
			}
		}

		private void ProcessPowerMessage(Message message) {
			switch(message.WParam.ToInt32()) {
				case API.PBT_APMQUERYSUSPEND:
					Debug.WriteLine("PBT_APMQUERYSUSPEND");
					break;

				case API.PBT_APMQUERYSTANDBY:
					Debug.WriteLine("PBT_APMQUERYSTANDBY");
					break;

				case API.PBT_APMQUERYSUSPENDFAILED:
					Debug.WriteLine("PBT_APMQUERYSUSPENDFAILED");
					break;

				case API.PBT_APMQUERYSTANDBYFAILED:
					Debug.WriteLine("PBT_APMQUERYSTANDBYFAILED");
					break;

				case API.PBT_APMSUSPEND:
					Debug.WriteLine("PBT_APMSUSPEND");
					break;

				case API.PBT_APMSTANDBY:
					Debug.WriteLine("PBT_APMSTANDBY");
					break;

				case API.PBT_APMRESUMECRITICAL:
					Debug.WriteLine("PBT_APMRESUMECRITICAL");
					break;

				case API.PBT_APMRESUMESUSPEND:
					Debug.WriteLine("PBT_APMRESUMESUSPEND");
					break;

				case API.PBT_APMRESUMESTANDBY:
					Debug.WriteLine("PBT_APMRESUMESTANDBY");
					break;

				case API.PBT_APMBATTERYLOW:
					Debug.WriteLine("PBT_APMBATTERYLOW");
					break;

				case API.PBT_APMPOWERSTATUSCHANGE:
					Debug.WriteLine("PBT_APMPOWERSTATUSCHANGE");
					break;

				case API.PBT_APMOEMEVENT:
					Debug.WriteLine("PBT_APMOEMEVENT");
					break;

				case API.PBT_APMRESUMEAUTOMATIC:
					Debug.WriteLine("PBT_APMRESUMEAUTOMATIC");
					break;

				default:
					break;
			}
		}

		private void ProcessInputMessage(Message message) {
			RAWINPUTHEADER header;
			uint result = API.GetRawInputData(message.LParam, API.RID_HEADER, out header);
			Debug.Assert(result != uint.MaxValue);
			if(result == uint.MaxValue) {
				throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
			}
			ValidateCurrentDevice(header.hDevice);
		}

		private void ValidateCurrentDevice(IntPtr hCurrentDevice) {
			string deviceName = API.GetRawInputDeviceName(hCurrentDevice);
			if (string.IsNullOrEmpty(deviceName)) {
				// Injected input (on-screen keyboard, remote desktop) or a device that just disconnected
				return;
			}

			if (keyboardsForm != null) {
				keyboardsForm.NotifyKeyboardUsed(deviceName);
			}

			ushort layout;
			KeyboardEntry entry = settings.Get(deviceName);
			if (entry != null) {
				layout = entry.Layout;
			}
			else {
				if (skippedThisSession.ContainsKey(deviceName)) {
					return;
				}

				// Another part of this keyboard (e.g. its media keys) may already have an input method
				if (!settings.TryGetGroupLayout(deviceName, out layout) && !AskForLayout(deviceName, out layout)) {
					skippedThisSession[deviceName] = true;
					return;
				}
				settings.SetLayout(deviceName, layout);
			}

			if (layout == KeyboardSettings.NoSwitch) {
				return;
			}

			ushort currentSysLayout = unchecked((ushort)API.GetKeyboardLayout().ToInt64());

			if (currentSysLayout != layout)
			{
				SetCurrentLayout(layout);
				SetDefaultLayout(layout);
			}
		}

		private bool AskForLayout(string deviceName, out ushort layout) {
			layout = KeyboardSettings.NoSwitch;
			selectingLayout = true;
			try {
				using (LayoutSelectionDialog dialog = new LayoutSelectionDialog(deviceName)) {
					if (dialog.ShowDialog() != DialogResult.OK) {
						return false;
					}
					layout = dialog.SelectedLayout;
					return true;
				}
			}
			finally {
				selectingLayout = false;
			}
		}

		private void SetCurrentLayout(ushort layout) {
				uint recipients = API.BSM_APPLICATIONS;
				API.BroadcastSystemMessage(API.BSF_POSTMESSAGE, ref recipients, API.WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, new IntPtr(layout));
		}

		private void SetDefaultLayout(ushort layout) {
			//IntPtr hkl = API.LoadKeyboardLayout(layout, 0);
			//Debug.Assert(hkl != IntPtr.Zero);
			//if(hkl == IntPtr.Zero) {
			//    throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
			//}

			IntPtr hkl = new IntPtr(unchecked((int)((uint)layout << 16 | (uint)layout)));

			bool ok = API.SystemParametersInfo(API.SPI_SETDEFAULTINPUTLANG, 0, new IntPtr[] { hkl }, API.SPIF_SENDCHANGE);
			//Debug.Assert(ok);
			//if(!ok) {
			//	throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
			//}
		}

		private void keyboardsToolStripMenuItem_Click(object sender, EventArgs e) {
			ShowKeyboards();
		}

		private void notifyIcon_MouseClick(object sender, MouseEventArgs e) {
			if(e.Button == MouseButtons.Left) {
				ShowKeyboards();
			}
		}

		private void clearToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DialogResult answer = MessageBox.Show(
				"Forget all saved keyboards?\n\nRightKeyboard will ask for the input method again the next time you type on each keyboard.",
				"RightKeyboard", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
			if(answer == DialogResult.Yes) {
				skippedThisSession.Clear();
				settings.Clear();
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
			Close();
		}
	}
}