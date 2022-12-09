// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web;

internal class ProxyComponent : IComponent, IHandleAfterRender, IAsyncDisposable
{
    private readonly string _identifier;
    private readonly IJSRuntime _jsRuntime;
    private readonly int _targetAppId;
    private readonly Dictionary<string, object> _lastParameters = new();

    private RenderHandle _renderHandle;
    private ElementReference _containerElementReference;
    private bool _isInitialized;

    private EventCallbackCollection? _eventCallbackCollection;
    private EventCallbackOfTCollection? _eventCallbackOfTCollection;

    public ProxyComponent(string identifier, IJSRuntime jsRuntime, int targetAppId)
    {
        _identifier = identifier;
        _jsRuntime = jsRuntime;
        _targetAppId = targetAppId;
    }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        _lastParameters.Clear();
        _eventCallbackCollection?.Clear();
        _eventCallbackOfTCollection?.Clear();

        foreach (var parameter in parameters)
        {
            var parameterType = parameter.Value.GetType();

            if (parameterType == typeof(EventCallback))
            {
                _eventCallbackCollection ??= new();
                _eventCallbackCollection.Set(parameter.Name, (EventCallback)parameter.Value);
            }
            else if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(EventCallback<>))
            {
                _eventCallbackOfTCollection ??= new();
                _eventCallbackOfTCollection.Set(parameter.Name, parameter.Value);
            }
            else
            {
                _lastParameters[parameter.Name] = parameter.Value;
            }
        }

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
        _isInitialized = true;

        await _jsRuntime.InvokeVoidAsync(
            "Blazor._internal.MixedRendering.setParameters",
            _containerElementReference,
            _identifier,
            _lastParameters,
            _eventCallbackCollection?.Proxy,
            _eventCallbackOfTCollection?.Proxy,
            _targetAppId
        );
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        // FIXME: What if the Dispose() method of the underlying component invokes the event callback?
        _eventCallbackCollection?.Dispose();
        _eventCallbackOfTCollection?.Dispose();

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

    private sealed class EventCallbackCollection : IDisposable
    {
        private readonly Dictionary<string, EventCallback> _eventCallbacksByName = new();
        private readonly DotNetObjectReference<EventCallbackCollection> _selfReference;

        public object Proxy { get; }

        public EventCallbackCollection()
        {
            _selfReference = DotNetObjectReference.Create(this);
            Proxy = new JSProxy()
            {
                DotNetObject = _selfReference,
                EventCallbackNames = _eventCallbacksByName.Keys,
            };
        }

        public void Set(string name, EventCallback eventCallback)
        {
            _eventCallbacksByName[name] = eventCallback;
        }

        public void Clear()
        {
            _eventCallbacksByName.Clear();
        }

        [JSInvokable]
        public async Task InvokeEventCallbackAsync(string eventCallbackName)
        {
            if (_eventCallbacksByName.TryGetValue(eventCallbackName, out var eventCallback))
            {
                await eventCallback.InvokeAsync();
            }
        }

        public void Dispose()
        {
            _selfReference.Dispose();
        }

        // TODO: Use a JsonConverter instead.
        private sealed class JSProxy
        {
            public required DotNetObjectReference<EventCallbackCollection> DotNetObject { get; set; }
            public required IReadOnlyCollection<string> EventCallbackNames { get; set; }
        }
    }

    private sealed class EventCallbackOfTCollection : IDisposable
    {
        // TODO: Don't need two types, the proxy can be simplified into one type.
        private readonly Dictionary<string, IEventCallbackWrapper> _eventCallbackWrappersByName = new();
        private readonly HashSet<string> _suppliedEventCallbackNames = new();
        private readonly List<JSEventCallbackOfTWrapperProxy> _proxies = new();

        public object Proxy => _proxies;

        public void Set(string name, object eventCallback)
        {
            if (!_eventCallbackWrappersByName.TryGetValue(name, out var eventCallbackWrapper))
            {
                var argumentType = eventCallback.GetType().GetGenericArguments()[0];
                var wrapperType = typeof(EventCallbackWrapper<>).MakeGenericType(argumentType);
                eventCallbackWrapper = (IEventCallbackWrapper)Activator.CreateInstance(wrapperType)!;
                _eventCallbackWrappersByName.Add(name, eventCallbackWrapper);
                _proxies.Add(new()
                {
                    EventCallbackName = name,
                    DotNetObject = eventCallbackWrapper.DotNetObject,
                });
            }

            eventCallbackWrapper.SetEventCallback(eventCallback);
            _suppliedEventCallbackNames.Add(name);
        }

        public void Clear()
        {
            _suppliedEventCallbackNames.Clear();
        }

        public void Dispose()
        {
            foreach (var wrapper in _eventCallbackWrappersByName.Values)
            {
                wrapper.Dispose();
            }

            _eventCallbackWrappersByName.Clear();
        }

        private sealed class JSEventCallbackOfTWrapperProxy
        {
            public required object DotNetObject { get; set; }
            public required string EventCallbackName { get; set; }
        }

        private interface IEventCallbackWrapper : IDisposable
        {
            object DotNetObject { get; }
            void SetEventCallback(object eventCallback);
        }

        private sealed class EventCallbackWrapper<T> : IEventCallbackWrapper
        {
            private readonly DotNetObjectReference<EventCallbackWrapper<T>> _selfReference;
            private EventCallback<T> _eventCallback;

            public object DotNetObject => _selfReference;

            public EventCallbackWrapper()
            {
                _selfReference = DotNetObjectReference.Create(this);
            }

            public void SetEventCallback(object eventCallback)
            {
                _eventCallback = (EventCallback<T>)eventCallback;
            }

            [JSInvokable]
            public Task InvokeAsync(T? arg)
            {
                return _eventCallback.InvokeAsync(arg);
            }

            public void Dispose()
            {
                _selfReference.Dispose();
            }
        }
    }
}
