import { SagaIterator } from 'redux-saga';
import { call, takeLatest } from 'redux-saga/effects';
// eslint-disable-next-line @typescript-eslint/no-unused-vars
import { get } from 'altinn-shared/utils/networking';
import CustomActions from '../customActions';
import * as CustomActionTypes from '../customActionTypes';

function* setCustomDataSaga(): SagaIterator {
  try {
    // Set up API calls or data processing here
    // f.ex.
    // const customData = yield call(get, url) // Axios GET call to api located at provided url
    const customData: any = {};
    yield call(
      CustomActions.setCustomDataFulfilled,
      customData,
    );
  } catch (err) {
    yield call(CustomActions.setCustomDataRejected, err);
  }
}

export function* watchSetCustomDataSaga(): SagaIterator {
  yield takeLatest(CustomActionTypes.SET_CUSTOM_DATA, setCustomDataSaga);
}
