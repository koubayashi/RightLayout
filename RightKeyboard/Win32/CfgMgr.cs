using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RightKeyboard.Win32 {
	/// <summary>
	/// Exposes the Configuration Manager (cfgmgr32) functions used to look up device names
	/// </summary>
	internal static class CfgMgr {
		private const int CR_SUCCESS = 0;
		private const uint CM_LOCATE_DEVNODE_PHANTOM = 0x00000001;
		private const uint CM_GETIDLIST_FILTER_ENUMERATOR = 0x00000001;
		private const uint DEVPROP_TYPE_STRING = 0x00000012;
		private const int MAX_DEVICE_ID_LEN = 200;

		[StructLayout(LayoutKind.Sequential)]
		public struct DEVPROPKEY {
			public Guid fmtid;
			public uint pid;

			public DEVPROPKEY(string fmtid, uint pid) {
				this.fmtid = new Guid(fmtid);
				this.pid = pid;
			}
		}

		public static readonly DEVPROPKEY DEVPKEY_Device_DeviceDesc = new DEVPROPKEY("a45c254e-df1c-4efd-8020-67d146a850e0", 2);
		public static readonly DEVPROPKEY DEVPKEY_Device_FriendlyName = new DEVPROPKEY("a45c254e-df1c-4efd-8020-67d146a850e0", 14);
		public static readonly DEVPROPKEY DEVPKEY_Device_BusReportedDeviceDesc = new DEVPROPKEY("540b947e-8b40-45bc-a8a2-6a0b894cbda2", 4);

		[DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
		private static extern int CM_Locate_DevNodeW(out uint pdnDevInst, string pDeviceID, uint ulFlags);

		[DllImport("cfgmgr32.dll")]
		private static extern int CM_Get_Parent(out uint pdnDevInst, uint dnDevInst, uint ulFlags);

		[DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
		private static extern int CM_Get_Device_IDW(uint dnDevInst, StringBuilder buffer, int bufferLen, uint ulFlags);

		[DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
		private static extern int CM_Get_DevNode_PropertyW(uint dnDevInst, ref DEVPROPKEY propertyKey, out uint propertyType, byte[] propertyBuffer, ref uint propertyBufferSize, uint ulFlags);

		[DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
		private static extern int CM_Get_Device_ID_List_SizeW(out uint pulLen, string pszFilter, uint ulFlags);

		[DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
		private static extern int CM_Get_Device_ID_ListW(string pszFilter, char[] buffer, uint bufferLen, uint ulFlags);

		/// <summary>
		/// Finds a device node, including devices that are not connected right now
		/// </summary>
		public static bool TryLocate(string instanceId, out uint devInst) {
			try {
				return CM_Locate_DevNodeW(out devInst, instanceId, CM_LOCATE_DEVNODE_PHANTOM) == CR_SUCCESS;
			}
			catch(Exception) {
				devInst = 0;
				return false;
			}
		}

		public static bool TryGetParent(uint devInst, out uint parent) {
			return CM_Get_Parent(out parent, devInst, 0) == CR_SUCCESS;
		}

		public static string GetDeviceId(uint devInst) {
			StringBuilder buffer = new StringBuilder(MAX_DEVICE_ID_LEN + 1);
			if(CM_Get_Device_IDW(devInst, buffer, buffer.Capacity, 0) != CR_SUCCESS) {
				return null;
			}
			return buffer.ToString();
		}

		public static string GetString(uint devInst, DEVPROPKEY key) {
			uint type;
			uint size = 0;
			CM_Get_DevNode_PropertyW(devInst, ref key, out type, null, ref size, 0);
			if(size == 0) {
				return null;
			}

			byte[] buffer = new byte[size];
			if(CM_Get_DevNode_PropertyW(devInst, ref key, out type, buffer, ref size, 0) != CR_SUCCESS || type != DEVPROP_TYPE_STRING) {
				return null;
			}
			return Encoding.Unicode.GetString(buffer, 0, (int)size).TrimEnd('\0');
		}

		/// <summary>
		/// Lists the instance IDs of every device created by an enumerator (e.g. "BTHENUM")
		/// </summary>
		public static string[] GetDeviceIds(string enumerator) {
			uint length;
			if(CM_Get_Device_ID_List_SizeW(out length, enumerator, CM_GETIDLIST_FILTER_ENUMERATOR) != CR_SUCCESS || length == 0) {
				return new string[0];
			}

			char[] buffer = new char[length];
			if(CM_Get_Device_ID_ListW(enumerator, buffer, length, CM_GETIDLIST_FILTER_ENUMERATOR) != CR_SUCCESS) {
				return new string[0];
			}
			return new string(buffer).Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
		}
	}
}
