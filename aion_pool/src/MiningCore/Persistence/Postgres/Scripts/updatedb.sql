set role miningcore;

CREATE TABLE poolnetworkpercentagestats
(
  id BIGSERIAL NOT NULL PRIMARY KEY,
  poolid TEXT NOT NULL,
  networkpercentage DOUBLE PRECISION NOT NULL DEFAULT 0,
  created TIMESTAMP DEFAULT current_timestamp 
);

CREATE INDEX IDX_POOLNETWORKPERCENTAGESTATS_POOL_CREATED on poolnetworkpercentagestats(poolid, created);
CREATE INDEX IDX_POOLNETWORKPERCENTAGESTATS_POOL_CREATED_HOUR on poolnetworkpercentagestats(poolid, date_trunc('hour',created));
CREATE INDEX IDX_POOLNETWORKPERCENTAGESTATS_POOL_CREATED_DAY on poolnetworkpercentagestats(poolid, date_trunc('day',created));
CREATE INDEX IDX_POOLNETWORKPERCENTAGESTATS_POOL_CREATED_MONTH on poolnetworkpercentagestats(poolid, date_trunc('month',created));
CREATE INDEX IDX_POOLNETWORKPERCENTAGESTATS_POOL_CREATED_YEAR on poolnetworkpercentagestats(poolid, date_trunc('year',created));
CREATE INDEX IDX_POOLNETWORKPERCENTAGESTATS_POOL_CREATED_POOLID on poolnetworkpercentagestats(poolid);
