// 5차 규칙 검증(헤드리스): node src/verify-rules.js
// 줄 돌리기, 한 줄 정렬로 단어 발동, 손 공격이 글자를 바꾸지 않음, 사망 시 손 HP 절반 회복, 처음부터 승리까지
const {game:g,CONFIG,botDecide,botAct}=require('./simtest.js'); const R=LayoutRules;
let fails=0; const ok=(cond,msg)=>{ console.log((cond?'PASS ':'FAIL ')+msg); if(!cond) fails++; };
const rowStr=(b,r)=>b.lineCells('row',r).filter(k=>!k.shift).map(k=>k.key.letter||'_').join('');
const lettersOf=b=>b.keyCells().map(k=>k.key.letter||'_').sort().join('');

// 1. 데이터 검사
const words=ATTACK_DATA.phases.flatMap(p=>p.words).concat(ATTACK_DATA.finisherWord);
ok(R.checkWordList(words,R.longestLine(R.MASK_LEFT)).ok,'단어 목록: 같은 글자 없음, 6글자 이하');
ok(R.check(LAYOUT_DATA.left,R.MASK_LEFT,LAYOUT_DATA.right,words).ok&&R.check(LAYOUT_DATA.right,R.MASK_RIGHT,LAYOUT_DATA.left,words).ok,'시작 배치: 단어가 3글자 이상 미리 맞춰져 있지 않음');
ok(words.every(w=>ATTACK_DATA.spells[w]),'모든 단어에 마법 연출이 있음');

// 2. 줄 돌리기: 행은 한 칸 순환, 플레이어 자리는 그대로, SHIFT 칸 위에서는 안 돌아감
g.startPhase(0); const p=g.player, b0=g.boards[0]; p.board=0; p.r=1; p.c=2; p.shiftHeld=true;
const before=rowStr(b0,1); p.rotate('right'); const after=rowStr(b0,1);
ok(after===before.slice(-1)+before.slice(0,-1)&&p.r===1&&p.c===2,`Shift+→: ${before} → ${after}, 플레이어 (1,2) 유지`);
const colBefore=b0.lineCells('col',2).map(k=>k.key.letter).join(''); p.rotate('up');
const colAfter=b0.lineCells('col',2).map(k=>k.key.letter).join('');
ok(colAfter===colBefore.slice(1)+colBefore[0],`Shift+↑: ${colBefore} → ${colAfter}`);
p.r=2; p.c=6; const v=b0.version; ok(!p.rotate('left')&&b0.version===v,'SHIFT 칸 위에서는 줄 돌리기 안 됨');

// 3. 공격 슬롯: 완성한 단어는 반대편 판 슬롯에 블록으로 들어가고, 슬롯에서 Space로 발동한다
const place=(bi,w)=>{ const b=g.boards[bi], row=b.lineCells('row',1).filter(k=>!k.shift); for(let i=0;i<w.length;i++){ const from=b.keyCells().find(k=>k.key.letter===w[i]), to=row[i]; [from.key,to.key]=[to.key,from.key]; } g.words.check(); };
const L=g.boards[0], Rb=g.boards[1];
g.startPhase(0); let w=g.words.current, hp=g.hand.hp; place(0,w);
ok(g.hand.hp===hp&&Rb.slots[0].block&&Rb.slots[0].block.word===w&&L.blocks()===0&&g.words.current!==w,`왼쪽에서 ${w} 완성 → 오른쪽 슬롯에 블록, 손 HP 그대로(${g.hand.hp}), 다음 단어 ${g.words.current}`);
p.board=1; p.r=Rb.slots[0].r; p.c=Rb.slots[0].c; p.shiftHeld=false;
ok(!p.rotate('up'),'슬롯 칸 위에서는 줄 돌리기 안 됨');
p.fire(); ok(g.hand.hp===hp-w.length&&!Rb.slots[0].block&&g.hand.state==='stun',`슬롯에서 Space → 발동: 손 HP ${hp} → ${g.hand.hp}, 손 멈춤`);
// 콤보: 같은 판 슬롯 두 개를 1.5초 안에 연달아 → 두 번째는 +50%
g.startPhase(0); hp=g.hand.hp; Rb.slots[0].block={word:'FIRE',hp:2,age:0,hitT:0}; Rb.slots[1].block={word:'BOLT',hp:2,age:0,hitT:0};
p.board=1; p.r=Rb.slots[0].r; p.c=Rb.slots[0].c; p.fire(); for(let i=0;i<20;i++) g.update(1/60); p.tryMove('down'); p.fire();
ok(g.hand.hp===hp-4-Math.round(4*(1+CONFIG.comboBonus)),`콤보: FIRE 4 + BOLT ${Math.round(4*(1+CONFIG.comboBonus))} → 손 HP ${hp} → ${g.hand.hp}`);
// 반대편 슬롯이 꽉 차면 바로 발동, 피해 80%
g.startPhase(0); hp=g.hand.hp; w=g.words.current; Rb.slots.forEach(s=>s.block={word:'NOVA',hp:2,age:0,hitT:0}); place(0,w);
ok(g.hand.hp===hp-Math.round(w.length*CONFIG.overflowMul)&&Rb.blocks()===2,`슬롯이 꽉 찼을 때 ${w} 완성 → 바로 ${Math.round(w.length*CONFIG.overflowMul)} 피해 (80%)`);
// 손의 슬롯 깨기: 한 번 맞으면 금(내구도 1), 두 번째에 사라짐
g.startPhase(0); const sl=Rb.slots[0]; sl.block={word:'FIRE',hp:2,age:0,hitT:0}; p.board=0;
const hitSlot=()=>{ g.hand.atk={type:'slot',name:'슬롯 깨기',board:1,slot:sl,cells:[sl],center:{x:0,y:0}}; g.hand.fire(); };
hitSlot(); const after1=sl.block&&sl.block.hp; hitSlot();
ok(after1===1&&!sl.block,'슬롯 깨기: 1번 맞으면 금, 2번 맞으면 블록 사라짐');
// 페이즈가 바뀌면 슬롯이 비워짐
g.startPhase(0); sl.block={word:'FIRE',hp:2,age:0,hitT:0}; g.startPhase(1);
ok(g.boards.every(b=>b.blocks()===0),'페이즈가 바뀌면 남은 블록 사라짐');
// 마무리: 손 HP ≤ 5가 되면 남은 블록은 사라지고, SHIFT는 슬롯을 거치지 않고 바로 발동
g.startPhase(2); g.boards[0].slots[0].block={word:'COMET',hp:2,age:0,hitT:0}; g.hand.hp=5; g.words.refresh();
ok(g.words.current==='SHIFT'&&g.boards.every(b=>b.blocks()===0),'마무리 전환: 목표 SHIFT, 남은 블록 정리');
place(0,'SHIFT'); ok(g.state==='win'&&Rb.blocks()===0,`SHIFT 완성 → 슬롯 없이 바로 승리 (${g.state})`);
// 마지막 페이즈: SHIFT 외 블록은 손 HP를 1 아래로 못 내린다
g.startPhase(2); g.hand.hp=7; Rb.slots[0].block={word:'DRAGON',hp:2,age:0,hitT:0}; p.board=1; p.r=Rb.slots[0].r; p.c=Rb.slots[0].c; p.fire();
ok(g.hand.hp===1&&g.words.current==='SHIFT',`마지막 페이즈 DRAGON(6) 발동: 손 HP 7 → ${g.hand.hp}, 목표 ${g.words.current}`);

// 3-1. 단어 위치 힌트는 계속 보이지 않고 주기적으로만 깜빡인다 (새 단어가 나오면 바로 한 번)
g.startPhase(0); const at=s=>{ g.time=g.hintT0+s; return g.hintAlpha(); };
ok(at(0.25)>0.9&&at(CONFIG.hintDuration+0.5)===0&&at(CONFIG.hintInterval+0.25)>0.9,`힌트 점멸: 시작 직후 ${at(0.25).toFixed(2)}, 사이 ${at(CONFIG.hintDuration+0.5)}, 다음 주기 ${at(CONFIG.hintInterval+0.25).toFixed(2)}`);

// 4. 사망: 손 HP는 잃은 만큼의 절반만 회복
g.startPhase(0); g.hand.hp=8; g.player.hp=1; g.player.invuln=0; g.player.hurt('test'); for(let i=0;i<120;i++) g.update(1/60);
ok(g.state==='play'&&g.hand.hp===Math.ceil(8+(20-8)*0.5),`사망 후 재시작: 손 HP 8 → ${g.hand.hp} (기대 14)`);

// 5. 처음부터 끝까지: 70%만 피하는 봇. 손 공격 중 글자 구성(26자 + 빈 키 2)이 바뀌지 않아야 한다.
g.stats={teleports:0,wordsDone:0,hits:{},attacks:{},deaths:0,rotations:0}; g.startPhase(0);
const base=g.boards.map(lettersOf); let t=0,moveT=0,changed=false; const done=[]; const ow=g.onWordComplete.bind(g); g.onWordComplete=(word,a)=>{ done.push(`${g.phase+1}:${word}`); ow(word,a); };
while(t<1500&&g.state!=='win'){ const dt=1/60; t+=dt; moveT-=dt;
  if(g.state==='play'&&moveT<=0){ moveT=0.22; botAct(g,botDecide(g,Math.random()<0.7)); }
  g.update(dt); if(g.state==='play'&&g.boards.some((b,i)=>lettersOf(b)!==base[i])) changed=true; }
ok(!changed,'손 공격과 줄 돌리기 중에 판의 글자 구성이 그대로');
ok(g.state==='win',`처음부터 승리까지: ${g.state}, ${t.toFixed(0)}초, 사망 ${g.stats.deaths}번, 공격 ${JSON.stringify(g.stats.attacks)}`);
console.log('완성 순서:',done.join(' '));
process.exit(fails?1:0);
