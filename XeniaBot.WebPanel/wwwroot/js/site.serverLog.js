const serverLogEvents = [
    'Fallback',

    'MemberJoin',
    'MemberLeave',
    'MemberBan',
    'MemberKick',

    'MessageEdit',
    'MessageDelete',

    'ChannelDelete',
    'ChannelEdit',
    'ChannelCreate',

    'MemberVoiceChange',
    'MemberRoleAdded',
    'MemberRoleRemoved',
    'MemberRoleUpdated',
    'MemberPermissionsUpdated',
    'MemberUpdated',

    'RoleCreate',
    'RoleEdit',
    'RoleDelete'
];

/**
 * @typedef {object} ServerLogChannelItem
 * @property {boolean} [category] - Only set if this is a category
 * @property {boolean} [text] - Only set if this is a text channel
 * @property {boolean} [voice] - Only set if this is a voice channel. Voice channels will not be rendered in dropdown menus for server logging.
 * @property {string} id - Channel Id (snowflake as string)
 * @property {string} name - Channel Name
 * @property {ServerLogChannelItem[]} [children] - Child channels. Only respected if `category` is `true`
 */

/**
 * @typedef {object} ServerLogConfigItem
 * @property {string} id - UUIDv4/Guid
 * @property {string} channelId - Channel Id (snowflake as string)
 * @property {string} event - Event
 */

const serverLogLogic = {
    _qs_jsonDataElement: '#server-log-channel-config input[name=jsonData]',
    _qs_configElement: '#server-log-channel-config',
    _an_channelData: 'xeniaDiscord-channelData',

    /**
     * @returns {ServerLogChannelItem[]}
     */
    getChannelData: function() {
        const element = document.querySelector(this._qs_configElement);
        if (!element) {
            throw new Error('failed to find element: ' + this._qs_configElement);
        }
        const attrValue = element.getAttribute(this._an_channelData);
        if (!attrValue) {
            throw new Error(`missing attribute "${this._an_channelData}" on element "${this._qs_configElement}"`);
        }
        if (typeof attrValue !== 'string') {
            throw new Error(`invalid type "${typeof attrValue}" on attribute "${this._an_channelData}" from element "${this._qs_configElement}"`);
        }
        let contentRaw;
        try {
            contentRaw = atob(attrValue);
        } catch (err) {
            throw new Error(`failed to parse value in attribute "${this._an_channelData}" from element "${this._qs_configElement}"\n${err}`);
        }
        let content = [];
        try {
            content = JSON.parse(contentRaw);
        } catch (err) {
            throw new Error(`failed to parse content in value from attribute "${this._an_channelData}" on element "${this._qs_configElement}"\n${err}\n\n-- content --\n${contentRaw}`);
        }
        return this.validateChannelData(content);
    },
    /**
     * @param {any} channelData 
     * @returns {ServerLogChannelItem[]}
     */
    validateChannelData: function(channelData) {
        function assertType(value, name, expected, cb = null) {
            let typeEq = typeof value === expected;
            if (expected === 'array') {
                typeEq = Array.isArray(value) && typeof value === 'object';
            }
            if (!typeEq) {
                const msg = `${name}: invalid type (got: ${typeof value}, expected: ${expected})`;
                if (cb && typeof cb === 'function') {
                    cb(msg);
                }
                return msg;
            }
            return null;
        }

        if (!channelData || typeof channelData !== 'object')
            throw new Error(`channelData: invalid type (got: ${typeof channelData}, expected: object)`);
        if (!Array.isArray(channelData))
            throw new Error(`channelData: object is not an array`);
        
        const errors = [];
        function assertTypeR(value, name, prop, expected) {
            if (!value) {
                const msg = `${name}: missing property "${prop}"`;
                errors.push(msg);
                return msg;
            }
            return assertType(value, name + '.' + prop, expected, msg => errors.push(msg));
        }
        function validateChannel(index, channel, name) {
            if (!channel || typeof channel !== 'object')  {
                errors.push(`${name}: invalid type (got: ${typeof channel}, expected: object)`);
                return;
            }

            assertTypeR(channel.id, name, 'id', 'string');
            assertTypeR(channel.name, name, 'name', 'string');
            if (channel.category === true) {
                const childFail = assertTypeR(channel.children, name, 'children', 'array');
                if (childFail == null) {
                    for (const i in channel.children) {
                        validateChannel(i, channel.children[i], `${name}.children[${i}]`);
                    }
                }
            }
        }

        for (const i in channelData) {
            validateChannel(i, channelData[i], `channelData[${i}]`);
        }
        if (errors.length > 0) {
            throw new Error('-- Validation Failed --\n' + errors.join('\n'));
        }
        return channelData;
    },
    /**
     * @param {any} config 
     * @returns {ServerLogChannelItem[]}
     */
    validateConfig: function(config) {
        function assertType(value, name, expected, cb = null) {
            if (typeof value !== expected) {
                const msg = `${name}: invalid type (got: ${typeof value}, expected: ${expected})`;
                if (cb && typeof cb === 'function') {
                    cb(msg);
                }
                return msg;
            }
            return null;
        }

        if (!config || typeof config !== 'object')
            throw new Error(`config: invalid type (got: ${typeof config}, expected: object)`);
        if (!Array.isArray(config))
            throw new Error(`config: object is not an array`);
        
        const errors = [];
        function assertTypeR(value, name, prop, expected) {
            if (!value) {
                const msg = `${name}: missing property "${prop}"`;
                errors.push(msg);
                return msg;
            }
            return assertType(value, name + '.' + prop, expected, msg => errors.push(msg));
        }
        const visitedKeys = [];
        const removeIndexes = [];
        for (const i in config) {
            const item = config[i];
            if (!item || typeof item !== 'object')  {
                errors.push(`config[${i}]: invalid type (got: ${typeof config[i]}, expected: object)`);
                continue;
            }
            const name = `config[${i}]`;

            assertTypeR(item.id, name, 'id', 'string');
            if (assertTypeR(item.channelId, name, 'channelId', 'string') != null) {
                if (item.channelId.length < 1) {
                    errors.push(`config[${i}].channelId: value cannot be empty`);
                }
            }
            if (assertTypeR(item.event, name, 'event', 'string') == null) {
                if (item.event.length < 1) {
                    errors.push(`config[${i}].event: value cannot be empty`);
                }
                if (serverLogEvents.filter(e => e == item.event).length < 1) {
                    errors.push(`config[${i}].event: value "${item.event}" is not a valid enum value`);
                }
            }

            if (item.channelId && item.event && typeof item.channelId == 'string' && typeof item.event === 'string') {
                const key = `${item.channelId}|${item.event}`;
                if (visitedKeys.filter(e => e == key).length > 0) {
                    removeIndexes.push(i);
                } else {
                    visitedKeys.push(key);
                }
            }
        }
        if (errors.length > 0) {
            throw new Error('-- Validation Failed --\n' + errors.join('\n'));
        }
        // deduplicate, and sort (desc)
        for (const i of [...new Set(removeIndexes)].sort().reverse()) {
            config.splice(i, 1);
        }
        return config;
    },
    /**
     * @description
     * Read config from the value in the hidden input with the name `jsonData` in an element matching `#server-log-channel-config`
     * @returns {ServerLogConfigItem[]|null}
     */
    readConfigFromInput: function() {
        const elem = document.querySelector(this._qs_jsonDataElement);
        if (!elem) {
            throw new Error('dafuq?? could not find element: ' + this._qs_jsonDataElement);
        }
        const value = elem.value;
        let contentRaw = '[]';
        if (value && typeof value === 'string' && value.trim().length > 0) {
            try {
                contentRaw = atob(value);
            } catch (err) {
                throw new Error(`Failed to parse value as base64: ${value}\nfrom element: ${this._qs_jsonDataElement}\nsource err: ${err}`);
            }
        } else {
            console.warn(`[serverLog.readConfigFromInput] no valid value from element "${this._qs_jsonDataElement}" (value type: ${typeof value})`, {
                value,
                element: elem
            });
        }
        console.debug('[serverLog.readConfigFromInput] reading data', contentRaw);
        let data = [];
        try {
            data = JSON.parse(contentRaw);
        } catch (err) {
            throw new Error('failed to parse content:\n' + err.toString() + '\n' + contentRaw);
        }
        try {
            data = this.validateConfig(data)
        } catch (err) {
            throw new Error(`failed to validate config\n${err}`);
        }
        this.writeChannelElements(data);
    },

    _an_item_id: 'xenia-serverLog-id',
    _an_item_channelId: 'xenia-serverLog-channelId',
    _an_item_event: 'xenia-serverLog-event',
    /**
     * @description
     * Generates config based off the current elements in query `tbody#server-log-channel-list tr`
     * @returns {ServerLogConfigItem[]|null}
     */
    readConfigFromElements: function() {
        const result = [];
        for (const childElement of document.querySelectorAll('tbody#server-log-channel-list tr')) {
            result.push({
                id: childElement.getAttribute(this._an_item_id),
                channelId: childElement.getAttribute(this._an_item_channelId),
                event: childElement.getAttribute(this._an_item_event),
            });
        }
        return this.validateConfig(result);
    },
    /**
     * @param {string} id 
     * @returns {ServerLogChannelItem|null}
     */
    channelFromId: function(id) {
        for (const channel of this.getChannelData()) {
            const x = this.channelFromId_inner(channel, id);
            if (x) return x;
        }
        return null;
    },
    /**
     * @param {ServerLogChannelItem} channel 
     * @param {string} id 
     * @returns {ServerLogChannelItem|null}
     */
    channelFromId_inner: function(channel, id) {
        if (channel.id == id) return channel;
        if (Array.isArray(channel.children)) {
            for (const inner of channel.children) {
                const x = this.channelFromId_inner(inner, id);
                if (x) return x;
            }
        }
        return null;
    },
    /**
     * @param {ServerLogConfigItem[]} config 
     */
    writeChannelElements: function(config) {
        const elements = [];
        let currentChannelId = null;
        const sortedConfig = config.sort((a, b) => {
            const nameA = a.channelId.toUpperCase(); // ignore upper and lowercase
            const nameB = b.channelId.toUpperCase(); // ignore upper and lowercase
            if (nameA < nameB) {
                return -1;
            }
            if (nameA > nameB) {
                return 1;
            }

            // names must be equal
            return 0;
        });
        for (const item of sortedConfig) {
            const channel = this.channelFromId(item.channelId);

            const row = document.createElement('tr');
            row.setAttribute('xenia-serverLog-id', item.id);
            row.setAttribute('xenia-serverLog-channelId', item.channelId);
            row.setAttribute('xenia-serverLog-event', item.event);
            
            // -- col: channel
            const channelElem = document.createElement('td');
            if (currentChannelId != item.channelId) {
                channelElem.innerText = channel ? channel.name : item.channelId;
                currentChannelId = item.channelId;
            }
            
            // -- col: event
            const eventElem = document.createElement('td');
            eventElem.innerText = item.event;

            // -- col: buttons
            const buttonsElem = document.createElement('td');
            const deleteElem = document.createElement('button');
            deleteElem.setAttribute('class', 'btn btn-danger btn-sm');
            deleteElem.innerHTML = '<i class="bi bi-trash3-fill"></i>';
            deleteElem.title = 'Remove';
            const self = this;
            $(deleteElem).click(() => {
                row.remove();
                self.writeChannelElements(self.readConfigFromElements());
            });
            buttonsElem.appendChild(deleteElem);

            row.appendChild(channelElem);
            row.appendChild(eventElem);
            row.appendChild(buttonsElem);
            elements.push(row);
        }
        const target = document.getElementById('server-log-channel-list');
        target.innerHTML = '';

        for (const child of elements) {
            target.appendChild(child);
        }
        document.querySelector(this._qs_jsonDataElement).value = btoa(JSON.stringify(sortedConfig));
    },
    _addChannel: function(data) {
        if (!data.channelId || typeof data.channelId !== 'string') {
            throw new Error('invalid property "channelId"');
        }
        if (!data.event || typeof data.event !== 'string') {
            throw new Error('invalid property "event"');
        }
        const config = this.readConfigFromElements();
        if (config.filter(e => e.channelId == data.channelId && e.event == data.event).length > 0) {
            return {
                error: `An item with the provided Channel Id and Event already exists!\nChannelId: ${data.channelId}\nEvent: ${data.event}`
            };
        }
        const item = {
            id: xeniaUtil.uuidv4(),
            channelId: data.channelId,
            event: data.event
        };
        console.debug(`[serverLog._addChannel] channelId: ${item.channelId}, event: ${item.event}`);
        config.push(item);
        this.writeChannelElements(config);

        return {
            success: true
        };
    },
    serverLogAddChannel: function() {
        const selectedEvent = document.querySelector('select#server-log-select-event').value;
        const selectedChannelId = document.querySelector('select#server-log-select-channel').value;
        const validationErrors = [];
        if (!selectedEvent || (typeof selectedEvent === 'string' && selectedEvent.trim().length < 1)) {
            validationErrors.push('Event is required!');
        }
        if (!selectedChannelId || (typeof selectedChannelId === 'string' && selectedChannelId.trim().length < 1)) {
            validationErrors.push('Channel is required!');
        }
        if (validationErrors.length > 0) {
            alert('-- Failed to validate user input! --\n' + validationErrors.join('\n'));
            return;
        }
        const result = this._addChannel({
            channelId: selectedChannelId,
            event: selectedEvent
        });
        if (result.error) {
            console.error(`[serverLog.serverLogAddChannel] failed to add channel (channelId=${selectedChannelId}, event=${selectedEvent})\n${result.error}`, result);
            let content = '<code><pre>' + JSON.stringify(result, null, '  ').replaceAll('<', '&lt;').replaceAll('>', '&gt;') + "</pre></code>";
            if (typeof result.error === 'string') {
                content = result.format && typeof result.format === 'function' ? result.format() : result.error;   
                content = content.replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('\n', '<br>');
            }
            xeniaDiscord.createBootstrapToast({
                title: 'Server Log - Failed to create channel',
                innerHTML: content,
                autohide: false
            });
        }
    },
    initEventSelect: function () {
        const eventSelect = document.getElementById('server-log-select-event');
        eventSelect.innerHTML = '';

        if (eventSelect.children.length < 1) {
            const elem = document.createElement('option');
            elem.value = '';
            elem.innerText = '';
            elem.disabled = true;
            elem.selected = true;
            eventSelect.appendChild(elem);
        }
        for (const value of serverLogEvents) {
            const elem = document.createElement('option');
            elem.value = value;
            elem.innerText = value;
            eventSelect.appendChild(elem);
        }
    },
    initChannelSelect: function () {
        const channelSelect = document.getElementById('server-log-select-channel');
        channelSelect.innerHTML = '';

        if (channelSelect.children.length < 1) {
            const elem = document.createElement('option');
            elem.value = '';
            elem.innerText = '';
            elem.disabled = true;
            elem.selected = true;
            channelSelect.appendChild(elem);
        }

        for (const channel of this.getChannelData()) {
            const elem = this.generateChannelElement(channel);
            if (!elem) continue;
            channelSelect.appendChild(elem);
        }
    },
    /**
     * @param {ServerLogChannelItem} channel 
     * @returns {HTMLOptionElement|HTMLOptGroupElement}
     */
    generateChannelElement: function (channel) {
        if (channel.category === true) {
            const categoryElem = document.createElement('optgroup');
            categoryElem.label = channel.name;
            categoryElem.setAttribute('discord-channel-id', channel.id);
            for (const childChannel of channel.children) {
                const childElem = this.generateChannelElement(childChannel);
                if (!childElem) continue;
                // console.log(childChannel, childElem);
                categoryElem.appendChild(childElem);
            }
            return categoryElem;
        }

        if (channel.voice === true) return null;
        // console.log(channel);
        const elem = document.createElement('option');
        elem.value = channel.id;
        elem.disabled = channel.text !== true;
        elem.innerText = channel.name;
        elem.innerHTML = `<span style="padding-left: 1rem;"><i class="bi bi-hash"></i>${elem.innerHTML}</span>`;
        elem.setAttribute('discord-channel-id', channel.id);
        return elem;
    },

    onReady: function() {
        this.getChannelData();
        this.initEventSelect();
        this.initChannelSelect();

        try {
            this.readConfigFromInput();
        } catch (err) {
            console.error(err);
            xeniaDiscord.createBootstrapToast({
                title: 'Server Log - Failed to load data',
                innerHTML: err.toString().replace('<', '&lt;').replaceAll('>', '&gt;').replace('\n', '<br>'),
                autohide: false
            });
        }
    }
}

$('#server-log-channel-add button[xenia-action=add]').click(() => {
    serverLogLogic.serverLogAddChannel();
});

$(document).ready(() => {
    serverLogLogic.onReady();
});