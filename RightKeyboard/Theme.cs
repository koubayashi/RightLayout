using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using RightKeyboard.Win32;

namespace RightKeyboard {
	/// <summary>
	/// Colors, fonts and sizes for the Windows 11 style UI. Follows the Windows light/dark app setting.
	/// </summary>
	internal static class Theme {
		public static readonly bool IsDark = ReadIsDark();

		public static readonly Color Window = IsDark ? Rgb(0x202020) : Rgb(0xF3F3F3);
		public static readonly Color Card = IsDark ? Rgb(0x2B2B2B) : Rgb(0xFFFFFF);
		public static readonly Color CardBorder = IsDark ? Rgb(0x1C1C1C) : Rgb(0xE5E5E5);
		public static readonly Color Control = IsDark ? Rgb(0x2D2D2D) : Rgb(0xFBFBFB);
		public static readonly Color ControlHover = IsDark ? Rgb(0x323232) : Rgb(0xF6F6F6);
		public static readonly Color ControlPressed = IsDark ? Rgb(0x272727) : Rgb(0xF0F0F0);
		public static readonly Color ControlBorder = IsDark ? Rgb(0x3F3F3F) : Rgb(0xD5D5D5);
		public static readonly Color ItemHover = IsDark ? Rgb(0x333333) : Rgb(0xF3F3F3);
		public static readonly Color ItemSelected = IsDark ? Rgb(0x3A3A3A) : Rgb(0xEAEAEA);
		public static readonly Color Text = IsDark ? Rgb(0xFFFFFF) : Rgb(0x1B1B1B);
		public static readonly Color SubText = IsDark ? Rgb(0xC5C5C5) : Rgb(0x5F5F5F);
		public static readonly Color DisabledText = IsDark ? Rgb(0x787878) : Rgb(0xA0A0A0);
		public static readonly Color Accent = IsDark ? Rgb(0x60CDFF) : Rgb(0x005FB8);
		public static readonly Color AccentHover = IsDark ? Rgb(0x5AB9E6) : Rgb(0x196EBF);
		public static readonly Color AccentPressed = IsDark ? Rgb(0x52A6CF) : Rgb(0x3183CA);
		public static readonly Color OnAccent = IsDark ? Rgb(0x000000) : Rgb(0xFFFFFF);
		public static readonly Color Connected = IsDark ? Rgb(0x6CCB5F) : Rgb(0x0F7B0F);

		private static readonly string iconFontName = FindIconFont();

		public static readonly Font Body = new Font("Segoe UI", 9.75f);
		public static readonly Font BodyStrong = new Font("Segoe UI Semibold", 10.5f);
		public static readonly Font Caption = new Font("Segoe UI", 9f);
		public static readonly Font Subtitle = new Font("Segoe UI Semibold", 14f);
		public static readonly Font Title = new Font("Segoe UI Semibold", 20f);
		public static readonly Font IconFont = new Font(iconFontName, 15f);
		public static readonly Font LargeIconFont = new Font(iconFontName, 22f);
		public static readonly Font SmallIconFont = new Font(iconFontName, 9f);

		private static readonly float scale = ReadScale();

		private static Icon appIcon;

		/// <summary>
		/// Scales a size given in 96 DPI pixels to the screen's DPI
		/// </summary>
		public static int S(int pixels) {
			return (int)Math.Round(pixels * scale);
		}

		public static Icon AppIcon {
			get {
				if(appIcon == null) {
					try {
						appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
					}
					catch(Exception) {
						appIcon = SystemIcons.Application;
					}
				}
				return appIcon;
			}
		}

		public static void ApplyTo(Form form, Color background) {
			form.BackColor = background;
			form.ForeColor = Text;
			form.Font = Body;
			form.AutoScaleMode = AutoScaleMode.None;
			form.Icon = AppIcon;
			if(IsDark) {
				form.HandleCreated += delegate { UseDarkTitleBar(form.Handle); };
			}
		}

		/// <summary>
		/// Gives scrollbars and drop-downs of a control the dark look when the dark theme is on
		/// </summary>
		public static void UseDarkScrollbars(Control control, string themeClass) {
			if(IsDark) {
				control.HandleCreated += delegate {
					try {
						API.SetWindowTheme(control.Handle, themeClass, null);
					}
					catch(Exception) {
					}
				};
			}
		}

		public static void StyleMenu(ToolStrip menu) {
			menu.Renderer = new ThemedMenuRenderer();
			menu.Font = Body;
			ContextMenuStrip contextMenu = menu as ContextMenuStrip;
			if(contextMenu != null) {
				contextMenu.ShowImageMargin = false;
			}
			foreach(ToolStripItem item in menu.Items) {
				if(!(item is ToolStripSeparator)) {
					item.Padding = new Padding(S(4), S(5), S(4), S(5));
				}
			}
		}

		public static GraphicsPath RoundedRectangle(RectangleF bounds, float radius) {
			float diameter = radius * 2;
			GraphicsPath path = new GraphicsPath();
			path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
			path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
			path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
			path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
			path.CloseFigure();
			return path;
		}

		/// <summary>
		/// Paints a rounded box with a 1px border, clearing the corners to the parent's color
		/// </summary>
		public static void PaintBox(Graphics g, Control control, Color fill, Color border, int radius) {
			g.Clear(control.Parent != null ? control.Parent.BackColor : Window);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			RectangleF bounds = new RectangleF(0.5f, 0.5f, control.Width - 1.5f, control.Height - 1.5f);
			using(GraphicsPath path = RoundedRectangle(bounds, radius)) {
				using(SolidBrush brush = new SolidBrush(fill)) {
					g.FillPath(brush, path);
				}
				using(Pen pen = new Pen(border)) {
					g.DrawPath(pen, path);
				}
			}
		}

		public static void PaintFocusRing(Graphics g, Control control, int radius) {
			g.SmoothingMode = SmoothingMode.AntiAlias;
			RectangleF bounds = new RectangleF(1f, 1f, control.Width - 3f, control.Height - 3f);
			using(GraphicsPath path = RoundedRectangle(bounds, radius)) {
				using(Pen pen = new Pen(Text, 2f)) {
					g.DrawPath(pen, path);
				}
			}
		}

		private static void UseDarkTitleBar(IntPtr handle) {
			try {
				int enabled = 1;
				// DWMWA_USE_IMMERSIVE_DARK_MODE is 20 on current Windows and 19 on early Windows 10 builds
				if(API.DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int)) != 0) {
					API.DwmSetWindowAttribute(handle, 19, ref enabled, sizeof(int));
				}
			}
			catch(Exception) {
			}
		}

		private static bool ReadIsDark() {
			try {
				using(RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")) {
					object value = key == null ? null : key.GetValue("AppsUseLightTheme");
					return value is int && (int)value == 0;
				}
			}
			catch(Exception) {
				return false;
			}
		}

		private static float ReadScale() {
			using(Graphics g = Graphics.FromHwnd(IntPtr.Zero)) {
				return g.DpiX / 96f;
			}
		}

		private static string FindIconFont() {
			using(InstalledFontCollection fonts = new InstalledFontCollection()) {
				foreach(FontFamily family in fonts.Families) {
					if(family.Name == "Segoe Fluent Icons") {
						return family.Name;
					}
				}
			}
			return "Segoe MDL2 Assets";
		}

		private static Color Rgb(int rgb) {
			return Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
		}
	}

	/// <summary>
	/// Draws menus in the current theme's colors
	/// </summary>
	internal class ThemedMenuRenderer : ToolStripProfessionalRenderer {
		public ThemedMenuRenderer() : base(new ThemedColorTable()) {
			RoundedEdges = false;
		}

		protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
			e.TextColor = e.Item.Enabled ? Theme.Text : Theme.DisabledText;
			base.OnRenderItemText(e);
		}

		protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) {
			e.ArrowColor = Theme.Text;
			base.OnRenderArrow(e);
		}

		private class ThemedColorTable : ProfessionalColorTable {
			public override Color ToolStripDropDownBackground { get { return Theme.Card; } }
			public override Color ImageMarginGradientBegin { get { return Theme.Card; } }
			public override Color ImageMarginGradientMiddle { get { return Theme.Card; } }
			public override Color ImageMarginGradientEnd { get { return Theme.Card; } }
			public override Color MenuBorder { get { return Theme.ControlBorder; } }
			public override Color MenuItemBorder { get { return Theme.ItemHover; } }
			public override Color MenuItemSelected { get { return Theme.ItemHover; } }
			public override Color MenuItemSelectedGradientBegin { get { return Theme.ItemHover; } }
			public override Color MenuItemSelectedGradientEnd { get { return Theme.ItemHover; } }
			public override Color MenuItemPressedGradientBegin { get { return Theme.ItemSelected; } }
			public override Color MenuItemPressedGradientEnd { get { return Theme.ItemSelected; } }
			public override Color SeparatorDark { get { return Theme.ControlBorder; } }
			public override Color SeparatorLight { get { return Theme.Card; } }
		}
	}
}
