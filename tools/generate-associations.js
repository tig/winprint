// Imports file-extension to language mapping from both
// prismjs and lang-map and outputs a JSON document that
// follows the vscode schema for extension mapping.
// PrismJS language definitions trump for my solution.
const fs = require('fs');
var map = require('lang-map');
var components = require('prismjs/components.js');

// var {PythonShell} = require( 'python-shell');

// var options = {
//   mode: 'text',
//   pythonPath: 'C:\Python38',
//   pythonOptions: ['-u'],
//   scriptPath: 'C:\Python38\Lib\site-packages\pygments',
//   args: ['value1', 'value2', 'value3']
// };

// PythonShell.run('my_script.py', options, function (err, results) {
//   if (err)
//     throw err;
//   // Results is an array consisting of messages collected during execution
//   console.log('results: %j', results);
// });

// vscode files.associations is not an array. Use a dictionary instead.
var assocDict = {};
var languages = [];

for (var key in components.languages) {
    if (components.languages.hasOwnProperty(key) && key != 'meta') {
        var language = components.languages[key];
        var langTemp = {
          id : key
        };
        langTemp.aliases = [];

        // vscode doesn't support title, but I want to use it
        if (typeof language.title != 'undefined')
            langTemp.title = language.title;

        if (typeof language.alias != 'undefined'){
            if (Array.isArray(language.alias)){
                langTemp.aliases = language.alias;
            }
            else{
                langTemp.aliases.push(language.alias);
            }
        }
        var extensions = [];
        // get lang to extension mapping from lang-map using key
        if (map.extensions(key).length > 0){
            map.extensions(key).forEach(ext =>{
                // Add it to the extensions for this language defn

                if (extensions.find(element => element == '.' + ext) == undefined)
                    extensions.push('.' + ext);

                // also add it to the files.associations dictionary
                var pattern = '*.' + ext;
                if (assocDict[pattern] == undefined)
                    assocDict[pattern] = key;
            });
        }
        if (typeof langTemp.title != 'undefined'){
            map.extensions(langTemp.title).forEach(ext =>{
                // Add it to the extensions for this langauge defn
                if (extensions.find(element => element == '.' + ext) == undefined)
                    extensions.push('.' + ext);

                // also add it to the files.associations dictionary
                var pattern = '*.' + ext;
                if (assocDict[pattern] == undefined)
                    assocDict[pattern] = key;
            });
        }

        langTemp.aliases.forEach(a => {
            map.extensions(a).forEach(ext =>{
                // Add it to the extensions for this langauge defn
                if (extensions.find(element => element == '.' + ext) == undefined)
                    extensions.push('.' + ext);

                // also add it to the files.associations dictionary
                var pattern = '*.' + ext;
                if (assocDict[pattern] == undefined)
                    assocDict[pattern] = key;
            });
        });
        langTemp.extensions = extensions;
        languages.push(langTemp);
    }
}

// create JSON doc conforming to vscode spec. associations is not an array
// languages is
var output = {
    'files.associations' : assocDict,
    'languages' : languages
};

var file = "prism_languages.json";
fs.writeFile(file, JSON.stringify(output, null, '  '), function (err) {
    if (err) {
        return console.log(err);
    }
    console.log("Wrote " + Object.keys(assocDict).length + " file-type associations and " + languages.length + " language defs to " + file);
});
