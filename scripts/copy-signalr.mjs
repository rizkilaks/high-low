import { mkdirSync, copyFileSync } from "node:fs";
mkdirSync("src/HighLow/wwwroot/lib/signalr", { recursive: true });
copyFileSync("node_modules/@microsoft/signalr/dist/browser/signalr.js", "src/HighLow/wwwroot/lib/signalr/signalr.js");
