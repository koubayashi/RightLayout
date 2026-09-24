using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RightKeyboard.Win32;

namespace RightKeyboard {
	/// <summary>
	/// Rounded button, either filled with the accent color (primary) or neutral
	/// </summary>
	internal class ModernButton : Button {
		private readonly bool primary;
		private bool hover;
		private bool pressed;

		public ModernButton(string text, bool primary) {
			this.primary = primary;
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			Text = text;
			Font = Theme.Body;
			Size = new Size(Math.Max(Theme.S(96), TextRenderer.MeasureText(text, Theme.Body).Width + Theme.S(32)), Theme.S(32));
		}

		protected override void OnMouseEnter(EventArgs e) {
			hover = true;
			Invalidate();
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e) {
			hover = false;
			pressed = false;
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnMouseDown(MouseEventArgs e) {
			pressed = e.Button == MouseButtons.Left;
			Invalidate();
			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e) {
			pressed = false;
			Invalidate();
			base.OnMouseUp(e);
		}

		protected override void OnEnabledChanged(EventArgs e) {
			Invalidate();
			base.OnEnabledChanged(e);
		}

		protected override void OnPaint(PaintEventArgs e) {
			Color fill, border, text;
			if(!Enabled) {
				fill = primary ? Theme.ControlBorder : Theme.Control;
				border = Theme.ControlBorder;
				text = Theme.DisabledText;
			}
			else if(primary) {
				fill = pressed ? Theme.AccentPressed : hover ? Theme.AccentHover : Theme.Accent;
				border = fill;
				text = Theme.OnAccent;
			}
			else {
				fill = pressed ? Theme.ControlPressed : hover ? Theme.ControlHover : Theme.Control;
				border = Theme.ControlBorder;
				text = pressed ? Theme.SubText : Theme.Text;
			}

			Theme.PaintBox(e.Graphics, this, fill, border, Theme.S(4));
			TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, text,
				TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
			if(Focused && ShowFocusCues) {
				Theme.PaintFocusRing(e.Graphics, this, Theme.S(4));
			}
		}
	}

	/// <summary>
	/// Rounded panel used for each keyboard; can be highlighted with the accent color
	/// </summary>
	internal class ModernCard : Panel {
		private bool highlighted;

		public ModernCard() {
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			BackColor = Theme.Card;
		}

		public bool Highlighted {
			get {
				return highlighted;
			}
			set {
				if(highlighted != value) {
					highlighted = value;
					Invalidate();
				}
			}
		}

		protected override void OnPaintBackground(PaintEventArgs e) {
			Theme.PaintBox(e.Graphics, this, Theme.Card, Theme.CardBorder, Theme.S(6));
			if(highlighted) {
				float width = Theme.S(2);
				RectangleF bounds = new RectangleF(width / 2, width / 2, Width - width - 0.5f, Height - width - 0.5f);
				using(GraphicsPath path = Theme.RoundedRectangle(bounds, Theme.S(6))) {
					using(Pen pen = new Pen(Theme.Accent, width)) {
						e.Graphics.DrawPath(pen, path);
					}
				}
			}
		}
	}

	/// <summary>
	/// Panel that scrolls its content without flicker
	/// </summary>
	internal class ScrollPanel : Panel {
		public ScrollPanel() {
			DoubleBuffered = true;
			AutoScroll = true;
			Theme.UseDarkScrollbars(this, "DarkMode_Explorer");
		}
	}

	/// <summary>
	/// A drop-down list drawn in the current theme
	/// </summary>
	internal class ModernComboBox : ComboBox {
		private bool hover;

		public ModernComboBox() {
			DropDownStyle = ComboBoxStyle.DropDownList;
			DrawMode = DrawMode.OwnerDrawFixed;
			FlatStyle = FlatStyle.Flat;
			SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			Font = Theme.Body;
			ItemHeight = Theme.S(28);
			BackColor = Theme.Control;
			ForeColor = Theme.Text;
			MaxDropDownItems = 12;
			Theme.UseDarkScrollbars(this, "DarkMode_CFD");
		}

		protected override void WndProc(ref Message m) {
			// Let the wheel scroll the page instead of silently changing the selection
			if(m.Msg == API.WM_MOUSEWHEEL && !DroppedDown && Parent != null) {
				API.SendMessage(Parent.Handle, m.Msg, m.WParam, m.LParam);
				return;
			}
			base.WndProc(ref m);
		}

		protected override void OnMouseEnter(EventArgs e) {
			hover = true;
			Invalidate();
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e) {
			hover = false;
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnDropDownClosed(EventArgs e) {
			Invalidate();
			base.OnDropDownClosed(e);
		}

		protected override void OnSelectedIndexChanged(EventArgs e) {
			Invalidate();
			base.OnSelectedIndexChanged(e);
		}

		protected override void OnGotFocus(EventArgs e) {
			Invalidate();
			base.OnGotFocus(e);
		}

		protected override void OnLostFocus(EventArgs e) {
			Invalidate();
			base.OnLostFocus(e);
		}

		protected override void OnPaint(PaintEventArgs e) {
			Color fill = DroppedDown ? Theme.ControlPressed : hover ? Theme.ControlHover : Theme.Control;
			Theme.PaintBox(e.Graphics, this, fill, Theme.ControlBorder, Theme.S(4));

			string text = SelectedIndex >= 0 ? GetItemText(SelectedItem) : "";
			Rectangle textBounds = new Rectangle(Theme.S(11), 0, Width - Theme.S(11) - Theme.S(34), Height);
			TextRenderer.DrawText(e.Graphics, text, Font, textBounds, Enabled ? Theme.Text : Theme.DisabledText,
				TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

			Rectangle chevronBounds = new Rectangle(Width - Theme.S(34), 0, Theme.S(24), Height);
			TextRenderer.DrawText(e.Graphics, "", Theme.SmallIconFont, chevronBounds, Theme.SubText,
				TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

			if(Focused && ShowFocusCues) {
				Theme.PaintFocusRing(e.Graphics, this, Theme.S(4));
			}
		}

		protected override void OnDrawItem(DrawItemEventArgs e) {
			if(e.Index < 0 || (e.State & DrawItemState.ComboBoxEdit) != 0) {
				return;
			}

			bool selected = (e.State & DrawItemState.Selected) != 0;
			using(SolidBrush brush = new SolidBrush(selected ? Theme.ItemSelected : Theme.Control)) {
				e.Graphics.FillRectangle(brush, e.Bounds);
			}
			Rectangle textBounds = new Rectangle(e.Bounds.X + Theme.S(10), e.Bounds.Y, e.Bounds.Width - Theme.S(14), e.Bounds.Height);
			TextRenderer.DrawText(e.Graphics, GetItemText(Items[e.Index]), Font, textBounds, Theme.Text,
				TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
		}
	}

	/// <summary>
	/// Rounded text box with a search icon and placeholder text
	/// </summary>
	internal class SearchBox : Panel {
		private readonly TextBox textBox;
		private readonly string placeholder;

		public SearchBox(string placeholder) {
			this.placeholder = placeholder;
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			BackColor = Theme.Control;
			Height = Theme.S(34);
			Cursor = Cursors.IBeam;

			textBox = new TextBox();
			textBox.BorderStyle = BorderStyle.None;
			textBox.BackColor = Theme.Control;
			textBox.ForeColor = Theme.Text;
			textBox.Font = Theme.Body;
			textBox.GotFocus += delegate { Invalidate(); };
			textBox.LostFocus += delegate { Invalidate(); };
			textBox.HandleCreated += delegate { API.SendMessage(textBox.Handle, API.EM_SETCUEBANNER, new IntPtr(1), this.placeholder); };
			Controls.Add(textBox);

			Click += delegate { textBox.Focus(); };
		}

		public TextBox TextBox {
			get {
				return textBox;
			}
		}

		protected override void OnLayout(LayoutEventArgs e) {
			base.OnLayout(e);
			if(textBox == null) {
				// Setting Height in the constructor lays out before the text box exists
				return;
			}
			int left = Theme.S(36);
			textBox.SetBounds(left, (Height - textBox.Height) / 2, Width - left - Theme.S(10), textBox.Height);
		}

		protected override void OnPaintBackground(PaintEventArgs e) {
			Theme.PaintBox(e.Graphics, this, Theme.Control, Theme.ControlBorder, Theme.S(4));
			if(textBox.Focused) {
				// Windows 11 marks the focused text box with an accent underline
				using(SolidBrush brush = new SolidBrush(Theme.Accent)) {
					e.Graphics.FillRectangle(brush, Theme.S(2), Height - Theme.S(2), Width - Theme.S(4), Theme.S(2));
				}
			}
			TextRenderer.DrawText(e.Graphics, "", Theme.SmallIconFont, new Rectangle(Theme.S(8), 0, Theme.S(24), Height), Theme.SubText,
				TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
		}
	}

	/// <summary>
	/// List with roomy rows, hover highlight and an accent marker on the selected row
	/// </summary>
	internal class ModernListBox : ListBox {
		private int hoverIndex = -1;

		public ModernListBox() {
			DrawMode = DrawMode.OwnerDrawFixed;
			ItemHeight = Theme.S(36);
			BorderStyle = BorderStyle.None;
			IntegralHeight = false;
			BackColor = Theme.Card;
			ForeColor = Theme.Text;
			Font = Theme.Body;
			Theme.UseDarkScrollbars(this, "DarkMode_Explorer");
		}

		protected override void OnMouseMove(MouseEventArgs e) {
			base.OnMouseMove(e);
			int index = IndexFromPoint(e.Location);
			if(index != hoverIndex) {
				InvalidateItem(hoverIndex);
				hoverIndex = index;
				InvalidateItem(hoverIndex);
			}
		}

		protected override void OnMouseLeave(EventArgs e) {
			base.OnMouseLeave(e);
			InvalidateItem(hoverIndex);
			hoverIndex = -1;
		}

		private void InvalidateItem(int index) {
			if(index >= 0 && index < Items.Count) {
				Invalidate(GetItemRectangle(index));
			}
		}

		protected override void OnDrawItem(DrawItemEventArgs e) {
			if(e.Index < 0) {
				return;
			}

			Graphics g = e.Graphics;
			using(SolidBrush background = new SolidBrush(Theme.Card)) {
				g.FillRectangle(background, e.Bounds);
			}

			bool selected = (e.State & DrawItemState.Selected) != 0;
			Rectangle row = new Rectangle(e.Bounds.X + Theme.S(4), e.Bounds.Y + Theme.S(2), e.Bounds.Width - Theme.S(8), e.Bounds.Height - Theme.S(4));
			g.SmoothingMode = SmoothingMode.AntiAlias;
			if(selected || e.Index == hoverIndex) {
				using(GraphicsPath path = Theme.RoundedRectangle(row, Theme.S(4))) {
					using(SolidBrush brush = new SolidBrush(selected ? Theme.ItemSelected : Theme.ItemHover)) {
						g.FillPath(brush, path);
					}
				}
			}
			if(selected) {
				RectangleF marker = new RectangleF(row.X, row.Y + row.Height / 2 - Theme.S(8), Theme.S(3), Theme.S(16));
				using(GraphicsPath path = Theme.RoundedRectangle(marker, Theme.S(1))) {
					using(SolidBrush brush = new SolidBrush(Theme.Accent)) {
						g.FillPath(brush, path);
					}
				}
			}

			Rectangle textBounds = new Rectangle(row.X + Theme.S(14), row.Y, row.Width - Theme.S(18), row.Height);
			TextRenderer.DrawText(g, GetItemText(Items[e.Index]), Font, textBounds, Theme.Text,
				TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
		}
	}
}
