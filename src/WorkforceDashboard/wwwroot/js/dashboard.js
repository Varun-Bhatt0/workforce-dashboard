// Workforce Analytics dashboard: renders the charts and keeps the page in sync with the filters.
// The first view is rendered on the server; after that, filter changes fetch /api/dashboard
// and update the page in place (the URL is updated too, so every view can be bookmarked).
(function () {
  "use strict";

  const nf = new Intl.NumberFormat(undefined);
  const pf = new Intl.NumberFormat(undefined, { minimumFractionDigits: 1, maximumFractionDigits: 1 });
  const $ = (sel) => document.querySelector(sel);

  let data = JSON.parse($("#dashboard-data").textContent);
  let trendKey = "total";
  const charts = {};
  let pending = null;

  // ---------- theme ----------

  function css(name) {
    return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  }

  function palette() {
    return {
      series: [1, 2, 3, 4, 5].map((i) => css("--series-" + i)),
      text: css("--text"),
      text2: css("--text-2"),
      grid: css("--grid"),
      surface: css("--surface"),
    };
  }

  function setTheme(theme) {
    document.documentElement.setAttribute("data-theme", theme);
    try { localStorage.setItem("theme", theme); } catch (e) { /* storage unavailable: theme lasts for this page only */ }
    renderCharts();
  }

  // ---------- chart helpers ----------

  // Draws each bar's value at its end (horizontal bars) so the numbers can be read without hovering.
  const valueLabels = {
    id: "valueLabels",
    afterDatasetsDraw(chart, _args, opts) {
      if (!opts || !opts.enabled) return;
      const { ctx } = chart;
      ctx.save();
      ctx.font = "600 12px system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif";
      chart.data.datasets.forEach((ds, di) => {
        const meta = chart.getDatasetMeta(di);
        if (meta.hidden) return;
        meta.data.forEach((bar, i) => {
          const v = ds.data[i];
          if (v == null) return;
          const text = opts.format ? opts.format(v, i, ds, chart) : nf.format(v);
          if (!text) return;
          if (opts.inside) {
            const width = Math.abs(bar.x - bar.base);
            if (ctx.measureText(text).width + 12 > width) return;
            ctx.fillStyle = ds.labelColor || "#ffffff";
            ctx.textAlign = "center";
            ctx.textBaseline = "middle";
            ctx.fillText(text, (bar.x + bar.base) / 2, bar.y);
          } else {
            ctx.fillStyle = opts.color;
            ctx.textAlign = "left";
            ctx.textBaseline = "middle";
            ctx.fillText(text, bar.x + 6, bar.y);
          }
        });
      });
      ctx.restore();
    },
  };

  Chart.register(valueLabels);

  function baseOptions(p) {
    return {
      responsive: true,
      maintainAspectRatio: false,
      animation: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? false : { duration: 350 },
      interaction: { mode: "index", intersect: false },
      plugins: {
        legend: {
          position: "top",
          align: "start",
          labels: { color: p.text2, boxWidth: 12, boxHeight: 12, useBorderRadius: true, borderRadius: 3, padding: 14 },
        },
        tooltip: {
          backgroundColor: p.surface,
          titleColor: p.text,
          bodyColor: p.text,
          borderColor: p.grid,
          borderWidth: 1,
          padding: 10,
          boxPadding: 4,
          callbacks: { label: (c) => ` ${c.dataset.label}: ${nf.format(c.parsed.y ?? c.parsed.x)}` },
        },
        valueLabels: { enabled: false },
      },
      scales: {
        x: { grid: { display: false }, border: { color: p.grid }, ticks: { color: p.text2 } },
        y: {
          grid: { color: p.grid },
          border: { display: false },
          ticks: { color: p.text2, callback: (v) => nf.format(v) },
          beginAtZero: true,
        },
      },
    };
  }

  function horizontalOptions(p, labelColor) {
    const o = baseOptions(p);
    o.indexAxis = "y";
    o.interaction = { mode: "nearest", axis: "y", intersect: true };
    o.plugins.legend.display = false;
    o.plugins.tooltip.callbacks = { label: (c) => ` ${nf.format(c.parsed.x)}` };
    o.plugins.valueLabels = { enabled: true, color: labelColor };
    o.layout = { padding: { right: 56 } };
    o.scales = {
      x: { grid: { color: p.grid }, border: { display: false }, ticks: { color: p.text2, callback: (v) => nf.format(v), maxTicksLimit: 5 }, beginAtZero: true },
      y: { grid: { display: false }, border: { color: p.grid }, ticks: { color: p.text } },
    };
    return o;
  }

  function draw(id, config) {
    if (charts[id]) charts[id].destroy();
    charts[id] = new Chart(document.getElementById(id), config);
  }

  function bar(label, values, color, p, extra) {
    return Object.assign({
      label, data: values, backgroundColor: color, borderColor: p.surface, borderWidth: 0,
      borderRadius: 4, borderSkipped: "start", maxBarThickness: 34,
    }, extra || {});
  }

  // ---------- charts ----------

  function renderTrend(p) {
    const t = data.trends[trendKey];
    const labels = t.years.map(String);
    let config;
    if (trendKey === "total") {
      config = {
        type: "line",
        data: {
          labels,
          datasets: [{
            label: "Headcount", data: t.series[0].values, borderColor: p.series[0], backgroundColor: p.series[0],
            borderWidth: 2, pointRadius: 4, pointHoverRadius: 6, pointBackgroundColor: p.series[0],
            pointBorderColor: p.surface, pointBorderWidth: 2, tension: 0.25,
          }],
        },
        options: baseOptions(p),
      };
      config.options.plugins.legend.display = false;
      config.options.scales.y.beginAtZero = false;
    } else {
      const last = t.series.length - 1;
      config = {
        type: "bar",
        data: {
          labels,
          datasets: t.series.map((s, i) => bar(s.name, s.values, p.series[i % 5], p, {
            stack: "s", borderWidth: { top: i === last ? 0 : 2 }, borderColor: p.surface,
            borderRadius: i === last ? { topLeft: 4, topRight: 4 } : 0, borderSkipped: false,
          })),
        },
        options: baseOptions(p),
      };
      config.options.scales.x.stacked = true;
      config.options.scales.y.stacked = true;
      config.options.plugins.tooltip.callbacks.footer = (items) =>
        "Total: " + nf.format(items.reduce((sum, it) => sum + it.parsed.y, 0));
    }
    $("#trend-title").textContent = t.title;
    $("#chart-trend").setAttribute("aria-label", t.title + ", " + labels[0] + " to " + labels[labels.length - 1]);
    draw("chart-trend", config);
  }

  function renderFlows(p) {
    const t = data.trends.flows;
    const config = {
      type: "bar",
      data: {
        labels: t.years.map(String),
        datasets: t.series.map((s, i) => bar(s.name, s.values, p.series[i], p, { maxBarThickness: 18 })),
      },
      options: baseOptions(p),
    };
    config.options.datasets = { bar: { categoryPercentage: 0.7, barPercentage: 0.9 } };
    draw("chart-flows", config);
  }

  function renderBreakdown(id, b, p) {
    draw(id, {
      type: "bar",
      data: { labels: b.labels, datasets: [bar(b.title, b.values, p.series[0], p, { maxBarThickness: 22 })] },
      options: horizontalOptions(p, p.text),
    });
  }

  function renderPeople(p) {
    const cadre = data.breakdowns.cadre;
    const gender = data.breakdowns.gender;
    const share = (values, i) => {
      const total = values.reduce((a, b) => a + b, 0);
      return total ? (100 * values[i]) / total : 0;
    };
    // White text only on the blue slot; the lighter hues take dark text so the label stays readable.
    const ds = (label, row, value, pct, color) => bar(label, row === 0 ? [value, null] : [null, value], color, p, {
      stack: "s", borderRadius: 0, borderSkipped: false, borderWidth: { right: 2 }, borderColor: p.surface, maxBarThickness: 40, pct,
      labelColor: color === p.series[0] ? "#ffffff" : "#191919",
    });
    const o = horizontalOptions(p, p.text);
    o.plugins.legend.display = true;
    o.plugins.tooltip.callbacks = { label: (c) => ` ${c.dataset.label}: ${nf.format(c.parsed.x)} (${pf.format(c.dataset.pct)}%)` };
    o.plugins.valueLabels = { enabled: true, inside: true, format: (_v, _i, d) => pf.format(d.pct) + "%" };
    o.layout = { padding: { right: 8 } };
    o.scales.x.stacked = true;
    o.scales.y.stacked = true;
    o.scales.x.display = false;
    draw("chart-people", {
      type: "bar",
      data: {
        labels: ["Cadre", "Gender"],
        datasets: [
          ds("Executives", 0, cadre.values[0], share(cadre.values, 0), p.series[0]),
          ds("Non-executives", 0, cadre.values[1], share(cadre.values, 1), p.series[1]),
          ds("Men", 1, gender.values[0], share(gender.values, 0), p.series[2]),
          ds("Women", 1, gender.values[1], share(gender.values, 1), p.series[3]),
        ],
      },
      options: o,
    });
  }

  function renderUnits(p) {
    const card = $("#card-units");
    const u = data.unitComparison;
    card.hidden = u.labels.length === 0;
    if (card.hidden) {
      if (charts["chart-units"]) { charts["chart-units"].destroy(); delete charts["chart-units"]; }
      return;
    }
    card.querySelector(".chart").style.height = Math.max(160, 30 * u.labels.length + 40) + "px";
    renderBreakdown("chart-units", u, p);
  }

  function renderCharts() {
    const p = palette();
    Chart.defaults.font.family = "system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif";
    renderTrend(p);
    renderFlows(p);
    renderBreakdown("chart-function", data.breakdowns.function, p);
    renderBreakdown("chart-qualification", data.breakdowns.qualification, p);
    renderPeople(p);
    renderUnits(p);
    renderTables();
  }

  // ---------- data tables (for screen readers and for exact numbers) ----------

  function table(head, rows) {
    const t = document.createElement("table");
    const thead = t.createTHead().insertRow();
    head.forEach((h) => { const th = document.createElement("th"); th.scope = "col"; th.textContent = h; thead.appendChild(th); });
    const body = t.createTBody();
    rows.forEach((r) => {
      const tr = body.insertRow();
      r.forEach((cell, i) => {
        const c = i === 0 ? document.createElement("th") : document.createElement("td");
        if (i === 0) c.scope = "row";
        c.textContent = typeof cell === "number" ? nf.format(cell) : cell;
        tr.appendChild(c);
      });
    });
    return t;
  }

  function trendTable(t) {
    return table(["Year"].concat(t.series.map((s) => s.name)), t.years.map((y, i) => [String(y)].concat(t.series.map((s) => s.values[i]))));
  }

  function breakdownTable(b) {
    const total = b.values.reduce((a, v) => a + v, 0);
    return table(["", "Headcount", "Share"], b.labels.map((l, i) => [l, b.values[i], total ? pf.format((100 * b.values[i]) / total) + "%" : "–"]));
  }

  function renderTables() {
    const fill = (key, el) => { const w = document.querySelector(`.data-table[data-for="${key}"] .table-wrap`); w.replaceChildren(el); };
    fill("trend", trendTable(data.trends[trendKey]));
    fill("flows", trendTable(data.trends.flows));
    fill("function", breakdownTable(data.breakdowns.function));
    fill("qualification", breakdownTable(data.breakdowns.qualification));
    const people = document.createElement("div");
    people.append(breakdownTable(data.breakdowns.cadre), breakdownTable(data.breakdowns.gender));
    fill("people", people);
    if (data.unitComparison.labels.length) fill("units", breakdownTable(data.unitComparison));
  }

  // ---------- text, KPIs and navigation ----------

  function renderText(keepNotices) {
    const k = data.kpis;
    document.title = data.scopeLabel + " · Workforce Analytics";
    $("#scope-label").textContent = data.scopeLabel;
    $("#scope-detail").textContent = data.scopeDetail;
    $("#as-of").textContent = "1 April " + data.year;
    $("#cadre-label").textContent = data.cadre === "All" ? "all cadres" : data.cadre + "s";
    $("#scope-input").value = data.scope;

    $("#kpi-manpower").textContent = nf.format(k.manpower);
    const change = $("#kpi-change");
    change.replaceChildren();
    if (k.changeFromPreviousYear == null) {
      change.textContent = "First year in the data";
    } else {
      const span = document.createElement("span");
      const down = k.changeFromPreviousYear < 0;
      span.className = "delta " + (down ? "down" : "up");
      span.textContent = `${down ? "▼" : "▲"} ${nf.format(Math.abs(k.changeFromPreviousYear))} (${pf.format(k.changePercent ?? 0)}%)`;
      change.append(span, ` vs ${data.year - 1}`);
    }
    $("#kpi-works").textContent = pf.format(k.worksShare) + "%";
    $("#kpi-age").textContent = pf.format(k.averageAge);
    $("#kpi-female").textContent = pf.format(k.femaleShare) + "%";
    $("#kpi-attrition").textContent = pf.format(k.attritionRate) + "%";
    $("#kpi-flows").textContent = `${nf.format(k.recruitments)} joined · ${nf.format(k.separations)} left`;
    $("#data-source").textContent = data.dataSource;
    $("#sample-note").hidden = !data.isSample;
    $("#sidebar-note").hidden = !data.isSample;

    if (!keepNotices) {
      $("#notices").replaceChildren();
      if (data.notice) addNotice(data.notice);
    }

    document.querySelectorAll("#unit-nav [data-scope]").forEach((a) => {
      const active = a.dataset.scope.toLowerCase() === data.scope.toLowerCase();
      a.classList.toggle("active", active);
      if (active) a.setAttribute("aria-current", "page"); else a.removeAttribute("aria-current");
      const url = new URL(a.href, location.href);
      url.searchParams.set("year", data.year);
      url.searchParams.set("cadre", cadreValue());
      a.href = url.pathname + url.search;
    });
  }

  function addNotice(text) {
    const div = document.createElement("div");
    div.className = "notice notice-warning";
    div.setAttribute("role", "status");
    div.textContent = text;
    $("#notices").appendChild(div);
  }

  function cadreValue() {
    const checked = document.querySelector('input[name="cadre"]:checked');
    return checked ? checked.value : "all";
  }

  async function load(params, push) {
    const query = new URLSearchParams({ scope: params.scope, year: params.year, cadre: params.cadre });
    if (pending) pending.abort();
    pending = new AbortController();
    $("#main").classList.add("loading");
    try {
      const res = await fetch("/api/dashboard?" + query, { signal: pending.signal, headers: { Accept: "application/json" } });
      if (!res.ok) {
        const problem = await res.json().catch(() => null);
        throw new Error((problem && problem.detail) || "The dashboard could not load that view (" + res.status + ").");
      }
      data = await res.json();
      if (push) history.pushState(null, "", "/?" + query);
      renderText();
      renderCharts();
    } catch (err) {
      if (err.name !== "AbortError") addNotice(err.message);
    } finally {
      $("#main").classList.remove("loading");
    }
  }

  function current() {
    return { scope: data.scope, year: $("#year-select").value, cadre: cadreValue() };
  }

  function init() {
    $("#unit-nav").addEventListener("click", (e) => {
      const a = e.target.closest("a[data-scope]");
      if (!a || e.metaKey || e.ctrlKey || e.shiftKey || e.button !== 0) return;
      e.preventDefault();
      load(Object.assign(current(), { scope: a.dataset.scope }), true);
    });
    $("#year-select").addEventListener("change", () => load(current(), true));
    document.querySelectorAll('input[name="cadre"]').forEach((r) => r.addEventListener("change", () => load(current(), true)));
    $("#filters").addEventListener("submit", (e) => { e.preventDefault(); load(current(), true); });
    $("#trend-select").addEventListener("change", (e) => { trendKey = e.target.value; renderTrend(palette()); renderTables(); });
    $("#theme-toggle").addEventListener("click", () =>
      setTheme(document.documentElement.getAttribute("data-theme") === "dark" ? "light" : "dark"));

    window.addEventListener("popstate", () => {
      const q = new URLSearchParams(location.search);
      const params = { scope: q.get("scope") || "all", year: q.get("year") || "", cadre: q.get("cadre") || "all" };
      $("#year-select").value = params.year || $("#year-select").options[0].value;
      const radio = document.getElementById("cadre-" + params.cadre);
      if (radio) radio.checked = true;
      load(params, false);
    });

    renderText(true);
    renderCharts();
  }

  init();
})();
