function pxToNumber(value) {
    if (!value) {
        return 0;
    }

    const parsed = Number.parseFloat(value.toString().replace("px", ""));
    return Number.isFinite(parsed) ? parsed : 0;
}

function getCellAt(row, index) {
    if (!row || !row.children || row.children.length <= index) {
        return null;
    }

    return row.children[index];
}

function collectColumnCells(table, index) {
    const rows = table.querySelectorAll("tr");
    const cells = [];

    for (const row of rows) {
        const cell = getCellAt(row, index);
        if (cell) {
            cells.push(cell);
        }
    }

    return cells;
}

function applyWidth(table, index, widthPx) {
    const safeWidth = Math.max(56, Math.round(widthPx));
    const cells = collectColumnCells(table, index);

    for (const cell of cells) {
        cell.style.width = `${safeWidth}px`;
        cell.style.minWidth = `${safeWidth}px`;
        cell.style.maxWidth = `${safeWidth}px`;
    }

    // Keep horizontal scrolling behavior predictable as columns widen.
    table.style.width = "max-content";
    table.style.minWidth = "100%";
}

function measureBestFitWidth(table, index) {
    const cells = collectColumnCells(table, index);
    let widest = 56;

    for (const cell of cells) {
        const computed = window.getComputedStyle(cell);
        const leftPadding = pxToNumber(computed.paddingLeft);
        const rightPadding = pxToNumber(computed.paddingRight);
        const borderLeft = pxToNumber(computed.borderLeftWidth);
        const borderRight = pxToNumber(computed.borderRightWidth);
        const fullWidth = cell.scrollWidth + leftPadding + rightPadding + borderLeft + borderRight + 8;

        if (fullWidth > widest) {
            widest = fullWidth;
        }
    }

    return widest;
}

export function enableColumnResize(table) {
    if (!table) {
        return {
            dispose: () => { }
        };
    }

    const headers = Array.from(table.querySelectorAll("thead th"));
    const cleanups = [];

    headers.forEach((header, index) => {
        if (header.dataset.resizeHandleAttached === "true") {
            return;
        }

        header.dataset.resizeHandleAttached = "true";
        header.style.position = "relative";

        const handle = document.createElement("span");
        handle.setAttribute("role", "separator");
        handle.setAttribute("aria-orientation", "vertical");
        handle.setAttribute("aria-label", `Resize column ${header.textContent?.trim() ?? index + 1}`);
        handle.setAttribute("tabindex", "0");
        handle.style.position = "absolute";
        handle.style.top = "0";
        handle.style.right = "-4px";
        handle.style.width = "8px";
        handle.style.height = "100%";
        handle.style.cursor = "col-resize";
        handle.style.userSelect = "none";
        handle.style.touchAction = "none";
        handle.style.zIndex = "2";
        handle.style.backgroundColor = "transparent";
        handle.style.transition = "background-color 120ms ease";
        handle.title = "Drag to resize, double-click to auto-fit, Arrow keys to resize";

        let isDragging = false;

        const setHoverVisual = () => {
            if (isDragging) {
                return;
            }

            handle.style.backgroundColor = "rgba(88, 166, 255, 0.25)";
        };

        const clearHoverVisual = () => {
            if (isDragging) {
                return;
            }

            handle.style.backgroundColor = "transparent";
        };

        const setDragVisual = () => {
            isDragging = true;
            handle.style.backgroundColor = "rgba(88, 166, 255, 0.55)";
            handle.style.boxShadow = "inset -1px 0 0 rgba(88, 166, 255, 0.95), inset 1px 0 0 rgba(88, 166, 255, 0.95)";
            table.style.cursor = "col-resize";
        };

        const clearDragVisual = () => {
            isDragging = false;
            handle.style.backgroundColor = "transparent";
            handle.style.boxShadow = "";
            table.style.cursor = "";
        };

        const onDoubleClick = (event) => {
            event.preventDefault();
            event.stopPropagation();
            const bestWidth = measureBestFitWidth(table, index);
            applyWidth(table, index, bestWidth);
        };

        const onMouseDown = (event) => {
            event.preventDefault();
            event.stopPropagation();

            const initialWidth = header.getBoundingClientRect().width;
            const startX = event.clientX;

            setDragVisual();
            document.body.style.cursor = "col-resize";
            document.body.style.userSelect = "none";

            const onMouseMove = (moveEvent) => {
                const delta = moveEvent.clientX - startX;
                applyWidth(table, index, initialWidth + delta);
            };

            const onMouseUp = () => {
                document.removeEventListener("mousemove", onMouseMove);
                document.removeEventListener("mouseup", onMouseUp);
                clearDragVisual();
                document.body.style.cursor = "";
                document.body.style.userSelect = "";
            };

            document.addEventListener("mousemove", onMouseMove);
            document.addEventListener("mouseup", onMouseUp);
        };

        const onKeyDown = (event) => {
            const currentWidth = header.getBoundingClientRect().width;
            const baseStep = event.shiftKey ? 40 : 16;

            if (event.key === "ArrowLeft") {
                event.preventDefault();
                applyWidth(table, index, currentWidth - baseStep);
                return;
            }

            if (event.key === "ArrowRight") {
                event.preventDefault();
                applyWidth(table, index, currentWidth + baseStep);
                return;
            }

            if (event.key === "Home") {
                event.preventDefault();
                applyWidth(table, index, 56);
                return;
            }

            if (event.key === "End" || event.key === "Enter") {
                event.preventDefault();
                const bestWidth = measureBestFitWidth(table, index);
                applyWidth(table, index, bestWidth);
            }
        };

        const onMouseEnter = () => {
            setHoverVisual();
        };

        const onMouseLeave = () => {
            clearHoverVisual();
        };

        handle.addEventListener("dblclick", onDoubleClick);
        handle.addEventListener("mousedown", onMouseDown);
        handle.addEventListener("keydown", onKeyDown);
        handle.addEventListener("mouseenter", onMouseEnter);
        handle.addEventListener("mouseleave", onMouseLeave);
        header.appendChild(handle);

        cleanups.push(() => {
            handle.removeEventListener("dblclick", onDoubleClick);
            handle.removeEventListener("mousedown", onMouseDown);
            handle.removeEventListener("keydown", onKeyDown);
            handle.removeEventListener("mouseenter", onMouseEnter);
            handle.removeEventListener("mouseleave", onMouseLeave);
            if (header.contains(handle)) {
                header.removeChild(handle);
            }
            delete header.dataset.resizeHandleAttached;
        });
    });

    return {
        dispose: () => {
            for (const cleanup of cleanups) {
                cleanup();
            }
        }
    };
}