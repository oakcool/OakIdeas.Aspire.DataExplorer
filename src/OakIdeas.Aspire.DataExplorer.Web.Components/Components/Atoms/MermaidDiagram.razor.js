let initialized = false;

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
        primaryTextColor: "#e2e8f0",
        primaryBorderColor: "#60a5fa",
        lineColor: "#64748b",
        fontFamily: "Inter, Segoe UI, sans-serif"
      }
    });
    initialized = true;
  }

  try {
    await mermaid.parse(source);
    const id = `de-mermaid-${Math.random().toString(36).slice(2, 10)}`;
    const result = await mermaid.render(id, source);
    container.innerHTML = result.svg;
    return null;
  } catch (error) {
    container.innerHTML = "";
    return error?.message ? `invalid: ${error.message}` : "invalid";
  }
}
