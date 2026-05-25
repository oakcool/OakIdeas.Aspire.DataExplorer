export function getViewportSize() {
    const width = typeof window !== "undefined" && typeof window.innerWidth === "number"
        ? window.innerWidth
        : 1366;
    const height = typeof window !== "undefined" && typeof window.innerHeight === "number"
        ? window.innerHeight
        : 768;

    return { width, height };
}
