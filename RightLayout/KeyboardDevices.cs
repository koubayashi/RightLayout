using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RightLayout.Win32;

namespace RightLayout {
	public enum KeyboardKind {
		Other,
		BuiltIn,
		Bluetooth,
		Usb,
		Remote
	}

	/// <summary>
	/// What Windows knows about a keyboard: how it is connected and its product name
	/// </summary>
	public class KeyboardDeviceInfo {
		private readonly KeyboardKind kind;
		private readonly string productName;
		private readonly string groupKey;

		public KeyboardDeviceInfo(KeyboardKind kind, string productName, string groupKey) {
			this.kind = kind;
			this.productName = productName;
			this.groupKey = groupKey;
		}

		public KeyboardKind Kind {
			get {
				return kind;
			}
		}

		/// <summary>
		/// Identifies the physical keyboard. Windows lists one keyboard as several devices (main keys,
		/// media keys, ...), which all share the same group key.
		/// </summary>
		public string GroupKey {
			get {
				return groupKey;
			}
		}

		/// <summary>
		/// Gets the product name, or a description of the keyboard when Windows doesn't know the product
		/// </summary>
		public string DisplayName {
			get {
				return productName ?? KeyboardDevices.GetFallbackName(kind);
			}
		}
	}

	/// <summary>
	/// One physical keyboard with all the devices Windows lists for it, and what is saved for them
	/// </summary>
	public class KeyboardGroup {
		public readonly string Key;
		public readonly List<string> DeviceNames = new List<string>();
		public readonly List<KeyboardEntry> SavedEntries = new List<KeyboardEntry>();
		public KeyboardKind Kind;
		public string DetectedName;
		public bool Connected;

		public KeyboardGroup(string key) {
			Key = key;
		}

		public bool IsSaved {
			get {
				return SavedEntries.Count > 0;
			}
		}

		/// <summary>
		/// True when the saved devices of this keyboard use different input methods
		/// </summary>
		public bool IsMixed {
			get {
				foreach(KeyboardEntry entry in SavedEntries) {
					if(entry.Layout != SavedEntries[0].Layout) {
						return true;
					}
				}
				return false;
			}
		}

		public ushort Layout {
			get {
				return SavedEntries[0].Layout;
			}
		}

		public string Nickname {
			get {
				foreach(KeyboardEntry entry in SavedEntries) {
					if(entry.Nickname != null) {
						return entry.Nickname;
					}
				}
				return null;
			}
		}

		public string DisplayName {
			get {
				return Nickname ?? DetectedName;
			}
		}

		/// <summary>
		/// Groups the saved keyboards and the connected ones by physical keyboard
		/// </summary>
		public static List<KeyboardGroup> Build(List<KeyboardEntry> savedEntries, List<string> connectedDevices) {
			List<KeyboardGroup> groups = new List<KeyboardGroup>();
			Dictionary<string, KeyboardGroup> byKey = new Dictionary<string, KeyboardGroup>(StringComparer.OrdinalIgnoreCase);

			foreach(KeyboardEntry entry in savedEntries) {
				KeyboardGroup group = GetOrAdd(entry.DeviceName, groups, byKey);
				group.SavedEntries.Add(entry);
			}
			foreach(string deviceName in connectedDevices) {
				KeyboardGroup group = GetOrAdd(deviceName, groups, byKey);
				group.Connected = true;
			}
			return groups;
		}

		private static KeyboardGroup GetOrAdd(string deviceName, List<KeyboardGroup> groups, Dictionary<string, KeyboardGroup> byKey) {
			KeyboardDeviceInfo info = KeyboardDevices.Get(deviceName);
			KeyboardGroup group;
			if(!byKey.TryGetValue(info.GroupKey, out group)) {
				group = new KeyboardGroup(info.GroupKey);
				group.Kind = info.Kind;
				group.DetectedName = info.DisplayName;
				groups.Add(group);
				byKey.Add(info.GroupKey, group);
			}
			if(!group.DeviceNames.Contains(deviceName)) {
				group.DeviceNames.Add(deviceName);
				// Prefer a real product name over "Bluetooth keyboard" if another part of the keyboard has one
				if(group.DetectedName == KeyboardDevices.GetFallbackName(group.Kind) && info.DisplayName != group.DetectedName) {
					group.DetectedName = info.DisplayName;
				}
			}
			return group;
		}
	}

	/// <summary>
	/// Looks up connected keyboards and turns raw input device names into something readable
	/// </summary>
	public static class KeyboardDevices {
		private static readonly Dictionary<string, KeyboardDeviceInfo> cache = new Dictionary<string, KeyboardDeviceInfo>(StringComparer.OrdinalIgnoreCase);

		// Names Windows gives to every keyboard, which don't help tell keyboards apart
		private static readonly string[] genericNames = {
			"hid keyboard", "hid-compliant", "hid device", "usb input device", "gatt compliant",
			"usb composite device", "standard ps/2", "enumerator", "hub", "bluetooth le service", "bluetooth le device"
		};

		private const string BluetoothBaseUuid = "00805F9B34FB";
		private static readonly Regex bluetoothAddress = new Regex("(?<![0-9A-F])[0-9A-F]{12}(?![0-9A-F])");

		/// <summary>
		/// Gets the raw input device names of the keyboards connected right now
		/// </summary>
		public static List<string> GetConnected() {
			List<string> result = new List<string>();
			foreach(API.RAWINPUTDEVICELIST device in API.GetRawInputDeviceList()) {
				if(device.dwType == API.RIM_TYPEKEYBOARD) {
					string deviceName = API.GetRawInputDeviceName(device.hDevice);
					if(!string.IsNullOrEmpty(deviceName)) {
						result.Add(deviceName);
					}
				}
			}
			return result;
		}

		public static KeyboardDeviceInfo Get(string deviceName) {
			KeyboardDeviceInfo info;
			if(!cache.TryGetValue(deviceName, out info)) {
				try {
					info = Inspect(deviceName);
				}
				catch(Exception) {
					info = new KeyboardDeviceInfo(KeyboardKind.Other, null, "device:" + deviceName);
				}
				cache[deviceName] = info;
			}
			return info;
		}

		public static void ClearCache() {
			cache.Clear();
		}

		public static string GetKindName(KeyboardKind kind) {
			switch(kind) {
				case KeyboardKind.BuiltIn: return "Built-in";
				case KeyboardKind.Bluetooth: return "Bluetooth";
				case KeyboardKind.Usb: return "USB";
				case KeyboardKind.Remote: return "Remote Desktop";
				default: return "Keyboard";
			}
		}

		public static string GetFallbackName(KeyboardKind kind) {
			switch(kind) {
				case KeyboardKind.BuiltIn: return "Built-in keyboard";
				case KeyboardKind.Bluetooth: return "Bluetooth keyboard";
				case KeyboardKind.Usb: return "USB keyboard";
				case KeyboardKind.Remote: return "Remote Desktop keyboard";
				default: return "Keyboard";
			}
		}

		/// <summary>
		/// Gets the Segoe icon font glyph for a kind of keyboard
		/// </summary>
		public static string GetIconGlyph(KeyboardKind kind) {
			switch(kind) {
				case KeyboardKind.BuiltIn: return "\uE7F8";		// DeviceLaptopNoPic
				case KeyboardKind.Bluetooth: return "\uE702";	// Bluetooth
				case KeyboardKind.Usb: return "\uE88E";			// USB
				case KeyboardKind.Remote: return "\uE8AF";		// Remote
				default: return "\uE765";						// KeyboardClassic
			}
		}

		private static KeyboardDeviceInfo Inspect(string deviceName) {
			// Walk from the keyboard up the device tree, e.g. HID\... -> BTHENUM\... -> BTH\... -> USB\...
			List<string> ids = new List<string>();
			List<uint> nodes = new List<uint>();
			uint node;
			if(CfgMgr.TryLocate(ToInstanceId(deviceName), out node)) {
				for(int depth = 0; depth < 12; ++depth) {
					string id = CfgMgr.GetDeviceId(node);
					if(id == null || id.StartsWith(@"HTREE\", StringComparison.OrdinalIgnoreCase)) {
						break;
					}
					ids.Add(id.ToUpperInvariant());
					nodes.Add(node);

					uint parent;
					if(!CfgMgr.TryGetParent(node, out parent)) {
						break;
					}
					node = parent;
				}
			}

			KeyboardKind kind = DetectKind(deviceName.ToUpperInvariant(), ids);

			// All parts of one physical device share a container ID
			Guid container = nodes.Count > 0 ? CfgMgr.GetGuid(nodes[0], CfgMgr.DEVPKEY_Device_ContainerId) : Guid.Empty;
			if(kind == KeyboardKind.Remote || container == Guid.Empty) {
				return new KeyboardDeviceInfo(kind, FindName(kind, ids, nodes), "device:" + deviceName);
			}
			if(container == CfgMgr.LocalMachineContainer) {
				// Built into the computer, even when wired internally over USB (e.g. a Fn/hotkey controller)
				return new KeyboardDeviceInfo(KeyboardKind.BuiltIn, null, "container:local");
			}
			return new KeyboardDeviceInfo(kind, FindName(kind, ids, nodes), "container:" + container.ToString("D"));
		}

		private static string FindName(KeyboardKind kind, List<string> ids, List<uint> nodes) {
			string name = FindProductName(kind, ids, nodes);
			if(name == null && kind == KeyboardKind.Bluetooth) {
				name = FindBluetoothDeviceName(ids);
			}
			return name;
		}

		/// <summary>
		/// Turns \\?\HID#VID_046D&amp;PID_C31C#7&amp;...#{guid} into the instance ID HID\VID_046D&amp;PID_C31C\7&amp;...
		/// </summary>
		private static string ToInstanceId(string deviceName) {
			string id = deviceName;
			if(id.StartsWith(@"\\?\") || id.StartsWith(@"\??\")) {
				id = id.Substring(4);
			}
			int interfaceGuid = id.LastIndexOf("#{");
			if(interfaceGuid >= 0) {
				id = id.Substring(0, interfaceGuid);
			}
			return id.Replace('#', '\\');
		}

		private static KeyboardKind DetectKind(string deviceName, List<string> ids) {
			List<string> all = new List<string>(ids);
			all.Add(deviceName);

			foreach(string id in all) {
				if(id.Contains("RDP_KBD")) {
					return KeyboardKind.Remote;
				}
			}
			foreach(string id in all) {
				if(id.StartsWith(@"BTHENUM\") || id.StartsWith(@"BTHLE") || id.Contains("{00001124-") || id.Contains("{00001812-")) {
					return KeyboardKind.Bluetooth;
				}
			}
			// Checked after Bluetooth, because Bluetooth radios are usually USB devices themselves
			foreach(string id in ids) {
				if(id.StartsWith(@"USB\")) {
					return KeyboardKind.Usb;
				}
			}
			foreach(string id in all) {
				if(id.StartsWith(@"ACPI\") || id.StartsWith(@"\\?\ACPI#") || id.Contains("CONVERTEDDEVICE")) {
					return KeyboardKind.BuiltIn;
				}
			}
			return KeyboardKind.Other;
		}

		private static string FindProductName(KeyboardKind kind, List<string> ids, List<uint> nodes) {
			for(int i = 0; i < nodes.Count; ++i) {
				// Stop before reaching hubs, radios and controllers
				string id = ids[i];
				bool partOfKeyboard = id.StartsWith(@"HID\") || id.StartsWith(@"BTHENUM\") || id.StartsWith(@"BTHLEDEVICE\") || id.StartsWith(@"BTHLE\")
					|| (kind == KeyboardKind.Usb && id.StartsWith(@"USB\VID_"));
				if(i > 0 && !partOfKeyboard) {
					break;
				}

				string name = GetSpecificName(nodes[i]);
				if(name != null) {
					return name;
				}
			}
			return null;
		}

		/// <summary>
		/// Classic Bluetooth keyboards keep their name on a sibling node (BTHENUM\DEV_&lt;address&gt;), found through the address
		/// </summary>
		private static string FindBluetoothDeviceName(List<string> ids) {
			string address = null;
			foreach(string id in ids) {
				foreach(Match match in bluetoothAddress.Matches(id)) {
					if(match.Value != BluetoothBaseUuid) {
						address = match.Value;
						break;
					}
				}
				if(address != null) {
					break;
				}
			}
			if(address == null) {
				return null;
			}

			foreach(string enumerator in new string[] { "BTHENUM", "BTHLE" }) {
				string prefix = enumerator + @"\DEV_" + address;
				foreach(string id in CfgMgr.GetDeviceIds(enumerator)) {
					uint node;
					if(id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && CfgMgr.TryLocate(id, out node)) {
						string name = GetSpecificName(node);
						if(name != null) {
							return name;
						}
					}
				}
			}
			return null;
		}

		private static string GetSpecificName(uint node) {
			foreach(CfgMgr.DEVPROPKEY key in new CfgMgr.DEVPROPKEY[] { CfgMgr.DEVPKEY_Device_FriendlyName, CfgMgr.DEVPKEY_Device_BusReportedDeviceDesc, CfgMgr.DEVPKEY_Device_DeviceDesc }) {
				string name = CleanName(CfgMgr.GetString(node, key));
				if(name != null && !IsGeneric(name)) {
					return name;
				}
			}
			return null;
		}

		private static string CleanName(string name) {
			if(name == null) {
				return null;
			}
			// Unresolved resource strings look like "@input.inf,%hid_device%;HID Keyboard Device"
			if(name.StartsWith("@") && name.Contains(";")) {
				name = name.Substring(name.LastIndexOf(';') + 1);
			}
			name = name.Trim();
			return name.Length == 0 ? null : name;
		}

		private static bool IsGeneric(string name) {
			// Names with a {GUID} describe a service, not a product
			if(name.Contains("{")) {
				return true;
			}
			string lower = name.ToLowerInvariant();
			foreach(string generic in genericNames) {
				if(lower.Contains(generic)) {
					return true;
				}
			}
			return false;
		}
	}
}
