"use strict";

const fs = require("node:fs");
const path = require("node:path");
const os = require("node:os");
const { spawn } = require("node:child_process");
const { randomBytes, scrypt: scryptCallback } = require("node:crypto");
const { promisify } = require("node:util");
const { StringDecoder } = require("node:string_decoder");

const scrypt = promisify(scryptCallback);
const envPath = path.join(__dirname, ".env");
const configLines = (() => {
  try { return fs.readFileSync(envPath, "utf8").split(/\r?\n/); }
  catch { return []; }
})();
for (const line of configLines) {
  const match = line.match(/^\s*([A-Z0-9_]+)\s*=\s*(.*?)\s*$/);
  if (!match || process.env[match[1]] !== undefined) continue;
  process.env[match[1]] = match[2].replace(/^(['"])(.*)\1$/, "$2");
}

const sqlServer = process.env.SQL_SERVER || "localhost";
const database = process.env.SQL_DATABASE || "WukongStudy";
const sqlAuth = (process.env.SQL_AUTH || "windows").toLowerCase();
const sqlTrustCert = process.env.SQL_TRUST_CERT === "1";

function readLine(prompt, masked = false) {
  return new Promise(resolve => {
    process.stdout.write(prompt);
    const decoder = new StringDecoder("utf8");
    let value = "";
    const finish = () => {
      process.stdin.setRawMode(false);
      process.stdin.pause();
      process.stdin.removeListener("data", onData);
      process.stdout.write("\n");
      resolve(value);
    };
    const onData = chunk => {
      const text = decoder.write(chunk);
      for (const char of text) {
        if (char === "\u0003") { process.stdout.write("\nCancelled.\n"); process.exit(130); }
        if (char === "\r" || char === "\n") return finish();
        if (char === "\u007f" || char === "\b") {
          if (value.length) { value = value.slice(0, -1); process.stdout.write("\b \b"); }
        } else if (char >= " ") {
          value += char;
          process.stdout.write(masked ? "*" : char);
        }
      }
    };
    process.stdin.setRawMode(true);
    process.stdin.resume();
    process.stdin.on("data", onData);
  });
}

function sqlString(value) { return "N'" + value.replace(/'/g, "''") + "'"; }

async function runSql(query) {
  if (!/^[A-Za-z0-9_]+$/.test(database)) throw new Error("SQL_DATABASE must contain only letters, numbers, and underscores.");
  const tempDir = await fs.promises.mkdtemp(path.join(os.tmpdir(), "wukong-reset-"));
  const inputFile = path.join(tempDir, "reset.sql");
  const outputFile = path.join(tempDir, "response.txt");
  try {
    await fs.promises.writeFile(inputFile, query, { encoding: "utf8", mode: 0o600 });
    const args = ["-S", sqlServer, "-d", database, "-b", "-r", "1", "-h", "-1", "-W", "-w", "65535", "-s", "|", "-f", "i:65001", "-u", "-i", inputFile, "-o", outputFile];
    const childEnv = { ...process.env };
    if (sqlAuth === "windows") args.push("-E");
    else if (sqlAuth === "sql") {
      args.push("-U", process.env.SQL_USER || "");
      childEnv.SQLCMDPASSWORD = process.env.SQL_PASSWORD || "";
      delete childEnv.SQL_PASSWORD;
    } else throw new Error("Set SQL_AUTH to windows or sql in server/.env.");
    if (sqlTrustCert) args.push("-C");
    return await new Promise((resolve, reject) => {
      const child = spawn("sqlcmd", args, { windowsHide: true, env: childEnv });
      const stderr = [];
      child.stderr.on("data", chunk => stderr.push(chunk));
      child.on("error", reject);
      child.on("close", async code => {
        const error = Buffer.concat(stderr).toString("utf8").replace(/\0/g, "").trim();
        if (code !== 0) reject(new Error(error || "SQL Server request failed."));
        else {
          try {
            const output = (await fs.promises.readFile(outputFile)).toString("utf16le").replace(/^\uFEFF/, "").replace(/\0/g, "").trim();
            resolve(output);
          } catch (readError) { reject(readError); }
        }
      });
    });
  } finally {
    await fs.promises.rm(tempDir, { recursive: true, force: true });
  }
}

async function main() {
  if (!process.stdin.isTTY || typeof process.stdin.setRawMode !== "function") {
    throw new Error("Run reset-password.bat in a Command Prompt window so the new password can be entered privately.");
  }
  console.log("SQL Server: " + sqlServer + " | Database: " + database + " | Authentication: " + sqlAuth);
  const identifier = (await readLine("Account email or UserId: ")).trim();
  const isUserId = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(identifier);
  const email = isUserId ? "" : identifier.toLowerCase();
  if (!isUserId && (email.length > 254 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))) throw new Error("Enter a valid email address or UserId GUID.");
  const whereClause = isUserId ? "UserId = CONVERT(UNIQUEIDENTIFIER, " + sqlString(identifier) + ")" : "NormalizedEmail = " + sqlString(email);
  const password = await readLine("New password (10-128 characters): ", true);
  if (password.length < 10 || password.length > 128) throw new Error("Use a password between 10 and 128 characters.");
  const confirm = await readLine("Confirm new password: ", true);
  if (password !== confirm) throw new Error("The passwords do not match.");
  const confirmation = (await readLine("Reset the password for " + identifier + "? Type YES to continue: ")).trim();
  if (confirmation !== "YES") { console.log("No changes were made."); return; }

  const accountQuery = "SET NOCOUNT ON; IF EXISTS (SELECT 1 FROM dbo.AppUser WHERE " + whereClause + ") SELECT N'ACCOUNT_FOUND';";
  const accountOutput = await runSql(accountQuery);
  if (!accountOutput.includes("ACCOUNT_FOUND")) {
    const visibleAccounts = await runSql("SET NOCOUNT ON; SELECT TOP (20) CONVERT(VARCHAR(36), UserId) + ' | ' + NormalizedEmail FROM dbo.AppUser ORDER BY CreatedAtUtc DESC;");
    throw new Error("No matching account exists in " + sqlServer + " / " + database + ". Accounts visible to this tool:\n" + (visibleAccounts || "(none)") + "\nUse one of these exact UserId values or check server/.env.");
  }

  const salt = randomBytes(16);
  const passwordHash = await scrypt(password, salt, 64, { N: 16384, r: 8, p: 1, maxmem: 64 * 1024 * 1024 });
  const query = "SET NOCOUNT ON; UPDATE dbo.AppUser SET PasswordSalt = '" + salt.toString("hex") + "', PasswordHash = '" + passwordHash.toString("hex") + "' WHERE " + whereClause + "; IF @@ROWCOUNT = 1 SELECT N'PASSWORD_UPDATED';";
  const output = await runSql(query);
  if (!output.includes("PASSWORD_UPDATED")) throw new Error("The account was found, but SQL Server did not confirm the update. No password change was reported.");
  console.log("Password updated. Return to the website and sign in using the new password.");
}

main().catch(error => {
  console.error("Password reset failed: " + error.message);
  process.exitCode = 1;
});
