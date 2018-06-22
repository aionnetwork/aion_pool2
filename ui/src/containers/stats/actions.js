import { getPoolStats as getPoolStatsApi } from "data-access";
import {
  GET_POOL_STATS_START,
  GET_POOL_STATS_SUCCESS,
  GET_POOL_STATS_FAIL
} from "./constants";
import { getPoolId } from "../pools/actions";

export const getPoolStats = () => {
  return (dispatch, getState) => {
    dispatch({ type: GET_POOL_STATS_START });

    getPoolId(dispatch).then(poolId => {
      getPoolStatsApi(poolId)
        .then(stats =>
          dispatch({ type: GET_POOL_STATS_SUCCESS, payload: stats })
        )
        .catch(() => dispatch({ type: GET_POOL_STATS_FAIL }));
    });
  };
};
