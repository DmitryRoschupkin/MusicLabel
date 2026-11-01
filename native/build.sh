set -eu
cd "$(dirname "$0")"
OUT=lib/linux-x64
mkdir -p "$OUT"
gcc -std=c11 -O2 -Wall -Wextra -Werror -fPIC -shared -fvisibility=hidden \
-o "$OUT/libmusiclabel.so" musiclabel.c
echo "Built $OUT/libmusiclabel.so"