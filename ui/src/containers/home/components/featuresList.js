import React from "react";

export default ({ features }) => (
  <div className="features__block">
    <h2 className="features__title">Features</h2>
    <ul>
      {features.map((feature, index) => (
        <li key={index} className="feature__item">
          {feature}
        </li>
      ))}
    </ul>
  </div>
);
