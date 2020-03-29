import React from "react";
import SimpleTable from "../../../components/simpleTable";
import { fullDateFormat, getAionTransactionUrl } from "../../../utils";

const columns = [
  {
    title: "Date",
    property: "created",
    width: 200
  },
  {
    title: "Amount",
    property: "amount",
    width: 200
  },
  {
    title: "Transaction Hash",
    property: "transactionConfirmationData",
    width: 565
  }
];

const renderCellContent = (rowIndex, columnIndex, cellData) => {
  switch (columnIndex) {
    case 0:
      return fullDateFormat(cellData);
    case 2:
      return (
        <a href={getAionTransactionUrl(cellData)} target="_blank">
          {cellData}
        </a>
      );
    default:
      return cellData;
  }
};

export default ({
  payments,
  paymentsPage,
  getMinerPaymentsPage,
  isLoadingPaymentsPage,
  totalPaymentsPages
}) =>
  payments.length > 0 ? (
    <React.Fragment>
      <h2 className="table_section_title">Payments</h2>
      <div className="paymentsTable">
        <SimpleTable
          hasPaging
          isLoadingPage={isLoadingPaymentsPage}
          currentPage={paymentsPage}
          totalPages={totalPaymentsPages}
          getPage={getMinerPaymentsPage}
          columns={columns}
          data={payments}
          renderCellContent={renderCellContent}
        />
      </div>
    </React.Fragment>
  ) : (
    ""
  );
