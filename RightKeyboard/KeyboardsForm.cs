using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RightKeyboard {
	/// <summary>
	/// Lists every keyboard with its input method, and lets the user change, rename or forget them
	/// </summary>
	public class KeyboardsForm : Form {
		private readonly KeyboardSettings settings;
		private readonly ScrollPanel listPanel;
		private readonly ModernButton btForgetAll;
		private readonly List<Control> rows = new List<Control>();
		private readonly Dictionary<string, KeyboardCard> cards = new Dictionary<string, KeyboardCard>(StringComparer.OrdinalIgnoreCase);
		private readonly Timer refreshTimer = new Timer();
		private readonly Timer activityTimer = new Timer();
		private string activeDevice;

		public KeyboardsForm(KeyboardSettings settings) {
			this.settings = settings;

			Theme.ApplyTo(this, Theme.Window);
			Text = "RightLayout";
			StartPosition = FormStartPosition.CenterScreen;
			Size = new Size(Theme.S(740), Theme.S(640));
			MinimumSize = new Size(Theme.S(580), Theme.S(420));

			Panel header = new Panel();
			header.Dock = DockStyle.Top;
			header.Height = Theme.S(104);

			Label title = new Label();
			title.Text = "Keyboards";
			title.Font = Theme.Title;
			title.AutoSize = true;
			title.Location = new Point(Theme.S(24), Theme.S(20));

			Label subtitle = new Label();
			subtitle.Text = "RightLayout switches to each keyboard's input method as soon as you type on it.";
			subtitle.ForeColor = Theme.SubText;
			subtitle.AutoSize = true;
			subtitle.Location = new Point(Theme.S(29), Theme.S(66));

			header.Controls.Add(title);
			header.Controls.Add(subtitle);

			Panel footer = new Panel();
			footer.Dock = DockStyle.Bottom;
			footer.Height = Theme.S(72);
			footer.Paint += delegate(object sender, PaintEventArgs e) {
				using(Pen pen = new Pen(Theme.CardBorder)) {
					e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
				}
			};

			btForgetAll = new ModernButton("Forget all keyboards", false);
			btForgetAll.Click += delegate { ForgetAll(); };

			ModernButton btDone = new ModernButton("Done", true);
			btDone.Click += delegate { Close(); };

			footer.Controls.Add(btForgetAll);
			footer.Controls.Add(btDone);
			footer.Layout += delegate {
				int top = (footer.Height - btDone.Height) / 2;
				btForgetAll.Location = new Point(Theme.S(28), top);
				btDone.Location = new Point(footer.Width - Theme.S(28) - btDone.Width, top);
			};

			listPanel = new ScrollPanel();
			listPanel.Dock = DockStyle.Fill;
			listPanel.AutoScrollMargin = new Size(0, Theme.S(20));
			listPanel.Resize += delegate { LayoutRows(); };

			// Docking goes from the last added control to the first, so the list fills what's left
			Controls.Add(listPanel);
			Controls.Add(footer);
			Controls.Add(header);

			refreshTimer.Interval = 200;
			refreshTimer.Tick += delegate {
				refreshTimer.Stop();
				RebuildList();
			};

			activityTimer.Interval = 2000;
			activityTimer.Tick += delegate {
				activityTimer.Stop();
				SetActiveDevice(null);
			};

			settings.Changed += Settings_Changed;
			RebuildList();
		}

		/// <summary>
		/// Highlights the keyboard that is being typed on
		/// </summary>
		public void NotifyKeyboardUsed(string deviceName) {
			activityTimer.Stop();
			activityTimer.Start();
			if(!string.Equals(activeDevice, deviceName, StringComparison.OrdinalIgnoreCase)) {
				SetActiveDevice(deviceName);
			}
		}

		/// <summary>
		/// Called when a keyboard is connected or disconnected
		/// </summary>
		public void NotifyDevicesChanged() {
			ScheduleRefresh();
		}

		protected override void OnFormClosed(FormClosedEventArgs e) {
			settings.Changed -= Settings_Changed;
			refreshTimer.Dispose();
			activityTimer.Dispose();
			base.OnFormClosed(e);
		}

		private void Settings_Changed(object sender, EventArgs e) {
			ScheduleRefresh();
		}

		private void ScheduleRefresh() {
			// Coalesces bursts of changes, and avoids rebuilding a card from inside its own event handler
			refreshTimer.Stop();
			refreshTimer.Start();
		}

		private void RebuildList() {
			foreach(KeyboardCard card in cards.Values) {
				if(card.IsRenaming) {
					ScheduleRefresh();
					return;
				}
			}

			Layout[] installedLayouts = RightKeyboard.Layout.GetInstalledLayouts();
			List<KeyboardGroup> saved = new List<KeyboardGroup>();
			List<KeyboardGroup> others = new List<KeyboardGroup>();
			foreach(KeyboardGroup group in KeyboardGroup.Build(settings.GetAll(), KeyboardDevices.GetConnected())) {
				if(group.IsSaved) {
					saved.Add(group);
				}
				else if(group.Kind != KeyboardKind.Remote) {
					others.Add(group);
				}
			}

			Dictionary<KeyboardGroup, string> names = GetUniqueNames(saved, others);
			saved.Sort(delegate(KeyboardGroup a, KeyboardGroup b) {
				if(a.Connected != b.Connected) {
					return a.Connected ? -1 : 1;
				}
				return string.Compare(names[a], names[b], StringComparison.CurrentCultureIgnoreCase);
			});
			others.Sort(delegate(KeyboardGroup a, KeyboardGroup b) {
				return string.Compare(names[a], names[b], StringComparison.CurrentCultureIgnoreCase);
			});

			Point scrollPosition = listPanel.AutoScrollPosition;
			listPanel.SuspendLayout();
			foreach(Control row in rows) {
				listPanel.Controls.Remove(row);
				row.Dispose();
			}
			rows.Clear();
			cards.Clear();

			rows.Add(new SectionHeader("Your keyboards", saved.Count == 0 ? null : "Changes are saved right away and kept until you forget the keyboard."));
			if(saved.Count == 0) {
				rows.Add(new MessageCard("No keyboards saved yet", "Type on any keyboard and RightLayout will ask which input method it should use."));
			}
			foreach(KeyboardGroup group in saved) {
				AddCard(group, names[group], installedLayouts);
			}

			if(others.Count > 0) {
				rows.Add(new SectionHeader("Other connected keyboards", "Choose an input method here, or just start typing on the keyboard and RightLayout will ask."));
				foreach(KeyboardGroup group in others) {
					AddCard(group, names[group], installedLayouts);
				}
			}

			foreach(Control row in rows) {
				listPanel.Controls.Add(row);
			}
			listPanel.ResumeLayout(false);
			LayoutRows();
			listPanel.AutoScrollPosition = new Point(-scrollPosition.X, -scrollPosition.Y);

			btForgetAll.Enabled = saved.Count > 0;

			KeyboardCard activeCard;
			if(activeDevice != null && cards.TryGetValue(activeDevice, out activeCard)) {
				activeCard.SetActive(true);
			}
		}

		private void AddCard(KeyboardGroup group, string displayName, Layout[] installedLayouts) {
			// Everything the card changes applies to all the devices Windows lists for this keyboard
			List<string> deviceNames = group.DeviceNames;
			KeyboardCard card = new KeyboardCard(displayName, group, installedLayouts);
			card.LayoutChosen = delegate(ushort layout) {
				settings.SetLayout(deviceNames, layout);
			};
			card.Renamed = delegate(string name) {
				settings.SetNickname(deviceNames, name == group.DetectedName ? null : name);
			};
			card.Forgotten = delegate {
				settings.Remove(deviceNames);
			};
			rows.Add(card);
			foreach(string deviceName in deviceNames) {
				cards[deviceName] = card;
			}
		}

		/// <summary>
		/// Picks the name shown for each keyboard, numbering keyboards that would otherwise look the same
		/// </summary>
		private static Dictionary<KeyboardGroup, string> GetUniqueNames(List<KeyboardGroup> saved, List<KeyboardGroup> others) {
			List<KeyboardGroup> groups = new List<KeyboardGroup>(saved);
			groups.AddRange(others);

			Dictionary<string, int> totals = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
			foreach(KeyboardGroup group in groups) {
				int count;
				totals.TryGetValue(group.DisplayName, out count);
				totals[group.DisplayName] = count + 1;
			}

			Dictionary<string, int> seen = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
			Dictionary<KeyboardGroup, string> result = new Dictionary<KeyboardGroup, string>();
			foreach(KeyboardGroup group in groups) {
				string name = group.DisplayName;
				if(totals[name] > 1) {
					int number;
					seen.TryGetValue(name, out number);
					seen[name] = ++number;
					result[group] = name + " (" + number + ")";
				}
				else {
					result[group] = name;
				}
			}
			return result;
		}

		private void LayoutRows() {
			int left = Theme.S(24);
			int width = Math.Max(Theme.S(300), listPanel.ClientSize.Width - 2 * left);
			int y = listPanel.AutoScrollPosition.Y + Theme.S(4);

			for(int i = 0; i < rows.Count; ++i) {
				Control row = rows[i];
				int height = row.Height;
				SectionHeader header = row as SectionHeader;
				MessageCard message = row as MessageCard;
				if(header != null) {
					if(i > 0) {
						y += Theme.S(24);
					}
					height = header.GetHeight(width);
				}
				else if(message != null) {
					height = message.GetHeight(width);
				}

				row.SetBounds(left, y, width, height);
				y += height + (header != null ? Theme.S(10) : Theme.S(4));
			}
		}

		private void SetActiveDevice(string deviceName) {
			KeyboardCard card;
			if(activeDevice != null && cards.TryGetValue(activeDevice, out card)) {
				card.SetActive(false);
			}
			activeDevice = deviceName;
			if(deviceName != null && cards.TryGetValue(deviceName, out card)) {
				card.SetActive(true);
				listPanel.ScrollControlIntoView(card);
			}
		}

		private void ForgetAll() {
			DialogResult answer = MessageBox.Show(this,
				"Forget all saved keyboards?\n\nRightLayout will ask for the input method again the next time you type on each keyboard.",
				"RightLayout", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
			if(answer == DialogResult.Yes) {
				settings.Clear();
			}
		}
	}

	/// <summary>
	/// One keyboard: icon, name, connection status, input method and a menu to rename or forget it
	/// </summary>
	internal class KeyboardCard : ModernCard {
		private readonly Label iconLabel = new Label();
		private readonly Label nameLabel = new Label();
		private readonly Label detailsLabel = new Label();
		private readonly ModernComboBox layoutBox = new ModernComboBox();
		private readonly ModernButton btMore;
		private readonly string details;
		private TextBox renameBox;

		public Action<ushort> LayoutChosen;
		public Action<string> Renamed;
		public Action Forgotten;

		public KeyboardCard(string displayName, KeyboardGroup group, Layout[] installedLayouts) {
			Height = Theme.S(72);
			details = KeyboardDevices.GetKindName(group.Kind) + "  ·  " + (group.Connected ? "Connected" : "Not connected");

			iconLabel.Text = KeyboardDevices.GetIconGlyph(group.Kind);
			iconLabel.Font = Theme.IconFont;
			iconLabel.ForeColor = group.Connected ? Theme.Text : Theme.DisabledText;
			iconLabel.TextAlign = ContentAlignment.MiddleCenter;

			nameLabel.Text = displayName;
			nameLabel.Font = Theme.BodyStrong;
			nameLabel.AutoEllipsis = true;
			nameLabel.UseMnemonic = false;

			detailsLabel.Text = details;
			detailsLabel.Font = Theme.Caption;
			detailsLabel.ForeColor = Theme.SubText;
			detailsLabel.AutoEllipsis = true;

			FillLayoutChoices(group, installedLayouts);
			layoutBox.SelectionChangeCommitted += delegate {
				LayoutChoice choice = layoutBox.SelectedItem as LayoutChoice;
				if(choice != null && !choice.IsPlaceholder && LayoutChosen != null) {
					LayoutChosen(choice.Identifier);
				}
			};

			Controls.Add(iconLabel);
			Controls.Add(nameLabel);
			Controls.Add(detailsLabel);
			Controls.Add(layoutBox);

			if(group.IsSaved) {
				btMore = new ModernButton("", false);		// More
				btMore.Font = Theme.SmallIconFont;
				btMore.Size = new Size(Theme.S(32), Theme.S(32));
				btMore.Click += delegate { ShowMenu(); };
				Controls.Add(btMore);
			}
		}

		public bool IsRenaming {
			get {
				return renameBox != null;
			}
		}

		public void SetActive(bool active) {
			Highlighted = active;
			detailsLabel.Text = active ? details + "  ·  Typing now" : details;
			detailsLabel.ForeColor = active ? Theme.Accent : Theme.SubText;
		}

		protected override void OnLayout(LayoutEventArgs e) {
			base.OnLayout(e);

			int right = Width - Theme.S(16);
			if(btMore != null) {
				btMore.Location = new Point(right - btMore.Width, (Height - btMore.Height) / 2);
				right = btMore.Left - Theme.S(8);
			}

			int comboWidth = Math.Min(Theme.S(250), Math.Max(Theme.S(170), Width / 3));
			layoutBox.SetBounds(right - comboWidth, (Height - layoutBox.Height) / 2, comboWidth, layoutBox.Height);

			iconLabel.SetBounds(Theme.S(14), (Height - Theme.S(36)) / 2, Theme.S(36), Theme.S(36));

			int textLeft = Theme.S(62);
			int textWidth = Math.Max(Theme.S(40), layoutBox.Left - Theme.S(16) - textLeft);
			nameLabel.SetBounds(textLeft, Height / 2 - Theme.S(22), textWidth, Theme.S(22));
			detailsLabel.SetBounds(textLeft, Height / 2 + Theme.S(1), textWidth, Theme.S(20));
			if(renameBox != null) {
				renameBox.SetBounds(textLeft - Theme.S(2), nameLabel.Top - Theme.S(2), textWidth, renameBox.Height);
			}
		}

		private void FillLayoutChoices(KeyboardGroup group, Layout[] installedLayouts) {
			// A single input method is known only when every saved part of the keyboard agrees
			bool known = group.IsSaved && !group.IsMixed;
			LayoutChoice selected = null;
			if(!known) {
				selected = new LayoutChoice(KeyboardSettings.NoSwitch, group.IsSaved ? "Mixed, choose one" : "Choose input method", true);
				layoutBox.Items.Add(selected);
			}

			bool found = false;
			foreach(Layout layout in installedLayouts) {
				LayoutChoice choice = new LayoutChoice(layout.Identifier, layout.Name, false);
				layoutBox.Items.Add(choice);
				if(known && group.Layout == layout.Identifier) {
					selected = choice;
					found = true;
				}
			}

			if(known && !found && group.Layout != KeyboardSettings.NoSwitch) {
				selected = new LayoutChoice(group.Layout, RightKeyboard.Layout.GetDisplayName(group.Layout) + " (not installed)", false);
				layoutBox.Items.Add(selected);
			}

			LayoutChoice noSwitch = new LayoutChoice(KeyboardSettings.NoSwitch, "Don't switch", false);
			layoutBox.Items.Add(noSwitch);
			if(known && group.Layout == KeyboardSettings.NoSwitch) {
				selected = noSwitch;
			}

			layoutBox.SelectedItem = selected;
			layoutBox.DropDownWidth = Theme.S(280);
		}

		private void ShowMenu() {
			ContextMenuStrip menu = new ContextMenuStrip();
			menu.Items.Add("Rename", null, delegate { BeginRename(); });
			menu.Items.Add("Forget this keyboard", null, delegate {
				if(Forgotten != null) {
					Forgotten();
				}
			});
			Theme.StyleMenu(menu);
			menu.Closed += delegate { BeginInvoke((MethodInvoker)delegate { menu.Dispose(); }); };
			menu.Show(btMore, new Point(btMore.Width - menu.PreferredSize.Width, btMore.Height + Theme.S(2)));
		}

		private void BeginRename() {
			if(renameBox != null) {
				return;
			}

			renameBox = new TextBox();
			renameBox.BorderStyle = BorderStyle.FixedSingle;
			renameBox.BackColor = Theme.Control;
			renameBox.ForeColor = Theme.Text;
			renameBox.Font = Theme.BodyStrong;
			renameBox.MaxLength = 64;
			renameBox.Text = nameLabel.Text;
			renameBox.KeyDown += delegate(object sender, KeyEventArgs e) {
				if(e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape) {
					e.SuppressKeyPress = true;
					EndRename(e.KeyCode == Keys.Enter);
				}
			};
			renameBox.LostFocus += delegate { EndRename(true); };

			nameLabel.Visible = false;
			Controls.Add(renameBox);
			renameBox.BringToFront();
			PerformLayout();
			renameBox.Focus();
			renameBox.SelectAll();
		}

		private void EndRename(bool commit) {
			if(renameBox == null) {
				return;
			}

			TextBox box = renameBox;
			renameBox = null;
			string newName = box.Text.Trim();
			box.Visible = false;
			nameLabel.Visible = true;
			// The box is still raising the event that brought us here, so dispose of it afterwards
			BeginInvoke((MethodInvoker)delegate { box.Dispose(); });

			if(commit && newName != nameLabel.Text && Renamed != null) {
				nameLabel.Text = newName.Length == 0 ? nameLabel.Text : newName;
				Renamed(newName);
			}
		}

		private class LayoutChoice {
			public readonly ushort Identifier;
			public readonly bool IsPlaceholder;
			private readonly string text;

			public LayoutChoice(ushort identifier, string text, bool isPlaceholder) {
				Identifier = identifier;
				IsPlaceholder = isPlaceholder;
				this.text = text;
			}

			public override string ToString() {
				return text;
			}
		}
	}

	/// <summary>
	/// Section title with an optional description below it
	/// </summary>
	internal class SectionHeader : Control {
		private readonly string title;
		private readonly string description;

		public SectionHeader(string title, string description) {
			this.title = title;
			this.description = description;
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			BackColor = Theme.Window;
		}

		public int GetHeight(int width) {
			int height = TextRenderer.MeasureText(title, Theme.BodyStrong).Height;
			if(description != null) {
				height += Theme.S(2) + MeasureDescription(width).Height;
			}
			return height;
		}

		private Size MeasureDescription(int width) {
			return TextRenderer.MeasureText(description, Theme.Caption, new Size(width, int.MaxValue), TextFormatFlags.WordBreak);
		}

		protected override void OnPaint(PaintEventArgs e) {
			e.Graphics.Clear(BackColor);
			int titleHeight = TextRenderer.MeasureText(title, Theme.BodyStrong).Height;
			TextRenderer.DrawText(e.Graphics, title, Theme.BodyStrong, new Rectangle(0, 0, Width, titleHeight), Theme.Text, TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
			if(description != null) {
				Rectangle bounds = new Rectangle(0, titleHeight + Theme.S(2), Width, Height - titleHeight);
				TextRenderer.DrawText(e.Graphics, description, Theme.Caption, bounds, Theme.SubText, TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
			}
		}
	}

	/// <summary>
	/// Card showing a short message, e.g. when no keyboard is saved yet
	/// </summary>
	internal class MessageCard : ModernCard {
		private readonly string title;
		private readonly string message;

		public MessageCard(string title, string message) {
			this.title = title;
			this.message = message;
		}

		public int GetHeight(int width) {
			int padding = Theme.S(20);
			int textWidth = width - 2 * padding;
			return 2 * padding + TextRenderer.MeasureText(title, Theme.BodyStrong).Height + Theme.S(4)
				+ TextRenderer.MeasureText(message, Theme.Body, new Size(textWidth, int.MaxValue), TextFormatFlags.WordBreak).Height;
		}

		protected override void OnPaint(PaintEventArgs e) {
			int padding = Theme.S(20);
			int titleHeight = TextRenderer.MeasureText(title, Theme.BodyStrong).Height;
			TextRenderer.DrawText(e.Graphics, title, Theme.BodyStrong, new Rectangle(padding, padding, Width - 2 * padding, titleHeight), Theme.Text,
				TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
			Rectangle bounds = new Rectangle(padding, padding + titleHeight + Theme.S(4), Width - 2 * padding, Height - padding - titleHeight);
			TextRenderer.DrawText(e.Graphics, message, Theme.Body, bounds, Theme.SubText, TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
		}
	}
}
