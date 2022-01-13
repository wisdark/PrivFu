﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SwitchPriv.Interop;

namespace SwitchPriv.Library
{
    public class Helpers
    {
        public static bool DisableSinglePrivilege(IntPtr hToken, Win32Struct.LUID priv)
        {
            int error;

            Win32Struct.TOKEN_PRIVILEGES tp = new Win32Struct.TOKEN_PRIVILEGES(1);
            tp.Privilege[0].Luid = priv;
            tp.Privilege[0].Attributes = 0;

            IntPtr pTokenPrivilege = Marshal.AllocHGlobal(Marshal.SizeOf(tp));
            Marshal.StructureToPtr(tp, pTokenPrivilege, true);

            if (!Win32Api.AdjustTokenPrivileges(
                hToken,
                false,
                pTokenPrivilege,
                0,
                IntPtr.Zero,
                IntPtr.Zero))
            {
                error = Marshal.GetLastWin32Error();
                Console.WriteLine("[-] Failed to disable {0}.", GetPrivilegeName(priv));
                Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(error));

                return false;
            }

            return true;
        }

        public static bool EnableSinglePrivilege(IntPtr hToken, Win32Struct.LUID priv)
        {
            int error;

            Win32Struct.TOKEN_PRIVILEGES tp = new Win32Struct.TOKEN_PRIVILEGES(1);
            tp.Privilege[0].Luid = priv;
            tp.Privilege[0].Attributes = (uint)Win32Const.PrivilegeAttributeFlags.SE_PRIVILEGE_ENABLED;

            IntPtr pTokenPrivilege = Marshal.AllocHGlobal(Marshal.SizeOf(tp));
            Marshal.StructureToPtr(tp, pTokenPrivilege, true);

            if (!Win32Api.AdjustTokenPrivileges(
                hToken,
                false,
                pTokenPrivilege,
                0,
                IntPtr.Zero,
                IntPtr.Zero))
            {
                error = Marshal.GetLastWin32Error();
                Console.WriteLine("[-] Failed to enable {0}.", GetPrivilegeName(priv));
                Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(error));

                return false;
            }

            return true;
        }

        public static Dictionary<Win32Struct.LUID, uint> GetAvailablePrivileges(IntPtr hToken)
        {
            int ERROR_INSUFFICIENT_BUFFER = 122;
            int error = ERROR_INSUFFICIENT_BUFFER;
            bool status = false;
            int bufferLength = Marshal.SizeOf(typeof(Win32Struct.TOKEN_PRIVILEGES));
            Dictionary<Win32Struct.LUID, uint> availablePrivs = new Dictionary<Win32Struct.LUID, uint>();
            IntPtr pTokenPrivileges = IntPtr.Zero;

            while (!status && (error == ERROR_INSUFFICIENT_BUFFER))
            {
                pTokenPrivileges = Marshal.AllocHGlobal(bufferLength);
                ZeroMemory(pTokenPrivileges, bufferLength);

                status = Win32Api.GetTokenInformation(
                    hToken,
                    Win32Const.TOKEN_INFORMATION_CLASS.TokenPrivileges,
                    pTokenPrivileges,
                    bufferLength,
                    out bufferLength);

                if (!status)
                {
                    error = Marshal.GetLastWin32Error();
                    Marshal.FreeHGlobal(pTokenPrivileges);
                }
            }

            if (!status)
                return availablePrivs;

            int privCount = Marshal.ReadInt32(pTokenPrivileges);
            IntPtr buffer = new IntPtr(pTokenPrivileges.ToInt64() + Marshal.SizeOf(privCount));

            for (var count = 0; count < privCount; count++)
            {
                var luidAndAttr = (Win32Struct.LUID_AND_ATTRIBUTES)Marshal.PtrToStructure(
                    buffer,
                    typeof(Win32Struct.LUID_AND_ATTRIBUTES));

                availablePrivs.Add(luidAndAttr.Luid, luidAndAttr.Attributes);
                buffer = new IntPtr(buffer.ToInt64() + Marshal.SizeOf(luidAndAttr));
            }

            Marshal.FreeHGlobal(pTokenPrivileges);

            return availablePrivs;
        }

        public static string GetFullPrivilegeName(string shortenName)
        {
            StringComparison opt = StringComparison.OrdinalIgnoreCase;

            if (string.Compare(shortenName, "CreateToken", opt) == 0)
                return "SeCreateTokenPrivilege";
            else if (string.Compare(shortenName, "AssignPrimaryToken", opt) == 0)
                return "SeAssignPrimaryTokenPrivilege";
            else if (string.Compare(shortenName, "LockMemory", opt) == 0)
                return "SeLockMemoryPrivilege";
            else if (string.Compare(shortenName, "IncreaseQuota", opt) == 0)
                return "SeIncreaseQuotaPrivilege";
            else if (string.Compare(shortenName, "MachineAccount", opt) == 0)
                return "SeMachineAccountPrivilege";
            else if (string.Compare(shortenName, "Tcb", opt) == 0)
                return "SeTcbPrivilege";
            else if (string.Compare(shortenName, "Security", opt) == 0)
                return "SeSecurityPrivilege";
            else if (string.Compare(shortenName, "TakeOwnership", opt) == 0)
                return "SeTakeOwnershipPrivilege";
            else if (string.Compare(shortenName, "LoadDriver", opt) == 0)
                return "SeLoadDriverPrivilege";
            else if (string.Compare(shortenName, "SystemProfile", opt) == 0)
                return "SeSystemProfilePrivilege";
            else if (string.Compare(shortenName, "Systemtime", opt) == 0)
                return "SeSystemtimePrivilege";
            else if (string.Compare(shortenName, "ProfileSingleProcess", opt) == 0)
                return "SeProfileSingleProcessPrivilege";
            else if (string.Compare(shortenName, "IncreaseBasePriority", opt) == 0)
                return "SeIncreaseBasePriorityPrivilege";
            else if (string.Compare(shortenName, "CreatePagefile", opt) == 0)
                return "SeCreatePagefilePrivilege";
            else if (string.Compare(shortenName, "CreatePermanent", opt) == 0)
                return "SeCreatePermanentPrivilege";
            else if (string.Compare(shortenName, "Backup", opt) == 0)
                return "SeBackupPrivilege";
            else if (string.Compare(shortenName, "Restore", opt) == 0)
                return "SeRestorePrivilege";
            else if (string.Compare(shortenName, "Shutdown", opt) == 0)
                return "SeShutdownPrivilege";
            else if (string.Compare(shortenName, "Debug", opt) == 0)
                return "SeDebugPrivilege";
            else if (string.Compare(shortenName, "Audit", opt) == 0)
                return "SeAuditPrivilege";
            else if (string.Compare(shortenName, "SystemEnvironment", opt) == 0)
                return "SeSystemEnvironmentPrivilege";
            else if (string.Compare(shortenName, "ChangeNotify", opt) == 0)
                return "SeChangeNotifyPrivilege";
            else if (string.Compare(shortenName, "RemoteShutdown", opt) == 0)
                return "SeRemoteShutdownPrivilege";
            else if (string.Compare(shortenName, "Undock", opt) == 0)
                return "SeUndockPrivilege";
            else if (string.Compare(shortenName, "SyncAgent", opt) == 0)
                return "SeSyncAgentPrivilege";
            else if (string.Compare(shortenName, "EnableDelegation", opt) == 0)
                return "SeEnableDelegationPrivilege";
            else if (string.Compare(shortenName, "ManageVolume", opt) == 0)
                return "SeManageVolumePrivilege";
            else if (string.Compare(shortenName, "Impersonate", opt) == 0)
                return "SeImpersonatePrivilege";
            else if (string.Compare(shortenName, "CreateGlobal", opt) == 0)
                return "SeCreateGlobalPrivilege";
            else if (string.Compare(shortenName, "TrustedCredManAccess", opt) == 0)
                return "SeTrustedCredManAccessPrivilege";
            else if (string.Compare(shortenName, "Relabel", opt) == 0)
                return "SeRelabelPrivilege";
            else if (string.Compare(shortenName, "IncreaseWorkingSet", opt) == 0)
                return "SeIncreaseWorkingSetPrivilege";
            else if (string.Compare(shortenName, "TimeZone", opt) == 0)
                return "SeTimeZonePrivilege";
            else if (string.Compare(shortenName, "CreateSymbolicLink", opt) == 0)
                return "SeCreateSymbolicLinkPrivilege";
            else if (string.Compare(shortenName, "DelegateSessionUserImpersonate", opt) == 0)
                return "SeDelegateSessionUserImpersonatePrivilege";
            else
                return string.Empty;
        }

        public static int GetParentProcessId(IntPtr hProcess)
        {
            var sizeInformation = Marshal.SizeOf(typeof(Win32Struct.PROCESS_BASIC_INFORMATION));
            var buffer = Marshal.AllocHGlobal(sizeInformation);

            if (hProcess == IntPtr.Zero)
                return 0;

            int ntstatus = Win32Api.NtQueryInformationProcess(
                hProcess,
                Win32Const.PROCESSINFOCLASS.ProcessBasicInformation,
                buffer,
                sizeInformation,
                IntPtr.Zero);

            if (ntstatus != Win32Const.STATUS_SUCCESS)
            {
                Console.WriteLine("[-] Failed to get process information.");
                Console.WriteLine("    |-> {0}", GetWin32ErrorMessage(ntstatus));
                Marshal.FreeHGlobal(buffer);
                return 0;
            }

            var basicInfo = (Win32Struct.PROCESS_BASIC_INFORMATION)Marshal.PtrToStructure(
                buffer,
                typeof(Win32Struct.PROCESS_BASIC_INFORMATION));
            int ppid = basicInfo.InheritedFromUniqueProcessId.ToInt32();

            Marshal.FreeHGlobal(buffer);

            return ppid;
        }

        public static bool GetPrivilegeLuid(
            string privilegeName, 
            out Win32Struct.LUID luid)
        {
            int error;

            if (!Win32Api.LookupPrivilegeValue(
                string.Empty,
                privilegeName,
                out luid))
            {
                error = Marshal.GetLastWin32Error();
                Console.WriteLine("[-] Failed to lookup {0}.", privilegeName);
                Console.WriteLine("    |-> {0}", GetWin32ErrorMessage(error));
                return false;
            }

            return true;
        }

        public static string GetPrivilegeName(Win32Struct.LUID priv)
        {
            int error;
            int cchName = 255;
            StringBuilder privilegeName = new StringBuilder(255);

            if (!Win32Api.LookupPrivilegeName(
                string.Empty,
                ref priv,
                privilegeName,
                ref cchName))
            {
                error = Marshal.GetLastWin32Error();
                Console.WriteLine("[-] Failed to lookup privilege name.");
                Console.WriteLine("    |-> {0}", GetWin32ErrorMessage(error));
                return string.Empty;
            }

            return privilegeName.ToString();
        }

        public static string GetWin32ErrorMessage(int code)
        {
            uint FORMAT_MESSAGE_FROM_HMODULE = 0x00000800;
            uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
            StringBuilder message = new StringBuilder(255);

            IntPtr pNtdll = Win32Api.LoadLibrary("ntdll.dll");

            uint status = Win32Api.FormatMessage(
                FORMAT_MESSAGE_FROM_HMODULE | FORMAT_MESSAGE_FROM_SYSTEM,
                pNtdll,
                code,
                0,
                message,
                255,
                IntPtr.Zero);

            Win32Api.FreeLibrary(pNtdll);

            if (status == 0)
            {
                return string.Format("[ERROR] Code 0x{0}\n", code.ToString("X8"));
            }
            else
            {
                return string.Format("[ERROR] Code 0x{0} : {1}\n",
                    code.ToString("X8"),
                    message.ToString().Trim());
            }
        }

        public static bool IsPrivilegeEnabled(
            IntPtr hToken, 
            Win32Struct.LUID priv, 
            out bool isEnabled)
        {
            int error;
            isEnabled = false;

            if (hToken == IntPtr.Zero)
                return false;

            var privSet = new Win32Struct.PRIVILEGE_SET(1, Win32Const.PRIVILEGE_SET_ALL_NECESSARY);
            privSet.Privilege[0].Luid = priv;
            privSet.Privilege[0].Attributes = (uint)Win32Const.PrivilegeAttributeFlags.SE_PRIVILEGE_ENABLED;

            IntPtr pPrivileges = Marshal.AllocHGlobal(Marshal.SizeOf(privSet));
            Marshal.StructureToPtr(privSet, pPrivileges, true);

            if (!Win32Api.PrivilegeCheck(
                hToken,
                pPrivileges,
                out isEnabled))
            {
                error = Marshal.GetLastWin32Error();
                Console.WriteLine("[-] Failed to check the target privilege is enabled.");
                Console.WriteLine("    |-> {0}", GetWin32ErrorMessage(error));
                Marshal.FreeHGlobal(pPrivileges);

                return false;
            }

            Marshal.FreeHGlobal(pPrivileges);

            return true;
        }

        public static void ListPrivilegeOptionValues()
        {
            Console.WriteLine();
            Console.WriteLine("Available values for --enable or --disable option:\n");
            Console.WriteLine("    + CreateToken                    : Specifies SeCreateTokenPrivilege.");
            Console.WriteLine("    + AssignPrimaryToken             : Specifies SeAssignPrimaryTokenPrivilege.");
            Console.WriteLine("    + LockMemory                     : Specifies SeLockMemoryPrivilege.");
            Console.WriteLine("    + IncreaseQuota                  : Specifies SeIncreaseQuotaPrivilege.");
            Console.WriteLine("    + MachineAccount                 : Specifies SeMachineAccountPrivilege.");
            Console.WriteLine("    + Tcb                            : Specifies SeTcbPrivilege.");
            Console.WriteLine("    + Security                       : Specifies SeSecurityPrivilege.");
            Console.WriteLine("    + TakeOwnership                  : Specifies SeTakeOwnershipPrivilege.");
            Console.WriteLine("    + LoadDriver                     : Specifies SeLoadDriverPrivilege.");
            Console.WriteLine("    + SystemProfile                  : Specifies SeSystemProfilePrivilege.");
            Console.WriteLine("    + Systemtime                     : Specifies SeSystemtimePrivilege.");
            Console.WriteLine("    + ProfileSingle                  : Specifies SeProfileSingleProcessPrivilege.");
            Console.WriteLine("    + IncreaseBasePriority           : Specifies SeIncreaseBasePriorityPrivilege.");
            Console.WriteLine("    + CreatePagefile                 : Specifies SeCreatePagefilePrivilege.");
            Console.WriteLine("    + CreatePermanent                : Specifies SeCreatePermanentPrivilege.");
            Console.WriteLine("    + Backup                         : Specifies SeBackupPrivilege.");
            Console.WriteLine("    + Restore                        : Specifies SeRestorePrivilege.");
            Console.WriteLine("    + Shutdown                       : Specifies SeShutdownPrivilege.");
            Console.WriteLine("    + Debug                          : Specifies SeDebugPrivilege.");
            Console.WriteLine("    + Audit                          : Specifies SeAuditPrivilege.");
            Console.WriteLine("    + SystemEnvironment              : Specifies SeSystemEnvironmentPrivilege.");
            Console.WriteLine("    + ChangeNotify                   : Specifies SeChangeNotifyPrivilege.");
            Console.WriteLine("    + RemoteShutdown                 : Specifies SeRemoteShutdownPrivilege.");
            Console.WriteLine("    + Undock                         : Specifies SeUndockPrivilege.");
            Console.WriteLine("    + SyncAgent                      : Specifies SeSyncAgentPrivilege.");
            Console.WriteLine("    + EnableDelegation               : Specifies SeEnableDelegationPrivilege.");
            Console.WriteLine("    + ManageVolume                   : Specifies SeManageVolumePrivilege.");
            Console.WriteLine("    + Impersonate                    : Specifies SeImpersonatePrivilege.");
            Console.WriteLine("    + CreateGlobal                   : Specifies SeCreateGlobalPrivilege.");
            Console.WriteLine("    + TrustedCredManAccess           : Specifies SeTrustedCredManAccessPrivilege.");
            Console.WriteLine("    + Relabel                        : Specifies SeRelabelPrivilege.");
            Console.WriteLine("    + IncreaseWorkingSet             : Specifies SeIncreaseWorkingSetPrivilege.");
            Console.WriteLine("    + TimeZone                       : Specifies SeTimeZonePrivilege.");
            Console.WriteLine("    + CreateSymbolicLink             : Specifies SeCreateSymbolicLinkPrivilege.");
            Console.WriteLine("    + DelegateSessionUserImpersonate : Specifies SeDelegateSessionUserImpersonatePrivilege.");
            Console.WriteLine("    + All                            : Specifies all token privileges.");
            Console.WriteLine();
        }

        public static void ZeroMemory(IntPtr buffer, int size)
        {
            byte[] nullBytes = new byte[size];

            for (var idx = 0; idx < size; idx++)
                nullBytes[idx] = 0;

            Marshal.Copy(nullBytes, 0, buffer, size);
        }
    }
}
