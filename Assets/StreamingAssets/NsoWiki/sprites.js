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

// All original zoom variants, independent of the highest-zoom web thumbnail.
function wikiItemSpriteFiles(item) {
  const files = item.spriteFiles || [];
  return `<section class="help-box"><b>Đường dẫn sprite · đầy đủ các bản zoom</b><p>Game tải ảnh theo mức zoom đang chạy. Ảnh xem trước trên web ưu tiên bản zoom lớn nhất. Sau khi thay ảnh cần làm mới cache icon trong game.</p>${files.length ? files.map(file => `<div class="sprite-file"><div><b>Zoom ${esc(file.zoom)}</b> · ${file.exists ? file.width > 0 && file.height > 0 ? `${esc(file.width)} × ${esc(file.height)} px` : "Có file · chưa đọc được kích thước" : "Chưa có file"}</div><code>${esc(file.fullPath || file.path)}</code><button class="button" data-copy="spriteFile" data-sprite-zoom="${esc(file.zoom)}">Sao chép full path</button></div>`).join("") : '<p>Chưa có dữ liệu đường dẫn. Nhấn Làm mới để tải lại.</p>'}</section>`;
}

// Capture image errors without inline handlers (the wiki CSP allows external scripts only).
document.addEventListener("error", event => {
  const img = event.target;
  if (!(img instanceof HTMLImageElement) || !img.hasAttribute("data-wiki-sprite")) return;
  img.parentElement.classList.add("sprite-missing");
  img.parentElement.title = `Chưa có sprite: ${img.dataset.spriteLabel || "ID này"}`;
}, true);
