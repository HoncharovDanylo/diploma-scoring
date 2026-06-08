const APPLICATION_STATUS_UA: Record<string, string> = {
  Submitted: 'Подано',
  ScoringPending: 'Скоринг виконується',
  ScoringRejectedFinal: 'Відхилено (скоринг)',
  ManualVerificationPending: 'Очікує рішення адміністратора',
  ManualApproved: 'Схвалено',
  ManualRejected: 'Відхилено',
  ScoringFailed: 'Помилка скорингу',
};

const PORTFOLIO_RUN_STATUS_UA: Record<string, string> = {
  Pending: 'Виконується',
  Succeeded: 'Завершено успішно',
  Failed: 'Помилка',
};

const SCORING_ATTEMPT_STATUS_UA: Record<string, string> = {
  Pending: 'Виконується',
  Succeeded: 'Завершено',
  Failed: 'Помилка',
};

const SCORING_DECISION_UA: Record<string, string> = {
  Approve: 'Схвалити (модель)',
  Reject: 'Відхилити (модель)',
  ManualReview: 'На перевірку адміністратора',
};

export function applicationStatusLabel(status: string | null | undefined): string {
  if (!status) return '—';
  return APPLICATION_STATUS_UA[status] ?? status;
}

export function portfolioRunStatusLabel(status: string | null | undefined): string {
  if (!status) return '—';
  return PORTFOLIO_RUN_STATUS_UA[status] ?? status;
}

export function scoringAttemptStatusLabel(status: string | null | undefined): string {
  if (!status) return '—';
  return SCORING_ATTEMPT_STATUS_UA[status] ?? status;
}

export function scoringDecisionLabel(decision: string | null | undefined): string {
  if (!decision) return '—';
  return SCORING_DECISION_UA[decision] ?? decision;
}

const SHAP_DIRECTION_UA: Record<string, string> = {
  increases_default_risk: 'підвищує ризик',
  decreases_default_risk: 'знижує ризик',
  neutral: 'нейтрально',
};

export function shapDirectionLabel(direction: string | null | undefined): string {
  if (!direction) return '—';
  return SHAP_DIRECTION_UA[direction] ?? direction;
}
