<div align="center">
  <img src="assets/logo_bytesync_1280x640.png" width="320" />
  <p>
    <!-- License Badge -->
    <a href="https://github.com/POW-Software/ByteSync/blob/master/LICENSE">
      <img alt="GitHub License" src="https://img.shields.io/github/license/POW-Software/ByteSync" />
    </a>
    <!-- Last Commit Badge -->
    <a href="https://github.com/POW-Software/ByteSync/commits/master">
      <img alt="Last commit" src="https://img.shields.io/github/last-commit/POW-Software/ByteSync" />
    </a>
  </p>
</div>

# ByteSync

**ByteSync** is an open-source, on-demand file synchronization software designed for fast, secure, and efficient remote sync & backup. With end-to-end encryption, your data stays private and protected. By transferring only the differences between files and leveraging optimized batching, compression, and delta transfers, ByteSync minimizes resource usage while maximizing speed. Supporting up to 5 remote locations per session, it offers a smart, self-orchestrated alternative to traditional FTP/SFTP and sync solutions.

---

## Table of Contents
- [Features](#features)
- [Gallery](#gallery)
- [Installation](#installation)
- [Support](#support)
- [License](#license)

---

## Features  

- 🔒 **End-to-end encryption** – Keep your data **secure and private** during transfers.  
- ⚡ **Smart synchronization** – Transfers **only the differences** between files, **compresses data**, groups small files to reduce overhead, and uploads a file **only once** when syncing with multiple recipients.  
- 🌍 **Multi-device sync** – Seamlessly synchronize files **across up to 5 remote machines** in a single session.  
- 🎯 **Customizable sync rules** – Set up rules based on **content, date, size, or presence** to automate synchronization.  
- ☁️ **Easy deployment** – The **server runs in the cloud**, eliminating the need for complex infrastructure or manual FTP configurations. ByteSync ensures **quick and seamless setup** for hassle-free synchronization.  
- 💻 **Cross-platform compatibility** – Works on **Windows, Linux and macOS** for a smooth experience everywhere.  
- 🛠 **Open-source and flexible** – Fully **customizable** to adapt to your workflow and needs.  


---

## Gallery

<div style="display: flex; flex-wrap: wrap; justify-content: center;">
  <div style="flex: 1 0 400px; text-align: center; margin: 10px 10px 30px 10px;">
    <img src="assets/gallery/2025-02-create-or-join-session.png" style="width: 80%; border: 1px solid #ccc;" alt="Create or Join a Cloud Session"/><br>
    <sub>Create or Join a Cloud Session</sub>
    <br><br>
  </div>
  <div style="flex: 1 0 400px; text-align: center; margin: 10px 10px 30px 10px;">
    <img src="assets/gallery/2025-02-settings-and-data-sources.png" style="width: 80%; border: 1px solid #ccc;" alt="Settings & Data Sources"/><br>
    <sub>Settings & Data Sources</sub>
    <br><br>
  </div>
  <div style="flex: 1 0 400px; text-align: center; margin: 10px 10px 30px 10px;">
    <img src="assets/gallery/2025-02-inventory-status-and-comparison-results.png" style="width: 80%; border: 1px solid #ccc;" alt="Inventory Status & Comparison Results"/><br>
    <sub>Inventory Status & Comparison Results</sub>
    <br><br>
  </div>
  <div style="flex: 1 0 400px; text-align: center; margin: 10px 10px 30px 10px;">
    <img src="assets/gallery/2025-02-synchronization-status.png" style="width: 80%; border: 1px solid #ccc;" alt="Synchronization Status"/><br>
    <sub>Synchronization</sub>
    <br><br>
  </div>
</div>

---

## Installation

### Download the Precompiled Client 
1. Visit our [official website](https://www.bytesyncapp.com#download).
2. Download the right version for your operating system.

### Building from Source
If you prefer to build ByteSync from source or want to deploy specific components:

- For **client-side deployment** steps, refer to [docs/client-deployment.md](docs/client-deployment.md).
- For **server-side deployment** steps, refer to [docs/server-deployment.md](docs/server-deployment.md).

---

## Support
If you encounter any issues or if you have feature requests, please open an [issue](https://github.com/POW-Software/ByteSync/issues) on GitHub.

---

## License
This project is licensed under the [MIT License](https://github.com/POW-Software/ByteSync/blob/master/LICENSE).