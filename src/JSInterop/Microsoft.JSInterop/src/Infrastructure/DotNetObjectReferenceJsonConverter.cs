// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class DotNetObjectReferenceJsonConverter<[DynamicallyAccessedMembers(JSInvokable)] TValue> : JsonConverter<DotNetObjectReference<TValue>> where TValue : class
{
    private readonly long _appId;

    public DotNetObjectReferenceJsonConverter(JSRuntime jsRuntime)
    {
        JSRuntime = jsRuntime;

        // FIXME: This might not be the right place for this.
        // We might also change this in the future if we allow multiple Blazor apps of the
        // same kind to run in the same document.
        _appId = OperatingSystem.IsBrowser() ? 2 : 1;
    }

    private static JsonEncodedText DotNetObjectRefKey => DotNetDispatcher.DotNetObjectRefKey;

    private static JsonEncodedText DotNetAppKey => DotNetDispatcher.DotNetAppKey;

    public JSRuntime JSRuntime { get; }

    public override DotNetObjectReference<TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        long dotNetObjectId = 0;
        long dotNetAppId = 0;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                if (dotNetObjectId == 0 && reader.ValueTextEquals(DotNetObjectRefKey.EncodedUtf8Bytes))
                {
                    reader.Read();
                    dotNetObjectId = reader.GetInt64();
                }
                else if (dotNetAppId == 0 && reader.ValueTextEquals(DotNetAppKey.EncodedUtf8Bytes))
                {
                    reader.Read();
                    dotNetAppId = reader.GetInt64();
                }
                else
                {
                    throw new JsonException($"Unexpected JSON property {reader.GetString()}.");
                }
            }
            else
            {
                throw new JsonException($"Unexpected JSON Token {reader.TokenType}.");
            }
        }

        if (dotNetObjectId is 0)
        {
            throw new JsonException($"Required property {DotNetObjectRefKey} not found.");
        }

        if (dotNetAppId is 0)
        {
            throw new JsonException($"Required property {DotNetAppKey} not found.");
        }

        if (dotNetAppId != _appId)
        {
            throw new InvalidOperationException($"Expected an app ID of {_appId}, but got {dotNetAppId} instead.");
        }

        var value = (DotNetObjectReference<TValue>)JSRuntime.GetObjectReference(dotNetObjectId);
        return value;
    }

    public override void Write(Utf8JsonWriter writer, DotNetObjectReference<TValue> value, JsonSerializerOptions options)
    {
        var objectId = JSRuntime.TrackObjectReference<TValue>(value);

        writer.WriteStartObject();
        writer.WriteNumber(DotNetObjectRefKey, objectId);
        writer.WriteNumber(DotNetAppKey, _appId);
        writer.WriteEndObject();
    }
}
