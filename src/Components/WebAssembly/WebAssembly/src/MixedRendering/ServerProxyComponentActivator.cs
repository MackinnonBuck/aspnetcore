// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly;

internal class ServerProxyComponentActivator : IComponentActivator
{
    private readonly IComponentActivator _underlyingActivator;
    private readonly IJSInProcessRuntime _jsRuntime;
    private readonly IReadOnlyDictionary<Type, string> _identifiersByComponentType;

    public ServerProxyComponentActivator(
        IComponentActivator underlyingActivator,
        IJSInProcessRuntime jsRuntime,
        IReadOnlyDictionary<Type, string> identifiersByComponentType)
    {
        _underlyingActivator = underlyingActivator;
        _jsRuntime = jsRuntime;
        _identifiersByComponentType = identifiersByComponentType;
    }

    public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
    {
        if (_identifiersByComponentType.TryGetValue(componentType, out var identifier))
        {
            return new ServerProxyComponent(identifier, _jsRuntime);
        }

        return _underlyingActivator.CreateInstance(componentType);
    }
}
