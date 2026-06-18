<script lang="ts">
  import { goto } from '$app/navigation';
  import { app } from '$lib/stores/app.svelte';
  import { viewToPath, basePath } from '$lib/nav';

  $effect(() => {
    if (app.currentUser) goto(viewToPath(app.activeView), { replaceState: true });
    else goto(`${basePath}/login`, { replaceState: true });
  });
</script>

<svelte:head>
  <title>Veterinary Clinic — Information System</title>
</svelte:head>

<div class="min-h-screen flex items-center justify-center bg-[#0D2E2B] text-teal-200 text-sm">Loading…</div>
