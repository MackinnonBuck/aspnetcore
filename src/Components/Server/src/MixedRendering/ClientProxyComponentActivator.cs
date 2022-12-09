// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server;

internal class ClientProxyComponentActivator : IComponentActivator
{
    private readonly IComponentActivator _underlyingActivator;
    private readonly IJSRuntime _jsRuntime;
    private readonly MixedRenderingManager _mixedRenderingManager;

    public ClientProxyComponentActivator(
        IJSRuntime jsRuntime,
        MixedRenderingManager mixedRenderingManager)
    {
        _underlyingActivator = DefaultComponentActivator.Instance;
        _jsRuntime = jsRuntime;
        _mixedRenderingManager = mixedRenderingManager;
    }

    public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
    {
        if (_mixedRenderingManager.TryGetClientProxyIdentifier(componentType, out var identifier))
        {
            return new ClientProxyComponent(identifier, _jsRuntime);
        }    

        return _underlyingActivator.CreateInstance(componentType);
    }
}
