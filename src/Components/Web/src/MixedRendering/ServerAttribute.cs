// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Indicates that a component should only be rendered in a Blazor Server application.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ServerAttribute : Attribute
{
}
