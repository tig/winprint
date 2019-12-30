// Imports file-extension to langauge mapping from both
// prismjs and lang-map and outputs a JSDON document that
// follows the vscode schema for extension mapping.
// PrismJS language definitions trump for my solution.
const fs = require('fs');
var map = require('lang-map');
var components = require('prismjs/components.js');

// vscode files.associations is not an array. Use a dictionary instead.
var assocDict = {};
var languages = [];

for (var key in components.languages) {
    if (components.languages.hasOwnProperty(key) && key != 'meta') {
        var language = components.languages[key];
        var langTemp = {
          id : key
        };

        // vscode doesn't support title, but I want to use it
        if (typeof language.title != 'undefined')
            langTemp.title = language.title;

        if (typeof language.alias != 'undefined'){
            if (Array.isArray(language.alias)){
                langTemp.aliases = language.alias;
            }
            else{
                langTemp.aliases = ['.' + language.alias];
            }
        }
        var extensions = [];
        map.extensions(key).forEach(ext =>{
            // Add it to the extnsions for this langauge defn
            extensions.push('.' + ext);

            // also add it to the files.associations dictionary
            var pattern = '*.' + ext;
            var assoc = { 
                pattern : key
            };
            assocDict[pattern] = key;
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

var file = "../winforms/WinPrint.Core/Properties/languages.json";
fs.writeFile(file, JSON.stringify(output, null, '  '), function (err) {
    if (err) {
        return console.log(err);
    }
    console.log("Wrote " + Object.keys(assocDict).length + " file-type associations and " + languages.length + " language defs to " + file);
});
