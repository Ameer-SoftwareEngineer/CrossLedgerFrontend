# CrossLedgerFrontend

The Blazor WebAssembly client for [CrossLedgerWeb](https://github.com/Ameer-SoftwareEngineer/CrossLedgerWeb), kept as its own repo rather than living inside the backend's solution.

## Why a separate repo

Same reasoning as [CrossLedgerDatabase](https://github.com/Ameer-SoftwareEngineer/CrossLedgerDatabase): this repo is deliberately independent of `CrossLedgerWeb`'s code. It talks to the API purely over HTTP against a documented contract (see the specification PDF in that repo) - its own request/response types in `Contracts/` are hand-written to match that contract, not shared via a project or package reference. That mirrors how a real, separately-deployed frontend actually relates to a backend it doesn't share a solution with.

## Stack

- Blazor WebAssembly (.NET 8)
- [MudBlazor](https://mudblazor.com/) (MIT) for the component library
- [Blazored.LocalStorage](https://github.com/Blazored/LocalStorage) (MIT) to persist the JWT across page refreshes

## Running locally

Requires `CrossLedgerWeb`'s API running on `http://localhost:5011` (its own default dev port) - `Program.cs` points at that address directly for now.

```bash
dotnet restore
dotnet run
```

## Status

Auth foundation: register, login, JWT-backed authentication state, protected routing. The wallet dashboard, send-money flow and the rest of the specification's front-end scope (section 9) land in following increments.
