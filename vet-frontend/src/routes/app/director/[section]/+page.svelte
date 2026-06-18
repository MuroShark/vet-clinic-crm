<script lang="ts">
  import { page } from '$app/state';
  import { app } from '$lib/stores/app.svelte';
  import DirectorView from '$lib/components/DirectorView.svelte';

  $effect(() => {
    app.activeView = 'director-' + page.params.section;
  });
</script>

<DirectorView />
