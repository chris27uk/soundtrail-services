const state = {
  templates: [],
  kinds: [],
  kind: "work",
  builtKey: null,
  selectedStreamId: null,
};

const els = {
  templateSelect: document.getElementById("template-select"),
  templateDescription: document.getElementById("template-description"),
  templatePattern: document.getElementById("template-pattern"),
  templateFields: document.getElementById("template-fields"),
  buildKey: document.getElementById("build-key"),
  openKeyed: document.getElementById("open-keyed"),
  keyPreview: document.getElementById("key-preview"),
  streamFilter: document.getElementById("stream-filter"),
  refreshStreams: document.getElementById("refresh-streams"),
  browseMeta: document.getElementById("browse-meta"),
  streamRows: document.getElementById("stream-rows"),
  eventsSubtitle: document.getElementById("events-subtitle"),
  eventsEmpty: document.getElementById("events-empty"),
  eventsContent: document.getElementById("events-content"),
  streamFacts: document.getElementById("stream-facts"),
  eventList: document.getElementById("event-list"),
  copyStreamId: document.getElementById("copy-stream-id"),
  toast: document.getElementById("toast"),
};

function toast(message) {
  els.toast.hidden = false;
  els.toast.textContent = message;
  clearTimeout(toast._t);
  toast._t = setTimeout(() => { els.toast.hidden = true; }, 2800);
}

async function api(path, options) {
  const response = await fetch(path, {
    headers: { "Content-Type": "application/json", ...(options?.headers ?? {}) },
    ...options,
  });
  const payload = await response.json().catch(() => ({}));
  if (!response.ok) {
    throw new Error(payload.error || `Request failed (${response.status})`);
  }
  return payload;
}

function selectedTemplate() {
  return state.templates.find((t) => t.id === els.templateSelect.value);
}

function renderTemplateFields() {
  const template = selectedTemplate();
  if (!template) return;

  els.templateDescription.textContent = template.description;
  els.templatePattern.textContent = `Pattern: ${template.pattern}`;
  els.templateFields.innerHTML = "";

  for (const field of template.fields) {
    const label = document.createElement("label");
    label.className = "field";
    label.innerHTML = `
      <span>${field.label}${field.isRequired ? "" : " (optional)"}</span>
      <input data-field="${field.name}" type="text" placeholder="${field.placeholder}" autocomplete="off" />
    `;
    els.templateFields.appendChild(label);
  }

  state.builtKey = null;
  els.openKeyed.disabled = true;
  els.keyPreview.hidden = true;
}

function collectFieldValues() {
  const values = {};
  for (const input of els.templateFields.querySelectorAll("input[data-field]")) {
    values[input.dataset.field] = input.value;
  }
  return values;
}

async function buildKey() {
  try {
    const result = await api("/api/keys/build", {
      method: "POST",
      body: JSON.stringify({
        templateId: els.templateSelect.value,
        values: collectFieldValues(),
      }),
    });
    state.builtKey = result;
    els.openKeyed.disabled = false;
    els.keyPreview.hidden = false;
    els.keyPreview.textContent = JSON.stringify(result, null, 2);
    toast("Key built");
  } catch (error) {
    toast(error.message);
  }
}

async function openKeyedStream() {
  if (!state.builtKey) return;
  state.kind = state.builtKey.kind;
  document.querySelectorAll(".seg").forEach((btn) => {
    btn.classList.toggle("active", btn.dataset.kind === state.kind);
  });
  await loadStreams();
  await openStream(state.builtKey.kind, state.builtKey.streamId);
}

async function loadStreams() {
  const q = els.streamFilter.value.trim();
  els.streamRows.innerHTML = `<tr><td colspan="4" class="empty">Loading…</td></tr>`;

  try {
    const query = new URLSearchParams({ kind: state.kind });
    if (q) query.set("q", q);

    const result = await api(`/api/streams?${query}`);
    const streams = result.streams ?? [];
    const kindMeta = state.kinds.find((k) => k.id === state.kind);
    els.browseMeta.textContent = kindMeta
      ? `${kindMeta.title} · ${result.total ?? streams.length} stream(s) · ${result.aggregateType} · ${kindMeta.metadataPrefix}`
      : `${result.total ?? streams.length} stream(s) · ${result.aggregateType}`;

    if (!streams.length) {
      els.streamRows.innerHTML = `<tr><td colspan="4" class="empty">No streams found.</td></tr>`;
      return;
    }

    els.streamRows.innerHTML = "";
    for (const stream of streams) {
      const tr = document.createElement("tr");
      if (stream.streamId === state.selectedStreamId) tr.classList.add("active");
      tr.innerHTML = `
        <td class="stream-id">${escapeHtml(stream.streamId)}</td>
        <td>${escapeHtml(stream.keyingHint ?? "—")}</td>
        <td>${stream.version}</td>
        <td>${formatTime(stream.updatedAtUtc)}</td>
      `;
      tr.addEventListener("click", () => openStream(stream.kind, stream.streamId));
      els.streamRows.appendChild(tr);
    }

    if (result.hasMore) {
      els.browseMeta.textContent += ` · truncated at ${streams.length}`;
    }
  } catch (error) {
    els.streamRows.innerHTML = `<tr><td colspan="4" class="empty">${escapeHtml(error.message)}</td></tr>`;
  }
}

async function openStream(kind, streamId) {
  state.selectedStreamId = streamId;
  state.kind = kind;
  document.querySelectorAll(".seg").forEach((btn) => {
    btn.classList.toggle("active", btn.dataset.kind === kind);
  });
  document.querySelectorAll("#stream-rows tr").forEach((tr) => {
    tr.classList.toggle("active", tr.querySelector(".stream-id")?.textContent === streamId);
  });

  els.eventsEmpty.hidden = true;
  els.eventsContent.hidden = false;
  els.copyStreamId.hidden = false;
  els.eventsSubtitle.textContent = "Loading events…";
  els.eventList.innerHTML = "";

  try {
    const detail = await api(`/api/streams/${encodeURIComponent(kind)}/${encodeURIComponent(streamId).replace(/%2F/gi, "/")}`);
    els.eventsSubtitle.textContent = `${detail.events.length} event(s) on ${detail.aggregateType}`;
    els.streamFacts.innerHTML = `
      <div><dt>Stream id</dt><dd>${escapeHtml(detail.streamId)}</dd></div>
      <div><dt>Aggregate</dt><dd>${escapeHtml(detail.aggregateType)}</dd></div>
      <div><dt>Metadata doc</dt><dd>${escapeHtml(detail.metadataDocumentId)}</dd></div>
      <div><dt>Event prefix</dt><dd>${escapeHtml(detail.eventDocumentPrefix)}</dd></div>
      <div><dt>Version</dt><dd>${detail.version}</dd></div>
      <div><dt>Updated</dt><dd>${formatTime(detail.updatedAtUtc)}</dd></div>
      <div><dt>Keying</dt><dd>${escapeHtml(detail.keyingHint ?? "—")}</dd></div>
      <div><dt>Applied ops</dt><dd>${detail.appliedOperationIds?.length ?? 0}</dd></div>
    `;

    if (!detail.events.length) {
      els.eventList.innerHTML = `<div class="empty-state">Stream exists but has no stored events.</div>`;
      return;
    }

    els.eventList.innerHTML = "";
    for (const event of detail.events) {
      const card = document.createElement("article");
      card.className = "event-card";
      const hintClass = event.projectionHint === "bulk-import" ? "bulk" : "live";
      let bodyPretty = event.bodyJson ?? "null";
      try {
        bodyPretty = JSON.stringify(JSON.parse(event.bodyJson), null, 2);
      } catch {
        // keep raw
      }
      card.innerHTML = `
        <header>
          <div>
            <span class="event-type">v${event.version} · ${escapeHtml(event.eventType)}</span>
            <span class="pill ${hintClass}">${escapeHtml(event.projectionHint)}</span>
          </div>
          <div class="event-meta">${formatTime(event.occurredAtUtc)}</div>
        </header>
        <div class="event-meta" style="margin-top:0.45rem">
          ${escapeHtml(event.id)}
          ${event.correlationId ? ` · corr ${escapeHtml(event.correlationId)}` : ""}
          ${event.causationId ? ` · cause ${escapeHtml(event.causationId)}` : ""}
        </div>
        <pre class="body-json">${escapeHtml(bodyPretty)}</pre>
      `;
      els.eventList.appendChild(card);
    }

    history.replaceState(null, "", `#/${kind}/${encodeURIComponent(streamId)}`);
  } catch (error) {
    els.eventsSubtitle.textContent = "Failed to load stream";
    els.eventList.innerHTML = `<div class="empty-state">${escapeHtml(error.message)}</div>`;
  }
}

function formatTime(value) {
  if (!value) return "—";
  try {
    return new Date(value).toLocaleString();
  } catch {
    return String(value);
  }
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function bindEvents() {
  els.templateSelect.addEventListener("change", renderTemplateFields);
  els.buildKey.addEventListener("click", buildKey);
  els.openKeyed.addEventListener("click", openKeyedStream);
  els.refreshStreams.addEventListener("click", loadStreams);
  els.streamFilter.addEventListener("keydown", (e) => {
    if (e.key === "Enter") loadStreams();
  });
  document.querySelectorAll(".seg").forEach((btn) => {
    btn.addEventListener("click", async () => {
      state.kind = btn.dataset.kind;
      document.querySelectorAll(".seg").forEach((b) => b.classList.toggle("active", b === btn));
      await loadStreams();
    });
  });
  els.copyStreamId.addEventListener("click", async () => {
    if (!state.selectedStreamId) return;
    await navigator.clipboard.writeText(state.selectedStreamId);
    toast("Copied stream id");
  });
}

async function boot() {
  bindEvents();
  const meta = await api("/api/meta");
  state.templates = meta.keyingTemplates;
  state.kinds = meta.streamKinds;

  els.templateSelect.innerHTML = state.templates
    .map((t) => `<option value="${t.id}">${t.title} (${t.kind})</option>`)
    .join("");
  renderTemplateFields();

  const hash = location.hash.replace(/^#\/?/, "");
  if (hash) {
    const slash = hash.indexOf("/");
    if (slash > 0) {
      const kind = hash.slice(0, slash);
      const streamId = decodeURIComponent(hash.slice(slash + 1));
      state.kind = kind;
      document.querySelectorAll(".seg").forEach((btn) => {
        btn.classList.toggle("active", btn.dataset.kind === kind);
      });
      await loadStreams();
      await openStream(kind, streamId);
      return;
    }
  }

  await loadStreams();
}

boot().catch((error) => toast(error.message));
