import crypto from "node:crypto";
import http from "node:http";
import { WebSocket, WebSocketServer } from "ws";

const port = Number.parseInt(process.env.PORT || "8787", 10);
const host = process.env.HOST || "127.0.0.1";
const maxPlayers = Number.parseInt(process.env.MAX_PLAYERS || "60", 10);
const clients = new Map();

const httpServer = http.createServer((request, response) => {
  if (request.url === "/health") {
    response.writeHead(200, { "content-type": "application/json; charset=utf-8" });
    response.end(JSON.stringify({ ok: true, players: joinedPlayers().length }));
    return;
  }
  response.writeHead(200, { "content-type": "application/json; charset=utf-8" });
  response.end(JSON.stringify({ service: "hibi-plaza", websocket: "/hibi" }));
});

const socketServer = new WebSocketServer({
  server: httpServer,
  path: "/hibi",
  maxPayload: 8192,
  perMessageDeflate: false
});

socketServer.on("connection", (socket) => {
  if (clients.size >= maxPlayers) {
    send(socket, { type: "error", message: "The plaza is full. Please try again soon." });
    socket.close(1013, "Plaza full");
    return;
  }

  const player = {
    id: crypto.randomUUID(),
    name: "Guest",
    avatar: "{}",
    x: 0,
    z: -7,
    r: 0,
    joined: false,
    alive: true,
    lastChatAt: 0,
    moveWindowAt: Date.now(),
    moveCount: 0
  };
  clients.set(socket, player);

  socket.on("pong", () => {
    player.alive = true;
  });

  socket.on("message", (buffer) => {
    let message;
    try {
      message = JSON.parse(buffer.toString("utf8"));
    } catch {
      send(socket, { type: "error", message: "That message could not be read." });
      return;
    }
    if (!message || typeof message.type !== "string") return;

    if (message.type === "join") {
      if (player.joined) return;
      player.name = cleanName(message.name);
      player.avatar = cleanAvatar(message.avatar, player.name);
      player.x = clampNumber(message.x, -31, 31, 0);
      player.z = clampNumber(message.z, -24, 24, -7);
      player.r = clampNumber(message.r, 0, 360, 0);
      player.joined = true;

      const current = joinedPlayers()
        .filter((candidate) => candidate.id !== player.id)
        .map(publicPlayer);
      send(socket, {
        type: "welcome",
        id: player.id,
        players: current,
        count: current.length + 1
      });
      broadcast(
        {
          type: "player_join",
          ...publicPlayer(player)
        },
        socket
      );
      broadcastCount();
      return;
    }

    if (!player.joined) {
      send(socket, { type: "error", message: "Please join the plaza first." });
      return;
    }

    if (message.type === "move") {
      if (!allowMove(player)) return;
      player.x = clampNumber(message.x, -31, 31, player.x);
      player.z = clampNumber(message.z, -24, 24, player.z);
      player.r = clampNumber(message.r, 0, 360, player.r);
      broadcast(
        {
          type: "move",
          ...publicPlayer(player)
        },
        socket
      );
      return;
    }

    if (message.type === "chat") {
      const now = Date.now();
      if (now - player.lastChatAt < 700) {
        send(socket, { type: "error", message: "Please wait a moment before chatting again." });
        return;
      }
      player.lastChatAt = now;
      const text = cleanChat(message.text);
      if (!text) return;
      broadcast({
        type: "chat",
        id: player.id,
        name: player.name,
        text
      });
      return;
    }

    if (message.type === "emote") {
      const emote = ["wave", "cheer", "dance"].includes(message.emote) ? message.emote : "wave";
      broadcast(
        {
          type: "emote",
          id: player.id,
          emote
        },
        socket
      );
    }
  });

  socket.on("close", () => {
    clients.delete(socket);
    if (player.joined) {
      broadcast({ type: "player_leave", id: player.id, name: player.name });
      broadcastCount();
    }
  });

  socket.on("error", (error) => {
    console.warn("WebSocket error", player.id, error.message);
  });

  setTimeout(() => {
    if (!player.joined && socket.readyState === WebSocket.OPEN) {
      socket.close(1008, "Join timeout");
    }
  }, 10000);
});

function publicPlayer(player) {
  return {
    id: player.id,
    name: player.name,
    avatar: player.avatar,
    x: player.x,
    z: player.z,
    r: player.r
  };
}

function joinedPlayers() {
  return Array.from(clients.values()).filter((player) => player.joined);
}

function cleanName(value) {
  const clean = cleanText(value, 16).replace(/\s+/g, " ").trim();
  return clean || "Guest";
}

function cleanChat(value) {
  return cleanText(value, 120)
    .replace(/https?:\/\/\S+/gi, "[link removed]")
    .replace(/\s+/g, " ")
    .trim();
}

function cleanText(value, maximum) {
  if (typeof value !== "string") return "";
  return value
    .replace(/[<>]/g, "")
    .replace(/[\u0000-\u001f\u007f]/g, " ")
    .slice(0, maximum);
}

function cleanAvatar(value, name) {
  try {
    const parsed = JSON.parse(typeof value === "string" ? value : "{}");
    return JSON.stringify({
      displayName: name,
      skin: clampInteger(parsed.skin, 0, 4),
      hair: clampInteger(parsed.hair, 0, 6),
      top: clampInteger(parsed.top, 0, 6),
      bottom: clampInteger(parsed.bottom, 0, 5),
      hairStyle: clampInteger(parsed.hairStyle, 0, 3)
    });
  } catch {
    return JSON.stringify({ displayName: name, skin: 0, hair: 0, top: 0, bottom: 0, hairStyle: 0 });
  }
}

function clampInteger(value, minimum, maximum) {
  const number = Number.isFinite(Number(value)) ? Math.floor(Number(value)) : minimum;
  return Math.max(minimum, Math.min(maximum, number));
}

function clampNumber(value, minimum, maximum, fallback) {
  const number = Number(value);
  return Number.isFinite(number) ? Math.max(minimum, Math.min(maximum, number)) : fallback;
}

function allowMove(player) {
  const now = Date.now();
  if (now - player.moveWindowAt >= 1000) {
    player.moveWindowAt = now;
    player.moveCount = 0;
  }
  player.moveCount += 1;
  return player.moveCount <= 24;
}

function send(socket, message) {
  if (socket.readyState === WebSocket.OPEN) {
    socket.send(JSON.stringify(message));
  }
}

function broadcast(message, except = null) {
  const payload = JSON.stringify(message);
  for (const socket of clients.keys()) {
    if (socket !== except && socket.readyState === WebSocket.OPEN) {
      socket.send(payload);
    }
  }
}

function broadcastCount() {
  broadcast({ type: "count", count: joinedPlayers().length });
}

const heartbeat = setInterval(() => {
  for (const [socket, player] of clients.entries()) {
    if (!player.alive) {
      socket.terminate();
      clients.delete(socket);
      continue;
    }
    player.alive = false;
    socket.ping();
  }
}, 30000);

socketServer.on("close", () => clearInterval(heartbeat));

httpServer.listen(port, host, () => {
  console.log("Hibi Plaza realtime server listening on http://" + host + ":" + port);
});
