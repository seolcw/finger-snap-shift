// LayoutRules: 키보드 마스크, 줄(행·열) 정의, 단어 정렬 검사 (브라우저/Node 공용)
(function (root) {
  const MASK_LEFT = ['.#####.', '######.', '######S', '.######', '.#####.'];
  const MASK_RIGHT = MASK_LEFT.map(row => row.split('').reverse().join(''));
  const ROWS = 5, COLS = 7;
  // 공격 슬롯: 판 바깥, SHIFT 칸의 반대쪽 끝에 2칸. 순간이동해 온 뒤 판을 가로질러 걸어가야 닿는다.
  // 걸을 수는 있지만 줄 돌리기에서 빠지는 고정 칸(열 -1 또는 7 = 마스크 바깥).
  const SLOTS_LEFT = [{ r: 1, c: -1 }, { r: 2, c: -1 }];
  const SLOTS_RIGHT = SLOTS_LEFT.map(s => ({ r: s.r, c: COLS - 1 - s.c }));
  // 시작 배치에서 한 단어가 한 줄에 이만큼 넘게 맞춰져 있으면 안 된다(시작하자마자 거의 완성되는 것 방지)
  const MAX_START_ALIGN = 2;

  function cellsOf(mask) {
    const list = [];
    for (let r = 0; r < ROWS; r++) for (let c = 0; c < COLS; c++) if (mask[r][c] !== '.') list.push({ r, c, shift: mask[r][c] === 'S' });
    return list;
  }
  function isCell(mask, r, c) { return r >= 0 && r < ROWS && c >= 0 && c < COLS && mask[r][c] !== '.'; }
  // layout: 5개 문자열(7자). 글자, '_' = 빈 키, 'S' 자리는 '^', '.' = 칸 없음
  function positions(layout) { const p = {}; layout.forEach((row, r) => row.split('').forEach((ch, c) => { if (/[A-Z]/.test(ch)) p[ch] = { r, c }; })); return p; }

  // 줄: 행·열마다 SHIFT 칸을 뺀 칸을 순서대로 담는다. 줄 돌리기와 단어 읽기(행 왼→오, 열 위→아래)가 같은 순서를 쓴다.
  function lines(mask) {
    const out = [];
    for (let r = 0; r < ROWS; r++) { const cells = []; for (let c = 0; c < COLS; c++) if (mask[r][c] === '#') cells.push({ r, c }); if (cells.length >= 2) out.push({ axis: 'row', idx: r, cells }); }
    for (let c = 0; c < COLS; c++) { const cells = []; for (let r = 0; r < ROWS; r++) if (mask[r][c] === '#') cells.push({ r, c }); if (cells.length >= 2) out.push({ axis: 'col', idx: c, cells }); }
    return out;
  }
  // 단어가 가장 많이 맞춰진 자리. 한 줄 안의 연속한 칸에서, 제자리에 맞는 글자 수를 센다.
  function bestAlign(lineList, letterAt, word) {
    let best = null;
    for (const ln of lineList) for (let s = 0; s + word.length <= ln.cells.length; s++) {
      let n = 0;
      for (let i = 0; i < word.length; i++) if (letterAt(ln.cells[s + i].r, ln.cells[s + i].c) === word[i]) n++;
      if (!best || n > best.count) best = { axis: ln.axis, idx: ln.idx, start: s, count: n, cells: ln.cells.slice(s, s + word.length) };
    }
    return best || { count: 0, cells: [] };
  }
  function longestLine(mask) { return Math.max(...lines(mask).map(l => l.cells.length)); }

  function check(layout, mask, other, words) {
    const errors = [], pos = positions(layout);
    let blanks = 0;
    layout.forEach((row, r) => row.split('').forEach((ch, c) => {
      const m = mask[r][c];
      if (m === '.' && ch !== '.') errors.push(`(${r},${c}) 칸 없음 자리에 ${ch}`);
      if (m === 'S' && ch !== '^') errors.push(`(${r},${c}) SHIFT 자리 불일치`);
      if (m === '#') { if (ch === '_') blanks++; else if (!/[A-Z]/.test(ch)) errors.push(`(${r},${c}) 잘못된 문자 ${ch}`); }
    }));
    const letters = Object.keys(pos);
    if (letters.length !== 26) errors.push(`글자 수 ${letters.length} (26이어야 함)`);
    if (blanks !== 2) errors.push(`빈 키 ${blanks}개 (2여야 함)`);
    // 시작부터 거의 맞춰진 단어가 없어야 한다
    const ls = lines(mask), at = (r, c) => layout[r][c];
    let worst = 0;
    for (const w of words || []) {
      const a = bestAlign(ls, at, w); worst = Math.max(worst, a.count);
      if (a.count > MAX_START_ALIGN) errors.push(`${w}: 시작 배치에서 ${a.count}글자가 이미 한 줄에 맞춰져 있음`);
    }
    // 좌우 배치 차이: 같은 칸(그대로·거울) 글자가 4개 이하
    if (other) {
      const op = positions(other); let same = 0, mirror = 0;
      for (const ch of letters) { const a = pos[ch], b = op[ch]; if (!b) continue; if (a.r === b.r && a.c === b.c) same++; if (a.r === b.r && a.c === COLS - 1 - b.c) mirror++; }
      if (same > 4) errors.push(`좌우 차이: 같은 칸 글자 ${same}개`);
      if (mirror > 4) errors.push(`좌우 차이: 거울 위치 같은 글자 ${mirror}개`);
    }
    return { ok: errors.length === 0, errors, worstAlign: worst };
  }

  // 단어 목록 검사: 키보드 한 판에 글자가 하나씩뿐이라 한 단어에 같은 글자가 두 번 나오면 한 줄로 맞출 수 없다.
  function checkWordList(words, maxLen) {
    const errors = [];
    for (const w of words) {
      if (!/^[A-Z]+$/.test(w)) errors.push(`${w}: A~Z 이외의 문자`);
      const seen = new Set();
      for (const ch of w) { if (seen.has(ch)) errors.push(`${w}: ${ch}가 두 번 (한 번만 가능)`); seen.add(ch); }
      if (maxLen && w.length > maxLen) errors.push(`${w}: ${w.length}글자 (가장 긴 줄 ${maxLen}칸)`);
    }
    return { ok: errors.length === 0, errors };
  }

  const api = { checkWordList, MASK_LEFT, MASK_RIGHT, SLOTS_LEFT, SLOTS_RIGHT, ROWS, COLS, MAX_START_ALIGN, cellsOf, isCell, positions, lines, bestAlign, longestLine, check };
  if (typeof module !== 'undefined' && module.exports) module.exports = api; else root.LayoutRules = api;
})(this);
