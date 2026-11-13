// Debug view of the offline AI players: NsoServer.Ai.AgentManager.DebugObject (GET /control/agents), refreshed every 2 s.
// Kept in its own file: the wiki's Content-Security-Policy (script-src 'self') blocks inline scripts.
(function () {
  "use strict";
  const $ = id => document.getElementById(id);
  const esc = s => String(s ?? "").replace(/[&<>"]/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]));
  const fmt = n => Number(n || 0).toLocaleString("vi-VN");
  const pct = (a, b) => b > 0 ? Math.max(0, Math.min(100, Math.round(a * 100 / b))) : 0;
  const store = (k, v) => { try { if (v === undefined) return localStorage.getItem(k); localStorage.setItem(k, v); } catch (e) { return null; } };
  // plan source -> chip (llm = local LLM, rl = learned model, anything else = rules)
  const SOURCE = s => s === "llm" ? ["blue", "LLM"] : s === "rl" ? ["green", "RL"] : ["amber", "Luật"];
  const CLASS_COLOR = ["#7a8193", "#c0533a", "#d08a1f", "#3f8f5a", "#2f86b8", "#8a55c7", "#c2457f"];
  let learningLoaded = false, learningDirty = false;
  const rewardFields = [
    ["questReward", "Xong nhiệm vụ", 20], ["stepReward", "Xong bước", 10], ["expReward", "EXP", 5],
    ["moneyReward", "Tài nguyên ròng", 5], ["deathPenalty", "Phạt chết (chờ hồi sinh)", 20], ["deathHopPenalty", "Phạt chết mỗi map phải đi lại", 5], ["stuckPenalty", "Phạt kẹt", 5], ["explorationReward", "Map mới", 5],
    ["newItemUseReward", "Dùng loại vật phẩm lần đầu", 5], ["newItemPurchaseReward", "Mua loại vật phẩm lần đầu", 5],
    ["timelyItemUseReward", "Dùng vật phẩm đúng lúc", 5], ["npcDiscoveryReward", "Khám phá tương tác NPC", 5],
    ["usefulNpcFeatureReward", "Dùng tính năng NPC hữu ích", 5],
    ["usefulHpPercent", "Ngưỡng HP cần hồi (%)", 100], ["usefulMpPercent", "Ngưỡng MP cần hồi (%)", 100],
    ["usefulItemCooldownSeconds", "Giãn thưởng dùng đúng lúc (giây)", 3600], ["usefulBagFreeSlots", "Túi cần dùng rương khi còn ≤ ô", 200],
    ["ledgerUselessAfter", "Vật phẩm vô ích sau số lần dùng", 100], ["wastefulPurchasePenalty", "Phạt mua lại đồ vô ích", 5]
  ];
  $("learning-rewards").innerHTML = rewardFields.map(([id, text, max]) => {
    const integer = (id.startsWith("useful") && !id.endsWith("Reward")) || id === "ledgerUselessAfter";
    return `<label>${text} <input id="lr-${id}" type="number" min="${integer && id !== "usefulBagFreeSlots" ? 1 : 0}" max="${max}" step="${integer ? 1 : 0.05}" style="width:80px"></label>`;
  }).join("");
  for (const input of $("learning-panel").querySelectorAll("input,select")) input.addEventListener("input", () => { learningDirty = true; });
  // Evaluation report (Tools/Learning/evaluation.py -> models/<version>/evaluation.json, read by RlBridge.ReadEvaluationDetails).
  // Built with textContent only: the report is a local file but is still treated as data.
  function learningReport(e) {
    const box = $("learning-report");
    const el = (tag, text, cls) => { const n = document.createElement(tag); if (text != null) n.textContent = text; if (cls) n.className = cls; return n; };
    box.replaceChildren(el("h2", "Kết quả đánh giá model"));
    if (!e || !e.version) { box.append(el("p", "Chưa có model ứng viên để đánh giá.", "muted")); return; }
    const r = e.report;
    const state = !r ? ["Đang chờ / đang chạy", "pending"] : r.passed ? ["ĐẠT", "pass"] : ["TRƯỢT", "fail"];
    box.append(el("p", `Ứng viên ${e.version}: `), el("span", state[0], "result " + state[1]));
    box.append(el("p", `Lượt đã chạy: ${e.completedRuns || 0}/${e.totalRuns || 6}` +
      (r ? ` · ${Math.round((r.seconds || 0) / 60)} phút/lượt · suy luận p95 ${Number(r.inferenceTickP95Ms || 0).toFixed(2)} ms` : "") +
      (r && r.timestamp ? ` · ${new Date(r.timestamp).toLocaleString("vi-VN")}` : ""), "muted"));
    if (e.error) box.append(el("p", "Lỗi đọc báo cáo: " + e.error, "fail"));
    if (r && e.matchesBalance === false) box.append(el("p", "Cân bằng game đã đổi sau lần đánh giá này; cần đánh giá lại.", "pending"));
    if (!r) return;
    if (r.reasons && r.reasons.length) {
      box.append(el("h3", "Lý do"));
      const ul = el("ul"); for (const why of r.reasons) ul.append(el("li", why)); box.append(ul);
    }
    const b = r.baseline || {}, c = r.candidate || {};
    const names = Object.keys(b).sort();
    if (!names.length) return;
    const avg = (p, k) => p && p.runs ? p[k] / p.runs : null;
    const num = v => v == null ? "–" : (Math.round(v * 10) / 10).toLocaleString("vi-VN");
    const cols = [["completed", "Xong NV 0-3", true], ["progress", "Tiến độ", true], ["exp", "EXP", true], ["deaths", "Chết", false], ["stuck", "Kẹt", false]];
    const table = el("table"), head = el("tr");
    table.append(el("caption", "Rules (B) so với RL (C), trung bình mỗi lượt. Xanh = RL tốt hơn, đỏ = kém hơn."));
    head.append(el("th", "Tính cách"));
    for (const [, label] of cols) { head.append(el("th", label + " B")); head.append(el("th", label + " C")); }
    table.append(head);
    for (const name of names) {
      const tr = el("tr"); tr.append(el("td", name));
      for (const [k, , higherBetter] of cols) {
        const vb = avg(b[name], k), vc = avg(c[name], k);
        tr.append(el("td", num(vb)));
        const cell = el("td", num(vc));
        if (vb != null && vc != null && vb !== vc) cell.className = (vc > vb) === higherBetter ? "pass" : "fail";
        tr.append(cell);
      }
      table.append(tr);
    }
    const scroll = el("div", null, "scroll"); scroll.append(table); box.append(scroll);
  }
  // Training progress (Tools/Learning/trainer.py Tracker -> trainer-status.json): phase, epochs, loss/accuracy,
  // PPO statistics, isolated evaluation runs and the history of training rounds. textContent only (data, not HTML).
  const PHASE = { clone: "Học theo rules (behaviour cloning)", ppo: "PPO trên dữ liệu RL tự chơi", evaluate: "Đánh giá ứng viên so với rules" };
  const KIND = { clone: "Học theo rules", ppo: "PPO" };
  const n3 = v => v == null || !isFinite(v) ? "–" : Number(v).toFixed(Math.abs(v) >= 100 ? 1 : 4);
  const pc = v => v == null || !isFinite(v) ? "–" : (v * 100).toFixed(1) + "%";
  const dur = s => { s = Math.max(0, Math.round(s)); const m = Math.floor(s / 60); return m >= 60 ? Math.floor(m / 60) + "h" + String(m % 60).padStart(2, "0") : m + ":" + String(s % 60).padStart(2, "0"); };
  function trainingView(d) {
    const box = $("learning-train");
    const el = (tag, text, cls) => { const x = document.createElement(tag); if (text != null) x.textContent = text; if (cls) x.className = cls; return x; };
    const table = (caption, head, rows) => {
      const t = el("table"); if (caption) t.append(el("caption", caption));
      const h = el("tr"); for (const c of head) h.append(el("th", c)); t.append(h);
      for (const r of rows) { const tr = el("tr"); for (const c of r) tr.append(el("td", c)); t.append(tr); }
      const sc = el("div", null, "scroll"); sc.append(t); return sc;
    };
    box.replaceChildren(el("h2", "Tiến độ huấn luyện"));
    d = d || {};
    const p = d.progress;
    const now = Date.now();
    if (p && p.total > 0) {
      const frac = Math.min(1, p.done / p.total), elapsed = (now - p.startedAt) / 1000;
      const eta = frac > 0.01 && frac < 1 ? " · còn ~" + dur(elapsed / frac - elapsed) : "";
      let line = `${PHASE[p.phase] || p.phase}${p.domain ? " · " + p.domain : ""}`;
      if (p.phase === "clone") line += ` · epoch ${p.epoch || 1}/${p.epochs} · batch ${fmt(p.done)}/${fmt(p.total)} · loss ${n3(p.loss)} · acc ${pc(p.accuracy)}`;
      else if (p.phase === "ppo") line += p.optimizing ? ` · tối ưu ${p.epochs} epoch trên ${fmt(p.done)} bước` : ` · gom rollout ${fmt(p.done)}/${fmt(p.total)} bước`;
      else if (p.phase === "evaluate") line += ` · lượt ${Math.min(p.total, p.done + 1)}/${p.total}` + (p.arm && p.arm !== "done" ? ` (lần ${p.trial}, ${p.arm === "rules" ? "rules" : "RL"}) · lượt này ${dur((now - p.runStartedAt) / 1000)}/${dur(p.seconds)}` : "");
      box.append(el("p", `${line} · ${Math.round(frac * 100)}% · đã chạy ${dur(elapsed)}${eta}`));
      const bar = el("div", null, "pbar"), fill = el("i"); fill.style.width = (frac * 100).toFixed(1) + "%"; bar.append(fill); box.append(bar);
      if (p.phase === "clone" && p.epochsDone && p.epochsDone.length)
        box.append(table("Từng epoch (val = dữ liệu rules giữ lại để kiểm tra)", ["Epoch", "Loss", "Acc", "Val loss", "Val acc"],
          p.epochsDone.map(e => [e.epoch, n3(e.loss), pc(e.accuracy), n3(e.valLoss), pc(e.valAccuracy)])));
      if (p.phase === "evaluate" && p.results && p.results.length)
        box.append(table("Kết quả từng lượt (tổng của các bot)", ["Lần", "Bên", "Xong NV 0-3", "Tiến độ", "EXP", "Chết", "Kẹt"],
          p.results.map(r => [r.trial, r.arm === "rules" ? "Rules" : "RL", r.quests + "/" + r.bots, fmt(r.progress), fmt(r.exp), r.deaths, r.stuck])));
    } else box.append(el("p", "Không có vòng train nào đang chạy: " + (d.status || "trainer chưa chạy"), "muted"));
    if (d.samples) box.append(el("p", "Dữ liệu trong bộ nhớ trainer: " + Object.entries(d.samples).map(([k, v]) => k + " " + fmt(v)).join(" · ") +
      (d.updates ? " · tổng bước PPO: " + Object.entries(d.updates).map(([k, v]) => k + " " + fmt(v)).join(", ") : ""), "muted"));
    const m = d.metrics || {};
    const latest = Object.keys(m).sort().map(k => m[k]);
    if (latest.length) {
      box.append(el("h3", "Vòng train gần nhất"));
      box.append(table(null, ["Mạng", "Kiểu", "Lúc", "Mẫu", "Loss", "Acc / val acc", "Thưởng TB", "Policy loss", "Value loss", "Entropy", "KL", "Clip", "Expl. var"],
        latest.map(e => [e.domain, KIND[e.kind] || e.kind, new Date(e.at).toLocaleTimeString("vi-VN"), fmt(e.rows), n3(e.loss),
          e.kind === "clone" ? pc(e.accuracy) + " / " + pc(e.valAccuracy) : "–", e.kind === "ppo" ? n3(e.meanReward) : "–",
          n3(e.policyLoss), n3(e.valueLoss), n3(e.entropy), n3(e.approxKl), e.clipFraction == null ? "–" : pc(e.clipFraction), n3(e.explainedVariance)])));
    }
    const hist = (d.history || []).slice(-20).reverse();
    if (hist.length) {
      box.append(el("h3", `Lịch sử (${(d.history || []).length} vòng gần nhất, hiện 20)`));
      box.append(table(null, ["Lúc", "Mạng", "Kiểu", "Mẫu", "Loss", "Val acc", "Thưởng TB", "KL", "Expl. var"],
        hist.map(e => [new Date(e.at).toLocaleString("vi-VN"), e.domain, KIND[e.kind] || e.kind, fmt(e.rows), n3(e.loss),
          e.kind === "clone" ? pc(e.valAccuracy) : "–", e.kind === "ppo" ? n3(e.meanReward) : "–", n3(e.approxKl), n3(e.explainedVariance)])));
    }
  }
  function learningView(l) {
    if (!l) return;
    $("learning-summary").textContent = `${l.model} · ${fmt(l.samples)} mẫu phiên này`;
    const detail = l.trainerDetails || {};
    $("learning-status").textContent = `Trainer: ${l.trainer} · ${detail.gpu || detail.device || "đang khởi động"}\nỨng viên: ${l.candidate || "chưa có"} · ${l.evaluation || "chưa đánh giá"}\nTầng model: ${l.modelStage} · FPS: ${Math.round(l.fps)} · suy luận/tick p95: ${Number(l.inferenceTickP95Ms || 0).toFixed(2)} ms\nMẫu bỏ do đầy hàng đợi: ${fmt(l.dropped)} · bước PPO: ${detail.updates ? Object.entries(detail.updates).map(([k,v]) => k + " " + fmt(v)).join(", ") : "chưa có"}${l.error ? "\nLỗi: " + l.error : ""}`;
    trainingView(l.trainerDetails);
    learningReport(l.evaluationDetails);
    if (learningLoaded && learningDirty) return; // polling must not overwrite edits, but should reflect automatic tier changes
    learningLoaded = true;
    $("learning-mode").value = l.mode === "Rules" ? "Rules" : "RL";   // an old "Guided" setting is shown (and saved) as RL
    $("learning-enabled").checked = l.learning;
    $("learning-human").checked = l.recordHuman;
    $("learning-auto").checked = l.autoApply;
    $("learning-evaluation").checked = l.evaluationEnabled;
    $("learning-device").value = l.device;
    for (const [id] of rewardFields) $("lr-" + id).value = l[id];
  }
  async function learningWrite(payload) {
    $("learning-feedback").textContent = "Đang lưu…";
    try {
      const state = await fetch("/control/state", { cache: "no-store" }).then(r => r.json());
      const response = await fetch("/control/learning", { method: "POST", headers: { "Content-Type": "application/json", "X-Nso-Control": state.token }, body: JSON.stringify(payload) });
      const result = await response.json();
      if (!response.ok) throw new Error(result.error || response.status);
      learningLoaded = false; learningDirty = false; learningView(result);
      $("learning-feedback").textContent = payload.command ? "Đã gửi yêu cầu; chỉ model đạt kiểm tra được áp dụng." : "Đã cập nhật. Tùy chọn được lưu nền trên máy.";
    } catch (e) { $("learning-feedback").textContent = "Không lưu được: " + e.message; }
  }
  $("learning-save").onclick = () => {
    const payload = { mode: $("learning-mode").value, learning: $("learning-enabled").checked, recordHuman: $("learning-human").checked,
      autoApply: $("learning-auto").checked, evaluationEnabled: $("learning-evaluation").checked, device: $("learning-device").value };
    for (const [id] of rewardFields) payload[id] = Number($("lr-" + id).value);
    learningWrite(payload);
  };
  $("learning-apply").onclick = () => learningWrite({ command: "apply" });
  $("learning-rollback").onclick = () => learningWrite({ command: "rollback" });

  let data = null, paused = false, nearOnly = store("ai.near") === "1", openId = null, tab = store("ai.tab") || "overview";
  $("sort").value = store("ai.sort") || "level";
  $("near").classList.toggle("on", nearOnly);
  $("pause").onclick = e => { paused = !paused; e.target.textContent = paused ? "Tiếp tục" : "Tạm dừng"; e.target.classList.toggle("on", paused); };
  $("near").onclick = e => { nearOnly = !nearOnly; store("ai.near", nearOnly ? "1" : "0"); e.target.classList.toggle("on", nearOnly); render(); };
  $("sort").onchange = e => { store("ai.sort", e.target.value); render(); };
  $("q").oninput = render;
  $("shade").onclick = close;
  document.addEventListener("keydown", e => { if (e.key === "Escape") close(); });

  function bar(label, value, max, color) {
    return `<div class="bar"><span>${label}</span><div class="t"><i style="width:${pct(value, max)}%;background:${color}"></i></div><span>${fmt(value)}/${fmt(max)}</span></div>`;
  }

  function questText(a) {
    const q = a.quest;
    if (!q) return "";
    if (!q.active) return `<b>${esc(q.name)}</b> · chưa nhận`;
    return `<b>${esc(q.name)}</b> · bước ${q.index + 1}/${q.steps}${q.step ? ": " + esc(q.step) : ""}${q.target > 0 ? ` (${q.count}/${q.target})` : ""}`;
  }

  function card(a) {
    const color = CLASS_COLOR[a.class || 0] || CLASS_COLOR[0];
    if (!a.online) {
      return `<section class="card off" data-id="${a.id}"><div class="head"><div class="ava" style="background:${CLASS_COLOR[0]}">${esc(a.name[0].toUpperCase())}</div>
        <div class="who"><b><span class="bid">#${a.id}</span> ${esc(a.name)}</b><div>${esc(a.loginStatus)}</div></div></div></section>`;
    }
    const q = a.quest || {};
    const qp = q.active && q.steps ? pct(q.index, q.steps) : 0;
    const chips = [];
    if (a.dead) chips.push('<span class="chip red">Đã ngã</span>');
    if (a.withHumans) chips.push('<span class="chip blue">Cùng khu với bạn</span>');
    if (a.pending) chips.push('<span class="chip amber">Đang nghĩ…</span>');
    if (a.recovering) chips.push('<span class="chip amber">Hồi phục</span>');
    if (a.plan) { const [c, t] = SOURCE(a.plan.source); chips.push(`<span class="chip ${c}">${t}</span>`); }
    return `<section class="card" data-id="${a.id}">
      <div class="head">
        <div class="ava" style="background:${color}">${esc(a.name[0].toUpperCase())}</div>
        <div class="who"><b><span class="bid">#${a.id}</span> ${esc(a.name)}</b><div>${esc(a.className)} · ${esc(a.mapName)} (map ${a.map}, khu ${a.zone})</div></div>
        <div class="lv"><b>${a.level}</b><small>cấp · ${a.expPercent}%</small></div>
      </div>
      <div class="bars">${bar("HP", a.hp, a.maxHp, "var(--hp)")}${bar("MP", a.mp, a.maxMp, "var(--mp)")}
        <div class="bar"><span>EXP</span><div class="t"><i style="width:${a.expPercent}%;background:var(--exp)"></i></div><span>${a.expPercent}%</span></div></div>
      <div class="row">${chips.join("")}<span class="chip" title="Bình HP/MP">🧪 ${a.hpPotions}/${a.mpPotions}</span><span class="chip" title="Thức ăn trong túi">🍙 ${a.foods ?? 0}</span>${usingChip(a)}<span class="chip">¥ ${fmt(a.yen)}</span><span class="chip">⚔ ${fmt(a.kills)}</span><span class="chip">☠ ${a.deaths}</span></div>
      <div class="doing" title="${esc(a.activity)}">${esc(a.activity)}</div>
      <div class="quest">${questText(a)}<div class="qbar"><i style="width:${qp}%"></i></div></div>
    </section>`;
  }

  // consumables: running effects first (what the bot is using now), else the last item used recently
  function usingText(a) {
    const fx = (a.effects || []).map(e => `${e.name} (${e.seconds}s)`);
    if (fx.length) return fx.join(", ");
    if (a.lastItem && a.lastItem.secondsAgo <= 60) return `${a.lastItem.name} (${a.lastItem.secondsAgo}s trước)`;
    return "";
  }
  function usingChip(a) { const t = usingText(a); return t ? `<span class="chip blue" title="Vật phẩm tiêu hao">💊 ${esc(t)}</span>` : ""; }

  function filtered() {
    if (!data || !data.agents) return [];
    const q = $("q").value.trim().toLowerCase();
    let list = data.agents.filter(a => !nearOnly || a.withHumans);
    if (q) list = list.filter(a => ["#" + a.id, a.name, a.mapName, a.activity, a.className, a.quest && a.quest.name].join(" ").toLowerCase().includes(q));
    const by = $("sort").value;
    const key = {
      level: a => -(a.level || 0) * 1e6 - (a.exp || 0) / 1e9,
      quest: a => -((a.quest ? a.quest.id : 0) * 100 + (a.quest ? a.quest.index : 0)),
      name: a => a.name,
      map: a => (a.map ?? 9999) * 100 + (a.zone || 0)
    }[by];
    return list.slice().sort((x, y) => { const a = key(x), b = key(y); return a < b ? -1 : a > b ? 1 : 0; });
  }

  function render() {
    if (!data) return;
    learningView(data.learning);
    const l = data.llm || {};
    $("llm").textContent = l.status || "";
    $("llm").classList.toggle("bad", l.enabled && l.available === false);
    const online = (data.agents || []).filter(a => a.online);
    const avg = online.length ? (online.reduce((s, a) => s + a.level, 0) / online.length).toFixed(1) : "—";
    const maxQuest = online.reduce((m, a) => Math.max(m, a.quest ? a.quest.id : 0), 0);
    const near = online.filter(a => a.withHumans).length;
    $("stats").innerHTML = [
      ["Đang chơi", online.length + "/" + (data.agents || []).length],
      ["Cấp trung bình", avg],
      ["Nhiệm vụ xa nhất", maxQuest],
      ["Cùng khu với bạn", near],
      ["LLM", (l.successes ?? 0) + "/" + (l.requests ?? 0) + (l.busy ? " · đang nghĩ" : "")],
      ["Trí nhớ RAG", !data.rag || !data.rag.enabled ? "tắt" : fmt(data.rag.stored) + " đã lưu · " + fmt(data.rag.knowledge) + " kiến thức game" + (data.rag.available ? "" : " · lỗi")]
    ].map(([k, v]) => `<div class="stat"><small>${k}</small><b>${esc(v)}</b></div>`).join("");
    const list = filtered();
    $("list").innerHTML = !data.running ? '<div class="empty">Người chơi AI đang tắt (NSO Server Control › AI players).</div>'
      : list.length ? list.map(card).join("") : '<div class="empty">Không có AI nào khớp bộ lọc.</div>';
    for (const c of document.querySelectorAll(".card")) c.onclick = () => open(Number(c.dataset.id));
    if (openId !== null) drawer();
  }

  function open(id) { openId = id; $("shade").classList.add("open"); $("drawer").classList.add("open"); $("drawer").setAttribute("aria-hidden", "false"); drawer(); }
  function close() { openId = null; $("shade").classList.remove("open"); $("drawer").classList.remove("open"); $("drawer").setAttribute("aria-hidden", "true"); }

  function drawer() {
    const a = (data.agents || []).find(x => x.id === openId);
    if (!a) { close(); return; }
    $("dh").innerHTML = `<button class="x" id="dx">✕</button><b style="font-size:17px"><span class="bid">#${a.id}</span> ${esc(a.name)}</b>
      <div style="color:var(--mute);font-size:12px">${esc(a.account)} · ${esc(a.traits)}</div><div style="font-size:13px;margin-top:4px">${esc(a.bio)}</div>`;
    $("dx").onclick = close;
    const tabs = [["overview", "Tổng quan"], ["bag", "Túi đồ"], ["knowledge", "Trí nhớ"], ["memory", "Sự kiện"], ["plan", "Kế hoạch"]];
    $("tabs").innerHTML = tabs.map(([k, n]) => `<button data-tab="${k}" class="${tab === k ? "on" : ""}">${n}${k === "knowledge" ? " (" + (a.knowledge || []).length + ")" : ""}</button>`).join("");
    for (const b of $("tabs").querySelectorAll("button")) b.onclick = () => { tab = b.dataset.tab; store("ai.tab", tab); drawer(); };
    $("dbody").innerHTML = ({ overview, bag, knowledge, memory, plan }[tab] || overview)(a);
  }

  function overview(a) {
    if (!a.online) return `<p>${esc(a.loginStatus)}</p>`;
    const q = a.quest || {};
    return `<dl class="kv">
      <dt>Cấp</dt><dd>${a.level} (${a.expPercent}% · ${fmt(a.exp)} exp)</dd>
      <dt>Môn phái</dt><dd>${esc(a.className)}</dd>
      <dt>HP / MP</dt><dd>${fmt(a.hp)}/${fmt(a.maxHp)} · ${fmt(a.mp)}/${fmt(a.maxMp)}</dd>
      <dt>ID</dt><dd>bot #${a.id} · nhân vật ${a.charId ?? "—"}</dd>
      <dt>Đang dùng</dt><dd>${esc(usingText(a)) || "—"}</dd>
      <dt>Dùng gần nhất</dt><dd>${a.lastItem ? esc(a.lastItem.name) + " · " + a.lastItem.secondsAgo + " s trước · tổng " + a.lastItem.count + " lần" : "—"}</dd>
      <dt>Tiền</dt><dd>${fmt(a.yen)} yên · ${fmt(a.xu)} xu</dd>
      <dt>Vị trí</dt><dd>${esc(a.mapName)} (map ${a.map}, khu ${a.zone}) · ${a.x}, ${a.y}</dd>
      <dt>Nhiệm vụ</dt><dd>${q.id} · ${questText(a)}</dd>
      <dt>Map đã mở</dt><dd>${q.unlockedMaps}</dd>
      <dt>Thống kê</dt><dd>${fmt(a.kills)} quái · ${a.deaths} lần ngã · ${a.questsDone} nhiệm vụ</dd>
      <dt>Đang làm</dt><dd>${esc(a.activity)}</dd>
      <dt>Quyết định</dt><dd>${esc(a.learning && a.learning.source)} · ${esc(a.learning && a.learning.version)}<br>${esc(a.learning && a.learning.fallback)}</dd>
      <dt>Học từ kết quả</dt><dd>${a.learning ? Number(a.learning.reward).toFixed(2) : "—"} · kẹt ${a.learning ? a.learning.stuck : 0}</dd>
      <dt>Lý do gần nhất</dt><dd>${esc(a.trigger)}</dd>
      <dt>Xem lại kế hoạch</dt><dd>sau ${a.nextReviewInS} s</dd>
    </dl>`;
  }

  function bag(a) {
    if (!a.online) return "";
    return `<h3>Trang bị</h3>${(a.equipment || []).length ? '<div class="row">' + a.equipment.map(e => `<span class="chip">${esc(e)}</span>`).join("") + "</div>" : "<p>Chưa mặc gì.</p>"}
      <h3>Hành trang (${a.bagSize - a.freeSlots}/${a.bagSize})</h3>${(a.bag || []).length ? '<div class="row">' + a.bag.map(e => `<span class="chip">${esc(e)}</span>`).join("") + "</div>" : "<p>Trống.</p>"}`;
  }

  function knowledge(a) {
    const groups = { danger: "Nguy hiểm", ground: "Bãi luyện", quest: "Nhiệm vụ", locked: "Map bị khoá", note: "Ghi chú", place: "Địa điểm", item: "Vật phẩm", map: "Map đã khám phá", player: "Người chơi", quiz: "Đáp án đã học" };
    const by = {};
    for (const f of a.knowledge || []) { const g = f.key.split(":")[0]; (by[g] = by[g] || []).push(f); }
    const keys = Object.keys(groups).filter(g => by[g]).concat(Object.keys(by).filter(g => !groups[g]));
    if (!keys.length) return "<p>Chưa học được gì.</p>";
    return keys.map(g => `<h3>${groups[g] || g}</h3>` + by[g].map(f => `<div class="fact">${esc(g === "quiz" ? f.key.replace("quiz:", "câu ") + " → " + f.text : f.text)}${f.count > 1 ? ` <small>×${f.count}</small>` : ""}</div>`).join("")).join("");
  }

  function memory(a) {
    return (a.memory || []).length ? '<ul class="list">' + a.memory.slice().reverse().map(m => `<li>${esc(m)}</li>`).join("") + "</ul>" : "<p>Chưa có sự kiện.</p>";
  }

  function plan(a) {
    let h = "";
    if (a.plan) {
      const [c, t] = SOURCE(a.plan.source);
      h += `<p><span class="chip ${c}">${t}</span> ${esc(a.plan.reason)}</p>`;
      if (a.plan.say) h += `<p>💬 “${esc(a.plan.say)}”</p>`;
      h += '<ol class="steps">' + a.plan.steps.map((s, i) => `<li class="${i === a.plan.stepIndex ? "cur" : ""}">${esc(s)}${i === a.plan.stepIndex ? ` · ${a.plan.stepProgress} · ${a.plan.stepSeconds}s` : ""}</li>`).join("") + "</ol>";
      h += `<p style="color:var(--mute)">Uống thuốc / rút lui dưới ${a.plan.fleeHp}% HP</p>`;
      if (a.priorities) h += `<p style="color:var(--mute)">Ưu tiên do LLM đặt (${a.prioritiesAgeS}s trước): ${Object.entries(a.priorities).map(([k, v]) => `${esc(k)} ×${v}`).join(", ")}</p>`;
    } else h += `<p>Chưa có kế hoạch${a.pending ? " — đang chờ LLM" : ""}.</p>`;
    h += "<h3>Các quyết định gần đây</h3>" + ((a.decisions || []).length ? "<pre>" + esc(a.decisions.slice().reverse().join("\n")) + "</pre>" : "<p>—</p>");
    if (data.llm && data.llm.lastAnswer) h += "<h3>Câu trả lời LLM gần nhất (mọi AI)</h3><pre>" + esc(data.llm.lastAnswer) + "</pre>";
    if (data.llm && data.llm.lastFailedAnswer) h += "<h3>Câu trả lời lỗi gần nhất · " + esc(data.llm.lastFailedAt) + "</h3><pre>" + esc(data.llm.lastFailedAnswer) + "</pre>";
    if (a.learning && a.learning.actions) h += "<h3>Phân bố hành động</h3><pre>" + esc(Object.entries(a.learning.actions).map(([k, v]) => `${k}: ${v}`).join("\n")) + "</pre>";
    if (a.itemLedger && a.itemLedger.length) h += "<h3>Sổ vật phẩm (đã mua / dùng thử)</h3><pre>" + esc(a.itemLedger.join("\n")) + "</pre>";
    if (a.actionRewards) h += "<h3>Thưởng hành động đã xác nhận · phiên này</h3><pre>" + esc(Object.entries(a.actionRewards).map(([k, v]) => `${(rewardFields.find(([id]) => id === k + "Reward" || id === k + "Penalty") || [k, k])[1]}: ${v.count} lần · ${Number(v.reward).toFixed(2)}`).join("\n")) + "</pre>";
    return h;
  }

  async function refresh() {
    if (!paused) {
      try {
        const r = await fetch("/control/agents", { cache: "no-store" });
        const d = await r.json();
        if (!r.ok) throw new Error(d.error || r.status);
        data = d;
        render();
      } catch (e) {
        $("llm").textContent = "Không kết nối được với game (" + e.message + "). Hãy bật Play mode.";
        $("llm").classList.add("bad");
      }
    }
    setTimeout(refresh, 2000);
  }
  refresh();
})();
