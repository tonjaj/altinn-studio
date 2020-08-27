import { SagaIterator, Task } from 'redux-saga';
import { fork } from 'redux-saga/effects';
import { sagaMiddleware } from './store';

import CustomSagas from '../features/custom/customSagas';

function* root(): SagaIterator {
  yield fork(CustomSagas);
}

export const initSagas: () => Task = () => sagaMiddleware.run(root);
