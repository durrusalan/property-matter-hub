# Property Matter Hub — Desktop App

A Windows desktop application for Irish property law practices.  
Indexes your Z: drive client folders, syncs Gmail, and provides two-way Google Calendar integration — all in one place.

---

## Contents

1. [System requirements](#1-system-requirements)
2. [Installing the app](#2-installing-the-app)
3. [First launch — the setup wizard](#3-first-launch--the-setup-wizard)
4. [Setting up the Z: drive](#4-setting-up-the-z-drive)
5. [Connecting your Google account (Gmail + Calendar)](#5-connecting-your-google-account-gmail--calendar)
6. [Day-to-day use](#6-day-to-day-use)
7. [Settings reference](#7-settings-reference)
8. [Building and publishing from source](#8-building-and-publishing-from-source)
9. [Troubleshooting](#9-troubleshooting)

---

## 1. System requirements

| Requirement | Minimum |
|---|---|
| Windows | Windows 10 (64-bit) or later |
| .NET runtime | **Not needed** — the published `.exe` is fully self-contained |
| Z: drive | A mapped network drive at `Z:\` (or a local folder mapped to `Z:`) |
| Google account | Any Gmail or Google Workspace account |
| Internet | Required only for Gmail/Calendar sync; the app works offline otherwise |

---

## 2. Installing the app

There is **no installer** — the app ships as a single `.exe` file.

1. Copy `PropertyMatterHub.App.exe` to any folder on your machine  
   (e.g. `C:\Users\Alan\Apps\PropertyMatterHub.App.exe`)
2. Double-click it to run
3. Windows may show a SmartScreen warning the first time ("Windows protected your PC").  
   Click **More info → Run anyway** — this is normal for unsigned apps.

> **Tip:** Right-click the `.exe` and choose **"Send to → Desktop (create shortcut)"** for easy access.

---

## 3. First launch — the setup wizard

The first time you open the app a **Welcome wizard** appears automatically.

### Step 1 — Z: Drive Root Folder

This is the path where your client folders live on the shared drive.

- Default: `Z:\Clients`
- Change it only if your folders are in a different location (e.g. `Z:\Active Clients`)

### Step 2 — Excel Client Database (optional)

If you have an existing Excel spreadsheet with client details, enter its path here.

- Default: `Z:\ClientDatabase.xlsx`
- Leave blank if you don't have one

### Step 3 — Connect Google Account (optional, can be done later)

Click **Connect** to set up Gmail and Calendar access now.  
See [Section 5](#5-connecting-your-google-account-gmail--calendar) for the full walkthrough.  
You can skip this and connect later from **Settings → Google Workspace**.

### Step 4 — Get Started

Click **Get Started →**. The app saves your choices and opens the main window.  
The wizard never appears again — all settings can be changed at any time in **Settings**.

---

## 4. Setting up the Z: drive

The app reads your client folders from `Z:\Clients` (or whatever path you set in the wizard).  
It expects folders named in this pattern:

```
Z:\Clients\
    Smith, John - PROP-2024-001\
    O'Brien, Mary - PROP-2024-002\
    Murphy & Associates - PROP-2024-003\
```

**Format:** `Client Name - Matter/Case Reference`

The part before the dash becomes the **Client name**.  
The part after the dash becomes the **Matter reference**.

### If you don't have a Z: drive yet

You can map any shared folder as `Z:` using Windows.

**Option A — Map a network folder:**
1. Open File Explorer → right-click **This PC** → **Map network drive**
2. Drive letter: `Z`
3. Folder: `\\server\share\Clients` (your server path)
4. Tick **Reconnect at sign-in** → Finish

**Option B — Map a local folder (testing / development):**
```powershell
# Run once in PowerShell (as Administrator)
subst Z: "C:\YourLocalFolder"

# To make it persist across reboots, add this to Windows startup:
# Create a shortcut in shell:startup pointing to:
#   subst Z: "C:\YourLocalFolder"
```

### Re-indexing the drive

The app automatically scans the Z: drive on startup.  
To trigger a manual re-scan: **Settings → Z: Drive Indexing → Re-scan Z: Drive**.  
Re-scanning is safe — it never deletes or overwrites existing records.

---

## 5. Connecting your Google account (Gmail + Calendar)

This is a **one-time setup** that takes about 10 minutes.  
After it's done, Gmail and Calendar sync automatically every few minutes in the background.

### Cost

**Free.** The Gmail API and Google Calendar API are both in Google's free tier.  
A property office will never get close to the usage limits.

---

### Part A — Create a Google Cloud project (one time)

1. Open [console.cloud.google.com](https://console.cloud.google.com) in your browser  
   Sign in with the **shared office Google account** (the one whose Gmail you want to sync)

2. Click the project dropdown at the top → **New Project**  
   Name it `Property Matter Hub` → click **Create**

3. Make sure the new project is selected in the dropdown at the top

---

### Part B — Enable the APIs

4. In the left sidebar go to **APIs & Services → Library**

5. Search for **Gmail API** → click it → click **Enable**

6. Go back to the Library, search for **Google Calendar API** → click it → click **Enable**

---

### Part C — Configure the consent screen

7. In the left sidebar go to **APIs & Services → OAuth consent screen**

8. **User type:**
   - If your office uses **Google Workspace** (paid Google business account): choose **Internal** — no review needed, skip to step 10
   - Otherwise choose **External** → click Create

9. Fill in the required fields:
   - App name: `Property Matter Hub`
   - User support email: your email address
   - Developer contact email: your email address
   - Click **Save and Continue** through the remaining screens (you don't need to add scopes manually)

10. On the **Test users** screen (External only):  
    Click **Add Users** → add the office Gmail address → Save

---

### Part D — Create the OAuth credentials

11. In the left sidebar go to **APIs & Services → Credentials**

12. Click **+ Create Credentials → OAuth client ID**

13. Application type: **Desktop app**  
    Name: `Matter Hub Desktop` (or anything you like)  
    Click **Create**

14. A popup appears showing your **Client ID** and **Client Secret**  
    Keep this window open — you'll paste these into the app in the next step

---

### Part E — Connect in the app

15. In Property Matter Hub, go to **Settings → Google Workspace**  
    (or use the Connect button in the first-run wizard)

16. Click **Connect Google Account**

17. A dialog opens — paste in:
    - **Client ID** — the long string ending in `.apps.googleusercontent.com`
    - **Client Secret** — the shorter string

18. Click **Connect →**

19. Your default browser opens automatically to Google's sign-in page  
    Sign in with the **shared office Gmail account** → click **Allow**

20. The browser shows a confirmation and you can close that tab  
    Back in the app, the status shows **Connected ✓**

**Done.** The credentials are saved to your user profile.  
You will never need to do this again — not even after reinstalling the app.

---

### What the app can access

| Permission | What it's used for |
|---|---|
| Gmail (read + modify labels) | Read emails, auto-classify by matter, mark as read |
| Gmail (send) | Send emails from the compose pane |
| Google Calendar (read + write) | Show upcoming events, create/edit/delete events, two-way sync |

The app never accesses Google Drive, Contacts, or any other Google service.

---

## 6. Day-to-day use

### Dashboard
Opens automatically on launch. Shows:
- Active matter count and recent activity
- Upcoming calendar events (next 7 days)
- Emails needing review (auto-classified but uncertain)

### Clients and Matters
Populated automatically from your Z: drive folders.  
Edit matter details, add notes, link emails and calendar events to specific matters.

### Email
- **Fetch & Classify** — pulls recent emails from Gmail and auto-links them to matters based on subject/sender
- **Needs Review** — emails the classifier wasn't confident about; link them manually
- **Compose** — send emails directly from the app, optionally linked to a matter

### Calendar
- Shows your Google Calendar events in an agenda view
- Create, edit, and delete events without leaving the app
- **Sync** button pulls the latest from Google Calendar

### Search
Global search across clients, matters, emails, and calendar events simultaneously.

---

## 7. Settings reference

| Setting | What it does | Default |
|---|---|---|
| Z: Drive Root Path | Where your client folders are | `Z:\Clients` |
| Excel File Path | Path to your client Excel spreadsheet | `Z:\ClientDatabase.xlsx` |
| Case Folder Pattern | Regex used to parse folder names into Client + Matter | `^(?<ClientName>.+?)\s*-\s*(?<CaseNumber>.+)$` |
| Case Folder Depth | How many levels deep to scan (1 = direct children of Root) | `1` |
| Test Pattern | Scans the Z: drive with the current pattern and shows a count | — |
| Re-scan Z: Drive | Re-indexes all folders; safe to run any time | — |
| Connect Google Account | Opens the credentials dialog | — |
| Disconnect | Revokes the Google token | — |

---

## 8. Building and publishing from source

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) (or later)
- Windows (required for WPF)

### Run from source
```powershell
cd PropertyMatterHub
dotnet run --project src/PropertyMatterHub.App
```

### Run the tests
```powershell
cd PropertyMatterHub
dotnet test
```

### Publish a single self-contained .exe
```powershell
cd PropertyMatterHub
dotnet publish src/PropertyMatterHub.App -p:PublishProfile=win-x64-singlefile
```

Output lands in `publish/PropertyMatterHub.App.exe` (~120–150 MB, no .NET runtime required on the target machine).

---

## 9. Troubleshooting

### App won't start — "Windows protected your PC"
This is a SmartScreen warning for unsigned applications.  
Click **More info → Run anyway**. You only need to do this once per machine.

### Z: drive folders not showing up
1. Confirm `Z:\Clients` (or your configured root) is accessible in File Explorer
2. Check the folder names match the pattern `ClientName - MatterRef`
3. Go to **Settings → Re-scan Z: Drive** and check the result message
4. Use **Test Pattern** in Settings to verify your regex matches

### Gmail / Calendar shows "Not connected"
- Go to **Settings → Google Workspace → Connect Google Account**
- If you get an error, make sure the Gmail API and Calendar API are both enabled in your Google Cloud project (Part B above)
- If you added the account as a Test User (External consent screen), confirm the email address matches exactly

### "OAuth cancelled" or browser doesn't open
- Make sure a default browser is set in Windows Settings
- Try disconnecting (**Settings → Disconnect**) and connecting again
- Check that the Client ID and Client Secret were copied correctly (no leading/trailing spaces)

### App is slow to start
The first launch scans the Z: drive and sets up the local database — this can take a few seconds if you have hundreds of client folders. Subsequent launches are fast.

### Where are my settings and data stored?

| Item | Location |
|---|---|
| User settings (paths, Google credentials) | `%LocalAppData%\PropertyMatterHub\appsettings.user.json` |
| Google OAuth token | `%LocalAppData%\PropertyMatterHub\google-token\` |
| SQLite database | `Z:\PropertyMatterHub\hub.db` (falls back to `%LocalAppData%\PropertyMatterHub\hub.db` if Z: is unavailable) |

To fully reset the app, delete the `%LocalAppData%\PropertyMatterHub\` folder and re-run the setup wizard.
