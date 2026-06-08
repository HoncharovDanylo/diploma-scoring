from __future__ import annotations
import json
import logging
from pathlib import Path
from typing import Any
import numpy as np
import pandas as pd
import shap
import xgboost as xgb
from app.ml.features import package_to_dataframe
log = logging.getLogger('analytics.ml')

def _normalize_shap_row(sv: Any) -> np.ndarray:
    if isinstance(sv, list):
        if len(sv) == 2:
            return np.asarray(sv[1], dtype=np.float64).ravel()
        return np.asarray(sv[0], dtype=np.float64).ravel()
    arr = np.asarray(sv, dtype=np.float64)
    if arr.ndim == 2:
        return arr[0].ravel()
    return arr.ravel()

class ScoringEngine:

    def __init__(self, artifacts_dir: Path, top_k: int=5) -> None:
        self._dir = Path(artifacts_dir)
        self._top_k = top_k
        self._clf: xgb.XGBClassifier | None = None
        self._explainer: shap.TreeExplainer | None = None
        self._feature_columns: list[str] = []
        self.model_id = 'xgb-credit-v4'
        self.model_version = '4.0.0'

    def load(self) -> None:
        meta_path = self._dir / 'model_meta.json'
        if not meta_path.exists():
            raise FileNotFoundError(f'Missing {meta_path}. Run: python -m app.train (from analytics_service root).')
        meta = json.loads(meta_path.read_text(encoding='utf-8'))
        self._feature_columns = list(meta['feature_columns'])
        self.model_id = str(meta.get('model_id', self.model_id))
        self.model_version = str(meta.get('model_version', self.model_version))
        model_path = self._dir / str(meta['model_file'])
        if not model_path.exists():
            raise FileNotFoundError(f'Missing model file: {model_path}')
        clf = xgb.XGBClassifier()
        clf.load_model(str(model_path))
        self._clf = clf
        self._explainer = shap.TreeExplainer(clf)
        log.info('Loaded scoring model %s v%s from %s', self.model_id, self.model_version, model_path)

    def predict_with_explanation(self, package: dict[str, Any], pd_threshold: float) -> tuple[float, str, list[dict[str, Any]]]:
        if self._clf is None or self._explainer is None:
            raise RuntimeError('ScoringEngine.load() was not called')
        X = package_to_dataframe(package)
        proba_default = float(self._clf.predict_proba(X)[0, 1])
        final = 'Approve' if proba_default <= pd_threshold else 'Reject'
        raw_sv = self._explainer.shap_values(X)
        sv = _normalize_shap_row(raw_sv)
        names = self._feature_columns
        if sv.size != len(names):
            log.warning('SHAP length %s != features %s; trimming', sv.size, len(names))
            n = min(sv.size, len(names))
            sv = sv[:n]
            names = names[:n]
        order = np.argsort(-np.abs(sv))[:self._top_k]
        factors: list[dict[str, Any]] = []
        for i in order:
            val = float(sv[i])
            if val > 1e-09:
                direction = 'increases_default_risk'
            elif val < -1e-09:
                direction = 'decreases_default_risk'
            else:
                direction = 'neutral'
            factors.append({'featureName': names[i], 'contribution': f'{val:.6f}', 'direction': direction})
        return (proba_default, final, factors)
