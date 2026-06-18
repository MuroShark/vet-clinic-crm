<script lang="ts">
  import { page } from '$app/state';
  import { app } from '$lib/stores/app.svelte';
  import ClinicView from '$lib/components/ClinicView.svelte';

  $effect(() => {
    app.activeView = 'clinic-' + page.params.section;
  });
</script>

<ClinicView />
