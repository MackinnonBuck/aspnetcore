// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly;

internal sealed class ServerProxyComponent : IComponent, IHandleAfterRender, IAsyncDisposable
{
    private readonly string _identifier;
    private readonly IJSInProcessRuntime _jsRuntime = default!;

    private RenderHandle _renderHandle;
    private ElementReference _containerElementReference;
    private IReadOnlyDictionary<string, object>? _pendingParameters;
    private bool _isInitialized;

    public ServerProxyComponent(string identifier, IJSRuntime jsRuntime)
    {
        _identifier = identifier;
        _jsRuntime = (IJSInProcessRuntime)jsRuntime;
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

    Task IHandleAfterRender.OnAfterRenderAsync()
    {
        if (_pendingParameters is null)
        {
            return Task.CompletedTask;
        }

        _jsRuntime.InvokeVoid(
            "Blazor._internal.MixedRendering.setParameters",
            _containerElementReference,
            _identifier,
            _pendingParameters,
            1 // App ID 1 because we're adding a root component on the server
        );

        _isInitialized = true;

        return Task.CompletedTask;
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_isInitialized)
        {
            _jsRuntime.InvokeVoid(
                "Blazor._internal.MixedRendering.dispose",
                _containerElementReference);
        }

        return ValueTask.CompletedTask;
    }
}
