using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using RightLayout.Win32;

namespace RightLayout {
	/// <summary>
	/// Runs RightLayout from the notification area: watches which keyboard each key press comes from
	/// and switches to the input method chosen for it
	/// </summary>
	public class TrayApp : ApplicationContext {
		private readonly KeyboardSettings settings = new KeyboardSettings();
		private readonly KeyboardListener listener;
		private readonly NotifyIcon trayIcon = new NotifyIcon();

		// Keyboards whose prompt was closed without choosing; asked about again after a restart
		private readonly HashSet<string> skippedThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Set while the new-keyboard prompt is open, so typing in it doesn't open another one
		private bool prompting;

		private KeyboardsForm keyboardsForm;

		public TrayApp() {
			settings.Load();

			listener = new KeyboardListener();
			listener.KeyPressed += OnKeyPressed;
			listener.KeyboardsChanged += delegate {
				if(keyboardsForm != null) {
					keyboardsForm.NotifyDevicesChanged();
				}
			};

			ContextMenuStrip menu = new ContextMenuStrip();
			ToolStripMenuItem keyboardsItem = new ToolStripMenuItem("&Keyboards...", null, delegate { ShowKeyboards(); });
			keyboardsItem.Font = new Font("Segoe UI Semibold", 9.75f);
			menu.Items.Add(keyboardsItem);
			menu.Items.Add(new ToolStripMenuItem("Open app &location", null, delegate { AppLocations.ShowApp(); }));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(new ToolStripMenuItem("&Forget all keyboards...", null, delegate { ForgetAll(); }));
			menu.Items.Add(new ToolStripMenuItem("&Exit", null, delegate { ExitThread(); }));
			Theme.StyleMenu(menu);

			trayIcon.Icon = Theme.GetAppIcon(SystemInformation.SmallIconSize.Width);
			trayIcon.Text = "RightLayout";
			trayIcon.ContextMenuStrip = menu;
			trayIcon.MouseClick += delegate(object sender, MouseEventArgs e) {
				if(e.Button == MouseButtons.Left) {
					ShowKeyboards();
				}
			};
			trayIcon.Visible = true;
		}

		protected override void ExitThreadCore() {
			if(keyboardsForm != null) {
				keyboardsForm.Close();
			}
			// Hide the icon now, or it lingers in the notification area until the mouse passes over it
			trayIcon.Visible = false;
			trayIcon.Dispose();
			listener.DestroyHandle();
			base.ExitThreadCore();
		}

		private void OnKeyPressed(string deviceName) {
			if(prompting) {
				return;
			}
			if(keyboardsForm != null) {
				keyboardsForm.NotifyKeyboardUsed(deviceName);
			}

			ushort language;
			if(!TryGetLanguage(deviceName, out language) || language == KeyboardSettings.NoSwitch) {
				return;
			}
			if(API.GetForegroundLanguage() != language) {
				API.SwitchLanguage(language);
			}
		}

		/// <summary>
		/// Finds the input method for a keyboard: the saved one, the one already chosen for another part of
		/// the same keyboard, or the user's answer to the new-keyboard prompt. Fails when the prompt is closed.
		/// </summary>
		private bool TryGetLanguage(string deviceName, out ushort language) {
			KeyboardEntry saved = settings.Get(deviceName);
			if(saved != null) {
				language = saved.Layout;
				return true;
			}

			language = KeyboardSettings.NoSwitch;
			if(skippedThisSession.Contains(deviceName)) {
				return false;
			}
			if(!settings.TryGetGroupLayout(deviceName, out language) && !Prompt(deviceName, out language)) {
				skippedThisSession.Add(deviceName);
				return false;
			}
			settings.SetLayout(deviceName, language);
			return true;
		}

		private bool Prompt(string deviceName, out ushort language) {
			language = KeyboardSettings.NoSwitch;
			prompting = true;
			try {
				using(LayoutSelectionDialog dialog = new LayoutSelectionDialog(deviceName)) {
					if(dialog.ShowDialog() != DialogResult.OK) {
						return false;
					}
					language = dialog.SelectedLayout;
					return true;
				}
			}
			finally {
				prompting = false;
			}
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

		private void ForgetAll() {
			DialogResult answer = MessageBox.Show(
				"Forget all saved keyboards?\n\nRightLayout will ask for the input method again the next time you type on each keyboard.",
				"RightLayout", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
			if(answer == DialogResult.Yes) {
				skippedThisSession.Clear();
				settings.Clear();
			}
		}

		/// <summary>
		/// An invisible window that Windows tells about key presses on every keyboard, even while other apps have focus
		/// </summary>
		private class KeyboardListener : NativeWindow {
			/// <summary>
			/// Raised for each key press with the raw input device name of the keyboard it came from
			/// </summary>
			public event Action<string> KeyPressed;

			/// <summary>
			/// Raised when a keyboard connects or disconnects
			/// </summary>
			public event EventHandler KeyboardsChanged;

			public KeyboardListener() {
				CreateHandle(new CreateParams());
				API.ListenToKeyboards(Handle);
			}

			protected override void WndProc(ref Message m) {
				if(m.Msg == API.WM_INPUT) {
					OnInput(m.LParam);
				}
				else if(m.Msg == API.WM_INPUT_DEVICE_CHANGE && KeyboardsChanged != null) {
					KeyboardsChanged(this, EventArgs.Empty);
				}
				// Also for WM_INPUT, which Windows needs to free the input data
				base.WndProc(ref m);
			}

			private void OnInput(IntPtr rawInput) {
				// This runs for every key press, so one unexpected failure must not stop layout switching
				try {
					// No name: injected input (on-screen keyboard, remote desktop) or a device that just disconnected
					string deviceName = API.GetDeviceName(API.GetInputDevice(rawInput));
					if(!string.IsNullOrEmpty(deviceName) && KeyPressed != null) {
						KeyPressed(deviceName);
					}
				}
				catch(Exception err) {
					Debug.WriteLine("Could not handle keyboard input: " + err);
				}
			}
		}
	}
}
