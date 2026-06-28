// auth-popup.js — JS module for OIDC popup authentication flow
// Communicates between the popup window and the Blazor Server main page via postMessage.

/**
 * Opens a browser popup window navigating to the given URL.
 * @param {string} url - The authorization URL to open in the popup.
 * @returns {Window|null} The popup window reference, or null if blocked.
 */
export function openPopup(url) {
    const width = 500;
    const height = 600;
    const left = (screen.width - width) / 2;
    const top = (screen.height - height) / 2;

    return window.open(
        url,
        'aura-auth-popup',
        `width=${width},height=${height},left=${left},top=${top},scrollbars=yes`
    );
}

/**
 * Registers a message event listener that waits for an auth result from the popup.
 * Returns a promise that resolves with the auth result.
 * @param {number} timeoutMs - Timeout in milliseconds (default: 120000 = 2 minutes).
 * @returns {Promise<{type: string, token?: string, error?: string}>}
 */
export function listenForAuthResult(timeoutMs = 120000) {
    return new Promise((resolve, reject) => {
        const timer = setTimeout(() => {
            window.removeEventListener('message', handler);
            reject(new Error('Authentication timed out'));
        }, timeoutMs);

        function handler(event) {
            if (event.data && event.data.type === 'auth-success') {
                clearTimeout(timer);
                window.removeEventListener('message', handler);
                resolve({ type: 'auth-success', token: event.data.token });
            } else if (event.data && event.data.type === 'auth-error') {
                clearTimeout(timer);
                window.removeEventListener('message', handler);
                resolve({ type: 'auth-error', error: event.data.error });
            }
        }

        window.addEventListener('message', handler);
    });
}

/**
 * Closes the current popup window (called from the popup page itself).
 */
export function closePopup() {
    if (window.opener) {
        window.close();
    }
}

/**
 * Constructs a full OIDC authorization URL with all required parameters.
 * @param {{ clientId: string, redirectUri: string, scope?: string, state?: string, nonce?: string }} config
 * @returns {string} The complete authorization URL.
 */
export function buildAuthUrl(config) {
    const scope = config.scope || 'openid profile email';
    const state = config.state || generateRandomString(32);
    const nonce = config.nonce || generateRandomString(32);

    const params = new URLSearchParams({
        client_id: config.clientId,
        redirect_uri: config.redirectUri,
        scope: scope,
        response_type: 'code',
        response_mode: 'query',
        state: state,
        nonce: nonce
    });

    return `https://login.microsoftonline.com/common/oauth2/v2.0/authorize?${params.toString()}`;
}

/**
 * Generates a cryptographically random string of the given length.
 * @param {number} length - The desired string length.
 * @returns {string} A random alphanumeric string.
 */
function generateRandomString(length) {
    const array = new Uint8Array(length);
    crypto.getRandomValues(array);
    return Array.from(array, byte => byte.toString(36).padStart(2, '0'))
        .join('')
        .substring(0, length);
}
