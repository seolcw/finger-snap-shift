// 로컬 확인용 정적 서버: node src/serve.js
const http = require('http'), fs = require('fs'), path = require('path');
http.createServer((req, res) => {
  fs.readFile(path.join(__dirname, '..', 'shift-magic-spell-prototype.html'), (e, buf) => {
    res.writeHead(e ? 500 : 200, { 'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-store' });
    res.end(e ? String(e) : '<!doctype html><meta charset="utf-8">' + buf);
  });
}).listen(4173, '127.0.0.1');
