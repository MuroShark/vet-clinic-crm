<script lang="ts">
  import { Truck } from 'lucide-svelte';
  import { apiClient } from '$lib/api/client';
  import { app } from '$lib/stores/app.svelte';
  import type { Batch, Material } from '$lib/types';
  import DataTable from './DataTable.svelte';

  let batches = $state<Batch[]>([]);
  let materials = $state<Material[]>([]);
  let showArrivalModal = $state(false);
  let arrivalForm = $state({ materialId: '', supplier: 'КалинкаВетФарм', purchasePrice: 10, clientPrice: 25, quantity: 100, expiryDate: '' });

  async function loadData() {
    const branchId = app.activeBranchId;
    const bList = await apiClient.batches.list();
    batches = bList.filter((b) => b.branchId === branchId);
    materials = await apiClient.materials.list();
  }

  $effect(() => {
    app.activeBranchId;
    app.reloadToken;
    loadData();
  });

  function getExpirationStatus(expiryDateStr: string) {
    const diffDays = Math.ceil((new Date(expiryDateStr).getTime() - Date.now()) / (1000 * 60 * 60 * 24));
    if (diffDays <= 0) return { label: 'ПРОСРОЧЕНО', style: 'bg-red-100 text-red-800 font-bold border border-red-200' };
    if (diffDays < 7) return { label: `Срок < 7 дней (${diffDays} д)`, style: 'bg-red-50 text-red-700 font-semibold border border-red-200' };
    if (diffDays < 30) return { label: `Срок < 30 дней (${diffDays} д)`, style: 'bg-yellow-50 text-yellow-800 font-semibold border border-yellow-200' };
    return { label: `В норме (${diffDays} д)`, style: 'bg-emerald-50 text-emerald-800 border border-emerald-100' };
  }

  async function handleRegisterArrival(e: SubmitEvent) {
    e.preventDefault();
    if (!arrivalForm.materialId || !arrivalForm.expiryDate || arrivalForm.quantity <= 0) return;
    await apiClient.batches.save({
      materialId: arrivalForm.materialId, supplier: arrivalForm.supplier, purchasePrice: arrivalForm.purchasePrice,
      clientPrice: arrivalForm.clientPrice, totalQuantity: arrivalForm.quantity, remainingQuantity: arrivalForm.quantity,
      expiryDate: arrivalForm.expiryDate, receivedAt: new Date().toISOString().split('T')[0], branchId: app.activeBranchId
    });
    app.audit('Приход на склад', `${materials.find((m) => m.id === arrivalForm.materialId)?.name ?? arrivalForm.materialId}: ${arrivalForm.quantity} шт. от «${arrivalForm.supplier}»`);
    showArrivalModal = false;
    loadData();
    arrivalForm = { materialId: materials[0]?.id || '', supplier: 'КалинкаВетФарм', purchasePrice: 10, clientPrice: 25, quantity: 100, expiryDate: '' };
  }

  function openArrival() {
    arrivalForm = { materialId: materials[0]?.id || '', supplier: 'КалинкаВетФарм', purchasePrice: 15, clientPrice: 35, quantity: 200, expiryDate: '' };
    showArrivalModal = true;
  }

  const columns = [
    { header: 'Артикул', sortKey: 'sku', sortValue: (b: Batch) => materials.find((m) => m.id === b.materialId)?.sku || '' },
    { header: 'Наименование / Категория', sortKey: 'materialName', sortValue: (b: Batch) => materials.find((m) => m.id === b.materialId)?.name || '' },
    { header: 'Партия', sortKey: 'id' },
    { header: 'Остаток партии', sortKey: 'remainingQuantity' },
    { header: 'Себестоимость', sortKey: 'purchasePrice' },
    { header: 'Цена продажи', sortKey: 'clientPrice' },
    { header: 'Поставщик', sortKey: 'supplier' },
    { header: 'Срок годности', sortKey: 'expiryDate' }
  ];
</script>

<div class="space-y-4">
  <div class="flex justify-between items-center bg-white p-3 border border-slate-200 rounded-lg shadow-sm">
    <div>
      <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wide">Аптечный и Складской учёт</h3>
      <p class="text-[10px] text-slate-400 font-normal">Мониторинг партий лекарств и перевязочных материалов. Уведомляет жёлтым/красным цветом при истечении срока годности партий.</p>
    </div>
    <button onclick={openArrival} class="px-3 py-1.5 bg-teal-700 text-white hover:bg-teal-800 transition rounded text-xs font-semibold cursor-pointer select-none flex items-center gap-1 shrink-0"><Truck class="w-3.5 h-3.5" /> Оформить приход товара</button>
  </div>

  <DataTable
    {columns}
    data={batches}
    searchPlaceholder="Поиск остатков на складе по артикулу или названию..."
    searchFilter={(b, q) => {
      const mat = materials.find((m) => m.id === b.materialId);
      return !!(mat && (mat.name.toLowerCase().includes(q.toLowerCase()) || mat.sku.toLowerCase().includes(q.toLowerCase())));
    }}
  >
    {#snippet row(b)}
      {@const mat = materials.find((m) => m.id === b.materialId)}
      {@const pct = (b.remainingQuantity / b.totalQuantity) * 100}
      {@const stat = getExpirationStatus(b.expiryDate)}
      <td class="px-4 py-3"><span class="font-mono text-slate-400">{mat?.sku || 'N/A'}</span></td>
      <td class="px-4 py-3"><div><span class="font-semibold text-slate-800 block">{mat?.name || 'unknown'}</span><span class="text-[10px] text-slate-400">Категория: {mat?.category}</span></div></td>
      <td class="px-4 py-3"><span class="font-mono font-bold uppercase text-[10px] text-slate-500">{b.id.toUpperCase()}</span></td>
      <td class="px-4 py-3">
        <div class="w-full max-w-[120px]">
          <div class="flex justify-between items-center text-[10px] pb-1"><span class="font-bold text-slate-700">{b.remainingQuantity} / {b.totalQuantity} шт.</span><span class="text-slate-400 font-semibold">{pct.toFixed(0)}%</span></div>
          <div class="w-full bg-slate-100 rounded-full h-1"><div class="bg-teal-700 h-1 rounded-full" style="width: {pct}%"></div></div>
        </div>
      </td>
      <td class="px-4 py-3"><span class="font-mono">{b.purchasePrice} руб.</span></td>
      <td class="px-4 py-3"><span class="font-mono text-slate-700 font-semibold">{b.clientPrice} руб.</span></td>
      <td class="px-4 py-3 text-slate-700">{b.supplier}</td>
      <td class="px-4 py-3"><span class="px-2 py-0.5 rounded text-[10px] {stat.style}">{stat.label}</span></td>
    {/snippet}
  </DataTable>

  {#if showArrivalModal}
    <div class="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4 z-50 text-xs text-slate-700">
      <form onsubmit={handleRegisterArrival} class="bg-white w-full max-w-md rounded-xl shadow-xl overflow-hidden border border-slate-200">
        <div class="p-4 bg-teal-700 text-white flex justify-between items-center shrink-0">
          <span class="font-semibold text-xs text-white">Прием партии материалов (Приходная накладная)</span>
          <button type="button" onclick={() => (showArrivalModal = false)} class="text-white hover:bg-white/10 p-1 rounded cursor-pointer leading-none">✕</button>
        </div>
        <div class="p-5 bg-slate-50 space-y-4">
          <div class="space-y-3">
            <div>
              <label class="block text-slate-500 mb-1">Выберите принимаемый материал *</label>
              <select bind:value={arrivalForm.materialId} required class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 bg-white">
                <option value="" disabled>-- Выберите из каталога --</option>
                {#each materials as m (m.id)}<option value={m.id}>[{m.category}] {m.name} ({m.sku})</option>{/each}
              </select>
            </div>
            <div>
              <label class="block text-slate-500 mb-1">Поставщик прихода *</label>
              <input type="text" required bind:value={arrivalForm.supplier} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 bg-white" />
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div><label class="block text-slate-500 mb-1">Себестоимость ед. *</label><input type="number" required min="1" bind:value={arrivalForm.purchasePrice} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 bg-white" /></div>
              <div><label class="block text-slate-500 mb-1">Цена продажи ед. *</label><input type="number" required min="0" bind:value={arrivalForm.clientPrice} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 bg-white" /></div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div><label class="block text-slate-500 mb-1">Количество прихода *</label><input type="number" required min="1" bind:value={arrivalForm.quantity} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 bg-white" /></div>
              <div><label class="block text-slate-500 mb-1">Срок годности партии *</label><input type="date" required bind:value={arrivalForm.expiryDate} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 bg-white font-mono" /></div>
            </div>
          </div>
        </div>
        <div class="p-4 bg-slate-100 flex justify-end gap-2 border-t border-slate-100">
          <button type="button" onclick={() => (showArrivalModal = false)} class="px-3.5 py-1.5 border border-slate-200 rounded text-slate-500 cursor-pointer">Отмена</button>
          <button type="submit" class="px-4 py-1.5 bg-teal-700 hover:bg-teal-800 font-semibold text-white rounded cursor-pointer">Оприходовать партию</button>
        </div>
      </form>
    </div>
  {/if}
</div>
