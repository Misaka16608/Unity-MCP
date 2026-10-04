const fs = require('fs');
const path = require('path');
const manifest = require('./sources.json');
const root = path.resolve(__dirname, '../../.local/request-safety');
fs.mkdirSync(root, { recursive: true });
(async () => {
  for (const source of manifest.sources) {
    const file = path.join(root, source.repo + '.zip');
    if (fs.existsSync(file)) continue;
    const url = `https://codeload.github.com/IvanMurzak/${source.repo}/zip/${source.commit}`;
    const response = await fetch(url); // verified TLS; no schannel
    if (!response.ok) throw new Error(`${source.repo}: HTTP ${response.status}`);
    const temporary = file + '.download';
    fs.writeFileSync(temporary, Buffer.from(await response.arrayBuffer()));
    fs.renameSync(temporary, file);
    console.log(`Fetched ${source.repo} @ ${source.commit}`);
  }
})().catch(error => { console.error(error.message); process.exitCode = 1; });
