/* eslint-disable @typescript-eslint/no-unused-vars */
import * as React from 'react';

export interface ICustomViewProps {
  textResources: any;
  language: any;
}

export default function CustomView(props: ICustomViewProps) {
  return (
    <div id='custom-view-container'>
      <h2>This is a custom view</h2>
    </div>
  );
}
