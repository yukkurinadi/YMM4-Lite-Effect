using System;
using System.IO;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("d3dcompiler_47.dll", CallingConvention = CallingConvention.Winapi)]
    public static extern int D3DCompile(
        byte[] srcData,
        IntPtr srcDataLen,
        string sourceName,
        IntPtr defines,
        IntPtr include,
        string entryPoint,
        string target,
        uint flags1,
        uint flags2,
        out IntPtr code,
        out IntPtr errorMsgs);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate IntPtr GetBufferPointerDelegate(IntPtr thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate IntPtr GetBufferSizeDelegate(IntPtr thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate uint ReleaseDelegate(IntPtr thisPtr);

    static void Main()
    {
        string hlslPath = Path.Combine("Shaders", "VoronoiNoiseLite.hlsl");
        string csoPath = Path.Combine("Shaders", "VoronoiNoiseLite.cso");

        string hlsl = File.ReadAllText(hlslPath);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(hlsl);

        // Compile for Direct2D pixel shader target (ps_4_1 or ps_5_0 - D2D custom effects use ps_4_0 / ps_4_1 / ps_5_0)
        // YMM4's voronoinoise.cso is compiled as ps_4_1
        int hr = D3DCompile(bytes, (IntPtr)bytes.Length, "VoronoiNoiseLite.hlsl", IntPtr.Zero, IntPtr.Zero, "main", "ps_4_1", 1, 0, out IntPtr codePtr, out IntPtr errPtr);

        if (errPtr != IntPtr.Zero)
        {
            IntPtr vtbl = Marshal.ReadIntPtr(errPtr);
            var getBufferPointer = Marshal.GetDelegateForFunctionPointer<GetBufferPointerDelegate>(Marshal.ReadIntPtr(vtbl, 3 * IntPtr.Size));
            var release = Marshal.GetDelegateForFunctionPointer<ReleaseDelegate>(Marshal.ReadIntPtr(vtbl, 2 * IntPtr.Size));
            string errMsg = Marshal.PtrToStringAnsi(getBufferPointer(errPtr));
            Console.WriteLine("Compiler Messages/Errors:\n" + errMsg);
            release(errPtr);
        }

        if (hr == 0 && codePtr != IntPtr.Zero)
        {
            IntPtr vtbl = Marshal.ReadIntPtr(codePtr);
            var getBufferPointer = Marshal.GetDelegateForFunctionPointer<GetBufferPointerDelegate>(Marshal.ReadIntPtr(vtbl, 3 * IntPtr.Size));
            var getBufferSize = Marshal.GetDelegateForFunctionPointer<GetBufferSizeDelegate>(Marshal.ReadIntPtr(vtbl, 4 * IntPtr.Size));
            var release = Marshal.GetDelegateForFunctionPointer<ReleaseDelegate>(Marshal.ReadIntPtr(vtbl, 2 * IntPtr.Size));

            IntPtr buf = getBufferPointer(codePtr);
            int size = (int)getBufferSize(codePtr);

            byte[] bytecode = new byte[size];
            Marshal.Copy(buf, bytecode, 0, size);
            File.WriteAllBytes(csoPath, bytecode);
            Console.WriteLine($"Successfully compiled {csoPath} ({size} bytes)!");
            release(codePtr);
        }
        else
        {
            Console.WriteLine($"Compilation failed with HRESULT: 0x{hr:X8}");
        }
    }
}
