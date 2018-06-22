import React, { Component } from "react";
import { Spinner } from "@blueprintjs/core";
import { connect } from "react-redux";
import { bindActionCreators } from "redux";
import { getMinerStats, getPaymentsPage, getMinersPage } from "./actions";
import MinersTable from "./components/minersTable";
import PaymentsTable from "./components/paymentsTable";
import "./styles.css";

class MinerStats extends Component {
  componentWillMount() {
    this.props.getMinerStats();
  }

  render() {
    const {
      isLoadingStats,
      miners,
      payments,
      paymentsPage,
      getPaymentsPage,
      isLoadingPaymentsPage,
      minersPage,
      getMinersPage,
      isLoadingMinersPage,
      totalMinersPages,
      totalPaymentsPages
    } = this.props;
    return (
      <div className="minersPage">
        {isLoadingStats ? (
          <div className="spinnerWrapper">
            <Spinner />
          </div>
        ) : (
          <React.Fragment>
            <h1 className="page__title">Miner Stats</h1>
            <h2 className="chartLabel">
              Share Contributors in the Last 24 Hours
            </h2>
            <div className="minerStatsTable">
              <MinersTable
                miners={miners}
                minersPage={minersPage}
                totalMinersPages={totalMinersPages}
                getMinersPage={getMinersPage}
                isLoadingMinersPage={isLoadingMinersPage}
              />
            </div>
            <h2 className="chartLabel">Payments</h2>
            <div className="minerStatsTable">
              <PaymentsTable
                payments={payments}
                totalPaymentsPages={totalPaymentsPages}
                paymentsPage={paymentsPage}
                getPaymentsPage={getPaymentsPage}
                isLoadingPaymentsPage={isLoadingPaymentsPage}
              />
            </div>
          </React.Fragment>
        )}
      </div>
    );
  }
}

const mapStateToProps = ({ minerStats }) => minerStats;

const mapDispatchToProps = dispatch =>
  bindActionCreators(
    {
      getMinerStats,
      getPaymentsPage,
      getMinersPage
    },
    dispatch
  );

export default connect(mapStateToProps, mapDispatchToProps)(MinerStats);
