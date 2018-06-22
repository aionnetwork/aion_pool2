import React from "react";
import PropTypes from "prop-types";
import SimpleTable from "components/simpleTable";
import { Link } from "react-router-dom";
import { Spinner } from "@blueprintjs/core";

const columns = [
  {
    title: "Height",
    property: "blockHeight",
    width: 75
  },
  {
    title: "Miner",
    property: "miner",
    width: 580
  }
];

const renderCellContent = (rowIndex, columnIndex, cellData) => {
  if (columnIndex === 1) {
    return <Link to={`/miner/${cellData}`}>{cellData}</Link>;
  }

  return cellData;
};

const LastMinedBlocks = ({ lastMinedBlocks, isLoadingStats }) => (
  <div className="last__block">
    <h2 className="features__title">Last Mined Blocks</h2>

    {isLoadingStats ? (
      <div className="spinnerWrapper">
        <Spinner />
      </div>
    ) : (
      <SimpleTable
        columns={columns}
        data={lastMinedBlocks}
        renderCellContent={renderCellContent}
      />
    )}
  </div>
);

LastMinedBlocks.propTypes = {
  lastMinedBlocks: PropTypes.array.isRequired
};

export default LastMinedBlocks;
