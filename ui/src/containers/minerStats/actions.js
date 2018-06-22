import { getMiners, getPayments } from "data-access";
import {
  GET_MINER_STATS_START,
  GET_MINER_STATS_SUCCESS,
  GET_MINER_STATS_FAIL,
  GET_PAYMENTS_PAGE_START,
  GET_PAYMENTS_PAGE_SUCCESS,
  GET_PAYMENTS_PAGE_FAIL,
  GET_MINERS_PAGE_START,
  GET_MINERS_PAGE_SUCCESS,
  GET_MINERS_PAGE_FAIL
} from "./constants";
import { getPoolId } from "../pools/actions";

const paymentsPageSize = 10;
const minersPageSize = 10;

export const getMinerStats = () => {
  return (dispatch, getState) => {
    dispatch({ type: GET_MINER_STATS_START });

    getPoolId(dispatch).then(poolId => {
      Promise.all([
        getMiners(poolId, 0, minersPageSize),
        getPayments(poolId, 0, paymentsPageSize)
      ])
        .then(([miners, payments]) =>
          dispatch({
            type: GET_MINER_STATS_SUCCESS,
            payload: { miners, payments, paymentsPageSize, minersPageSize }
          })
        )
        .catch(() => dispatch({ type: GET_MINER_STATS_FAIL }));
    });
  };
};

export const getPaymentsPage = pageNumber => {
  return dispatch => {
    dispatch({ type: GET_PAYMENTS_PAGE_START });

    getPoolId(dispatch).then(poolId => {
      getPayments(poolId, pageNumber, paymentsPageSize)
        .then(payments =>
          dispatch({
            type: GET_PAYMENTS_PAGE_SUCCESS,
            payload: { payments, paymentsPage: pageNumber }
          })
        )
        .catch(() => dispatch({ type: GET_PAYMENTS_PAGE_FAIL }));
    });
  };
};

export const getMinersPage = pageNumber => {
  return dispatch => {
    dispatch({ type: GET_MINERS_PAGE_START });

    getPoolId(dispatch).then(poolId => {
      getMiners(poolId, pageNumber, minersPageSize)
        .then(miners =>
          dispatch({
            type: GET_MINERS_PAGE_SUCCESS,
            payload: { miners, minersPage: pageNumber }
          })
        )
        .catch(() => dispatch({ type: GET_MINERS_PAGE_FAIL }));
    });
  };
};
