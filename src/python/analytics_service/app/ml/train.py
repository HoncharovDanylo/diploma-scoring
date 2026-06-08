from __future__ import annotations
import json
import logging
from pathlib import Path
import numpy as np
import pandas as pd
import xgboost as xgb
from sklearn.metrics import roc_auc_score
from sklearn.model_selection import train_test_split
from app.ml.features import FEATURE_COLUMNS
log = logging.getLogger('analytics.train')

def train_and_save(*, package_root: Path, train_csv: Path | None=None, artifacts_dir: Path | None=None, random_state: int=42) -> None:
    root = package_root
    data_path = train_csv or root / 'app' / 'data' / 'credit_train.csv'
    out_dir = artifacts_dir or root / 'artifacts'
    out_dir.mkdir(parents=True, exist_ok=True)
    if not data_path.exists():
        raise FileNotFoundError(f'Training data not found: {data_path}')
    df = pd.read_csv(data_path)
    if 'repayment_to_income_ratio' not in df.columns:
        baseline_daily_rate = 0.0015
        repayment = df['requested_principal'] * (1.0 + baseline_daily_rate * df['term_days'])
        income = df['monthly_income'].replace(0, np.nan)
        df['repayment_to_income_ratio'] = (repayment / income).replace([np.inf, -np.inf], np.nan).fillna(0.0).clip(0.0, 20.0)
    for c in FEATURE_COLUMNS:
        if c not in df.columns:
            raise ValueError(f'Column {c!r} missing in {data_path}')
    if 'default' not in df.columns:
        raise ValueError("Column 'default' missing in training CSV")
    X = df[FEATURE_COLUMNS].astype(np.float64)
    y = df['default'].astype(np.int32)
    X_train, X_val, y_train, y_val = train_test_split(X, y, test_size=0.2, random_state=random_state, stratify=y)
    clf = xgb.XGBClassifier(n_estimators=220, max_depth=5, learning_rate=0.06, subsample=0.88, colsample_bytree=0.88, reg_lambda=1.2, min_child_weight=2, objective='binary:logistic', random_state=random_state, eval_metric='logloss', tree_method='hist')
    clf.fit(X_train, y_train, eval_set=[(X_val, y_val)], verbose=False)
    val_proba = clf.predict_proba(X_val)[:, 1]
    auc = roc_auc_score(y_val, val_proba)
    log.info('Validation ROC-AUC: %.4f', auc)
    model_file = 'xgb_model.json'
    model_path = out_dir / model_file
    clf.save_model(str(model_path))
    meta = {'model_file': model_file, 'feature_columns': FEATURE_COLUMNS, 'model_id': 'xgb-credit-v4', 'model_version': '4.0.0', 'train_rows': len(df), 'validation_roc_auc': round(float(auc), 4), 'positive_class_is_default': True}
    (out_dir / 'model_meta.json').write_text(json.dumps(meta, indent=2), encoding='utf-8')
    log.info('Wrote model to %s', model_path)
