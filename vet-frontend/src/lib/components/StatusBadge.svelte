<script lang="ts">
  import type { AppointmentStatus } from '$lib/types';
  let { status, variant = 'reception' }: { status: AppointmentStatus; variant?: 'reception' | 'vet' } = $props();

  const labels = $derived<Record<string, { text: string; cls: string }>>({
    Planned: { text: 'Запланирован', cls: 'bg-blue-100 text-blue-800' },
    InProgress: { text: variant === 'vet' ? 'На приёме (ЭМК)' : 'Приём врача', cls: 'bg-amber-100 text-amber-800' },
    Closed: { text: variant === 'vet' ? 'Закрыт' : 'Завершён', cls: 'bg-emerald-100 text-emerald-800' },
    Cancelled: { text: 'Отмена', cls: 'bg-slate-100 text-slate-500' }
  });
  const badge = $derived(labels[status] ?? { text: status, cls: 'bg-slate-100 text-slate-700' });
</script>

<span class="px-2 py-0.5 rounded-full text-[10px] font-bold {badge.cls}">{badge.text}</span>
