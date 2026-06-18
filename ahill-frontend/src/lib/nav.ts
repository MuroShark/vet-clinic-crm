/**
 * Преобразует идентификатор активного представления (app.activeView) в путь URL и наоборот.
 * Идентификаторы представлений следуют шаблону "<group>-<section>", за исключением
 * изолированных представлений, таких как "warehouse-overview" и "profile".
 */

import type { Role } from './types';

export function viewToPath(view: string): string {
  if (view === 'warehouse-overview') return '/app/warehouse';
  if (view === 'profile') return '/app/profile';
  const idx = view.indexOf('-');
  if (idx === -1) return `/app/${view}`;
  return `/app/${view.slice(0, idx)}/${view.slice(idx + 1)}`;
}

/** Роли, имеющие доступ к каждой группе маршрутов верхнего уровня (отражает видимость в Sidebar). */
const ACCESS: Record<string, Role[]> = {
  reception: ['receptionist'],
  vet: ['vet'],
  clinic: ['chief_vet'],
  warehouse: ['chief_vet', 'receptionist', 'director'],
  director: ['director'],
  admin: ['director']
};

/**
 * Проверяет наличие у ролей пользователя прав доступа к указанной группе маршрутов.
 * @param group - Первый сегмент пути после /app/: reception, vet, clinic, warehouse, director, admin, profile.
 */
export function canAccess(group: string, roles: Role[]): boolean {
  if (group === 'profile' || group === '') return true;
  const allowed = ACCESS[group];
  if (!allowed) return true;
  return roles.some((r) => allowed.includes(r));
}
