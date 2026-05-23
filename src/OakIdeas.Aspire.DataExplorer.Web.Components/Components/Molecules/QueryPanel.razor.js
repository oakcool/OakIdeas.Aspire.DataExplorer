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

    const state = { hasSuggestion: false, selectedText: '' };

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

    // Cache the selection whenever it changes so that clicking the Execute
    // button (which moves focus away from the textarea) does not lose it.
    const onSelectionChange = () => {
        const { selectionStart, selectionEnd } = textarea;
        state.selectedText = selectionStart === selectionEnd
            ? ''
            : textarea.value.substring(selectionStart, selectionEnd);
    };

    textarea.addEventListener('scroll', onScroll);
    textarea.addEventListener('keydown', onKeyDown);
    textarea.addEventListener('select', onSelectionChange);
    textarea.addEventListener('mouseup', onSelectionChange);
    textarea.addEventListener('keyup', onSelectionChange);

    _instances.set(id, { textarea, onScroll, onKeyDown, onSelectionChange, state });
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
    inst.textarea.removeEventListener('select', inst.onSelectionChange);
    inst.textarea.removeEventListener('mouseup', inst.onSelectionChange);
    inst.textarea.removeEventListener('keyup', inst.onSelectionChange);
    _instances.delete(id);
}

/**
 * Returns the last text selection captured in the editor textarea, or an
 * empty string when nothing was selected. The selection is cached on each
 * 'select', 'mouseup', and 'keyup' event so that a button click (which moves
 * focus away from the textarea) cannot clear it before this is called.
 */
export function getSelectedText(id) {
    const inst = _instances.get(id);
    return inst ? inst.state.selectedText : '';
}
