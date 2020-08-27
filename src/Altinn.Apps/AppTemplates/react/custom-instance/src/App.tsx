import { createMuiTheme, MuiThemeProvider } from '@material-ui/core';
import * as React from 'react';
import { AltinnAppTheme } from 'altinn-shared/theme';
import ProcessStepWrapper from './containers/ProcessStepWrapper';

const theme = createMuiTheme(AltinnAppTheme);

export default function App() {
  return (
    <MuiThemeProvider theme={theme}>
      <ProcessStepWrapper />
    </MuiThemeProvider>
  );
}
