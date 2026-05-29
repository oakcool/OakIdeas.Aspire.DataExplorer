let initialized = false;

function applyPreferredDirection(source) {
  if (!source) {
    return source;
  }

  const wideLayout = globalThis?.matchMedia?.("(min-width: 1100px)")?.matches ?? true;
  const preferredDirection = wideLayout ? "LR" : "TB";

  return source.replace(/(^\s*flowchart\s+)(TB|TD|BT|RL|LR)\b/i, `$1${preferredDirection}`);
}

export async function renderMermaid(container, source) {
  if (!container || !source) {
    return "Missing container or diagram";
  }

  const mermaid = globalThis?.mermaid;
  if (!mermaid) {
    return "Mermaid runtime unavailable";
  }

  if (!initialized) {
    mermaid.initialize({
      startOnLoad: false,
      securityLevel: "strict",
      theme: "base",
      flowchart: {
        htmlLabels: true,
        nodeSpacing: 35,
        rankSpacing: 55,
        curve: "basis",
        padding: 12
      },
      themeVariables: {
        background: "#0b1220",
        primaryColor: "#0f172a",
        primaryTextColor: "#f1f5f9",
        primaryBorderColor: "#60a5fa",
        lineColor: "#94a3b8",
        fontSize: "11px",
        fontFamily: "JetBrains Mono, Cascadia Mono, ui-monospace, SFMono-Regular, Menlo, Consolas, monospace"
      }
    });
    initialized = true;
  }

  const directionAwareSource = applyPreferredDirection(source);

  try {
    await mermaid.parse(directionAwareSource);
    const id = `de-mermaid-${Math.random().toString(36).slice(2, 10)}`;
    const result = await mermaid.render(id, directionAwareSource);
    container.innerHTML = result.svg;
    return null;
  } catch (error) {
    container.innerHTML = "";
    return error?.message ? `invalid: ${error.message}` : "invalid";
  }
}
