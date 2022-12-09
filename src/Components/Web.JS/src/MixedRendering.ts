// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Blazor } from './GlobalExports';
import { ComponentParameters, DynamicRootComponent } from './Rendering/JSRootComponents';

export const MixedRendering = {
  setParameters: async (containerElement: ContainerElement, identifier: string, parameters: ComponentParameters, appId: number) => {
    const component = await containerElement.component;
    if (component) {
      component.setParameters(parameters);
    } else {
      containerElement.component = Blazor.rootComponents.add(
        containerElement,
        identifier,
        parameters,
        appId,
      );
    }
  },
  dispose: async (containerElement: ContainerElement) => {
    const component = await containerElement.component;
    if (component) {
      component.dispose();
      delete containerElement.component;
    }
  },
};

interface ContainerElement extends HTMLElement {
  component?: Promise<DynamicRootComponent>
}
