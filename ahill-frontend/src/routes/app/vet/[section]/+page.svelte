<script lang="ts">
  import { page } from '$app/state';
  import { app } from '$lib/stores/app.svelte';
  import VetView from '$lib/components/VetView.svelte';

  $effect(() => {
    app.activeView = 'vet-' + page.params.section;
  });
</script>

<VetView />
