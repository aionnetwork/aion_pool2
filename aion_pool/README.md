## AION Pool

AION official pool

Fork from https://github.com/coinfoundry/miningcore

The pool software has been fully tested on Ubuntu 18.04 (desktop and server), while compatible with Windows we recommended using Ubuntu server while operating the pool. 

### Features

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
- Runs on Linux and Windows

### Runtime Requirements

- [.Net Core 2.1 **SDK** (or higher) ](https://www.microsoft.com/net/download/core#/runtime)
  - register key and feed
  - install .Net

- [PostgreSQL Database (version 10 or higher)](https://www.postgresql.org/)
  - for Ubuntu: https://www.postgresql.org/download/linux/ubuntu/
    - May also be installed via the package manager: ```sudo apt-get install postgresql```
  - for Windows: https://www.postgresql.org/download/windows/

- [Aion Kernel](https://github.com/aionnetwork/aion/releases)


### Recommended Minimum Hardware Specifications

- SSD based storage
- 32 GB RAM
- 16 core CPU

* Assumes the Aion pool and kernel will be running on the same machine.

## Building the Pool

### Clone the repository

```console
$ apt-get update -y 
$ apt-get -y install git cmake build-essential libssl-dev pkg-config libboost-all-dev libsodium-dev 
$ git clone https://github.com/aionnetwork/aion_pool2.git
```
Then navigate to the cloned repository.

### PostgreSQL Database setup

Due to slight differences between AION desktop and server configurations, ensure the correct instruction set is followed based on your installation method. 

#### Ubuntu Desktop APT package manager:

PostgreSQL packages may be installed through the apt package mangager. Creating a postgres user under the current username may simplify database configuration; the following command creates that postgres user.

```sudo -u postgres createuser -s $USERNAME```

Choose a secure password for the miningcore user, this account will be the to store and process share payouts.

```console
$ createuser miningcore
$ createdb miningcore
$ psql miningcore
```

Run the query after login:

```sql
alter user miningcore with encrypted password '{PASSWORD}'; 
grant all privileges on database miningcore to miningcore;
```
\q (to quit)

Navigate back to the terminate and import the database schema (ensure you are at the root of the repository)

Import the database schema (make sure that you are in the root of the repositiory directory):

```console
$ cd src/MiningCore/Persistence/Postgres/Scripts
$ psql -d miningcore -f createdb.sql
```

#### Ubuntu Server APT package manager:


Choose a secure password for the miningcore user, this account will be the to store and process share payouts.

```console
$ sudo -u postgres createuser miningcore
$ sudo -u postgres createdb miningcore
$ sudo -u postgres psql
```

Run the query after login:

```sql
alter user miningcore with encrypted password '{PASSWORD}'; 
grant all privileges on database miningcore to miningcore;
```
\q (to quit)

Navigate back to the terminate and import the database schema (ensure you are at the root of the repository)

Import the database schema (make sure that you are in the root of the repositiory directory):

```console
$ cd src/MiningCore/Persistence/Postgres/Scripts
$ sudo -u postgres psql -d miningcore -f createdb.sql
```

## Building the Aion Pool


#### Linux (Ubuntu example)

```console
$ cd aion_pool/src/MiningCore
$ ./linux-build.sh
```

#### Windows

```dosbatch
> git clone https://github.com/coinfoundry/miningcore
> cd miningcore/src/MiningCore
> windows-build.bat
```

### Building from Source (Visual Studio)

- Install [Visual Studio 2017](https://www.visualstudio.com/vs/) (Community Edition is sufficient) for your platform
- Install [.Net Core 2.0 SDK](https://www.microsoft.com/net/download/core) for your platform
- Open `MiningCore.sln` in VS 2017


#### After successful build

Before proceeding ensure the aion configuration file has been modified to meet mining pool specifications. 

**Aion kernel config file:**

1. Make sure you have an Aion account set up (instructions can be found [here](https://github.com/aionnetwork/aion/wiki/Aion-Owner's-Manual/#user-content-kernel)). 

2. Enable to net API option. 

Navigate to the Aion kernel config.xml file, and under the <apis-enabled> section, include the "net" option:

```
<apis-enabled>web3,eth,personal,stratum,net</apis-enabled>
```

3. Increase the number of RPC server threads.

The number minimum number of RPC threads should be 4, although it is recommended that the number of threads be increased to 8. Failure to increase the number of threads may cause RPC requests to become queued and eventually dropped.

```
<threads>8</threads>

```

4. Replace the miner address with your account address; this is the address which will receive mining rewards. 

**Back to the aion_pool configuration file:**

Navigate to the build directory: 
``` 
cd ../../build
```

Now copy the sample configuration file 
```
cp ../examples/aion_pool.json .
```

Make the following edits to the config file (aion_pool.json):
- ***persistence***: change the password to one set during postgres set-up
- ***paymentProcessing*** 
  - (optional) change the "interval" to the desired time until each payout, in seconds
  - (optional) change the minimum payout threshold
- ***pools*** 
  - Change ***id*** to the desired pool id
  - Change ***address*** to the miner address configured in the kernel
  - Change ***rewardRecepients*** to operator addresses that will receive rewards at a certain defined percentage. Multiple rewardRecepients may be setup to by defining comma separated recipient objects.
   - ***ports***
     - Change the port and listening address as needed based on your server configuration
     - Adjust difficulty, minDiff, maxDiff, targetTime and retargetTime as needed.
   - ***paymentProcessing***
     - ***accountPassword***: Change to contain the "password" which matches the miner account specified in the kernel configuration.
     - (Optional) Change ***minimumConfirmations*** to the number of blocks (depth in the chain) that must be established before rewards begin to be distributed.
     - (Optional) ***minimumPeerCount*** is the number of peers your kernel must be connected to minimum this number of peers in order to distribute rewards. This ensures rewards are not distributed should the aion kernel become disconnected from the rest of the network. 

More information on the configuration file [here](https://github.com/coinfoundry/miningcore/wiki/Configuration).

## Starting the pool

***NOTE***: Start the Aion Kernel before proceeding with starting the pool

Navigate to the root of the pool directory. 

```
cd ../../build
dotnet MiningCore.dll -c aion_pool.json
```
