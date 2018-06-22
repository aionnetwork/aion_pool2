# Aion Pool UI

Aion official mining pool UI. 


## Building Requirements

In order to build the code on your machine, you need to have node `9.11` (or higher) and `yarn 1.7` (or higher) installed.
Note: you can also use npm instead of yarn.

## Installation

```bash
git clone from repo
cd into the ui folder
yarn
```

## Configuration

Ensure that the API URL from `src/config.json` is correct (note that depending on your setup, you might encounter cross origin issues as well).

The API URL must point to the mining pool hosting the API. The default configuration assumes the API is running locally on port 4000, changes to this configuration must be applied both to the mining pool as well as the UI. 


## Development build + server

```bash
yarn start
```

## Production Build

```bash
yarn build
```

## Production Deployment

Any web server should be able to host the app but since we're using html5 push state history API,
there might be some extra configuration to be done.
[More info here](https://github.com/facebook/create-react-app/blob/master/packages/react-scripts/template/README.md#serving-apps-with-client-side-routing)

## FAQ

### Is this the only UI compatible with the Aion pool?
No, the UI and mining pool are separate entities allowing UIs to be interchanged as needed. All mining pool data is exposed via REST APIs on the mining pool. 

Documentation on the mining pool APIs may be found [here](https://github.com/coinfoundry/miningcore/wiki/API)

### When I try "run yarn start" I'm getting a compile error

The NODE_PATH variable may cause difficulties when building the required path variables depending on your specific system setup. Once possible fix for this is to replace line 45 of the package.json file with:

```    
"start": "NODE_PATH=./src npm-run-all -p watch-css start-js",
```

### When I try "run yarn build" I'm getting a compile error

The NODE_PATH variable may cause difficulties when building the required path variables depending on your specific system setup. Once possible fix for this is to replace line 45 of the package.json file with:

```    
"build": "NODE_PATH=./src npm-run-all -p build-css build-js"
```

### I don't see my miner stats after connecting a new miner to the pool

The API may not update miner information immediately, try checking miner status again after several minutes. 

