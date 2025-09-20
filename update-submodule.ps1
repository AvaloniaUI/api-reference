# Update submodule to upstream latest commit
git submodule update --init --depth=1 --remote --force

# Commit changes
git add .\ext\*
git commit -m "Updated Avalonia submodule to latest commit"

# Need to fetch submodules recursive
git submodule update --init --recursive