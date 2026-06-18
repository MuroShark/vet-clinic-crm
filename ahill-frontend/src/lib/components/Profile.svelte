<script lang="ts">
  import { User, ShieldCheck, GitBranch, Mail, Phone, BadgeCheck } from 'lucide-svelte';
  import { apiClient } from '$lib/api/client';
  import { app } from '$lib/stores/app.svelte';
  import type { Position, Branch } from '$lib/types';

  let positions = $state<Position[]>([]);
  let branches = $state<Branch[]>([]);

  $effect(() => {
    apiClient.positions.list().then((p) => (positions = p));
    apiClient.branches.list().then((b) => (branches = b));
  });

  const emp = $derived(app.currentEmployee);
  const positionName = $derived(emp ? positions.find((p) => p.id === emp.positionId)?.name ?? 'Сотрудник' : '—');
  const empBranches = $derived(emp ? emp.branchIds.map((id) => branches.find((b) => b.id === id)?.name).filter(Boolean) : []);

  const roleLabel = (r: string) =>
    r === 'receptionist' ? 'Регистратор' : r === 'vet' ? 'Врач' : r === 'chief_vet' ? 'Главврач' : 'Директор';
</script>

<div class="space-y-6 max-w-3xl">
  <div class="bg-white border border-slate-200 rounded-lg shadow-sm overflow-hidden">
    <div class="p-6 bg-[#0D2E2B] text-white flex items-center gap-4">
      <div class="w-16 h-16 rounded-full bg-white/10 border border-white/20 flex items-center justify-center shrink-0">
        <User class="w-8 h-8 text-teal-300" />
      </div>
      <div class="min-w-0">
        <h2 class="text-lg font-bold text-white truncate">{emp?.name ?? app.currentUser?.username}</h2>
        <p class="text-[11px] text-teal-300 uppercase tracking-wider font-semibold mt-0.5">{positionName}</p>
      </div>
    </div>

    <div class="p-6 grid grid-cols-1 md:grid-cols-2 gap-5 text-xs">
      <div class="space-y-1">
        <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5"><BadgeCheck class="w-3.5 h-3.5" /> Логин учётной записи</span>
        <p class="font-semibold text-slate-800 font-mono">{app.currentUser?.username}</p>
      </div>
      <div class="space-y-1">
        <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5"><ShieldCheck class="w-3.5 h-3.5" /> Статус учётной записи</span>
        <p class="font-semibold {app.currentUser?.status === 'active' ? 'text-emerald-700' : 'text-red-500'}">{app.currentUser?.status === 'active' ? 'Активна' : 'Заблокирована'}</p>
      </div>
      {#if emp}
        <div class="space-y-1">
          <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5"><Phone class="w-3.5 h-3.5" /> Телефон</span>
          <p class="font-semibold text-slate-800 font-mono">{emp.phone || 'не указан'}</p>
        </div>
        <div class="space-y-1">
          <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5"><Mail class="w-3.5 h-3.5" /> Электронная почта</span>
          <p class="font-semibold text-slate-800">{emp.email || 'не указана'}</p>
        </div>
        <div class="space-y-1">
          <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Ставка KPI</span>
          <p class="font-semibold text-teal-800 font-mono">{(emp.KPIRate * 100).toFixed(0)}%</p>
        </div>
        <div class="space-y-1">
          <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5"><GitBranch class="w-3.5 h-3.5" /> Филиалы</span>
          <p class="font-semibold text-slate-800">{empBranches.join(', ') || '—'}</p>
        </div>
      {:else}
        <div class="md:col-span-2 p-3 bg-amber-50 border border-amber-200 rounded text-amber-800 text-[11px]">
          Учётная запись не привязана к карточке сотрудника в штатном расписании.
        </div>
      {/if}
    </div>

    <div class="px-6 pb-6">
      <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider block mb-2">Назначенные роли доступа</span>
      <div class="flex flex-wrap gap-2">
        {#each app.currentUser?.roles ?? [] as r (r)}
          <span class="px-3 py-1 rounded-full text-[10px] font-bold bg-teal-50 text-teal-800 border border-teal-100">{roleLabel(r)}</span>
        {/each}
      </div>
    </div>
  </div>
</div>
