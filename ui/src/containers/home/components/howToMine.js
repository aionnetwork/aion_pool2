import React from "react";

const HowToMine = props => (
  <React.Fragment>
    <hr />
    <div className="instruction__block">
      <h2 className="instruction__block--section-title">Prerequisites</h2>
      <ul>
        <li>
          <a href="https://www.ubuntu.com/download">Ubuntu 16.04 or higher</a>
        </li>
        <li>
          <a href="https://medium.com/@shaaslam/how-to-install-oracle-java-9-in-ubuntu-16-04-671e598f0116">
            Java 9 installed
          </a>
        </li>
      </ul>
    </div>
    <div className="instruction__block">
      <h2 className="instruction__block--section-title">
        Getting an Aion Address
      </h2>
      <h3 className="instruction__title">Using Kernel Binary (Most Secure)</h3>
      <ol>
        <li>
          Download the latest Aion kernel release from the
          <a href="https://github.com/aionnetwork/aion/releases">
            official github repository
          </a>
          and unpack the archive
        </li>
        <li>
          Navigate to the Aion kernel folder and run
          <span className="code">./aion.sh -a create</span>
          to create a new address.
        </li>
        <li>
          Your keystore file which holds your private key will be saved in the
          <span className="code">/aion/keystore</span>
          folder. Save it somewhere secure along with your public key.
        </li>
      </ol>
      <h3 className="instruction__title">Using Hosted Wallet</h3>
      <ol>
        <li>
          Go to the hosted Aion wallet application at
          <a href="https://wallet.aion.network">wallet.aion.network</a>
        </li>
        <li>
          Select a password and follow the instructions to generate your
          keystore file.
        </li>
        <li>
          Save your keystore file (which holds your private key) somewhere
          secure, along with your public key.
        </li>
      </ol>
    </div>
    <div className="instruction__block">
      <h2 className="instruction__block--section-title">Setting Up A Miner</h2>
      <p>
        This following are miner reference implementations maintained by Aion
        Foundation. Community members have been working on their own miner
        implementations; see
        <a href="https://forum.aion.network"> forum.aion.network </a>
        for other available miner implementations.
      </p>
      <h3 className="instruction__title">Prerequisites</h3>
      <ol>
        <li>
          You'll need to be running
          <a href="https://www.ubuntu.com/desktop">Ubuntu 16.04 LTS</a>. Non LTS
          versions (&gt; 16.04) may require addional packages that Canonical
          removed from their default ppas
        </li>
      </ol>
      <h3 className="instruction__title">Nvidia CUDA Miner</h3>
      <ol>
        <li>
          Download the latest miner build from the
          <a href="https://github.com/aionnetwork/aion_miner/releases">
            official github repository
          </a>
        </li>
        <li>
          Make sure you have the appropriate drivers to run the miner depending
          on your card.
        </li>
        <li>
          Run the aionminer executable, specifying the <b>pool's stratum url</b>{" "}
          (using -l) and your <b>rewards address</b> (using -u) where you will
          recieve your payouts: <br />
          <div className="code__block">
            &nbsp;<span className="code">{`./aionminer -l 127.0.0.1:3333 -u {rewards_address}`}</span>
          </div>
        </li>
      </ol>
      <h3 className="instruction__title">
        CPU Miner (recommended only for test usage)
      </h3>
      <ol>
        <li>
          Download the latest miner build from the
          <a href="https://github.com/aionnetwork/aion_miner/releases">
            {" "}
            official github repository
          </a>
        </li>
        <li>
          Run the aionminer executable, specifying the <b>pool's stratum url</b>{" "}
          (using -l), your <b>rewards address</b> (using -u) where you will
          recieve your payouts and <b>cpu cores to mine with</b> (using -t):{" "}
          <br />
          <div className="code__block">
            &nbsp;<span className="code">{`./aionminer -l 127.0.0.1:3333 -u {rewards_address} -t {thread_count}`}</span>
          </div>
        </li>
      </ol>
    </div>
  </React.Fragment>
);

export default HowToMine;
