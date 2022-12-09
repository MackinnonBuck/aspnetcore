// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';
import { Blazor } from './GlobalExports';
import { ComponentParameters, DynamicRootComponent } from './Rendering/JSRootComponents';

interface EventCallbackCollection {
  dotNetObject: DotNet.DotNetObject;
  eventCallbackNames: string[];
}

interface EventCallbackOfTWrapper {
  dotNetObject: DotNet.DotNetObject;
  eventCallbackName: string;
}

export const MixedRendering = {
  setParameters: async (
    containerElement: ContainerElement,
    identifier: string,
    parameters: ComponentParameters,
    eventCallbacks: EventCallbackCollection | null,
    eventCallbackOfTs: EventCallbackOfTWrapper[] | null,
    appId: number
  ) => {
    let eventCallbackParameters: object | null = null;
    if (eventCallbacks) {
      eventCallbackParameters = {};
      for (const eventCallbackName of eventCallbacks.eventCallbackNames) {
        eventCallbackParameters[eventCallbackName] = () => eventCallbacks.dotNetObject.invokeMethodAsync('InvokeEventCallbackAsync', eventCallbackName);
      }
    }

    if (eventCallbackOfTs) {
      eventCallbackParameters ??= {};
      for (const eventCallback of eventCallbackOfTs) {
        eventCallbackParameters[eventCallback.eventCallbackName] = (arg: object | null) => eventCallback.dotNetObject.invokeMethodAsync('InvokeAsync', arg);
      }
    }

    parameters = {
      ...parameters,
      ...eventCallbackParameters,
    };

    const component = await containerElement.component;
    if (component) {
      component.setParameters(parameters);
    } else {
      const startPromise = Blazor._internal.startPromises.get(appId);
      if (!startPromise) {
        throw new Error('Cannot render a component in an app that has not started.');
      }
      await startPromise;
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
