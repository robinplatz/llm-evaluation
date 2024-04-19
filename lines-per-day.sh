#!/bin/bash

# Get the date for today's commits
date=$(date +%Y-%m-%d)

# Get the list of files changed in today's commits
files=$(git log --since="$date 00:00" --until="$date 23:59" --pretty=format: --name-only | sort -u)

# Initialize line counter
total_lines=0

# Loop through the files and count the lines
for file in $files
do
  if [ -f "$file" ]; then
    lines=$(wc -l < "$file")
    total_lines=$((total_lines + lines))
  fi
done

# Print the total number of lines
echo "Number of lines of code committed today: $total_lines"
