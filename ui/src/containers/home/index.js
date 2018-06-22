import React, { Component } from "react";
import { bindActionCreators } from "redux";
import { connect } from "react-redux";
import { getHomeStats } from "./actions";
import HomeStatsBoxes from "./components/homeStatsBoxes";
import HowToMine from "./components/howToMine";
import FeaturesSection from "./components/featuresSection";
import texts from "../../texts";
import "./styles.css";
import get from "lodash.get";

class Home extends Component {
  componentWillMount() {
    this.props.getHomeStats();
  }

  render() {
    const { pool, price, lastMinedBlocks, isLoadingStats } = this.props;

    return (
      <div id="pool-home">
        <h1 className="page__title">{texts.homeTitle}</h1>
        <HomeStatsBoxes
          {...{
            hashRate: get(pool, "poolStats.poolHashrate"),
            activeMiners: get(pool, "poolStats.connectedMiners"),
            price,
            lastMinedBlockNumber: get(lastMinedBlocks, "[0].blockHeight"),
            isLoadingStats,
            totalPaid: get(pool, "totalPaid", 0)
          }}
        />
        <FeaturesSection
          poolFeePercent={get(pool, "poolFeePercent", "")}
          lastMinedBlocks={lastMinedBlocks}
          isLoadingStats={isLoadingStats}
        />
        <HowToMine />
      </div>
    );
  }
}

const mapStateToProps = ({ homeStats, pools }) => ({
  lastMinedBlocks: homeStats.lastMinedBlocks,
  price: homeStats.price,
  isLoadingStats: homeStats.isLoadingStats,
  pool: pools.list[0]
});

const mapDispatchToProps = dispatch =>
  bindActionCreators(
    {
      getHomeStats
    },
    dispatch
  );

export default connect(mapStateToProps, mapDispatchToProps)(Home);
