set role miningcore;

CREATE TABLE poolhashratepercentagestats
(
  id BIGSERIAL NOT NULL PRIMARY KEY,
  poolid TEXT NOT NULL,
  poolhashrate DOUBLE PRECISION NOT NULL DEFAULT 0,
  networkhashrate DOUBLE PRECISION NOT NULL DEFAULT 0,
  created TIMESTAMP DEFAULT current_timestamp 
);

CREATE INDEX IDX_POOLHASHRATEPERCENTAGESTATS_POOL_CREATED on poolhashratepercentagestats(poolid, created);
CREATE INDEX IDX_POOLHASHRATEPERCENTAGESTATS_POOL_CREATED_HOUR on poolhashratepercentagestats(poolid, date_trunc('hour',created));
CREATE INDEX IDX_POOLHASHRATEPERCENTAGESTATS_POOL_CREATED_DAY on poolhashratepercentagestats(poolid, date_trunc('day',created));
CREATE INDEX IDX_POOLHASHRATEPERCENTAGESTATS_POOL_CREATED_MONTH on poolhashratepercentagestats(poolid, date_trunc('month',created));
CREATE INDEX IDX_POOLHASHRATEPERCENTAGESTATS_POOL_CREATED_YEAR on poolhashratepercentagestats(poolid, date_trunc('year',created));
CREATE INDEX IDX_POOLHASHRATEPERCENTAGESTATS_POOL_CREATED_POOLID on poolhashratepercentagestats(poolid);
