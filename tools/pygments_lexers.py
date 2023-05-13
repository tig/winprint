# This is needed because Pygments doesn't suppport `-L lexers`. 
# See https://github.com/pygments/pygments/issues/1437
#
import pygments
import json
from pygments.lexers import get_all_lexers
l = get_all_lexers()
a = []

# Input looks like this:
# [
#  "XQuery",
#  [
#   "xquery",
#   "xqy",
#   "xq",
#   "xql",
#   "xqm"
#  ],
#  [
#   "*.xqy",
#   "*.xquery",
#   "*.xq",
#   "*.xql",
#   "*.xqm"
#  ],
#  [
#   "text/xquery",
#   "application/xquery"
#  ]
# ]

for i, lexer in enumerate(get_all_lexers()):
    for mimetype in lexer[3]:
        defn = {"id": mimetype, "title": lexer[0], "aliases": lexer[1], "extensions": lexer[2] }
        a.append(defn)
        # d[mimetype] = defn

#print (len(a))
# for idx, lexer in enumerate(a):
#      print (json.dumps(lexer, indent=True, sort_keys=False))

with open('pygments_langs.json', 'w') as outfile:
    json.dump(a, outfile, indent=4)

# print(d["text/html"])