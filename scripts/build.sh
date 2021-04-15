#!/bin/bash
set -e

source "$(dirname $0)/_common.sh"

title "Unity license"
install_license

title "Build binary"
/opt/unity/Editor/Unity \
  -quit \
  -batchmode \
  -nographics \
  -logFile \
  -projectPath "$(dirname $0)/../nekoyume" \
  -executeMethod "Editor.Builder.BuildLinuxHeadlessDevelopment"
