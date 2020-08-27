import { Action } from 'redux';
import * as ActionTypes from '../customActionTypes';

export interface ISetCustomDataFulfilled extends Action {
  customData: any;
}

export interface ISetCustomDataRejected extends Action {
  error: Error;
}

export function setCustomData(): Action {
  return {
    type: ActionTypes.SET_CUSTOM_DATA,
  };
}

export function setCustomDataFulfilled(
  customData: any,
): ISetCustomDataFulfilled {
  return {
    type: ActionTypes.SET_CUSTOM_DATA_FULFILLED,
    customData,
  };
}

export function setCustomDataRejected(error: Error): ISetCustomDataRejected {
  return {
    type: ActionTypes.SET_CUSTOM_DATA_REJECTED,
    error,
  };
}
