// End-to-end probe: hand delivery (getView) and deadline auto-submit (RoomSweeper tick).
// Run against a locally started app:  docker compose up -d --build  then  node scripts/probe.mjs
import signalr from "@microsoft/signalr";
import { setTimeout as sleep } from "node:timers/promises";

const { HubConnectionBuilder, LogLevel } = signalr;
const URL = process.env.PROBE_URL ?? "http://localhost:8080/hubs/game";

let failed = false;
function assert(cond, msg) {
    if (cond) console.log(`PASS  ${msg}`);
    else { console.error(`FAIL  ${msg}`); failed = true; }
}

const conn = new HubConnectionBuilder()
    .withUrl(URL)
    .configureLogging(LogLevel.Warning)
    .build();

const seen = { roundStarted: [], roundResolved: 0 };
const resolved = new Promise(res => conn.on("roundResolved", () => { seen.roundResolved++; res(true); }));
conn.on("roundStarted", e => seen.roundStarted.push(e.round));
for (const name of ["lobbyState", "gameStarted", "specialsRevealed", "cardsRevealed", "giftPrompt"]) {
    conn.on(name, () => {});
}

try {
    await conn.start();
} catch {
    console.error(`cannot connect to ${URL} — is the app running? (docker compose up -d --build)`);
    process.exit(1);
}
console.log(`connected to ${URL}`);

const joined = await conn.invoke("createRoom", "Probe", true);
assert(joined && joined.roomCode, "createRoom returns a room");
const { roomCode, token } = joined;

let view = await conn.invoke("getView", roomCode, token);
assert(view && view.myHand?.length === 10, `getView after join: hand has 10 cards (got ${view?.myHand?.length})`);

const started = await conn.invoke("startWithBots", roomCode, token);
assert(started === true, "startWithBots returns true");

view = await conn.invoke("getView", roomCode, token);
assert(view && view.myHand?.length === 10, `getView after start: hand has 10 cards (got ${view?.myHand?.length})`);

console.log("waiting up to 90s for the 60s deadline, possible 15s gift phase, to resolve…");
const got = await Promise.race([resolved, sleep(90_000).then(() => false)]);
assert(got === true, "RoundResolved arrives WITHOUT submitting (deadline auto-submit works)");
await sleep(2_000);
assert(seen.roundStarted.includes(2), `round 2 starts after resolution (seen: ${seen.roundStarted.join(",")})`);

view = await conn.invoke("getView", roomCode, token);
assert(view && view.myHand?.length === 9, `getView after resolution: hand has 9 cards (got ${view?.myHand?.length})`);

await conn.stop();
if (failed) { console.error("PROBE FAILED"); process.exit(1); }
console.log("PROBE OK");
process.exit(0);
