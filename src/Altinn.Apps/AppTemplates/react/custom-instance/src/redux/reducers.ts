/* eslint-disable import/no-cycle */
import { combineReducers,
  Reducer,
  ReducersMapObject } from 'redux';
import CustomReducer, { ICustomState } from '../features/custom/customReducer';

export interface IReducers<T1> {
  custom: T1;
}

export interface IRuntimeReducers extends IReducers<
  Reducer<ICustomState>
  >,
  ReducersMapObject {
}

const reducers: IRuntimeReducers = {
  custom: CustomReducer,
};

export default combineReducers(reducers);
