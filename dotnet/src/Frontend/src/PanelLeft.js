import React, { Component } from 'react'
import css from "../css/PanelLeft.less"
import Log from "./widgets/Log"
import GraphView from "./widgets/GraphView"
import ProjectView from "./widgets/ProjectView"
import Cluster from "./widgets/Cluster"
import Discovery from "./widgets/Discovery"
import { CuePlayerModel } from "../fable/Frontend/Widgets/CuePlayer.fs"

function cardClicked(title, global) {
  switch (title.toUpperCase()) {
    case "LOG":
      global.addWidget(new Log());
      break;
    case "GRAPH VIEW":
      global.addWidget(new GraphView());
      break;
    case "CUE PLAYER":
      global.addWidget(new CuePlayerModel());
      break;
    case "PROJECT VIEW":
      global.addWidget(new ProjectView());
      break;
    case "CLUSTER":
      global.addWidget(new Cluster());
      break;
    case "DISCOVERY":
      global.addWidget(new Discovery());
      break;
    default:
      alert("Widget " + title + " is not currently supported")
  }
}

const Card = props => (
  <div className="iris-panel-left-child" onClick={() => cardClicked(props.title, props.global)}>
    <div>{props.letter}</div>
    <div>
      <p><strong>{props.title}</strong></p>
      <p>{props.text}</p>
    </div>
  </div>
);

export default class PanelLeft extends Component {
  constructor(props) {
    super(props);
  }

  render() {
    return (
      <div className="iris-panel-left">
        <Card key={0} global={this.props.global} letter="L" title="LOG" text="Cluster Settings" />
        <Card key={1} global={this.props.global} letter="G" title="Graph View" text="Cluster Settings" />
        <Card key={2} global={this.props.global} letter="C" title="Cue Player" text="Cluster Settings" />
        <Card key={3} global={this.props.global} letter="M" title="Manager" text="Cluster Settings" />
        <Card key={4} global={this.props.global} letter="P" title="Project View" text="Cluster Settings" />
        <Card key={5} global={this.props.global} letter="R" title="Cluster" text="Cluster Settings" />
        <Card key={6} global={this.props.global} letter="D" title="Discovery" text="Cluster Settings" />
        <Card key={7} global={this.props.global} letter="H" title="Unassigned Hosts" text="Cluster Settings" />
        <Card key={8} global={this.props.global} letter="R" title="Remotter" text="Cluster Settings" />
        <Card key={9} global={this.props.global} letter="S" title="Project Settings" text="Cluster Settings" />
        <Card key={10} global={this.props.global} letter="L" title="Library" text="Graph View" />
        <Card key={11} global={this.props.global} letter="P" title="Project Overview (Big)" text="Cluster Settings" />
      </div>
    )
  }
}
