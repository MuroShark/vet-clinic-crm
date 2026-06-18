<script lang="ts">
  import { GitBranch, User, Sliders, Menu, PanelLeft } from 'lucide-svelte';
  import { goto } from '$app/navigation';
  import { apiClient } from '$lib/api/client';
  import { app } from '$lib/stores/app.svelte';
  import type { Branch, Role } from '$lib/types';

  let branches = $state<Branch[]>([]);

  $effect(() => {
    app.reloadToken;
    apiClient.branches.list().then((b) => (branches = b.filter((x) => x.isActive !== false)));
  });

  const roleLabels: { value: Role; label: string }[] = [
    { value: 'receptionist', label: 'Регистратор' },
    { value: 'vet', label: 'Врач (Ветеринар)' },
    { value: 'chief_vet', label: 'Главврач (Клиника)' },
    { value: 'director', label: 'Директор' }
  ];

  const displayName = $derived(app.currentEmployee?.name ?? app.currentUser?.username ?? 'Сотрудник');

  const rolesLabel = $derived(
    app.activeRoles
      .map((r) => (r === 'receptionist' ? 'Регистратура' : r === 'vet' ? 'Врач' : r === 'chief_vet' ? 'Главврач' : 'Управление'))
      .join(' + ')
  );
</script>

<header class="bg-white border-b border-slate-200 h-16 px-4 lg:px-8 flex items-center justify-between gap-2 shrink-0 font-sans z-10">
  <div class="flex items-center gap-2 lg:gap-3 min-w-0">
    <!-- Мобильное меню: открыть боковую панель -->
    <button
      type="button"
      aria-label="Меню"
      onclick={() => (app.mobileSidebarOpen = true)}
      class="lg:hidden p-2 -ml-1 rounded text-slate-600 hover:bg-slate-100 transition cursor-pointer shrink-0"
    >
      <Menu class="w-5 h-5" />
    </button>
    <!-- Десктоп: переключение видимости боковой панели -->
    <button
      type="button"
      aria-label="Свернуть меню"
      onclick={() => (app.sidebarCollapsed = !app.sidebarCollapsed)}
      class="hidden lg:inline-flex p-2 -ml-1 rounded text-slate-600 hover:bg-slate-100 transition cursor-pointer shrink-0"
    >
      <PanelLeft class="w-5 h-5" />
    </button>

    <span class="hidden sm:flex p-1.5 bg-teal-50 rounded text-[#0F766E] shrink-0 border border-teal-100">
      <GitBranch class="w-4 h-4" />
    </span>
    <div class="text-xs">
      <label for="branch-select" class="block text-[10px] text-slate-400 font-bold uppercase tracking-wider">Филиал клиники</label>
      <select
        id="branch-select"
        value={app.activeBranchId}
        onchange={(e) => app.updateBranchId(e.currentTarget.value)}
        class="font-semibold text-slate-800 bg-transparent py-0.5 focus:outline-none border-b border-transparent focus:border-[#0F766E] max-w-[180px] cursor-pointer hover:text-[#0F766E] transition-colors"
      >
        {#each branches as b (b.id)}
          <option value={b.id}>{b.name}</option>
        {/each}
      </select>
    </div>
  </div>

  <div class="hidden items-center gap-3 bg-slate-50 border border-slate-200/60 p-1 px-4 rounded-full text-xs">
    <span class="text-slate-500 font-bold flex items-center gap-1.5 shrink-0 text-[10px] uppercase tracking-wider">
      <Sliders class="w-3.5 h-3.5 text-[#0F766E]" />
      <span>Смена Роли (Для отладки):</span>
    </span>
    <div class="flex items-center gap-1.5">
      {#each roleLabels as rl (rl.value)}
        {@const isChecked = app.activeRoles.includes(rl.value)}
        <button
          onclick={() => app.toggleRole(rl.value)}
          class="px-3 py-1 rounded-full text-[10px] font-bold tracking-tight transition cursor-pointer select-none {isChecked
            ? 'bg-[#0F766E] text-white shadow-sm hover:bg-teal-800'
            : 'bg-white hover:bg-slate-100 text-slate-600 border border-slate-200'}"
        >
          {rl.label}
        </button>
      {/each}
    </div>
  </div>

  <button
    type="button"
    onclick={() => goto('/app/profile')}
    title="Открыть профиль"
    class="flex items-center gap-3 cursor-pointer hover:opacity-80 transition rounded-lg px-1 py-0.5 shrink-0"
  >
    <div class="text-right leading-tight hidden sm:block max-w-[160px]">
      <span class="text-xs font-bold text-slate-800 block truncate">{displayName}</span>
      <span class="text-[10px] font-semibold text-teal-600 uppercase tracking-wide block truncate">{rolesLabel}</span>
    </div>
    <div class="w-9 h-9 rounded-full bg-slate-50 border border-slate-200 flex items-center justify-center text-slate-600 shadow-sm">
      <User class="w-4 h-4 text-slate-500" />
    </div>
  </button>
</header>
