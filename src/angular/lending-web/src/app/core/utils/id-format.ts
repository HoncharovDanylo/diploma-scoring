function compactId(id: string): string {
  return id.replace(/-/g, '').toUpperCase();
}

export function appDisplayId(id: string): string {
  const c = compactId(id);
  return `APP-${c.slice(0, 8)}`;
}

export function runDisplayId(id: string): string {
  const c = compactId(id);
  return `RUN-${c.slice(0, 8)}`;
}

export function attemptDisplayId(id: string): string {
  const c = compactId(id);
  return `SCR-${c.slice(0, 8)}`;
}
