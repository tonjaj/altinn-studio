import { ActionCreatorsMapObject, bindActionCreators, Action } from 'redux';
import { store } from '../../redux/store';
import * as SetCustomDataActions from './setCustomData/setCustomDataActions';

export interface ICustomActions extends ActionCreatorsMapObject {
  setCustomData: () => Action;
  setCustomDataFulfilled: (customData: any) => SetCustomDataActions.ISetCustomDataFulfilled;
  setCustomDataRejected: (error: Error) => SetCustomDataActions.ISetCustomDataRejected;
}

const actions: ICustomActions = {
  setCustomData: SetCustomDataActions.setCustomData,
  setCustomDataFulfilled: SetCustomDataActions.setCustomDataFulfilled,
  setCustomDataRejected: SetCustomDataActions.setCustomDataRejected,
};

const CustomActions: ICustomActions = bindActionCreators(actions, store.dispatch);

export default CustomActions;
