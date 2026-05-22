// Query editor JS module — Tab key interception and scroll sync between the
// syntax-highlight layer and the transparent textarea overlay.

const _instances = new Map();

/**
 * Initialise the editor for a QueryPanel instance.
 * @param {string} id          Unique instance identifier (Guid from .NET)
 * @param {Element} editorEl   The root .query-panel element
 * @param {DotNetObjectRef} dotNetRef  Reference to the QueryPanel C# instance
 */
export function initEditor(id, editorEl, dotNetRef) {
    const textarea = editorEl.querySelector('.query-panel__textarea');
    const highlight = editorEl.querySelector('.query-panel__highlight');

    if (!textarea) return;

    const state = { hasSuggestion: false };

    const onScroll = () => {
        if (highlight) {
            highlight.scrollTop = textarea.scrollTop;
            highlight.scrollLeft = textarea.scrollLeft;
        }
    };

    const onKeyDown = (e) => {
        if (e.key === 'Tab' && state.hasSuggestion) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('HandleTab');
        }
    };

    textarea.addEventListener('scroll', onScroll);
    textarea.addEventListener('keydown', onKeyDown);

    _instances.set(id, { textarea, onScroll, onKeyDown, state });
}

/**
 * Notify the JS side whether there is an active suggestion so Tab is
 * only intercepted when a completion is available.
 */
export function updateSuggestionState(id, hasSuggestion) {
    const inst = _instances.get(id);
    if (inst) inst.state.hasSuggestion = hasSuggestion;
}

/** Clean up event listeners for a QueryPanel instance. */
export function dispose(id) {
    const inst = _instances.get(id);
    if (!inst) return;
    inst.textarea.removeEventListener('scroll', inst.onScroll);
    inst.textarea.removeEventListener('keydown', inst.onKeyDown);
    _instances.delete(id);
}
