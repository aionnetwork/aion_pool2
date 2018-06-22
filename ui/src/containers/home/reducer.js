import { handleActions } from "redux-actions";
import {
  GET_HOME_STATS_START,
  GET_HOME_STATS_SUCCESS,
  GET_HOME_STATS_FAIL
} from "./constants";

const initialState = {
  isLoadingStats: false,
  hashRate: "",
  activeMiners: "",
  price: "",
  lastMinedBlockNumber: "",
  lastMinedBlocks: []
};

export default handleActions(
  {
    [GET_HOME_STATS_START]: state => ({ ...state, isLoadingStats: true }),
    [GET_HOME_STATS_SUCCESS]: (state, { payload }) => ({
      ...state,
      isLoadingStats: false,
      ...payload
    }),
    [GET_HOME_STATS_FAIL]: (state, { payload }) => ({
      ...state,
      isLoadingStats: false,
      errorMessage: payload
    })
  },
  initialState
);
