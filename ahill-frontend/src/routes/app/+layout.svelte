<script lang="ts">
  import { goto } from '$app/navigation';
  import { base } from '$app/paths';
  import { page } from '$app/state';
  import { Check, Info, ShieldAlert } from 'lucide-svelte';
  import { app } from '$lib/stores/app.svelte';
  import { canAccess, viewToPath } from '$lib/nav';
  import Sidebar from '$lib/components/Sidebar.svelte';
  import Header from '$lib/components/Header.svelte';
  import AppointmentWizard from '$lib/components/AppointmentWizard.svelte';
  import CashierModal from '$lib/components/CashierModal.svelte';

  let { children } = $props();

  // Группа берётся из URL (обновляется синхронно с навигацией, не зависит от эффектов страниц).
  const group = $derived(page.url.pathname.split('/')[2] ?? '');
  const allowed = $derived(!!app.currentUser && canAccess(group, app.activeRoles));

  // Защита маршрутов: без авторизации — на вход; без прав на раздел — на стартовый раздел роли.
  $effect(() => {
    if (!app.currentUser) {
      goto(`${base}/login`, { replaceState: true });
    } else if (!canAccess(group, app.activeRoles)) {
      goto(viewToPath(app.defaultViewFor(app.activeRoles)), { replaceState: true });
    }
  });
</script>

{#if app.currentUser}
  <div class="flex h-screen bg-[#F8FAFC] font-sans text-slate-900 antialiased overflow-hidden">
    <Sidebar />

    <div class="flex-1 flex flex-col min-w-0 overflow-hidden relative bg-[#F8FAFC]">
      <Header />

      <main class="flex-1 overflow-y-auto p-4 lg:p-8 bg-[#F8FAFC] print:p-0">
        {#if allowed}
          {@render children()}
        {:else}
          <div class="p-8 text-center text-slate-400 text-sm">Перенаправление…</div>
        {/if}
      </main>

      {#if app.toast}
        <div class="fixed bottom-6 right-6 p-4 rounded-lg shadow-xl border flex items-center gap-3 z-50 text-xs bg-slate-900 text-white border-slate-800 transition duration-300">
          {#if app.toast.type === 'success'}
            <span class="p-1 bg-emerald-600 text-white rounded"><Check class="w-4 h-4 text-white" /></span>
          {:else if app.toast.type === 'error'}
            <span class="p-1 bg-red-600 text-white rounded"><ShieldAlert class="w-4 h-4 text-white" /></span>
          {:else}
            <span class="p-1 bg-teal-600 text-white rounded"><Info class="w-4 h-4 text-white" /></span>
          {/if}
          <div>
            <span class="font-semibold block text-[10px] text-slate-400 uppercase tracking-widest">Уведомление системы</span>
            <p class="text-slate-200 mt-0.5">{app.toast.message}</p>
          </div>
        </div>
      {/if}

      {#if app.schedulerApptId}
        <AppointmentWizard
          editingAppointmentId={app.schedulerApptId}
          onClose={() => (app.schedulerApptId = null)}
          onSuccess={(appt: any) => {
            app.schedulerApptId = null;
            app.audit(appt.status === 'Closed' ? 'Закрытие приёма' : 'Изменение приёма', `Приём ${appt.id} (${appt.status})`);
            app.showNotification('Изменения по приёму успешно сохранены и синхронизированы с базой', 'success');
            app.triggerReload();
          }}
        />
      {/if}

      {#if app.showSchedulerNew}
        <AppointmentWizard
          onClose={() => (app.showSchedulerNew = false)}
          onSuccess={(appt) => {
            app.showSchedulerNew = false;
            app.audit('Создание приёма', `Новый приём ${appt.id} (${appt.status})`);
            app.showNotification('Новый приём питомца успешно запланирован в базе данных', 'success');
            app.triggerReload();
          }}
        />
      {/if}

      {#if app.cashierApptId}
        {@const paidApptId = app.cashierApptId}
        <CashierModal
          appointmentId={app.cashierApptId}
          onClose={() => (app.cashierApptId = null)}
          onSuccess={() => {
            app.cashierApptId = null;
            app.audit('Приём оплаты', `Касса по приёму ${paidApptId}`);
            app.showNotification('Платёжные средства успешно зачислены в кассовое хранилище', 'success');
            app.triggerReload();
          }}
        />
      {/if}
    </div>
  </div>
{/if}
