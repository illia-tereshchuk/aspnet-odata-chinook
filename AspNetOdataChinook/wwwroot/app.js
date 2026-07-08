'use strict';

// Base of the OData service exposed by the ASP.NET backend (same origin).
const BASE = '/odata';

// A tiny hand-written description of each entity set: which scalar fields it has,
// which one reads well as a text search target, and which navigation properties
// can be $expand-ed. This drives the whole control panel — nothing here is OData
// specific, it just tells the UI what checkboxes to render.
const SCHEMA = {
  Artists: {
    fields: ['ArtistId', 'Name'],
    search: 'Name',
    expand: ['Albums'],
  },
  Albums: {
    fields: ['AlbumId', 'Title', 'ArtistId'],
    search: 'Title',
    expand: ['Artist', 'Tracks'],
  },
  Tracks: {
    fields: ['TrackId', 'Name', 'Composer', 'Milliseconds', 'UnitPrice', 'AlbumId', 'GenreId'],
    search: 'Name',
    expand: ['Album', 'Genre', 'MediaType'],
  },
  Genres: {
    fields: ['GenreId', 'Name'],
    search: 'Name',
    expand: ['Tracks'],
  },
  Customers: {
    fields: ['CustomerId', 'FirstName', 'LastName', 'Company', 'City', 'Country', 'Email'],
    search: 'LastName',
    expand: ['Invoices'],
  },
  Invoices: {
    fields: ['InvoiceId', 'CustomerId', 'InvoiceDate', 'BillingCity', 'BillingCountry', 'Total'],
    search: 'BillingCountry',
    expand: ['Customer', 'Lines'],
  },
  InvoiceLines: {
    fields: ['InvoiceLineId', 'InvoiceId', 'TrackId', 'UnitPrice', 'Quantity'],
    search: null,
    expand: ['Track', 'Invoice'],
  },
};

// Analytics presets showcasing $apply (group + aggregate pushed into SQL).
const PRESETS = [
  {
    title: 'Tracks per genre',
    desc: 'Count how many tracks each genre has.',
    url: `Tracks?$apply=groupby((Genre/Name),aggregate($count as Tracks))&$orderby=Tracks desc`,
  },
  {
    title: 'Revenue by country',
    desc: 'Sum invoice totals grouped by billing country.',
    url: `Invoices?$apply=groupby((BillingCountry),aggregate(Total with sum as Revenue))&$orderby=Revenue desc`,
  },
  {
    title: 'Average track length by genre',
    desc: 'Mean duration (ms) per genre.',
    url: `Tracks?$apply=groupby((Genre/Name),aggregate(Milliseconds with average as AvgMs))&$orderby=AvgMs desc`,
  },
  {
    title: 'Best-selling tracks',
    desc: 'Total quantity sold per track id.',
    url: `InvoiceLines?$apply=groupby((TrackId),aggregate(Quantity with sum as Sold))&$orderby=Sold desc&$top=15`,
  },
];

// ---- Explore tab state ---------------------------------------------------

const state = {
  entity: 'Artists',  //  /odata/Artists
  select: new Set(),  //  $select=Name
  expand: new Set(),  //  $expand=Albums
  orderby: '',        //  $orderby=Name asc
  dir: 'asc',         //  ^
  top: 10,            //  $top=10
  skip: 0,            //  $skip=0
  count: true,        //  $count=true
};

const $ = (id) => document.getElementById(id);

function buildUrl() {
  const s = SCHEMA[state.entity]; // SCHEMA['Artists']
  const q = [];

  const term = $('search').value.trim(); // user input

  if (term && s.search) { // s.search = 'Name'
    const termEscaped = term.replace(/'/g, "''");
    q.push(`$filter=contains(${s.search},'${termEscaped}')`); // $filter=contains(Name, 'Dzhigurda')
  }

  if (state.select.size) q.push(`$select=${[...state.select].join(',')}`); // $select=Name,ArtistId
  if (state.expand.size) q.push(`$expand=${[...state.expand].join(',')}`);
  if (state.orderby) q.push(`$orderby=${state.orderby} ${state.dir}`);

  q.push(`$top=${state.top}`);

  if (state.skip) q.push(`$skip=${state.skip}`);
  if (state.count) q.push(`$count=true`);

  // /odata/Artists?$filter=contains(Name,'rock')&$select=Name&$orderby=Name asc&$top=10&$count=true
  return `${BASE}/${state.entity}?${q.join('&')}`;
}

function syncQueryBox() {
  $('query').value = decodeURIComponent(buildUrl());
}

// Render the checkbox "chips" for $select and $expand when the entity changes.
function renderControls() {
  const s = SCHEMA[state.entity];

  // $select chips
  $('selectFields').innerHTML = s.fields.map((f) => chip('sel', f)).join('');

  // $expand chips
  $('expandFields').innerHTML = s.expand.length
    ? s.expand.map((f) => chip('exp', f)).join('')
    : '<span class="text-secondary small">no relations</span>';

  // $orderby options
  $('orderby').innerHTML = '<option value="">— none —</option>' +
    s.fields.map((f) => `<option>${f}</option>`).join('');

  // disable search for entities that have no text field to search
  $('search').disabled = !s.search;

  wireChips();
}

function chip(kind, field) {
  const id = `${kind}-${field}`;
  return `<div class="form-check chip-check">
    <input class="form-check-input" type="checkbox" id="${id}" data-kind="${kind}" data-field="${field}">
    <label class="form-check-label small" for="${id}">${field}</label>
  </div>`;
}

function wireChips() { // add event listeners to newly created chechboxes
  document.querySelectorAll('.chip-check input').forEach((cb) => {
    cb.addEventListener('change', () => {
      const set = cb.dataset.kind === 'sel' ? state.select : state.expand;
      cb.checked ? set.add(cb.dataset.field) : set.delete(cb.dataset.field);
      state.skip = 0;
      syncQueryBox();
    });
  });
}

// ---- Fetch + render ------------------------------------------------------

async function run(url) {
  const target = url || $('query').value.trim(); // URL or textarea content

  $('resultMeta').textContent = '· loading…';

  explain(target, 'sql'); // fetch the SQL alongside the data, no await - no standing

  try {
    const res = await fetch(target); // "res" is response but WITHOUT body
    const data = await res.json();

    if (!res.ok) // res.ok is "true" for HTTP 200-299
      throw new Error(data.error?.message || res.statusText);

    renderResults(data);
  } catch (e) {
    $('results').innerHTML = `<div class="alert alert-danger mb-0">${e.message}</div>`;
    $('resultMeta').textContent = '· error';
  }
}

// Ask the /explain endpoint what SQL this same query becomes. Runs in parallel
// with the data request; failures are shown in-place, never fatal.
async function explain(queryUrl, sqlId) {
  const url = queryUrl.replace('/odata/', '/explain/');
  $(sqlId).textContent = '…';
  try {
    const res = await fetch(url);
    const data = await res.json();
    if (!res.ok) throw new Error(data.error || res.statusText);
    $(sqlId).textContent = data.sql || '(no SQL generated)';
  } catch (e) {
    $(sqlId).textContent = '-- explain failed: ' + e.message;
  }
}

function renderResults(data) { // goes to "table" next
  const rows = data.value || [];
  const count = data['@odata.count'];
  $('resultMeta').textContent =
    `· ${rows.length} shown` + (count != null ? ` of ${count}` : '');
  $('jsonView').textContent = JSON.stringify(data, null, 2);
  $('results').innerHTML = rows.length ? table(rows) : '<em class="text-secondary">no rows</em>';
  renderPager(count);
}

// Turn an array of (possibly nested) objects into a Bootstrap table. Nested
// objects/arrays from $expand are shown as a compact summary so the shape of
// the data stays visible without exploding the table.
function table(rows) {
  // Object.keys(r) -> ['ArtistId', 'Name']
  // ".map" would give arrays of arrays, ".flatMap" flattens them
  // new Set - removes duplicates
  const cols = [...new Set(rows.flatMap((r) => Object.keys(r)))]
    .filter((c) => !c.startsWith('@odata'));

  const head = cols.map((c) => `<th>${c}</th>`).join('');

  const body = rows.map((r) => {
    const tds = cols.map((c) => `<td>${cell(r[c])}</td>`).join(''); // dynamic access
    return `<tr>${tds}</tr>`;
  }).join('');

  return `<table class="table table-sm table-striped table-hover align-middle">
    <thead><tr>${head}</tr></thead><tbody>${body}</tbody></table>`;
}

function cell(v) {
  // NOTE: order of "null - array - object - scalar" is specific to JS
  
  if (v == null) return '<span class="text-secondary">null</span>';

  if (Array.isArray(v)) {
    return `<span class="badge text-bg-info">${v.length} ↳</span> ` +
      `<span class="text-secondary small">
        ${v.map(summ).slice(0, 3).join(', ')}${v.length > 3 ? '…' : ''}
      </span>`;
  }

  if (typeof v === 'object') return `<span class="text-info small">${summ(v)}</span>`;

  return String(v);
}

// A short label for a nested entity: prefer Name/Title, else first scalar.
function summ(o) {
  if (o == null || typeof o !== 'object') return String(o);

  const key = ['Name', 'Title'].find((k) => o[k] != null)
    || Object.keys(o).find((k) => !k.startsWith('@') && typeof o[k] !== 'object');

  return key ? `${o[key]}` : '{…}';
}

function renderPager(count) {
  const canPrev = state.skip > 0;
  const canNext = count != null ? state.skip + state.top < count : true;
  $('pager').innerHTML = `
    <div class="btn-group btn-group-sm">
      <button class="btn btn-outline-secondary" ${canPrev ? '' : 'disabled'} id="prev">← Prev</button>
      <button class="btn btn-outline-secondary" disabled>skip ${state.skip}</button>
      <button class="btn btn-outline-secondary" ${canNext ? '' : 'disabled'} id="next">Next →</button>
    </div>`;
  if (canPrev) $('prev').onclick = () => { state.skip = Math.max(0, state.skip - state.top); syncQueryBox(); run(); };
  if (canNext) $('next').onclick = () => { state.skip += state.top; syncQueryBox(); run(); };
}

// ---- Analytics tab -------------------------------------------------------

function renderPresets() {
  $('presets').innerHTML = PRESETS.map((p, i) =>
    `<button class="list-group-item list-group-item-action" data-i="${i}">
       <div class="fw-semibold">${p.title}</div>
       <div class="small text-secondary">${p.desc}</div>
     </button>`).join('');
  document.querySelectorAll('#presets button').forEach((b) => {
    b.onclick = () => {
      const p = PRESETS[b.dataset.i];
      $('query2').value = `${BASE}/${p.url}`;
      runAnalytics();
    };
  });
}

async function runAnalytics() {
  const target = $('query2').value.trim();
  $('resultMeta2').textContent = '· loading…';
  explain(target, 'sql2');
  try {
    const res = await fetch(target);
    const data = await res.json();
    if (!res.ok) throw new Error(data.error?.message || res.statusText);
    const rows = data.value || [];
    $('resultMeta2').textContent = `· ${rows.length} groups`;
    $('results2').innerHTML = rows.length ? table(rows) : '<em>no rows</em>';
  } catch (e) {
    $('results2').innerHTML = `<div class="alert alert-danger mb-0">${e.message}</div>`;
    $('resultMeta2').textContent = '· error';
  }
}

// ---- Wiring --------------------------------------------------------------

function init() {
  // entity dropdown — force the value so a browser-restored selection can't
  // drift out of sync with state.entity after a reload.
  $('entity').innerHTML = Object.keys(SCHEMA).map((e) => `<option>${e}</option>`).join('');

  $('entity').value = state.entity; // 'Artists'

  $('entity').onchange = () => {
    state.entity = $('entity').value;
    state.select.clear(); state.expand.clear();
    state.orderby = ''; state.skip = 0;
    $('search').value = '';
    renderControls();
    syncQueryBox();
    run();
  };
  $('search').oninput = () => { state.skip = 0; syncQueryBox(); };

  $('orderby').onchange = () => { state.orderby = $('orderby').value; syncQueryBox(); };

  $('orderdir').onclick = () => {
    state.dir = state.dir === 'asc' ? 'desc' : 'asc'; // flip
    $('orderdir').dataset.dir = state.dir;
    $('orderdir').textContent = state.dir === 'asc' ? 'asc ↑' : 'desc ↓';
    syncQueryBox();
  };
  $('top').onchange = () => { state.top = +$('top').value; state.skip = 0; syncQueryBox(); };
  $('count').onchange = () => { state.count = $('count').checked; syncQueryBox(); };

  $('runBtn').onclick = () => run();
  $('runBtn2').onclick = () => runAnalytics();
  
  $('copyBtn').onclick = async () => {
    await navigator.clipboard.writeText(new URL($('query').value, location.origin).href);
    $('copyBtn').textContent = 'Copied!';
    setTimeout(() => ($('copyBtn').textContent = 'Copy URL'), 1200);
  };

  // JSON / table view toggle
  $('viewTable').onclick = () => toggleView(true);
  $('viewJson').onclick = () => toggleView(false);

  // tabs
  document.querySelectorAll('#tabs [data-tab]').forEach((b) => {
    b.onclick = () => {
      document.querySelectorAll('#tabs .nav-link').forEach((x) => x.classList.remove('active'));
      b.classList.add('active');
      $('tab-explore').classList.toggle('d-none', b.dataset.tab !== 'explore');
      $('tab-analytics').classList.toggle('d-none', b.dataset.tab !== 'analytics');
    };
  });

  renderControls();
  renderPresets();
  syncQueryBox();
  run();
}

function toggleView(showTable) {
  $('results').classList.toggle('d-none', !showTable);
  $('jsonView').classList.toggle('d-none', showTable);
  $('viewTable').classList.toggle('active', showTable);
  $('viewJson').classList.toggle('active', !showTable);
}

document.addEventListener('DOMContentLoaded', init);
