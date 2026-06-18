import { browser } from '$app/environment';
import { realApiClient } from './realClient';
import { mockApiClient } from './mockClient';
import type { ApiClient } from './apiClientInterface';

const STORAGE_PREFIX = 'vetcrm_';

/** Управляет предпочтением активного филиала в localStorage (состояние UI, а не данные домена). */
export class ApiDb {
  static getActiveBranchId = (): string => {
    if (!browser) return 'br-1';
    return localStorage.getItem(STORAGE_PREFIX + 'active_branch_id') || 'br-1';
  };
  static setActiveBranchId = (branchId: string) => {
    if (browser) localStorage.setItem(STORAGE_PREFIX + 'active_branch_id', branchId);
  };
}

export const apiClient: ApiClient = import.meta.env.VITE_USE_MOCK === 'true' 
  ? mockApiClient 
  : realApiClient;