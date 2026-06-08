import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService, UserProfile } from '../../../core/services/auth.service';

@Component({
  selector: 'app-profile',
  imports: [RouterLink, DatePipe, DecimalPipe],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export class ProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);

  protected readonly profile = signal<UserProfile | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly loading = signal(true);

  async ngOnInit(): Promise<void> {
    try {
      this.profile.set(await this.auth.getMe());
    } catch (e) {
      this.error.set(
        e instanceof Error ? e.message : 'Не вдалося завантажити профіль'
      );
    } finally {
      this.loading.set(false);
    }
  }

  protected employmentLabel(code: string | null | undefined): string {
    if (!code) return '—';
    const m: Record<string, string> = {
      Employed: 'Працівник',
      SelfEmployed: 'ФОП / самозайнятий',
      Unemployed: 'Безробітний / без зайнятості',
      Student: 'Студент',
      Retired: 'Пенсіонер',
    };
    return m[code] ?? code;
  }
}
