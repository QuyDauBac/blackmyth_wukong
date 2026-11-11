"use strict";

// No npm packages are required. SQL Server queries are sent through Microsoft's sqlcmd utility.
const http = require("node:http");
const fs = require("node:fs");
const fsp = require("node:fs/promises");
const path = require("node:path");
const os = require("node:os");
const { spawn } = require("node:child_process");
const { randomBytes, scrypt: scryptCallback, timingSafeEqual } = require("node:crypto");
const { promisify } = require("node:util");

const scrypt = promisify(scryptCallback);
const projectRoot = path.resolve(__dirname, "..");
const envFile = path.join(__dirname, ".env");
const sessions = new Map();
const attempts = new Map();

function loadLocalConfig() {
  try {
    const lines = fs.readFileSync(envFile, "utf8").split(/\r?\n/);
    for (const line of lines) {
      const match = line.match(/^\s*([A-Z0-9_]+)\s*=\s*(.*?)\s*$/);
      if (!match || match[1].startsWith("#") || process.env[match[1]] !== undefined) continue;
      process.env[match[1]] = match[2].replace(/^(['"])(.*)\1$/, "$2");
    }
  } catch {
    // An absent .env uses the local development defaults below.
  }
}
loadLocalConfig();

const port = Number(process.env.PORT || 3000);
const sqlServer = process.env.SQL_SERVER || "localhost";
const database = process.env.SQL_DATABASE || "WukongStudy";
const sqlAuth = (process.env.SQL_AUTH || "windows").toLowerCase();

function sqlString(value) {
  return "N'" + String(value).replace(/'/g, "''") + "'";
}

function sqlcmdArgs(inputFile, outputFile, targetDatabase = database) {
  const args = ["-S", sqlServer, "-d", targetDatabase, "-b", "-r", "1", "-h", "-1", "-W", "-w", "65535", "-s", "|", "-f", "i:65001", "-u", "-i", inputFile, "-o", outputFile];
  if (sqlAuth === "windows") args.push("-E");
  else if (sqlAuth === "sql") args.push("-U", process.env.SQL_USER || "");
  else throw new Error("Set SQL_AUTH to windows or sql in server/.env.");
  if (process.env.SQL_TRUST_CERT === "1") args.push("-C");
  return args;
}

async function runSql(query, targetDatabase = database) {
  const tempDir = await fsp.mkdtemp(path.join(os.tmpdir(), "wukong-sql-"));
  const inputFile = path.join(tempDir, "request.sql");
  const outputFile = path.join(tempDir, "response.txt");
  try {
    await fsp.writeFile(inputFile, query, { encoding: "utf8", mode: 0o600 });
    const args = sqlcmdArgs(inputFile, outputFile, targetDatabase);
    const childEnv = { ...process.env };
    if (sqlAuth === "sql") {
      childEnv.SQLCMDPASSWORD = process.env.SQL_PASSWORD || "";
      delete childEnv.SQL_PASSWORD;
    }
    const output = await new Promise((resolve, reject) => {
      const child = spawn("sqlcmd", args, { windowsHide: true, env: childEnv });
      const stderr = [];
      child.stderr.on("data", chunk => stderr.push(chunk));
      child.on("error", reject);
      child.on("close", async code => {
        const errorText = Buffer.concat(stderr).toString("utf8").replace(/\0/g, "").trim();
        if (code !== 0) {
          const error = new Error("SQL Server request failed.");
          error.sqlMessage = errorText;
          reject(error);
        } else {
          try {
            const text = (await fsp.readFile(outputFile)).toString("utf16le").replace(/^\uFEFF/, "").replace(/\0/g, "").trim();
            resolve(text);
          } catch (error) { reject(error); }
        }
      });
    });
    return output;
  } finally {
    await fsp.rm(tempDir, { recursive: true, force: true });
  }
}

function outputLines(value) {
  return value.split(/\r?\n/).map(line => line.trim()).filter(Boolean);
}

function isDuplicateSqlError(error) {
  return /2601|2627/.test(error.sqlMessage || "");
}

async function prepareDatabase() {
  if (!/^[A-Za-z0-9_]+$/.test(database)) {
    throw new Error("SQL_DATABASE must contain only letters, numbers, and underscores.");
  }
  const setupPath = path.join(projectRoot, "database", "setup.sql");
  const setupSql = (await fsp.readFile(setupPath, "utf8")).replaceAll("WukongStudy", database);
  await runSql(setupSql, "master");
}

let databaseInitialization;
async function requireDatabase(response) {
  const ready = await databaseInitialization;
  if (ready) return true;
  sendJson(response, 503, {
    error: "The local site is running, but it could not open or create the SQL Server database. Run database/setup.sql in SSMS or use a SQL login with database creation permission."
  });
  return false;
}

function setSessionCookie(response, token) {
  response.setHeader("Set-Cookie", "wukong_session=" + token + "; Path=/; HttpOnly; SameSite=Strict; Max-Age=86400");
}

function clearSessionCookie(response) {
  response.setHeader("Set-Cookie", "wukong_session=; Path=/; HttpOnly; SameSite=Strict; Max-Age=0");
}

function cookieValue(request, key) {
  const cookies = (request.headers.cookie || "").split(";");
  const prefix = key + "=";
  const found = cookies.map(item => item.trim()).find(item => item.startsWith(prefix));
  return found ? found.slice(prefix.length) : "";
}

function currentSession(request) {
  const token = cookieValue(request, "wukong_session");
  const session = sessions.get(token);
  if (!session) return null;
  if (session.expiresAt < Date.now()) {
    sessions.delete(token);
    return null;
  }
  return { token, ...session };
}

function createSession(response, user) {
  const token = randomBytes(32).toString("base64url");
  sessions.set(token, { user, expiresAt: Date.now() + 86_400_000 });
  setSessionCookie(response, token);
}

function sendJson(response, status, value, extraHeaders = {}) {
  const body = JSON.stringify(value);
  response.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store",
    "X-Content-Type-Options": "nosniff",
    ...extraHeaders
  });
  response.end(body);
}

function readJson(request) {
  return new Promise((resolve, reject) => {
    let body = "";
    request.setEncoding("utf8");
    request.on("data", chunk => {
      body += chunk;
      if (body.length > 8192) {
        reject(Object.assign(new Error("Request is too large."), { status: 413 }));
        request.destroy();
      }
    });
    request.on("end", () => {
      try { resolve(JSON.parse(body || "{}")); }
      catch { reject(Object.assign(new Error("Enter valid form information."), { status: 400 })); }
    });
    request.on("error", reject);
  });
}

function checkOrigin(request) {
  const origin = request.headers.origin;
  if (!origin) return;
  try {
    if (new URL(origin).host !== request.headers.host) throw new Error();
  } catch {
    throw Object.assign(new Error("This request could not be verified."), { status: 403 });
  }
}

function checkRate(request) {
  const address = request.socket.remoteAddress || "unknown";
  const now = Date.now();
  const prior = attempts.get(address);
  const current = !prior || prior.until < now ? { count: 0, until: now + 15 * 60 * 1000 } : prior;
  current.count += 1;
  attempts.set(address, current);
  if (current.count > 12) throw Object.assign(new Error("Too many attempts. Please try again in 15 minutes."), { status: 429 });
}

function validateEmail(value) {
  const email = String(value || "").trim().toLowerCase();
  if (email.length > 254 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    throw Object.assign(new Error("Enter a valid email address."), { status: 400 });
  }
  return email;
}

function validatePassword(value) {
  if (typeof value !== "string" || value.length < 10 || value.length > 128) {
    throw Object.assign(new Error("Use a password between 10 and 128 characters."), { status: 400 });
  }
  return value;
}

function validateName(value) {
  const name = String(value || "").trim();
  if (name.length < 2 || name.length > 80 || /[\u0000-\u001f\u007f]/.test(name)) {
    throw Object.assign(new Error("Enter a name between 2 and 80 characters."), { status: 400 });
  }
  return name;
}

async function hashPassword(password, saltHex) {
  const salt = saltHex ? Buffer.from(saltHex, "hex") : randomBytes(16);
  const derived = await scrypt(password, salt, 64, { N: 16384, r: 8, p: 1, maxmem: 64 * 1024 * 1024 });
  return { saltHex: salt.toString("hex"), hashHex: derived.toString("hex"), hash: derived };
}

async function findStoredAccount(email) {
  const query = "SET NOCOUNT ON; SELECT PasswordSalt + '|' + PasswordHash + '|' + DisplayName FROM dbo.AppUser WHERE NormalizedEmail = " + sqlString(email) + ";";
  const row = outputLines(await runSql(query)).find(line => /^[a-fA-F0-9]{32}\|[a-fA-F0-9]{128}\|/.test(line));
  if (!row) return null;
  return {
    saltHex: row.slice(0, 32),
    hash: Buffer.from(row.slice(33, 161), "hex"),
    name: row.slice(162)
  };
}

async function passwordMatches(password, account) {
  const actual = await hashPassword(password, account.saltHex);
  return actual.hash.length === account.hash.length && timingSafeEqual(actual.hash, account.hash);
}

async function signInExisting(response, account, email, password) {
  if (!await passwordMatches(password, account)) {
    sendJson(response, 409, { error: "An account with this email already exists. Sign in with its existing password." });
    return;
  }
  await runSql("UPDATE dbo.AppUser SET LastLoginAtUtc = SYSUTCDATETIME() WHERE NormalizedEmail = " + sqlString(email) + ";");
  const user = { name: account.name, email };
  createSession(response, user);
  sendJson(response, 200, { user, existing: true });
}

async function register(request, response) {
  checkOrigin(request);
  checkRate(request);
  if (!await requireDatabase(response)) return;
  const body = await readJson(request);
  const email = validateEmail(body.email);
  const name = validateName(body.name);
  const password = validatePassword(body.password);
  const existing = await findStoredAccount(email);
  if (existing) return signInExisting(response, existing, email, password);

  const { saltHex, hashHex } = await hashPassword(password);
  try {
    const query = "INSERT INTO dbo.AppUser (Email, NormalizedEmail, DisplayName, PasswordSalt, PasswordHash) VALUES (" +
      sqlString(String(body.email).trim()) + ", " + sqlString(email) + ", " + sqlString(name) + ", '" + saltHex + "', '" + hashHex + "');";
    await runSql(query);
  } catch (error) {
    if (isDuplicateSqlError(error)) {
      const duplicate = await findStoredAccount(email);
      if (duplicate) return signInExisting(response, duplicate, email, password);
      return sendJson(response, 409, { error: "An account with this email already exists. Sign in with its existing password." });
    }
    throw error;
  }
  const user = { name, email };
  createSession(response, user);
  sendJson(response, 201, { user });
}

async function login(request, response) {
  checkOrigin(request);
  checkRate(request);
  if (!await requireDatabase(response)) return;
  const body = await readJson(request);
  const email = validateEmail(body.email);
  const password = validatePassword(body.password);
  const account = await findStoredAccount(email);
  if (!account) {
    await hashPassword(password, "9e85da2be5a34f92a01d4db87fef0000");
    return sendJson(response, 401, { error: "Email or password was not recognized." });
  }
  if (!await passwordMatches(password, account)) {
    return sendJson(response, 401, { error: "Email or password was not recognized." });
  }
  await runSql("UPDATE dbo.AppUser SET LastLoginAtUtc = SYSUTCDATETIME() WHERE NormalizedEmail = " + sqlString(email) + ";");
  const user = { name: account.name, email };
  createSession(response, user);
  sendJson(response, 200, { user });
}

async function createPurchase(request, response) {
  checkOrigin(request);
  const session = currentSession(request);
  if (!session) return sendJson(response, 401, { error: "Log in to your account before checkout." });
  checkRate(request);
  if (!await requireDatabase(response)) return;
  const body = await readJson(request);
  if (body.confirmDemoPayment !== true) {
    throw Object.assign(new Error("Confirm the demo payment to create the order."), { status: 400 });
  }

  // Server-owned product and amount: the browser cannot submit a different price.
  const amount = 999000.00;
  const paidAt = "SYSUTCDATETIME()";
  const query = "SET NOCOUNT ON; DECLARE @PurchaseId UNIQUEIDENTIFIER = NEWID(); DECLARE @PaidAt DATETIME2(0) = " + paidAt + "; " +
    "INSERT INTO dbo.GamePurchase (PurchaseId, UserId, ProductCode, ProductName, Edition, Amount, Currency, PaymentMethod, PaymentStatus, PaidAtUtc) " +
    "SELECT @PurchaseId, UserId, N'WUKONG-STD', N'Black Myth: Wukong', N'Standard Edition', " + amount + ", 'VND', N'Demo checkout', N'Paid', @PaidAt " +
    "FROM dbo.AppUser WHERE NormalizedEmail = " + sqlString(session.user.email) + "; " +
    "IF @@ROWCOUNT = 0 THROW 50001, 'The signed-in account no longer exists.', 1; " +
    "SELECT CONVERT(VARCHAR(36), @PurchaseId) + '|' + CONVERT(VARCHAR(19), @PaidAt, 120);";
  const row = outputLines(await runSql(query)).find(line => /^[0-9a-fA-F-]{36}\|/.test(line));
  if (!row) throw new Error("The purchase record was not returned by SQL Server.");
  const [purchaseId, paidAtUtc] = row.split("|");
  return sendJson(response, 201, {
    purchase: { id: purchaseId, product: "Black Myth: Wukong", edition: "Standard Edition", amount, currency: "VND", paymentMethod: "Demo checkout", status: "Paid", paidAtUtc },
    demo: true
  });
}

function staticFile(request, response, pathname) {
  const decoded = decodeURIComponent(pathname);
  const relative = decoded === "/" ? "index.html" : decoded.slice(1);
  if (!/^(index\.html|app\.js|styles\.css|checkout\.html|checkout\.js|checkout\.css|assets\/[^/]+|media\/[^/]+)$/.test(relative) || relative.includes("..") || relative.includes("\\")) {
    response.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" });
    return response.end("Not found");
  }
  const file = path.resolve(projectRoot, relative);
  const types = { ".html": "text/html; charset=utf-8", ".js": "text/javascript; charset=utf-8", ".css": "text/css; charset=utf-8", ".png": "image/png", ".jpg": "image/jpeg", ".jpeg": "image/jpeg", ".woff2": "font/woff2", ".mp4": "video/mp4" };
  fs.stat(file, (error, info) => {
    if (error || !info.isFile()) {
      response.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" });
      return response.end("Not found");
    }
    response.writeHead(200, {
      "Content-Type": types[path.extname(file).toLowerCase()] || "application/octet-stream",
      "Cache-Control": "no-store",
      "X-Content-Type-Options": "nosniff",
      "Content-Security-Policy": "default-src 'self'; img-src 'self' data:; media-src 'self'; style-src 'self'; script-src 'self'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'"
    });
    fs.createReadStream(file).pipe(response);
  });
}

async function handle(request, response) {
  const url = new URL(request.url, "http://localhost");
  try {
    if (request.method === "GET" && url.pathname === "/api/me") {
      const session = currentSession(request);
      return sendJson(response, 200, { user: session ? session.user : null });
    }
    if (request.method === "POST" && url.pathname === "/api/register") return await register(request, response);
    if (request.method === "POST" && url.pathname === "/api/login") return await login(request, response);
    if (request.method === "POST" && url.pathname === "/api/purchases") return await createPurchase(request, response);
    if (request.method === "POST" && url.pathname === "/api/logout") {
      checkOrigin(request);
      const session = currentSession(request);
      if (session) sessions.delete(session.token);
      clearSessionCookie(response);
      return sendJson(response, 200, { user: null });
    }
    if (request.method === "GET" || request.method === "HEAD") return staticFile(request, response, url.pathname);
    return sendJson(response, 405, { error: "This action is not available." }, { Allow: "GET, HEAD, POST" });
  } catch (error) {
    const status = error.status || 503;
    const message = error.status ? error.message : "The account service could not reach SQL Server. Check the database setup and connection settings.";
    if (!error.status) console.error("Account service error:", error.message, error.sqlMessage || "");
    if (!response.headersSent) sendJson(response, status, { error: message });
    else response.destroy();
  }
}

const server = http.createServer(handle);
server.listen(port, "127.0.0.1", () => {
  console.log("Wukong study site: http://127.0.0.1:" + port);
  console.log("SQL Server: " + sqlServer + " / " + database + " (" + sqlAuth + " authentication)");
  databaseInitialization = prepareDatabase()
    .then(() => {
      console.log("SQL database and user table are ready.");
      return true;
    })
    .catch(error => {
      console.error("Could not initialize SQL database:", error.message, error.sqlMessage || "");
      return false;
    });
});
server.on("error", error => {
  console.error("Could not start the local site:", error.message);
  process.exitCode = 1;
});
