// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server;

internal class MixedRenderingManager
{
    private readonly CircuitOptions _circuitOptions;
    private readonly Dictionary<Type, string> _clientProxyIdentifiersByComponentType = new();

    private bool _isInitialized;

    public MixedRenderingManager(IOptions<CircuitOptions> circuitOptions)
    {
        _circuitOptions = circuitOptions.Value;
    }

    public bool TryGetClientProxyIdentifier(Type componentType, [NotNullWhen(true)] out string? identifier)
        => _clientProxyIdentifiersByComponentType.TryGetValue(componentType, out identifier);

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        var mixedRenderingAssemblies = _circuitOptions.MixedRenderingAssemblies;

        if (mixedRenderingAssemblies.Count == 0)
        {
            return;
        }

        var assemblies = new Assembly[mixedRenderingAssemblies.Count];

        for (var i = 0; i < assemblies.Length; i++)
        {
            assemblies[i] = Assembly.Load(mixedRenderingAssemblies[i]);
        }

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                var isClientOnly = type.GetCustomAttribute<ClientAttribute>() is not null;
                var isServerOnly = type.GetCustomAttribute<ServerAttribute>() is not null;

                if (isClientOnly && isServerOnly)
                {
                    throw new InvalidOperationException(
                        $"The component type '{type.FullName}' should not be annotated with both " +
                        $"'{nameof(ClientAttribute)}' and '{nameof(ServerAttribute)}");
                }

                var identifier = $"bl_{type.FullName}";

                if (isClientOnly)
                {
                    _clientProxyIdentifiersByComponentType.Add(type, identifier);
                }
                else if (isServerOnly)
                {
                    _circuitOptions.RootComponents.RegisterForJavaScript(type, identifier);
                }
            }
        }
    }
}
