import { handleActions } from "redux-actions";
import {
  GET_POOLS_START,
  GET_POOLS_SUCCESS,
  GET_POOLS_FAIL
} from "./constants";

const initialState = {
  list: [],
  isLoadingStats: false
};

export default handleActions(
  {
    [GET_POOLS_START]: state => ({ ...state, isLoadingStats: true }),
    [GET_POOLS_SUCCESS]: (state, { payload }) => ({
      ...state,
      list: payload.pools,
      isLoadingStats: false
    }),
    [GET_POOLS_FAIL]: (state, { payload }) => ({
      ...state,
      isLoadingStats: false,
      errorMessage:
        payload ||
        "Could not load pool data. Make sure the connection  to the server is correctly configured."
    })
  },
  initialState
);
