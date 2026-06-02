const fs = require('fs');
const file = 'c:\\Projects\\Decode\\Microservices\\chunk-PCUXB42Z.js';
if (fs.existsSync(file)) {
  const code = fs.readFileSync(file, 'utf8');
  const idx = code.indexOf('efar');
  if (idx !== -1) {
    console.log('Found efar at index:', idx);
    console.log('--- Snippet ---');
    console.log(code.substring(idx - 250, idx + 250));
  } else {
    console.log('efar not found in the file.');
  }
} else {
  console.log('File does not exist.');
}
