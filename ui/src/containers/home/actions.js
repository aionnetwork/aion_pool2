import { getLastBlocks, getCoinPrice } from "data-access";
import {
  GET_HOME_STATS_START,
  GET_HOME_STATS_SUCCESS,
  GET_HOME_STATS_FAIL
} from "./constants";
import { getPoolId } from "../pools/actions";

const COIN = "aion";

export const getHomeStats = () => {
  return (dispatch, getState) => {
    dispatch({ type: GET_HOME_STATS_START });

    getPoolId(dispatch).then(poolId => {
      dispatch({ type: GET_HOME_STATS_START });

      Promise.all([getLastBlocks(poolId), getCoinPrice(COIN)])
        .then(([lastMinedBlocks, market]) => {
          dispatch({
            type: GET_HOME_STATS_SUCCESS,
            payload: {
              lastMinedBlocks,
              price: market.priceUSD.toFixed(2)
            }
          });
        })
        .catch(() => dispatch({ type: GET_HOME_STATS_FAIL }));
    });
  };
};
