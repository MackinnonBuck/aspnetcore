// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { shouldAutoStart } from './BootCommon';
import { boot } from './Config.WebAssembly';
import { Blazor } from './GlobalExports';
import { Module } from './Platform/Mono/MonoPlatform';

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
