<div align="center">
  <img src="/assets/logo_bytesync_1280x640.png" width="360" alt="ByteSync Logo" />

<h3>Sync, Backup & Deduplication — Cross-platform, LAN & WAN</h3>

  <p align="center">
    <!-- License -->
    <a href="https://github.com/POW-Software/ByteSync/blob/master/LICENSE">
      <img alt="License: MIT" src="https://img.shields.io/github/license/POW-Software/ByteSync?color=brightgreen" />
    </a>
    <!-- Last commit -->
    <a href="https://github.com/POW-Software/ByteSync/commits/master">
      <img alt="Last commit" src="https://img.shields.io/github/last-commit/POW-Software/ByteSync?color=blue" />
    </a>
    <!-- GitHub stars -->
    <a href="https://github.com/POW-Software/ByteSync/stargazers">
      <img alt="GitHub stars" src="https://img.shields.io/github/stars/POW-Software/ByteSync?color=gold" />
    </a>
    <!-- Platform -->
    <img alt="Platforms" src="https://img.shields.io/badge/platforms-Windows%20%7C%20macOS%20%7C%20Linux-blueviolet" />
    <!-- Language -->
    <img alt="Language" src="https://img.shields.io/badge/language-C%23-blue" />
  </p>

  <p align="center">
    <a href="https://www.bytesyncapp.com/">Website</a> •
    <a href="https://www.bytesyncapp.com/documentation">Documentation</a> •
    <a href="https://www.bytesyncapp.com/blog">Blog</a> •
    <a href="https://github.com/POW-Software/ByteSync/discussions">Community</a>
  </p>
</div>

# 🌀 ByteSync

**Free, open-source software for file synchronization, backup & deduplication — local or remote, secure, and cross-platform.**  
No VPN. No setup. Full control.

---

## 🔍 Overview

**ByteSync** is an **on-demand file synchronization, backup and deduplication tool** designed for professionals, teams, and individuals.  
It works **locally or across remote sites** with **no VPN, firewall, or manual configuration** required.

ByteSync compares files using a **block-level delta engine** and transfers only modified parts, saving time and bandwidth.  
All data is protected with **end-to-end encryption (E2EE)**, ensuring privacy even when synchronization passes through remote connections.

---

## 🧠 Key Features

- ⚡ **Hybrid synchronization** — works across LAN and remote endpoints automatically.
- 🔒 **End-to-end encryption** — no data ever leaves your devices unencrypted.
- 📦 **Delta-based transfers** — only modified data blocks are exchanged.
- 🧩 **Backup & deduplication** — compare, clean, and consolidate datasets easily.
- 💻 **Cross-platform** — runs on **Windows, Linux, and macOS**.
- 🚫 **Zero network configuration** — no VPN, no port forwarding, no hassle.
- ⚙️ **Rule-based control** — define granular synchronization conditions and actions.
- 🧾 **Transparent results** — full comparison and report before applying changes.

---

## 🌍 How It Works

ByteSync automatically detects the best route between endpoints:

- **Local peers** communicate directly through LAN for maximum speed.
- **Remote peers** connect securely through encrypted relay channels.
- You can mix **local and remote endpoints** in the same session effortlessly.

This hybrid model makes ByteSync ideal for distributed setups, multi-site businesses, or personal backups.

<div align="center">
    <img src="/assets/gallery/2025-10-local-and-remote-sync.png" width="480" alt="ByteSync Logo" />
</div>

---

## 🧰 Use Cases

- 🏢 **Small Business:** synchronize local servers and remote backup sites.
- 💾 **IT Teams:** keep distributed datasets consistent across multiple environments.
- 🏠 **Home Users:** sync between PC, NAS, and cloud backup targets.
- 🧹 **Deduplication:** identify and remove duplicate files using filtering and rule-based actions.
- 🎬 **Creative Studios:** collaborate efficiently on large media files with delta-based transfers.

---

## 📷 Gallery

<div align="center" style="display: flex; flex-wrap: wrap; justify-content: center;">
  <div style="flex: 1 0 400px; text-align: center; margin: 10px 10px 30px 10px;">
    <img src="assets/gallery/2025-02-create-or-join-session.png" style="width: 70%; border: 1px solid #ccc;" alt="Create or Join a Synchronization Session"/><br>
    <sub>Create or Join a Synchronization Session</sub>
    <br><br>
  </div>
  <div style="flex: 1 0 400px; text-align: center; margin: 10px 10px 30px 10px;">
    <img src="assets/gallery/2025-02-settings-and-data-sources.png" style="width: 70%; border: 1px solid #ccc;" alt="Settings & Data Sources"/><br>
    <sub>Settings & Data Sources</sub>
    <br><br>
  </div>
  <div style="flex: 1 0 400px; text-align: center; margin: 10px 10px 30px 10px;">
    <img src="assets/gallery/2025-02-inventory-status-and-comparison-results.png" style="width: 70%; border: 1px solid #ccc;" alt="Inventory Status & Comparison Results"/><br>
    <sub>Inventory Status & Comparison Results</sub>
    <br><br>
  </div>
  <div style="flex: 1 0 400px; text-align: center; margin: 10px 10px 30px 10px;">
    <img src="assets/gallery/2025-02-synchronization-status.png" style="width: 70%; border: 1px solid #ccc;" alt="Synchronization Status"/><br>
    <sub>Synchronization</sub>
    <br><br>
  </div>
</div>

---

## 🧩 Installation

### Download the Precompiled Client 
1. Visit our [official website](https://www.bytesyncapp.com#download).
2. Download the right version for your operating system.

### Building from Source
If you prefer to build ByteSync from source or want to deploy specific components:

- For **client-side deployment** steps, refer to [docs/client-deployment.md](docs/client-deployment.md).
- For **server-side deployment** steps, refer to [docs/server-deployment.md](docs/server-deployment.md).

---

## 🧑‍💻 Support
If you encounter any issues or if you have feature requests, please open an [issue](https://github.com/POW-Software/ByteSync/issues) on GitHub.

---

## 🛡️ License
This project is licensed under the [MIT License](https://github.com/POW-Software/ByteSync/blob/master/LICENSE).