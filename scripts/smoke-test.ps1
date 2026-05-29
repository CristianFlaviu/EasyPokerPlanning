param(
    [switch]$SkipBuild,
    [switch]$ReadOnly,
    [switch]$KeepStartedServers,
    [string]$FrontendUrl = "http://localhost:4200",
    [string]$ApiBaseUrl = "http://localhost:5218",
    [string]$ChromePath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runDir = Join-Path $repoRoot ".codex-run\smoke-$stamp"
New-Item -ItemType Directory -Force -Path $runDir | Out-Null

$started = New-Object System.Collections.Generic.List[System.Diagnostics.Process]

function Write-Step([string]$Message) {
    Write-Host "==> $Message"
}

function Test-HttpOk([string]$Url) {
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
        return [int]$response.StatusCode -ge 200 -and [int]$response.StatusCode -lt 400
    }
    catch {
        return $false
    }
}

function Wait-HttpOk([string]$Url, [int]$TimeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if (Test-HttpOk $Url) {
            return
        }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for $Url"
}

function Start-LoggedProcess(
    [string]$Name,
    [string]$FilePath,
    [string[]]$ArgumentList,
    [string]$WorkingDirectory
) {
    $out = Join-Path $runDir "$Name.out.log"
    $err = Join-Path $runDir "$Name.err.log"
    $process = Start-Process `
        -FilePath $FilePath `
        -ArgumentList $ArgumentList `
        -WorkingDirectory $WorkingDirectory `
        -RedirectStandardOutput $out `
        -RedirectStandardError $err `
        -WindowStyle Hidden `
        -PassThru
    $started.Add($process)
    Write-Host "Started $Name (pid $($process.Id)); logs: $out / $err"
}

function Resolve-ChromePath {
    if ($ChromePath -and (Test-Path $ChromePath)) {
        return $ChromePath
    }

    $candidates = @(
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
        "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
        "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "Chrome or Edge was not found. Pass -ChromePath to a Chromium executable."
}

try {
    $apiHealthUrl = "$ApiBaseUrl/openapi/v1.json"
    $apiAlreadyRunning = Test-HttpOk $apiHealthUrl
    $frontendAlreadyRunning = Test-HttpOk $FrontendUrl

    if (-not $SkipBuild) {
        if ($apiAlreadyRunning) {
            Write-Step "Skipping backend build because API is already running at $ApiBaseUrl"
        }
        else {
            Write-Step "Building backend"
            dotnet build (Join-Path $repoRoot "backend\PokerPlanning.slnx")
        }

        Write-Step "Building frontend"
        Push-Location (Join-Path $repoRoot "frontend")
        try {
            npm run build
        }
        finally {
            Pop-Location
        }
    }

    if (-not $apiAlreadyRunning -and -not (Test-HttpOk $apiHealthUrl)) {
        Write-Step "Starting AppHost"
        Start-LoggedProcess `
            -Name "apphost" `
            -FilePath "dotnet" `
            -ArgumentList @("run", "--project", "backend/src/PokerPlanning.AppHost") `
            -WorkingDirectory $repoRoot
    }
    else {
        Write-Step "Using existing API at $ApiBaseUrl"
    }

    if (-not $frontendAlreadyRunning -and -not (Test-HttpOk $FrontendUrl)) {
        Write-Step "Starting Angular dev server"
        Start-LoggedProcess `
            -Name "frontend-ng-serve" `
            -FilePath "npm.cmd" `
            -ArgumentList @("start") `
            -WorkingDirectory (Join-Path $repoRoot "frontend")
    }
    else {
        Write-Step "Using existing frontend at $FrontendUrl"
    }

    Write-Step "Waiting for API and frontend"
    Wait-HttpOk $apiHealthUrl 90
    Wait-HttpOk $FrontendUrl 90

    $chrome = Resolve-ChromePath
    $nodeScriptPath = Join-Path $runDir "browser-smoke.cjs"
    $readOnlyFlag = if ($ReadOnly) { "true" } else { "false" }

    @'
const { spawn } = require("node:child_process");
const fs = require("node:fs");
const path = require("node:path");

const chromePath = process.argv[2];
const frontendUrl = process.argv[3].replace(/\/$/, "");
const apiBaseUrl = process.argv[4].replace(/\/$/, "");
const runDir = process.argv[5];
const readOnly = process.argv[6] === "true";
const port = 9300 + Math.floor(Math.random() * 200);
const userDir = path.join(runDir, "chrome-profile");
fs.mkdirSync(userDir, { recursive: true });

const chrome = spawn(chromePath, [
  "--headless=new",
  `--remote-debugging-port=${port}`,
  `--user-data-dir=${userDir}`,
  "--disable-gpu",
  "--no-first-run",
  "--no-default-browser-check",
  "about:blank",
], { stdio: "ignore" });

const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

async function requestJson(url, options) {
  const response = await fetch(url, options);
  if (!response.ok) {
    const body = await response.text().catch(() => "");
    throw new Error(`HTTP ${response.status} for ${url}: ${body}`);
  }
  return response.json();
}

async function apiPost(apiBaseUrl, path, body, roomToken) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-Room-Token": roomToken,
    },
    body: JSON.stringify(body ?? {}),
  });

  if (!response.ok) {
    const responseBody = await response.text().catch(() => "");
    throw new Error(`HTTP ${response.status} for ${path}: ${responseBody}`);
  }

  const text = await response.text();
  return text.length > 0 ? JSON.parse(text) : null;
}

async function waitForChrome() {
  for (let i = 0; i < 80; i++) {
    try {
      return await requestJson(`http://127.0.0.1:${port}/json/version`);
    } catch {
      await delay(100);
    }
  }
  throw new Error("Timed out waiting for Chromium debugging port.");
}

class Cdp {
  constructor(wsUrl) {
    this.ws = new WebSocket(wsUrl);
    this.nextId = 0;
    this.pending = new Map();
    this.events = [];
  }

  async open() {
    await new Promise((resolve, reject) => {
      this.ws.addEventListener("open", resolve, { once: true });
      this.ws.addEventListener("error", reject, { once: true });
    });

    this.ws.addEventListener("message", (event) => {
      const message = JSON.parse(event.data);
      if (message.id && this.pending.has(message.id)) {
        const pending = this.pending.get(message.id);
        this.pending.delete(message.id);
        if (message.error) pending.reject(new Error(JSON.stringify(message.error)));
        else pending.resolve(message.result);
      } else if (message.method) {
        this.events.push(message);
      }
    });
  }

  send(method, params = {}) {
    const id = ++this.nextId;
    this.ws.send(JSON.stringify({ id, method, params }));
    return new Promise((resolve, reject) => this.pending.set(id, { resolve, reject }));
  }

  close() {
    this.ws.close();
  }
}

async function evalExpr(cdp, expression) {
  const result = await cdp.send("Runtime.evaluate", {
    expression,
    awaitPromise: true,
    returnByValue: true,
  });

  if (result.exceptionDetails) {
    throw new Error(result.exceptionDetails.text || "Browser evaluation failed.");
  }

  return result.result.value;
}

async function waitFor(cdp, expression, label, timeoutMs = 15000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    try {
      if (await evalExpr(cdp, expression)) {
        return;
      }
    } catch (error) {
      const message = String(error?.message ?? error);
      if (!message.includes("navigated") && !message.includes("Execution context")) {
        throw error;
      }
    }
    await delay(250);
  }
  const snapshot = await evalExpr(cdp, `({
    url: location.href,
    text: document.body?.innerText?.slice(0, 1200) ?? ""
  })`).catch((error) => ({ url: "unavailable", text: String(error?.message ?? error) }));
  throw new Error(`Timed out waiting for ${label}.\nURL: ${snapshot.url}\nText: ${snapshot.text}`);
}

async function screenshot(cdp, name) {
  const image = await cdp.send("Page.captureScreenshot", { format: "png", fromSurface: true });
  const file = path.join(runDir, `${name}.png`);
  fs.writeFileSync(file, image.data, "base64");
  return file;
}

async function reloadAndWaitFor(cdp, expression, label, timeoutMs = 15000) {
  await cdp.send("Page.reload", { ignoreCache: true });
  await delay(1000);
  await waitFor(cdp, expression, label, timeoutMs);
}

async function navigateAndWaitFor(cdp, url, expression, label, timeoutMs = 15000) {
  await cdp.send("Page.navigate", { url: "about:blank" });
  await delay(300);
  await cdp.send("Page.navigate", { url });
  await delay(1000);
  await waitFor(cdp, expression, label, timeoutMs);
}

async function clickByText(cdp, text) {
  const clicked = await evalExpr(cdp, `
    (() => {
      const wanted = ${JSON.stringify(text)};
      const candidates = [...document.querySelectorAll("button,a")]
        .filter((el) => !el.disabled && el.offsetParent !== null);
      const el = candidates.find((candidate) => candidate.innerText.trim() === wanted)
        ?? candidates.find((candidate) => candidate.innerText.trim().includes(wanted));
      if (!el) return false;
      el.click();
      return true;
    })()
  `);

  if (!clicked) {
    throw new Error(`Could not click visible control: ${text}`);
  }
}

async function mouseClickByText(cdp, text) {
  const rect = await evalExpr(cdp, `
    (() => {
      const wanted = ${JSON.stringify(text)};
      const candidates = [...document.querySelectorAll("button,a")]
        .filter((el) => !el.disabled && el.offsetParent !== null);
      const el = candidates.find((candidate) => candidate.innerText.trim() === wanted)
        ?? candidates.find((candidate) => candidate.innerText.trim().includes(wanted));
      if (!el) return null;
      el.scrollIntoView({ block: "center", inline: "center" });
      const r = el.getBoundingClientRect();
      return { x: r.left + r.width / 2, y: r.top + r.height / 2 };
    })()
  `);

  if (!rect) {
    throw new Error(`Could not find visible control for mouse click: ${text}`);
  }

  await cdp.send("Input.dispatchMouseEvent", {
    type: "mouseMoved",
    x: rect.x,
    y: rect.y,
    button: "none",
  });
  await cdp.send("Input.dispatchMouseEvent", {
    type: "mousePressed",
    x: rect.x,
    y: rect.y,
    button: "left",
    clickCount: 1,
  });
  await cdp.send("Input.dispatchMouseEvent", {
    type: "mouseReleased",
    x: rect.x,
    y: rect.y,
    button: "left",
    clickCount: 1,
  });
}

async function setInput(cdp, selector, value) {
  const ok = await evalExpr(cdp, `
    (() => {
      const el = document.querySelector(${JSON.stringify(selector)});
      if (!el) return false;
      el.focus();
      el.value = ${JSON.stringify(value)};
      el.dispatchEvent(new Event("input", { bubbles: true }));
      el.dispatchEvent(new Event("change", { bubbles: true }));
      return true;
    })()
  `);

  if (!ok) {
    throw new Error(`Could not set input: ${selector}`);
  }
}

function collectErrors(cdp) {
  return cdp.events
    .filter((event) => {
      if (event.method === "Runtime.exceptionThrown") return true;
      if (event.method === "Log.entryAdded") {
        return ["error", "warning"].includes(event.params.entry.level);
      }
      if (event.method === "Network.responseReceived") {
        const url = event.params.response.url;
        return event.params.response.status >= 400 && !url.endsWith("/favicon.ico");
      }
      return false;
    })
    .map((event) => {
      if (event.method === "Network.responseReceived") {
        return {
          type: "network",
          status: event.params.response.status,
          url: event.params.response.url,
        };
      }
      return {
        type: event.method,
        level: event.params?.entry?.level,
        text: event.params?.entry?.text || event.params?.exceptionDetails?.text,
      };
    });
}

(async () => {
  let cdp;
  try {
    const participantId = crypto.randomUUID();

    await requestJson(`${apiBaseUrl}/openapi/v1.json`);
    const anonymousHistory = await fetch(`${apiBaseUrl}/rooms/history`, {
      headers: { "X-Participant-Id": participantId },
    });
    if (anonymousHistory.status !== 401) {
      throw new Error(`Expected anonymous history to return 401, got ${anonymousHistory.status}`);
    }

    await waitForChrome();
    const target = await requestJson(`http://127.0.0.1:${port}/json/new?about:blank`, { method: "PUT" });
    cdp = new Cdp(target.webSocketDebuggerUrl);
    await cdp.open();
    await cdp.send("Page.enable");
    await cdp.send("Runtime.enable");
    await cdp.send("Log.enable");
    await cdp.send("Network.enable");

    await cdp.send("Page.navigate", { url: frontendUrl });
    await waitFor(cdp, "document.body.innerText.includes('Create a planning room')", "lobby page");
    await screenshot(cdp, "01-lobby");

    await clickByText(cdp, "History");
    await waitFor(cdp, "location.pathname === '/history' && document.body.innerText.includes('Sign in to view history')", "signed-out history page");
    await screenshot(cdp, "02-history");

    await clickByText(cdp, "Back to lobby");
    await waitFor(cdp, "location.pathname === '/' && document.body.innerText.includes('Create a planning room')", "return to lobby");

    if (!readOnly) {
      const roomName = `Smoke Room ${new Date().toISOString()}`;
      await setInput(cdp, "input[formcontrolname='name']", roomName);
      await setInput(cdp, "input[formcontrolname='ownerDisplayName']", "Smoke Owner");
      await clickByText(cdp, "Create room");
      await waitFor(cdp, "location.pathname.startsWith('/room/') && document.body.innerText.includes('WAITING FOR ROUND')", "created room");
      await screenshot(cdp, "03-room-created");

      const roomId = await evalExpr(cdp, "location.pathname.split('/').filter(Boolean).at(-1)");
      const roomToken = await evalExpr(cdp, "localStorage.getItem('pp.roomToken.' + location.pathname.split('/').filter(Boolean).at(-1))");
      if (!roomToken) throw new Error("Room token was not stored after create.");
      const roomUrl = `${frontendUrl}/room/${roomId}`;
      await apiPost(apiBaseUrl, `/rooms/${roomId}/rounds`, { title: "Smoke round" }, roomToken);
      await navigateAndWaitFor(cdp, `${roomUrl}?smoke=${Date.now()}`, "document.body.innerText.includes('VOTING') && document.body.innerText.toLowerCase().includes('your pick')", "started round");

      await apiPost(apiBaseUrl, `/rooms/${roomId}/round/vote`, { card: "5" }, roomToken);
      await navigateAndWaitFor(cdp, `${roomUrl}?smoke=${Date.now()}`, "document.body.innerText.includes('1 / 1 voted')", "submitted vote");
      await screenshot(cdp, "04-voted");

      await apiPost(apiBaseUrl, `/rooms/${roomId}/round/reveal`, {}, roomToken);
      await navigateAndWaitFor(cdp, `${roomUrl}?smoke=${Date.now()}`, "document.body.innerText.includes('REVEALED')", "revealed votes");
      await screenshot(cdp, "05-revealed");

      await apiPost(apiBaseUrl, `/rooms/${roomId}/round/end`, { finalEstimate: "5" }, roomToken);
      await navigateAndWaitFor(cdp, `${roomUrl}?smoke=${Date.now()}`, "document.body.innerText.includes('WAITING FOR ROUND')", "ended round");

      await clickByText(cdp, "History");
      await waitFor(cdp, "location.pathname === '/history' && document.body.innerText.includes('Sign in to view history')", "signed-out history remains hidden", 20000);
      await screenshot(cdp, "06-history-signed-out");
    }

    const errors = collectErrors(cdp);
    if (errors.length > 0) {
      throw new Error(`Browser/network errors detected:\n${JSON.stringify(errors, null, 2)}`);
    }

    console.log(JSON.stringify({
      ok: true,
      mode: readOnly ? "read-only" : "full",
      screenshots: fs.readdirSync(runDir).filter((name) => name.endsWith(".png")),
    }, null, 2));
  } finally {
    if (cdp) cdp.close();
    chrome.kill();
  }
})().catch((error) => {
  chrome.kill();
  console.error(error.stack || error.message);
  process.exit(1);
});
'@ | Set-Content -Path $nodeScriptPath -Encoding UTF8

    Write-Step "Running browser smoke test"
    node $nodeScriptPath $chrome $FrontendUrl $ApiBaseUrl $runDir $readOnlyFlag
    if ($LASTEXITCODE -ne 0) {
        throw "Browser smoke test failed with exit code $LASTEXITCODE. Artifacts: $runDir"
    }

    Write-Step "Smoke test passed"
    Write-Host "Artifacts: $runDir"
}
finally {
    if (-not $KeepStartedServers) {
        foreach ($process in $started) {
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            }
        }
    }
}
