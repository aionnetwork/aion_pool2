import { getPools as getPoolsApi } from "../../data-access";
import {
  GET_POOLS_START,
  GET_POOLS_SUCCESS,
  GET_POOLS_FAIL
} from "./constants";
import get from "lodash.get";

export const getPools = () => {
  return dispatch => {
    dispatch({ type: GET_POOLS_START });

    return getPoolsApi()
      .then(payload => {
        const poolId = get(payload, "pools[0].id");

        return poolId
          ? dispatch({ type: GET_POOLS_SUCCESS, payload: payload })
          : dispatch({ type: GET_POOLS_FAIL });
      })
      .catch(() => dispatch({ type: GET_POOLS_FAIL }));
  };
};

let poolIdPromise;

export const getPoolId = dispatch => {
  if (poolIdPromise) {
    return poolIdPromise;
  }

  poolIdPromise = dispatch(getPools()).then(({ payload }) => {
    const poolId = get(payload, "pools[0].id");

    return poolId;
  });

  return poolIdPromise;
};
