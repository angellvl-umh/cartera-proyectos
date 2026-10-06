# project-list-promoter-filter Specification

## Purpose
TBD - created by archiving change filtro-promotor. Update Purpose after archive.

## Requirements

### Requirement: Filter project list API by promoter
`GET /api/projects` MUST accept an optional integer query parameter `promoterId` and, when present, MUST return only projects whose `PromoterId` equals that value. The filter MUST combine with every other supported filter (`status`, `year`, `teamId`, `complexity`, `q`, `tagId`, `tagIds`) using logical AND, and `Total` MUST reflect the filtered count before pagination.

#### Scenario: Filter by an existing promoter
- **WHEN** an authenticated client calls `GET /api/projects?promoterId=3` and projects 10 and 11 have promoter 3 while project 12 has promoter 4
- **THEN** the response `Items` contains projects 10 and 11 only and `Total` is 2

#### Scenario: Promoter without projects or unknown id
- **WHEN** a client calls `GET /api/projects?promoterId=999` and no project has promoter 999 (whether or not that promoter exists)
- **THEN** the system responds 200 with empty `Items` and `Total = 0`

#### Scenario: Projects without promoter are excluded
- **WHEN** a client filters by `promoterId=3` and some projects have no promoter (`PromoterId` null)
- **THEN** those projects MUST NOT appear in the result

#### Scenario: Combined with other filters
- **WHEN** a client calls `GET /api/projects?promoterId=3&status=InSprint&q=portal`
- **THEN** only projects matching promoter 3 AND status InSprint AND the text "portal" are returned

#### Scenario: No promoter filter
- **WHEN** a client calls `GET /api/projects` without `promoterId`
- **THEN** the result is the same as before this change (backwards compatible)

#### Scenario: Pagination with filter
- **WHEN** a client calls `GET /api/projects?promoterId=3&page=2&pageSize=1` and promoter 3 has 2 projects
- **THEN** `Items` contains exactly one project, `Total` is 2 and `Page` is 2

### Requirement: Portfolio exposes project promoter
Each project in the `GET /api/portfolio` response MUST include a nullable `promoterId` field with the project's promoter id, or `null` when the project has no promoter. Existing fields MUST NOT change.

#### Scenario: Project with promoter
- **WHEN** a client calls `GET /api/portfolio` and project 10 has promoter 3
- **THEN** the entry for project 10 includes `promoterId: 3`

#### Scenario: Project without promoter
- **WHEN** a project has no promoter
- **THEN** its entry includes `promoterId: null`

### Requirement: Promoter filter in the projects list UI
The projects list page (`/projects`) MUST show a "Promotor" single-select in the filter toolbar, listing all promoters by name, searchable by text and clearable. Changing it MUST filter the visible projects in both the "Tabla" and "Tablero" views, combined with the other active filters.

#### Scenario: Select a promoter in table view
- **WHEN** the user selects a promoter in the "Promotor" filter while in "Tabla" view on page 3
- **THEN** the table reloads showing only that promoter's projects, starting at page 1, and the subtitle count reflects the filtered total

#### Scenario: Clear the promoter filter
- **WHEN** the user clears the "Promotor" filter
- **THEN** the list shows projects of all promoters (still applying the other active filters), starting at page 1

#### Scenario: Board view honours the promoter filter
- **WHEN** a promoter is selected and the user switches to "Tablero"
- **THEN** each status column shows only projects of that promoter that also match the text and complexity filters

#### Scenario: Promoter with no matching projects
- **WHEN** the selected promoter has no projects matching the active filters
- **THEN** the table shows its empty state and the board shows empty columns, without errors

#### Scenario: Search inside the selector
- **WHEN** the user types part of a promoter name in the selector (e.g. "vice")
- **THEN** the options narrow to promoters whose name contains that text, case-insensitively

#### Scenario: Promoter catalog fails to load
- **WHEN** loading the promoter list fails
- **THEN** the projects list still loads and works with the other filters, and the "Promotor" selector shows no options

### Requirement: Project list API exposes organic unit
Each item of `GET /api/projects` MUST include nullable `organicUnitId` and `organicUnitName` fields with the project's organic unit, or `null` when it has none. Existing fields MUST NOT change.

#### Scenario: Project with organic unit
- **WHEN** a client calls `GET /api/projects` and project 10 belongs to organic unit 5 named "Servicio de Informática"
- **THEN** the item for project 10 includes `organicUnitId: 5` and `organicUnitName: "Servicio de Informática"`

#### Scenario: Project without organic unit
- **WHEN** a project has no organic unit
- **THEN** its item includes `organicUnitId: null` and `organicUnitName: null`

### Requirement: Promoter and organic unit columns in the projects table
The "Tabla" view of `/projects` MUST show a "Promotor" column with the project's promoter name and an "Unidad orgánica" column with its organic unit name. When the project has none, the cell MUST show "—".

#### Scenario: Project with promoter and organic unit
- **WHEN** a project row has promoter "Vicerrectorado de Estudios" and organic unit "Servicio de Informática"
- **THEN** the row shows those names in the "Promotor" and "Unidad orgánica" columns

#### Scenario: Project without promoter or organic unit
- **WHEN** a project has no promoter and no organic unit
- **THEN** both cells show "—"

### Requirement: Projects list state in the URL
The projects list page MUST reflect its filter state in the URL query string and MUST restore that state from the URL on load. The parameters are `q` (text), `status`, `complexity`, `tagIds` (repeated, one per tag), `promoterId`, `page`, `pageSize` (10, 20, 50 or 100), `sortBy` and `sortDir` (same values as the API) and `view` (`tabla` or `tablero`). Parameters with an empty or default value (no filter, page 1, page size 20, no sort, view `tabla`) MUST be omitted from the URL; `sortDir` MUST be omitted when `sortBy` is absent. Updating the URL MUST NOT reload the page component.

#### Scenario: Selecting filters updates the URL
- **WHEN** the user selects promoter 3 and status "InSprint"
- **THEN** the URL becomes `/projects?status=InSprint&promoterId=3` (parameter order is not significant) and no `page` parameter is present

#### Scenario: Opening a shared URL restores the state
- **WHEN** the user opens `/projects?promoterId=3&tagIds=1&tagIds=2&page=2&pageSize=50&sortBy=promoter&sortDir=desc&view=tabla`
- **THEN** the "Promotor" selector shows promoter 3, the "Etiquetas" selector shows tags 1 and 2, the table shows page 2 with 50 items per page sorted by promoter descending (with the header indicator), and the first request to the API already includes those filters

#### Scenario: Reload keeps the state
- **WHEN** the user has filters applied and reloads the browser
- **THEN** the list reopens with the same filters, page and view

#### Scenario: Board view in the URL
- **WHEN** the user switches to "Tablero"
- **THEN** the URL includes `view=tablero`; switching back to "Tabla" removes the `view` parameter

#### Scenario: Browser back navigation
- **WHEN** the user changes the promoter filter and then presses the browser back button
- **THEN** the list returns to the previous filter state shown in the URL

#### Scenario: Clearing all filters
- **WHEN** the user clears every filter and is on page 1 of "Tabla"
- **THEN** the URL is `/projects` with no query parameters

#### Scenario: Invalid values in the URL
- **WHEN** the URL contains invalid values (e.g. `promoterId=abc`, `page=-1`, `pageSize=7`, `sortBy=foo`, `sortDir=up`, `status=Unknown`, `complexity=Huge`, `tagIds=x`, `view=foo`)
- **THEN** each invalid value MUST be ignored (treated as not set: page 1, page size 20, no sort or `asc` direction, view "Tabla") and the list MUST load without errors

#### Scenario: Promoter id in URL that does not exist
- **WHEN** the URL has `promoterId=999` and no such promoter exists
- **THEN** the filter is still applied (empty result) and the page does not fail

### Requirement: Sort the project list API
`GET /api/projects` MUST accept optional query parameters `sortBy` (one of `title`, `promoter`, `organicUnit`, `complexity`, `portfolioYear`, case-insensitive) and `sortDir` (`asc` or `desc`, case-insensitive, default `asc`). Sorting MUST be applied before pagination. Text sorts (`title`, `promoter`, `organicUnit`, by name) MUST be case-insensitive. `complexity` MUST sort in its natural order (VerySmall, Small, Medium, Large, VeryLarge), not alphabetically. Projects with a null sort value MUST appear last in both directions. Ties MUST be broken by `Id` descending so pages are stable. Without `sortBy`, or with an unknown `sortBy`, the order MUST remain the current default (`Id` descending). An unknown `sortDir` MUST be treated as `asc`.

#### Scenario: Sort by promoter ascending
- **WHEN** a client calls `GET /api/projects?sortBy=promoter&sortDir=asc` and projects have promoters "Biblioteca", "Alumnos" and none
- **THEN** the items are ordered "Alumnos", "Biblioteca", then the project without promoter

#### Scenario: Nulls last when descending
- **WHEN** a client calls `GET /api/projects?sortBy=organicUnit&sortDir=desc`
- **THEN** projects with organic unit are ordered by name descending and projects without organic unit come last

#### Scenario: Complexity natural order
- **WHEN** a client calls `GET /api/projects?sortBy=complexity` with projects of complexity Large, VerySmall and Medium
- **THEN** the items are ordered VerySmall, Medium, Large

#### Scenario: Sort applies across pages
- **WHEN** a client calls `GET /api/projects?sortBy=title&page=2&pageSize=2` with titles "A", "B", "C", "D"
- **THEN** page 2 contains "C" and "D"

#### Scenario: Stable ties
- **WHEN** two projects have the same `portfolioYear` and the client sorts by `portfolioYear`
- **THEN** the one with the higher `Id` comes first, on every request

#### Scenario: Unknown sort values
- **WHEN** a client calls `GET /api/projects?sortBy=foo&sortDir=bar`
- **THEN** the system responds 200 with the default order (`Id` descending)

#### Scenario: Sort combined with filters
- **WHEN** a client calls `GET /api/projects?promoterId=3&sortBy=title`
- **THEN** only promoter 3's projects are returned, ordered by title

### Requirement: Sortable columns in the projects table
In the "Tabla" view, the columns "Título", "Promotor", "Unidad orgánica", "Complejidad" and "Año cartera" MUST be sortable by clicking their header, cycling ascending → descending → no sort. Only one column MUST be sorted at a time. Changing the sort MUST reload from the server starting at page 1.

#### Scenario: Sort by a column
- **WHEN** the user clicks the "Promotor" header once
- **THEN** the table reloads sorted by promoter ascending from page 1, and the header shows the ascending indicator

#### Scenario: Cycle back to no sort
- **WHEN** the user clicks the same header three times
- **THEN** the table returns to the default order and no header shows a sort indicator

#### Scenario: Switching sorted column
- **WHEN** the table is sorted by "Promotor" and the user clicks "Título"
- **THEN** the table is sorted by title ascending only, and "Promotor" shows no indicator

### Requirement: Selectable page size
The "Tabla" view MUST let the user choose the page size among 10, 20, 50 and 100. Changing it MUST reload from page 1.

#### Scenario: Change page size
- **WHEN** the user on page 3 selects 50 items per page
- **THEN** the table reloads showing up to 50 projects from page 1

### Requirement: Board view honours tag filter
`GET /api/portfolio` MUST include, for each project, a `tagIds` array with the ids of its tags (empty array when none). In the "Tablero" view, when one or more tags are selected in the "Etiquetas" filter, each column MUST show only projects that have at least one of the selected tags, combined with the other filters, matching the semantics of the "Tabla" view.

#### Scenario: Board with tag filter
- **WHEN** tags 1 and 2 are selected and the user is in "Tablero", and project A has tag 1, project B has tag 3 and project C has no tags
- **THEN** only project A is shown

#### Scenario: Board without tag filter
- **WHEN** no tag is selected
- **THEN** the board shows projects regardless of their tags, as before

#### Scenario: Portfolio project without tags
- **WHEN** a project has no tags
- **THEN** its `/api/portfolio` entry includes `tagIds: []`
