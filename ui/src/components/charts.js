import React from "react";
import NVD3Chart from "react-nvd3";
import d3 from "d3";
import { getReadableHashRateString, timeOfDayFormat } from "utils";
import "./charts.css";

const chartDefaults = {
  margin: { left: 90, right: 40 },
  useInteractiveGuideline: true,
  clipEdge: false,
  x: d => d[0],
  y: d => d[1]
};

const ChartWrapping = ({ children, title }) => (
  <React.Fragment>
    <div className="chartLabel">{title}</div>
    <div className="chartWrapper">{children}</div>
  </React.Fragment>
);

export const HashRateChart = ({ data, title }) => (
  <ChartWrapping title={title}>
    <NVD3Chart
      type="lineChart"
      xAxis={{ tickFormat: timeOfDayFormat }}
      yAxis={{ tickFormat: getReadableHashRateString }}
      datum={data}
      {...chartDefaults}
    />
  </ChartWrapping>
);

export const IntegerChart = ({ data, title }) => (
  <ChartWrapping title={title}>
    <NVD3Chart
      type="stackedAreaChart"
      xAxis={{ tickFormat: timeOfDayFormat }}
      yAxis={{ tickFormat: d3.format("d") }}
      showControls={false}
      datum={data}
      {...chartDefaults}
    />
  </ChartWrapping>
);

export const PercentageChart = ({ data, title }) => (
  <ChartWrapping title={title}>
    <NVD3Chart
      type="lineChart"
      xAxis={{ tickFormat: timeOfDayFormat }}
      yAxis={{ tickFormat: d3.format(".0%") }}
      datum={data}
      {...chartDefaults}
    />
  </ChartWrapping>
);
