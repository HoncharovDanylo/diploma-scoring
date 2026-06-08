from __future__ import annotations
import logging
from typing import Any
import pandas as pd
log = logging.getLogger('analytics.ml')
FEATURE_COLUMNS: list[str] = ['requested_principal', 'term_days', 'income_to_principal_ratio', 'credit_utilization_ratio', 'months_since_last_delinquency', 'purpose_encoding', 'applicant_age_years', 'monthly_income', 'employment_encoding', 'repayment_to_income_ratio']

def package_to_dataframe(package: dict[str, Any]) -> pd.DataFrame:
    feats = package.get('features') or {}
    row: dict[str, float] = {}
    missing: list[str] = []
    for c in FEATURE_COLUMNS:
        if c in feats:
            row[c] = float(feats[c])
        else:
            missing.append(c)
            row[c] = 0.0
    if missing:
        log.warning('Missing feature keys %s; filled with 0.0', missing)
    return pd.DataFrame([row], columns=FEATURE_COLUMNS)
