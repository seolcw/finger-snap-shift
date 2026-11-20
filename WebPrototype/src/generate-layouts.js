// 규칙을 통과하는 좌우 시작 배치를 찾아 layouts.json으로 저장 (node src/generate-layouts.js [seed])
// 규칙: 마스크 일치, 글자 26 + 빈 키 2, 어떤 단어도 시작부터 3글자 이상 한 줄에 맞춰져 있지 않음, 좌우 배치가 충분히 다름
const R = require('./layoutRules');
const fs = require('fs');
const DATA = require('./attackPatterns.json');
const WORDS = DATA.phases.flatMap(p => p.words).concat(DATA.finisherWord);
let seed = Number(process.argv[2] || 7);
const rand = () => { seed = (seed * 1103515245 + 12345) & 0x7fffffff; return seed / 0x7fffffff; };
const SYMBOLS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ__'.split('');
function build(mask, perm) {
  let i = 0; return mask.map(row => row.split('').map(m => m === '.' ? '.' : m === 'S' ? '^' : perm[i++]).join(''));
}
function search(mask, other) {
  for (let tries = 0; tries < 5000; tries++) {
    const perm = SYMBOLS.slice().sort(() => rand() - 0.5), lay = build(mask, perm);
    if (R.check(lay, mask, other, WORDS).ok) return lay;
  }
  throw new Error('배치를 찾지 못함');
}
const left = search(R.MASK_LEFT, null);
const right = search(R.MASK_RIGHT, left);
const out = { left, right };
for (const [k, m, o] of [['left', R.MASK_LEFT, right], ['right', R.MASK_RIGHT, left]]) {
  const res = R.check(out[k], m, o, WORDS); console.log(k, res.ok ? 'PASS' : 'FAIL', `최대 정렬 ${res.worstAlign}글자`, res.errors.join('; '));
  console.log(out[k].join('\n'));
}
fs.writeFileSync(__dirname + '/layouts.json', JSON.stringify(out, null, 2));
