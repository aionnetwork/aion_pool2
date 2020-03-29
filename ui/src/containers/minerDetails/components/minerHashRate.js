import React from "react";
import { HashRateChart } from "../../../components/charts.js";
import get from "lodash.get";

export default ({ hashrate }) =>
  get(hashrate, "[0].values.length", 0) > 0 ? (
    <HashRateChart title="Hashrate" data={hashrate} />
  ) : (
    ""
  );
