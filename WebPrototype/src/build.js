// src/* 를 한 장짜리 HTML로 합친다: node src/build.js
const fs = require('fs'), d = __dirname + '/';
const rd = f => fs.readFileSync(d + f, 'utf8');
const html = rd('template.html')
  .replace('/*__LAYOUT_RULES__*/', () => rd('layoutRules.js'))
  .replace('/*__LAYOUTS__*/', () => rd('layouts.json'))
  .replace('/*__ATTACKS__*/', () => rd('attackPatterns.json'))
  .replace('/*__GAME__*/', () => rd('game.js'));
fs.writeFileSync(d + '../finger-snap-shift-prototype.html', html);
console.log('built', html.length, 'bytes');
