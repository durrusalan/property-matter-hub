# Property Matter Hub Demo (Mock UI)

A React demo for an **Irish property law** practice. It shows a mock matter-management UI with two view modes and a unified search that includes document content (simulating Microsoft Graph Search / SharePoint).

## What’s in the demo

- **Property Matter Hub** – Toggle between:
  - **Custom app** – Dashboard (KPIs, matter list, workload chart, key dates, automations) and a rich matter workspace (client details, dates/tasks/documents/templates tabs).
  - **Lists-style** – SharePoint Lists / Power Apps style: list-first tables with filters (matters, key dates, email filing).
- **Mock data** – Irish property focus: residential purchase/sale, mortgage, clients (Irish names/addresses), matters (e.g. PROP-2026-0042), key dates (closing, searches, contract signing), tasks, and templates (client care, contract for sale, requisitions, letter to lender).
- **Unified search** – One search bar that returns **grouped results**:
  - **Matters** – by ID, title, practice area, or client name.
  - **Clients** – by name, email, or address.
  - **Documents** – by file name or **document content** (mock “Graph Search”): each doc has `searchableContent`; search shows a **snippet** with the **matching phrase highlighted**, similar to Graph Search hit highlights.
  - **Templates** – by name, tags, or practice area.
- **Document search behaviour** – In a real app, SharePoint indexes Word and text-based PDFs; the [Microsoft Graph Search API](https://learn.microsoft.com/en-us/graph/search-concept-files) returns hit-highlighted snippets. This demo uses mock content and client-side snippet/highlight logic to show how that would look.

## How to run

### Option A: Use the included Next.js app (recommended)

From the repo root:

```bash
cd demo-app
npm install
npm run dev
```

Then open [http://localhost:3000](http://localhost:3000). The demo is the default page.

### Run at another location (no network)

When you’re at the client’s office (or anywhere without your usual network), you can run the demo **offline** from your laptop. No internet or WiFi needed.

1. **Before you go** – Build a static export and put it on your laptop (or USB):
   ```bash
   cd demo-app
   npm install
   npm run build
   ```
   This creates an `out` folder with plain HTML/CSS/JS (no Node server required at the client’s).

2. **What to bring** – Copy the whole `demo-app` folder to your laptop (or at least the `out` folder). You’ll need Node.js on the laptop to run the small file server in the next step.

3. **At the client’s** – From the `demo-app` folder on your laptop run:
   ```bash
   npm run serve
   ```
   Then open **http://localhost:3000** in your browser. The demo runs entirely on your machine; the client can look at your screen, or you can connect the laptop to a projector/TV.

If you prefer not to use Node at the client’s, you can use any static file server that can serve the `out` folder (e.g. Python: `cd out && python -m http.server 3000`, then open http://localhost:3000).

### Run on phone or tablet (single file, no server)

One self-contained HTML file works offline on any device. No Node, no build, no server.

1. **Get the file** – In this repo, open or copy:
   - **`PropertyMatterHub-standalone.html`** (in the repo root)
2. **On your phone or tablet** – Either:
   - **Email** the file to yourself and open the attachment in your browser, or  
   - **Upload** it to Google Drive / iCloud / OneDrive, open the app on the device, then “Open in browser” or “Preview”, or  
   - **Transfer** via USB / AirDrop to the device and open it from the Files app (e.g. “Open in Safari” or “Open in Chrome”).
3. The demo runs entirely in the browser: unified search (Matters / Clients / Documents / Templates), **document snippets with highlighted matches**, and a Custom vs Lists-style toggle. Same mock data as the full app, in one file.

Note: Some browsers may restrict local file access (e.g. `file://`). If the file doesn’t run when opened directly, use “Open in browser” from a cloud app or a simple local server (e.g. on your laptop run `npx serve .` in the folder containing the HTML file, then on the same WiFi open the shown URL on your phone).

### Option B: Drop the component into your own project

1. Use a Next.js (or React) project with **shadcn/ui** and **Tailwind** already set up.
2. Copy `demo-app/src/components/MatterHubDemo.tsx` into your app (e.g. `src/components/MatterHubDemo.tsx`).
3. Ensure the same shadcn components and dependencies are installed (see below).
4. Render it from a page:

```tsx
import MatterHubDemo from "@/components/MatterHubDemo";
export default function Page() {
  return <MatterHubDemo />;
}
```

## Dependencies

- **framer-motion** – animations
- **lucide-react** – icons
- **recharts** – dashboard chart
- **shadcn/ui** – Card, Button, Badge, Tabs, Table, Select, Dialog, DropdownMenu, Separator, Switch, Input, Label

If you need to add them:

```bash
npm i framer-motion lucide-react recharts
# and add the shadcn components you’re missing via: npx shadcn@latest add card button badge tabs table select dialog dropdown-menu separator switch input label
```

## Notes

- **Demo only** – No backend, no auth, no real Microsoft 365 or SharePoint. URLs and “SharePoint” references are placeholders.
- **Real app** – To search real document content you’d call the **Microsoft Graph Search API** (e.g. `entityTypes: driveItem`) and merge those results with your matter/register search; the API returns a `summary` (snippet) with hit-highlighted text per result.
