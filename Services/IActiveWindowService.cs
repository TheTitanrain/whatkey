using System;

namespace WhatKey.Services
{
    public interface IActiveWindowService
    {
        string GetActiveProcessName();
        (string ProcessName, IntPtr Hwnd) GetActiveWindowInfo();
    }
}
