import React from 'react';
import Tabs from 'muicss/lib/react/tabs';
import Tab from 'muicss/lib/react/tab';

export default function PanelRight(props) { return (
  <Tabs id="panel-right" style={{width: props.width}}>
    <Tab label="PARAMETER" >
      <div>
        <p>Laboris cillum ut cillum dolore velit excepteur qui ea non incididunt in officia sit magna.</p>
      </div>
    </Tab>
    <Tab label="WIDGETS" >
      <div>
        <p>Id excepteur cupidatat proident fugiat.</p>
      </div>
    </Tab>
  </Tabs>
)}