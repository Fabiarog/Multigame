#!/bin/sh
printf '\033c\033]0;%s\a' Game Hub
base_path="$(dirname "$(realpath "$0")")"
"$base_path/Game Hub-linux.x86_64" "$@"
