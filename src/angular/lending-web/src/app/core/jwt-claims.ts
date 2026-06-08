const ROLE =
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

export function rolesFromJwt(token: string): string[] {
  try {
    const parts = token.split('.');
    if (parts.length < 2) return [];
    const payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const json = atob(
      payload.length % 4 === 0
        ? payload
        : payload + '==='.slice(0, 4 - (payload.length % 4))
    );
    const body = JSON.parse(json) as Record<string, string | string[] | undefined>;
    const r = body[ROLE] ?? body['role'];
    if (r == null) return [];
    if (Array.isArray(r)) return r;
    return [r];
  } catch {
    return [];
  }
}
