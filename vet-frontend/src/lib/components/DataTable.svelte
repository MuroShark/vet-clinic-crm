<script lang="ts" generics="T">
  import { ChevronLeft, ChevronRight, Search } from 'lucide-svelte';
  import type { Snippet } from 'svelte';

  interface Column {
    header: string;
    sortKey?: string;
    sortValue?: (item: T) => any;
  }

  let {
    columns,
    data,
    searchPlaceholder = 'Поиск...',
    searchFilter,
    emptyMessage = 'Ничего не найдено.',
    row,
    actions
  }: {
    columns: Column[];
    data: T[];
    searchPlaceholder?: string;
    searchFilter?: (item: T, query: string) => boolean;
    emptyMessage?: string;
    row: Snippet<[T]>;
    actions?: Snippet<[T]>;
  } = $props();

  const pageSize = 10;
  let query = $state('');
  let page = $state(1);
  let sortKey = $state<string | null>(null);
  let sortAsc = $state(true);

  function handleSort(key: string) {
    if (sortKey === key) {
      sortAsc = !sortAsc;
    } else {
      sortKey = key;
      sortAsc = true;
    }
    page = 1;
  }

  const filteredData = $derived.by(() => {
    let result = [...data];
    if (query && searchFilter) {
      result = result.filter((item) => searchFilter(item, query));
    }
    if (sortKey) {
      const sortColumn = columns.find((col) => col.sortKey === sortKey);
      result.sort((a, b) => {
        let valA: any;
        let valB: any;
        if (sortColumn?.sortValue) {
          valA = sortColumn.sortValue(a);
          valB = sortColumn.sortValue(b);
        } else {
          valA = (a as any)[sortKey!];
          valB = (b as any)[sortKey!];
        }
        if (valA === undefined || valA === null) return sortAsc ? 1 : -1;
        if (valB === undefined || valB === null) return sortAsc ? -1 : 1;
        if (typeof valA === 'string' && typeof valB === 'string') {
          return sortAsc ? valA.localeCompare(valB) : valB.localeCompare(valA);
        }
        if (typeof valA === 'number' && typeof valB === 'number') {
          return sortAsc ? valA - valB : valB - valA;
        }
        return 0;
      });
    }
    return result;
  });

  const totalPages = $derived(Math.max(1, Math.ceil(filteredData.length / pageSize)));
  const paginatedData = $derived(filteredData.slice((page - 1) * pageSize, (page - 1) * pageSize + pageSize));
</script>

<div class="bg-white border border-slate-200 rounded-[8px] shadow-sm">
  {#if searchFilter}
    <div class="p-4 border-b border-slate-100 flex items-center justify-between gap-4">
      <div class="relative w-full max-w-sm">
        <span class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none text-slate-400">
          <Search class="w-4 h-4" />
        </span>
        <input
          type="text"
          placeholder={searchPlaceholder}
          bind:value={query}
          oninput={() => (page = 1)}
          class="w-full pl-9 pr-4 py-1.5 text-xs text-slate-800 bg-slate-50 border border-slate-200 rounded-[6px] focus:outline-none focus:border-teal-700 focus:bg-white focus:ring-1 focus:ring-teal-700/20 transition-all"
        />
      </div>
      <div class="text-xs text-slate-500 font-medium">
        Всего записей: <span class="font-bold text-slate-900">{filteredData.length}</span>
      </div>
    </div>
  {/if}

  <div class="overflow-x-auto">
    <table class="w-full text-left border-collapse">
      <thead>
        <tr class="bg-slate-50 border-b border-slate-200 text-[11px] uppercase tracking-wider text-slate-500 font-bold">
          {#each columns as col, idx (idx)}
            <th
              onclick={() => col.sortKey && handleSort(col.sortKey)}
              class="px-4 py-3 {col.sortKey ? 'cursor-pointer select-none hover:text-slate-800' : ''}"
            >
              <div class="flex items-center gap-1">
                {col.header}
                {#if col.sortKey && sortKey === col.sortKey}
                  <span class="text-[10px] text-teal-700 font-bold">{sortAsc ? ' ▲' : ' ▼'}</span>
                {/if}
              </div>
            </th>
          {/each}
          {#if actions}
            <th class="px-4 py-3 text-right">Действия</th>
          {/if}
        </tr>
      </thead>
      <tbody class="divide-y divide-slate-100 text-xs">
        {#if paginatedData.length > 0}
          {#each paginatedData as item, rowIdx (rowIdx)}
            <tr class="hover:bg-slate-50/50 transition">
              {@render row(item)}
              {#if actions}
                <td class="px-4 py-3 text-right font-medium">
                  <div class="flex justify-end gap-2">{@render actions(item)}</div>
                </td>
              {/if}
            </tr>
          {/each}
        {:else}
          <tr>
            <td colspan={columns.length + (actions ? 1 : 0)} class="text-center py-8 text-slate-400">
              {emptyMessage}
            </td>
          </tr>
        {/if}
      </tbody>
    </table>
  </div>

  {#if totalPages > 1}
    <div class="p-4 border-t border-slate-100 flex items-center justify-between gap-4 text-xs">
      <div class="text-slate-500">
        Показано {(page - 1) * pageSize + 1} – {Math.min(page * pageSize, filteredData.length)} из {filteredData.length}
      </div>
      <div class="flex items-center gap-1">
        <button
          onclick={() => (page = Math.max(1, page - 1))}
          disabled={page === 1}
          class="p-1 px-2.5 border border-slate-200 rounded-[6px] text-slate-600 hover:bg-slate-50 disabled:opacity-50 cursor-pointer transition-colors"
        >
          <ChevronLeft class="w-3.5 h-3.5 inline-block" />
        </button>
        <span class="px-3 py-1 font-semibold text-slate-700 bg-slate-50 border border-slate-200 rounded-[6px] select-none">
          {page} / {totalPages}
        </span>
        <button
          onclick={() => (page = Math.min(totalPages, page + 1))}
          disabled={page === totalPages}
          class="p-1 px-2.5 border border-slate-200 rounded-[6px] text-slate-600 hover:bg-slate-50 disabled:opacity-50 cursor-pointer transition-colors"
        >
          <ChevronRight class="w-3.5 h-3.5 inline-block" />
        </button>
      </div>
    </div>
  {/if}
</div>
