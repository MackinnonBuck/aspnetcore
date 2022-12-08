(() => {
    window.__combo = {
        setParameters: async (containerElement, identifier, parameters, appId) => {
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
        dispose: async (containerElement) => {
            const component = await containerElement.component;
            if (component) {
                component.dispose();
                delete containerElement.component;
            }
        }
    };
})();
