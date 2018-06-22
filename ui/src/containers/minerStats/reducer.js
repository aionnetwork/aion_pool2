import { handleActions } from "redux-actions";
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

const initialState = {
  isLoadingStats: false,
  miners: [],
  minersPage: 0,
  totalMiners: 0,
  totalMinersPages: 0,
  isLoadingMinersPage: false,
  payments: [],
  paymentsPage: 0,
  totalPayments: 0,
  totalPaymentsPages: 0,
  isLoadingPaymentsPage: false
};

export default handleActions(
  {
    [GET_MINER_STATS_START]: state => ({
      ...state,
      isLoadingStats: true,
      minersPage: 0,
      totalMiners: 0,
      totalMinersPages: 0,
      paymentsPage: 0,
      totalPayments: 0,
      totalPaymentsPages: 0
    }),
    [GET_MINER_STATS_SUCCESS]: (state, { payload }) => ({
      ...state,
      payments: payload.payments.results,
      totalPayments: payload.payments.total,
      miners: payload.miners.results,
      totalMiners: payload.miners.total,
      isLoadingStats: false,
      totalPaymentsPages: Math.ceil(
        payload.payments.total / payload.paymentsPageSize
      ),
      totalMinersPages: Math.ceil(payload.miners.total / payload.minersPageSize)
    }),
    [GET_MINER_STATS_FAIL]: (state, { payload }) => ({
      ...state,
      isLoadingStats: false,
      errorMessage: payload
    }),

    [GET_PAYMENTS_PAGE_START]: state => ({
      ...state,
      isLoadingPaymentsPage: true
    }),
    [GET_PAYMENTS_PAGE_SUCCESS]: (state, { payload }) => ({
      ...state,
      payments: payload.payments.results,
      paymentsPage: payload.paymentsPage,
      isLoadingPaymentsPage: false
    }),
    [GET_PAYMENTS_PAGE_FAIL]: (state, { payload }) => ({
      ...state,
      isLoadingPaymentsPage: false,
      errorMessage: payload
    }),

    [GET_MINERS_PAGE_START]: state => ({ ...state, isLoadingMinersPage: true }),
    [GET_MINERS_PAGE_SUCCESS]: (state, { payload }) => ({
      ...state,
      miners: payload.miners.results,
      minersPage: payload.minersPage,
      isLoadingMinersPage: false
    }),
    [GET_MINERS_PAGE_FAIL]: (state, { payload }) => ({
      ...state,
      isLoadingMinersPage: false,
      errorMessage: payload
    })
  },
  initialState
);
