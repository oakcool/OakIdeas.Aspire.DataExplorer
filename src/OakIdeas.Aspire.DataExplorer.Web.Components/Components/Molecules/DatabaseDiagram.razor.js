/**
 * DatabaseDiagram.razor.js
 * Interactive SVG-based ERD diagram engine.
 * Uses vanilla JS + SVG — no external library dependencies.
 */

const CARD_WIDTH = 220;
const CARD_HEADER_HEIGHT = 44;
const CARD_ROW_HEIGHT = 22;
const CARD_FOOTER_PADDING = 6;
const CARD_HORIZONTAL_GAP = 60;
const CARD_VERTICAL_GAP = 40;
const INITIAL_ZOOM_STEP = 0.15;
const MIN_ZOOM = 0.1;
const MAX_ZOOM = 3;
const SVG_NS = 'http://www.w3.org/2000/svg';

// Monotonically increasing counter for stable, unique SVG element IDs.
let _idCounter = 0;
function nextId(prefix) {
  return `${prefix}-${++_idCounter}`;
}

// Token colours — match the app's design system.
const COLORS = {
  cardBorder: '#1e293b',
  cardHeaderBg: '#0b1120',
  headerText: '#f1f5f9',
  schemaText: '#64748b',
  rowBg: '#0f172a',
  rowAltBg: '#0c1425',
  rowText: '#cbd5e1',
  typeText: '#64748b',
  pkColor: '#fbbf24',
  fkColor: '#38bdf8',
  idColor: '#a78bfa',
  nullText: '#475569',
  edgeColor: '#334155',
  edgeHighlight: '#38bdf8',
  arrowHead: '#475569',
  viewHeaderBg: '#0e1a2b',
  viewBorder: '#1e3a5f',
  selectionBorder: '#38bdf8',
  dimmedOpacity: 0.25,
};

// Map of canvas-element → state object so multiple instances work correctly.
const _stateMap = new WeakMap();

function getState(canvas) {
  return _stateMap.get(canvas);
}

function entityCardHeight(entity) {
  return CARD_HEADER_HEIGHT + entity.columns.length * CARD_ROW_HEIGHT + CARD_FOOTER_PADDING;
}

function sortEntities(entities) {
  return [...entities].sort((a, b) => {
    const schemaCompare = a.schema.localeCompare(b.schema);
    return schemaCompare !== 0 ? schemaCompare : a.name.localeCompare(b.name);
  });
}

function computeGridLayout(entities) {
  const sortedEntities = sortEntities(entities);
  const count = sortedEntities.length;
  const columns = Math.max(2, Math.ceil(Math.sqrt(count)));
  const positions = new Map();

  let x = 40;
  let y = 40;
  let colIndex = 0;

  for (const entity of sortedEntities) {
    positions.set(entity.id, { x, y });
    const h = entityCardHeight(entity);
    y += h + CARD_VERTICAL_GAP;
    colIndex++;

    if (colIndex >= Math.ceil(count / columns)) {
      colIndex = 0;
      x += CARD_WIDTH + CARD_HORIZONTAL_GAP;
      y = 40;
    }
  }

  return positions;
}

// ── Layout ───────────────────────────────────────────────────────────────────

/**
 * Compute initial positions using a simple hierarchical/topological layout.
 * Tables that are referenced by others are placed to the right.
 * Returns a Map<entityId, {x, y}>.
 */
function computeLayout(entities, relationships) {
  const idSet = new Set(entities.map(e => e.id));
  // Build adjacency: childId → Set<parentId>
  const inbound = new Map();
  const outbound = new Map();
  for (const e of entities) {
    inbound.set(e.id, new Set());
    outbound.set(e.id, new Set());
  }
  for (const r of relationships) {
    if (idSet.has(r.parentEntityId) && idSet.has(r.referencedEntityId)) {
      outbound.get(r.parentEntityId)?.add(r.referencedEntityId);
      inbound.get(r.referencedEntityId)?.add(r.parentEntityId);
    }
  }

  const validRelationshipCount = [...outbound.values()].reduce((sum, rels) => sum + rels.size, 0);
  if (validRelationshipCount === 0) {
    return computeGridLayout(entities);
  }

  // Assign tiers (columns) via simple BFS / topological levels.
  const tier = new Map();
  const visited = new Set();
  // Roots: nodes with no inbound edges.
  const roots = entities.filter(e => (inbound.get(e.id)?.size ?? 0) === 0);
  const queue = roots.map(e => ({ id: e.id, level: 0 }));

  while (queue.length > 0) {
    const { id, level } = queue.shift();
    if (visited.has(id)) continue;
    visited.add(id);
    tier.set(id, level);
    for (const childId of (outbound.get(id) ?? [])) {
      if (!visited.has(childId)) {
        queue.push({ id: childId, level: level + 1 });
      }
    }
  }
  // Assign unvisited nodes (disconnected) to their own tier.
  let floatTier = (roots.length === 0 ? 0 : Math.max(...[...tier.values()]) + 1);
  for (const e of entities) {
    if (!visited.has(e.id)) {
      tier.set(e.id, floatTier);
      floatTier++;
    }
  }

  // Group by tier, then assign x/y positions.
  const byTier = new Map();
  for (const e of entities) {
    const t = tier.get(e.id) ?? 0;
    if (!byTier.has(t)) byTier.set(t, []);
    byTier.get(t).push(e);
  }

  const positions = new Map();
  const sortedTiers = [...byTier.keys()].sort((a, b) => a - b);
  const maxColumnHeight = sortedTiers.reduce((maxHeight, t) => {
    const col = byTier.get(t) ?? [];
    const totalHeight = col.reduce((sum, entity) => sum + entityCardHeight(entity), 0)
      + Math.max(0, col.length - 1) * CARD_VERTICAL_GAP;
    return Math.max(maxHeight, totalHeight);
  }, 0);

  let x = 40;
  for (const t of sortedTiers) {
    const col = [...(byTier.get(t) ?? [])].sort((a, b) => {
      const outboundDelta = (outbound.get(b.id)?.size ?? 0) - (outbound.get(a.id)?.size ?? 0);
      if (outboundDelta !== 0) return outboundDelta;
      const inboundDelta = (inbound.get(b.id)?.size ?? 0) - (inbound.get(a.id)?.size ?? 0);
      if (inboundDelta !== 0) return inboundDelta;
      return a.name.localeCompare(b.name);
    });

    let maxWidth = 0;
    const colHeight = col.reduce((sum, entity) => sum + entityCardHeight(entity), 0)
      + Math.max(0, col.length - 1) * CARD_VERTICAL_GAP;
    let y = 40 + Math.max(0, (maxColumnHeight - colHeight) / 2);

    for (const e of col) {
      positions.set(e.id, { x, y });
      const h = entityCardHeight(e);
      y += h + CARD_VERTICAL_GAP;
      maxWidth = Math.max(maxWidth, CARD_WIDTH);
    }
    x += maxWidth + CARD_HORIZONTAL_GAP;
  }

  return positions;
}

// ── SVG helpers ──────────────────────────────────────────────────────────────

function svgEl(tag, attrs = {}) {
  const el = document.createElementNS(SVG_NS, tag);
  for (const [k, v] of Object.entries(attrs)) {
    el.setAttribute(k, v);
  }
  return el;
}

function truncate(text, maxChars) {
  return text.length > maxChars ? text.slice(0, maxChars - 1) + '…' : text;
}

function buildArrowMarker(defs, id, color) {
  const marker = svgEl('marker', {
    id,
    markerWidth: '8',
    markerHeight: '6',
    refX: '7',
    refY: '3',
    orient: 'auto',
    markerUnits: 'strokeWidth',
  });
  const poly = svgEl('polygon', {
    points: '0 0, 8 3, 0 6',
    fill: color,
  });
  marker.appendChild(poly);
  defs.appendChild(marker);
}

function buildCardGroup(entity, x, y, onPointerDown) {
  const height = entityCardHeight(entity);
  const isView = entity.entityType === 'View';
  const borderColor = isView ? COLORS.viewBorder : COLORS.cardBorder;
  const headerBg = isView ? COLORS.viewHeaderBg : COLORS.cardHeaderBg;

  const g = svgEl('g', {
    'data-entity-id': entity.id,
    transform: `translate(${x},${y})`,
    class: 'de-diagram-card',
    style: 'cursor:move',
  });

  // Shadow / outline rect.
  const shadow = svgEl('rect', {
    x: '2', y: '2',
    width: CARD_WIDTH, height: height,
    rx: '6', ry: '6',
    fill: 'none',
    stroke: '#000',
    'stroke-width': '1',
    opacity: '0.4',
  });
  g.appendChild(shadow);

  // Main card background.
  const bg = svgEl('rect', {
    x: '0', y: '0',
    width: CARD_WIDTH, height: height,
    rx: '6', ry: '6',
    fill: COLORS.cardBg,
    stroke: borderColor,
    'stroke-width': '1',
  });
  g.appendChild(bg);

  // Header background.
  const headerBgEl = svgEl('rect', {
    x: '0', y: '0',
    width: CARD_WIDTH,
    height: CARD_HEADER_HEIGHT,
    rx: '6', ry: '6',
    fill: headerBg,
    stroke: borderColor,
    'stroke-width': '1',
  });
  g.appendChild(headerBgEl);
  // Cover bottom radius.
  const headerCover = svgEl('rect', {
    x: '0', y: String(CARD_HEADER_HEIGHT - 6),
    width: CARD_WIDTH, height: '6',
    fill: headerBg,
  });
  g.appendChild(headerCover);
  // Header separator line.
  const sep = svgEl('line', {
    x1: '0', y1: CARD_HEADER_HEIGHT,
    x2: CARD_WIDTH, y2: CARD_HEADER_HEIGHT,
    stroke: borderColor,
    'stroke-width': '1',
  });
  g.appendChild(sep);

  // Entity type badge.
  const typeLabel = isView ? 'VIEW' : 'TABLE';
  const typeBadge = svgEl('text', {
    x: '8', y: '13',
    fill: isView ? '#38bdf8' : '#94a3b8',
    'font-size': '8',
    'font-weight': '700',
    'font-family': 'ui-monospace, Cascadia Mono, Menlo, Consolas, monospace',
    'letter-spacing': '0.08em',
    opacity: '0.7',
  });
  typeBadge.textContent = typeLabel;
  g.appendChild(typeBadge);

  // Schema text.
  const schemaEl = svgEl('text', {
    x: String(CARD_WIDTH - 8),
    y: '13',
    fill: COLORS.schemaText,
    'font-size': '8',
    'text-anchor': 'end',
    'font-family': 'ui-sans-serif, system-ui, sans-serif',
    opacity: '0.7',
  });
  schemaEl.textContent = truncate(entity.schema, 20);
  g.appendChild(schemaEl);

  // Entity name.
  const nameEl = svgEl('text', {
    x: '8', y: '34',
    fill: COLORS.headerText,
    'font-size': '11',
    'font-weight': '600',
    'font-family': 'ui-sans-serif, system-ui, sans-serif',
  });
  nameEl.textContent = truncate(entity.name, 26);
  g.appendChild(nameEl);

  // Column rows.
  entity.columns.forEach((col, i) => {
    const rowY = CARD_HEADER_HEIGHT + i * CARD_ROW_HEIGHT;
    const rowBg = svgEl('rect', {
      x: '0', y: String(rowY),
      width: CARD_WIDTH,
      height: CARD_ROW_HEIGHT,
      fill: i % 2 === 0 ? COLORS.rowBg : COLORS.rowAltBg,
    });
    g.appendChild(rowBg);

    // PK / FK badge.
    if (col.isPrimaryKey) {
      const pk = svgEl('text', {
        x: '6', y: String(rowY + 14),
        fill: COLORS.pkColor,
        'font-size': '7',
        'font-weight': '700',
        'font-family': 'ui-monospace, Cascadia Mono, Menlo, Consolas, monospace',
      });
      pk.textContent = 'PK';
      g.appendChild(pk);
    } else if (col.isForeignKey) {
      const fk = svgEl('text', {
        x: '6', y: String(rowY + 14),
        fill: COLORS.fkColor,
        'font-size': '7',
        'font-weight': '700',
        'font-family': 'ui-monospace, Cascadia Mono, Menlo, Consolas, monospace',
      });
      fk.textContent = 'FK';
      g.appendChild(fk);
    } else if (col.isIdentity) {
      const id = svgEl('text', {
        x: '6', y: String(rowY + 14),
        fill: COLORS.idColor,
        'font-size': '7',
        'font-weight': '700',
        'font-family': 'ui-monospace, Cascadia Mono, Menlo, Consolas, monospace',
      });
      id.textContent = 'ID';
      g.appendChild(id);
    }

    // Column name.
    const colName = svgEl('text', {
      x: '22', y: String(rowY + 14),
      fill: col.isPrimaryKey ? COLORS.pkColor : col.isForeignKey ? COLORS.fkColor : COLORS.rowText,
      'font-size': '9.5',
      'font-family': 'ui-monospace, Cascadia Mono, Menlo, Consolas, monospace',
    });
    const maxColChars = col.isNullable ? 16 : 18;
    colName.textContent = truncate(col.name, maxColChars);
    g.appendChild(colName);

    // Nullable indicator.
    if (col.isNullable) {
      const nullable = svgEl('text', {
        x: '138', y: String(rowY + 14),
        fill: COLORS.nullText,
        'font-size': '7.5',
        'font-family': 'ui-monospace, Cascadia Mono, Menlo, Consolas, monospace',
        opacity: '0.7',
      });
      nullable.textContent = 'NULL';
      g.appendChild(nullable);
    }

    // Data type.
    const typeEl = svgEl('text', {
      x: String(CARD_WIDTH - 6),
      y: String(rowY + 14),
      fill: COLORS.typeText,
      'font-size': '8.5',
      'text-anchor': 'end',
      'font-family': 'ui-monospace, Cascadia Mono, Menlo, Consolas, monospace',
    });
    typeEl.textContent = truncate(col.dataType, 14);
    g.appendChild(typeEl);
  });

  // Bottom clip rect (rounded corners).
  const bottomClip = svgEl('rect', {
    x: '0', y: String(height - CARD_FOOTER_PADDING),
    width: CARD_WIDTH,
    height: CARD_FOOTER_PADDING,
    fill: COLORS.cardBg,
    rx: '0', ry: '0',
  });
  g.appendChild(bottomClip);

  // Drag interaction — attach to the main rect.
  g.addEventListener('pointerdown', onPointerDown, { passive: false });

  return g;
}

// ── Edge drawing ─────────────────────────────────────────────────────────────

function computeAnchorPoints(positions, entities) {
  const anchors = new Map();
  for (const e of entities) {
    const pos = positions.get(e.id);
    if (!pos) continue;
    const h = entityCardHeight(e);
    anchors.set(e.id, {
      left:   { x: pos.x,               y: pos.y + h / 2 },
      right:  { x: pos.x + CARD_WIDTH,  y: pos.y + h / 2 },
      top:    { x: pos.x + CARD_WIDTH / 2, y: pos.y },
      bottom: { x: pos.x + CARD_WIDTH / 2, y: pos.y + h },
    });
  }
  return anchors;
}

function bestAnchor(from, to) {
  const dx = to.right.x - from.right.x;
  if (dx >= 0) {
    return { start: from.right, end: to.left };
  } else {
    return { start: from.left, end: to.right };
  }
}

function buildEdgePath(start, end, relationshipIndex = 0) {
  const dx = end.x - start.x;
  const dy = end.y - start.y;
  const len = Math.hypot(dx, dy);
  if (len < 0.001) {
    return `M ${start.x} ${start.y} L ${end.x} ${end.y}`;
  }

  const nx = -dy / len;
  const ny = dx / len;
  const lane = (relationshipIndex % 5) - 2; // -2..2
  const baseOffset = Math.min(90, Math.max(32, len * 0.18));
  const bendOffset = lane === 0 ? baseOffset : Math.sign(lane) * (baseOffset + Math.abs(lane) * 10);
  const mx = (start.x + end.x) / 2;
  const my = (start.y + end.y) / 2;
  const cx = mx + nx * bendOffset;
  const cy = my + ny * bendOffset;

  return `M ${start.x} ${start.y} Q ${cx} ${cy} ${end.x} ${end.y}`;
}

function buildEdgeGroup(relationship, anchors, markerId, relationshipIndex) {
  const fromAnchors = anchors.get(relationship.parentEntityId);
  const toAnchors = anchors.get(relationship.referencedEntityId);
  if (!fromAnchors || !toAnchors) return null;

  const { start, end } = bestAnchor(fromAnchors, toAnchors);
  const d = buildEdgePath(start, end, relationshipIndex);

  const g = svgEl('g', {
    'data-edge-id': relationship.id,
    class: 'de-diagram-edge',
  });

  // Hit area (wider, invisible stroke).
  const hitPath = svgEl('path', {
    d,
    fill: 'none',
    stroke: 'transparent',
    'stroke-width': '10',
    style: 'cursor:pointer',
  });
  g.appendChild(hitPath);

  // Visible stroke.
  const visPath = svgEl('path', {
    d,
    fill: 'none',
    stroke: COLORS.edgeColor,
    'stroke-width': '1.5',
    'marker-end': `url(#${markerId})`,
    class: 'de-diagram-edge__line',
  });
  g.appendChild(visPath);

  // Hover tooltip via title.
  const title = svgEl('title');
  title.textContent = relationship.constraintName;
  g.appendChild(title);

  g.addEventListener('pointerenter', () => {
    visPath.setAttribute('stroke', COLORS.edgeHighlight);
    visPath.setAttribute('stroke-width', '2');
  });
  g.addEventListener('pointerleave', () => {
    visPath.setAttribute('stroke', COLORS.edgeColor);
    visPath.setAttribute('stroke-width', '1.5');
  });

  return g;
}

// ── Diagram state & public API ────────────────────────────────────────────────

function createState(canvas) {
  const state = {
    canvas,
    svg: null,
    viewGroup: null,
    edgeGroup: null,
    cardGroup: null,
    defs: null,
    entities: [],
    relationships: [],
    positions: new Map(),
    zoom: 1,
    panX: 0,
    panY: 0,
    isPanning: false,
    panStartX: 0,
    panStartY: 0,
    panStartPanX: 0,
    panStartPanY: 0,
    draggingEntityId: null,
    dragStartX: 0,
    dragStartY: 0,
    dragStartEntityX: 0,
    dragStartEntityY: 0,
    filterTerm: '',
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
  // Clear previous edges.
  while (edgeGroup.firstChild) edgeGroup.removeChild(edgeGroup.firstChild);

  const anchors = computeAnchorPoints(state.positions, state.entities);
  const markerId = nextId('de-diagram-arrow');

  // Ensure arrowhead marker.
  buildArrowMarker(state.defs, markerId, COLORS.arrowHead);

  const edgeLaneByPair = new Map();
  for (const rel of state.relationships) {
    const pairKey = `${rel.parentEntityId}->${rel.referencedEntityId}`;
    const relationshipIndex = edgeLaneByPair.get(pairKey) ?? 0;
    edgeLaneByPair.set(pairKey, relationshipIndex + 1);
    const eg = buildEdgeGroup(rel, anchors, markerId, relationshipIndex);
    if (eg) edgeGroup.appendChild(eg);
  }
}

function rebuildCards(state) {
  const cardGroup = state.cardGroup;
  if (!cardGroup) return;
  while (cardGroup.firstChild) cardGroup.removeChild(cardGroup.firstChild);

  for (const entity of state.entities) {
    const pos = state.positions.get(entity.id);
    if (!pos) continue;

    const card = buildCardGroup(
      entity,
      pos.x,
      pos.y,
      (evt) => onCardPointerDown(state, entity.id, evt)
    );
    cardGroup.appendChild(card);
  }
}

function fullRebuild(state) {
  rebuildEdges(state);
  rebuildCards(state);
}

// ── Pointer event handlers ────────────────────────────────────────────────────

function onCardPointerDown(state, entityId, evt) {
  evt.stopPropagation();
  evt.preventDefault();
  state.draggingEntityId = entityId;
  const pos = state.positions.get(entityId);
  state.dragStartX = evt.clientX;
  state.dragStartY = evt.clientY;
  state.dragStartEntityX = pos.x;
  state.dragStartEntityY = pos.y;
}

function onCanvasPointerDown(state, evt) {
  if (state.draggingEntityId) return;
  state.isPanning = true;
  state.panStartX = evt.clientX;
  state.panStartY = evt.clientY;
  state.panStartPanX = state.panX;
  state.panStartPanY = state.panY;
}

function onPointerMove(state, evt) {
  if (state.draggingEntityId) {
    const dx = (evt.clientX - state.dragStartX) / state.zoom;
    const dy = (evt.clientY - state.dragStartY) / state.zoom;
    const newX = state.dragStartEntityX + dx;
    const newY = state.dragStartEntityY + dy;
    state.positions.set(state.draggingEntityId, { x: newX, y: newY });
    // Update card position directly.
    const card = state.cardGroup?.querySelector(`[data-entity-id="${state.draggingEntityId}"]`);
    if (card) card.setAttribute('transform', `translate(${newX},${newY})`);
    rebuildEdges(state);
  } else if (state.isPanning) {
    const dx = evt.clientX - state.panStartX;
    const dy = evt.clientY - state.panStartY;
    state.panX = state.panStartPanX + dx;
    state.panY = state.panStartPanY + dy;
    applyTransform(state);
  }
}

function onPointerUp(state) {
  state.draggingEntityId = null;
  state.isPanning = false;
}

function onWheel(state, evt) {
  evt.preventDefault();
  const rect = state.canvas.getBoundingClientRect();
  const mouseX = evt.clientX - rect.left;
  const mouseY = evt.clientY - rect.top;

  const delta = evt.deltaY > 0 ? -INITIAL_ZOOM_STEP : INITIAL_ZOOM_STEP;
  const newZoom = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, state.zoom + delta));

  const ratio = newZoom / state.zoom;
  state.panX = mouseX - ratio * (mouseX - state.panX);
  state.panY = mouseY - ratio * (mouseY - state.panY);
  state.zoom = newZoom;

  applyTransform(state);
}

// ── Public exported functions ─────────────────────────────────────────────────

export function initDiagram(canvas, diagramData) {
  // Clean up any previous instance.
  destroyDiagram(canvas);

  const state = createState(canvas);
  state.entities = diagramData.entities ?? [];
  state.relationships = diagramData.relationships ?? [];
  state.positions = computeLayout(state.entities, state.relationships);

  // Create SVG root.
  const svg = svgEl('svg', {
    width: '100%',
    height: '100%',
    style: 'display:block;',
  });
  state.svg = svg;

  // Defs for markers.
  const defs = svgEl('defs');
  state.defs = defs;
  svg.appendChild(defs);

  // Single transform group for zoom/pan.
  const viewGroup = svgEl('g');
  state.viewGroup = viewGroup;
  svg.appendChild(viewGroup);

  // Edge layer (drawn first, below cards).
  const edgeGroup = svgEl('g', { class: 'de-diagram-edges' });
  state.edgeGroup = edgeGroup;
  viewGroup.appendChild(edgeGroup);

  // Card layer.
  const cardGroup = svgEl('g', { class: 'de-diagram-cards' });
  state.cardGroup = cardGroup;
  viewGroup.appendChild(cardGroup);

  canvas.appendChild(svg);

  // Attach canvas pointer events.
  const boundPointerDown = (evt) => onCanvasPointerDown(state, evt);
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
  fitDiagram(canvas);
}

export function destroyDiagram(canvas) {
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

export function fitDiagram(canvas) {
  const state = getState(canvas);
  if (!state || state.entities.length === 0) return;

  const canvasRect = canvas.getBoundingClientRect();
  const cw = canvasRect.width || 800;
  const ch = canvasRect.height || 600;

  // Compute bounding box of all entities.
  let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
  for (const e of state.entities) {
    const pos = state.positions.get(e.id);
    if (!pos) continue;
    const h = entityCardHeight(e);
    minX = Math.min(minX, pos.x);
    minY = Math.min(minY, pos.y);
    maxX = Math.max(maxX, pos.x + CARD_WIDTH);
    maxY = Math.max(maxY, pos.y + h);
  }

  const diagW = maxX - minX;
  const diagH = maxY - minY;
  if (diagW <= 0 || diagH <= 0) return;

  const padding = 40;
  const scaleX = (cw - padding * 2) / diagW;
  const scaleY = (ch - padding * 2) / diagH;
  const scale = Math.min(scaleX, scaleY, 1); // Don't upscale beyond 1x.

  state.zoom = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, scale));
  state.panX = (cw - diagW * state.zoom) / 2 - minX * state.zoom;
  state.panY = (ch - diagH * state.zoom) / 2 - minY * state.zoom;
  applyTransform(state);
}

export function resetLayout(canvas) {
  const state = getState(canvas);
  if (!state) return;
  state.positions = computeLayout(state.entities, state.relationships);
  fullRebuild(state);
  fitDiagram(canvas);
}

/**
 * Filter visible entities by a search term.
 * Returns the count of matching entities.
 */
export function filterEntities(canvas, term) {
  const state = getState(canvas);
  if (!state) return 0;
  state.filterTerm = (term ?? '').trim().toLowerCase();

  if (!state.cardGroup) return state.entities.length;

  let count = 0;
  for (const card of state.cardGroup.querySelectorAll('[data-entity-id]')) {
    const entityId = card.getAttribute('data-entity-id');
    const entity = state.entities.find(e => e.id === entityId);
    if (!entity) continue;
    const matches = !state.filterTerm
      || entity.name.toLowerCase().includes(state.filterTerm)
      || entity.schema.toLowerCase().includes(state.filterTerm)
      || entity.columns.some(c => c.name.toLowerCase().includes(state.filterTerm));

    card.style.opacity = matches ? '1' : String(COLORS.dimmedOpacity);
    if (matches) count++;
  }

  // Dim edges when filtering.
  if (state.edgeGroup) {
    for (const edge of state.edgeGroup.querySelectorAll('[data-edge-id]')) {
      edge.style.opacity = state.filterTerm ? String(COLORS.dimmedOpacity) : '1';
    }
  }

  return count;
}
