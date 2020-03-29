import React, { Component } from "react";
// import { Spinner } from "@blueprintjs/core";
import { connect } from "react-redux";
import { bindActionCreators } from "redux";
import { getMinerData, getMinerPaymentsPage } from "./actions";
import "./styles.css";
import MinerStatsBoxes from "./components/minerStatsBoxes";
import PaymentsTable from "./components/paymentsTable";
import MinerHashRate from "./components/minerHashRate";
import { Spinner } from "@blueprintjs/core";
import { getAionDashboardAccountUrl } from "../../utils";

class WorkerStats extends Component {
  componentWillMount() {
    const { match, getMinerData } = this.props;
    getMinerData(match.params.hash);
  }

  componentWillReceiveProps(newProps) {
    const { match, getMinerData } = this.props;
    if (match.params.hash !== newProps.match.params.hash) {
      getMinerData(newProps.match.params.hash);
    }
  }

  render() {
    const {
      match,
      isLoadingMinerData,
      minerDetails,
      hashrate,
      payments,
      paymentsPage,
      getMinerPaymentsPage,
      isLoadingPaymentsPage,
      totalPaymentsPages
    } = this.props;

    return (
      <div className="minerDetailsPage">
        <h1 className="page__title">Stats for</h1>
        <a
          className="page_subtitle"
          href={getAionDashboardAccountUrl(match.params.hash)}
          target="_blank"
        >
          <h2>{match.params.hash}</h2>
        </a>
        <MinerStatsBoxes
          minerDetails={minerDetails}
          isLoadingMinerData={isLoadingMinerData}
        />

        {isLoadingMinerData ? (
          <div className="spinnerWrapper">
            <Spinner />
          </div>
        ) : (
          <React.Fragment>
            <PaymentsTable
              payments={payments}
              totalPaymentsPages={totalPaymentsPages}
              paymentsPage={paymentsPage}
              getMinerPaymentsPage={page =>
                getMinerPaymentsPage(match.params.hash, page)
              }
              isLoadingPaymentsPage={isLoadingPaymentsPage}
            />
            <MinerHashRate hashrate={hashrate} />
          </React.Fragment>
        )}
      </div>
    );
  }
}

const mapStateToProps = ({ minerDetails }) => minerDetails;

const mapDispatchToProps = dispatch =>
  bindActionCreators(
    {
      getMinerData,
      getMinerPaymentsPage
    },
    dispatch
  );

export default connect(mapStateToProps, mapDispatchToProps)(WorkerStats);
