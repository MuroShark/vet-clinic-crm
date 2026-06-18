<script lang="ts">
  import {
    Home, Users, Calendar, Layers, Stethoscope, ChevronLeft, ChevronRight,
    Database, ShieldCheck, TrendingUp, FileSpreadsheet, GitBranch, FolderLock, LogOut, Clock, ShoppingBag, KeyRound, ScrollText
  } from 'lucide-svelte';
  import { goto } from '$app/navigation';
  import { app } from '$lib/stores/app.svelte';
  import { viewToPath } from '$lib/nav';
  import type { Role } from '$lib/types';

  const hasRole = (rList: Role[]) => app.activeRoles.some((role) => rList.includes(role));

  const menuGroups = $derived([
    {
      title: 'Регистратура',
      visible: hasRole(['receptionist']),
      items: [
        { id: 'reception-main', label: 'Быстрые действия', icon: Home },
        { id: 'reception-queue', label: 'Очередь сегодня', icon: Clock },
        { id: 'reception-appointments', label: 'Все приёмы', icon: Calendar },
        { id: 'reception-clients', label: 'Реестр клиентов', icon: Users },
        { id: 'reception-patients', label: 'Паспорта питомцев', icon: FolderLock }
      ]
    },
    {
      title: 'Врач',
      visible: hasRole(['vet']),
      items: [
        { id: 'vet-schedule', label: 'Моё расписание', icon: Calendar },
        { id: 'vet-patients', label: 'Карта пациентов', icon: Stethoscope },
        { id: 'vet-diagnoses', label: 'Справочник диагнозов', icon: Database }
      ]
    },
    {
      title: 'Клиническое управление',
      visible: hasRole(['chief_vet']),
      items: [
        { id: 'clinic-schedule', label: 'Расписание клиники', icon: Calendar },
        { id: 'clinic-services', label: 'Услуги и цены', icon: Layers },
        { id: 'clinic-diagnoses', label: 'Диагнозы', icon: Database }
      ]
    },
    {
      title: 'Склад',
      visible: hasRole(['chief_vet', 'receptionist', 'director']),
      items: [{ id: 'warehouse-overview', label: 'Складской учёт', icon: ShoppingBag }]
    },
    {
      title: 'Аналитика & Отчёты',
      visible: hasRole(['director']),
      items: [
        { id: 'director-dashboard', label: 'Главный дашборд', icon: TrendingUp },
        { id: 'director-payments', label: 'Список оплат', icon: Layers },
        { id: 'director-reports', label: 'Генератор отчётов', icon: FileSpreadsheet }
      ]
    },
    {
      title: 'Администрирование',
      visible: hasRole(['director']),
      items: [
        { id: 'admin-users', label: 'Кадры и доступы', icon: ShieldCheck },
        { id: 'admin-accounts', label: 'Учётные записи', icon: KeyRound },
        { id: 'admin-branches', label: 'Филиалы клиники', icon: GitBranch },
        { id: 'admin-audit', label: 'Журнал действий', icon: ScrollText }
      ]
    }
  ]);

  // Компактный режим (только иконки) применяется на десктопе; на мобильных всегда показываются названия.
  const compact = $derived(app.sidebarCollapsed && !app.mobileSidebarOpen);

  function navigate(id: string) {
    app.mobileSidebarOpen = false;
    goto(viewToPath(id));
  }
</script>

{#if app.mobileSidebarOpen}
  <button
    type="button"
    aria-label="Close menu"
    onclick={() => (app.mobileSidebarOpen = false)}
    class="fixed inset-0 bg-black/50 z-40 lg:hidden"
  ></button>
{/if}

<aside
  class="bg-[#0D2E2B] text-slate-300 border-r border-teal-900 flex flex-col justify-between shrink-0
         fixed lg:static inset-y-0 left-0 z-50 w-64
         transform transition-transform duration-300 lg:translate-x-0
         {app.mobileSidebarOpen ? 'translate-x-0' : '-translate-x-full'}
         {app.sidebarCollapsed ? 'lg:w-16' : 'lg:w-60'}"
>
  <div class="border-b border-teal-800/50 flex items-center overflow-hidden {compact ? 'p-3 justify-center' : 'p-6 justify-between gap-3'}">
    <div class="flex items-center gap-3 min-w-0 {compact ? 'justify-center' : ''}">
      <div class="w-10 h-10 rounded-md bg-teal-700 flex items-center justify-center shrink-0 shadow-sm">
        <span class="font-bold text-white text-base select-none">V</span>
      </div>
      {#if !compact}
        <div class="min-w-0 leading-tight">
          <span class="font-bold text-white tracking-tight block text-base">Veterinary Clinic</span>
          <span class="text-[10px] text-teal-400 font-semibold uppercase tracking-wider block">Information System</span>
        </div>
      {/if}
    </div>
  </div>

  <div class="sidebar-scroll flex-1 overflow-y-auto px-2 py-4 space-y-4">
    {#each menuGroups as group, groupIdx (groupIdx)}
      {#if group.visible}
        <div class="space-y-1">
          {#if !compact}
            <div class="px-3 text-[10px] font-bold text-teal-500 uppercase tracking-wider mb-1.5">{group.title}</div>
          {/if}
          <nav class="space-y-0.5">
            {#each group.items as item (item.id)}
              {@const Icon = item.icon}
              {@const isActive = app.activeView === item.id}
              <button
                onclick={() => navigate(item.id)}
                title={compact ? item.label : undefined}
                class="w-full flex items-center gap-3 px-4 py-2.5 rounded-sm text-xs select-none cursor-pointer transition-colors {isActive
                  ? 'bg-teal-800/30 text-white font-semibold border-l-4 border-teal-500'
                  : 'text-slate-300 hover:bg-teal-800/20'} {compact ? 'justify-center py-2.5 px-0' : ''}"
              >
                <Icon class="w-4 h-4 shrink-0 transition-opacity {isActive ? 'opacity-100 text-teal-400' : 'opacity-70'}" />
                {#if !compact}<span class="truncate">{item.label}</span>{/if}
              </button>
            {/each}
          </nav>
        </div>
      {/if}
    {/each}
  </div>

  <div class="p-3 border-t border-teal-800/50 space-y-1">
    <button
      onclick={() => app.logout()}
      title="Выход из системы"
      class="w-full flex items-center gap-3 px-4 py-2 rounded text-xs select-none cursor-pointer text-slate-400 hover:bg-teal-800/20 hover:text-red-400 transition {compact ? 'justify-center px-0' : ''}"
    >
      <LogOut class="w-4 h-4 shrink-0" />
      {#if !compact}<span class="truncate">Выйти</span>{/if}
    </button>
    <button
      onclick={() => (app.sidebarCollapsed = !app.sidebarCollapsed)}
      class="hidden lg:flex w-full items-center justify-center py-1.5 rounded text-slate-400 hover:bg-teal-800/20 hover:text-slate-200 transition cursor-pointer"
    >
      {#if app.sidebarCollapsed}<ChevronRight class="w-4 h-4" />{:else}<ChevronLeft class="w-4 h-4" />{/if}
    </button>
  </div>
</aside>

<style>
  .sidebar-scroll {
    scrollbar-width: thin;
    scrollbar-color: rgba(94, 234, 212, 0.25) transparent;
  }
  .sidebar-scroll::-webkit-scrollbar {
    width: 6px;
  }
  .sidebar-scroll::-webkit-scrollbar-track {
    background: transparent;
  }
  .sidebar-scroll::-webkit-scrollbar-thumb {
    background-color: rgba(94, 234, 212, 0.22);
    border-radius: 9999px;
  }
  .sidebar-scroll:hover::-webkit-scrollbar-thumb {
    background-color: rgba(94, 234, 212, 0.4);
  }
</style>
