#!/bin/bash

# the following dev-dependencies must be installed
# Ubuntu: apt-get update -y && apt-get -y install git cmake build-essential libssl-dev pkg-config libboost-all-dev libsodium-dev

BUILDIR=${1:-../../build}

echo "Building into $BUILDIR"

# publish
mkdir -p $BUILDIR
dotnet publish -c Release --framework netcoreapp3.0 -o $BUILDIR

# build libcryptonote
(cd ../Native/libcryptonote && make)
cp ../Native/libcryptonote/libcryptonote.so $BUILDIR
(cd ../Native/libcryptonote && make clean)

# build libmultihash
(cd ../Native/libmultihash && make)
cp ../Native/libmultihash/libmultihash.so $BUILDIR
(cd ../Native/libmultihash && make clean)

# copy libmultihash to bin for Visual Code
mkdir -p ./bin/Debug/netcoreapp3.0/ ./bin/Release/netcoreapp3.0/ ../MiningCore.Tests/bin/Debug/netcoreapp3.0/
cp ../../build/libmultihash.so ./bin/Debug/netcoreapp3.0/
cp ../../build/libmultihash.so ./bin/Release/netcoreapp3.0/
cp ../../build/libmultihash.so ../MiningCore.Tests/bin/Debug/netcoreapp3.0/
