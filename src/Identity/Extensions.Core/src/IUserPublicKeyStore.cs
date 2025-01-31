// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides an abstraction for a store containing users' public keys
/// </summary>
/// <typeparam name="TUser">The type encapsulating a user.</typeparam>
#pragma warning disable RS0016 // Add public types and members to the declared API
public interface IUserPublicKeyStore<TUser> : IUserStore<TUser> where TUser : class
{
    /// <summary>
    /// Sets the public key for the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user whose public key to set.</param>
    /// <param name="publicKey">The public key to set.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task SetPublicKeyAsync(TUser user, string? publicKey, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the public key for the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user whose public key to retrieve.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, returning the public key for the specified <paramref name="user"/>.</returns>
    Task<string?> GetPublicKeyAsync(TUser user, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a flag indicating whether the specified <paramref name="user"/> has a public key.
    /// </summary>
    /// <param name="user">The user to return a flag for, indicating whether they have a public key or not.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, returning true if the specified <paramref name="user"/> has a public key
    /// otherwise false.
    /// </returns>
    Task<bool> HasPublicKeyAsync(TUser user, CancellationToken cancellationToken);
}
#pragma warning restore RS0016 // Add public types and members to the declared API
