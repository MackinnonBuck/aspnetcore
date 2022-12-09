// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server;

internal class ClientProxyComponent : IComponent, IHandleAfterRender, IAsyncDisposable
{
    private readonly string _identifier;
    private readonly IJSRuntime _jsRuntime;

    private RenderHandle _renderHandle;
    private ElementReference _containerElementReference;
    private IReadOnlyDictionary<string, object>? _pendingParameters;
    private bool _isInitialized;

    public ClientProxyComponent(string identifier, IJSRuntime jsRuntime)
    {
        _identifier = identifier;
        _jsRuntime = jsRuntime;
    }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        _pendingParameters = parameters.ToDictionary();

        _renderHandle.Render(builder =>
        {
            builder.OpenElement(0, "bl-wrapper");
            builder.AddElementReferenceCapture(1, elementReference => _containerElementReference = elementReference);
            builder.CloseElement();
        });

        return Task.CompletedTask;
    }

    async Task IHandleAfterRender.OnAfterRenderAsync()
    {
        if (_pendingParameters is null)
        {
            return;
        }

        var parameters = _pendingParameters;
        _pendingParameters = null;
        _isInitialized = true;

        await _jsRuntime.InvokeVoidAsync(
            "Blazor._internal.MixedRendering.setParameters",
            _containerElementReference,
            _identifier,
            parameters,
            2 // App ID 2 because we're adding a root component on the client
        );
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_isInitialized)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync(
                    "Blazor._internal.MixedRendering.dispose",
                    _containerElementReference);
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }
}
