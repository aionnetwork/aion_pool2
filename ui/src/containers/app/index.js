import React from "react";
import { Route, Redirect } from "react-router-dom";

import Home from "../home";
import Stats from "../stats";
import MinerStats from "../minerStats";
import MinerDetails from "../minerDetails";

import "./styles.css";

const App = () => (
  <div id="pool-app" className="pool__app--color">
    <main>
      <Route exact path="/" render={() => <Redirect to="/home" />} />
      <Route exact path="/home" component={Home} />
      <Route exact path="/stats" component={Stats} />
      <Route exact path="/miner-stats" component={MinerStats} />
      <Route exact path="/miner/:hash" component={MinerDetails} />
    </main>
  </div>
);

export default App;
