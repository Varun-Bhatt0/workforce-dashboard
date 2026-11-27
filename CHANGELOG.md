# Changelog

## 2.0.0 — 2026-11

Rewritten from the 2025 prototype.

- ASP.NET Core 10 MVC app replacing the Web Forms page and the separate MVC mock-up.
- Full chart set: headcount trend (total or by cadre, function, gender, qualification), recruitments vs separations,
  function, qualification, cadre and gender mix, and a unit comparison.
- Five headline figures: headcount and yearly change, works share, weighted average age, share of women, attrition rate.
- Filters for unit, unit group, year and cadre, with shareable URLs and working back/forward navigation.
- Light and dark themes, mobile layout, and a data table under every chart.
- Oracle repository with pooled per-request connections, caching, and a fallback to sample data with a retry back-off.
- New Oracle schema: `DATE` snapshot column, primary/foreign keys, and check constraints that the breakdowns add up.
- Synthetic sample dataset (14 units, 2019–2024) and the generator that produces it, plus matching Oracle seed SQL.
- Removed placeholders: the fixed "LP" figure, the hard-coded trend line and the month selector.
- Security headers with a strict Content Security Policy, input validation, API rate limiting, JSON logging and a `/health` endpoint.
- 42 xUnit tests, GitHub Actions CI and Dependabot.

## 1.0.0 — 2025-07

First prototype: a Web Forms page reading headcount from Oracle, and an MVC mock-up of the full layout.
