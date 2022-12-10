// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Demo;

internal static class DemoUtils
{
    public static string PlatformClass => OperatingSystem.IsBrowser() ? "client-boundary" : "server-boundary";
}
