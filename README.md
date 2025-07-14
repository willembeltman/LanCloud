# LanCloud – Distributed RAID-like Storage over LAN
LanCloud is a C# project inspired by concepts from Storage Spaces Direct and blockchain-based redundancy, aiming to create a lightweight, distributed storage system across multiple machines on a local network.

## 💡 Core Concept
LanCloud splits and distributes data across several machines with XOR-based RAID-5-style redundancy. You define how many backup copies you want, and the system automatically spreads the data across available nodes in the most optimal way.

## 🚀 What’s Working Now

✅ A working RAID-5 system based on XOR parity across multiple machines

✅ Built-in FTP server that translates standard FTP operations to the internal LanCloud protocol

✅ Support for multiple servers, each responsible for a portion of the RAID

✅ Ability to download files back via FTP, reconstructed from the distributed data

## 🧪 Experimental Ideas

Support for temporary peer nodes (short-lived machines that assist in speed but aren’t relied upon for durability)

Long-lived nodes form the core backbone of the system's reliability

Intelligent distribution algorithm based on node availability and backup count

## ⚙️ Use Case
Perfect for LAN setups where you want a resilient, decentralized storage pool without relying on expensive enterprise solutions.

**👉 Still very much a work in progress, but the core ideas are functional and demonstrate the viability of distributed parity-based storage.**

