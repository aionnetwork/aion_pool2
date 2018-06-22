import React from "react";
import StatsBoxes from "components/statsBoxes";
import get from "lodash.get";

export default ({ isLoadingMinerData, minerDetails }) => {
  const boxes = [
    {
      title: "PENDING SHARES",
      icon: "pt-icon-polygon-filter",
      value: get(minerDetails, "pendingShares", 0)
    },
    {
      title: "PENDING BALANCE",
      icon: "pt-icon-time",
      value: get(minerDetails, "pendingBalance", 0).toFixed(3)
    },
    {
      title: "TOTAL PAID",
      icon: "pt-icon-bank-account",
      value: get(minerDetails, "totalPaid", 0).toFixed(3)
    }
  ];

  return <StatsBoxes boxes={boxes} isLoadingData={isLoadingMinerData} />;
};
