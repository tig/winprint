const Prism = require('prismjs');
const loadLanguages = require('prismjs/components/');
loadLanguages(['csharp']);

 const code = 'using System';
 const html = Prism.highlight(code, Prism.languages.csharp, 'csharp');
console.log(html);
console.log('done');