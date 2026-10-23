using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BuffTimer;
internal sealed class KeyboardHook : IDisposable
{
    private delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
    private readonly HookProc callback;
    private readonly HashSet<int> held = new();
    private IntPtr handle;
    public event Action<int>? Pressed;
    public KeyboardHook()
    {
        callback = Process;
        handle = SetWindowsHookEx(13, callback, GetModuleHandle(null), 0);
        if (handle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    private IntPtr Process(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0)
        {
            int key = Marshal.ReadInt32(data);
            int msg = message.ToInt32();
            if (msg == 0x100 || msg == 0x104)
            {
                if (held.Add(key)) Pressed?.Invoke(key);
            }
            else if (msg == 0x101 || msg == 0x105) held.Remove(key);
        }
        return CallNextHookEx(handle, code, message, data);
    }
    public void Dispose() { if (handle != IntPtr.Zero) { UnhookWindowsHookEx(handle); handle = IntPtr.Zero; } }
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr module, uint thread);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);
}
