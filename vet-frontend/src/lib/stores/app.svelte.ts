import { browser } from '$app/environment';
import { apiClient, ApiDb } from '$lib/api/client';
import type { User, Employee, Role } from '$lib/types';

type ToastType = 'success' | 'info' | 'error';

class AppState {
  currentUser = $state<User | null>(null);
  currentEmployee = $state<Employee | null>(null);
  activeRoles = $state<Role[]>([]);
  activeBranchId = $state<string>('br-1');
  activeView = $state<string>('reception-main');
  sidebarCollapsed = $state<boolean>(false);
  mobileSidebarOpen = $state<boolean>(false);

  // Триггеры модальных окон
  schedulerApptId = $state<string | null>(null);
  showSchedulerNew = $state<boolean>(false);
  cashierApptId = $state<string | null>(null);

  // Увеличивайте это значение для принудительной перезагрузки subview после изменения данных в модалке
  reloadToken = $state<number>(0);

  toast = $state<{ message: string; type: ToastType } | null>(null);
  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    // SPA на localStorage: восстанавливаем сессию один раз при старте,
    // вне реактивного контекста (не внутри $effect — иначе цикл записи/чтения).
    this.restoreSession();
  }

  restoreSession() {
    if (!browser) return;
    // Сессия восстанавливается из JWT + сохранённого профиля (реальная авторизация)
    const token = localStorage.getItem('vetcrm_token');
    const authRaw = localStorage.getItem('vetcrm_auth');
    if (token && authRaw) {
      try {
        const { user, employee } = JSON.parse(authRaw) as { user: User; employee?: Employee };
        this.currentUser = user;
        this.currentEmployee = employee ?? null;
        this.activeRoles = user.roles;
        this.pickStarterView(user.roles);
      } catch {
        /* повреждённый профиль — игнорируем */
      }
    }
    this.activeBranchId = ApiDb.getActiveBranchId();
  }

  /** Стартовый раздел для набора ролей (чистая функция, без мутаций). */
  defaultViewFor(roles: Role[]): string {
    if (roles.includes('receptionist')) return 'reception-main';
    if (roles.includes('vet')) return 'vet-schedule';
    if (roles.includes('chief_vet')) return 'clinic-schedule';
    if (roles.includes('director')) return 'director-dashboard';
    return 'warehouse-overview';
  }

  private pickStarterView(roles: Role[]) {
    this.activeView = this.defaultViewFor(roles);
  }

  showNotification(message: string, type: ToastType = 'success') {
    this.toast = { message, type };
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => (this.toast = null), 3500);
  }

  loginSuccess(user: User, employee?: Employee) {
    this.currentUser = user;
    this.currentEmployee = employee || null;
    this.activeRoles = user.roles;
    this.pickStarterView(user.roles);
    this.audit('Вход в систему', `Авторизация пользователя ${user.username}`);
    this.showNotification(`Авторизация пройдена: Добро пожаловать, ${user.username}!`, 'success');
  }

  logout() {
    this.audit('Выход из системы', `Завершение сеанса ${this.currentUser?.username ?? ''}`);
    apiClient.auth.logout();
    this.currentUser = null;
    this.currentEmployee = null;
    this.activeRoles = [];
    this.activeView = 'reception-main';
    this.showNotification('Сеанс завершён. Работа с локальным хранилищем закрыта.', 'info');
  }

  /** Записать действие текущего пользователя в журнал аудита (fire-and-forget). */
  audit(action: string, details: string) {
    apiClient.audit.log(this.currentUser?.username ?? 'система', action, details).catch(() => {});
  }

  updateBranchId(branchId: string) {
    ApiDb.setActiveBranchId(branchId);
    this.activeBranchId = branchId;
    this.showNotification('Активный филиал успешно изменен', 'info');
  }

  toggleRole(role: Role) {
    if (this.activeRoles.includes(role)) {
      if (this.activeRoles.length > 1) {
        this.activeRoles = this.activeRoles.filter((r) => r !== role);
      }
    } else {
      this.activeRoles = [...this.activeRoles, role];
    }
  }

  triggerReload() {
    this.reloadToken++;
  }
}

export const app = new AppState();
