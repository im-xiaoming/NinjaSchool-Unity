"use strict";

// Presentation only: a single slow clock, up to maxPlaying visible canvases, bounded decoded-image cache.
const effectPreview = (() => {
  let cleanup = () => {}, page = 0, paused = matchMedia("(prefers-reduced-motion: reduce)").matches;
  const images = new Map(), pageSize = 12, maxPlaying = pageSize; // every card of a page animates
  function imageFor(id) {
    if (images.has(id)) return images.get(id);
    const img = new Image(); img.src = `/sprites/effects/${id}.svg`;
    images.set(id, img);
    if (images.size > 128) images.delete(images.keys().next().value);
    return img;
  }
  function view() {
    const rows = (DB.collections.effectAnimations || []).filter(x => !q || String(x.id).includes(q.replace(/^id:/, "")));
    const pages = Math.max(1, Math.ceil(rows.length / pageSize)); page = Math.min(page, pages - 1);
    return `<section class="panel effect-library"><div class="panel-head"><div><div class="eyebrow">MINH HỌA TRỰC TIẾP</div><h2>Thư viện hoạt ảnh</h2></div><button class="button" id="effectToggle" aria-pressed="${!paused}">${paused ? "Phát minh họa" : "Tạm dừng"}</button></div><p>Vòng lặp chậm 4 khung/giây · tối đa ${maxPlaying} ô đang nhìn thấy. Tự dừng khi ẩn trang. ID hoạt ảnh riêng, không trùng với ID trạng thái bên dưới.</p><div class="effect-grid">${rows.slice(page * pageSize, (page + 1) * pageSize).map(x => `<figure class="effect-card"><canvas width="192" height="144" data-effect-preview="${x.id}" role="img" aria-label="Hoạt ảnh ${x.id}"></canvas><figcaption><b>Hoạt ảnh #${x.id}</b><span>${x.frames.length} khung · ${(x.frames.length / 4).toFixed(1)} giây/vòng</span><small data-effect-note>Đang chờ hiển thị</small></figcaption></figure>`).join("") || '<p>Không có hoạt ảnh phù hợp.</p>'}</div><div class="paging"><button class="button" id="effectPrev" ${page === 0 ? "disabled" : ""}>← Trước</button><span>${page + 1} / ${pages}</span><button class="button" id="effectNext" ${page + 1 >= pages ? "disabled" : ""}>Sau →</button></div></section><h2>Hiệu ứng trạng thái</h2>`;
  }
  function mount() {
    cleanup();
    const button = document.getElementById("effectToggle"); if (!button) return;
    const visible = new Set(), states = new Map(); let timer = null;
    const definitions = new Map((DB.collections.effectAnimations || []).map(x => [String(x.id), x]));
    for (const canvas of document.querySelectorAll("[data-effect-preview]")) {
      const data = definitions.get(canvas.dataset.effectPreview), frames = data?.frames || [];
      const xs = frames.map(f => f.dx), ys = frames.map(f => f.dy);
      const left = Math.min(0, ...xs) - 64, right = Math.max(0, ...xs) + 64;
      const top = Math.min(0, ...ys) - 64, bottom = Math.max(0, ...ys) + 64;
      states.set(canvas, { frame: 0, data, x: (left + right) / 2, y: (top + bottom) / 2, scale: Math.min(1.5, 192 / (right - left), 144 / (bottom - top)) });
    }
    function paint(canvas, advance) {
      const state = states.get(canvas), frames = state.data?.frames; if (!frames?.length) return;
      if (advance) state.frame = (state.frame + 1) % frames.length;
      const frame = frames[state.frame], img = imageFor(frame.imgId);
      const note = canvas.parentElement.querySelector("[data-effect-note]");
      if (!img.complete) { note.textContent = "Đang tải hình…"; return; }
      const ctx = canvas.getContext("2d"); ctx.clearRect(0, 0, canvas.width, canvas.height);
      if (!img.naturalWidth) { note.textContent = "Thiếu hình gốc cho khung này"; return; }
      // ServerEffect.paint uses centred SmallImage frames with original logical dx/dy.
      // SVG dimensions are normalized by the server; keep a fixed scale across frames.
      const scale = state.scale, w = img.naturalWidth * scale, h = img.naturalHeight * scale;
      ctx.imageSmoothingEnabled = false;
      ctx.drawImage(img, 96 + (frame.dx - state.x) * scale - w / 2, 72 + (frame.dy - state.y) * scale - h / 2, w, h);
      note.textContent = paused ? "Đã tạm dừng" : `Khung ${state.frame + 1}/${frames.length}`;
    }
    function tick() { [...visible].slice(0, maxPlaying).forEach(c => paint(c, !paused)); }
    function schedule() {
      clearInterval(timer); timer = null;
      if (document.hidden || !visible.size) return;
      [...visible].slice(0, maxPlaying).forEach(firstFrame);
      [...visible].slice(maxPlaying).forEach(c => { c.parentElement.querySelector("[data-effect-note]").textContent = "Đang chờ lượt minh họa"; });
      // Paused previews still get their first loaded frame, then stop the clock completely.
      if (!paused) timer = setInterval(tick, 250);
    }
    const observer = new IntersectionObserver(entries => {
      for (const entry of entries) {
        if (entry.isIntersecting) visible.add(entry.target); else visible.delete(entry.target);
      }
      schedule();
    }, { threshold: 0.1 });
    states.forEach((_, canvas) => observer.observe(canvas));
    // Image completion repaints once; no per-frame network requests or per-card timers.
    // Load completion is handled only for visible previews, with cleanup on navigation.
    const pending = new Map();
    function firstFrame(canvas) {
      const first = states.get(canvas).data?.frames?.[0]; if (!first) return;
      const img = imageFor(first.imgId);
      if (img.complete) { paint(canvas, false); return; }
      if (pending.has(canvas)) return;
      const done = () => { pending.delete(canvas); img.removeEventListener("load", done); img.removeEventListener("error", done); if (visible.has(canvas) && !document.hidden) paint(canvas, false); };
      pending.set(canvas, { img, done }); img.addEventListener("load", done, { once: true }); img.addEventListener("error", done, { once: true });
    }
    button.onclick = () => { paused = !paused; button.textContent = paused ? "Phát minh họa" : "Tạm dừng"; button.setAttribute("aria-pressed", String(!paused)); schedule(); };
    document.getElementById("effectPrev").onclick = () => { page--; render(); };
    document.getElementById("effectNext").onclick = () => { page++; render(); };
    document.addEventListener("visibilitychange", schedule);
    cleanup = () => { clearInterval(timer); observer.disconnect(); document.removeEventListener("visibilitychange", schedule); pending.forEach(({img, done}) => { img.removeEventListener("load", done); img.removeEventListener("error", done); }); pending.clear(); visible.clear(); };
  }
  return { view, mount, stop: () => cleanup() };
})();
