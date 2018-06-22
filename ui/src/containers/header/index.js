import React, { Component } from "react";
import { withRouter, Link } from "react-router-dom";
import MenuItem from "./menuItem";
import logo from "../assets/logo_aion.svg";
import logo_short from "../assets/logo_aion_mobile.svg";
import { AppToaster } from "../../components/toaster";
import { connect } from "react-redux";

import "./styles.css";

const menuItems = [
  {
    path: "/home",
    isDefault: true,
    tabBaseClass: "pt-icon-large",
    tabIconClass: "pt-icon-home",
    textKey: "home"
  },
  {
    path: "/stats",
    isDefault: false,
    tabBaseClass: "pt-icon-large",
    tabIconClass: "pt-icon-pulse",
    textKey: "stats"
  },
  {
    path: "/miner-stats",
    isDefault: false,
    tabBaseClass: "pt-icon-large",
    tabIconClass: "pt-icon-th-list",
    textKey: "minerStats"
  }
];

const regex = "^0x[a-f0-9]{64}$";

class Header extends Component {
  onKeyPress = ev => {
    const value = ev.target.value;

    if (ev.key === "Enter") {
      if (value.match(regex)) {
        this.props.history.push("/miner/" + value);
        ev.target.value = "";
      } else {
        AppToaster.clear();
        AppToaster.show({
          message: "Please input a valid aion address",
          icon: "warning-sign",
          intent: "WARNING"
        });
      }
    }
  };

  componentWillMount() {
    document.addEventListener("click", this.closeMobileMenu);
  }

  componentWillUnmount() {
    document.removeEventListener("click");
  }

  closeMobileMenu = ev => {
    const mobileMenuButton = document.getElementById("menu-button");

    if (mobileMenuButton.checked && ev.target.tagName !== "SPAN") {
      mobileMenuButton.click();
    }
  };

  render() {
    const { errorMessage } = this.props;
    return (
      <React.Fragment>
        <div className={`page-error ${errorMessage ? "visible" : ""}`}>
          {errorMessage}
        </div>
        <nav id="pool-nav" className="pt-navbar">
          <div className="pt-navbar-group pt-align-left">
            <div className="pt-navbar-heading">
              <Link to="/home">
                <img
                  src={logo}
                  alt="Pool desktop Logo"
                  className="pool__logo desktop__logo"
                />
                <img
                  src={logo_short}
                  alt="Pool mobile Logo"
                  className="pool__logo mobile__logo"
                />
              </Link>
            </div>
          </div>
          <div className="pt-navbar-group pt-align-right menu--items">
            <ul className="desktop__menu">
              {menuItems.map((item, index) => (
                <li key={item.path}>
                  <MenuItem
                    pathname={this.props.location.pathname}
                    menuItem={item}
                  />
                </li>
              ))}
            </ul>
            <div className="pt-input-group .modifier nav__search--field">
              <input
                className="pt-input"
                type="search"
                placeholder="Search miner..."
                onKeyPress={this.onKeyPress}
                dir="auto"
              />
              <span className="pt-icon pt-icon-search" />
            </div>
            <span className="mobile__menu">
              <label htmlFor="menu-button" className="menu-button--label">
                <span className="pt-icon-standard pt-icon-menu" />
              </label>
              <input type="checkbox" id="menu-button" />

              <div className="menu-wrap">
                <div className="side-menu">
                  <div className="side-menu--list">
                    {menuItems.map(item => (
                      <MenuItem
                        key={`${item.path}_mobile`}
                        onItemClicked={this.closeMobileMenu}
                        pathname={this.props.location.pathname}
                        menuItem={item}
                      />
                    ))}
                  </div>
                </div>
              </div>
            </span>
          </div>
        </nav>
      </React.Fragment>
    );
  }
}

const mapStateToProps = ({ pools }) => ({
  errorMessage: pools.errorMessage
});

export default withRouter(connect(mapStateToProps)(Header));
