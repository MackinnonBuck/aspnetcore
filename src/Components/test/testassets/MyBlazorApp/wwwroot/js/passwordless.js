(() => {
    function base64ToUint8Array(base64) {
        const binary = atob(base64);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        return bytes;
    }

    function guidToUnit8Array(guid) {
        const hex = guid.replace(/[{}-]/g, '');
        const bytes = new Uint8Array(hex.length / 2);
        for (let i = 0; i < hex.length; i += 2) {
            bytes[i / 2] = parseInt(hex.substr(i, 2), 16);
        }
        return bytes;
    }

    async function enableAuthentication() {
        const response = await fetch('Account/MakeCredentialCreationOptions', {
            method: 'POST',
        });

        // 7.1.1. Let 'options' be a new CredentialCreationOptions structure configured to the Relying Party's needs for the ceremony.
        const options = await response.json();

        // 7.1.1. Let 'pkOptions' be 'options.publicKey'.
        const pkOptions = options.publicKey;

        // Convert strings to Uint8Arrays.
        if (typeof pkOptions.challenge === 'string') {
            pkOptions.challenge = base64ToUint8Array(pkOptions.challenge);
        }
        if (typeof pkOptions.user.id === 'string') {
            pkOptions.user.id = guidToUnit8Array(pkOptions.user.id);
        }

        // HACK: We'd actually need to generate this on the server.
        pkOptions.rp.id = window.location.hostname;

        // 7.1.2. Call navigator.credentials.create() and pass 'options' as the argument.
        try {
            // 7.1.2. Let 'credential' be the result of the successfully resolved promise.
            const credential = await navigator.credentials.create(options);

            // 7.1.3. Let 'response' by 'credential.response'.
            const response = credential.response;

            // 7.1.3. If 'response' is an instance of AuthenticatorAttestationResponse, abort the ceremony with a user visible error.
            if (!(response instanceof AuthenticatorAttestationResponse)) {
                throw new Error('The response from the authenticator was in an unexpected format.');
            }

            // 7.1.4. Let 'clientExtensionResults' be 'credential.getClientExtensionResults()'.
            // TODO: Is this needed?
            // const clientExtensionResults = credential.getClientExtensionResults();

            // 7.1.5. Let 'JSONtext' be the result of running UTF-8 decode on the value of 'response.clientDataJSON'.
            const jsonText = new TextDecoder().decode(response.clientDataJSON);

            // 7.1.6. Let C, the client data claimed as collected during the credential creation, be the result of running
            //        an implementation-specific JSON parser on 'JSONtext'.
            // const clientData = JSON.parse(jsonText);

            // Continue the ceremony on the server.
            await fetch('Account/RegisterCredential', {
                method: 'POST',
                body: JSON.stringify({
                    clientDataJson: jsonText,
                    attestationObjectCbor: response.attestationObject,
                }),
                headers: {
                    'Content-Type': 'application/json',
                },
            });
        } catch (error) {
            // 7.1.2. If the promise is rejected, abort the ceremony with a user-visible error.
            // TODO
            throw error;
        }
    }

    window.passwordless = {
        enableAuthentication,
    };
})();
