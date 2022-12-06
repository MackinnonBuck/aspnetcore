// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { shouldAutoStart } from './BootCommon';
import { boot as bootServer } from './Config.Server';
import { boot as bootWebAssembly } from './Config.WebAssembly';
import { Blazor } from './GlobalExports';
import { CircuitStartOptions } from './Platform/Circuits/CircuitStartOptions';
import { Module } from './Platform/Mono/MonoPlatform';
import { WebAssemblyStartOptions } from './Platform/WebAssemblyStartOptions';

interface ComboStartOptions extends CircuitStartOptions, WebAssemblyStartOptions {
  bootMode: 'webassembly' | 'server' | undefined;
}

async function boot(userOptions?: Partial<ComboStartOptions>): Promise<void> {
  if (!userOptions?.bootMode || userOptions.bootMode === 'server') {
    await bootServer(userOptions);
  }

  if (!userOptions?.bootMode || userOptions.bootMode === 'webassembly') {
    await bootWebAssembly({
      ...userOptions,
      yieldNavigationControl: true,
    });
  }
}

Blazor.start = boot;

if (shouldAutoStart()) {
  boot().catch(error => {
    if (typeof Module !== 'undefined' && Module.printErr) {
      // Logs it, and causes the error UI to appear
      Module.printErr(error);
    } else {
      // The error must have happened so early we didn't yet set up the error UI, so just log to console
      console.error(error);
    }
  });
}
