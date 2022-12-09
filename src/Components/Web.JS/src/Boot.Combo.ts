// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AppIds } from './AppIds';
import { shouldAutoStart } from './BootCommon';
import { boot as bootServer } from './Config.Server';
import { boot as bootWebAssembly } from './Config.WebAssembly';
import { Blazor } from './GlobalExports';
import { CircuitStartOptions } from './Platform/Circuits/CircuitStartOptions';
import { Module } from './Platform/Mono/MonoPlatform';
import { WebAssemblyStartOptions } from './Platform/WebAssemblyStartOptions';

Blazor.startServer = (userOptions?: Partial<CircuitStartOptions>): Promise<void> => {
  const bootServerPromise = bootServer(userOptions);
  Blazor._internal.startPromises.set(AppIds.Server, bootServerPromise);
  return bootServerPromise;
};

Blazor.startWebAssembly = (userOptions?: Partial<WebAssemblyStartOptions>): Promise<void> => {
  userOptions = {
    ...userOptions,
    yieldNavigationControl: true,
  };
  const bootServerPromise = Blazor._internal.startPromises.get(AppIds.Server) || Promise.resolve();
  const bootWebAssemblyPromise = bootServerPromise.then(() => bootWebAssembly(userOptions).catch(error => {
    if (typeof Module !== 'undefined' && Module.printErr) {
      // Logs it, and causes the error UI to appear
      Module.printErr(error);
    } else {
      // The error must have happened so early we didn't yet set up the error UI, so just log to console
      console.error(error);
    }
  }));
  Blazor._internal.startPromises.set(AppIds.WebAssembly, bootWebAssemblyPromise);
  return bootWebAssemblyPromise;
};

if (shouldAutoStart()) {
  Blazor.startServer();
  Blazor.startWebAssembly();
}
