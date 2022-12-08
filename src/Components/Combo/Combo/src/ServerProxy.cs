// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Combo.Infrastructure;

public sealed class ServerProxy<TComponent> : IComponent, IHandleAfterRender, IAsyncDisposable where TComponent : IComponent
{
    private static readonly string s_identifier = typeof(TComponent).Name;

    private RenderHandle _renderHandle;
    private ElementReference _containerElementReference;
    private IReadOnlyDictionary<string, object>? _pendingParameters;
    private IJSInProcessRuntime _jsInProcessRuntime = default!;
    private bool _isInitialized;

    [Inject]
    private IJSRuntime JSRuntime
    {
        get => _jsInProcessRuntime;
        set
        {
            if (value is not IJSInProcessRuntime jSInProcessRuntime)
            {
                throw new InvalidOperationException($"{GetType().Name} expected the JS runtime to be an '{nameof(IJSInProcessRuntime)}'.");
            }

            _jsInProcessRuntime = jSInProcessRuntime;
        }
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

        _jsInProcessRuntime.InvokeVoid(
            "__combo.setParameters",
            _containerElementReference,
            s_identifier,
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
            _jsInProcessRuntime.InvokeVoid(
                "__combo.dispose",
                _containerElementReference);
        }

        return ValueTask.CompletedTask;
    }
}
