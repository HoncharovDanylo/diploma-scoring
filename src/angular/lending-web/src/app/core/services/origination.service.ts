import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, catchError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { toUserError } from './api-error';

const base = `${environment.apiUrl}/api/v1/applications`;

export interface ApplicationListItem {
  applicationId: string;
  status: string;
  requestedPrincipal: number;
  approvedPrincipal: number | null;
  productMinPrincipal: number;
  requestedTermDays: number;
  calculatedRepaymentAmount: number;
  appliedInterestRatePerDay: number;
  purpose: string | null;
  createdAtUtc: string;
}

export interface CreateApplicationRequest {
  requestedPrincipal: number;
  requestedTermDays: number;
  purpose?: string | null;
}

export interface CreateApplicationConfig {
  productCode: string;
  productVersion: number;
  minPrincipal: number;
  maxPrincipal: number;
  minTermDays: number;
  maxTermDays: number;
  interestRatePerDay: number;
}

export interface CreateApplicationResponse {
  applicationId: string;
  scoringAttemptId: string;
  correlationId: string;
}

export interface ScoringResultDto {
  probabilityOfDefault: number;
  finalDecision: string;
  modelId?: string;
  modelVersion?: string;
  topFactors: Array<{
    featureName: string;
    contribution: string;
    direction?: string;
  }> | null;
  explanation?: string;
}

export interface ScoringAttemptDto {
  scoringAttemptId: string;
  status: string;
  result: ScoringResultDto | null;
}

export interface ApplicationDetail {
  applicationId: string;
  status: string;
  requestedPrincipal: number;
  approvedPrincipal: number | null;
  productMinPrincipal: number;
  requestedTermDays: number;
  calculatedRepaymentAmount: number;
  appliedInterestRatePerDay: number;
  productCode: string;
  productVersion: number;
  purpose: string | null;
  applicant: { fullName: string };
  scoring: Array<{
    scoringAttemptId: string;
    status: string;
    result:
      | (ScoringResultDto & { explanation?: string })
      | null;
  }>;
}

export interface ManualVerificationItem {
  applicationId: string;
  requestedPrincipal: number;
  productMinPrincipal: number;
  productMaxPrincipal: number;
  requestedTermDays: number;
  purpose: string | null;
  createdAtUtc: string;
  applicant: {
    fullName: string;
    monthlyIncome: number | null;
    employmentStatus: string | null;
  };
  scoring: {
    probabilityOfDefault: number;
    finalDecision: string;
    modelId: string;
    modelVersion: string;
  } | null;
}

export interface LoanProductDto {
  loanProductId: string;
  productCode: string;
  version: number;
  isActive: boolean;
  minPrincipal: number;
  maxPrincipal: number;
  minTermDays: number;
  maxTermDays: number;
  interestRatePerDay: number;
  updatedAtUtc: string;
}

export interface CreateLoanProductRequest {
  productCode: string;
  minPrincipal: number;
  maxPrincipal: number;
  minTermDays: number;
  maxTermDays: number;
  interestRatePerDay: number;
  activate: boolean;
}

@Injectable({ providedIn: 'root' })
export class OriginationService {
  private readonly http = inject(HttpClient);

  list(): Observable<ApplicationListItem[]> {
    return this.http
      .get<ApplicationListItem[]>(base)
      .pipe(catchError((e) => toUserError(e)));
  }

  create(
    body: CreateApplicationRequest
  ): Observable<CreateApplicationResponse> {
    return this.http
      .post<CreateApplicationResponse>(base, body)
      .pipe(catchError((e) => toUserError(e)));
  }

  getById(id: string): Observable<ApplicationDetail> {
    return this.http
      .get<ApplicationDetail>(`${base}/${id}`)
      .pipe(catchError((e) => toUserError(e)));
  }

  getCreateConfig(): Observable<CreateApplicationConfig> {
    return this.http
      .get<CreateApplicationConfig>(`${base}/config`)
      .pipe(catchError((e) => toUserError(e)));
  }

  getManualPending(): Observable<ManualVerificationItem[]> {
    return this.http
      .get<ManualVerificationItem[]>(
        `${environment.apiUrl}/api/v1/manual-verification/pending`
      )
      .pipe(catchError((e) => toUserError(e)));
  }

  applyManualDecision(
    applicationId: string,
    decision: 'Approve' | 'Reject',
    reason?: string,
    approvedPrincipal?: number
  ): Observable<void> {
    const body: {
      decision: string;
      reason?: string;
      approvedPrincipal?: number;
    } = { decision };
    if (reason) body.reason = reason;
    if (approvedPrincipal != null && Number.isFinite(approvedPrincipal)) {
      body.approvedPrincipal = approvedPrincipal;
    }
    return this.http
      .post<void>(
        `${environment.apiUrl}/api/v1/manual-verification/${applicationId}/decision`,
        body
      )
      .pipe(catchError((e) => toUserError(e)));
  }

  listLoanProducts(): Observable<LoanProductDto[]> {
    return this.http
      .get<LoanProductDto[]>(`${environment.apiUrl}/api/v1/loan-products`)
      .pipe(catchError((e) => toUserError(e)));
  }

  createLoanProduct(body: CreateLoanProductRequest): Observable<void> {
    return this.http
      .post<void>(`${environment.apiUrl}/api/v1/loan-products`, body)
      .pipe(catchError((e) => toUserError(e)));
  }

  activateLoanProduct(loanProductId: string): Observable<void> {
    return this.http
      .put<void>(`${environment.apiUrl}/api/v1/loan-products/${loanProductId}/activate`, {})
      .pipe(catchError((e) => toUserError(e)));
  }
}
