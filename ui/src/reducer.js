import { combineReducers } from "redux";
import { routerReducer } from "react-router-redux";
import homeStats from "./containers/home/reducer";
import minerStats from "./containers/minerStats/reducer";
import poolStats from "./containers/stats/reducer";
import pools from "./containers/pools/reducer";
import minerDetails from "./containers/minerDetails/reducer";

export default combineReducers({
  router: routerReducer,
  pools,
  homeStats,
  minerStats,
  poolStats,
  minerDetails
});
