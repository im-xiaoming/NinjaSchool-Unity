"use strict";

// Template IDs resolve to original game art on the server; no paths or image data in DB records.
function wikiSprite(collection, id, label = "", size = "") {
  if (!["items", "mobs", "effects"].includes(collection) || !/^\d+$/.test(String(id)) || Number(id) > 32767) return "";
  if (collection === "effects") {
    id = DB?.collections.effects?.find(x => String(x.id) === String(id))?.icon;
    if (!Number.isInteger(id) || id < 0) return "";
  }
  const extension = collection === "items" ? "png" : "svg";
  return `<span class="wiki-sprite ${size === "large" ? "large" : size === "tiny" ? "tiny" : ""}" title="${esc(label)}"><img data-wiki-sprite data-sprite-label="${esc(label)}" src="/sprites/${collection}/${Number(id)}.${extension}" alt="" loading="${size === "large" ? "eager" : "lazy"}" decoding="async"></span>`;
}
function wikiSpriteName(collection, id, label) {
  return `<span class="sprite-name">${wikiSprite(collection,id,label)}<span>${esc(label)}</span></span>`;
}

// Capture image errors without inline handlers (the wiki CSP allows external scripts only).
document.addEventListener("error", event => {
  const img = event.target;
  if (!(img instanceof HTMLImageElement) || !img.hasAttribute("data-wiki-sprite")) return;
  img.parentElement.classList.add("sprite-missing");
  img.parentElement.title = `Chưa có sprite: ${img.dataset.spriteLabel || "ID này"}`;
}, true);
