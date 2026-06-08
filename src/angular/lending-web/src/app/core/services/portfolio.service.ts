import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, catchError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { toUserError } from './api-error';

const root = `${environment.apiUrl}/api/v1/portfolio`;

export interface StartRunRequest {
  businessDate: string;
}

export interface StartRunResponse {
  portfolioRunId: string;
}

export interface PortfolioSelectionRow {
  applicationId: string;
  recommendedPrincipal: number;
  expectedProfitSnapshot: number;
  requestedPrincipal: number | null;
  productMinPrincipal: number | null;
  applicationStatus: string | null;
  approvedPrincipal: number | null;
}

export interface PortfolioRunDetail {
  portfolioRunId: string;
  businessDate: string;
  status: string;
  budgetCapSnapshot: number;
  objectiveValue: number | null;
  usedBudget: number | null;
  expectedPortfolioProfit: number | null;
  selections: PortfolioSelectionRow[];
}

export interface PortfolioDailySummary {
  businessDate: string;
  totalLimit: number;
  issuedToday: number;
  availableBudget: number;
}

@Injectable({ providedIn: 'root' })
export class PortfolioService {
  private readonly http = inject(HttpClient);

  setDailyBudget(body: {
    businessDate: string;
    budgetCap: number;
  }): Observable<void> {
    return this.http
      .put<void>(`${root}/daily-budget`, body)
      .pipe(catchError((e) => toUserError(e)));
  }

  getDailySummary(businessDate: string): Observable<PortfolioDailySummary> {
    return this.http
      .get<PortfolioDailySummary>(`${root}/summary/${businessDate}`)
      .pipe(catchError((e) => toUserError(e)));
  }

  startRun(
    businessDate: string
  ): Observable<StartRunResponse> {
    return this.http
      .post<StartRunResponse>(`${root}/runs`, { businessDate })
      .pipe(catchError((e) => toUserError(e)));
  }

  getRun(id: string): Observable<PortfolioRunDetail> {
    return this.http
      .get<PortfolioRunDetail>(`${root}/runs/${id}`)
      .pipe(catchError((e) => toUserError(e)));
  }
}
