const fs = require('fs');
const path = require('path');

const apiBaseUrl = process.env.NG_APP_API_BASE_URL || '/api';
const targetPath = path.join(__dirname, '..', 'src', 'environments', 'environment.production.ts');
const escapedApiBaseUrl = apiBaseUrl.replace(/\\/g, '\\\\').replace(/'/g, "\\'");
const contents = `export const environment = {\n  apiBaseUrl: '${escapedApiBaseUrl}'\n};\n`;

fs.writeFileSync(targetPath, contents, 'utf8');
console.log(`Wrote production environment with apiBaseUrl=${apiBaseUrl}`);
