// Експорт англійського словника з web/js/lang.js у src/Kuriatnyk/Assets/Lang/en.json
// Запуск: node tools/export-lang.js
const fs = require('fs'), path = require('path'), vm = require('vm');
const root = path.join(__dirname, '..');
const src = fs.readFileSync(path.join(root, 'web/js/lang.js'), 'utf8');
const ctx = {};
vm.createContext(ctx);
vm.runInContext(src + '\n;this.__EN = EN;', ctx);
const out = path.join(root, 'src/Kuriatnyk/Assets/Lang/en.json');
fs.mkdirSync(path.dirname(out), { recursive: true });
fs.writeFileSync(out, JSON.stringify(ctx.__EN, null, 1), 'utf8');
console.log('keys:', Object.keys(ctx.__EN).length, '->', out);
