import { SagaIterator } from 'redux-saga';
import { fork } from 'redux-saga/effects';

import { watchSetCustomDataSaga } from './setCustomData/setCustomDataSagas';

export default function* (): SagaIterator {
  yield fork(watchSetCustomDataSaga);
}
