// Copyright (c) hippieZhou. All rights reserved.

using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets.Providers;
using WinRT;

namespace BinggoWallpapers.WidgetProvider.Com;

/// <summary>
/// COM GUID 常量
/// </summary>
internal static class ComGuids
{
    public const string IClassFactory = "00000001-0000-0000-C000-000000000046";
    public const string IUnknown = "00000000-0000-0000-C000-000000000046";
}

/// <summary>
/// COM 类工厂接口
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid(ComGuids.IClassFactory)]
internal interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);

    [PreserveSig]
    int LockServer(bool fLock);
}

/// <summary>
/// Widget Provider 工厂
/// </summary>
internal sealed class WidgetProviderFactory : IClassFactory
{
    private static readonly Guid IUnknownGuid = Guid.Parse(ComGuids.IUnknown);
    private static readonly Guid WidgetProviderInterfaceGuid = typeof(IWidgetProvider).GUID;

    private const int S_OK = 0;
    private const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);
    private const int E_NOINTERFACE = unchecked((int)0x80004002);

    private readonly Func<IWidgetProvider> _providerFactory;

    /// <summary>
    /// 初始化 <see cref="WidgetProviderFactory"/> 的新实例
    /// </summary>
    /// <param name="providerFactory">Provider 工厂函数</param>
    public WidgetProviderFactory(Func<IWidgetProvider> providerFactory)
    {
        _providerFactory = providerFactory;
    }

    /// <inheritdoc/>
    public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
    {
        ppvObject = IntPtr.Zero;

        if (pUnkOuter != IntPtr.Zero)
        {
            return CLASS_E_NOAGGREGATION;
        }

        var classGuid = typeof(WidgetProvider).GUID;
        if (riid != classGuid && riid != WidgetProviderInterfaceGuid && riid != IUnknownGuid)
        {
            return E_NOINTERFACE;
        }

        var provider = _providerFactory();
        ppvObject = MarshalInspectable<IWidgetProvider>.FromManaged(provider);
        return S_OK;
    }

    /// <inheritdoc/>
    public int LockServer(bool fLock) => S_OK;
}
