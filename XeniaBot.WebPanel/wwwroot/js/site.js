// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

/**
 * @typedef {object} CreateBootstrapToastOptions
 * @property {string|null} [iconUrl] - Url of an icon to display to the left of the title
 * @property {string|null} [iconAlt] - alt text for icon (when provided)
 * @property {string|null} [title] - Title to display for the toast
 * @property {string|null} [innerHTML] - When provided, the innerHTML of the body will be set to this value, and it overrides the `text` property.
 * @property {string|null} [text] - Text to display in the body of the toast. Can be overridden by property `innerHTML`
 * @property {bool|null} [autohide=false] - If the toast should automatically hide after a certain amount of time.
 * @property {number|null} [autohideDelay=5000] - Delay in milliseconds until the toast should close, when `autohide` is enabled.
 */

const xeniaUtil = {
    /**
     * @description
     * Generate a UUIDv4 string.
     * @returns {string} Generated UUIDv4
     */
    uuidv4: function () {
        return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c =>
            (+c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> +c / 4).toString(16)
        );
    },
    /**
     * @param {string|boolean|number|BigInt} value 
     * @param {number} fallback 
     * @returns {number}
     */
    parseFloatEx: function(value, fallback) {
        if (typeof value === 'string') {
            return parseFloat(value);
        } else if (typeof value === 'number' || typeof value === 'bigint') {
            return value;
        } else if (typeof value === 'boolean') {
            return value ? 1.0 : 0.0;
        } else {
            return fallback;
        }
    },
    /**
     * @param {string|boolean|number|BigInt} value 
     * @param {number|BigInt} fallback 
     * @returns {number|BigInt}
     */
    parseIntEx: function(value, fallback) {
        let rs = null;
        if (typeof value === 'string') {
            rs = value;
        } else if (typeof value === 'boolean') {
            rs = value ? 1 : 0;
        } else if (typeof value === 'number' || typeof value === 'bigint') {
            rs = value.toString();
        }

        const r = rs ? parseInt(rs) : null;

        const maxsafe = 9007199254740991n;
        if (r && r >= maxsafe) {
            return BigInt(value);
        }
        if (r === null || r === NaN || r === undefined) {
            return fallback;
        } else if (r >= maxsafe) {
            return BigInt(value);
        }
    }
};

const xeniaDiscord = {
    /**
     * @description
     * Create and show a bootstrap toast
     * @param {CreateBootstrapToastOptions} options - Options to use when creating the toast.
     * @returns {HTMLDivElement}
     */
    createBootstrapToast: function (options) {
        const div = document.createElement('div');
        div.className = 'toast';
        div.setAttribute('role', 'alert');
        div.setAttribute('aria-live', 'assertive');
        div.setAttribute('aria-atomic', 'true');
        
        const header = document.createElement('div');
        header.className = 'toast-header';
        
        if (typeof options.iconUrl === 'string' && options.iconUrl.trim().length > 0) {
            const img = document.createElement('img');
            img.src = options.iconUrl;
            img.className = 'rounded me-2';
            if (options.iconAlt && typeof options.iconAlt === 'string') {
                img.alt = options.iconAlt;
            }
            header.appendChild(img);
        }
        
        const title = document.createElement('strong');
        title.className = 'mr-auto';
        if (typeof options.title === 'string' && options.title.length > 0) {
            title.innerText = options.title;
        }
        header.appendChild(title);

        // <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
        // <button type="button" class="ml-2 mb-1 close" data-dismiss="toast" aria-label="Close"><span aria-hidden="true">&times;</span></button>
        const closeButton = document.createElement('button');
        closeButton.type = 'button';
        closeButton.className = 'btn-close';
        closeButton.setAttribute('data-dismiss', 'toast');
        closeButton.setAttribute('aria-label', 'Close');
        closeButton.innerHTML = '<span aria-hidden="true">&times;</span>';
        header.appendChild(closeButton);

        div.appendChild(header);
        

        if (typeof options.innerHTML === 'string' || typeof options.text === 'string') {
            const body = document.createElement('div');
            body.className = 'toast-body';

            if (options.innerHTML && typeof options.innerHTML === 'string') {
                body.innerHTML = options.innerHTML;
            } else if (options.text && typeof options.text === 'string') {
                body.innerText = options.text;
            }
            div.appendChild(body);
        }

        let autohide = false;
        if (options.autohide !== null && options.autohide !== undefined) {
            if (typeof options.autohide === 'string') {
                let trm = options.autohide.trim();
                if (trm == 'true' || trm == 'y' || trm == 'yes') {
                    trm = '1';
                } else {
                    trm = '0';
                }
                const numps = xeniaUtil.parseIntEx(trm);
                if (numps) {
                    autohide = typeof numps === 'bigint' ? numps >= BigInt(1) : numps >= 1;
                }
            } else if (typeof options.autohide === 'boolean') {
                autohide = options.autohide === true;
            } else if (typeof options.autohide === 'number') {
                autohide = options.autohide >= 1.0;
            } else if (typeof options.autohide === 'bigint') {
                autohide = options.autohide >= BigInt('1');
            }
        }

        // append and show toast.
        document.querySelector(".toast-container").appendChild(div);
        const toastOptions = {
            animation: true,
            autohide,
            delay: xeniaUtil.parseIntEx(options.autohideDelay) || 5000
        };
        console.debug('[xeniaDiscord.createBootstrapToast] options', toastOptions);
        $(div).toast(toastOptions);
        $(div).toast('show');
        return div;
    }
};

window.xeniaUtil = xeniaUtil;
window.xeniaDiscord = xeniaDiscord;
