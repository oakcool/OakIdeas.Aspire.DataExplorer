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
      theme: "dark"
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
