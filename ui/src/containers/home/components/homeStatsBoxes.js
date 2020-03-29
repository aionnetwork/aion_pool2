import React from "react";
import { getReadableHashRateString } from "../../../utils";
import StatsBoxes from "../../../components/statsBoxes";

export default ({
  hashRate,
  activeMiners,
  price,
  totalPaid,
  lastMinedBlockNumber,
  isLoadingStats
}) => {
  const boxes = [
    {
      title: "POOL HASHRATE",
      icon: "pt-icon-dashboard",
      value: getReadableHashRateString(hashRate)
    },
    {
      title: "CONNECTED MINERS",
      icon: "pt-icon-wrench",
      value: activeMiners
    },
    {
      title: "LAST MINED BLOCK",
      icon: "pt-icon-box",
      value: lastMinedBlockNumber
    },
    {
      title: "TOTAL PAID",
      icon: "pt-icon-bank-account",
      value: totalPaid.toFixed(2)
    },
    {
      title: "AION PRICE",
      icon: "pt-icon-dollar",
      value: price
    }
  ];

  return <StatsBoxes boxes={boxes} isLoadingData={isLoadingStats} />;
};
