import { SagaIterator } from 'redux-saga';
import { fork } from 'redux-saga/effects';

import { watchSetCustomDataSaga } from './setCustomData/setCustomDataSagas';

export default function* CustomSagas(): SagaIterator {
  yield fork(watchSetCustomDataSaga);
}
