import fs from 'node:fs';
import path from 'node:path';

const root = process.cwd();
const nm = path.join(root, 'node_modules');

const topDirs = ['internal', 'packages', 'scripts', 'apps'];

function findPackageJsons(baseDir, out = []) {
  if (!fs.existsSync(baseDir)) return out;
  for (const entry of fs.readdirSync(baseDir, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    if (entry.name === 'node_modules' || entry.name === 'dist' || entry.name === '.turbo') continue;
    const full = path.join(baseDir, entry.name);
    const pj = path.join(full, 'package.json');
    if (fs.existsSync(pj)) {
      out.push(full);
    }
    // keep recursing to catch nested workspace packages
    findPackageJsons(full, out);
  }
  return out;
}

const dirs = new Set();
for (const top of topDirs) {
  for (const d of findPackageJsons(path.join(root, top))) dirs.add(d);
}

let created = 0;
let skipped = 0;
let failed = 0;
for (const dir of dirs) {
  const pjPath = path.join(dir, 'package.json');
  const pj = JSON.parse(fs.readFileSync(pjPath, 'utf8'));
  const name = pj.name;
  if (!name || !name.includes('/')) continue;

  const [scope, pkg] = name.split('/');
  const scopeDir = path.join(nm, scope);
  const linkPath = path.join(scopeDir, pkg);

  fs.mkdirSync(scopeDir, { recursive: true });

  if (fs.existsSync(linkPath) || fs.existsSync(path.join(scopeDir, pkg))) {
    skipped++;
    continue;
  }

  try {
    fs.symlinkSync(dir, linkPath, 'junction');
    console.log(`[link] ${name} -> ${path.relative(root, dir)}`);
    created++;
  } catch (e) {
    console.error(`[fail] ${name}: ${e.message}`);
    failed++;
  }
}

console.log(`\nDone. created=${created} skipped=${skipped} failed=${failed}`);
