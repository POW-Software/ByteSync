# Testing Inaccessible Paths and Symlinks (Windows, Linux, macOS)

This document provides safe, reproducible commands to turn a test directory into a non‑traversable/inaccessible path, and then restore it.
It also documents how to create symlinks on each OS.

Use these recipes to validate inventory behavior (e.g., `IsAccessible` paths, continued scanning, validator blocks) without leaving your
machine in a broken state.

## Quick scripts (run from current directory/Desktop)

Run these from the directory you want to test in (e.g., your Desktop). They create a subdirectory, put a random‑content file inside, then
make the directory non‑traversable. A matching restore script makes it removable again.

### Windows — PowerShell

Create and make non‑traversable (stores original ACL SDDL next to the folder):

```
# create-nontraversable.ps1
$name = "bytesync-test-" + (Get-Date -Format "yyyyMMddHHmmss")
$root = Join-Path (Get-Location) $name
New-Item -ItemType Directory -Path $root | Out-Null

# Add a file with random content
Set-Content -Path (Join-Path $root 'file.txt') -Value ("content_" + (Get-Date -Format o))

# Save original ACL SDDL to a sidecar file for easy restore
$sddl = (Get-Acl $root).Sddl
Set-Content -Path ("$root.acl.sddl") -Value $sddl

# Deny read & list (prevents traversal) for current user
$user = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
$rights = [System.Security.AccessControl.FileSystemRights]::ReadAndExecute,
          [System.Security.AccessControl.FileSystemRights]::ListDirectory
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule($user, $rights, 'None','None','Deny')
$acl = Get-Acl $root
$acl.AddAccessRule($rule)
Set-Acl -Path $root -AclObject $acl

Write-Host "Created and locked: $root" -ForegroundColor Cyan
```

Restore (allows deletion):

```
# restore-removable.ps1
param([string]$name)
if (-not $name) { throw "Usage: .\restore-removable.ps1 <folder-name>" }
$root = Join-Path (Get-Location) $name
$sddlPath = "$root.acl.sddl"
if (-not (Test-Path $sddlPath)) { throw "Missing sidecar SDDL file: $sddlPath" }

$sddl = Get-Content -Path $sddlPath -Raw
$aclOriginal = New-Object System.Security.AccessControl.DirectorySecurity
$aclOriginal.SetSecurityDescriptorSddlForm($sddl)
Set-Acl -Path $root -AclObject $aclOriginal

Write-Host "Restored ACLs for: $root" -ForegroundColor Green
```

Usage:

```
PS> cd "$env:USERPROFILE\Desktop"
PS> .\create-nontraversable.ps1
# ... run inventory/tests targeting .\bytesync-test-YYYYMMDDhhmmss ...
PS> .\restore-removable.ps1 bytesync-test-YYYYMMDDhhmmss
PS> Remove-Item -Recurse -Force .\bytesync-test-YYYYMMDDhhmmss
```

### Linux / macOS — Bash

Create and make non‑traversable:

```
# create-nontraversable.sh
name="bytesync-test-$(date +%Y%m%d%H%M%S)"
root="$PWD/$name"
mkdir -p "$root"
echo "content_$(date +%s)" > "$root/file.txt"

# Remove all perms (owner can chmod back); prevents listing/traversal
chmod 000 "$root"
echo "Created and locked: $root"
```

Restore (allows deletion):

```
# restore-removable.sh
name="$1"; [ -z "$name" ] && { echo "Usage: restore-removable.sh <folder-name>"; exit 1; }
root="$PWD/$name"

# Typical perms so the folder can be traversed/removed
chmod 755 "$root"
echo "Restored perms for: $root"
```

Usage:

```
$ cd ~/Desktop
$ bash create-nontraversable.sh
# ... run inventory/tests targeting ./bytesync-test-YYYYMMDDhhmmss ...
$ bash restore-removable.sh bytesync-test-YYYYMMDDhhmmss
$ rm -rf ./bytesync-test-YYYYMMDDhhmmss
```

> Tip: If you prefer using /tmp for ephemeral tests, run the same scripts from `/tmp`.

---

## General Test Pattern

1) Create a temporary test directory and content

```
# Windows (PowerShell)
$root = Join-Path $env:TEMP ("bytesync-test-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $root | Out-Null
Set-Content -Path (Join-Path $root 'file.txt') -Value 'hello'

# Linux/macOS (bash)
root="/tmp/bytesync-test-$(uuidgen)"; mkdir -p "$root"; echo hello > "$root/file.txt"
```

2) Make the directory (or a child) non‑traversable/inaccessible (per OS below)

3) Run the scenario (inventory/compare)

4) Restore permissions (see Restore sections), then delete the directory

```
# Windows (PowerShell)
Remove-Item -Recurse -Force $root

# Linux/macOS (bash)
rm -rf "$root"
```

---

## Windows (PowerShell / icacls)

Windows volume roots (e.g. `C:\`) may surface as Hidden/System at the API level, even if File Explorer shows them. For permission testing,
prefer a subdirectory under `%TEMP%`.

### Option A — Deny read/list on the test directory (PowerShell Set-Acl)

This prevents listing/traversal (inventory calls will hit `UnauthorizedAccessException`).

```
# Save current ACL (SDDL) so you can restore later
$sddl = (Get-Acl $root).Sddl

# Build a deny rule for the current user on Read & Execute + ListDirectory
$user = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
$rights = [System.Security.AccessControl.FileSystemRights]::ReadAndExecute,
          [System.Security.AccessControl.FileSystemRights]::ListDirectory
$inherit = [System.Security.AccessControl.InheritanceFlags]::None
$prop = [System.Security.AccessControl.PropagationFlags]::None
$deny = [System.Security.AccessControl.AccessControlType]::Deny
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule($user, $rights, $inherit, $prop, $deny)

$acl = Get-Acl $root
$acl.AddAccessRule($rule)
Set-Acl -Path $root -AclObject $acl

# ... run inventory/tests here ...

# Restore original ACL (ensures deletion works)
$aclOriginal = New-Object System.Security.AccessControl.DirectorySecurity
$aclOriginal.SetSecurityDescriptorSddlForm($sddl)
Set-Acl -Path $root -AclObject $aclOriginal
```

Notes:

- The owner can always take ownership/change DACL. Keeping the SDDL snapshot is the most reliable way to restore.
- If you also need to block inherited permissions, remove/disable inheritance before adding the deny:
  `icacls "$root" /inheritance:d`

### Option B — Deny read/list via icacls (alternative)

```
# Save ACLs to a file (relative paths from current dir)
Push-Location (Split-Path $root)
icacls "." /save acls.txt /t

# Deny RX for current user on the test dir only
$u = "$env:USERDOMAIN\$env:USERNAME"
icacls "$root" /deny $u:(RX)

# ... run inventory/tests here ...

# Restore from saved file
icacls . /restore acls.txt
Pop-Location
```

### Make a single file inaccessible (optional)

```
$file = Join-Path $root 'file.txt'
$sddlFile = (Get-Acl $file).Sddl
$aclF = Get-Acl $file
$ruleF = New-Object System.Security.AccessControl.FileSystemAccessRule($user, 'Read', 'None', 'None', 'Deny')
$aclF.AddAccessRule($ruleF)
Set-Acl -Path $file -AclObject $aclF

# ...

$aclOrigF = New-Object System.Security.AccessControl.FileSecurity
$aclOrigF.SetSecurityDescriptorSddlForm($sddlFile)
Set-Acl -Path $file -AclObject $aclOrigF
```

---

## Linux / macOS (POSIX)

On POSIX systems, directory traversal requires the execute (`x`) bit. Removing it makes the directory non‑traversable. Removing read (`r`)
prevents listing.

### Make a directory non‑traversable

```
# Remove all permissions (owner can still chmod back)
chmod 000 "$root"

# Alternatively, remove only execute bit(s)
chmod a-x "$root"
```

### Restore permissions

```
# Typical directory perms
chmod 755 "$root"
# or more conservative
chmod u+rwx,go+rx "$root"
```

### Make a file unreadable (optional)

```
chmod 000 "$root/file.txt"

# Restore
chmod 644 "$root/file.txt"
```

Notes:

- Deleting a directory entry depends on permissions on its parent, not the directory itself. If your test directory is under `/tmp` (owned
  by you) you can still remove it after restoring perms.

---

## Symlinks

ByteSync ignores entries flagged as reparse points/symlinks during inventory to avoid following links inadvertently.

### Create a symlink — Windows (PowerShell / CMD)

```
# PowerShell (requires Developer Mode or elevated rights depending on policy)
New-Item -ItemType SymbolicLink -Path "C:\path\to\link" -Target "C:\path\to\target"

# CMD (directory symlink)
mklink /D C:\path\to\link C:\path\to\target
```

### Create a symlink — Linux/macOS (bash)

```
ln -s /path/to/target /path/to/link
```

---

## Troubleshooting

- Windows: If Deny rules make the directory unreadable and inheritance complicates restore, revert using the saved SDDL or `icacls /restore`
  from the parent directory. As the owner, you can always reset the ACL.
- POSIX: If you cannot traverse a directory, ensure you have write+execute on its parent and reset permissions with `chmod` as the owner.
