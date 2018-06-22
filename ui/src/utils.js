import d3 from "d3";

export const aionDashboardUrl = "https://mainnet.aion.network";

export const units = [
  " Sol/s",
  " KSol/s",
  " MSol/s",
  " GSol/s",
  " TSol/s",
  " PSol/s"
];

export const getReadableHashRateString = number => {
  // what tier? (determines prefix)
  const tier = (Math.log10(number) / 3) | 0;

  // get prefix and determine scale
  const unit = units[tier];
  const scale = Math.pow(10, tier * 3);

  // scale the number
  const scaled = number / scale;

  // format number and add prefix as suffix
  return scaled.toFixed(2) + unit;
};

export const timeOfDayFormat = timestamp => {
  const dStr = d3.time.format("%I:%M %p")(new Date(timestamp));

  return dStr.indexOf("0") === 0 ? dStr.slice(1) : dStr;
};

export const fullDateFormat = unparsedDate =>
  d3.time.format("%x %X")(new Date(unparsedDate));

export const getAionDashboardAccountUrl = address =>
  `${aionDashboardUrl}/#/account/${address}`;

export const getAionTransactionUrl = hash =>
  `${aionDashboardUrl}/#/transaction/${hash}`;

export const convertToTimestamp = unparsedDate =>
  new Date(unparsedDate).valueOf();
