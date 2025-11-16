// wwwroot/js/drawflowInterop.js
window.DrawflowBlazor = (function () {
    const instances = new Map();

    function ensureInstance(id) {
        if (!instances.has(id)) {
            throw new Error("Drawflow instance not found for id: " + id);
        }
        return instances.get(id);
    }

    function create(id, dotNetRef, options) {
        const el = document.getElementById(id);
        if (!el) throw new Error("Element not found: " + id);
        if (instances.has(id)) {
            try { instances.get(id).editor?.destroy(); } catch { }
            instances.delete(id);
        }

        const opts = Object.assign({}, options || {});
        if (typeof Drawflow === "undefined") {
            throw new Error("Drawflow global not found. Include the library first.");
        }

        const editor = new Drawflow(el, opts);
        window._df_editor = editor;                      // expose globally
        window.dispatchEvent(new CustomEvent('editor:ready', { detail: { editor } })); // optional but nice
        if (typeof opts.reroute !== "undefined") editor.reroute = opts.reroute;
        editor.start();

        const state = { id, editor, dotNetRef, eventHandlers: {} };

        const knownEvents = [
            "nodeCreated", "nodeRemoved", "nodeSelected", "nodeUnselected",
            "nodeDataChanged", "nodeMoved", "connectionCreated", "connectionRemoved",
            "connectionSelected", "connectionUnselected", "moduleCreated",
            "moduleChanged", "moduleRemoved", "import", "zoom", "translate",
            "addReroute", "removeReroute"
        ];

        knownEvents.forEach(evt => {
            const handler = (...args) => {
                try {
                    const payload = JSON.stringify(args, (_k, v) => (v instanceof HTMLElement ? undefined : v));
                    state.dotNetRef.invokeMethodAsync("OnDrawflowEvent", evt, payload);
                } catch (e) {
                    console.warn("Failed to forward Drawflow event", evt, e);
                }
            };
            state.eventHandlers[evt] = handler;
            try { editor.on(evt, handler); } catch { }
        });

        instances.set(id, state);
        return true;
    }

    function destroy(id) {
        const s = instances.get(id);
        if (!s) return false;
        try {
            Object.entries(s.eventHandlers || {}).forEach(([evt, h]) => {
                try { s.editor.off?.(evt, h); } catch { }
            });
            s.editor?.destroy?.();
        } finally {
            instances.delete(id);
        }
        return true;
    }

    function on(id, eventName) {
        const s = ensureInstance(id);
        if (!s.eventHandlers[eventName]) {
            const handler = (...args) => {
                try {
                    const payload = JSON.stringify(args, (_k, v) => (v instanceof HTMLElement ? undefined : v));
                    s.dotNetRef.invokeMethodAsync("OnDrawflowEvent", eventName, payload);
                } catch (e) {
                    console.warn("Failed to forward Drawflow event", eventName, e);
                }
            };
            s.eventHandlers[eventName] = handler;
            s.editor.on(eventName, handler);
        }
        return true;
    }

    function off(id, eventName) {
        const s = ensureInstance(id);
        const h = s.eventHandlers[eventName];
        if (h) {
            try { s.editor.off?.(eventName, h); } catch { }
            delete s.eventHandlers[eventName];
        }
        return true;
    }

    async function call(id, methodName, args) {
        const s = ensureInstance(id);
        const target = s.editor;
        if (!target) throw new Error("Editor missing for id: " + id);
        const fn = target[methodName];
        if (typeof fn !== "function") {
            throw new Error("Method not found on Drawflow: " + methodName);
        }
        const resolvedArgs = (args || []).map(a => {
            if (typeof a === "string") {
                try { return JSON.parse(a); } catch { return a; }
            }
            return a;
        });
        const result = fn.apply(target, resolvedArgs);
        if (result && typeof result.then === "function") {
            return await result;
        }
        return result ?? null;
    }

    function get(id, propName) {
        const s = ensureInstance(id);
        const v = s.editor?.[propName];
        return v ?? null;
    }

    function set(id, propName, value) {
        const s = ensureInstance(id);
        if (!s.editor) return false;
        s.editor[propName] = value;
        return true;
    }

    function autoSizePortLabels(elementId, nodeId, inLabels = [], outLabels = [], labelGap = 8)
    {
        const host = document.getElementById(elementId);
        if (!host) return;

        const nodeEl =
            host.querySelector(`.drawflow-node[data-id="\${nodeId}"]`) ||
            host.querySelector(`.drawflow-node#node-${nodeId}`);
        if (!nodeEl) return;

        let left = inLabels.length ? Math.max(...inLabels.map(l => Math.max(_measureTextPx(l[0]), _measureTextPx(l[1])) + 15)) : 0;
        let right = outLabels.length ? Math.max(...outLabels.map(l => Math.max(_measureTextPx(l[0]), _measureTextPx(l[1])) + 15)) : 0;

        // ✅ Set CSS vars on this node only
        nodeEl.style.setProperty('--df-left-label-width', left + 'px');
        nodeEl.style.setProperty('--df-right-label-width', right + 'px');
        nodeEl.style.setProperty('--df-label-gap', labelGap + 'px');
    };

    function setNodeWidthFromTitle(elementId, nodeId)
    {
        const host = document.getElementById(elementId);
        if (!host) return;

        const nodeEl =
            host.querySelector(`.drawflow-node[data-id="${nodeId}"]`) ||
            host.querySelector(`.drawflow-node#node-${nodeId}`);
        if (!nodeEl) return;

        const titleEl = nodeEl.querySelector(".title");
        if (!titleEl) return;

        const title = titleEl.innerText;
        let titleWidth = _measureTextPx(title, "1rem 'Helvetica Neue', Helvetica, Arial, sans-serif");
        // Add comfortable margins: 30px left (icon space) + 24px right = 54px total + 16px extra breathing room
        titleWidth += 70;

        nodeEl.style.setProperty('--df-title-width', titleWidth + 'px');
    }

    function autoSizeNode(elementId, nodeId, inLabels = [], outLabels = [], labelGap = 8)
    {
        autoSizePortLabels(elementId, nodeId, inLabels, outLabels, labelGap);
        setNodeWidthFromTitle(elementId, nodeId);
    }

    function labelPorts(elementId, nodeId, inLabels = [], outLabels = [])
    {
        const host = document.getElementById(elementId);
        if (!host) return;

        const nodeEl =
            host.querySelector(`.drawflow-node[data-id="${nodeId}"]`) ||
            host.querySelector(`.drawflow-node#node-${nodeId}`);
        if (!nodeEl) return;

        const styleId = "df-port-label-style";
        if (!document.getElementById(styleId)) {
            const s = document.createElement("style");
            s.id = styleId;

            s.textContent = `
              /* Label pseudo-elements */
                .drawflow-node::before {
                    height: 100%;
                    width: calc(var(--df-left-label-width));
                    position: absolute;
                    left: 0;
                    content: "";
                }

                .drawflow-node::after {
                      height: 100%;
                      width: calc(var(--df-right-label-width));
                      position: absolute;
                      right: 0;
                      content: "";
                  }
              /* Reserve gutter space inside node box so content never overlaps */
              .drawflow .drawflow-node .drawflow_content_node {
                  padding-left: calc(var(--df-left-label-width, 0px) + var(--df-label-gap, 8px));
                  padding-right: calc(var(--df-right-label-width, 0px) + var(--df-label-gap, 8px));
              }
            `;
            document.head.appendChild(s);
        }

        const inputs = nodeEl.querySelectorAll(".inputs .input");
        const outputs = nodeEl.querySelectorAll(".outputs .output");

        for (let i = 0; i < inputs.length; i++) {
            if (inLabels.length <= i)
            {
                continue;
            }
            const text = inLabels[i] ?? `In ${i + 1}`;
            const typeText = inLabels[i][0];
            const valueText = inLabels[i][1];
            inputs[i].setAttribute("data-label", text);
            inputs[i].setAttribute("title", text);
            const newDiv = document.createElement('div');
            const typeLabel = document.createElement('p');
            const inputLabel = document.createElement('p');

            newDiv.style.margin = '-2px 0 0 20px';
            newDiv.style.pointerEvents = 'none';

            typeLabel.style.margin = '0';
            typeLabel.style.color = 'rgb(138, 180, 233)';
            typeLabel.style.fontSize = '10px';
            typeLabel.innerHTML = typeText;

            inputLabel.style.margin = '0';
            inputLabel.style.padding = '0';
            inputLabel.style.fontSize = '12px';

            inputLabel.style.margin = '-7px 0px 0px 0px';
            inputLabel.innerHTML = valueText;

            newDiv.appendChild(typeLabel);
            newDiv.appendChild(inputLabel);
            inputs[i].appendChild(newDiv);
        }
        for (let i = 0; i < outputs.length; i++) {
            if (outLabels.length <= i) {
                continue;
            }
            const text = outLabels[i] ?? `Out ${i + 1}`;
            const typeText = outLabels[i][0];
            const valueText = outLabels[i][1];
            outputs[i].setAttribute("data-label", text);
            outputs[i].setAttribute("title", text);
            const newDiv = document.createElement('div');
            const typeLabel = document.createElement('p');
            const outputLabel = document.createElement('p');

            newDiv.style.pointerEvents = 'none';
            newDiv.style.width = 'fit-content';
            newDiv.style.textAlign = 'right';

            typeLabel.style.margin = '0';
            typeLabel.style.color = 'rgb(138, 180, 233)';
            typeLabel.style.fontSize = '10px';
            typeLabel.innerHTML = typeText;
            typeLabel.classList.add('port_type');

            outputLabel.style.margin = '0';
            outputLabel.style.padding = '0';
            outputLabel.style.fontSize = '12px';
            outputLabel.style.margin = '-7px 0px 0px 0px';
            outputLabel.innerHTML = valueText;
            outputLabel.classList.add('port_label');

            newDiv.appendChild(typeLabel);
            newDiv.appendChild(outputLabel);
            outputs[i].appendChild(newDiv);

            newDiv.style.margin = '-2px 0 0 calc(-5px - ' + outputLabel.clientWidth + 'px)';
        }

        autoSizeNode(elementId, nodeId, inLabels, outLabels, 8)
    }

    function setNodeStatus(elementId, nodeId, status)
    {
        const host = document.getElementById(elementId);
        if (!host) return;

        const nodeEl =
            host.querySelector(`.drawflow-node[data-id="${nodeId}"]`) ||
            host.querySelector(`.drawflow-node#node-${nodeId}`);

        if (!nodeEl) return;

        if (status.isRunning != null) {
            if (status.isRunning) {
                nodeEl.classList.add("processing-bar");
            }
            else if (nodeEl.classList.contains("processing-bar")) {
                nodeEl.classList.remove("processing-bar")
            }
        }

        // Handle error state
        if (status.hasError != null) {
            if (status.hasError) {
                nodeEl.classList.add("node-error");
                nodeEl.setAttribute("title", status.errorMessage || "Error occurred");
            } else {
                nodeEl.classList.remove("node-error");
            }
        }

        if (status.outputPortResults != null) {
            const outputs = nodeEl.querySelectorAll(".outputs .output");
            for (let i = 0; i < outputs.length; i++) {
                if (status.outputPortResults[i] != null) {
                    outputs[i].classList.add('computed_node')
                    outputs[i].setAttribute('title', status.outputPortResults[i]);
                }
            }
        }

    }

    function setNodeDoubleClickCallback(elementId, callbackReference) {
        const host = document.getElementById(elementId);
        if (!host) return;

        host.addEventListener('dblclick', function (e) {
            const nodeEl = e.target.closest('.drawflow-node');
            if (!nodeEl) return;

            const idAttr = nodeEl.dataset.id || nodeEl.getAttribute('data-id')
                || (nodeEl.id && nodeEl.id.startsWith('node-')
                    ? nodeEl.id.substring('node-'.length)
                    : nodeEl.id);

            if (!idAttr) return;

            // Call into Blazor
            callbackReference.invokeMethodAsync(
                'OnNodeDoubleClickFromJs',
                idAttr
            );
        });
    }

    return { create, destroy, on, off, call, get, set, labelPorts, setNodeStatus, setNodeDoubleClickCallback, setNodeWidthFromTitle };
})();

window.nextFrame = () => {
    return new Promise(resolve => requestAnimationFrame(() => resolve()));
};

function _measureTextPx(text, font = "12px 'Helvetica Neue', Helvetica, Arial, sans-serif") {
    const span = document.createElement('span');
    span.style.visibility = 'hidden';
    span.style.whiteSpace = 'pre';
    span.style.font = font;
    span.textContent = text;
    document.body.appendChild(span);

    const width = span.getBoundingClientRect().width;
    span.remove();
    return Math.ceil(width);
}
