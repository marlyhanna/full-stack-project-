# Library System — Web (Angular)

Angular 18 standalone frontend for the Library System API: catalog browsing,
borrow requests, admin approvals, and live notifications over SignalR.

## Prerequisites

- Node.js 18+ and npm
- The Library System backend running locally (default: `https://localhost:7080`)

## Setup

```bash
cd library-system-web
npm install
npm start
```

This opens the app at `http://localhost:4200`.

If your backend runs on a different URL, edit the two constants in
`src/app/core/config.ts`:

```ts
export const API_BASE_URL = 'https://localhost:7080/api';
export const NOTIFICATIONS_HUB_URL = 'https://localhost:7080/hubs/notifications';
```

## Demo accounts

Same seeded accounts as the backend:

| Role  | Email               | Password  |
|-------|---------------------|-----------|
| Admin | admin@library.local | Admin123! |
| User  | user@library.local  | User123!  |

The login screen has one-click buttons to fill either in.

## What's here

- **Login** — JWT auth against `/api/auth/login`, session persisted in localStorage.
- **Catalog** (`/books`) — search, availability filter, sort, pagination. Readers see a "Borrow" button per book that opens a request dialog.
- **My Borrowing** (`/requests/my`, readers only) — your requests as checkout cards, with status, due date, or deny reason.
- **Requests** (`/requests`, admins only) — review pending requests, approve or deny (with a required reason) in one click.
- **Notifications** (`/notifications`) — your notification inbox, click to mark as read.
- **Live toasts** — while signed in, a SignalR connection to `/hubs/notifications` pushes new borrow requests (to admins) and approval/denial/due-date reminders (to the requester) as they happen, no refresh needed.

## Verified

This project was built and verified in a sandboxed environment with real npm
access: `npm install`, a clean `ng build --configuration production` (zero
errors, zero warnings), and `ng serve` all succeed. It has not been tested
against a live instance of the backend, so double-check the end-to-end flows
once both are running together.

## Notes

- Uses Angular signals throughout for state (no NgRx here — the state surface is small enough that it wasn't worth the extra ceremony, but it would drop in cleanly on top of the existing services if this grows).
- Font optimization/inlining is turned off in `angular.json` since it requires internet access at build time — worth knowing if you ever build in a locked-down CI environment.
- CORS: the backend's `appsettings.json` allows `http://localhost:4200` by default, matching `ng serve`'s default port.
