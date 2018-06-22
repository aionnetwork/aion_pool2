import React from "react";
import { connect } from "react-redux";
import { bindActionCreators } from "redux";
import { getPoolStats } from "./actions";
import { Spinner } from "@blueprintjs/core";
import {
  HashRateChart,
  IntegerChart,
  PercentageChart
} from "components/charts";

class Stats extends React.Component {
  componentWillMount() {
    this.props.getPoolStats();
  }

  render() {
    const {
      hashRate,
      workers,
      poolPercentHashRate,
      isLoadingStats
    } = this.props;

    return (
      <React.Fragment>
        <h1 className="page__title">Recent Statistics</h1>
        {isLoadingStats ? (
          <div className="spinnerWrapper">
            <Spinner />
          </div>
        ) : (
          <React.Fragment>
            <IntegerChart
              title="Active Miners (shares submitted within last 24 hours)"
              data={workers}
            />
            <HashRateChart title="Pool Hashrate" data={hashRate} />
            <PercentageChart
              title="Hashrate Pecentage of Total Network"
              data={poolPercentHashRate}
            />
          </React.Fragment>
        )}
      </React.Fragment>
    );
  }
}

const mapStateToProps = ({ poolStats }) => poolStats;

const mapDispatchToProps = dispatch =>
  bindActionCreators(
    {
      getPoolStats
    },
    dispatch
  );

export default connect(mapStateToProps, mapDispatchToProps)(Stats);
