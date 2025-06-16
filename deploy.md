# Deploy Guide

This document explains how to use the provided `deploy.sh` script to publish your Blazor WebAssembly app to a remote server.

> **Note:** This script is highly customized to the author’s web-server environment and is probably not useful for most users.

---

## Environment Variables

Before running the script, you **must** set the following environment variables (replace the placeholder values with your own):

```bash
export Server__User="your_ssh_username"
export Server__Host="your.remote.host"
export Server__RemoteDir="/absolute/path/to/your/web/blazor"
export Server__Group="your_unix_group"
