// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// The default activator for creating <see cref="IComponent"/> instances.
/// </summary>
public sealed class DefaultComponentActivator : IComponentActivator
{
    /// <summary>
    /// Gets the default component activator instance.
    /// </summary>
    public static IComponentActivator Instance { get; } = new DefaultComponentActivator();

    internal DefaultComponentActivator()
    {
    }

    /// <inheritdoc />
    public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
    {
        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
        }

        return (IComponent)Activator.CreateInstance(componentType)!;
    }
}
