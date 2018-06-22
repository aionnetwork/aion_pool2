# Aion Pool
Welcome to the official Aion mining pool repository. 
This respositoy is created for pool operators; it contains the Aion mining pool and corresponding UI compatible with Aion 2.7+.

## Key Features
- Supports clusters of pools each running individual currencies
- Ultra-low-latency Stratum implementation using asynchronous I/O (LibUv)
- Adaptive share difficulty ("vardiff")
- PoW validation (hashing) using native code for maximum performance
- Session management for purging DDoS/flood initiated zombie workers
- Payment processing (PPLNS)
- Banning System for banning peers that are flooding with invalid shares
- Live Stats API on Port 4000
- POW (proof-of-work) & POS (proof-of-stake) support
- Detailed per-pool logging to console & filesystem
- Responsive User Interface 
