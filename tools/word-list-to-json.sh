#!/bin/sh

WORDLIST=google-10000-english-usa-no-swears.txt

(
echo '{'
echo '  "lorem": {'
echo '    "words": ['

for w in `cat $WORDLIST`; do
  echo -n '      "'$w'",'
done

echo '    ]'
echo '  }'
echo '}'
) | sed -E 's/,( *])/\n\1/' | sed -E 's/,/,\n/g'
