import config from "./config.json";

export const getPools = () => makeRequest(`${config.apiUrl}/pools`);

export const getMiners = (poolId, page, pageSize) =>
  makeRequest(
    `${config.apiUrl}/pools/${poolId}/miners?page=${page}&pageSize=${pageSize}`
  );

export const getLastBlocks = poolId =>
  makeRequest(
    `${
      config.apiUrl
    }/pools/${poolId}/blocks?page=0&pageSize=10&status=confirmed`
  );

export const getMinerDetails = (poolId, minerAddress) =>
  makeRequest(`${config.apiUrl}/pools/${poolId}/miners/${minerAddress}`);

export const getMinerPerformance = (poolId, minerAddress) =>
  makeRequest(
    `${config.apiUrl}/pools/${poolId}/miners/${minerAddress}/performance`
  );

export const getMinerPayments = (poolId, minerAddress, page, pageSize) =>
  makeRequest(
    `${
      config.apiUrl
    }/pools/${poolId}/miners/${minerAddress}/payments?page=${page}&pageSize=${pageSize}`
  );

export const getPoolStats = poolId =>
  makeRequest(`${config.apiUrl}/pools/${poolId}/stats`);

export const getCoinPrice = coinName =>
  makeRequest(`${config.apiUrl}/coin/${coinName}`);

export const getPayments = (poolId, page, pageSize) =>
  makeRequest(
    `${
      config.apiUrl
    }/pools/${poolId}/payments?page=${page}&pageSize=${pageSize}`
  );

const makeRequest = url => {
  return new Promise((resolve, reject) => {
    fetch(url)
      .then(response => {
        if (response.ok) {
          return response.json().catch(() => "Error deserializing JSON data");
        }
      })
      .then(response => resolve(response))
      .catch(error => {
        reject(error);
      });
  });
};
