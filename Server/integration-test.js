import assert from "node:assert/strict";
import { WebSocket } from "ws";

const url = process.env.HIBI_TEST_URL || "ws://127.0.0.1:8787/hibi";

function createClient(name, avatar) {
  return new Promise((resolve, reject) => {
    const socket = new WebSocket(url);
    const messages = [];
    socket.on("open", () => {
      socket.send(JSON.stringify({
        type: "join",
        name,
        avatar: JSON.stringify(avatar),
        x: 1,
        z: 2,
        r: 45
      }));
      resolve({ socket, messages });
    });
    socket.on("message", (data) => messages.push(JSON.parse(data.toString())));
    socket.on("error", reject);
  });
}

function waitFor(client, predicate, timeout = 3000) {
  return new Promise((resolve, reject) => {
    const started = Date.now();
    const timer = setInterval(() => {
      const found = client.messages.find(predicate);
      if (found) {
        clearInterval(timer);
        resolve(found);
      } else if (Date.now() - started > timeout) {
        clearInterval(timer);
        reject(new Error("Timed out waiting for network message"));
      }
    }, 20);
  });
}

const avatarA = { skin: 1, hair: 2, top: 3, bottom: 1, hairStyle: 2 };
const avatarB = { skin: 3, hair: 4, top: 5, bottom: 2, hairStyle: 1 };
const first = await createClient("Mina", avatarA);
const firstWelcome = await waitFor(first, (message) => message.type === "welcome");
assert.equal(firstWelcome.count, 1);

const second = await createClient("Ren", avatarB);
const secondWelcome = await waitFor(second, (message) => message.type === "welcome");
assert.equal(secondWelcome.count, 2);
assert.equal(secondWelcome.players.length, 1);
assert.equal(secondWelcome.players[0].name, "Mina");
await waitFor(first, (message) => message.type === "player_join" && message.name === "Ren");

first.socket.send(JSON.stringify({ type: "move", x: 8.5, z: -3.25, r: 180 }));
const move = await waitFor(second, (message) => message.type === "move" && message.name === "Mina");
assert.equal(move.x, 8.5);
assert.equal(move.z, -3.25);

first.socket.send(JSON.stringify({ type: "chat", text: "Hello <b> https://example.com" }));
const chat = await waitFor(second, (message) => message.type === "chat" && message.name === "Mina");
assert.equal(chat.text.includes("<"), false);
assert.equal(chat.text.includes("https://"), false);
assert.equal(chat.text.includes("[link removed]"), true);

first.socket.send(JSON.stringify({ type: "emote", emote: "wave" }));
const emote = await waitFor(second, (message) => message.type === "emote");
assert.equal(emote.emote, "wave");

first.socket.close();
second.socket.close();
console.log("HIBI_SERVER_TEST_OK join move chat emote moderation");
