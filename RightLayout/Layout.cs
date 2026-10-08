using System;
using System.Collections.Generic;
using System.Globalization;
using RightLayout.Win32;

namespace RightLayout {
	/// <summary>
	/// An input method RightLayout can switch to, identified by its Windows language identifier (e.g. 0x0409)
	/// </summary>
	public class Layout {
		public readonly ushort Identifier;
		public readonly string Name;

		public Layout(ushort identifier, string name) {
			Identifier = identifier;
			Name = name;
		}

		public override string ToString() {
			return Name;
		}

		/// <summary>
		/// Gets the input methods installed in Windows, in the user's order
		/// </summary>
		public static Layout[] GetInstalledLayouts() {
			List<Layout> installed = new List<Layout>();
			foreach(ushort language in API.GetInstalledLanguages()) {
				installed.Add(new Layout(language, GetDisplayName(language)));
			}
			return installed.ToArray();
		}

		/// <summary>
		/// Gets the language's name as Windows shows it, e.g. "English (United States)", or its hex code when Windows has none
		/// </summary>
		public static string GetDisplayName(ushort identifier) {
			try {
				return CultureInfo.GetCultureInfo(identifier).DisplayName;
			}
			catch(ArgumentException) {
				return identifier.ToString("X4");
			}
		}
	}
}
