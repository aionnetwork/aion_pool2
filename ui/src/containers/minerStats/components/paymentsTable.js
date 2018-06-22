import React from "react";
import SimpleTable from "components/simpleTable";
import { fullDateFormat, getAionTransactionUrl } from "utils";
import { Link } from "react-router-dom";

const columns = [
  {
    title: "Miner Address",
    property: "address",
    width: 510
  },
  {
    title: "Transaction Hash",
    property: "transactionConfirmationData",
    width: 280
  },
  {
    title: "Amount",
    property: "amount",
    width: 100
  },
  {
    title: "Date",
    property: "created",
    width: 150
  }
];

const renderCellContent = (rowIndex, columnIndex, cellData) => {
  switch (columnIndex) {
    case 0:
      return <Link to={`/miner/${cellData}`}>{cellData}</Link>;
    case 1:
      return (
        <a href={getAionTransactionUrl(cellData)} target="_blank">
          {cellData}
        </a>
      );
    case 3:
      return fullDateFormat(cellData);
    default:
      return cellData;
  }
};

export default ({
  payments,
  paymentsPage,
  getPaymentsPage,
  isLoadingPaymentsPage,
  totalPaymentsPages
}) =>
  payments.length > 0 ? (
    <SimpleTable
      hasPaging
      isLoadingPage={isLoadingPaymentsPage}
      currentPage={paymentsPage}
      totalPages={totalPaymentsPages}
      getPage={getPaymentsPage}
      columns={columns}
      data={payments}
      renderCellContent={renderCellContent}
    />
  ) : (
    <p>No Payments yet.</p>
  );
