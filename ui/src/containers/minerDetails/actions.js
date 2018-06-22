import {
  getMinerPerformance,
  getMinerDetails,
  getMinerPayments
} from "data-access";
import {
  GET_MINER_DETAILS_START,
  GET_MINER_DETAILS_SUCCESS,
  GET_MINER_DETAILS_FAIL,
  GET_MINER_PAYMENTS_PAGE_START,
  GET_MINER_PAYMENTS_PAGE_SUCCESS,
  GET_MINER_PAYMENTS_PAGE_FAIL
} from "./constants";
import { getPoolId } from "../pools/actions";

const paymentsPageSize = 10;

export const getMinerData = minerAddress => {
  return (dispatch, getState) => {
    getPoolId(dispatch).then(poolId => {
      dispatch({ type: GET_MINER_DETAILS_START });

      Promise.all([
        getMinerDetails(poolId, minerAddress),
        getMinerPerformance(poolId, minerAddress),
        getMinerPayments(poolId, minerAddress, 0, paymentsPageSize)
      ])
        .then(([minerDetails, minerPerformance, payments]) => {
          dispatch({
            type: GET_MINER_DETAILS_SUCCESS,
            payload: {
              minerDetails,
              minerPerformance,
              payments,
              paymentsPageSize
            }
          });
        })
        .catch(() => dispatch({ type: GET_MINER_DETAILS_FAIL }));
    });
  };
};

export const getMinerPaymentsPage = (minerAddress, pageNumber) => {
  return dispatch => {
    dispatch({ type: GET_MINER_PAYMENTS_PAGE_START });

    getPoolId(dispatch).then(poolId => {
      getMinerPayments(poolId, minerAddress, pageNumber, paymentsPageSize)
        .then(payments =>
          dispatch({
            type: GET_MINER_PAYMENTS_PAGE_SUCCESS,
            payload: { payments, paymentsPage: pageNumber }
          })
        )
        .catch(() => dispatch({ type: GET_MINER_PAYMENTS_PAGE_FAIL }));
    });
  };
};
