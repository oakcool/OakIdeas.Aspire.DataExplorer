/**
 * ExecutionPlanDiagram.razor.js
 * Interactive SVG-based execution plan diagram engine.
 * Uses vanilla JS + SVG — no external library dependencies.
 */

const NODE_WIDTH = 230;
const NODE_HEADER_HEIGHT = 42;
const NODE_SUBHEADER_HEIGHT = 18;
const NODE_METRIC_ROW_HEIGHT = 17;
const NODE_SECTION_LABEL_HEIGHT = 16;
const NODE_FOOTER_PADDING = 10;
const NODE_HORIZONTAL_GAP = 70;
const NODE_VERTICAL_GAP = 22;
const INITIAL_ZOOM_STEP = 0.15;
const MIN_ZOOM = 0.08;
const MAX_ZOOM = 3;
const SVG_NS = 'http://www.w3.org/2000/svg';
const FIT_PADDING = 40;
const DEFAULT_CANVAS_WIDTH = 600;
const DEFAULT_CANVAS_HEIGHT = 400;

let _idCounter = 0;
function nextId(prefix) {
  return `${prefix}-${++_idCounter}`;
}

const COLORS = {
  canvasBg: '#020917',
  // Nodes by kind
  operatorBorder: '#60a5fa',
  operatorHeaderBg: '#0f172a',
  operatorCardBg: '#0b1525',
  accessBorder: '#38bdf8',
  accessHeaderBg: '#0b1b33',
  accessCardBg: '#081525',
  joinBorder: '#c084fc',
  joinHeaderBg: '#2a1736',
  joinCardBg: '#1a0d25',
  computeBorder: '#34d399',
  computeHeaderBg: '#0d2520',
  computeCardBg: '#081a18',
  // Text
  headerText: '#f1f5f9',
  subText: '#94a3b8',
  metricLabel: '#64748b',
  metricValue: '#cbd5e1',
  sectionLabel: '#475569',
  // Edges
  edgeColor: '#334155',
  edgeHighlight: '#38bdf8',
  arrowColor: '#475569',
};

const _stateMap = new WeakMap();

function getState(canvas) {
  return _stateMap.get(canvas);
}

// ── Node kind helpers ──────────────────────────────────────────────────────────

function nodeColors(nodeKind) {
  switch (nodeKind) {
    case 'access':
      return { border: COLORS.accessBorder, headerBg: COLORS.accessHeaderBg, cardBg: COLORS.accessCardBg };
    case 'join':
      return { border: COLORS.joinBorder, headerBg: COLORS.joinHeaderBg, cardBg: COLORS.joinCardBg };
    case 'compute':
      return { border: COLORS.computeBorder, headerBg: COLORS.computeHeaderBg, cardBg: COLORS.computeCardBg };
    default:
      return { border: COLORS.operatorBorder, headerBg: COLORS.operatorHeaderBg, cardBg: COLORS.operatorCardBg };
  }
}

function nodeKindLabel(nodeKind) {
  switch (nodeKind) {
    case 'access': return 'DATA ACCESS';
    case 'join': return 'JOIN';
    case 'compute': return 'COMPUTE';
    default: return 'OPERATOR';
  }
}

// ── Node height calculation ───────────────────────────────────────────────────

function computeNodeHeight(node) {
  let height = NODE_HEADER_HEIGHT;

  // Sub-header lines: logicalOp (if different from physicalOp) + objectName
  const showLogical = node.logicalOp && node.logicalOp !== node.physicalOp;
  if (showLogical) height += NODE_SUBHEADER_HEIGHT;
  if (node.objectName) height += NODE_SUBHEADER_HEIGHT;

  // Estimated metrics section
  const estCount = node.estimatedMetrics?.length ?? 0;
  if (estCount > 0) {
    height += NODE_SECTION_LABEL_HEIGHT;
    height += estCount * NODE_METRIC_ROW_HEIGHT;
  }

  // Actual metrics section
  const actCount = node.actualMetrics?.length ?? 0;
  if (actCount > 0) {
    height += NODE_SECTION_LABEL_HEIGHT;
    height += actCount * NODE_METRIC_ROW_HEIGHT;
  }

  return height + NODE_FOOTER_PADDING;
}

// ── Layout ────────────────────────────────────────────────────────────────────

function computeLayout(nodes, edges) {
  const idSet = new Set(nodes.map(n => n.id));
  const inbound = new Map();
  const children = new Map();

  for (const n of nodes) {
    inbound.set(n.id, new Set());
    children.set(n.id, new Set());
  }

  for (const e of edges) {
    if (idSet.has(e.parentId) && idSet.has(e.childId)) {
      inbound.get(e.childId)?.add(e.parentId);
      children.get(e.parentId)?.add(e.childId);
    }
  }

  // BFS from root nodes (no inbound edges) to assign levels.
  const level = new Map();
  const visited = new Set();
  const roots = nodes.filter(n => (inbound.get(n.id)?.size ?? 0) === 0);
  const queue = roots.map(n => ({ id: n.id, lvl: 0 }));

  while (queue.length > 0) {
    const { id, lvl } = queue.shift();
    if (visited.has(id)) continue;
    visited.add(id);
    level.set(id, lvl);
    for (const childId of (children.get(id) ?? [])) {
      if (!visited.has(childId)) {
        queue.push({ id: childId, lvl: lvl + 1 });
      }
    }
  }

  // Assign unvisited nodes sequentially.
  let floatLevel = roots.length === 0 ? 0 : (Math.max(...[...level.values()]) + 1);
  for (const n of nodes) {
    if (!visited.has(n.id)) {
      level.set(n.id, floatLevel++);
    }
  }

  // Group nodes by level.
  const byLevel = new Map();
  const nodeById = new Map(nodes.map(n => [n.id, n]));
  for (const n of nodes) {
    const lvl = level.get(n.id) ?? 0;
    if (!byLevel.has(lvl)) byLevel.set(lvl, []);
    byLevel.get(lvl).push(n);
  }

  // Compute column widths and maximum column height.
  const sortedLevels = [...byLevel.keys()].sort((a, b) => a - b);
  const maxColHeight = sortedLevels.reduce((max, lvl) => {
    const col = byLevel.get(lvl) ?? [];
    const totalH = col.reduce((sum, n) => sum + computeNodeHeight(n), 0)
      + Math.max(0, col.length - 1) * NODE_VERTICAL_GAP;
    return Math.max(max, totalH);
  }, 0);

  const positions = new Map();
  let x = 40;

  for (const lvl of sortedLevels) {
    const col = byLevel.get(lvl) ?? [];
    const colH = col.reduce((sum, n) => sum + computeNodeHeight(n), 0)
      + Math.max(0, col.length - 1) * NODE_VERTICAL_GAP;
    let y = 40 + Math.max(0, (maxColHeight - colH) / 2);

    for (const n of col) {
      positions.set(n.id, { x, y });
      y += computeNodeHeight(n) + NODE_VERTICAL_GAP;
    }

    x += NODE_WIDTH + NODE_HORIZONTAL_GAP;
  }

  return positions;
}

// ── SVG helpers ───────────────────────────────────────────────────────────────

function svgEl(tag, attrs = {}) {
  const el = document.createElementNS(SVG_NS, tag);
  for (const [k, v] of Object.entries(attrs)) {
    el.setAttribute(k, v);
  }
  return el;
}

function truncate(text, maxChars) {
  if (!text) return '';
  return text.length > maxChars ? text.slice(0, maxChars - 1) + '…' : text;
}

function buildArrowMarker(defs, id, color) {
  const marker = svgEl('marker', {
    id,
    markerWidth: '7',
    markerHeight: '5',
    refX: '6',
    refY: '2.5',
    orient: 'auto',
    markerUnits: 'strokeWidth',
  });
  const poly = svgEl('polygon', { points: '0 0, 7 2.5, 0 5', fill: color });
  marker.appendChild(poly);
  defs.appendChild(marker);
}

// ── Node rendering ────────────────────────────────────────────────────────────

function buildNodeGroup(node, x, y) {
  const height = computeNodeHeight(node);
  const colors = nodeColors(node.nodeKind);

  const g = svgEl('g', {
    'data-node-id': node.id,
    transform: `translate(${x},${y})`,
    class: 'de-ep-node',
  });

  // Drop shadow.
  const shadow = svgEl('rect', {
    x: '2', y: '3',
    width: NODE_WIDTH, height,
    rx: '7', ry: '7',
    fill: '#000',
    opacity: '0.35',
  });
  g.appendChild(shadow);

  // Card background.
  const bg = svgEl('rect', {
    x: '0', y: '0',
    width: NODE_WIDTH, height,
    rx: '7', ry: '7',
    fill: colors.cardBg,
    stroke: colors.border,
    'stroke-width': '1',
  });
  g.appendChild(bg);

  // Header background.
  const headerBg = svgEl('rect', {
    x: '0', y: '0',
    width: NODE_WIDTH, height: NODE_HEADER_HEIGHT,
    rx: '7', ry: '7',
    fill: colors.headerBg,
    stroke: colors.border,
    'stroke-width': '1',
  });
  g.appendChild(headerBg);

  // Cover header bottom radius.
  const headerCover = svgEl('rect', {
    x: '0', y: String(NODE_HEADER_HEIGHT - 7),
    width: NODE_WIDTH, height: '7',
    fill: colors.headerBg,
  });
  g.appendChild(headerCover);

  // Header separator.
  const sep = svgEl('line', {
    x1: '0', y1: String(NODE_HEADER_HEIGHT),
    x2: String(NODE_WIDTH), y2: String(NODE_HEADER_HEIGHT),
    stroke: colors.border,
    'stroke-width': '1',
    opacity: '0.6',
  });
  g.appendChild(sep);

  // Kind badge (top-left small label).
  const badge = svgEl('text', {
    x: '8', y: '13',
    fill: colors.border,
    'font-size': '7',
    'font-weight': '700',
    'font-family': 'ui-monospace, Cascadia Mono, Menlo, Consolas, monospace',
    'letter-spacing': '0.06em',
    opacity: '0.75',
  });
  badge.textContent = nodeKindLabel(node.nodeKind);
  g.appendChild(badge);

  // Node ID badge (top-right).
  const idBadge = svgEl('text', {
    x: String(NODE_WIDTH - 8),
    y: '13',
    fill: COLORS.sectionLabel,
    'font-size': '7',
    'text-anchor': 'end',
    'font-family': 'ui-monospace, Cascadia Mono, Menlo, Consolas, monospace',
    opacity: '0.6',
  });
  idBadge.textContent = node.id;
  g.appendChild(idBadge);

  // Physical operator name (main header).
  const opName = svgEl('text', {
    x: '8', y: '33',
    fill: COLORS.headerText,
    'font-size': '12',
    'font-weight': '600',
    'font-family': 'ui-sans-serif, system-ui, sans-serif',
  });
  opName.textContent = truncate(node.physicalOp, 24);
  g.appendChild(opName);

  // Sub-header rows (logicalOp, objectName).
  let subY = NODE_HEADER_HEIGHT + 13;

  const showLogical = node.logicalOp && node.logicalOp !== node.physicalOp;
  if (showLogical) {
    const logicalEl = svgEl('text', {
      x: '8', y: String(subY),
      fill: COLORS.subText,
      'font-size': '9.5',
      'font-family': 'ui-sans-serif, system-ui, sans-serif',
    });
    logicalEl.textContent = truncate(`Logical: ${node.logicalOp}`, 30);
    g.appendChild(logicalEl);
    subY += NODE_SUBHEADER_HEIGHT;
  }

  if (node.objectName) {
    const objEl = svgEl('text', {
      x: '8', y: String(subY),
      fill: colors.border,
      'font-size': '9.5',
      'font-weight': '500',
      'font-family': 'ui-monospace, Cascadia Mono, Menlo, Consolas, monospace',
    });
    objEl.textContent = truncate(node.objectName, 28);
    g.appendChild(objEl);
    subY += NODE_SUBHEADER_HEIGHT;
  }

  // Metrics sections.
  let metricY = subY;

  function renderMetricSection(label, metrics) {
    if (!metrics || metrics.length === 0) return;

    // Section label.
    const sectionEl = svgEl('text', {
      x: String(NODE_WIDTH / 2),
      y: String(metricY + 12),
      fill: COLORS.sectionLabel,
      'font-size': '8',
      'text-anchor': 'middle',
      'font-family': 'ui-monospace, Cascadia Mono, Menlo, Consolas, monospace',
      'letter-spacing': '0.05em',
    });
    sectionEl.textContent = label;
    g.appendChild(sectionEl);
    metricY += NODE_SECTION_LABEL_HEIGHT;

    for (const m of metrics) {
      const labelEl = svgEl('text', {
        x: '10',
        y: String(metricY + 12),
        fill: COLORS.metricLabel,
        'font-size': '9',
        'font-family': 'ui-monospace, Cascadia Mono, Menlo, Consolas, monospace',
      });
      labelEl.textContent = truncate(m.label, 16) + ':';
      g.appendChild(labelEl);

      const valueEl = svgEl('text', {
        x: String(NODE_WIDTH - 10),
        y: String(metricY + 12),
        fill: COLORS.metricValue,
        'font-size': '9',
        'text-anchor': 'end',
        'font-family': 'ui-monospace, Cascadia Mono, Menlo, Consolas, monospace',
      });
      valueEl.textContent = truncate(m.value, 14);
      g.appendChild(valueEl);
      metricY += NODE_METRIC_ROW_HEIGHT;
    }
  }

  renderMetricSection('─ Estimates ─', node.estimatedMetrics);
  renderMetricSection('─ Actuals ─', node.actualMetrics);

  // Tooltip with full operator name.
  const title = svgEl('title');
  title.textContent = [
    node.physicalOp,
    node.logicalOp && node.logicalOp !== node.physicalOp ? `Logical: ${node.logicalOp}` : '',
    node.objectName ? `Object: ${node.objectName}` : '',
  ].filter(Boolean).join('\n');
  g.appendChild(title);

  return g;
}

// ── Edge rendering ────────────────────────────────────────────────────────────

function buildEdgePath(start, end) {
  const dx = end.x - start.x;
  const cp = Math.abs(dx) * 0.45 + 20;
  const cx1 = start.x + cp;
  const cx2 = end.x - cp;
  return `M ${start.x} ${start.y} C ${cx1} ${start.y} ${cx2} ${end.y} ${end.x} ${end.y}`;
}

function buildEdgeGroup(edge, positions, nodeById, markerId) {
  const parentNode = nodeById.get(edge.parentId);
  const childNode = nodeById.get(edge.childId);
  const parentPos = positions.get(edge.parentId);
  const childPos = positions.get(edge.childId);

  if (!parentNode || !childNode || !parentPos || !childPos) return null;

  const parentHeight = computeNodeHeight(parentNode);
  const childHeight = computeNodeHeight(childNode);

  const start = {
    x: parentPos.x + NODE_WIDTH,
    y: parentPos.y + parentHeight / 2,
  };
  const end = {
    x: childPos.x,
    y: childPos.y + childHeight / 2,
  };

  const d = buildEdgePath(start, end);

  const g = svgEl('g', { class: 'de-ep-edge' });

  // Hit area.
  const hit = svgEl('path', {
    d,
    fill: 'none',
    stroke: 'transparent',
    'stroke-width': '10',
    style: 'cursor:default',
  });
  g.appendChild(hit);

  // Visible path.
  const vis = svgEl('path', {
    d,
    fill: 'none',
    stroke: COLORS.edgeColor,
    'stroke-width': '1.5',
    'marker-end': `url(#${markerId})`,
    class: 'de-ep-edge__line',
  });
  g.appendChild(vis);

  g.addEventListener('pointerenter', () => {
    vis.setAttribute('stroke', COLORS.edgeHighlight);
    vis.setAttribute('stroke-width', '2');
  });
  g.addEventListener('pointerleave', () => {
    vis.setAttribute('stroke', COLORS.edgeColor);
    vis.setAttribute('stroke-width', '1.5');
  });

  return g;
}

// ── State & rebuild ───────────────────────────────────────────────────────────

function createState(canvas) {
  const state = {
    canvas,
    svg: null,
    viewGroup: null,
    edgeGroup: null,
    nodeGroup: null,
    defs: null,
    nodes: [],
    edges: [],
    positions: new Map(),
    zoom: 1,
    panX: 0,
    panY: 0,
    isPanning: false,
    panStartX: 0,
    panStartY: 0,
    panStartPanX: 0,
    panStartPanY: 0,
  };
  _stateMap.set(canvas, state);
  return state;
}

function applyTransform(state) {
  if (!state.viewGroup) return;
  state.viewGroup.setAttribute(
    'transform',
    `translate(${state.panX},${state.panY}) scale(${state.zoom})`
  );
}

function rebuildEdges(state) {
  const edgeGroup = state.edgeGroup;
  if (!edgeGroup) return;
  while (edgeGroup.firstChild) edgeGroup.removeChild(edgeGroup.firstChild);

  const markerId = nextId('de-ep-arrow');
  buildArrowMarker(state.defs, markerId, COLORS.arrowColor);

  const nodeById = new Map(state.nodes.map(n => [n.id, n]));
  for (const edge of state.edges) {
    const eg = buildEdgeGroup(edge, state.positions, nodeById, markerId);
    if (eg) edgeGroup.appendChild(eg);
  }
}

function rebuildNodes(state) {
  const nodeGroup = state.nodeGroup;
  if (!nodeGroup) return;
  while (nodeGroup.firstChild) nodeGroup.removeChild(nodeGroup.firstChild);

  for (const node of state.nodes) {
    const pos = state.positions.get(node.id);
    if (!pos) continue;
    const ng = buildNodeGroup(node, pos.x, pos.y);
    nodeGroup.appendChild(ng);
  }
}

function fullRebuild(state) {
  rebuildEdges(state);
  rebuildNodes(state);
}

// ── Pointer handling (pan) ─────────────────────────────────────────────────────

function onPointerDown(state, evt) {
  // Only handle canvas-level drag (not node hover).
  if (evt.target.closest('.de-ep-node')) return;
  state.isPanning = true;
  state.panStartX = evt.clientX;
  state.panStartY = evt.clientY;
  state.panStartPanX = state.panX;
  state.panStartPanY = state.panY;
  evt.currentTarget.setPointerCapture(evt.pointerId);
}

function onPointerMove(state, evt) {
  if (!state.isPanning) return;
  const dx = evt.clientX - state.panStartX;
  const dy = evt.clientY - state.panStartY;
  state.panX = state.panStartPanX + dx;
  state.panY = state.panStartPanY + dy;
  applyTransform(state);
}

function onPointerUp(state) {
  state.isPanning = false;
}

function onWheel(state, evt) {
  evt.preventDefault();
  const rect = state.canvas.getBoundingClientRect();
  const mx = evt.clientX - rect.left;
  const my = evt.clientY - rect.top;
  const delta = evt.deltaY < 0 ? INITIAL_ZOOM_STEP : -INITIAL_ZOOM_STEP;
  const newZoom = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, state.zoom + delta));
  const ratio = newZoom / state.zoom;
  state.panX = mx - ratio * (mx - state.panX);
  state.panY = my - ratio * (my - state.panY);
  state.zoom = newZoom;
  applyTransform(state);
}

// ── Public API ────────────────────────────────────────────────────────────────

export function initPlan(canvas, planData) {
  destroyPlan(canvas);

  const state = createState(canvas);
  state.nodes = planData.nodes ?? [];
  state.edges = planData.edges ?? [];
  state.positions = computeLayout(state.nodes, state.edges);

  const svg = svgEl('svg', {
    width: '100%',
    height: '100%',
    style: 'display:block;',
  });
  state.svg = svg;

  const defs = svgEl('defs');
  state.defs = defs;
  svg.appendChild(defs);

  const viewGroup = svgEl('g');
  state.viewGroup = viewGroup;
  svg.appendChild(viewGroup);

  const edgeGroup = svgEl('g', { class: 'de-ep-edges' });
  state.edgeGroup = edgeGroup;
  viewGroup.appendChild(edgeGroup);

  const nodeGroup = svgEl('g', { class: 'de-ep-nodes' });
  state.nodeGroup = nodeGroup;
  viewGroup.appendChild(nodeGroup);

  canvas.appendChild(svg);

  // Pointer events for pan.
  const boundPointerDown = (evt) => onPointerDown(state, evt);
  const boundPointerMove = (evt) => onPointerMove(state, evt);
  const boundPointerUp = () => onPointerUp(state);
  const boundWheel = (evt) => onWheel(state, evt);

  canvas.addEventListener('pointerdown', boundPointerDown);
  canvas.addEventListener('pointermove', boundPointerMove);
  canvas.addEventListener('pointerup', boundPointerUp);
  canvas.addEventListener('pointerleave', boundPointerUp);
  canvas.addEventListener('pointercancel', boundPointerUp);
  canvas.addEventListener('wheel', boundWheel, { passive: false });

  state._cleanup = () => {
    canvas.removeEventListener('pointerdown', boundPointerDown);
    canvas.removeEventListener('pointermove', boundPointerMove);
    canvas.removeEventListener('pointerup', boundPointerUp);
    canvas.removeEventListener('pointerleave', boundPointerUp);
    canvas.removeEventListener('pointercancel', boundPointerUp);
    canvas.removeEventListener('wheel', boundWheel);
  };

  fullRebuild(state);
  fitPlan(canvas);
}

export function destroyPlan(canvas) {
  const state = getState(canvas);
  if (!state) return;
  state._cleanup?.();
  canvas.innerHTML = '';
  _stateMap.delete(canvas);
}

export function zoomIn(canvas) {
  const state = getState(canvas);
  if (!state) return;
  const rect = canvas.getBoundingClientRect();
  const cx = rect.width / 2;
  const cy = rect.height / 2;
  const newZoom = Math.min(MAX_ZOOM, state.zoom + INITIAL_ZOOM_STEP);
  const ratio = newZoom / state.zoom;
  state.panX = cx - ratio * (cx - state.panX);
  state.panY = cy - ratio * (cy - state.panY);
  state.zoom = newZoom;
  applyTransform(state);
}

export function zoomOut(canvas) {
  const state = getState(canvas);
  if (!state) return;
  const rect = canvas.getBoundingClientRect();
  const cx = rect.width / 2;
  const cy = rect.height / 2;
  const newZoom = Math.max(MIN_ZOOM, state.zoom - INITIAL_ZOOM_STEP);
  const ratio = newZoom / state.zoom;
  state.panX = cx - ratio * (cx - state.panX);
  state.panY = cy - ratio * (cy - state.panY);
  state.zoom = newZoom;
  applyTransform(state);
}

export function fitPlan(canvas) {
  const state = getState(canvas);
  if (!state || state.nodes.length === 0) return;

  const cw = canvas.clientWidth || DEFAULT_CANVAS_WIDTH;
  const ch = canvas.clientHeight || DEFAULT_CANVAS_HEIGHT;

  // Compute bounding box.
  let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
  for (const node of state.nodes) {
    const pos = state.positions.get(node.id);
    if (!pos) continue;
    const h = computeNodeHeight(node);
    minX = Math.min(minX, pos.x);
    minY = Math.min(minY, pos.y);
    maxX = Math.max(maxX, pos.x + NODE_WIDTH);
    maxY = Math.max(maxY, pos.y + h);
  }

  const diagW = maxX - minX;
  const diagH = maxY - minY;
  if (diagW <= 0 || diagH <= 0) return;

  const scaleX = (cw - FIT_PADDING * 2) / diagW;
  const scaleY = (ch - FIT_PADDING * 2) / diagH;
  const scale = Math.min(scaleX, scaleY, 1);

  state.zoom = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, scale));
  state.panX = (cw - diagW * state.zoom) / 2 - minX * state.zoom;
  state.panY = (ch - diagH * state.zoom) / 2 - minY * state.zoom;
  applyTransform(state);
}
