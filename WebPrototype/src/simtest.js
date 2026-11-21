// DOM 없이 로직만 돌리는 헤드리스 시뮬레이션: node src/simtest.js [횟수] [nododge] [행동 간격]
const fs=require('fs');
const stubEl=()=>({addEventListener(){},append(){},getContext:()=>new Proxy({}, {get:(t,k)=>k in t?t[k]:()=>({width:10}), set:(t,k,v)=>{t[k]=v;return true}}),focus(){},blur(){},set textContent(v){},className:''});
global.window={addEventListener(){},matchMedia:()=>({matches:false})};global.document={readyState:'complete',getElementById:stubEl,createElement:stubEl};
global.performance={now:()=>0};global.requestAnimationFrame=()=>{};
const h=fs.readFileSync(__dirname+'/../shift-magic-spell-prototype.html','utf8');
const scripts=[...h.matchAll(/<script>([\s\S]*?)<\/script>/g)].map(m=>m[1]);
(0,eval)(scripts[0].replace('})(this);','})(globalThis);'));(0,eval)(scripts[1].replace(/const /g,'globalThis.'));(0,eval)(scripts[2]);
const {game,CONFIG,DEBUG}=window.FSS; module.exports={game,CONFIG,DEBUG};
const K=(r,c)=>r*16+c+4;   // game.js의 칸 번호와 같은 방식(슬롯이 열 -1, 7)

// ── 퍼즐 풀이: 목표 자리(한 줄의 연속 칸)에 단어 글자를 앞에서부터 하나씩 옮긴다 ──
// 이미 놓은 글자가 있는 줄은 돌리지 않으므로, 한 글자를 옮길 때마다 앞 글자는 그대로 남는다.
// 글자 하나의 위치만 보고 BFS(줄 돌리기 ±1)로 최단 경로를 찾는다. 반환: [{axis, idx, sign}] 또는 null
function solve(b, targets, word){
  const lines=b.lines, letters=new Map();
  for(const cell of b.keyCells()) letters.set(K(cell.r,cell.c), cell.key.letter);
  const lineOf=(axis,idx)=>lines.find(l=>l.axis===axis&&l.idx===idx);
  const rot=(ln,sign)=>{ const vals=ln.cells.map(c=>letters.get(K(c.r,c.c))), L=vals.length; ln.cells.forEach((c,i)=>letters.set(K(c.r,c.c), vals[((i-sign)%L+L)%L])); };
  const placed=new Set(), ops=[];
  for(let i=0;i<word.length;i++){
    const tgt=targets[i], tk=K(tgt.r,tgt.c);
    let start=null; for(const [k,v] of letters) if(v===word[i]) start=k;
    if(start===null) return null;
    const allowed=ln=>!ln.cells.some(c=>placed.has(K(c.r,c.c)));
    const prev=new Map([[start,null]]), q=[start];
    while(q.length && !prev.has(tk)){
      const cur=q.shift(), r=Math.floor(cur/16), c=cur%16-4;
      for(const ln of [lineOf('row',r),lineOf('col',c)]){
        if(!ln||!allowed(ln)) continue;
        const j=ln.cells.findIndex(x=>x.r===r&&x.c===c), L=ln.cells.length;
        for(const sign of [1,-1]){ const n=ln.cells[((j+sign)%L+L)%L], nk=K(n.r,n.c); if(!prev.has(nk)){ prev.set(nk,{from:cur,axis:ln.axis,idx:ln.idx,sign}); q.push(nk); } }
      }
    }
    if(!prev.has(tk)) return null;
    const path=[]; for(let k=tk; prev.get(k); k=prev.get(k).from) path.unshift(prev.get(k));
    for(const st of path){ rot(lineOf(st.axis,st.idx), st.sign); ops.push({axis:st.axis, idx:st.idx, sign:st.sign}); }
    placed.add(tk);
  }
  return ops;
}
// 모든 줄·시작 칸·판을 시도해 비용(돌리기 수 + 줄 바꿀 때 걷기 + 반대편 판이면 이동)이 가장 낮은 계획
function plan(game){
  const w=game.words.current, p=game.player; if(!w) return null; let best=null;
  for(const b of game.boards) for(const ln of b.lines) for(let s=0;s+w.length<=ln.cells.length;s++){
    const ops=solve(b, ln.cells.slice(s,s+w.length), w); if(!ops) continue;
    let cost=ops.length; for(let i=1;i<ops.length;i++) if(ops[i].axis!==ops[i-1].axis||ops[i].idx!==ops[i-1].idx) cost+=2;
    if(b.index!==p.board) cost+=6;
    if(!best||cost<best.cost) best={cost, board:b.index, ops};
  }
  return best;
}
// SHIFT 칸(순간이동)을 피해서 goal 칸들까지 걷는 거리
function walkDist(b, goals, avoidShift){
  const d=new Map(), q=[]; for(const g of goals){ d.set(K(g.r,g.c),0); q.push(g); }
  while(q.length){ const cur=q.shift(), cd=d.get(K(cur.r,cur.c));
    for(const [dr,dc] of [[1,0],[-1,0],[0,1],[0,-1]]){ const n=b.cell(cur.r+dr,cur.c+dc); if(!n||d.has(K(n.r,n.c))||(avoidShift&&n.shift)) continue; d.set(K(n.r,n.c),cd+1); q.push(n); } }
  return d;
}
// 봇의 다음 행동: {act:'move'|'rotate'|'none', dir}. 위험하면 먼저 피하고, 아니면 계획의 첫 줄 돌리기를 하러 간다.
function botDecide(game, dodge){
  const p=game.player,b=game.boards[p.board],hd=game.hand;
  const danger=new Set();
  if(dodge){ if(hd.atk&&hd.atk.board===p.board&&hd.atk.cells&&hd.state!=='idle') for(const k of hd.atk.cells) danger.add(K(k.r,k.c));
    for(const bl of hd.bullets) if(bl.board===p.board) for(const k of b.lineCells(bl.axis,bl.idx)) danger.add(K(k.r,k.c)); }
  const full=dodge&&hd.atk&&hd.atk.type==='full'&&hd.atk.board===p.board, inD=danger.has(K(p.r,p.c));
  const step=(goals, avoidShift)=>{ const dg=walkDist(b,goals,avoidShift); let bd=dg.get(K(p.r,p.c)), best=null; if(bd===undefined) bd=99;
    for(const [n,[dr,dc]] of Object.entries({up:[-1,0],down:[1,0],left:[0,-1],right:[0,1]})){ const kk=K(p.r+dr,p.c+dc),d=dg.get(kk); if(d!==undefined&&d<bd&&(inD||full||!danger.has(kk))){bd=d;best=n;} }
    return best?{act:'move',dir:best}:{act:'none'}; };
  if(full&&p.shiftCd<=0.3) return step([b.shiftCell],false);
  if(inD&&!full){ const safe=b.keyCells().concat(b.slots).filter(k=>!danger.has(K(k.r,k.c))); return step(safe,true); }
  // 공격 슬롯: 이 판에 블록이 있으면 쏘러 간다(붙어 있는 두 칸이라 연달아 쏘면 콤보).
  // 반대편 슬롯이 꽉 찼거나(다음 완성이 80%로 새 버림) 쌓인 피해로 손을 끝낼 수 있으면 반대편으로 건너간다.
  const here=b.cell(p.r,p.c); if(here.slot&&here.block) return {act:'fire'};
  const mine=b.slots.filter(s=>s.block); if(mine.length) return step(mine,true);
  const o=game.boards[1-p.board], stored=o.slots.filter(s=>s.block).reduce((n,s)=>n+s.block.word.length,0);
  if(o.blocks()&&(!o.emptySlot()||stored>=game.hand.hp)) return p.shiftCd<=0.3?step([b.shiftCell],false):{act:'none'};
  const pl=plan(game); if(!pl||!pl.ops.length) return {act:'none'};
  if(pl.board!==p.board) return p.shiftCd<=0.3?step([b.shiftCell],false):{act:'none'};
  const op=pl.ops[0];
  if(!here.shift&&(op.axis==='row'?p.r===op.idx:p.c===op.idx)) return {act:'rotate',dir:op.axis==='row'?(op.sign>0?'right':'left'):(op.sign>0?'down':'up')};
  return step(b.lineCells(op.axis,op.idx).filter(k=>!k.shift),true);
}
function botAct(game,d){ if(d.act==='fire') game.player.fire(); else if(d.act==='rotate') game.player.rotate(d.dir); else if(d.act==='move') game.player.tryMove(d.dir); }
module.exports.botDecide=botDecide; module.exports.botAct=botAct; module.exports.plan=plan;
if(require.main===module){
  const runs=Number(process.argv[2]||5), dodge=process.argv[3]!=='nododge', step=Number(process.argv[4]||0.22); const tot={}; const out=[];
  for(let run=0;run<runs;run++){
    game.stats={teleports:0,wordsDone:0,hits:{},attacks:{},deaths:0,rotations:0}; game.startPhase(0); let t=0,moveT=0,phaseStart=0,lastPhase=0; const phaseTimes=[];
    while(t<1500 && game.state!=='win'){
      const dt=1/60; t+=dt; moveT-=dt;
      if(game.phase!==lastPhase){phaseTimes.push(+(t-phaseStart).toFixed(0));phaseStart=t;lastPhase=game.phase;}
      if(game.state==='play'&&moveT<=0){ moveT=step; botAct(game,botDecide(game,dodge)); }
      game.update(dt);
    }
    phaseTimes.push(+(t-phaseStart).toFixed(0));
    for(const [k,n] of Object.entries(game.stats.attacks)) tot[k]=(tot[k]||0)+n;
    out.push({state:game.state,time:+t.toFixed(0),phaseTimes,words:game.stats.wordsDone,casts:game.stats.casts,combos:game.stats.combos||0,lost:game.stats.blocksLost||0,rot:game.stats.rotations,hits:Object.values(game.stats.hits).reduce((s,n)=>s+n,0),deaths:game.stats.deaths,tp:game.stats.teleports});
  }
  console.log(out.map(o=>JSON.stringify(o)).join('\n')); console.log('attacks',JSON.stringify(tot));
}
