using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RightLayout.Win32 {
	/// <summary>
	/// The Windows functions RightLayout calls, wrapped so the rest of the app never deals with buffers or structs
	/// </summary>
	public static class API {
		// Window messages
		public const int WM_INPUT = 0x00FF;
		public const int WM_INPUT_DEVICE_CHANGE = 0x00FE;
		public const int WM_MOUSEWHEEL = 0x020A;
		public const int WM_SETICON = 0x0080;
		public const int EM_SETCUEBANNER = 0x1501;
		public const int ICON_SMALL = 0;

		public const uint LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;

		#region Keyboard input

		// HID usage page and usage that identify keyboards
		private const ushort UsagePageGenericDesktop = 0x01;
		private const ushort UsageKeyboard = 0x06;

		// Receive input even when another app has focus, and be told when keyboards come and go
		private const uint RIDEV_INPUTSINK = 0x00000100;
		private const uint RIDEV_DEVNOTIFY = 0x00002000;

		private const uint RID_HEADER = 0x10000005;
		private const uint RIDI_DEVICENAME = 0x20000007;
		private const uint RIM_TYPEKEYBOARD = 1;

		[StructLayout(LayoutKind.Sequential)]
		private struct RawInputDevice {
			public ushort UsagePage;
			public ushort Usage;
			public uint Flags;
			public IntPtr Target;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RawInputHeader {
			public uint Type;
			public uint Size;
			public IntPtr Device;
			public IntPtr WParam;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RawInputDeviceListItem {
			public IntPtr Device;
			public uint Type;
		}

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool RegisterRawInputDevices([In] RawInputDevice[] devices, uint count, uint size);

		[DllImport("user32.dll")]
		private static extern uint GetRawInputData(IntPtr rawInput, uint command, out RawInputHeader header, ref uint size, uint headerSize);

		[DllImport("user32.dll")]
		private static extern int GetRawInputDeviceList([Out] RawInputDeviceListItem[] devices, ref uint count, uint itemSize);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern uint GetRawInputDeviceInfo(IntPtr device, uint command, [Out] char[] data, ref uint size);

		/// <summary>
		/// Sends WM_INPUT for every key press on any keyboard, and WM_INPUT_DEVICE_CHANGE when keyboards connect or disconnect, to a window
		/// </summary>
		public static void ListenToKeyboards(IntPtr window) {
			RawInputDevice[] request = new RawInputDevice[1];
			request[0].UsagePage = UsagePageGenericDesktop;
			request[0].Usage = UsageKeyboard;
			request[0].Flags = RIDEV_INPUTSINK | RIDEV_DEVNOTIFY;
			request[0].Target = window;
			if(!RegisterRawInputDevices(request, 1, (uint)Marshal.SizeOf(typeof(RawInputDevice)))) {
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}

		/// <summary>
		/// Gets the device that produced a WM_INPUT message (its lParam), or IntPtr.Zero when Windows can't tell.
		/// Only the header is read: which key was pressed is never looked at.
		/// </summary>
		public static IntPtr GetInputDevice(IntPtr rawInput) {
			RawInputHeader header;
			uint size = (uint)Marshal.SizeOf(typeof(RawInputHeader));
			if(GetRawInputData(rawInput, RID_HEADER, out header, ref size, size) == uint.MaxValue) {
				return IntPtr.Zero;
			}
			return header.Device;
		}

		/// <summary>
		/// Gets the handles of the keyboards connected right now
		/// </summary>
		public static List<IntPtr> GetKeyboardHandles() {
			List<IntPtr> keyboards = new List<IntPtr>();
			uint itemSize = (uint)Marshal.SizeOf(typeof(RawInputDeviceListItem));

			// Ask for the count, then fill a list of that many items. If a device arrives in between, the
			// second call fails rather than overflowing, and we try again with the new count.
			for(int attempt = 0; attempt < 3; ++attempt) {
				uint count = 0;
				if(GetRawInputDeviceList(null, ref count, itemSize) != 0) {
					break;
				}
				RawInputDeviceListItem[] items = new RawInputDeviceListItem[count];
				int filled = GetRawInputDeviceList(items, ref count, itemSize);
				if(filled < 0) {
					continue;
				}
				for(int i = 0; i < filled; ++i) {
					if(items[i].Type == RIM_TYPEKEYBOARD) {
						keyboards.Add(items[i].Device);
					}
				}
				break;
			}
			return keyboards;
		}

		/// <summary>
		/// Gets a device's name, e.g. \\?\HID#VID_046D&amp;PID_C31C#..., which stays the same when it reconnects.
		/// Returns null for input that wasn't typed on a real keyboard, or a device that just disconnected.
		/// </summary>
		public static string GetDeviceName(IntPtr device) {
			if(device == IntPtr.Zero) {
				return null;
			}
			uint length = 0;
			if(GetRawInputDeviceInfo(device, RIDI_DEVICENAME, null, ref length) != 0 || length == 0) {
				return null;
			}
			char[] name = new char[length];
			uint copied = GetRawInputDeviceInfo(device, RIDI_DEVICENAME, name, ref length);
			if(copied == 0 || copied == uint.MaxValue) {
				return null;
			}
			return new string(name, 0, (int)Math.Min(copied, length)).TrimEnd('\0');
		}

		#endregion

		#region Input languages

		private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
		private const uint BSF_POSTMESSAGE = 0x00000010;
		private const uint BSM_APPLICATIONS = 0x00000008;
		private const uint SPI_SETDEFAULTINPUTLANG = 0x005A;
		private const uint SPIF_SENDCHANGE = 0x0002;

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

		[DllImport("user32.dll")]
		private static extern IntPtr GetKeyboardLayout(uint threadId);

		private const uint KLF_SETFORPROCESS = 0x00000100;

		[DllImport("user32.dll")]
		private static extern IntPtr ActivateKeyboardLayout(IntPtr layout, uint flags);

		[DllImport("user32.dll")]
		private static extern int GetKeyboardLayoutList(int count, [Out] IntPtr[] layouts);

		[DllImport("user32.dll")]
		private static extern int BroadcastSystemMessage(uint flags, ref uint recipients, uint message, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool SystemParametersInfo(uint action, uint param, [In] IntPtr[] value, uint winIni);

		/// <summary>
		/// The language identifier is the low word of a keyboard layout handle (HKL)
		/// </summary>
		private static ushort LanguageOf(IntPtr layout) {
			return unchecked((ushort)(layout.ToInt64() & 0xFFFF));
		}

		/// <summary>
		/// Gets the language of the input method used by the window the user is typing in
		/// </summary>
		public static ushort GetForegroundLanguage() {
			uint processId;
			uint threadId = GetWindowThreadProcessId(GetForegroundWindow(), out processId);
			return LanguageOf(GetKeyboardLayout(threadId));
		}

		/// <summary>
		/// Gets the input methods installed in Windows (keyboard layout handles), in the user's order
		/// </summary>
		private static IntPtr[] GetInstalledLayouts() {
			int count = GetKeyboardLayoutList(0, null);
			if(count <= 0) {
				return new IntPtr[0];
			}
			IntPtr[] layouts = new IntPtr[count];
			count = GetKeyboardLayoutList(layouts.Length, layouts);
			if(count < layouts.Length) {
				Array.Resize(ref layouts, Math.Max(count, 0));
			}
			return layouts;
		}

		/// <summary>
		/// Gets the languages of the input methods installed in Windows, in the user's order, without duplicates
		/// </summary>
		public static List<ushort> GetInstalledLanguages() {
			List<ushort> languages = new List<ushort>();
			foreach(IntPtr layout in GetInstalledLayouts()) {
				ushort language = LanguageOf(layout);
				if(!languages.Contains(language)) {
					languages.Add(language);
				}
			}
			return languages;
		}

		/// <summary>
		/// Switches the open apps to a language, and makes it the default for apps opened afterwards
		/// </summary>
		public static void SwitchLanguage(ushort language) {
			uint recipients = BSM_APPLICATIONS;
			BroadcastSystemMessage(BSF_POSTMESSAGE, ref recipients, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, new IntPtr(language));

			// The default input method's handle repeats the language in its high word, e.g. 0x04090409
			IntPtr defaultLayout = new IntPtr(unchecked((int)(((uint)language << 16) | language)));
			SystemParametersInfo(SPI_SETDEFAULTINPUTLANG, 0, new IntPtr[] { defaultLayout }, SPIF_SENDCHANGE);

			// When one of RightLayout's own windows has focus, the broadcast changes this app's input method
			// without telling the window, so Windows keeps showing the old one until focus moves elsewhere.
			// Switching directly announces the change to the active window.
			uint processId;
			GetWindowThreadProcessId(GetForegroundWindow(), out processId);
			if(processId == (uint)System.Diagnostics.Process.GetCurrentProcess().Id) {
				foreach(IntPtr layout in GetInstalledLayouts()) {
					if(LanguageOf(layout) == language) {
						ActivateKeyboardLayout(layout, KLF_SETFORPROCESS);
						break;
					}
				}
			}
		}

		#endregion

		#region Process setup

		[DllImport("user32.dll")]
		public static extern bool SetProcessDPIAware();

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool SetDefaultDllDirectories(uint directoryFlags);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool SetDllDirectory(string pathName);

		#endregion

		#region Appearance

		[DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
		public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

		[DllImport("dwmapi.dll")]
		public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

		private const int RT_GROUP_ICON = 14;
		private const uint IMAGE_ICON = 1;

		private delegate bool EnumResNameProc(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpType, EnumResNameProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr LoadImage(IntPtr hinst, IntPtr name, uint type, int cx, int cy, uint fuLoad);

		/// <summary>
		/// Loads a module's first icon resource at an exact size, or returns IntPtr.Zero.
		/// Windows picks the best frame itself and, unlike System.Drawing.Icon, handles PNG-compressed frames.
		/// The caller owns the returned icon handle.
		/// </summary>
		public static IntPtr LoadModuleIcon(IntPtr module, int size) {
			IntPtr groupName = IntPtr.Zero;
			EnumResourceNames(module, new IntPtr(RT_GROUP_ICON), delegate(IntPtr m, IntPtr type, IntPtr name, IntPtr param) {
				groupName = name;
				return false;
			}, IntPtr.Zero);
			// The compiler gives the icon a numeric ID; a named resource would need its string copied inside the callback
			if(groupName == IntPtr.Zero || (groupName.ToInt64() >> 16) != 0) {
				return IntPtr.Zero;
			}
			return LoadImage(module, groupName, IMAGE_ICON, size, size, 0);
		}

		#endregion
	}
}
