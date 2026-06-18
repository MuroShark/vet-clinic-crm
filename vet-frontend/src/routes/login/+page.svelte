<script lang="ts">
  import { goto } from '$app/navigation';
  import { app } from '$lib/stores/app.svelte';
  import { viewToPath } from '$lib/nav';
  import LoginScreen from '$lib/components/LoginScreen.svelte';

  $effect(() => {
    if (app.currentUser) goto(viewToPath(app.activeView), { replaceState: true });
  });
</script>

<svelte:head>
  <title>Sign In — Veterinary Clinic</title>
</svelte:head>

<LoginScreen />
