/* eslint-disable @typescript-eslint/no-unused-vars */
import { createMuiTheme, MuiThemeProvider } from '@material-ui/core';
import * as React from 'react';
import { AltinnAppTheme } from 'altinn-shared/theme';
import CustomView from './features/custom/CustomView';

const theme = createMuiTheme(AltinnAppTheme);

export default function App(props: any) {
  return (
    <MuiThemeProvider theme={theme}>
      <CustomView
        textResources={null}
        language={null}
      />
    </MuiThemeProvider>
  );
}
