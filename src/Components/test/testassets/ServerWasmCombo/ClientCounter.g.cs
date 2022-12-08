// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Combo.Infrastructure;

namespace ServerWasmCombo;

public class ClientCounter : IComponent
{
    private readonly Type _underlyingComponentType;
    private RenderHandle _renderHandle;

    [Parameter]
    public int IncrementAmount { get; set; }

    public ClientCounter()
    {
        _underlyingComponentType = OperatingSystem.IsBrowser()
            ? typeof(ClientCounter_client)
            : typeof(ClientProxy<ClientCounter_client>);
    }

    public void Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        var parameterDictionary = parameters.ToDictionary();

        _renderHandle.Render((builder) =>
        {
            builder.OpenComponent(0, _underlyingComponentType);
            builder.AddMultipleAttributes(1, parameterDictionary);
            builder.CloseComponent();
        });

        return Task.CompletedTask;
    }
}
