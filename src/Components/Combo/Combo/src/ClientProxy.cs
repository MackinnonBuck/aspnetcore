// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Combo.Infrastructure;

public sealed class ClientProxy<TComponent> : IComponent, IHandleAfterRender, IAsyncDisposable where TComponent : IComponent
{
    private static readonly string s_identifier = typeof(TComponent).Name;

    private RenderHandle _renderHandle;
    private ElementReference _containerElementReference;
    private IReadOnlyDictionary<string, object>? _pendingParameters;
    private bool _isInitialized;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

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

        await JSRuntime.InvokeVoidAsync(
            "__combo.setParameters",
            _containerElementReference,
            s_identifier,
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
                await JSRuntime.InvokeVoidAsync(
                    "__combo.dispose",
                    _containerElementReference);
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }
}
