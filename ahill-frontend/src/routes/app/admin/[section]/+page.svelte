<script lang="ts">
  import { page } from '$app/state';
  import { app } from '$lib/stores/app.svelte';
  import DirectorView from '$lib/components/DirectorView.svelte';

  // Разделы admin-users / admin-accounts / admin-branches / admin-audit
  // обслуживаются тем же DirectorView (он переключается по app.activeView).
  $effect(() => {
    app.activeView = 'admin-' + page.params.section;
  });
</script>

<DirectorView />
