<script lang="ts">
  import { app } from '$lib/stores/app.svelte';
  import WarehouseView from '$lib/components/WarehouseView.svelte';

  $effect(() => {
    app.activeView = 'warehouse-overview';
  });
</script>

<WarehouseView />
