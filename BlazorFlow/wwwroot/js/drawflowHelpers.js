/**
 * NodeParamBinding (auto version) — Drawflow-friendly
 * - Auto-detects editor instance (default: window._df_editor)
 * - Initializes controls from node.data
 * - Writes changes back via deep path-set (preserves siblings)
 * - Supports dotted / bracket paths in data-param, e.g. "config.apiKey", "items[0].value"
 * - Robust node id detection: data-id, data-node-id, or id="node-123"
 * - Global delegated listeners; no Blazor interop calls required
 * - NEW: All data from [data-param] writes under node.data.params (with backward-compatible reads)
 *
 * Optional config (no code calls):
 *   <body data-editor-global="_df_editor">
 * Optional alternative signal (if you don't want a global):
 *   window.dispatchEvent(new CustomEvent('editor:ready', { detail: { editor } }));
 */
(function () {
    let editor = null;
    let ready = false;

    // ---- Editor detection (no Blazor calls) ----
    const globalName = document.body?.dataset?.editorGlobal || '_df_editor';

    function tryGrabEditor() {
        const ed = (globalName && window[globalName]) || null;
        if (!ed) return null;
        // Shim common method names if needed
        if (typeof ed.updateNodeDataFromId !== 'function' && typeof ed.updateNodeData === 'function') {
            ed.updateNodeDataFromId = (nid, patch) => ed.updateNodeData(nid, patch);
        }
        if (typeof ed.getNodeFromId !== 'function' && typeof ed.getNode === 'function') {
            ed.getNodeFromId = (nid) => ed.getNode(nid);
        }
        return (typeof ed.updateNodeDataFromId === 'function') ? ed : null;
    }

    function waitForEditor() {
        if (ready) return;
        const ed = tryGrabEditor();
        if (ed) { init(ed); return; }

        // Also listen for a manual custom event if your editor isn't global
        window.addEventListener('editor:ready', (e) => {
            if (ready) return;
            const ed2 = e?.detail?.editor;
            if (!ed2) return;
            // apply shims if needed
            if (typeof ed2.updateNodeDataFromId !== 'function' && typeof ed2.updateNodeData === 'function') {
                ed2.updateNodeDataFromId = (nid, patch) => ed2.updateNodeData(nid, patch);
            }
            if (typeof ed2.getNodeFromId !== 'function' && typeof ed2.getNode === 'function') {
                ed2.getNodeFromId = (nid) => ed2.getNode(nid);
            }
            if (typeof ed2.updateNodeDataFromId === 'function') init(ed2);
        }, { once: true });

        // Poll a bit for globals (20s max)
        let tries = 0;
        const h = setInterval(() => {
            const ed3 = tryGrabEditor();
            if (ed3) {
                clearInterval(h);
                init(ed3);
            } else if (++tries > 200) {
                clearInterval(h);
                console.warn('NodeParamBinding: editor not found. Set body[data-editor-global] or dispatch window "editor:ready".');
            }
        }, 100);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', waitForEditor, { once: true });
    } else {
        waitForEditor();
    }

    // ---- Small utils for deep path read/write ----
    const isObj = (v) => v !== null && typeof v === 'object';
    const isPlain = (v) => Object.prototype.toString.call(v) === '[object Object]';

    // Parse "a.b[0].c" -> ["a","b","0","c"]
    function parsePath(path) {
        if (Array.isArray(path)) return path;
        const s = String(path)
            .replace(/\[(\w+)\]/g, '.$1') // [0] or [foo] -> .0 / .foo
            .replace(/^\./, '');          // leading dot
        return s.split('.').filter(Boolean);
    }

    function cloneData(v) {
        if (typeof structuredClone === 'function') return structuredClone(v);
        try { return JSON.parse(JSON.stringify(v)); } catch { return isPlain(v) ? { ...v } : v; }
    }

    function getByPath(root, path) {
        const segs = parsePath(path);
        let cur = root;
        for (const k of segs) {
            if (!isObj(cur)) return undefined;
            cur = cur[k];
        }
        return cur;
    }

    function setByPath(root, path, value) {
        const segs = parsePath(path);
        if (!segs.length) return root;

        let cur = root;
        for (let i = 0; i < segs.length; i++) {
            const k = segs[i];
            const isLast = i === segs.length - 1;
            if (isLast) {
                cur[k] = value;
            } else {
                const nextK = segs[i + 1];
                const shouldBeArray = /^\d+$/.test(nextK);
                if (!isObj(cur[k])) {
                    cur[k] = shouldBeArray ? [] : {};
                } else if (Array.isArray(cur[k]) && !shouldBeArray) {
                    // convert array to object if path demands
                    cur[k] = Object.assign({}, cur[k]);
                }
                cur = cur[k];
            }
        }
        return root;
    }

    // ---- NEW: normalize data-param -> params.* path ----
    function normalizeParamPath(p) {
        const s = String(p || '').trim();
        if (!s) return 'params';
        if (/^params(\.|\[)/.test(s)) return s;
        return s[0] === '[' ? `params${s}` : `params.${s}`;
    }

    // ---- Safe deep setter (prevents overwriting siblings) ----
    function safeSetNodeDataValue(id, path, value) {
        try {
            const node = editor.getNodeFromId?.(id);
            const current = (node && isPlain(node.data)) ? cloneData(node.data) : {};
            setByPath(current, path, value);
            // Important: send the full merged object so editors that "replace" data
            // still keep siblings we preserved above.
            editor.updateNodeDataFromId(id, current);
        } catch (err) {
            console.warn('NodeParamBinding: safeSetNodeDataValue failed', err);
        }
    }

    // ---- Core binding logic ----
    function init(ed) {
        editor = ed;
        ready = true;

        // Initial sweep to sync existing nodes' inputs from node.data
        bindAllExistingNodes();

        // Observe DOM for nodes added later and sync their inputs
        const mo = new MutationObserver(muts => {
            for (const m of muts) {
                for (const el of m.addedNodes) {
                    if (!(el instanceof HTMLElement)) continue;
                    if (el.matches?.('.drawflow-node')) bindNode(el);
                    el.querySelectorAll?.('.drawflow-node').forEach(bindNode);
                }
            }
        });
        mo.observe(document.body, { childList: true, subtree: true });

        // If the editor provides events, optionally resync when nodes change
        try {
            editor.on?.('nodeCreated', id => syncNodeById(id));
            editor.on?.('nodeDataChanged', id => syncNodeById(id));
        } catch { /* non-fatal */ }

        // Global delegated listeners (capture) so dynamic nodes always work
        document.addEventListener('change', onParamEvent, true);
        document.addEventListener('input', onParamEvent, true);
        // Some engines don’t fire 'input' for checkbox/radio; click fallback helps.
        document.addEventListener('click', (e) => {
            const t = e.target;
            if (t instanceof HTMLInputElement && /^(checkbox|radio)$/i.test(t.type)) {
                onParamChanged(t);
            }
        }, true);

        console.info('NodeParamBinding: initialized.');
    }

    function bindAllExistingNodes() {
        document.querySelectorAll('.drawflow-node').forEach(bindNode);
    }

    function bindNode(nodeEl) {
        const id = getNodeId(nodeEl);
        if (!id) return;
        // Initialize UI from node.data
        syncInputsFromData(nodeEl, id);
    }

    // Delegated handlers -> normalize target and send to updater
    function onParamEvent(e) {
        const t = e.target;
        if (!(t instanceof HTMLElement)) return;
        if (!t.matches('[data-param]')) return;
        onParamChanged(t);
    }

    function onParamChanged(targetEl) {
        if (!(targetEl instanceof HTMLElement)) return;

        const rawPath = targetEl.getAttribute('data-param'); // supports dot/bracket paths
        if (!rawPath) return;

        const path = normalizeParamPath(rawPath);

        const nodeEl = targetEl.closest('.drawflow-node, [data-node-id], [id^="node-"]');
        const id = getNodeId(nodeEl);
        if (!id) return;

        const value = readControlValue(targetEl);
        safeSetNodeDataValue(id, path, value);
    }

    function readControlValue(el) {
        const tag = el.tagName;
        if (tag === 'INPUT') {
            const type = (el.getAttribute('type') || '').toLowerCase();
            if (type === 'checkbox') return el.checked;
            if (type === 'number' || type === 'range') {
                const n = Number(el.value);
                return Number.isFinite(n) ? n : el.value;
            }
            return el.value;
        }
        if (tag === 'SELECT' || tag === 'TEXTAREA') return el.value;
        if (el.isContentEditable) return el.textContent;
        return el.getAttribute('data-value') ?? el.textContent ?? '';
    }

    function setControlValue(el, value) {
        const tag = el.tagName;
        if (tag === 'INPUT') {
            const type = (el.getAttribute('type') || '').toLowerCase();
            if (type === 'checkbox') { el.checked = !!value; return; }
            el.value = value ?? '';
            return;
        }
        if (tag === 'SELECT' || tag === 'TEXTAREA') { el.value = value ?? ''; return; }
        if (el.isContentEditable) { el.textContent = value ?? ''; return; }
        el.setAttribute('data-value', value ?? '');
    }

    function syncInputsFromData(nodeEl, id) {
        let node = null, data = {};
        try {
            node = editor.getNodeFromId?.(id) ?? null;
            data = node?.data ?? {};
        } catch { /* non-fatal */ }

        nodeEl.querySelectorAll('[data-param]').forEach(ctrl => {
            const raw = ctrl.getAttribute('data-param');
            if (!raw) return;

            const norm = normalizeParamPath(raw);

            // Prefer new schema under params.*, but fall back to legacy root
            let existing = getByPath(data, norm);
            if (typeof existing === 'undefined') existing = getByPath(data, raw);

            if (typeof existing !== 'undefined') {
                setControlValue(ctrl, existing);
            } else {
                // Seed node.data under params.*
                const currentVal = readControlValue(ctrl);
                safeSetNodeDataValue(id, norm, currentVal);
            }
        });
    }

    function getNodeId(nodeEl) {
        if (!nodeEl) return null;

        // Preferred: data-id / data-node-id
        let val = nodeEl.getAttribute?.('data-id') || nodeEl.getAttribute?.('data-node-id');
        if (val) {
            const n = parseInt(val, 10);
            if (Number.isFinite(n)) return n;
        }

        // Drawflow default: id="node-123"
        const rawId = nodeEl.getAttribute?.('id');
        if (rawId) {
            const m = /^node-(\d+)$/.exec(rawId);
            if (m) {
                const n = parseInt(m[1], 10);
                if (Number.isFinite(n)) return n;
            }
        }

        return null;
    }

    function syncNodeById(id) {
        const nodeEl =
            document.querySelector(`.drawflow-node[data-id='${id}']`) ||
            document.querySelector(`[data-node-id='${id}']`) ||
            document.getElementById(`node-${id}`);
        if (nodeEl) syncInputsFromData(nodeEl, id);
    }
})();
