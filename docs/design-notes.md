# Design notes

## Where the design came from

The dashboard implements a two-slide layout brief for an HR headcount dashboard:

1. **Overview**: a sidebar listing every plant and unit (plus groupings), a header naming the selection, month and
   year selectors, three headline figures (headcount, "LP", works/non-works ratio), category tabs (cadre, function,
   grade, gender, qualification, works/non-works, technical/non-technical, contract labour) and a large headcount trend chart.
2. **Historical trends**: a date range, a unit picker, an executives / non-executives / total switch, and a trend
   chart for any of function, gender, qualification, contract labour, separations, recruitments, labour productivity
   or average age.

The data came as one spreadsheet: a row per unit, cadre and snapshot date, with the twenty columns now in
`WorkforceSnapshot`.

## How the brief maps to this version

| Brief | This version |
|---|---|
| Sidebar of units and groupings | Sidebar with "All units", three groups and every unit; groups are selectable |
| Header naming the selection | Title, group/unit count and snapshot date |
| Year selector | Year dropdown (latest by default) |
| Month selector | Dropped: the data has one snapshot per year (1 April), so a month filter would change nothing |
| Executives / non-executives / total | Cadre switch, applied to every figure and chart |
| Headcount headline | Headcount with change from the previous year |
| Works/non-works ratio | Works share, plus a full function breakdown chart |
| Cadre, function, gender, qualification | One chart each for the selected year, and each available as a trend |
| Recruitments, separations | Recruitments vs separations chart, and attrition rate as a headline figure |
| Average age | Headline figure, weighted by headcount |
| Grade-wise, technical/non-technical, contract labour | Not included: the data has no such columns |
| "LP" (labour productivity) | Not included: it needs production output, which isn't in the data. An earlier prototype showed 85% of headcount as a placeholder; this version doesn't show numbers it can't calculate |

## What changed from the first prototype

The 2025 prototype had two versions:

- **A Web Forms page connected to Oracle.** It showed total headcount, works/non-works and a headcount trend line.
  The snapshot date was stored as text (`dd.mm.yyyy`) and the year was extracted with `SUBSTR`. One Oracle
  connection was held as a page field and reused across queries, and errors were written into the page.
- **An MVC mock-up** with the full chart set, but six hard-coded rows, a made-up trend line and a fixed 85% "LP" figure.

This version keeps the Oracle design and the full chart set, and fixes the problems:

- **Dates**: a real `DATE` column, indexed, compared as a date.
- **Connections**: opened per request, pooled and disposed.
- **Errors**: logged, never shown to users; if Oracle is down, the page shows the sample data with a notice.
- **Data quality**: check constraints in the schema, and tests that the breakdowns add up.
- **Calculations**: weighted average age and a proper attrition rate instead of placeholders, all in one tested class.
- **Placeholders removed**: every number on screen is calculated from the data.
- **Platform**: ASP.NET Core 10 instead of .NET Framework Web Forms, so it runs on Windows, Linux and macOS.

## Chart choices

- Headline numbers are **tiles**, not charts.
- Breakdowns for one year are **horizontal bars with the value printed at the end**, so labels never need rotating
  and exact numbers are readable without hovering.
- Composition over time uses **stacked bars** (one bar per year), which stays readable with five series and six years.
- Cadre and gender mix is one **100% bar per dimension**, labelled with percentages.
- Colours come from a fixed, ordered categorical palette (blue, orange, aqua, yellow, magenta) with separate light
  and dark steps. Both were checked for colour-blind separation. Colour always follows the category (Executives are
  always blue), and every chart has a legend or direct labels plus a data table.
