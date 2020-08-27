import update from 'immutability-helper';
import { Action, Reducer } from 'redux';
import { ISetCustomDataFulfilled, ISetCustomDataRejected } from './setCustomData/setCustomDataActions';
import * as ActionTypes from './customActionTypes';

export interface ICustomState {
  customData: any;
  error: Error;
}

const initalState: ICustomState = {
  customData: null,
  error: null,
};

const CustomDataReducer: Reducer<ICustomState> = (
  state: ICustomState = initalState,
  action?: Action,
): ICustomState => {
  if (!action) {
    return state;
  }

  switch (action.type) {
    case ActionTypes.SET_CUSTOM_DATA_FULFILLED: {
      const { customData } = action as ISetCustomDataFulfilled;
      return update<ICustomState>(state, {
        customData: {
          $set: customData,
        },
      });
    }
    case ActionTypes.SET_CUSTOM_DATA_REJECTED: {
      const { error } = action as ISetCustomDataRejected;
      return update<ICustomState>(state, {
        error: {
          $set: error,
        },
      });
    }
    default: {
      return state;
    }
  }
};

export default CustomDataReducer;
