import React, { Component } from "react";
import { Cell, Column, Table } from "@blueprintjs/table";
import { Button } from "@blueprintjs/core";
import "./simpleTable.css";

class SimpleTable extends Component {
  renderCell = (rowIndex, columnIndex) => {
    const { renderCellContent } = this.props;
    const cellData = this.getCellData(rowIndex, columnIndex);
    return (
      <Cell>
        {renderCellContent
          ? renderCellContent(rowIndex, columnIndex, cellData)
          : cellData}
      </Cell>
    );
  };

  getCellData = (rowIndex, columnIndex) => {
    const { data, columns } = this.props;
    const rowData = data[rowIndex];
    return rowData[columns[columnIndex].property];
  };

  render() {
    const {
      columns,
      data,
      hasPaging,
      currentPage,
      getPage,
      isLoadingPage,
      totalPages
    } = this.props;
    return (
      <div className="simpleTable">
        {hasPaging && (
          <div className="paging-wrapper">
            <div>
              <Button
                minimal
                disabled={currentPage === 0}
                loading={isLoadingPage}
                onClick={() => getPage(currentPage - 1)}
              >
                <span className="pt-icon-chevron-left pt-icon-standard " />
              </Button>
              <span>
                Page {currentPage + 1} of {totalPages}
              </span>
              <Button
                minimal
                disabled={currentPage + 1 === totalPages}
                loading={isLoadingPage}
                onClick={() => getPage(currentPage + 1)}
              >
                <span className="pt-icon-chevron-right pt-icon-standard " />
              </Button>
            </div>
          </div>
        )}
        <Table
          numRows={data.length}
          columnWidths={columns.map(c => c.width)}
          defaultRowHeight={35}
          enableRowHeader={false}
          getCellClipboardData={this.getCellData}
        >
          {columns.map((c, index) => (
            <Column key={index} name={c.title} cellRenderer={this.renderCell} />
          ))}
        </Table>
      </div>
    );
  }
}

export default SimpleTable;
