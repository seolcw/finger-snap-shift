// Finger Snap Shift 웹 프로토타입 (5차) — Unity C# 이식을 염두에 둔 모듈 구성
// BoardModel / PlayerController / WordSystem / Perimeter / HandBrain / HandController / Game(조립 + 렌더)
// 핵심 동작은 Shift = 줄 돌리기. 플레이어는 Shift+방향키로 서 있는 행·열을 돌려 공격 단어를 한 줄로 맞추고,
// 손은 피해를 주는 공격 위주로 하되 가끔 줄을 섞어 방해한다. 글자 자체는 바뀌지 않는다(판의 글자는 늘 26자 그대로).
(function () {
  'use strict';
  const R = LayoutRules;
  const ROWS = R.ROWS, COLS = R.COLS;
  const CS = 50, BW = COLS * CS, BH = ROWS * CS, MARGIN = 46;
  const VIEW = { w: 980, h: 500 };
  const OX = [72, VIEW.w - 72 - BW], OY = 158;
  const CONFIG = Object.assign({}, ATTACK_DATA.config);
  const DEBUG = { invincible: false };
  const DIRS = { up: [-1, 0], down: [1, 0], left: [0, -1], right: [0, 1] };
  const KEYMAP = { KeyW: 'up', ArrowUp: 'up', KeyS: 'down', ArrowDown: 'down', KeyA: 'left', ArrowLeft: 'left', KeyD: 'right', ArrowRight: 'right' };
  const SLIDE_TIME = 0.24;   // 줄 돌리기 키 하나가 미끄러지는 시간 (기본값)
  const ALL_WORDS = ATTACK_DATA.phases.flatMap(p => p.words).concat(ATTACK_DATA.finisherWord);
  // 칸 번호. 슬롯이 열 -1, 7에 있어서 한 행에 16칸 폭을 잡는다.
  const KW = 16, key = (r, c) => r * KW + c + 4, unkey = kk => ({ r: Math.floor(kk / KW), c: kk % KW - 4 });
  const cellCenter = (b, r, c) => ({ x: OX[b] + c * CS + CS / 2, y: OY + r * CS + CS / 2 });
  const rand = (a, b) => a + Math.random() * (b - a);
  const randInt = (a, b) => Math.floor(rand(a, b + 1));
  const distToSeg = (p, a, b) => {
    const dx = b.x - a.x, dy = b.y - a.y, L = dx * dx + dy * dy;
    const t = L ? Math.max(0, Math.min(1, ((p.x - a.x) * dx + (p.y - a.y) * dy) / L)) : 0;
    return Math.hypot(p.x - (a.x + dx * t), p.y - (a.y + dy * t));
  };
  // 위치 Shift 연출: 예전 자리에서 새 자리로 미끄러진다. 끝을 넘은 키는 밖으로 튀었다가 반대편으로 들어온다.
  // carry: 이전 미끄러짐이 아직 남아 있으면 그 자리에서 이어서 움직인다(Shift+방향키를 누르고 있을 때 끊기지 않게)
  const makeSlide = (from, to, ux, uy, wrap, delay, dur, carry) => ({ px: (from.c - to.c) * CS + (wrap ? 0 : carry.dx), py: (from.r - to.r) * CS + (wrap ? 0 : carry.dy), ux, uy, wrap, delay, dur: dur || SLIDE_TIME, t: 0 });
  const slideOffset = s => {
    if (!s) return { dx: 0, dy: 0, alpha: 1 };
    const u = (s.t - s.delay) / s.dur;
    if (u <= 0) return { dx: s.px, dy: s.py, alpha: 1 };
    if (u >= 1) return { dx: 0, dy: 0, alpha: 1 };
    if (!s.wrap) { const e = 1 - (1 - u) * (1 - u); return { dx: s.px * (1 - e), dy: s.py * (1 - e), alpha: 1 }; }
    if (u < 0.5) return { dx: s.px + s.ux * CS * 1.6 * u * 2, dy: s.py + s.uy * CS * 1.6 * u * 2, alpha: 1 - u * 2 };
    const v = (u - 0.5) * 2; return { dx: -s.ux * CS * 1.6 * (1 - v), dy: -s.uy * CS * 1.6 * (1 - v), alpha: v };
  };

  // ───────────────────────── BoardModel ─────────────────────────
  // 마스크 + 칸마다 키 하나. 키 = { id, letter(빈 키는 null), slide(연출) }. 줄 돌리기는 키 객체가 칸 사이를 옮겨 다닌다.
  class BoardModel {
    constructor(index, mask, layout, slots) { this.index = index; this.mask = mask; this.layout = layout; this.slotDefs = slots; this.lines = R.lines(mask); this.reset(); }
    reset() {
      this.cells = []; let id = 0; this.version = 0;
      for (let r = 0; r < ROWS; r++) {
        const row = [];
        for (let c = 0; c < COLS; c++) {
          const m = this.mask[r][c], ch = this.layout[r][c];
          row.push(m === '.' ? null : { r, c, shift: m === 'S', key: m === 'S' ? null : { id: id++, letter: /[A-Z]/.test(ch) ? ch : null, slide: null } });
        }
        this.cells.push(row);
      }
      this.shiftCell = this.allCells().find(k => k.shift);
      // 공격 슬롯 칸: block = 담긴 공격 블록 { word, hp(손에게 맞을 수 있는 횟수), age, hitT } 또는 null. 페이즈가 바뀌면 비워진다.
      this.slots = this.slotDefs.map(d => ({ r: d.r, c: d.c, slot: true, shift: false, key: null, block: null }));
    }
    emptySlot() { return this.slots.find(s => !s.block) || null; }
    blocks() { return this.slots.filter(s => s.block).length; }
    letterAt(r, c) { const k = this.cell(r, c); return k && k.key ? k.key.letter : null; }
    // 이 판에서 단어가 가장 많이 맞춰진 자리 { axis, idx, start, count, cells }
    align(word) { const a = R.bestAlign(this.lines, (r, c) => this.letterAt(r, c), word); a.board = this.index; return a; }
    canRotate(axis, idx) { return this.lineCells(axis, idx).filter(k => !k.shift).length >= 2; }
    cell(r, c) { return r >= 0 && r < ROWS && c >= 0 && c < COLS ? this.cells[r][c] : (this.slots.find(s => s.r === r && s.c === c) || null); }
    allCells() { return this.cells.flat().filter(Boolean).concat(this.slots); }
    keyCells() { return this.allCells().filter(k => k.key); }
    cellOfKey(keyObj) { return this.keyCells().find(k => k.key === keyObj) || null; }
    lineCells(axis, idx) {
      const out = [];
      if (axis === 'row') { for (let c = 0; c < COLS; c++) if (this.cells[idx][c]) out.push(this.cells[idx][c]); }
      else for (let r = 0; r < ROWS; r++) if (this.cells[r][idx]) out.push(this.cells[r][idx]);
      return out;
    }
    area(r0, c0, h, w) { const out = []; for (let r = r0; r < r0 + h; r++) for (let c = c0; c < c0 + w; c++) if (this.cell(r, c)) out.push(this.cell(r, c)); return out; }
    // (r,c)에서 걸어서 가는 걸음 수. Map(key → dist)
    distFrom(r, c) {
      const d = new Map([[key(r, c), 0]]), q = [[r, c]];
      while (q.length) {
        const [cr, cc] = q.shift(), cd = d.get(key(cr, cc));
        for (const [dr, dc] of [[-1, 0], [1, 0], [0, -1], [0, 1]]) {
          const k = this.cell(cr + dr, cc + dc);
          if (k && !d.has(key(k.r, k.c))) { d.set(key(k.r, k.c), cd + 1); q.push([k.r, k.c]); }
        }
      }
      return d;
    }
    // 줄 돌리기(Shift): SHIFT 칸은 고정, 나머지 키가 sign(+1 = 오른쪽/아래) 방향으로 n칸 순환
    rotateLine(axis, idx, n, sign, step, dur) {
      const ks = this.lineCells(axis, idx).filter(k => !k.shift), L = ks.length; if (L < 2) return false;
      const old = ks.map(k => k.key), ux = axis === 'row' ? sign : 0, uy = axis === 'col' ? sign : 0;
      for (let i = 0; i < L; i++) {
        const raw = i + sign * n, j = ((raw % L) + L) % L;
        ks[j].key = old[i]; old[i].slide = makeSlide(ks[i], ks[j], ux, uy, raw < 0 || raw >= L, (sign > 0 ? i : L - 1 - i) * step, dur, slideOffset(old[i].slide));
      }
      this.version++;
      return true;
    }
    // 그 줄 위에 타고 있던 것(플레이어)이 같이 밀려 도착하는 칸과 연출 정보
    rideLine(axis, idx, r, c, n, sign, step) {
      const ks = this.lineCells(axis, idx).filter(k => !k.shift), L = ks.length, i = ks.findIndex(k => k.r === r && k.c === c);
      if (i < 0 || L < 2) return null;
      const raw = i + sign * n, to = ks[((raw % L) + L) % L];
      return { to, slide: makeSlide(ks[i], to, axis === 'row' ? sign : 0, axis === 'col' ? sign : 0, raw < 0 || raw >= L, (sign > 0 ? i : L - 1 - i) * step, 0, { dx: 0, dy: 0 }) };
    }
    update(dt) {
      for (const cell of this.keyCells()) { const k = cell.key; if (k.slide && (k.slide.t += dt) >= k.slide.delay + k.slide.dur) k.slide = null; }
      for (const s of this.slots) if (s.block) { s.block.age += dt; s.block.hitT = Math.max(0, s.block.hitT - dt); }
    }
  }

  // ───────────────────────── PlayerController ─────────────────────────
  class PlayerController {
    constructor(game) { this.game = game; this.held = []; this.shiftHeld = false; this.reset(); }
    reset() {
      this.board = 0; this.r = 2; this.c = 2; this.hp = CONFIG.playerHp;
      this.invuln = 0; this.shiftCd = 0; this.dwell = 0; this.boardTime = 0;
      this.lastDir = [0, 0]; this.sinceMove = 99; this.repeatT = 0; this.slide = null;
    }
    press(dir) {
      if (this.held.includes(dir)) return; this.held.push(dir); this.act(dir);
    }
    // Shift를 누른 채 방향키 = 서 있는 줄 돌리기(←→ 행, ↑↓ 열). Shift 없이 = 한 칸 이동. 누르고 있으면 반복.
    act(dir) {
      if (this.shiftHeld) { this.rotate(dir); this.repeatT = CONFIG.rotateRepeat; }
      else { this.tryMove(dir); this.repeatT = CONFIG.moveRepeat; }
    }
    release(dir) { this.held = this.held.filter(d => d !== dir); }
    here() { return this.game.boards[this.board].cell(this.r, this.c); }
    // 플레이어 Shift: 서 있는 행(←→) 또는 열(↑↓)을 그 방향으로 한 칸 돌린다. 플레이어는 칸에 그대로 있고 키만 지나간다.
    rotate(dir) {
      const g = this.game; if (g.state !== 'play') return false;
      const b = g.boards[this.board], cell = this.here(), [dr, dc] = DIRS[dir];
      const axis = dc !== 0 ? 'row' : 'col', idx = axis === 'row' ? this.r : this.c, sign = dc !== 0 ? dc : dr;
      if (cell.shift || cell.slot || !b.canRotate(axis, idx)) { g.fx.push({ kind: 'deny', b: this.board, r: this.r, c: this.c, t: 0.2 }); return false; }
      b.rotateLine(axis, idx, 1, sign, CONFIG.dominoStep, CONFIG.rotateSlide);
      g.fx.push({ kind: 'rotate', b: this.board, axis, idx, sign, t: 0.45, dur: 0.45 });
      g.stats.rotations = (g.stats.rotations || 0) + 1; this.dwell = 0;
      g.words.check();
      return true;
    }
    // Space: 슬롯 칸에 서 있으면 그 블록을 발동
    fire() {
      const cell = this.here(); if (this.game.state !== 'play' || !cell.slot) return false;
      if (!cell.block) { this.game.fx.push({ kind: 'deny', b: this.board, r: this.r, c: this.c, t: 0.2 }); return false; }
      return this.game.fireSlot(this.game.boards[this.board], cell);
    }
    update(dt) {
      this.invuln = Math.max(0, this.invuln - dt); this.shiftCd = Math.max(0, this.shiftCd - dt);
      this.dwell += dt; this.sinceMove += dt; this.boardTime += dt;
      if (this.slide && (this.slide.t += dt) >= this.slide.delay + this.slide.dur) this.slide = null;
      if (this.held.length && (this.repeatT -= dt) <= 0) this.act(this.held[this.held.length - 1]);
    }
    tryMove(dir) {
      if (this.game.state !== 'play') return false;
      const [dr, dc] = DIRS[dir], b = this.game.boards[this.board];
      if (!b.cell(this.r + dr, this.c + dc)) return false;
      this.r += dr; this.c += dc; this.lastDir = [dr, dc]; this.sinceMove = 0; this.dwell = 0; this.slide = null;
      this.enterCell(); return true;
    }
    enterCell() {
      const g = this.game, k = this.here();
      if (!k.shift || this.shiftCd > 0) return;
      const other = g.boards[1 - this.board], dest = other.shiftCell;
      g.fx.push({ kind: 'warp', b: this.board, r: this.r, c: this.c, t: 0.4 });
      this.board = other.index; this.r = dest.r; this.c = dest.c;
      this.shiftCd = CONFIG.shiftCooldown; this.invuln = Math.max(this.invuln, CONFIG.shiftInvuln);
      this.boardTime = 0; this.dwell = 0; g.stats.teleports++;
      g.fx.push({ kind: 'warp', b: this.board, r: this.r, c: this.c, t: 0.4 });
    }
    hurt(source) {
      if (DEBUG.invincible || this.invuln > 0 || this.game.state !== 'play') return false;
      this.hp--; this.invuln = CONFIG.hitInvuln; this.game.onPlayerHurt(source); return true;
    }
  }

  // ───────────────────────── WordSystem ─────────────────────────
  // 목표 단어를 한 줄(행은 왼→오, 열은 위→아래)의 연속한 칸에 맞추면 발동. 양쪽 키보드 어디든 된다.
  class WordSystem {
    constructor(game) { this.game = game; this.listCheck = R.checkWordList(ALL_WORDS, R.longestLine(R.MASK_LEFT)); this.reset(); }
    reset() { this.order = 0; this.current = null; this.next = null; this.current = this.pick(); this.next = this.pick(); this.onNewWord(); }
    // 새 목표 단어: 위치 힌트 점멸을 처음부터 다시(바로 한 번 깜빡인다)
    onNewWord() { this.lastCount = 0; this.game.hintT0 = this.game.time; }
    // 두 판 중 가장 많이 맞춰진 자리 (같으면 플레이어가 있는 판)
    best() {
      if (!this.current) return null;
      const pb = this.game.player.board; let best = null;
      for (const b of this.game.boards) { const a = b.align(this.current); if (!best || a.count > best.count || (a.count === best.count && b.index === pb)) best = a; }
      return best;
    }
    // 판이 바뀔 때마다(플레이어·손의 줄 돌리기) 부른다
    check() {
      const a = this.best(); if (!a) return;
      if (a.count === this.current.length) { this.complete(a); return; }
      // 제자리 글자가 늘었을 때만 그 칸들을 잠깐 초록으로 번쩍여 알려 준다(위치를 계속 보여 주지는 않는다)
      if (a.count > this.lastCount) this.game.fx.push({ kind: 'match', b: a.board, cells: a.cells.filter((k, i) => this.game.boards[a.board].letterAt(k.r, k.c) === this.current[i]).map(k => ({ r: k.r, c: k.c })), t: 0.5 });
      this.lastCount = a.count;
    }
    complete(a) {
      const done = this.current;
      this.current = this.next; this.next = this.pick(); this.onNewWord();
      this.game.onWordComplete(done, a);
    }
    finisherReady() { const g = this.game; return g.isLastPhase() && g.hand && g.hand.hp <= CONFIG.finisherHp; }
    // 페이즈 단어를 목록 순서대로(쉬운 것부터) 돌려 쓴다. 지금 목표와 같거나 이미 맞춰져 있는 단어는 건너뛴다.
    pick() {
      const words = this.game.phaseDef().words;
      for (let i = 0; i < words.length; i++) {
        const w = words[this.order++ % words.length];
        if (w !== this.current && this.game.boards.every(b => b.align(w).count < w.length)) return w;
      }
      return words[0];
    }
    // 마지막 페이즈에서 손 HP가 낮아지면 목표를 바로 마무리 단어로 바꾼다
    refresh() {
      const fin = ATTACK_DATA.finisherWord;
      if (this.finisherReady() && this.current !== fin) {
        // 슬롯에 남은 블록은 마무리 구간에서 쓸모가 없으므로(손 HP를 1 아래로 못 내림) 여기서 사라진다
        for (const b of this.game.boards) for (const s of b.slots) if (s.block) { this.game.fx.push({ kind: 'fizzle', b: b.index, r: s.r, c: s.c, t: 0.5 }); s.block = null; }
        this.current = fin; this.next = null; this.onNewWord(); this.game.toast('마무리! SHIFT를 한 줄로 맞춰라'); }
    }
  }

  // ───────────────────────── 손 이동 경로 (키보드 바깥 둘레) ─────────────────────────
  const Perimeter = {
    rect(b) { return { x0: OX[b] - MARGIN, y0: OY - MARGIN, x1: OX[b] + BW + MARGIN, y1: OY + BH + MARGIN }; },
    size(rc) { const w = rc.x1 - rc.x0, h = rc.y1 - rc.y0; return { w, h, P: 2 * (w + h) }; },
    paramOf(rc, p) {
      const { w, h } = this.size(rc);
      const x = Math.min(rc.x1, Math.max(rc.x0, p.x)), y = Math.min(rc.y1, Math.max(rc.y0, p.y));
      const d = [Math.abs(y - rc.y0), Math.abs(x - rc.x1), Math.abs(y - rc.y1), Math.abs(x - rc.x0)];
      const e = d.indexOf(Math.min(...d));
      return e === 0 ? x - rc.x0 : e === 1 ? w + (y - rc.y0) : e === 2 ? w + h + (rc.x1 - x) : 2 * w + h + (rc.y1 - y);
    },
    pointAt(rc, t) {
      const { w, h, P } = this.size(rc); t = ((t % P) + P) % P;
      if (t <= w) return { x: rc.x0 + t, y: rc.y0 };
      if (t <= w + h) return { x: rc.x1, y: rc.y0 + (t - w) };
      if (t <= 2 * w + h) return { x: rc.x1 - (t - w - h), y: rc.y1 };
      return { x: rc.x0, y: rc.y1 - (t - 2 * w - h) };
    },
    onRect(rc, tA, tB) {
      const { w, h, P } = this.size(rc), corners = [0, w, w + h, 2 * w + h];
      const cw = ((tB - tA) % P + P) % P, pts = [];
      if (cw <= P / 2) corners.map(ct => ((ct - tA) % P + P) % P).filter(d => d > 0.01 && d < cw).sort((a, b) => a - b).forEach(d => pts.push(this.pointAt(rc, tA + d)));
      else corners.map(ct => ((tA - ct) % P + P) % P).filter(d => d > 0.01 && d < P - cw).sort((a, b) => a - b).forEach(d => pts.push(this.pointAt(rc, tA - d)));
      pts.push(this.pointAt(rc, tB));
      return { pts, len: Math.min(cw, P - cw) };
    },
    nearest(b, p) { const rc = this.rect(b); return this.pointAt(rc, this.paramOf(rc, p)); },
    locate(p) { for (const b of [0, 1]) { const q = this.nearest(b, p); if (Math.hypot(q.x - p.x, q.y - p.y) < 1.5) return b; } return -1; },
    // from(둘레 위, 두 키보드 사이, 또는 판 위) → to(boardB 둘레 위) 경유점 목록
    path(from, boardB, to) {
      const boardA = this.locate(from), rb = this.rect(boardB);
      if (boardA === -1) {
        const r0 = this.rect(0), r1 = this.rect(1), inGap = from.x > r0.x1 && from.x < r1.x0;
        const via = from.x <= r0.x1 ? 0 : 1;       // 판 위에 있으면 먼저 그 판의 가장 가까운 둘레로 나간다
        const q = inGap ? { x: boardB === 0 ? rb.x1 : rb.x0, y: Math.abs(from.y - rb.y0) < Math.abs(from.y - rb.y1) ? rb.y0 : rb.y1 } : this.nearest(via, from);
        const rest = this.path(q, boardB, to);
        return { pts: [q, ...rest.pts], len: Math.hypot(q.x - from.x, q.y - from.y) + rest.len };
      }
      const ra = this.rect(boardA);
      if (boardA === boardB) return this.onRect(ra, this.paramOf(ra, from), this.paramOf(rb, to));
      let best = null;
      for (const top of [true, false]) {
        const exit = { x: boardA === 0 ? ra.x1 : ra.x0, y: top ? ra.y0 : ra.y1 };
        const entry = { x: boardB === 0 ? rb.x1 : rb.x0, y: exit.y };
        const p1 = this.onRect(ra, this.paramOf(ra, from), this.paramOf(ra, exit));
        const p2 = this.onRect(rb, this.paramOf(rb, entry), this.paramOf(rb, to));
        const len = p1.len + Math.abs(entry.x - exit.x) + p2.len;
        if (!best || len < best.len) best = { pts: [...p1.pts, entry, ...p2.pts], len };
      }
      return best;
    },
    lineAnchor(b, axis, idx, end) {
      return axis === 'row'
        ? { x: end === 0 ? OX[b] - MARGIN : OX[b] + BW + MARGIN, y: OY + idx * CS + CS / 2 }
        : { x: OX[b] + idx * CS + CS / 2, y: end === 0 ? OY - MARGIN : OY + BH + MARGIN };
    },
    // from에서 target 방향으로 쏜 직선이 boardB 둘레 사각형을 빠져나가는 점
    rayExit(b, from, target) {
      const rc = this.rect(b), dx = target.x - from.x, dy = target.y - from.y, L = Math.hypot(dx, dy) || 1, ux = dx / L, uy = dy / L;
      const tx = ux > 0 ? (rc.x1 - from.x) / ux : ux < 0 ? (rc.x0 - from.x) / ux : Infinity;
      const ty = uy > 0 ? (rc.y1 - from.y) / uy : uy < 0 ? (rc.y0 - from.y) / uy : Infinity;
      const t = Math.max(L, Math.min(tx, ty));
      return { x: Math.min(rc.x1, Math.max(rc.x0, from.x + ux * t)), y: Math.min(rc.y1, Math.max(rc.y0, from.y + uy * t)) };
    }
  };

  // ───────────────────────── HandBrain ─────────────────────────
  // 상황 점수 → 공정성 검사(BFS: 걸어서 피할 수 있는가) → 상위 후보 가중치 랜덤
  // 손은 피해 공격 위주. 줄 섞기(flick)만 판을 바꾸고, 쿨다운이 길어서 가끔만 나온다.
  const SHIFT_SAFE = { flick: true };   // 줄 섞기는 SHIFT 칸을 건드리지 않는다
  const ARROW = { row: { 1: '→', '-1': '←' }, col: { 1: '↓', '-1': '↑' } };
  class HandBrain {
    constructor(game) { this.game = game; this.lastType = null; this.cooldowns = {}; this.report = []; }
    reset() { this.lastType = null; this.cooldowns = {}; this.report = []; }
    update(dt) { for (const t in this.cooldowns) this.cooldowns[t] = Math.max(0, this.cooldowns[t] - dt); }
    warnTime(type) { return CONFIG.warnTime * ATTACK_DATA.attacks[type].warnMul * this.game.phaseDef().warnMul; }
    features() {
      const g = this.game, p = g.player, b = g.boards[p.board];
      let nb = 0; for (const [dr, dc] of Object.values(DIRS)) if (b.cell(p.r + dr, p.c + dc)) nb++;
      const dist = b.distFrom(p.r, p.c);
      // 플레이어가 이쪽 판에서 단어를 2글자 이상 맞춰 둔 줄
      const best = g.words.best(), prog = best && best.board === p.board && best.count >= 2 ? best : null;
      return { p, b, corner: nb <= 2, nb, dShift: dist.get(key(b.shiftCell.r, b.shiftCell.c)), dwell: p.dwell, moving: p.sinceMove < 0.35, dir: p.lastDir, prog };
    }
    // 주어진 시간 안에 걸어서 닿을 수 있는 안전한 칸(또는 쓸 수 있는 SHIFT 칸)이 하나라도 있는가
    canEscape(boardIndex, cells, time, shiftSafe) {
      const g = this.game, p = g.player, b = g.boards[boardIndex];
      if (p.board !== boardIndex) return true;
      const danger = new Set(); for (const k of cells) if (!(k.shift && shiftSafe)) danger.add(key(k.r, k.c));
      if (!danger.has(key(p.r, p.c))) return true;
      const steps = Math.max(1, Math.floor((time - CONFIG.reaction) / CONFIG.moveRepeat));
      for (const [kk, d] of b.distFrom(p.r, p.c)) {
        if (d === 0 || d > steps) continue;
        const u = unkey(kk); if (b.cell(u.r, u.c).shift && p.shiftCd <= d * CONFIG.moveRepeat + CONFIG.reaction) return true;
        if (!danger.has(kk)) return true;
      }
      return false;
    }
    aimCells(b, from, to) { return b.allCells().filter(k => distToSeg(cellCenter(b.index, k.r, k.c), from, to) < CS * 0.62); }
    choose() {
      const g = this.game, f = this.features(), p = f.p, b = f.b, ph = g.phaseDef(), hand = g.hand, phaseKey = String(g.phase + 1);
      const allowed = ph.attacks, cands = [], pc = cellCenter(b.index, p.r, p.c);
      const def = type => ATTACK_DATA.attacks[type];
      const base = type => def(type).base;
      const travel = (anchor, board) => Perimeter.path(hand.pos, board, anchor).len / (CONFIG.handSpeed * ph.speedMul);
      const push = (type, target, cells, anchor, score, why, extra) => {
        const c = Object.assign({ type, name: def(type).name, board: b.index, target, cells, anchor, warn: this.warnTime(type), why, badge: '' }, extra || {});
        c.travel = travel(anchor, c.board);
        c.score = Math.round((score - c.travel * 1.5) * 10) / 10;
        c.cooling = (this.cooldowns[type] || 0) > 0; c.repeat = type === this.lastType;
        c.fair = this.canEscape(c.board, cells, c.warn, SHIFT_SAFE[type]);
        cands.push(c);
      };
      const dwellBonus = Math.min(f.dwell, 3) * 1.5;
      // ── 줄 공격: 노릴 줄 후보 ──
      const lines = [];
      const addLine = (axis, idx, w, tag, progLine) => {
        const ex = lines.find(l => l.axis === axis && l.idx === idx);
        if (ex) { ex.w = Math.max(ex.w, w) + 0.15; ex.tags.push(tag); ex.progLine = ex.progLine || progLine; } else lines.push({ axis, idx, w, tags: [tag], progLine });
      };
      addLine('row', p.r, 1.0, '플레이어 행', false); addLine('col', p.c, 1.0, '플레이어 열', false);
      if (f.prog) {
        // 맞춰 둔 줄을 가로지르는 줄을 섞으면 맞춘 글자 하나가 빠진다
        const word = g.words.current, hit = f.prog.cells.filter((k, i) => b.letterAt(k.r, k.c) === word[i]), k = hit[Math.floor(hit.length / 2)];
        const cross = f.prog.axis === 'row' ? ['col', k.c] : ['row', k.r];
        addLine(cross[0], cross[1], 0.9, `맞춘 줄(${f.prog.count}/${word.length}) 가로지르기`, true);
      }
      if (f.dShift <= 3 && p.shiftCd < 1) addLine('row', b.shiftCell.r, 1.1, `SHIFT 근처(${f.dShift}칸)`, false);
      for (const l of lines) {
        const cells = b.lineCells(l.axis, l.idx), movable = cells.filter(k => !k.shift).length;
        const onLine = l.axis === 'row' ? l.idx === p.r : l.idx === p.c;
        const along = f.moving && ((l.axis === 'row' && f.dir[1] !== 0) || (l.axis === 'col' && f.dir[0] !== 0));
        const a0 = Perimeter.lineAnchor(b.index, l.axis, l.idx, 0), a1 = Perimeter.lineAnchor(b.index, l.axis, l.idx, 1);
        const end = travel(a0, b.index) <= travel(a1, b.index) ? 0 : 1, anchor = end === 0 ? a0 : a1;
        const target = `${l.axis === 'row' ? '행' : '열'}${l.idx}`, db = onLine ? dwellBonus : 0;
        const why = l.tags.join(', ') + (along && onLine ? ', 줄 따라 이동 중' : '') + (db > 1.5 ? `, 정지 ${f.dwell.toFixed(1)}초` : '');
        const ex = { axis: l.axis, idx: l.idx, end };
        if (allowed.includes('flick') && movable >= 2) {
          const n = randInt(def('flick').shiftMin, Math.min(def('flick').shiftMax, movable - 1)), sign = end === 0 ? 1 : -1;
          push('flick', target, cells, anchor, base('flick') * l.w + (onLine ? 1 : 0) + (l.progLine ? 2 + f.prog.count : 0), why + (l.progLine ? ' → 맞춘 글자 흐트러뜨리기' : ''), Object.assign({ n, sign, badge: `${ARROW[l.axis][sign]}${n}` }, ex));
        }
        if (allowed.includes('summon')) push('summon', target, cells, anchor, base('summon') * l.w + (onLine ? 2 : 0) + (along && onLine ? 2 : 0) + db, why, ex);
        if (allowed.includes('charge')) push('charge', target, cells, anchor, base('charge') * l.w + (onLine ? 2 : 0) + (f.corner ? 2 : 0) + db, why, ex);
      }
      // ── 판 위로 들어오는 공격 (조준·잠금 시점에 공정성 재검사) ──
      const entry = Perimeter.locate(hand.pos) === b.index ? { x: hand.pos.x, y: hand.pos.y } : Perimeter.nearest(b.index, hand.pos);
      if (allowed.includes('aim')) {
        const d = def('aim'), chain = randInt(d.chainMin[phaseKey] || 1, d.chainMax[phaseKey] || 1), exit = Perimeter.rayExit(b.index, entry, pc);
        push('aim', chain > 1 ? `${chain}연속` : '1회', this.aimCells(b, entry, exit), entry, base('aim') + (f.moving ? 2 : 0) + dwellBonus + (chain - 1) * 1.5, (f.moving ? '이동 중인 플레이어' : '플레이어') + ' 조준' + (dwellBonus > 1.5 ? `, 정지 ${f.dwell.toFixed(1)}초` : ''), { chain, recheck: true });
      }
      if (allowed.includes('track')) push('track', '2x2', [], Perimeter.nearest(b.index, pc), base('track') + (f.corner ? 5 : 0), f.corner ? '구석에 몰림' : '플레이어 추적', { recheck: true });
      if (allowed.includes('click')) {
        const d = def('click'), dbl = Math.random() < (d.doubleChance[phaseKey] || 0), stay = f.dwell > 0.8 ? Math.min(f.dwell, 3) * 3 : 0;
        push('click', dbl ? '더블클릭' : '클릭', [b.cell(p.r, p.c)], Perimeter.nearest(b.index, pc), base('click') + stay + (dbl ? 1 : 0), stay ? `정지 ${f.dwell.toFixed(1)}초 → 그 칸 정밀 타격` : '플레이어 칸', { double: dbl, recheck: true });
      }
      if (allowed.includes('drag')) {
        // 플레이어를 포함하는 사각형 하나
        const shapes = [[2, 3], [3, 2], [2, 2]], opts = [];
        for (const [h, w] of shapes) for (let r0 = p.r - h + 1; r0 <= p.r; r0++) for (let c0 = p.c - w + 1; c0 <= p.c; c0++) {
          const cells = b.area(r0, c0, h, w); if (cells.length >= h * w - 1) opts.push({ r0, c0, h, w, cells });
        }
        if (opts.length) {
          const o = opts[randInt(0, opts.length - 1)];
          push('drag', `${o.w}x${o.h}`, o.cells, Perimeter.nearest(b.index, cellCenter(b.index, o.r0, o.c0)), base('drag') + (f.corner ? 2 : 0), '플레이어 주변 선택', { rectSel: o });
        }
      }
      if (allowed.includes('full')) {
        const top = { x: OX[b.index] + BW / 2, y: OY - MARGIN };
        push('full', '키보드 전체', b.allCells(), top, base('full') + Math.min(p.boardTime, 18) * 0.4, `이쪽 체류 ${p.boardTime.toFixed(0)}초 → SHIFT 칸으로 피하게 만들기`, { center: { x: OX[b.index] + BW / 2, y: OY + BH / 2 } });
      }
      // ── 슬롯 깨기: 공격 블록이 든 슬롯을 친다(어느 판이든, 플레이어가 없어도). 블록이 많이 쌓인 판일수록 가산.
      if (allowed.includes('slot')) for (const bb of g.boards) for (const sl of bb.slots) {
        if (!sl.block) continue;
        const center = cellCenter(bb.index, sl.r, sl.c), side = bb.index === 0 ? '왼쪽' : '오른쪽';
        push('slot', `${side} 슬롯 ${sl.block.word}`, [sl], Perimeter.nearest(bb.index, center), base('slot') + bb.blocks() * 3 + (sl.block.hp < CONFIG.blockHp ? 2 : 0), `${side} 판 블록 ${bb.blocks()}개 → 깨뜨리기`, { board: bb.index, slot: sl, center });
      }
      // 선택: 쿨다운·연속·불공정 제외 → 상위 4개 가중치 랜덤
      const usable = cands.filter(c => !c.cooling && !c.repeat && c.fair && c.score > 0).sort((a, b2) => b2.score - a.score).slice(0, 4);
      let chosen = null;
      if (usable.length) {
        const ws = usable.map(c => c.score * c.score); let x = Math.random() * ws.reduce((s, w) => s + w, 0);
        for (let i = 0; i < usable.length; i++) { x -= ws[i]; if (x <= 0) { chosen = usable[i]; break; } }
        chosen = chosen || usable[0];
      }
      cands.sort((a, b2) => b2.score - a.score);
      this.report = cands.map(c => ({ mark: c === chosen ? '▶' : !c.fair ? '✕' : c.cooling ? '⏳' : c.repeat ? '↺' : ' ', text: `${c.name} ${c.target} ${c.badge}`, score: c.score, why: c.why, note: !c.fair ? '불공정(피할 칸 없음)' : c.cooling ? '쿨다운' : c.repeat ? '연속 금지' : '' }));
      this.reportFeat = `플레이어 ${b.index === 0 ? '왼쪽' : '오른쪽'} (${p.r},${p.c}) · 이웃 ${f.nb}${f.corner ? ' 구석' : ''} · SHIFT까지 ${f.dShift} · 정지 ${f.dwell.toFixed(1)}초 · 맞춘 줄 ${f.prog ? `${f.prog.axis === 'row' ? '행' : '열'}${f.prog.idx} ${f.prog.count}글자` : '-'}`;
      if (chosen) { this.lastType = chosen.type; this.cooldowns[chosen.type] = def(chosen.type).cooldown; }
      return chosen;
    }
  }

  // ───────────────────────── HandController ─────────────────────────
  // idle → travel(이동이 곧 예고) → warn(빨간 테두리/조준선/그림자 + Shift 양 표시) → exec → idle
  // anim: Unity 손 애니메이션 상태 이름(FlickCharge → FlickRelease 등)에 대응
  class HandController {
    constructor(game) { this.game = game; this.reset(); }
    reset() {
      this.hp = this.maxHp(); this.pos = { x: OX[0] + BW / 2, y: OY - MARGIN };
      this.state = 'idle'; this.t = 1.6; this.atk = null; this.path = []; this.bullets = []; this.pendingStun = 0; this.execT = 0; this.charging = false;
      this.anim = 'Idle'; this.animT = 0; this.animDir = { x: 0, y: 0 };
    }
    maxHp() { return this.game.phaseDef().handHp; }
    speed() { return CONFIG.handSpeed * this.game.phaseDef().speedMul; }
    setAnim(name, dir) { this.anim = name; this.animT = 0; if (dir) this.animDir = dir; }
    moveAlong(dt, speed) {
      let left = speed * dt;
      while (left > 0 && this.path.length) {
        const n = this.path[0], dx = n.x - this.pos.x, dy = n.y - this.pos.y, d = Math.hypot(dx, dy);
        if (d <= left) { this.pos = { x: n.x, y: n.y }; this.path.shift(); left -= d; }
        else { this.pos.x += dx / d * left; this.pos.y += dy / d * left; left = 0; }
      }
      return this.path.length === 0;
    }
    stun(sec) {
      if (this.state === 'exec') { this.pendingStun = sec; return; }
      this.atk = null; this.path = []; this.state = 'stun'; this.t = sec; this.setAnim('Stun');
    }
    toIdle(shortWait) {
      this.atk = null; this.charging = false;
      if (this.pendingStun > 0) { this.state = 'stun'; this.t = this.pendingStun; this.pendingStun = 0; this.setAnim('Stun'); return; }
      // 플레이어가 있는 판에 블록이 많이 쌓여 있을수록(쏘러 온 것) 더 자주 공격한다
      const g = this.game, stack = g.boards[g.player.board].blocks();
      this.state = 'idle'; this.t = shortWait ? 0.3 : CONFIG.attackInterval * g.phaseDef().intervalMul * Math.max(0.4, 1 - CONFIG.blockAggro * stack);
    }
    update(dt) {
      const g = this.game, p = g.player;
      this.animT += dt; if (this.anim === 'FlickRelease' && this.animT > 0.16) this.setAnim('Idle');
      this.updateBullets(dt);
      if (this.state === 'stun') { this.t -= dt; if (this.t <= 0) { this.state = 'idle'; this.t = 0.3; this.setAnim('Idle'); } return; }
      if (this.state === 'idle') {
        // 쉬는 동안 플레이어 쪽 둘레로 천천히 따라간다
        const tgt = Perimeter.nearest(p.board, cellCenter(p.board, p.r, p.c));
        this.path = Perimeter.path(this.pos, p.board, tgt).pts;
        this.moveAlong(dt, this.speed() * 0.6);
        this.t -= dt;
        if (this.t <= 0) {
          const atk = g.brain.choose();
          if (!atk) { this.t = 0.4; return; }
          this.atk = atk; this.path = Perimeter.path(this.pos, atk.board, atk.anchor).pts; this.state = 'travel';
        }
        return;
      }
      if (this.state === 'travel') { if (this.moveAlong(dt, this.speed())) this.startWarn(); return; }
      if (this.state === 'warn') { this.updateWarn(dt); return; }
      if (this.state === 'exec') this.updateExec(dt);
    }

    // ── 예고 시작: 조준/잠금이 필요한 공격은 여기서 대상을 정하고 공정성을 다시 본다 ──
    startWarn() {
      const g = this.game, a = this.atk, p = g.player;
      this.state = 'warn'; this.t = a.warn;
      if (a.recheck && p.board !== a.board) return this.toIdle(true);
      if (a.type === 'aim' && !this.lockAim(a.warn)) return this.toIdle(true);
      if (a.type === 'click' && !this.lockClick(a.warn)) return this.toIdle(true);
      if (a.type === 'slot') { if (!a.slot.block) return this.toIdle(true); this.path = [a.center]; }
      if (a.type === 'track') { a.stage = 'track'; this.t = ATTACK_DATA.attacks.track.trackTime; }
      if (a.type === 'drag') { const o = a.rectSel; a.from = cellCenter(a.board, o.r0, o.c0); a.to = cellCenter(a.board, o.r0 + o.h - 1, o.c0 + o.w - 1); this.path = [a.from]; }
      if (a.type === 'flick') this.setAnim('FlickCharge', a.axis === 'row' ? { x: a.sign, y: 0 } : { x: 0, y: a.sign });
    }
    lockAim(time) {
      const g = this.game, a = this.atk, b = g.boards[a.board], p = g.player;
      a.from = { x: this.pos.x, y: this.pos.y }; a.to = Perimeter.rayExit(a.board, a.from, cellCenter(p.board, p.r, p.c));
      a.cells = g.brain.aimCells(b, a.from, a.to); a.warn = time; a.done = new Set();
      return g.brain.canEscape(a.board, a.cells, time, false);
    }
    lockClick(time) {
      const g = this.game, a = this.atk, b = g.boards[a.board], p = g.player;
      const k = b.cell(p.r, p.c);
      a.cells = [k]; a.center = cellCenter(a.board, k.r, k.c); this.path = [a.center];
      return g.brain.canEscape(a.board, a.cells, time, false);
    }
    updateWarn(dt) {
      const g = this.game, a = this.atk, b = g.boards[a.board], p = g.player, def = ATTACK_DATA.attacks[a.type];
      this.t -= dt;
      if (a.recheck && p.board !== a.board) return this.toIdle(true);          // 플레이어가 반대편으로 넘어가면 판 위 공격은 취소
      if (a.type === 'click' || a.type === 'slot') this.moveAlong(dt, this.speed() * 1.6);
      if (a.type === 'slot' && !a.slot.block) return this.toIdle(true);   // 플레이어가 먼저 쏴 버리면 취소
      if (a.type === 'drag') {
        if (this.path.length) { if (this.moveAlong(dt, this.speed() * 1.6)) a.dragT = this.t; }
        else { const u = a.dragT > 0.05 ? Math.min(1, 1 - (this.t - a.dragT * 0.25) / (a.dragT * 0.75)) : 1; this.pos = { x: a.from.x + (a.to.x - a.from.x) * Math.max(0, u), y: a.from.y + (a.to.y - a.from.y) * Math.max(0, u) }; }
      }
      if (a.type === 'track' && a.stage === 'track') {
        // 그림자가 플레이어를 따라다닌다 (플레이어보다 느림)
        const c = cellCenter(p.board, p.r, p.c), tx = c.x + CS * 0.25, ty = c.y + CS * 0.25, dx = tx - this.pos.x, dy = ty - this.pos.y, d = Math.hypot(dx, dy), step = def.followSpeed * g.phaseDef().speedMul * dt;
        if (d <= step) this.pos = { x: tx, y: ty }; else { this.pos.x += dx / d * step; this.pos.y += dy / d * step; }
        if (this.t <= 0) {
          const cr = Math.round((this.pos.y - OY) / CS), cc = Math.round((this.pos.x - OX[a.board]) / CS);
          a.cells = b.area(cr - 1, cc - 1, 2, 2); a.stage = 'lock'; this.t = a.warn * def.lockMul;
          if (!a.cells.length || !g.brain.canEscape(a.board, a.cells, this.t, false)) return this.toIdle(true);
        }
        return;
      }
      if (this.t <= 0) this.fire();
    }
    fire() {
      const g = this.game, a = this.atk, b = g.boards[a.board], p = g.player, def = ATTACK_DATA.attacks[a.type];
      const playerIn = () => p.board === a.board && a.cells.some(k => k.r === p.r && k.c === p.c);
      const count = () => { g.stats.attacks[a.type] = (g.stats.attacks[a.type] || 0) + 1; };
      if (a.type === 'flick') {
        // 줄 섞기: 회전 밀기. 줄 위의 플레이어는 피해를 받고 키와 함께 밀린다.
        count(); const step = CONFIG.dominoStep;
        if (p.board === a.board && !b.cell(p.r, p.c).shift && a.cells.some(k => k.r === p.r && k.c === p.c)) {
          p.hurt(def.name); const ride = b.rideLine(a.axis, a.idx, p.r, p.c, a.n, a.sign, step);
          if (ride) { p.r = ride.to.r; p.c = ride.to.c; p.slide = ride.slide; p.dwell = 0; }
        }
        b.rotateLine(a.axis, a.idx, a.n, a.sign, step);
        g.stats.lastFlick = { axis: a.axis, idx: a.idx, n: a.n, sign: a.sign };
        this.setAnim('FlickRelease'); this.toIdle();
        g.words.check();   // 손이 섞다가 우연히 단어가 맞춰지면 그것도 발동
      } else if (a.type === 'summon') {
        count(); const len = a.axis === 'row' ? COLS : ROWS;
        this.bullets.push({ board: a.board, axis: a.axis, idx: a.idx, pos: a.end === 0 ? -0.8 : len - 0.2, dir: a.end === 0 ? 1 : -1, speed: def.bulletSpeed * g.phaseDef().speedMul });
        this.toIdle();
      } else if (a.type === 'full') {
        count(); if (playerIn()) p.hurt(def.name);
        g.fx.push({ kind: 'impact', b: a.board, cells: a.cells.map(k => ({ r: k.r, c: k.c })), t: 0.3 });
        g.shake = 0.25; this.state = 'exec'; this.execT = 0;
      } else if (a.type === 'charge') {
        count(); this.state = 'exec'; this.charging = true; a.done = new Set(); this.path = [Perimeter.lineAnchor(a.board, a.axis, a.idx, 1 - a.end)];
      } else if (a.type === 'aim') {
        if (!a.counted) { count(); a.counted = true; }
        g.stats.aimDashes = (g.stats.aimDashes || 0) + 1; this.state = 'exec'; this.charging = true; this.path = [a.to];
      } else if (a.type === 'track') {
        count(); if (playerIn()) p.hurt(def.name);
        g.fx.push({ kind: 'impact', b: a.board, cells: a.cells.map(k => ({ r: k.r, c: k.c })), t: 0.3 }); g.shake = 0.2; this.toIdle();
      } else if (a.type === 'slot') {
        // 슬롯 깨기: 블록 내구도 -1(흔들림), 0이 되면 블록이 사라진다. 슬롯 위의 플레이어도 맞는다.
        count(); if (playerIn()) p.hurt(def.name);
        const blk = a.slot.block;
        if (blk) { blk.hp--; blk.hitT = 0.4; if (blk.hp <= 0) { a.slot.block = null; g.fx.push({ kind: 'fizzle', b: a.board, r: a.slot.r, c: a.slot.c, t: 0.5 }); g.toast(`${blk.word} 블록이 깨졌다`); g.stats.blocksLost = (g.stats.blocksLost || 0) + 1; } }
        g.fx.push({ kind: 'click', x: a.center.x, y: a.center.y, t: 0.25 }); g.shake = 0.15; this.toIdle();
      } else if (a.type === 'click') {
        if (!a.second) count();
        if (playerIn()) p.hurt(a.double ? '더블클릭' : '클릭');
        g.fx.push({ kind: 'click', x: a.center.x, y: a.center.y, t: 0.25 });
        if (a.double && !a.second && p.board === a.board && this.lockClick(def.doubleGap)) { a.second = true; g.stats.doubleClicks = (g.stats.doubleClicks || 0) + 1; this.state = 'warn'; this.t = def.doubleGap; a.warn = def.doubleGap; }
        else this.toIdle();
      } else if (a.type === 'drag') {
        count(); if (playerIn()) p.hurt(def.name);
        g.fx.push({ kind: 'impact', b: a.board, cells: a.cells.map(k => ({ r: k.r, c: k.c })), t: 0.3 }); this.toIdle();
      }
    }
    updateExec(dt) {
      const g = this.game, a = this.atk, b = g.boards[a.board], p = g.player;
      this.execT += dt;
      if (a.type === 'charge' || a.type === 'aim') {
        const def = ATTACK_DATA.attacks[a.type];
        // 터널링 방지: 한 프레임을 잘게 나눠서 본체 충돌을 본다
        const speed = def.chargeSpeed * g.phaseDef().speedMul, sub = Math.max(1, Math.ceil(speed * dt / (CS * 0.3)));
        let arrived = false;
        for (let i = 0; i < sub && !arrived; i++) {
          arrived = this.moveAlong(dt / sub, speed);
          const c = cellCenter(p.board, p.r, p.c);
          if (p.board === a.board && Math.hypot(this.pos.x - c.x, this.pos.y - c.y) < CS * 0.55) p.hurt(def.name);
        }
        if (!arrived) return;
        this.charging = false;
        if (a.type === 'aim' && --a.chain > 0 && p.board === a.board && g.state === 'play') {
          const w = a.warn * def.chainWarnMul;
          if (this.lockAim(w)) { this.state = 'warn'; this.t = w; return; }
        }
        this.toIdle();
      } else {   // full: 가운데를 찍고 돌아온다
        const T = 0.36, u = Math.min(1, this.execT / T), s = u < 0.4 ? u / 0.4 : 1 - (u - 0.4) / 0.6;
        this.pos = { x: a.anchor.x + (a.center.x - a.anchor.x) * s, y: a.anchor.y + (a.center.y - a.anchor.y) * s };
        if (u >= 1) { this.pos = { x: a.anchor.x, y: a.anchor.y }; this.toIdle(); }
      }
    }
    updateBullets(dt) {
      const g = this.game, p = g.player;
      for (const bl of this.bullets) {
        bl.pos += bl.dir * bl.speed * dt;
        const i = Math.round(bl.pos), b = g.boards[bl.board];   // 탄이 지나가는 칸
        const r = bl.axis === 'row' ? bl.idx : i, c = bl.axis === 'row' ? i : bl.idx, k = b.cell(r, c);
        // 플레이어를 맞히면 탄은 거기서 사라진다
        if (k && p.board === bl.board && p.r === r && p.c === c && p.hurt('소환')) { bl.dead = true; continue; }
        const len = bl.axis === 'row' ? COLS : ROWS; bl.dead = bl.pos < -1 || bl.pos > len;
      }
      this.bullets = this.bullets.filter(bl => !bl.dead);
    }
  }

  // ───────────────────────── Game (조립, 상태, 렌더) ─────────────────────────
  const COL = { bg: '#14161c', slot: '#0f1116', key: '#3a3f4d', keyHi: '#4a5062', keyDown: '#2a2e39', keyEdge: '#252832', text: '#e6e8ee', dim: '#8a90a0', warn: '#ff4d4d', glow: '#ffd24a', shift: '#3d7bff', shiftHi: '#7fa8ff', player: '#6ff0c0', ok: '#5fe08a', hand: '#d9dce6', bullet: '#ffa23a', hp: '#ff5d73' };
  const FONT = '"Space Grotesk", "Segoe UI", system-ui, sans-serif', MONO = '"IBM Plex Mono", Consolas, monospace';

  class Game {
    constructor(canvas) {
      this.canvas = canvas; this.ctx = canvas.getContext('2d');
      this.boards = [new BoardModel(0, R.MASK_LEFT, LAYOUT_DATA.left, R.SLOTS_LEFT), new BoardModel(1, R.MASK_RIGHT, LAYOUT_DATA.right, R.SLOTS_RIGHT)];
      this.phase = 0; this.state = 'ready'; this.time = 0; this.fx = []; this.shake = 0; this.toastMsg = null; this.stateT = 0;
      this.stats = { teleports: 0, wordsDone: 0, hits: {}, attacks: {}, deaths: 0, rotations: 0 };
      this.player = new PlayerController(this); this.hand = new HandController(this);
      this.brain = new HandBrain(this); this.words = new WordSystem(this);
      this.reducedMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    }
    phaseDef() { return ATTACK_DATA.phases[this.phase]; }
    // 단어 위치 힌트 세기(0~1). hintInterval초마다 hintDuration초 동안 두 번 깜빡이고, 나머지 시간에는 0.
    hintAlpha() {
      if (!this.words.current) return 0;
      const local = (this.time - (this.hintT0 || 0)) % CONFIG.hintInterval;
      if (local > CONFIG.hintDuration) return 0;
      return Math.sin(((local / CONFIG.hintDuration * 2) % 1) * Math.PI);
    }
    isLastPhase() { return this.phase === ATTACK_DATA.phases.length - 1; }
    // handHp를 주면 손 HP를 그 값으로 시작한다(사망 후 재시작)
    startPhase(n, handHp) {
      this.phase = n; this.boards.forEach(b => b.reset());
      this.player.reset(); this.hand.reset(); if (handHp !== undefined) this.hand.hp = handHp;
      this.brain.reset(); this.words.reset(); this.words.refresh();
      this.fx = []; this.lastFire = null; this.state = 'play'; this.banner = { text: `PHASE ${n + 1}`, t: 1.4 };
    }
    restartPhase() { this.startPhase(this.state === 'win' ? 0 : this.phase); }
    skipPhase() { if (!this.isLastPhase()) this.startPhase(this.phase + 1); else { this.state = 'win'; this.stateT = 0; } }
    toast(text) { this.toastMsg = { text, t: 1.8 }; }
    onPlayerHurt(source) {
      this.stats.hits[source] = (this.stats.hits[source] || 0) + 1; this.shake = 0.3;
      const c = cellCenter(this.player.board, this.player.r, this.player.c);
      this.fx.push({ kind: 'float', x: c.x, y: c.y - 20, text: `-1 ${source}`, color: COL.hp, t: 0.9 });
      if (this.player.hp <= 0) { this.state = 'dead'; this.stateT = 1.3; this.stats.deaths++; }
    }
    // 단어 완성. a는 맞춘 자리({ board, cells }).
    // 보통 단어는 바로 쏘지 않고 공격 블록이 되어 **반대편 판**의 빈 슬롯에 들어간다(양쪽 판을 오가게 만드는 장치).
    // 반대편 슬롯이 꽉 찼으면 바로 발동하되 피해는 80%. 마무리 단어 SHIFT는 슬롯을 거치지 않고 바로 발동.
    onWordComplete(word, a) {
      this.stats.wordsDone++; this.stats.lastWord = word;
      const from = this.centerOf(a.board, a.cells);
      this.fx.push({ kind: 'match', b: a.board, cells: a.cells.map(k => ({ r: k.r, c: k.c })), t: 0.6 });
      if (word === ATTACK_DATA.finisherWord) { this.castSpell(word, from, 1); return; }
      const target = this.boards[1 - a.board], slot = target.emptySlot();
      if (!slot) { this.toast('반대편 슬롯이 꽉 차서 바로 발동 (피해 80%)'); this.castSpell(word, from, CONFIG.overflowMul); return; }
      slot.block = { word, hp: CONFIG.blockHp, age: 0, hitT: 0 };
      this.stats.stored = (this.stats.stored || 0) + 1;
      const to = cellCenter(target.index, slot.r, slot.c), spell = ATTACK_DATA.spells[word];
      this.fx.push({ kind: 'store', word, color: spell ? spell.color : COL.glow, from, to, t: CONFIG.storeTime, dur: CONFIG.storeTime });
      this.toast(`${word} 블록 → ${target.index === 0 ? '왼쪽' : '오른쪽'} 슬롯. 가서 Space로 발동`);
    }
    centerOf(b, cells) { const cs = cells.map(k => cellCenter(b, k.r, k.c)); return { x: cs.reduce((s, p) => s + p.x, 0) / cs.length, y: cs.reduce((s, p) => s + p.y, 0) / cs.length }; }
    // 슬롯에서 Space: 블록 발동. 같은 판 슬롯을 comboWindow초 안에 연달아 쏘면 콤보 보너스.
    fireSlot(b, slot) {
      const blk = slot.block; if (!blk || this.state !== 'play') return false;
      slot.block = null;
      const lf = this.lastFire, combo = lf && lf.board === b.index && this.time - lf.t <= CONFIG.comboWindow ? lf.combo + 1 : 0;
      this.lastFire = { board: b.index, t: this.time, combo };
      if (combo > 0) { this.toast(`콤보 ×${combo + 1}! 피해 +${Math.round(CONFIG.comboBonus * combo * 100)}%`); this.stats.combos = (this.stats.combos || 0) + 1; }
      this.castSpell(blk.word, cellCenter(b.index, slot.r, slot.c), 1 + CONFIG.comboBonus * combo);
      return true;
    }
    // 마법 발동: 피해 = 글자 수 × 배율(반올림, 최소 1). 손은 맞은 뒤 잠깐 멈춘다.
    castSpell(word, from, mul) {
      const h = this.hand, fin = ATTACK_DATA.finisherWord, spell = ATTACK_DATA.spells[word] || { ko: word, fx: 'beam', color: COL.glow };
      let dmg = Math.max(1, Math.round(word.length * mul));
      // 마지막 페이즈에서는 마무리 단어로만 쓰러진다: 다른 단어는 손 HP를 1 아래로 못 내린다
      if (this.isLastPhase() && word !== fin) dmg = Math.min(dmg, Math.max(0, h.hp - 1));
      h.hp = word === fin ? 0 : Math.max(0, h.hp - dmg); this.stats.casts = (this.stats.casts || 0) + 1;
      const dur = CONFIG.spellTime;
      this.fx.push({ kind: 'spell', style: spell.fx, color: spell.color, count: spell.count || 1, size: spell.size || 10, from, t: dur, dur, seed: Math.random() * 1000 });
      this.fx.push({ kind: 'float', x: h.pos.x, y: h.pos.y - 30, text: `${spell.ko}! -${word === fin ? '끝' : dmg}`, color: spell.color, t: 1.3, delay: dur * 0.55, age: 0 });
      this.hitT = dur * 0.55;   // 마법이 닿는 순간 손이 움찔
      if (h.hp <= 0) {
        if (!this.isLastPhase()) { this.state = 'phase'; this.stateT = 2.0; } else { this.state = 'win'; this.stateT = 0; }
        return dmg;
      }
      h.stun(CONFIG.spellStun + dur * 0.5);
      this.words.refresh();
      return dmg;
    }
    update(dt) {
      this.time += dt;
      this.fx.forEach(f => { f.t -= dt; if (f.age !== undefined) f.age += dt; }); this.fx = this.fx.filter(f => f.t > 0);
      this.shake = Math.max(0, this.shake - dt);
      if (this.hitT > 0 && (this.hitT -= dt) <= 0) { this.shake = Math.max(this.shake, 0.25); this.hand.setAnim('Hurt'); }
      if (this.toastMsg && (this.toastMsg.t -= dt) <= 0) this.toastMsg = null;
      if (this.banner && (this.banner.t -= dt) <= 0) this.banner = null;
      if (this.state === 'win') { this.stateT += dt; return; }
      // 사망: 현재 페이즈 처음부터, 손 HP는 잃은 만큼의 절반만 회복
      if (this.state === 'dead') { this.stateT -= dt; if (this.stateT <= 0) { const h = this.hand, max = h.maxHp(); this.startPhase(this.phase, Math.ceil(h.hp + (max - h.hp) * CONFIG.deathHandRecover)); } return; }
      if (this.state === 'phase') { this.stateT -= dt; if (this.stateT <= 0) this.startPhase(this.phase + 1); return; }
      if (this.state !== 'play') return;
      for (const b of this.boards) b.update(dt);
      this.brain.update(dt); this.player.update(dt);
      if (this.state === 'play') this.hand.update(dt);
    }

    // ── 렌더 ──
    render() {
      const x = this.ctx, t = this.time;
      x.save(); x.fillStyle = COL.bg; x.fillRect(0, 0, VIEW.w, VIEW.h);
      if (this.shake > 0 && !this.reducedMotion) x.translate(rand(-4, 4) * this.shake * 3, rand(-4, 4) * this.shake * 3);
      this.drawHud(x);
      for (const b of this.boards) this.drawBoard(x, b, t);
      this.drawWarn(x, t); this.drawBullets(x); this.drawPlayer(x, t); this.drawHand(x, t); this.drawFx(x);
      x.restore(); this.drawOverlay(x);
    }
    drawHud(x) {
      const w = this.words, h = this.hand, p = this.player;
      x.textBaseline = 'middle'; x.textAlign = 'left'; x.fillStyle = COL.dim; x.font = `600 13px ${FONT}`;
      x.fillText(`PHASE ${this.phase + 1} / ${ATTACK_DATA.phases.length}`, 24, 24);
      for (let i = 0; i < CONFIG.playerHp; i++) { x.fillStyle = i < p.hp ? COL.player : COL.keyEdge; x.fillRect(24 + i * 22, 40, 16, 16); }
      x.fillStyle = COL.dim; x.font = `12px ${MONO}`;
      x.fillText(p.shiftCd > 0 ? `SHIFT 칸 쿨다운 ${p.shiftCd.toFixed(1)}s` : 'SHIFT 칸 사용 가능', 24, 74);
      const bw = 220, bx = VIEW.w - 24 - bw; x.textAlign = 'right'; x.font = `600 13px ${FONT}`; x.fillStyle = COL.dim;
      x.fillText(`손 HP ${h.hp} / ${h.maxHp()}${h.state === 'stun' ? '  · 멈춤' : ''}`, VIEW.w - 24, 24);
      x.fillStyle = COL.keyEdge; x.fillRect(bx, 40, bw, 14); x.fillStyle = COL.hp; x.fillRect(bx, 40, bw * h.hp / h.maxHp(), 14);
      x.font = `12px ${MONO}`; x.fillStyle = p.shiftHeld ? COL.shiftHi : COL.dim; x.fillText('Shift+방향키: 줄 돌리기 · 슬롯에서 Space: 발동', VIEW.w - 24, 74);
      if (!w.current) return;
      // 목표 단어: 가장 많이 맞춘 줄에서 제자리에 온 글자는 초록
      const best = w.best(), bb = best ? this.boards[best.board] : null, gap = 38, total = w.current.length * gap; let cx = VIEW.w / 2 - total / 2 + gap / 2;
      const spell = ATTACK_DATA.spells[w.current];
      x.textAlign = 'center'; x.font = `700 40px ${FONT}`;
      for (let i = 0; i < w.current.length; i++) {
        const ok = bb && best.cells[i] && bb.letterAt(best.cells[i].r, best.cells[i].c) === w.current[i];
        x.fillStyle = ok ? COL.ok : COL.text; x.fillText(w.current[i], cx, 30);
        x.fillStyle = ok ? COL.ok : COL.keyEdge; x.fillRect(cx - 13, 52, 26, 3);
        cx += gap;
      }
      x.textAlign = 'left'; x.font = `600 13px ${FONT}`; x.fillStyle = spell ? spell.color : COL.glow;
      x.fillText(spell ? spell.ko : '', VIEW.w / 2 + total / 2 + 18, 22);
      x.fillStyle = COL.dim; x.fillText(w.next ? `다음: ${w.next}` : '마무리 단어', VIEW.w / 2 + total / 2 + 18, 42);
      x.textAlign = 'center'; x.font = `12px ${MONO}`; x.fillStyle = COL.dim;
      x.fillStyle = this.hintAlpha() > 0 ? COL.glow : COL.dim;
      x.fillText(`한 줄(왼→오 또는 위→아래)로 맞추면 발동 · ${best ? best.count : 0}/${w.current.length}글자 · 글자 위치는 ${CONFIG.hintInterval}초마다 깜빡임`, VIEW.w / 2, 78);
      // 슬롯 위에 서 있으면 아래쪽에 발동 안내 (슬롯이 위아래로 붙어 있어 칸 옆에는 자리가 없다)
      const here = p.here();
      if (here && here.slot && here.block && this.state === 'play') { x.font = `700 16px ${FONT}`; x.fillStyle = COL.glow; x.textAlign = 'center'; x.fillText(`Space: ${here.block.word} 발동${this.lastFire && this.lastFire.board === p.board && this.time - this.lastFire.t <= CONFIG.comboWindow ? ' (콤보 가능!)' : ''}`, VIEW.w / 2, VIEW.h - 48); }
      if (this.toastMsg) { x.font = `600 14px ${FONT}`; x.fillStyle = COL.glow; x.textAlign = 'center'; x.globalAlpha = Math.min(1, this.toastMsg.t * 2); x.fillText(this.toastMsg.text, VIEW.w / 2, VIEW.h - 22); x.globalAlpha = 1; }
    }
    drawBoard(x, b, t) {
      const word = this.words.current || '', best = this.words.best(), s = CS - 4, p = this.player;
      // 가장 많이 맞춘 자리: 이 판이면 그 칸들과 제자리에 맞은 칸
      const guide = best && best.board === b.index && best.count > 0 ? best.cells : [];
      const okAt = new Set(guide.filter((k, i) => b.letterAt(k.r, k.c) === word[i]).map(k => key(k.r, k.c)));
      x.textAlign = 'center'; x.textBaseline = 'middle';
      // 칸 자리(홈)와 SHIFT 칸은 움직이지 않는다
      for (const cell of b.allCells()) {
        const px = OX[b.index] + cell.c * CS, py = OY + cell.r * CS;
        if (cell.slot) {
          // 공격 슬롯 칸: 판 바깥의 점선 받침
          x.fillStyle = '#191c24'; x.fillRect(px + 2, py + 2, s, s); x.strokeStyle = 'rgba(255,210,74,0.45)'; x.setLineDash([3, 3]); x.lineWidth = 1.5; x.strokeRect(px + 4, py + 4, s - 4, s - 4); x.setLineDash([]); x.lineWidth = 1;
          if (!cell.block) { x.fillStyle = '#4a4f5e'; x.font = `600 9px ${MONO}`; x.fillText('SLOT', px + CS / 2, py + CS / 2); }
          continue;
        }
        if (!cell.shift) { x.fillStyle = COL.slot; x.fillRect(px + 2, py + 2, s, s); continue; }
        x.fillStyle = COL.shift; x.fillRect(px + 2, py + 2, s, s); x.fillStyle = '#2c5fd1'; x.fillRect(px + 2, py + CS - 8, s, 6);
        x.fillStyle = '#fff'; x.font = `700 11px ${FONT}`; x.fillText('SHIFT', px + CS / 2, py + CS / 2 - 2);
        if (this.player.shiftCd > 0) { x.fillStyle = 'rgba(10,12,18,0.6)'; x.fillRect(px + 2, py + 2, s, s * this.player.shiftCd / CONFIG.shiftCooldown); }
      }
      // Shift를 누르고 있으면 돌릴 수 있는 행·열을 파랗게 보여 준다
      if (p.shiftHeld && p.board === b.index && !p.here().shift && !p.here().slot) {
        x.fillStyle = 'rgba(61,123,255,0.16)';
        for (const k of [...b.lineCells('row', p.r), ...b.lineCells('col', p.c)]) if (!k.shift) x.fillRect(OX[b.index] + k.c * CS, OY + k.r * CS, CS, CS);
      }
      const H = this.hintAlpha();
      // 맞출 자리 안내: 노란 점선 슬롯 (점멸할 때만)
      if (H > 0) for (const k of guide) {
        x.strokeStyle = `rgba(255,210,74,${0.75 * H})`; x.setLineDash([4, 4]); x.lineWidth = 2;
        x.strokeRect(OX[b.index] + k.c * CS + 1, OY + k.r * CS + 1, CS - 2, CS - 2); x.setLineDash([]); x.lineWidth = 1;
      }
      // 줄 돌리기 잔상: 미끄러지는 키 뒤로 옅은 키 두 장을 남겨 움직이는 방향이 보이게 한다
      const moving = b.keyCells().filter(c => c.key.slide && c.key.slide.t >= c.key.slide.delay);
      if (!this.reducedMotion) for (const cell of moving) {
        const k = cell.key;
        for (const [lag, a] of [[0.07, 0.28], [0.035, 0.45]]) {
          const off = slideOffset(Object.assign({}, k.slide, { t: k.slide.t - lag }));
          x.globalAlpha = a * off.alpha; x.fillStyle = COL.shiftHi;
          x.fillRect(OX[b.index] + cell.c * CS + off.dx + 6, OY + cell.r * CS + off.dy + 6, s - 8, s - 8);
        }
        x.globalAlpha = 1;
      }
      // 키: 줄 돌리기 중이면 미끄러지는 위치에 그린다(움직이는 키는 위에). 점멸하는 동안만 단어 글자는 노랑, 제자리에 맞은 글자는 초록.
      const order = b.keyCells().sort((p1, p2) => (p1.key.slide ? 1 : 0) - (p2.key.slide ? 1 : 0));
      for (const cell of order) {
        const k = cell.key, off = slideOffset(k.slide), px = OX[b.index] + cell.c * CS + off.dx, py = OY + cell.r * CS + off.dy;
        const ok = okAt.has(key(cell.r, cell.c)), inWord = k.letter && word.includes(k.letter);
        x.globalAlpha = off.alpha;
        x.fillStyle = COL.key; x.fillRect(px + 2, py + 2, s, s); x.fillStyle = COL.keyEdge; x.fillRect(px + 2, py + CS - 8, s, 6);
        if (off.dx || off.dy) { x.strokeStyle = COL.shiftHi; x.lineWidth = 2; x.strokeRect(px + 3, py + 3, s - 2, s - 2); x.lineWidth = 1; }
        if (k.letter) { x.font = `700 20px ${FONT}`; x.fillStyle = COL.text; x.fillText(k.letter, px + CS / 2, py + CS / 2 - 2); }
        if (H > 0 && (ok || inWord)) {
          x.globalAlpha = off.alpha * H;
          if (ok) { x.fillStyle = '#2f5a3e'; x.fillRect(px + 2, py + 2, s, s - 6); x.strokeStyle = COL.ok; x.lineWidth = 2; x.strokeRect(px + 3, py + 3, s - 2, s - 2); x.lineWidth = 1; }
          else { x.fillStyle = 'rgba(255,210,74,0.22)'; x.fillRect(px + 2, py + 2, s, s - 6); }
          x.fillStyle = ok ? COL.ok : COL.glow; x.fillText(k.letter, px + CS / 2, py + CS / 2 - 2);
        }
        x.globalAlpha = 1;
      }
      for (const sl of b.slots) if (sl.block) this.drawBlock(x, b, sl, t);
    }
    // 공격 블록: 마법 색의 블록에 단어. 들어올 때 커지며 나타나고, 손에게 맞으면 흔들리고 금이 간다.
    drawBlock(x, b, sl, t) {
      const blk = sl.block, sp = ATTACK_DATA.spells[blk.word] || { color: COL.glow }, p = this.player;
      const pop = Math.min(1, blk.age / 0.25), sc = pop < 1 ? 0.4 + 0.6 * pop + Math.sin(pop * Math.PI) * 0.25 : 1 + 0.04 * Math.sin(t * 6);
      const shake = blk.hitT > 0 ? Math.sin(t * 70) * 4 * blk.hitT / 0.4 : 0;
      const c = cellCenter(b.index, sl.r, sl.c), w = (CS - 8) * sc;
      x.save(); x.translate(c.x + shake, c.y);
      x.fillStyle = sp.color; x.globalAlpha = 0.25 + 0.15 * Math.sin(t * 4); x.fillRect(-w / 2 - 4, -w / 2 - 4, w + 8, w + 8);
      x.globalAlpha = 1; x.fillStyle = '#20232c'; x.fillRect(-w / 2, -w / 2, w, w); x.strokeStyle = sp.color; x.lineWidth = 3; x.strokeRect(-w / 2, -w / 2, w, w);
      x.fillStyle = sp.color; x.font = `700 ${blk.word.length > 5 ? 9 : 10}px ${FONT}`; x.textAlign = 'center'; x.fillText(blk.word, 0, 0);
      if (blk.hp < CONFIG.blockHp) { x.strokeStyle = '#0c0d11'; x.lineWidth = 2; x.beginPath(); x.moveTo(-w / 2 + 4, -w / 2 + 6); x.lineTo(-2, -3); x.lineTo(3, 4); x.lineTo(w / 2 - 3, w / 2 - 5); x.stroke(); }
      x.restore(); x.lineWidth = 1;
    }
    drawWarn(x, t) {
      const h = this.hand, a = h.atk; if (!a || (h.state !== 'warn' && h.state !== 'travel')) return;
      const warning = h.state === 'warn', blink = warning && Math.floor(t * 10) % 2 === 0;
      const tracking = a.type === 'track' && a.stage !== 'lock';
      const label = () => { x.font = `700 13px ${FONT}`; x.fillStyle = COL.warn; x.textAlign = 'center'; x.fillText(`${a.name} ${a.badge} · ${Math.max(0, h.t).toFixed(1)}`, h.pos.x, h.pos.y + (h.pos.y < OY + 30 ? -34 : 44)); };
      if (a.type === 'aim' && warning) {
        x.strokeStyle = `rgba(255,77,77,${blink ? 0.9 : 0.55})`; x.lineWidth = CS * 0.5; x.lineCap = 'round'; x.globalAlpha = 0.35;
        x.beginPath(); x.moveTo(a.from.x, a.from.y); x.lineTo(a.to.x, a.to.y); x.stroke(); x.globalAlpha = 1; x.lineWidth = 2; x.setLineDash([8, 6]);
        x.beginPath(); x.moveTo(a.from.x, a.from.y); x.lineTo(a.to.x, a.to.y); x.stroke(); x.setLineDash([]); x.lineWidth = 1; x.lineCap = 'butt';
      }
      if (a.type === 'drag' && warning && !h.path.length) {
        const x0 = Math.min(a.from.x, h.pos.x) - CS / 2 + 2, y0 = Math.min(a.from.y, h.pos.y) - CS / 2 + 2, w = Math.abs(h.pos.x - a.from.x) + CS - 4, hh = Math.abs(h.pos.y - a.from.y) + CS - 4;
        x.fillStyle = 'rgba(61,123,255,0.22)'; x.fillRect(x0, y0, w, hh); x.strokeStyle = COL.shift; x.setLineDash([5, 4]); x.lineWidth = 2; x.strokeRect(x0, y0, w, hh); x.setLineDash([]); x.lineWidth = 1;
      }
      if (!tracking && a.cells && !(a.recheck && !warning)) {
        // 범위 테두리. 줄 섞기만 칸마다 미는 양(→1 …)을 붙인다.
        const perCell = a.type === 'flick';
        for (const k of a.cells) {
          if (k.shift && SHIFT_SAFE[a.type]) continue;
          const px = OX[a.board] + k.c * CS, py = OY + k.r * CS;
          x.lineWidth = warning ? 3 : 1.5; x.strokeStyle = COL.warn; x.globalAlpha = warning ? 1 : 0.45;
          if (blink) { x.fillStyle = 'rgba(255,77,77,0.28)'; x.fillRect(px + 2, py + 2, CS - 4, CS - 4); }
          x.strokeRect(px + 3, py + 3, CS - 6, CS - 6);
          if (perCell && !k.shift) {
            x.font = `700 11px ${MONO}`; const tw = x.measureText(a.badge).width + 6;
            x.fillStyle = COL.warn; x.fillRect(px + CS - tw - 3, py + 3, tw, 14); x.fillStyle = '#fff'; x.textAlign = 'center'; x.fillText(a.badge, px + CS - tw / 2 - 3, py + 10.5);
          }
        }
        x.globalAlpha = 1; x.lineWidth = 1;
      }
      if ((a.type === 'click' || a.type === 'slot') && warning) { const c = a.center; x.strokeStyle = COL.warn; x.lineWidth = 2; x.beginPath(); x.arc(c.x, c.y, 16, 0, Math.PI * 2); x.moveTo(c.x - 24, c.y); x.lineTo(c.x - 8, c.y); x.moveTo(c.x + 8, c.y); x.lineTo(c.x + 24, c.y); x.moveTo(c.x, c.y - 24); x.lineTo(c.x, c.y - 8); x.moveTo(c.x, c.y + 8); x.lineTo(c.x, c.y + 24); x.stroke(); x.lineWidth = 1; }
      if (warning) label();
    }
    drawBullets(x) {
      for (const bl of this.hand.bullets) {
        const cx = bl.axis === 'row' ? OX[bl.board] + bl.pos * CS + CS / 2 : OX[bl.board] + bl.idx * CS + CS / 2;
        const cy = bl.axis === 'row' ? OY + bl.idx * CS + CS / 2 : OY + bl.pos * CS + CS / 2;
        x.fillStyle = COL.bullet; x.beginPath(); x.arc(cx, cy, 11, 0, Math.PI * 2); x.fill();
        x.fillStyle = '#fff'; x.beginPath(); x.arc(cx - 3, cy - 3, 3, 0, Math.PI * 2); x.fill();
      }
    }
    drawPlayer(x, t) {
      const p = this.player, c0 = cellCenter(p.board, p.r, p.c), off = slideOffset(p.slide), c = { x: c0.x + off.dx, y: c0.y + off.dy };
      x.globalAlpha = off.alpha * (p.invuln > 0 && Math.floor(t * 16) % 2 === 0 ? 0.35 : 1);
      x.fillStyle = COL.player; x.fillRect(c.x - 11, c.y - 15, 22, 22); x.fillStyle = '#0c2a20'; x.fillRect(c.x - 6, c.y - 9, 4, 6); x.fillRect(c.x + 2, c.y - 9, 4, 6);
      x.globalAlpha = 1;
      if (p.shiftHeld) { x.strokeStyle = COL.shiftHi; x.lineWidth = 2; x.strokeRect(c.x - 14, c.y - 18, 28, 28); x.lineWidth = 1; }
      if (DEBUG.invincible) { x.strokeStyle = COL.glow; x.strokeRect(c.x - 17, c.y - 21, 34, 34); }
    }
    drawHand(x, t) {
      const h = this.hand, a = h.atk, big = !!a && a.type === 'track';
      const over = !h.charging && [0, 1].some(b => h.pos.x > OX[b] && h.pos.x < OX[b] + BW && h.pos.y > OY && h.pos.y < OY + BH);
      const lift = over ? (big ? 26 : 10) : 0, s = over && big ? 1.9 : 1.5;
      // FlickCharge: 줄 바깥쪽으로 당겼다가, FlickRelease: 줄 쪽으로 튕긴다
      let ax = 0, ay = 0;
      if (h.anim === 'FlickCharge' && a) { const u = Math.min(1, h.animT / Math.max(0.05, a.warn)); ax = -h.animDir.x * 16 * u; ay = -h.animDir.y * 16 * u; }
      if (h.anim === 'FlickRelease') { const u = 1 - Math.min(1, h.animT / 0.16); ax = h.animDir.x * 22 * u; ay = h.animDir.y * 22 * u; }
      const hurt = h.anim === 'Hurt' && h.animT < 0.35;   // 마법에 맞아 뒤로 튕기며 번쩍인다
      if (hurt) { const u = 1 - h.animT / 0.35; ax += rand(-5, 5) * u; ay -= 14 * u; }
      if (over) { x.fillStyle = 'rgba(0,0,0,0.45)'; x.beginPath(); x.ellipse(h.pos.x, h.pos.y + 4, big ? 40 : 18, big ? 22 : 9, 0, 0, Math.PI * 2); x.fill(); }
      x.save(); x.translate(h.pos.x + ax - 10 * s, h.pos.y + ay - 14 * s - lift); x.scale(s, s);
      x.beginPath(); [[0, 0], [0, 30], [8, 23], [13, 35], [18, 33], [13, 21], [23, 21]].forEach(([px, py], i) => i ? x.lineTo(px, py) : x.moveTo(px, py)); x.closePath();
      x.fillStyle = hurt && Math.floor(h.animT * 20) % 2 === 0 ? '#ffffff' : h.charging ? COL.warn : h.state === 'stun' ? '#6f7890' : COL.hand; x.fill(); x.strokeStyle = '#0c0d11'; x.lineWidth = 1.5; x.stroke();
      x.restore();
      if (h.state === 'stun') { x.font = `700 12px ${FONT}`; x.fillStyle = COL.glow; x.textAlign = 'center'; x.fillText(`멈춤 ${h.t.toFixed(1)}`, h.pos.x, h.pos.y - 34); }
    }
    drawFx(x) {
      for (const f of this.fx) {
        if (f.delay && f.age < f.delay) continue;
        if (f.kind === 'spell') this.drawSpell(x, f);
        else if (f.kind === 'rotate') {
          // 플레이어 줄 돌리기: 줄 전체가 파란 띠로 빛나고, 그 위로 화살표(›)가 돌리는 방향으로 흐른다.
          // 양 끝 바깥에는 큰 화살표: 한쪽 끝에서 밀려 나간 키가 반대쪽 끝으로 들어온다는 뜻.
          const u = f.t / f.dur, ks = this.boards[f.b].lineCells(f.axis, f.idx).filter(k => !k.shift), row = f.axis === 'row';
          const ux = row ? f.sign : 0, uy = row ? 0 : f.sign, flow = ((1 - u) * 2.5) % 1;
          x.globalAlpha = u * 0.3; x.fillStyle = COL.shift;
          for (const k of ks) x.fillRect(OX[f.b] + k.c * CS, OY + k.r * CS, CS, CS);
          x.globalAlpha = u * 0.9; x.strokeStyle = COL.shiftHi; x.lineWidth = 3; x.lineCap = 'round';
          const chevron = (cx, cy, sz) => { x.beginPath(); x.moveTo(cx - ux * sz - uy * sz, cy - uy * sz - ux * sz); x.lineTo(cx, cy); x.lineTo(cx - ux * sz + uy * sz, cy - uy * sz + ux * sz); x.stroke(); };
          for (const k of ks) { const c = cellCenter(f.b, k.r, k.c), o = (flow - 0.5) * CS; chevron(c.x + ux * o + (row ? 0 : CS * 0.36), c.y + uy * o + (row ? CS * 0.36 : 0), 5); }   // 글자를 가리지 않게 키 가장자리에
          const first = cellCenter(f.b, ks[0].r, ks[0].c), last = cellCenter(f.b, ks[ks.length - 1].r, ks[ks.length - 1].c);
          const [outEnd, inEnd] = f.sign > 0 ? [last, first] : [first, last], gap = CS * 0.78;
          x.lineWidth = 4; chevron(outEnd.x + ux * gap, outEnd.y + uy * gap, 9); chevron(inEnd.x - ux * gap, inEnd.y - uy * gap, 9);
          x.globalAlpha = 1; x.lineWidth = 1; x.lineCap = 'butt';
        }
        else if (f.kind === 'match') {
          const u = f.t / 0.6; x.globalAlpha = u; x.fillStyle = COL.ok;
          for (const k of f.cells) { const g = (1 - u) * 6; x.fillRect(OX[f.b] + k.c * CS + 2 - g, OY + k.r * CS + 2 - g, CS - 4 + g * 2, CS - 4 + g * 2); }
          x.globalAlpha = 1;
        }
        else if (f.kind === 'float') { x.globalAlpha = Math.min(1, f.t * 2); x.font = `700 18px ${FONT}`; x.fillStyle = f.color; x.textAlign = 'center'; x.fillText(f.text, f.x, f.y - (1 - f.t) * 24); x.globalAlpha = 1; }
        else if (f.kind === 'beam') { x.globalAlpha = f.t / 0.3; x.strokeStyle = COL.glow; x.lineWidth = 4; x.beginPath(); x.moveTo(f.from.x, f.from.y); x.lineTo(f.to.x, f.to.y); x.stroke(); x.lineWidth = 1; x.globalAlpha = 1; }
        else if (f.kind === 'warp') { const c = cellCenter(f.b, f.r, f.c); x.strokeStyle = COL.shift; x.lineWidth = 3; x.globalAlpha = f.t / 0.4; x.beginPath(); x.arc(c.x, c.y, 12 + (0.4 - f.t) * 90, 0, Math.PI * 2); x.stroke(); x.globalAlpha = 1; x.lineWidth = 1; }
        else if (f.kind === 'click') { x.strokeStyle = COL.warn; x.lineWidth = 3; x.globalAlpha = f.t / 0.25; x.beginPath(); x.arc(f.x, f.y, 8 + (0.25 - f.t) * 120, 0, Math.PI * 2); x.stroke(); x.globalAlpha = 1; x.lineWidth = 1; }
        else if (f.kind === 'deny') { const c = cellCenter(f.b, f.r, f.c); x.strokeStyle = COL.dim; x.lineWidth = 2; x.globalAlpha = f.t / 0.2; x.strokeRect(c.x - 21, c.y - 21, 42, 42); x.globalAlpha = 1; x.lineWidth = 1; }
        else if (f.kind === 'store') {
          // 완성한 단어가 블록이 되어 반대편 슬롯으로 날아간다
          const u = 1 - f.t / f.dur, e = 1 - (1 - u) * (1 - u), px = f.from.x + (f.to.x - f.from.x) * e, py = f.from.y + (f.to.y - f.from.y) * e - Math.sin(u * Math.PI) * 70;
          x.globalAlpha = 0.9; x.fillStyle = f.color; x.fillRect(px - 16, py - 12, 32, 24); x.fillStyle = '#20232c'; x.font = `700 9px ${FONT}`; x.textAlign = 'center'; x.fillText(f.word, px, py); x.globalAlpha = 1;
        }
        else if (f.kind === 'fizzle') { const c = cellCenter(f.b, f.r, f.c), u = 1 - f.t / 0.5; x.globalAlpha = 1 - u; x.fillStyle = COL.dim; for (let i = 0; i < 6; i++) { const a = i * Math.PI / 3; x.fillRect(c.x + Math.cos(a) * 30 * u - 4, c.y + Math.sin(a) * 30 * u - 4, 8, 8); } x.globalAlpha = 1; }
        else if (f.kind === 'impact') { x.globalAlpha = f.t / 0.3 * 0.6; x.fillStyle = COL.warn; for (const k of f.cells) x.fillRect(OX[f.b] + k.c * CS + 2, OY + k.r * CS + 2, CS - 4, CS - 4); x.globalAlpha = 1; }
      }
    }
    // 공격 마법 연출. 앞 55%는 맞춘 줄에서 손까지 날아가고, 뒤 45%는 손에서 터진다.
    // style: fireball / dragon / rocket / shards / bolt / beam / gust / nova / sparkle / slash / shift
    drawSpell(x, f) {
      const TRAVEL = 0.55, u = 1 - f.t / f.dur, to = { x: this.hand.pos.x, y: this.hand.pos.y - 10 }, from = f.from, col = f.color;
      const ease = v => 1 - (1 - v) * (1 - v), n = f.count, sr = i => { const s = Math.sin(f.seed + i * 12.9898) * 43758.5453; return s - Math.floor(s); };
      const along = (v, arc, side) => { const e = ease(Math.max(0, Math.min(1, v))); return { x: from.x + (to.x - from.x) * e + side * Math.sin(e * Math.PI) * 30, y: from.y + (to.y - from.y) * e - Math.sin(e * Math.PI) * arc }; };
      const circle = (p, r, fill) => { x.beginPath(); x.arc(p.x, p.y, Math.max(0.1, r), 0, Math.PI * 2); if (fill) x.fill(); else x.stroke(); };
      const star = (p, r, rot) => { x.beginPath(); for (let i = 0; i < 10; i++) { const a = rot + i * Math.PI / 5, rr = i % 2 ? r * 0.45 : r; x.lineTo(p.x + Math.cos(a) * rr, p.y + Math.sin(a) * rr); } x.closePath(); x.fill(); };
      const zigzag = (a, b, seg, amp, k) => { x.beginPath(); x.moveTo(a.x, a.y); for (let i = 1; i < seg; i++) { const v = i / seg; x.lineTo(a.x + (b.x - a.x) * v + (sr(k * 31 + i + Math.floor(this.time * 20)) - 0.5) * amp, a.y + (b.y - a.y) * v); } x.lineTo(b.x, b.y); x.stroke(); };
      const v = u / TRAVEL, w = (u - TRAVEL) / (1 - TRAVEL);   // v: 날아가는 진행도, w: 터지는 진행도
      x.save(); x.fillStyle = col; x.strokeStyle = col; x.lineCap = 'round';
      switch (f.style) {
        case 'fireball': case 'dragon': {
          const balls = f.style === 'dragon' ? 3 : 1, r0 = f.style === 'dragon' ? 15 : f.size;
          for (let b = 0; b < balls; b++) {
            const vb = v - b * 0.18; if (vb < 0 || vb > 1) continue;
            for (let i = 6; i >= 0; i--) { const p = along(vb - i * 0.04, 50 + b * 25, b - 1); x.globalAlpha = (1 - i / 7) * 0.9; circle(p, r0 * (1 - i * 0.1), true); }
            x.globalAlpha = 1; x.fillStyle = '#fff3c4'; circle(along(vb, 50 + b * 25, b - 1), r0 * 0.45, true); x.fillStyle = col;
          }
          if (w > 0) { x.globalAlpha = 1 - w; circle(to, (f.style === 'dragon' ? 70 : 40) * ease(w), true); x.fillStyle = '#fff3c4'; circle(to, 22 * ease(w), true); x.lineWidth = 3; circle(to, (f.style === 'dragon' ? 100 : 60) * w, false); }
          break;
        }
        case 'rocket': {
          if (v <= 1) {
            for (let i = 1; i < 10; i++) { const p = along(v - i * 0.035, 70, 0.6); x.globalAlpha = 0.5 * (1 - i / 10); x.fillStyle = '#9aa0ad'; circle(p, 5 + i * 1.2, true); }
            const p = along(v, 70, 0.6), q = along(v + 0.02, 70, 0.6), ang = Math.atan2(q.y - p.y, q.x - p.x);
            x.globalAlpha = 1; x.save(); x.translate(p.x, p.y); x.rotate(ang); x.fillStyle = '#e6e8ee'; x.fillRect(-14, -5, 24, 10); x.fillStyle = col; x.beginPath(); x.moveTo(10, -5); x.lineTo(18, 0); x.lineTo(10, 5); x.fill();
            x.fillStyle = '#ff5d3a'; x.beginPath(); x.moveTo(-14, -4); x.lineTo(-24 - Math.random() * 6, 0); x.lineTo(-14, 4); x.fill(); x.restore();
          }
          if (w > 0) { for (let i = 0; i < 3; i++) { x.globalAlpha = (1 - w) * 0.8; x.fillStyle = i ? col : '#fff3c4'; circle({ x: to.x + (sr(i) - 0.5) * 40, y: to.y + (sr(i + 5) - 0.5) * 30 }, (50 - i * 10) * ease(w), true); } x.lineWidth = 4; x.strokeStyle = col; circle(to, 90 * w, false); }
          break;
        }
        case 'shards': {
          for (let i = 0; i < n; i++) {
            const vi = v - i * 0.05; if (vi < 0 || vi > 1) continue;
            const p = along(vi, 40 + sr(i) * 60, (sr(i + 9) - 0.5) * 3), a = this.time * 8 + i;
            x.globalAlpha = 1; x.save(); x.translate(p.x, p.y); x.rotate(a); x.beginPath(); x.moveTo(0, -9); x.lineTo(4, 0); x.lineTo(0, 9); x.lineTo(-4, 0); x.closePath(); x.fill(); x.restore();
          }
          if (w > 0) { x.globalAlpha = 1 - w; x.lineWidth = 3; for (let i = 0; i < 8; i++) { const a = i * Math.PI / 4, L = 50 * ease(w); x.beginPath(); x.moveTo(to.x, to.y); x.lineTo(to.x + Math.cos(a) * L, to.y + Math.sin(a) * L); x.stroke(); } x.globalAlpha = (1 - w) * 0.35; circle(to, 45 * ease(w), true); }
          break;
        }
        case 'bolt': {
          if (v <= 1) { x.globalAlpha = 0.5 + 0.5 * Math.sin(this.time * 60); for (let i = 0; i < 4; i++) circle({ x: from.x + (sr(i + Math.floor(this.time * 30)) - 0.5) * 50, y: from.y + (sr(i + 3 + Math.floor(this.time * 30)) - 0.5) * 30 }, 3, true); }
          if (w > 0) {
            x.lineWidth = 4; x.globalAlpha = 1 - w * 0.8;
            for (let i = 0; i < n; i++) { const ox = (i - (n - 1) / 2) * 34; zigzag({ x: to.x + ox * 1.6, y: 0 }, { x: to.x + ox * 0.3, y: to.y }, 9, 34, i); }
            x.globalAlpha = (1 - w) * 0.25; x.fillStyle = '#ffffff'; x.fillRect(0, 0, VIEW.w, VIEW.h);
          }
          break;
        }
        case 'beam': {
          const len = Math.min(1, v), widths = n > 1 ? [3, 3, 3] : [14];
          widths.forEach((wd, i) => {
            const off = (i - (widths.length - 1) / 2) * 10, end = { x: from.x + (to.x - from.x) * len, y: from.y + (to.y - from.y) * len + off };
            x.globalAlpha = w > 0 ? 1 - w : 0.9; x.lineWidth = wd * (w > 0 ? 1 + w : 1); x.beginPath(); x.moveTo(from.x, from.y + off); x.lineTo(end.x, end.y); x.stroke();
            x.strokeStyle = '#ffffff'; x.lineWidth = Math.max(1, wd * 0.35); x.stroke(); x.strokeStyle = col;
          });
          if (w > 0) { x.globalAlpha = 1 - w; circle(to, 30 * ease(w), true); }
          break;
        }
        case 'gust': {
          x.lineWidth = 3;
          for (let i = 0; i < n; i++) {
            const vi = v - i * 0.06; if (vi < 0) continue;
            const p = vi <= 1 ? along(vi, 30, (i % 2 ? 1 : -1)) : to, r = vi <= 1 ? 12 : 12 + w * 40, a0 = this.time * 12 + i * 2;
            x.globalAlpha = vi <= 1 ? 0.9 : 1 - w; x.beginPath(); x.arc(p.x, p.y, r, a0, a0 + Math.PI * 1.3); x.stroke();
          }
          break;
        }
        case 'nova': case 'sparkle': {
          const k = f.style === 'sparkle' ? n : 1;
          for (let i = 0; i < k; i++) {
            const vi = v - i * 0.04; if (vi < 0 || vi > 1) continue;
            const p = along(vi, 40 + Math.sin(vi * 12 + i) * 20, (sr(i) - 0.5) * 3); x.globalAlpha = 1; star(p, f.style === 'nova' ? 12 : 7, this.time * 6 + i);
          }
          if (w > 0) {
            x.globalAlpha = 1 - w; x.lineWidth = 4; circle(to, 80 * ease(w), false); x.lineWidth = 2; circle(to, 50 * ease(w), false);
            if (f.style === 'sparkle') for (let i = 0; i < 8; i++) { const a = i * Math.PI / 4 + w; star({ x: to.x + Math.cos(a) * 55 * ease(w), y: to.y + Math.sin(a) * 55 * ease(w) }, 6, a); }
          }
          break;
        }
        case 'slash': {
          if (v <= 1) { const p = along(v, 20, 0); x.globalAlpha = 1; x.lineWidth = 4; x.beginPath(); x.arc(p.x, p.y, 16, -0.8 + this.time * 20, 0.8 + this.time * 20); x.stroke(); }
          if (w > 0) for (let i = 0; i < n; i++) {
            const wi = Math.max(0, Math.min(1, w * 2 - i * 0.35)); if (wi <= 0) continue;
            const a = -0.6 + i * 0.6, L = 46, cx2 = Math.cos(a), sy = Math.sin(a);
            x.globalAlpha = 1 - w; x.lineWidth = 5 * (1 - wi) + 1; x.beginPath(); x.moveTo(to.x - cx2 * L, to.y - sy * L - 20 + i * 14); x.lineTo(to.x - cx2 * L + cx2 * 2 * L * wi, to.y - sy * L - 20 + i * 14 + sy * 2 * L * wi); x.stroke();
          }
          break;
        }
        case 'shift': {
          if (v <= 1) { const p = along(v, 90, 0); x.globalAlpha = 1; x.font = `700 44px ${FONT}`; x.textAlign = 'center'; x.fillText('⇧', p.x, p.y); }
          if (w > 0) { x.globalAlpha = 1 - w; x.lineWidth = 8; circle(to, 260 * ease(w), false); x.lineWidth = 3; circle(to, 160 * ease(w), false); x.globalAlpha = (1 - w) * 0.3; x.fillRect(0, 0, VIEW.w, VIEW.h); }
          break;
        }
        default: break;
      }
      x.restore();
    }
    drawOverlay(x) {
      const center = (title, sub, color) => {
        x.fillStyle = 'rgba(12,13,17,0.78)'; x.fillRect(0, 0, VIEW.w, VIEW.h); x.textAlign = 'center';
        x.fillStyle = color || COL.text; x.font = `700 46px ${FONT}`; x.fillText(title, VIEW.w / 2, VIEW.h / 2 - 16);
        x.fillStyle = COL.dim; x.font = `500 15px ${FONT}`; x.fillText(sub, VIEW.w / 2, VIEW.h / 2 + 32);
      };
      if (this.state === 'ready') center('FINGER SNAP SHIFT', '클릭 후 아무 키나 눌러 시작 · 이동 WASD/방향키 · Shift+방향키로 줄 돌리기 · 단어를 한 줄로 맞추면 반대편 슬롯에 공격 블록 · 슬롯에서 Space로 발동 · R 재시작');
      else if (this.state === 'dead') center('쓰러짐', `페이즈 ${this.phase + 1} 처음부터 다시 · 손 HP는 절반만 회복`, COL.hp);
      else if (this.state === 'phase' && this.stateT < 1.2) center(`PHASE ${this.phase + 1} 클리어`, '손이 빨라진다');
      else if (this.state === 'win' && this.stateT > 0.9) center('승리!', 'SHIFT로 마우스 커서를 물리쳤다 · R을 누르면 처음부터', COL.glow);
      else if (this.banner) { x.globalAlpha = Math.min(1, this.banner.t); x.textAlign = 'center'; x.fillStyle = COL.text; x.font = `700 28px ${FONT}`; x.fillText(this.banner.text, VIEW.w / 2, OY + BH + 62); x.globalAlpha = 1; }
    }
  }

  // ───────────────────────── 부트: 입력, 디버그 패널, 루프 ─────────────────────────
  function boot() {
    const canvas = document.getElementById('stage'), game = new Game(canvas);
    window.FSS = { game, CONFIG, DEBUG, Perimeter };
    // 물리 키 위치(code)와 shiftKey로 읽는다: 자판 배열과 무관
    window.addEventListener('keydown', e => {
      if (e.ctrlKey || e.metaKey || e.altKey) return;
      game.player.shiftHeld = e.shiftKey;
      const dir = KEYMAP[e.code];
      if (dir || e.code === 'KeyR' || e.code === 'Space') e.preventDefault();
      if (game.state === 'ready') { if (!e.repeat && e.code !== 'ShiftLeft' && e.code !== 'ShiftRight') game.startPhase(0); return; }
      if (e.code === 'KeyR' && !e.repeat) { game.restartPhase(); return; }
      if (e.code === 'Space' && !e.repeat) { game.player.fire(); return; }
      if (dir && !e.repeat) game.player.press(dir);
    }, true);
    window.addEventListener('keyup', e => { game.player.shiftHeld = e.shiftKey; const dir = KEYMAP[e.code]; if (dir) game.player.release(dir); }, true);
    window.addEventListener('blur', () => { game.player.held = []; game.player.shiftHeld = false; });
    canvas.addEventListener('pointerdown', () => canvas.focus());

    // 배치·단어 목록 검사 결과
    const cl = R.check(LAYOUT_DATA.left, R.MASK_LEFT, LAYOUT_DATA.right, ALL_WORDS), cr = R.check(LAYOUT_DATA.right, R.MASK_RIGHT, LAYOUT_DATA.left, ALL_WORDS), wl = game.words.listCheck;
    document.getElementById('layout-check').textContent =
      `시작 배치: 왼쪽 ${cl.ok ? '통과' : '실패'} (단어 최대 ${cl.worstAlign}글자 정렬) · 오른쪽 ${cr.ok ? '통과' : '실패'} (최대 ${cr.worstAlign}글자)\n단어 목록 ${wl.ok ? '통과' : '실패'} (같은 글자 없음, 6글자 이하)` + [...cl.errors, ...cr.errors, ...wl.errors].map(e => '\n' + e).join('');

    const box = document.getElementById('sliders');
    for (const s of ATTACK_DATA.sliders) {
      const row = document.createElement('label'); row.className = 'slider';
      const name = document.createElement('span'); name.textContent = s.label;
      const val = document.createElement('output'); val.textContent = CONFIG[s.key];
      const input = document.createElement('input'); Object.assign(input, { type: 'range', id: 'dbg-' + s.key, min: s.min, max: s.max, step: s.step, value: CONFIG[s.key] });
      input.addEventListener('input', () => { CONFIG[s.key] = Number(input.value); val.textContent = input.value; });
      input.addEventListener('change', () => input.blur());
      row.append(name, val, input); box.append(row);
    }
    const inv = document.getElementById('dbg-invincible'); inv.addEventListener('change', () => { DEBUG.invincible = inv.checked; inv.blur(); });
    document.getElementById('dbg-skip').addEventListener('click', e => { game.skipPhase(); e.currentTarget.blur(); });
    document.getElementById('dbg-restart').addEventListener('click', e => { game.restartPhase(); e.currentTarget.blur(); });

    const aiEl = document.getElementById('ai-scores'), statEl = document.getElementById('ai-state'); let aiT = 0;
    const stateName = { idle: '대기', travel: '이동(예고)', warn: '예고', exec: '공격', stun: '멈춤' };
    function refreshPanel() {
      const h = game.hand, b = game.brain, at = game.stats.attacks, tot = Object.values(at).reduce((s, n) => s + n, 0);
      statEl.textContent = `손: ${stateName[h.state]}${h.atk ? ` · ${h.atk.name} ${h.atk.target} ${h.atk.badge}` : ''} · 애니메이션 ${h.anim}`
        + (tot ? `\n공격 횟수: ` + Object.entries(at).map(([k, n]) => `${ATTACK_DATA.attacks[k].name} ${n}`).join(' · ') + ` (줄 섞기 ${((at.flick || 0) / tot * 100).toFixed(0)}%)` : '')
        + `\n플레이어 줄 돌리기 ${game.stats.rotations || 0}번 · 완성 단어 ${game.stats.wordsDone}`;
      if (!b.report.length) { aiEl.textContent = '아직 공격을 고르지 않았어요.'; return; }
      aiEl.textContent = (b.reportFeat || '') + '\n\n' + b.report.map(c => `${c.mark} ${String(c.score.toFixed(1)).padStart(5)}  ${c.text.padEnd(18)} ${c.note ? '[' + c.note + '] ' : ''}${c.why}`).join('\n');
    }
    let last = performance.now();
    function frame(now) {
      const dt = Math.min(0.05, (now - last) / 1000); last = now;
      game.update(dt); game.render();
      if ((aiT -= dt) <= 0) { aiT = 0.2; refreshPanel(); }
      requestAnimationFrame(frame);
    }
    game.render(); requestAnimationFrame(frame);
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot); else boot();
})();
