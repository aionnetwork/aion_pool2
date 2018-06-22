import { handleActions } from "redux-actions";
import { convertToTimestamp } from "utils";
import {
  GET_POOL_STATS_START,
  GET_POOL_STATS_SUCCESS,
  GET_POOL_STATS_FAIL
} from "./constants";

const initialState = {
  isLoadingStats: false,
  hashRate: [],
  workers: [],
  poolPercentHashRate: []
};

const handleLoadStats = (state, { payload }) => {
  const hashRate = {
    key: "Sol/s",
    values: []
  };
  const workers = {
    key: "Miners",
    values: []
  };
  const poolPercentHashRate = {
    key: "Percentage",
    values: []
  };

  payload.stats.forEach(s => {
    const timeStamp = convertToTimestamp(s.created);

    hashRate.values.push([timeStamp, s.poolHashrate]);
    workers.values.push([timeStamp, s.connectedMiners]);
    if (s.networkHashrate > 0 && s.poolHashrate / s.networkHashrate < 2)
      //ignore absurd outliers and division by 0
      poolPercentHashRate.values.push([
        timeStamp,
        s.poolHashrate / s.networkHashrate
      ]);
  });

  return {
    ...state,
    isLoadingStats: false,
    hashRate: [hashRate],
    workers: [workers],
    poolPercentHashRate: [poolPercentHashRate]
  };
};

export default handleActions(
  {
    [GET_POOL_STATS_START]: state => ({ ...state, isLoadingStats: true }),
    [GET_POOL_STATS_SUCCESS]: handleLoadStats,
    [GET_POOL_STATS_FAIL]: (state, { payload }) => ({
      ...state,
      isLoadingStats: false,
      errorMessage: payload
    })
  },
  initialState
);
