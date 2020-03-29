import { handleActions } from "redux-actions";
import { convertToTimestamp } from "../../utils";
import {
  GET_MINER_DETAILS_START,
  GET_MINER_DETAILS_SUCCESS,
  GET_MINER_DETAILS_FAIL,
  GET_MINER_PAYMENTS_PAGE_START,
  GET_MINER_PAYMENTS_PAGE_SUCCESS,
  GET_MINER_PAYMENTS_PAGE_FAIL
} from "./constants";

const initialState = {
  isLoadingMinerData: false,
  hashrate: [],
  payments: [],
  paymentsPage: 0,
  isLoadingPaymentsPage: false
};

const handleLoadMinerData = (state, { payload }) => {
  const hashrate = {
    key: "solutions",
    values: []
  };

  payload.minerPerformance.forEach(s => {
    const timeStamp = convertToTimestamp(s.created);
    const stats = s.workers[""];
    hashrate.values.push([timeStamp, stats.hashrate]);
  });

  return {
    ...state,
    minerDetails: payload.minerDetails,
    payments: payload.payments.results,
    totalPayments: payload.payments.total,
    totalPaymentsPages: Math.ceil(
      payload.payments.total / payload.paymentsPageSize
    ),
    hashrate: [hashrate],
    isLoadingMinerData: false
  };
};

export default handleActions(
  {
    [GET_MINER_DETAILS_START]: state => ({
      ...state,
      isLoadingMinerData: true,
      totalPayments: 0,
      totalPaymentsPages: 0
    }),
    [GET_MINER_DETAILS_SUCCESS]: handleLoadMinerData,
    [GET_MINER_DETAILS_FAIL]: (state, { payload }) => ({
      ...state,
      isLoadingMinerData: false,
      errorMessage: payload
    }),
    [GET_MINER_PAYMENTS_PAGE_START]: state => ({
      ...state,
      isLoadingPaymentsPage: true
    }),
    [GET_MINER_PAYMENTS_PAGE_SUCCESS]: (state, { payload }) => ({
      ...state,
      payments: payload.payments.results,
      paymentsPage: payload.paymentsPage,
      isLoadingPaymentsPage: false
    }),
    [GET_MINER_PAYMENTS_PAGE_FAIL]: (state, { payload }) => ({
      ...state,
      isLoadingPaymentsPage: false,
      errorMessage: payload
    })
  },
  initialState
);
