using System;
using System.Drawing;
using System.Windows.Forms;
using RightKeyboard.Win32;

namespace RightKeyboard {
	/// <summary>
	/// Asks which input method a newly detected keyboard should use.
	/// OK: SelectedLayout holds the choice (possibly KeyboardSettings.NoSwitch). Cancel: ask again next session.
	/// </summary>
	public class LayoutSelectionDialog : Form {
		private readonly Layout[] installedLayouts = RightKeyboard.Layout.GetInstalledLayouts();
		private readonly SearchBox searchBox;
		private readonly ModernListBox lbLayouts;
		private readonly ModernButton btOk;
		private ushort selectedLayout = KeyboardSettings.NoSwitch;

		public LayoutSelectionDialog(string deviceName) {
			KeyboardDeviceInfo device = KeyboardDevices.Get(deviceName);

			Theme.ApplyTo(this, Theme.Card);
			Text = "RightKeyboard";
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			StartPosition = FormStartPosition.CenterScreen;
			TopMost = true;
			ShowInTaskbar = true;
			ClientSize = new Size(Theme.S(460), Theme.S(580));

			int margin = Theme.S(24);
			int width = ClientSize.Width - 2 * margin;
			int textLeft = margin + Theme.S(56);

			Label icon = new Label();
			icon.Text = KeyboardDevices.GetIconGlyph(device.Kind);
			icon.Font = Theme.LargeIconFont;
			icon.ForeColor = Theme.Accent;
			icon.TextAlign = ContentAlignment.MiddleCenter;
			icon.SetBounds(margin, margin, Theme.S(44), Theme.S(44));

			Label title = new Label();
			title.Text = "New keyboard detected";
			title.Font = Theme.Subtitle;
			title.AutoSize = true;
			title.Location = new Point(textLeft - Theme.S(3), margin - Theme.S(4));

			Label deviceLabel = new Label();
			deviceLabel.Text = device.DisplayName + "  ·  " + KeyboardDevices.GetKindName(device.Kind);
			deviceLabel.ForeColor = Theme.SubText;
			deviceLabel.AutoEllipsis = true;
			deviceLabel.UseMnemonic = false;
			deviceLabel.SetBounds(textLeft, margin + Theme.S(28), ClientSize.Width - textLeft - margin, Theme.S(22));

			Label description = new Label();
			description.Text = "Choose the input method to switch to whenever you type on this keyboard. You can change it later from the tray icon.";
			description.SetBounds(margin, Theme.S(92), width, Theme.S(44));

			searchBox = new SearchBox("Search input methods");
			searchBox.SetBounds(margin, Theme.S(146), width, searchBox.Height);
			searchBox.TextBox.TextChanged += delegate { FilterLayouts(); };
			searchBox.TextBox.KeyDown += SearchBox_KeyDown;

			Panel footer = new Panel();
			footer.Dock = DockStyle.Bottom;
			footer.Height = Theme.S(80);
			footer.BackColor = Theme.Window;
			footer.Paint += delegate(object sender, PaintEventArgs e) {
				using(Pen pen = new Pen(Theme.CardBorder)) {
					e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
				}
			};

			int listTop = Theme.S(194);
			ModernCard listCard = new ModernCard();
			listCard.Padding = new Padding(Theme.S(4));
			listCard.SetBounds(margin, listTop, width, ClientSize.Height - footer.Height - listTop - Theme.S(20));

			lbLayouts = new ModernListBox();
			lbLayouts.Dock = DockStyle.Fill;
			lbLayouts.SelectedIndexChanged += delegate { btOk.Enabled = lbLayouts.SelectedIndex >= 0; };
			lbLayouts.DoubleClick += delegate {
				if(btOk.Enabled) {
					Accept();
				}
			};
			listCard.Controls.Add(lbLayouts);

			ModernButton btSkip = new ModernButton("Don't switch for this keyboard", false);
			btSkip.Location = new Point(margin, (footer.Height - btSkip.Height) / 2);
			btSkip.Click += delegate {
				selectedLayout = KeyboardSettings.NoSwitch;
				DialogResult = DialogResult.OK;
			};

			btOk = new ModernButton("Use this input method", true);
			btOk.Location = new Point(ClientSize.Width - margin - btOk.Width, (footer.Height - btOk.Height) / 2);
			btOk.Click += delegate { Accept(); };

			footer.Controls.Add(btSkip);
			footer.Controls.Add(btOk);

			Controls.Add(icon);
			Controls.Add(title);
			Controls.Add(deviceLabel);
			Controls.Add(description);
			Controls.Add(searchBox);
			Controls.Add(listCard);
			Controls.Add(footer);

			AcceptButton = btOk;

			FilterLayouts();
			SelectCurrentLayout();
		}

		/// <summary>
		/// Gets the chosen language identifier, or KeyboardSettings.NoSwitch
		/// </summary>
		public ushort SelectedLayout {
			get {
				return selectedLayout;
			}
		}

		protected override void OnShown(EventArgs e) {
			base.OnShown(e);
			Activate();
			searchBox.TextBox.Focus();
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
			if(keyData == Keys.Escape) {
				DialogResult = DialogResult.Cancel;
				return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void Accept() {
			Layout layout = lbLayouts.SelectedItem as Layout;
			if(layout != null) {
				selectedLayout = layout.Identifier;
				DialogResult = DialogResult.OK;
			}
		}

		private void SelectCurrentLayout() {
			ushort current = unchecked((ushort)API.GetKeyboardLayout().ToInt64());
			for(int i = 0; i < lbLayouts.Items.Count; ++i) {
				if(((Layout)lbLayouts.Items[i]).Identifier == current) {
					lbLayouts.SelectedIndex = i;
					return;
				}
			}
		}

		private void FilterLayouts() {
			string query = searchBox.TextBox.Text.Trim();
			Layout previous = lbLayouts.SelectedItem as Layout;

			lbLayouts.BeginUpdate();
			lbLayouts.Items.Clear();
			foreach(Layout layout in installedLayouts) {
				if(query.Length == 0 || layout.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) {
					lbLayouts.Items.Add(layout);
				}
			}
			if(previous != null && lbLayouts.Items.Contains(previous)) {
				lbLayouts.SelectedItem = previous;
			}
			else if(lbLayouts.Items.Count > 0) {
				lbLayouts.SelectedIndex = 0;
			}
			lbLayouts.EndUpdate();

			btOk.Enabled = lbLayouts.SelectedIndex >= 0;
		}

		private void SearchBox_KeyDown(object sender, KeyEventArgs e) {
			// Arrow keys move through the list while typing in the search box
			if(e.KeyCode == Keys.Down && lbLayouts.SelectedIndex < lbLayouts.Items.Count - 1) {
				lbLayouts.SelectedIndex++;
				e.Handled = true;
			}
			else if(e.KeyCode == Keys.Up && lbLayouts.SelectedIndex > 0) {
				lbLayouts.SelectedIndex--;
				e.Handled = true;
			}
		}
	}
}
