import React from "react";
import { Link } from "react-router-dom";
import texts from "../../texts";

const MenuItems = ({ pathname, menuItem, onItemClicked }) => {
  return (
    <Link to={menuItem.path} onClick={onItemClicked}>
      <span className={`${menuItem.tabBaseClass} ${menuItem.tabIconClass}`}>
        &nbsp;
      </span>
      <div className="menu__label">{texts[menuItem.textKey]}</div>
      <div
        className={`active__block ${
          pathname === menuItem.path ? "active" : ""
        }`}
      >
        &nbsp;
      </div>
    </Link>
  );
};

export default MenuItems;
