"use strict";

// Presentation and draft state only. All stat calculations go through the server's gameplay calculator.
let pgScope = "0", pgReference = 1;
let pgFrom = 1, pgTo = 20, pgCompare = [], pgExpanded = false, pgAdvanced = false;
let pgDrafts = {}, pgLastPreview = null, pgTimer, pgSequence = 0, pgSaving = false;
let pgWoodenSword = true;

const PG_NATIVE = {
  Potential0: "Sức mạnh gốc + tăng tự nhiên + điểm đã phân bổ",
  Potential1: "Khéo léo gốc + tăng tự nhiên + điểm đã phân bổ",
  Potential2: "Thể lực gốc + tăng tự nhiên + điểm đã phân bổ",
  Potential3: "Nội công gốc + tăng tự nhiên + điểm đã phân bổ",
  MaxHp: "Thể lực × 10, sau đó áp dụng trang bị/kỹ năng",
  MaxMp: "Công thức MP gốc từ nội công, có sàn 100 MP",
  Damage: "Ngoại công/nội công theo lớp + kỹ năng + trang bị",
  Damage2: "Sát thương tối đa − phần nguyên của 10%",
  Exactly: "Khéo léo + thuộc tính chính xác",
  Miss: "Phần nguyên của khéo léo × 150% + thuộc tính né",
  Speed: "Tốc độ nền gốc: 5; giới hạn hiện tại: 0–17",
  DameDown: "Tổng thuộc tính giảm sát thương của trang bị",
  ResFire: "Kháng hỏa từ trang bị và kỹ năng hỗ trợ",
  ResIce: "Kháng băng từ trang bị và kỹ năng hỗ trợ",
  ResWind: "Kháng phong từ trang bị và kỹ năng hỗ trợ",
  Fatal: "Chí mạng từ trang bị/kỹ năng (1000 = 100%)",
  FatalDame: "Sát thương chí mạng cộng từ thuộc tính vật phẩm",
  PercentFatalDame: "Phần trăm sát thương chí mạng từ thuộc tính vật phẩm"
};
const PG_CORE = new Set(["Potential0","Potential1","Potential2","Potential3","MaxHp","MaxMp","Damage","DameDown","Speed"]);

function progressionExplanation() {
  return `
    <div class="help-box">
      <b>Kết quả = nền cấp 1 + mức tăng tự nhiên × (hệ số tăng trưởng / 100) + cộng thêm/cấp × (cấp − 1) + đóng góp riêng.</b>
      <p>Đây là công thức cho <b>một thuộc tính tại cấp đang xem</b>. Mỗi dòng bên dưới có bộ tham số riêng.</p>
    </div>
    <details class="progression-guide">
      <summary>Hiểu từng thành phần và cách nhập</summary>
      <dl class="progression-terms">
        <dt>Nền cấp 1</dt><dd>Giá trị bắt đầu của thuộc tính, chỉnh ở phần 01. Ví dụ HP cấp 1 là 80 thì mọi cấp đều bắt đầu tính từ nền 80. Với chỉ số chiến đấu, đây là nền của nhân vật mẫu chưa mặc trang bị và chưa phân thêm điểm.</dd>
        <dt>Mức tăng tự nhiên</dt><dd><b>Tổng phần tăng từ cấp 1 đến cấp đang xem</b>, do công thức gốc tính ra. Ví dụ HP gốc cấp 1 là 50, cấp 5 là 90 thì mức tăng tự nhiên tại cấp 5 là <b>90 − 50 = 40</b>. Đây có thể là đường tăng bậc thang do phép tính số nguyên và giới hạn của game.</dd>
        <dt>Hệ số tăng trưởng (%)</dt><dd>Nhân riêng phần tăng tự nhiên: <b>100%</b> giữ nguyên, <b>150%</b> tăng gấp 1,5 lần, <b>50%</b> còn một nửa, <b>0%</b> bỏ phần tăng tự nhiên. Nền cấp 1 và các phần cộng khác vẫn còn.</dd>
        <dt>Cộng thêm mỗi cấp</dt><dd>Một lượng cộng độc lập sau mỗi lần lên cấp. Nhập <b>2</b> thì cấp 1 cộng 0, cấp 2 cộng 2, cấp 5 cộng 8. Có thể nhập số âm để giảm, hoặc số thập phân như 0.5 (nửa điểm/cấp); kết quả cuối dùng số nguyên theo quy tắc của game.</dd>
        <dt>(Cấp − 1)</dt><dd>Số lần lên cấp tính từ cấp 1. Cấp 5 có <b>4</b> lần tăng, nên phần cộng thêm/cấp được nhân với 4.</dd>
        <dt>Đóng góp riêng</dt><dd>Phần chênh lệch do điểm người chơi đã phân, trang bị và kỹ năng thực tế so với nhân vật mẫu. Hệ số tăng trưởng của dòng này giữ phần đó riêng. Mẫu cơ sở chưa mặc trang bị, chưa phân thêm điểm; trường hợp Kiếm gỗ được tính thêm vũ khí riêng. Mẫu chưa vào lớp vẫn có chiêu cơ bản tự tăng cấp như gameplay.</dd>
      </dl>
      <div class="progression-example">
        <h3>Ví dụ minh họa: HP tại cấp 5</h3>
        <p>Giả sử nền muốn dùng là <b>80</b>; công thức gốc tăng <b>40 HP</b> từ cấp 1 đến cấp 5; hệ số là <b>150%</b>; cộng thêm <b>2 HP/cấp</b>; nhân vật có đóng góp riêng <b>20 HP</b>.</p>
        <p class="progression-example-result">80 + 40 × (150 / 100) + 2 × (5 − 1) + 20 = <b>168 HP</b></p>
        <small>Ví dụ giải thích cách tính, không phải kết quả của cấu hình đang chọn. Hãy dùng phần 03 để xem kết quả thật.</small>
      </div>
      <p class="description"><b>Để trống</b> dùng tham số kế thừa/gốc. <b>Nhập 0</b> là đặt giá trị 0: 0% bỏ tăng tự nhiên, còn +0/cấp chỉ bỏ phần cộng thêm. Nhóm đã vào lớp mặc định không tự tăng tiềm năng; nếu chưa phân điểm và chưa thêm tăng trưởng thì tăng 150% của phần tăng bằng 0 vẫn là 0. Giới hạn chỉ số có thể làm kết quả dừng tăng hoặc chạm sàn.</p>
    </details>
    <details class="progression-guide">
      <summary>Ý nghĩa các thuộc tính nhân vật</summary>
      <dl class="progression-terms">
        <dt>Sức mạnh</dt><dd>Tiềm năng dùng để tính công của các lớp ngoại công; cũng là đầu vào của chiêu cơ bản khi chưa vào lớp.</dd>
        <dt>Khéo léo</dt><dd>Tiềm năng ảnh hưởng đến chính xác và né tránh.</dd>
        <dt>Thể lực</dt><dd>Tiềm năng dùng để tính HP tối đa.</dd>
        <dt>Nội công</dt><dd>Tiềm năng dùng để tính MP tối đa và công của các lớp nội công.</dd>
        <dt>HP / MP tối đa</dt><dd>Trần máu và năng lượng. Chỉnh tăng trưởng thay đổi trần chỉ số; việc dùng thuốc và hồi phục vẫn theo cơ chế gameplay.</dd>
        <dt>Sát thương tối đa / tối thiểu</dt><dd>Khoảng công cơ bản dùng khi tính một đòn đánh. Sát thương cuối còn chịu ảnh hưởng của kỹ năng, chí mạng và các thuộc tính khác. Giá trị tối thiểu được giới hạn không vượt tối đa.</dd>
        <dt>Giảm sát thương</dt><dd>Điểm giảm sát thương nhận vào, được đưa vào công thức phòng thủ hiện có của game.</dd>
        <dt>Tốc độ di chuyển</dt><dd>Tốc độ theo thang nội bộ của game, giới hạn 0–17.</dd>
        <dt>Chính xác / né tránh</dt><dd>Điểm đầu vào để tính đánh trúng hoặc né. Tỷ lệ thực tế phụ thuộc đối thủ và thiết lập cân bằng chiến đấu.</dd>
        <dt>Chí mạng</dt><dd>Điểm xác suất chí mạng trong phép đánh hiện tại: 1000 tương ứng 100%.</dd>
        <dt>Sát thương chí mạng cộng</dt><dd>Lượng sát thương cộng thêm khi đòn đánh chí mạng.</dd>
        <dt>Sát thương chí mạng (%)</dt><dd>Phần trăm công cơ bản cộng thêm khi chí mạng. Xác suất xảy ra chí mạng được chỉnh ở thuộc tính Chí mạng phía trên.</dd>
        <dt>Kháng hỏa / băng / phong</dt><dd>Các điểm kháng tương ứng được dùng trong công thức nhận sát thương theo hệ của đối thủ.</dd>
      </dl>
    </details>
  `;
}

function pgClassName(id) {
  return id === 0 ? "Chưa vào lớp" : record("classes", id)?.name || `Lớp ${id}`;
}
function pgReferenceClass() {
  return pgScope === "0" ? 0 : pgScope === "classed" ? pgReference : Number(pgScope);
}
function pgProfile() {
  return pgDrafts[pgScope] ?? dashData.progression.profiles[pgScope] ?? {};
}
function pgFields() {
  return [...dashData.playerFields].sort((a,b) => Number(b.startsWith("Potential")) - Number(a.startsWith("Potential")));
}
function pgInput(key, field, rule) {
  const isBase = field === "base";
  const min = isBase ? (key === "MaxHp" || key === "MaxMp" ? 1 : 0) : field === "growthPercent" ? 0 : -10000;
  const max = isBase ? (key === "Speed" ? 17 : key.startsWith("Potential") ? 30000 : 100000000) : field === "growthPercent" ? 1000 : 10000;
  return `<input data-pg-stat="${key}" data-pg-field="${field}" aria-label="${esc(DASH_LABEL[key] || key)} · ${field}" type="number" min="${min}" max="${max}" step="${field === "growthFlat" ? ".01" : "1"}" value="${rule[field] ?? ""}" placeholder="Kế thừa">`;
}

function progressionView() {
  const cls = pgReferenceClass(), meta = dashData.progression;
  const original = meta.defaults[String(cls)].base, ownProfile = pgProfile();
  pgTo = Math.min(pgTo, meta.maxLevel);
  return `
    <form id="pgForm" class="${pgAdvanced ? "" : "progression-core-only"}">
      <section class="panel dashboard-live">
        <div class="panel-header"><div><div class="eyebrow">01 · NỀN CẤP 1</div><h2>Bắt đầu với chỉ số nào?</h2></div><span class="badge neutral">${esc(pgClassName(cls))}</span></div>
        <div class="form-grid">
          <label class="field">Phạm vi cấu hình<select id="pgScope">
            <option value="0" ${pgScope === "0" ? "selected" : ""}>Chưa vào lớp</option>
            <option value="classed" ${pgScope === "classed" ? "selected" : ""}>Đã vào lớp · cấu hình chung</option>
            ${meta.classIds.filter(id => id !== 0).map(id => `<option value="${id}" ${pgScope === String(id) ? "selected" : ""}>${esc(pgClassName(id))} · ghi đè riêng</option>`).join("")}
          </select></label>
          ${pgScope === "classed" ? `<label class="field">Lớp tham chiếu<select id="pgReference">${meta.classIds.filter(id => id !== 0).map(id => `<option value="${id}" ${pgReference === id ? "selected" : ""}>${esc(pgClassName(id))}</option>`).join("")}</select></label>` : ""}
        </div>
        <p class="description">Cột “Gốc” luôn là giá trị từ game chưa chỉnh cân bằng. Nhập giá trị cấp 1 mong muốn; để trống để dùng nền gốc hoặc kế thừa. Cột kết quả được server tính lại từ bản nháp. Khôi phục nhóm chỉ bỏ tham số của nhóm; hệ số nâng cao đã lưu vẫn được giữ.</p>
        <label class="progression-advanced-toggle"><input type="checkbox" id="pgAdvanced" ${pgAdvanced ? "checked" : ""}> Hiện thêm chính xác, né tránh, chí mạng, kháng và sát thương tối thiểu</label>
        <div class="table-box"><table class="data-table progression-edit"><thead><tr><th>Thuộc tính</th><th>Gốc cấp 1</th><th>Nền cấp 1 muốn dùng</th><th>Kết quả bản nháp</th><th></th></tr></thead><tbody>
          ${pgFields().map(key => `<tr class="${PG_CORE.has(key) ? "" : "pg-extra"}"><td><b>${esc(DASH_LABEL[key] || key)}</b><small class="muted">${key.startsWith("Potential") ? "Tiềm năng đầu vào" : "Chỉ số tính ra từ công thức"}</small></td><td>${fmt(original[key])}</td><td>${pgInput(key,"base",ownProfile[key] || {})}</td><td id="pgBase-${key}" class="progression-result">—</td><td><button type="button" class="text-button" data-pg-clear="${key}" aria-label="Bỏ cấu hình ${esc(DASH_LABEL[key] || key)}">↺</button></td></tr>`).join("")}
        </tbody></table></div>
      </section>

      <details class="panel dashboard-live progression-growth">
        <summary class="progression-growth-title"><span class="eyebrow">02 · TĂNG TRƯỞNG THEO CẤP</span><span class="progression-section-title">Mỗi cấp tăng như thế nào?</span></summary>
        ${progressionExplanation()}
        <div class="table-box"><table class="data-table progression-edit"><thead><tr><th>Thuộc tính & công thức gốc</th><th>Hệ số tăng trưởng (%)</th><th>Cộng thêm mỗi cấp</th><th>Công thức đang dùng</th></tr></thead><tbody>
          ${pgFields().map(key => `<tr class="${PG_CORE.has(key) ? "" : "pg-extra"}"><td><b>${esc(DASH_LABEL[key] || key)}</b><small class="muted">${esc(PG_NATIVE[key] || key)}${key.startsWith("Potential") ? ` · Tự tăng gốc: +${meta.defaults[String(cls)].potentialGrowth[Number(key.slice(-1))]}/cấp` : ""}</small></td><td>${pgInput(key,"growthPercent",ownProfile[key] || {})}</td><td>${pgInput(key,"growthFlat",ownProfile[key] || {})}</td><td id="pgFormula-${key}" class="progression-formula">Đang tính…</td></tr>`).join("")}
        </tbody></table></div>

      </details>
        <div class="form-footer progression-save"><div><button type="button" class="button" id="pgDiscard">Bỏ bản nháp</button><button type="button" class="button" id="pgReset">${pgScope === "0" || pgScope === "classed" ? "Khôi phục gốc của nhóm" : "Bỏ ghi đè, dùng cấu hình chung"}</button></div><div><span id="pgDraftState" class="muted">${own(pgDrafts, pgScope) ? "Có bản nháp chưa lưu" : "Đang xem cấu hình đã lưu"}</span><button type="submit" class="button primary" id="pgSave">Lưu & áp dụng phạm vi này</button></div></div>
    </form>

    <section class="panel dashboard-live">
      <div class="panel-header"><div><div class="eyebrow">03 · XEM TRƯỚC TIẾN TRÌNH</div><h2>So sánh qua từng cấp</h2></div><span class="badge green">Cùng hàm tính với gameplay</span></div>
      <div class="toolbar progression-range">
        <label>Từ cấp <input type="number" id="pgFrom" min="1" max="${meta.maxLevel}" step="1" value="${pgFrom}"></label>
        <label>Đến cấp <input type="number" id="pgTo" min="1" max="${meta.maxLevel}" step="1" value="${pgTo}"></label>
        <label><input type="checkbox" id="pgExpanded" ${pgExpanded ? "checked" : ""}> Hiện tất cả thuộc tính</label>
      </div>
      <div class="progression-scenarios">
        <fieldset><legend>Chưa vào lớp</legend><div class="progression-compare">
          <label><input type="checkbox" data-pg-compare="0" ${cls === 0 || pgCompare.includes(0) ? "checked" : ""} ${cls === 0 ? "disabled" : ""}> Không trang bị</label>
        </div></fieldset>
        <fieldset><legend>Kiếm gỗ</legend><div class="progression-compare">
          <label><input type="checkbox" id="pgWoodenSword" ${pgWoodenSword ? "checked" : ""}> Cầm kiếm gỗ nhiệm vụ</label>
        </div></fieldset>
        <fieldset><legend>Đã vào lớp</legend><div class="progression-compare">${meta.classIds.filter(id => id !== 0).map(id => `<label><input type="checkbox" data-pg-compare="${id}" ${id === cls || pgCompare.includes(id) ? "checked" : ""} ${id === cls ? "disabled" : ""}> ${esc(pgClassName(id))}</label>`).join("")}</div></fieldset>
      </div>
      <p class="description">Bản nháp chỉ áp dụng cho phạm vi đang chỉnh; các lớp khác dùng cấu hình đã lưu. Mẫu Kiếm gỗ là nhân vật chưa vào lớp cầm kiếm #${meta.woodenSwordItemId} nhận từ nhiệm vụ, tính cả luật cân bằng trang bị hiện tại. Các mẫu còn lại không trang bị; mọi mẫu chưa phân thêm điểm. Cấp 1 sau khi vào lớp là mốc lý thuyết để so sánh nền.</p>
      <div id="pgPreviewStatus" role="status" class="muted">Đang tính bản nháp…</div>
      <div id="pgPreview" class="progression-preview"></div>
    </section>

    <details class="panel dashboard-live"><summary>Thiết lập nâng cao: hệ số toàn game & EXP hiện có</summary><p class="description">Giữ lại cấu hình phiên bản trước để tương thích. Các hệ số này áp dụng trước lớp nền/tăng trưởng phía trên; thay đổi ở đây ảnh hưởng cả hai nhóm.</p>${dashPlayerViewLegacy()}</details>
  `;
}

function pgCollect() {
  const profile = {};
  for (const input of $("pgForm").querySelectorAll("[data-pg-stat]")) {
    if (input.value === "") continue;
    (profile[input.dataset.pgStat] ??= {})[input.dataset.pgField] = Number(input.value);
  }
  return profile;
}
function pgRemember() {
  if ($("pgForm")) pgDrafts[pgScope] = pgCollect();
}
function pgChanged() {
  pgRemember();
  $("pgDraftState").textContent = "Bản nháp · chưa lưu vào game";
  clearTimeout(pgTimer);
  ++pgSequence; // Invalidate the previous response immediately, not after the debounce expires.
  pgTimer = setTimeout(pgPreview, 180);
}
async function pgPreview() {
  if (dashTab !== "player" || routeName !== "dashboard" || !$("pgForm")) return;
  const sequence = ++pgSequence, scope = pgScope;
  const fromInput = $("pgFrom"), toInput = $("pgTo");
  if (!$("pgForm").checkValidity() || !fromInput.checkValidity() || !toInput.checkValidity() || fromInput.value === "" || toInput.value === "" || Number(fromInput.value) > Number(toInput.value)) {
    $("pgPreviewStatus").textContent = "Nhập chỉ số hợp lệ và khoảng cấp từ nhỏ đến lớn để xem trước.";
    return;
  }
  pgFrom = Number(fromInput.value); pgTo = Number(toInput.value);
  const classes = [...new Set([pgReferenceClass(), ...pgCompare])];
  $("pgPreviewStatus").textContent = "Server đang tính bản nháp…";
  try {
    const result = await request("/control/dashboard", {
      method: "POST", headers: {"Content-Type":"application/json", "X-Nso-Control":control.token},
      body: JSON.stringify({revision:dashData.revision,kind:"progressionPreview",scope,values:pgCollect(),from:pgFrom,to:pgTo,classes,includeWoodenSword:pgWoodenSword})
    });
    if (sequence !== pgSequence || scope !== pgScope || !$("pgPreview") || dashTab !== "player") return;
    pgLastPreview = result;
    const reference = result.bases[String(pgReferenceClass())];
    for (const key of pgFields()) {
      $("pgBase-"+key).textContent = fmt(reference.stats[key]);
      const r = reference.resolved[key];
      $("pgFormula-"+key).textContent = `${fmt(r.base)} + Δgốc(L) × ${r.growthPercent}% + (${fmt(r.growthFlat)}) × (L − 1) + đóng góp riêng`;
    }
    $("pgPreviewStatus").textContent = `Đã tính ${result.rows.length} dòng từ bản nháp · Chưa ghi cấu hình hoặc save nhân vật. Hàng tô màu: mốc cấp; huy hiệu đỏ: chỉ số giảm.`;
    pgRenderPreview();
  } catch (error) {
    if (sequence === pgSequence && $("pgPreviewStatus")) $("pgPreviewStatus").textContent = error.message;
  }
}
function pgRenderPreview() {
  if (!pgLastPreview || !$("pgPreview")) return;
  const columns = pgExpanded ? pgFields() : ["MaxHp","MaxMp","Damage","DameDown","Speed"];
  const order = row => row.scenario === "woodenSword" ? .5 : row.classId;
  const rows = [...pgLastPreview.rows].sort((a,b) => a.level-b.level || order(a)-order(b));
  const label = row => row.scenario === "woodenSword" ? "Kiếm gỗ · chưa vào lớp" : row.classId === 0 ? "Chưa vào lớp · không trang bị" : pgClassName(row.classId);
  $("pgPreview").innerHTML = `<div class="table-box"><table class="data-table"><thead><tr><th>Cấp</th><th>Nhóm / trường hợp</th>${columns.map(key=>`<th>${esc(DASH_LABEL[key] || key)}</th>`).join("")}<th>Δ HP/cấp</th><th>Δ công/cấp</th></tr></thead><tbody>${rows.map(row=>`<tr class="${[1,10,20,30,50,70,100,150,200].includes(row.level) ? "progression-milestone" : ""}"><td>${row.level}</td><td>${esc(label(row))}</td>${columns.map(key=>`<td>${fmt(row.stats[key])}</td>`).join("")}<td><span class="${row.deltaHp < 0 ? "badge red" : ""}">${fmt(row.deltaHp)}</span></td><td><span class="${row.deltaAttack < 0 ? "badge red" : ""}">${fmt(row.deltaAttack)}</span></td></tr>`).join("")}</tbody></table></div>`;
}

function progressionMount() {
  if ($("pgScope")) $("pgScope").onchange = event => {
    pgRemember(); ++pgSequence;
    pgScope = event.target.value;
    render();
  };
  $("pgScope").oninput = event => event.stopPropagation();
  if ($("pgReference")) $("pgReference").oninput = event => event.stopPropagation();
  if ($("pgReference")) $("pgReference").onchange = event => {
    pgRemember(); ++pgSequence; pgReference = Number(event.target.value); render();
  };
  $("pgForm").oninput = pgChanged;
  // Reveal a collapsed growth input before the browser tries to focus an invalid value.
  $("pgForm").addEventListener("invalid", event => {
    const growth = event.target.closest(".progression-growth");
    if (growth) growth.open = true;
  }, true);
  $("pgAdvanced").oninput = event => event.stopPropagation();
  $("pgAdvanced").onchange = event => {pgAdvanced=event.target.checked;$("pgForm").classList.toggle("progression-core-only",!pgAdvanced);};
  $("pgForm").onsubmit = event => {event.preventDefault();pgSave(false);};
  $("pgReset").onclick = () => pgSave(true);
  $("pgDiscard").onclick = () => {delete pgDrafts[pgScope];++pgSequence;render();};
  for (const button of document.querySelectorAll("[data-pg-clear]")) button.onclick = () => {
    for (const input of $("pgForm").querySelectorAll(`[data-pg-stat="${button.dataset.pgClear}"]`)) input.value = "";
    pgChanged();
  };
  for (const id of ["pgFrom","pgTo"]) $(id).oninput = pgChanged;
  $("pgWoodenSword").onchange = event => {pgWoodenSword=event.target.checked;pgChanged();};
  for (const input of document.querySelectorAll("[data-pg-compare]")) input.onchange = () => {
    pgCompare = [...document.querySelectorAll("[data-pg-compare]:checked")].map(x=>Number(x.dataset.pgCompare));
    pgChanged();
  };
  $("pgExpanded").onchange = event => {pgExpanded=event.target.checked;pgRenderPreview();};
  pgPreview();
}
async function pgSave(reset) {
  if (pgSaving || !$("pgForm") || (!reset && !$("pgForm").reportValidity())) return;
  const scope = pgScope, values = reset ? {} : pgCollect();
  pgSaving = true; ++pgSequence;
  for (const id of ["pgSave","pgReset"]) $(id).disabled = true;
  try {
    await request("/control/dashboard", {
      method:"POST",headers:{"Content-Type":"application/json","X-Nso-Control":control.token},
      body:JSON.stringify({revision:dashData.revision,kind:"progression",scope,values,reset})
    });
    delete pgDrafts[scope];
    toast(reset ? "Đã khôi phục phạm vi về gốc/kế thừa." : "Đã lưu tiến trình và áp dụng toàn game.");
    await dashLoad();
  } catch (error) { toast(error.message,true); }
  finally {pgSaving=false;for(const id of ["pgSave","pgReset"]) if($(id)) $(id).disabled=false;}
}
