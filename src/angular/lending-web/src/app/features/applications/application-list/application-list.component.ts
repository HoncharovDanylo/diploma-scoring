import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ApplicationListItem, OriginationService } from '../../../core/services/origination.service';
import { applicationStatusLabel } from '../../../core/utils/domain-labels';
import { appDisplayId } from '../../../core/utils/id-format';

@Component({
  selector: 'app-application-list',
  imports: [RouterLink, DatePipe, DecimalPipe],
  templateUrl: './application-list.component.html',
  styleUrl: './application-list.component.scss',
})
export class ApplicationListComponent implements OnInit {
  private readonly origination = inject(OriginationService);
  protected readonly auth = inject(AuthService);

  protected readonly items = signal<ApplicationListItem[] | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly loading = signal(true);

  protected appId(id: string): string {
    return appDisplayId(id);
  }

  protected readonly statusLabel = applicationStatusLabel;

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.error.set(null);
    this.loading.set(true);
    this.origination.list().subscribe({
      next: (r) => {
        this.items.set(r);
        this.loading.set(false);
      },
      error: (e: Error) => {
        this.error.set(e.message);
        this.loading.set(false);
      },
    });
  }
}
