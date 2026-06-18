<script lang="ts">
  import { page } from '$app/state';
  import { app } from '$lib/stores/app.svelte';
  import ReceptionView from '$lib/components/ReceptionView.svelte';

  $effect(() => {
    app.activeView = 'reception-' + page.params.section;
  });
</script>

<ReceptionView />
