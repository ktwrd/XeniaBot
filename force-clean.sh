#!/usr/bin/env bash
set -xeo -u pipefail

for d in ./*/
do
    d=${d%*/}
    if [ -d "$d/obj" ]; then
        /bin/rm -rf "$d/obj"
    fi
    if [ -d "$d/bin" ]; then
        /bin/rm -rf "$d/bin"
    fi
done