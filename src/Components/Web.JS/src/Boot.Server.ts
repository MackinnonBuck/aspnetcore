// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { shouldAutoStart } from './BootCommon';
import { boot } from './Config.Server';
import { Blazor } from './GlobalExports';

Blazor.start = boot;

if (shouldAutoStart()) {
  boot();
}
